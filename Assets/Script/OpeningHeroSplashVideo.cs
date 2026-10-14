using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

// Each opening has two stationary overscanned side frames.
// A single random JPG/PNG/MP4 is picked once per frame per visit.
// Both sides appear at t=27s with ONE fade-in; there is no automatic
// slideshow, camera zoom/pan or crossfade afterward.
public sealed class OpeningHeroSplashVideo : MonoBehaviour
{
    public const float RevealSeconds = 1.4f;
    public const string MediaFolder = "OpeningSplashArt";
    private const string LegacyFolder = "OpeningSplash";
    private const string LegacyVideo = "splash.mp4";
    private const int TargetWidth = 640;
    private const int TargetHeight = 960;

    private bool leftSide;
    private RectTransform rect;
    private CanvasGroup fade;
    private RawImage display;
    private Image placeholder;
    private TextMeshProUGUI label;
    private VideoPlayer video;
    private RenderTexture videoTexture;
    private Texture2D loadedImage;
    private string chosenPath;
    private bool picked, videoReady, active;

    // Second side excludes the first side's selection when possible.
    public string CurrentPath => chosenPath;

    public static OpeningHeroSplashVideo Install(RectTransform parent,
        TextMeshProUGUI font, bool left)
    {
        if(parent==null)return null;
        var obj=new GameObject(left ? "07 SPLASH LEFT - OVERSCAN" :
            "08 SPLASH RIGHT - OVERSCAN", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Image),
            typeof(RectMask2D));
        var root=(RectTransform)obj.transform;
        root.SetParent(parent,false);
        var player=obj.AddComponent<OpeningHeroSplashVideo>();
        player.leftSide=left;
        player.rect=root;
        player.fade=obj.GetComponent<CanvasGroup>();
        player.fade.alpha=0f;
        player.fade.blocksRaycasts=false;
        player.placeholder=obj.GetComponent<Image>();
        player.placeholder.color=left
            ? new Color(.32f,.29f,.46f,.83f)
            : new Color(.39f,.27f,.39f,.83f);
        player.placeholder.raycastTarget=false;

        var surface=new GameObject("Random Image or Video",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
        surface.transform.SetParent(root,false);
        player.display=surface.GetComponent<RawImage>();
        player.display.enabled=false;
        player.display.raycastTarget=false;
        var mediaRect=(RectTransform)surface.transform;
        mediaRect.anchorMin=mediaRect.anchorMax=mediaRect.pivot=new Vector2(.5f,.5f);
        mediaRect.anchoredPosition=Vector2.zero;

        if(font!=null)
        {
            var labelObject=Instantiate(font.gameObject,root);
            labelObject.name="PLACEHOLDER - "+(left?"SPLASH LEFT":"SPLASH RIGHT");
            labelObject.SetActive(true);
            player.label=labelObject.GetComponent<TextMeshProUGUI>();
            if(player.label!=null)
            {
                player.label.text=left?"SPLASH TRÁI\nTHÊM ẢNH / VIDEO":
                    "SPLASH PHẢI\nTHÊM ẢNH / VIDEO";
                player.label.fontSize=15;
                player.label.alignment=TextAlignmentOptions.Center;
                player.label.color=new Color(1f,1f,1f,.8f);
                player.label.raycastTarget=false;
                var t=(RectTransform)player.label.transform;
                t.anchorMin=t.anchorMax=t.pivot=new Vector2(.5f,.5f);
                t.anchoredPosition=Vector2.zero;
                t.sizeDelta=new Vector2(220,72);
            }
        }
        player.Layout(parent.rect.width,parent.rect.height);
        return player;
    }

    public void Layout(float width,float height)
    {
        if(rect==null)return;
        width=Mathf.Max(320f,width);
        height=Mathf.Max(240f,height);
        // Both frames extend 20-25% outside the left/right viewport edges,
        // leaving the center clear for the title and four menu buttons.
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.sizeDelta=new Vector2(width*.68f,height*1.15f);
        rect.anchoredPosition=new Vector2((leftSide?-.415f:.415f)*width,-height*.02f);
        rect.localScale=Vector3.one;
        if(display!=null && display.enabled)FitMedia();
    }

    private static bool IsSupported(string path)
    {
        switch(Path.GetExtension(path).ToLowerInvariant())
        {
            case ".png":case ".jpg":case ".jpeg":case ".mp4":return true;
            default:return false;
        }
    }

    private static string[] GetAvailableFiles()
    {
        var dir=Path.Combine(Application.streamingAssetsPath,MediaFolder);
        if(Directory.Exists(dir))
        {
            var files=Directory.GetFiles(dir,"*",SearchOption.TopDirectoryOnly);
            var found=new System.Collections.Generic.List<string>();
            foreach(var file in files)if(IsSupported(file))found.Add(file);
            if(found.Count>0){found.Sort(StringComparer.OrdinalIgnoreCase);return found.ToArray();}
        }
        // Compatibility with an existing splash.mp4 from the previous build.
        var old=Path.Combine(Application.streamingAssetsPath,LegacyFolder,LegacyVideo);
        return File.Exists(old)?new[]{old}:new string[0];
    }

    private static string Choose(string[] candidates,string exclude)
    {
        if(candidates==null||candidates.Length==0)return null;
        int options=0;
        foreach(var path in candidates)
            if(candidates.Length==1||path!=exclude)options++;
        int index=UnityEngine.Random.Range(0,options);
        foreach(var path in candidates)
        {
            if(candidates.Length>1&&path==exclude)continue;
            if(index--==0)return path;
        }
        return candidates[0];
    }

    public void Tick(float seconds,string otherSidePath)
    {
        if(seconds<OpeningCinematicUI.ShowcaseCueSeconds)return;
        if(!picked)
        {
            picked=true;
            active=true;
            chosenPath=Choose(GetAvailableFiles(),otherSidePath);
            LoadChosen();
        }
        float t=Mathf.Clamp01((seconds-OpeningCinematicUI.ShowcaseCueSeconds) / RevealSeconds);
        fade.alpha=t*t*(3f-2f*t);
    }

    private void LoadChosen()
    {
        if(string.IsNullOrEmpty(chosenPath))return; // placeholder until user adds files
        try
        {
            if(string.Equals(Path.GetExtension(chosenPath),".mp4",StringComparison.OrdinalIgnoreCase))
                LoadVideo();
            else
                LoadStill();
        }
        catch(Exception ex)
        {
            Debug.LogWarning("[Opening] Cannot load splash "+chosenPath+": "+ex.Message);
            ShowPlaceholder();
        }
    }

    private void LoadStill()
    {
        byte[] bytes=File.ReadAllBytes(chosenPath);
        loadedImage=new Texture2D(2,2,TextureFormat.RGBA32,false);
        if(!ImageConversion.LoadImage(loadedImage,bytes,false))
        {
            Destroy(loadedImage);loadedImage=null;
            ShowPlaceholder();return;
        }
        display.texture=loadedImage;
        display.enabled=true;
        placeholder.enabled=false;
        if(label!=null)label.gameObject.SetActive(false);
        FitMedia();
    }

    private void LoadVideo()
    {
        videoTexture=new RenderTexture(TargetWidth,TargetHeight,0,RenderTextureFormat.ARGB32);
        videoTexture.Create();
        video=gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake=false;
        video.isLooping=true;
        video.skipOnDrop=true;
        video.source=VideoSource.Url;
        video.url=new Uri(Path.GetFullPath(chosenPath)).AbsoluteUri;
        video.renderMode=VideoRenderMode.RenderTexture;
        video.targetTexture=videoTexture;
        video.audioOutputMode=VideoAudioOutputMode.None;
        video.prepareCompleted+=OnVideoPrepared;
        video.errorReceived+=OnVideoError;
        display.texture=videoTexture;
        video.Prepare();
    }
    private void OnVideoPrepared(VideoPlayer source)
    {
        if(source!=video||!active)return;
        videoReady=true;
        display.enabled=true;
        placeholder.enabled=false;
        if(label!=null)label.gameObject.SetActive(false);
        FitMedia();
        source.Play();
    }
    private void OnVideoError(VideoPlayer source,string error)
    {
        Debug.LogWarning("[Opening] Splash video decoder failed: "+error);
        ShowPlaceholder();
        if(video!=null)video.Stop();
    }

    private void FitMedia()
    {
        if(display==null||rect==null)return;
        var t=(RectTransform)display.transform;
        float sourceWidth=loadedImage!=null?loadedImage.width:
            (videoReady&&video!=null?(float)video.width:TargetWidth);
        float sourceHeight=loadedImage!=null?loadedImage.height:
            (videoReady&&video!=null?(float)video.height:TargetHeight);
        if(sourceWidth<=0f||sourceHeight<=0f)return;
        // Cover fills the oversized frame, preserving aspect and cropping
        // outside its boundaries; this never stretches the author's art.
        float factor=Mathf.Max(rect.rect.width/sourceWidth,rect.rect.height/sourceHeight);
        t.anchorMin=t.anchorMax=t.pivot=new Vector2(.5f,.5f);
        t.anchoredPosition=Vector2.zero;
        t.sizeDelta=new Vector2(sourceWidth*factor,sourceHeight*factor);
    }

    private void ShowPlaceholder()
    {
        if(display!=null)display.enabled=false;
        if(placeholder!=null)placeholder.enabled=true;
        if(label!=null)label.gameObject.SetActive(true);
    }

    private void ReleaseMedia()
    {
        active=false;videoReady=false;
        if(video!=null)
        {
            video.prepareCompleted-=OnVideoPrepared;
            video.errorReceived-=OnVideoError;
            video.Stop();video.targetTexture=null;
            Destroy(video);video=null;
        }
        if(videoTexture!=null)
        {
            videoTexture.Release();Destroy(videoTexture);videoTexture=null;
        }
        if(loadedImage!=null){Destroy(loadedImage);loadedImage=null;}
        if(display!=null){display.texture=null;display.enabled=false;}
    }

    public void StopVideo(){ReleaseMedia();}
    public void ResetToTitle()
    {
        ReleaseMedia();
        picked=false;chosenPath=null;
        if(fade!=null)fade.alpha=0f;
        ShowPlaceholder();
    }
    private void OnDestroy(){ReleaseMedia();}
}
