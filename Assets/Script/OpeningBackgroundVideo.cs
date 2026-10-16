using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Full-window looping video behind clouds/mountains/hero/menu.
// No video assigned => existing solid sky placeholder continues unchanged.
// Place opening.mp4 at Assets/StreamingAssets/OpeningBackground/opening.mp4.
public sealed class OpeningBackgroundVideo : MonoBehaviour
{
    private const string RelativeVideoPath = "OpeningBackground/opening.mp4";
    private const int VideoWidth = 960;
    private const int VideoHeight = 540;

    private RectTransform surface;
    private RawImage picture;
    private VideoPlayer player;
    private RenderTexture target;
    private bool prepared;
    private bool preparing;
    private bool wantPlayback;
    private bool available;
    private bool reportedError;

    public bool IsDisplaying => prepared && picture != null && picture.enabled;

    public static OpeningBackgroundVideo Install(RectTransform parent)
    {
        if (parent == null) return null;
        var go = new GameObject("00 VIDEO BACKGROUND - full window",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent,false);
        rect.SetAsFirstSibling();
        var layer = go.AddComponent<OpeningBackgroundVideo>();
        layer.surface=rect;
        layer.picture=go.GetComponent<RawImage>();
        layer.picture.color=Color.white;
        layer.picture.raycastTarget=false;
        layer.picture.enabled=false;
        layer.Resize(parent.rect.width,parent.rect.height);
        layer.StartPlayback();
        return layer;
    }

    public void Resize(float width,float height)
    {
        if(surface==null)return;
        surface.anchorMin=Vector2.zero;
        surface.anchorMax=Vector2.one;
        surface.offsetMin=Vector2.zero;
        surface.offsetMax=Vector2.zero;
    }

    public void StartPlayback()
    {
        wantPlayback=true;
        if(player!=null)
        {
            if(prepared) {picture.enabled=true;player.Play();}
            // Repeated return-to-title must not call Prepare twice while a
            // previous VideoPlayer.Prepare() is still pending.
            else if(available && !preparing) {preparing=true;player.Prepare();}
            return;
        }
        string path=Path.Combine(Application.streamingAssetsPath,
            "OpeningBackground","opening.mp4");
        if(!File.Exists(path)) return;
        available=true;
        target=new RenderTexture(VideoWidth,VideoHeight,0,RenderTextureFormat.ARGB32);
        target.Create();
        player=gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake=false;
        player.isLooping=true;
        player.skipOnDrop=true;
        player.source=VideoSource.Url;
        player.url=new Uri(Path.GetFullPath(path)).AbsoluteUri;
        player.renderMode=VideoRenderMode.RenderTexture;
        player.targetTexture=target;
        player.audioOutputMode=VideoAudioOutputMode.None;
        player.prepareCompleted+=OnVideoReady;
        player.errorReceived+=OnVideoError;
        picture.texture=target;
        preparing=true;
        player.Prepare();
    }

    private void OnVideoReady(VideoPlayer ready)
    {
        if(ready!=player || !preparing)return;
        preparing=false;
        prepared=true;
        reportedError=false;
        // A late decoder callback is not allowed to restart video after the
        // menu has closed or a new game has begun.
        if(!wantPlayback || !gameObject.activeInHierarchy)return;
        if(picture!=null)picture.enabled=true;
        ready.Play();
        Debug.Log("[Opening] Background video ready: "+RelativeVideoPath);
    }

    private void OnVideoError(VideoPlayer source,string error)
    {
        if(!reportedError && wantPlayback)
        {
            Debug.LogWarning("[Opening] Video background unavailable ("+
                RelativeVideoPath+"); using sky placeholder. Decoder: "+error);
            reportedError=true;
        }
        prepared=false;
        preparing=false;
        if(picture!=null)picture.enabled=false;
        if(player!=null)player.Stop();
    }

    public void StopPlayback()
    {
        wantPlayback=false;
        if(player!=null)
        {
            if(preparing)
            {
                // Cancel an in-flight Prepare before the next title visit.
                player.Stop();
                preparing=false;
                prepared=false;
            }
            else if(prepared)player.Pause();
        }
        if(picture!=null)picture.enabled=false;
    }

    private void OnDestroy()
    {
        wantPlayback=false;
        preparing=false;
        if(player!=null)
        {
            player.prepareCompleted-=OnVideoReady;
            player.errorReceived-=OnVideoError;
            player.Stop();
            player.targetTexture=null;
        }
        if(target!=null)
        {
            target.Release();
            Destroy(target);
        }
    }
}
