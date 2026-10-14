using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

// A single fixed-size, motionless splash-video frame. No panning, zooming,
// random cuts, looping crossfades or independent left/right splash animations.
// The author handles all character motion/editing inside splash.mp4.
public sealed class OpeningHeroSplashVideo : MonoBehaviour
{
    public const float RevealSeconds = 1.4f;
    private const string DirectoryName = "OpeningSplash";
    private const string FileName = "splash.mp4";

    private RectTransform rect;
    private CanvasGroup group;
    private RawImage surface;
    private Image placeholder;
    private TextMeshProUGUI placeholderLabel;
    private VideoPlayer player;
    private RenderTexture texture;
    private bool ready;
    private float revealElapsed;

    public static OpeningHeroSplashVideo Install(RectTransform parent, TextMeshProUGUI font)
    {
        if(parent==null)return null;
        var go=new GameObject("07 SPLASH VIDEO - FIXED NO PAN",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(CanvasGroup),typeof(Image));
        var rect=(RectTransform)go.transform;
        rect.SetParent(parent,false);
        var controller=go.AddComponent<OpeningHeroSplashVideo>();
        controller.rect=rect;
        controller.group=go.GetComponent<CanvasGroup>();
        controller.group.alpha=0f;
        controller.group.blocksRaycasts=false;
        controller.placeholder=go.GetComponent<Image>();
        controller.placeholder.color=new Color(.23f,.29f,.38f,.82f);
        controller.placeholder.raycastTarget=false;
        var display=new GameObject("Splash Video Display",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
        display.transform.SetParent(rect,false);
        controller.surface=display.GetComponent<RawImage>();
        controller.surface.raycastTarget=false;
        controller.surface.enabled=false;
        var videoRect=(RectTransform)display.transform;
        videoRect.anchorMin=Vector2.zero;
        videoRect.anchorMax=Vector2.one;
        videoRect.offsetMin=Vector2.zero;
        videoRect.offsetMax=Vector2.zero;
        if(font!=null)
        {
            var label=Instantiate(font.gameObject,rect);
            label.name="SPLASH VIDEO - ADD splash.mp4";
            label.SetActive(true);
            controller.placeholderLabel=label.GetComponent<TextMeshProUGUI>();
            if(controller.placeholderLabel!=null)
            {
                controller.placeholderLabel.text="KHUNG VIDEO SPLASH\nCHỜ TỆP splash.mp4";
                controller.placeholderLabel.fontSize=15f;
                controller.placeholderLabel.alignment=TextAlignmentOptions.Center;
                controller.placeholderLabel.color=new Color(1f,1f,1f,.82f);
                controller.placeholderLabel.raycastTarget=false;
                var t=(RectTransform)controller.placeholderLabel.transform;
                t.anchorMin=t.anchorMax=t.pivot=new Vector2(.5f,.5f);
                t.anchoredPosition=Vector2.zero;
                t.sizeDelta=new Vector2(260,70);
            }
        }
        controller.Layout(parent.rect.width,parent.rect.height);
        return controller;
    }

    public void Layout(float w,float h)
    {
        if(rect==null)return;
        // One stationary, centered frame. Change these multipliers later
        // without touching the individual MP4 transitions inside the video.
        w=Mathf.Max(320f,w);h=Mathf.Max(240f,h);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=new Vector2(0f,-h*.005f);
        rect.sizeDelta=new Vector2(w*.90f,h*.79f);
        rect.localScale=Vector3.one;
    }

    private void PrepareVideo()
    {
        if(player!=null || rect==null)return;
        var path=Path.Combine(Application.streamingAssetsPath,DirectoryName,FileName);
        if(!File.Exists(path))return;
        // No decoder until the showcase cue. The RenderTexture bounds
        // decoding/display work, not the on-disk file size.
        texture=new RenderTexture(960,540,0,RenderTextureFormat.ARGB32);
        texture.Create();
        player=gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake=false;
        player.isLooping=true;
        player.skipOnDrop=true;
        player.source=VideoSource.Url;
        player.url=new Uri(Path.GetFullPath(path)).AbsoluteUri;
        player.renderMode=VideoRenderMode.RenderTexture;
        player.targetTexture=texture;
        player.audioOutputMode=VideoAudioOutputMode.None;
        player.prepareCompleted+=OnPrepared;
        player.errorReceived+=OnError;
        surface.texture=texture;
        player.Prepare();
    }
    private void OnPrepared(VideoPlayer value)
    {
        if(value!=player)return;
        ready=true;
        surface.enabled=true;
        placeholder.enabled=false;
        if(placeholderLabel!=null)placeholderLabel.gameObject.SetActive(false);
        player.Play();
    }
    private void OnError(VideoPlayer value,string error)
    {
        Debug.LogWarning("[Opening] Splash video not playable, placeholder retained: "+error);
        ready=false;
        surface.enabled=false;
        placeholder.enabled=true;
        if(placeholderLabel!=null)placeholderLabel.gameObject.SetActive(true);
        if(player!=null)player.Stop();
    }

    public void Tick(float openingSeconds)
    {
        if(openingSeconds<OpeningCinematicUI.ShowcaseCueSeconds)return;
        if(player==null)PrepareVideo();
        revealElapsed=openingSeconds-OpeningCinematicUI.ShowcaseCueSeconds;
        float t=Mathf.Clamp01(revealElapsed/RevealSeconds);
        group.alpha=t*t*(3f-2f*t);
    }

    public void StopVideo()
    {
        if(player!=null)player.Stop();
        if(surface!=null)surface.enabled=false;
    }
    public void ResetToTitle()
    {
        revealElapsed=0f;
        group.alpha=0f;
        if(player!=null)player.Stop();
        ready=false;
        if(surface!=null)surface.enabled=false;
        if(placeholder!=null)placeholder.enabled=true;
        if(placeholderLabel!=null)placeholderLabel.gameObject.SetActive(true);
    }
    private void OnDestroy()
    {
        if(player!=null)
        {
            player.prepareCompleted-=OnPrepared;
            player.errorReceived-=OnError;
            player.Stop();
            player.targetTexture=null;
        }
        if(texture!=null)
        {
            texture.Release();
            Destroy(texture);
        }
    }
}
