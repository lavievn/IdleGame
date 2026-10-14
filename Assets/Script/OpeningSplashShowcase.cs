using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

// Optional media carousel. No media means OpeningCinematicUI's existing
// purple/pink placeholder art remains exactly as it was in 0.0.53.
// The single VideoPlayer is shared by both sides to bound video decoding.
public sealed class OpeningSplashShowcase : MonoBehaviour
{
    public const float MinimumHoldSeconds = 4.8f;
    public const float MaximumHoldSeconds = 7.4f;
    public const float FadeSeconds = .7f;
    private const int VideoWidth = 512, VideoHeight = 896;

    private sealed class Frame
    {
        public RectTransform rect;
        public CanvasGroup fade;
        public Image picture;
        public RawImage video;
        public bool isVideo;
        public string key;
        public int motion;
    }
    private sealed class Lane
    {
        public RectTransform holder;
        public Image placeholder;
        public Color originalColor;
        public TextMeshProUGUI caption;
        public Frame[] frames = new Frame[2];
        public int active;
        public float age, duration, mix;
        public bool started, transitioning;
    }

    private readonly Lane[] lanes = {new Lane(),new Lane()};
    private OpeningSplashCatalog catalog;
    private string[] images = new string[0], videos = new string[0];
    private VideoPlayer player;
    private RenderTexture videoTexture;
    private Frame videoFrame;
    private int videoLane = -1;
    private bool activated;

    public bool HasMedia => images.Length + videos.Length > 0;

    public void Configure(RectTransform left, RectTransform right,
        Image leftBase, Image rightBase, TextMeshProUGUI leftLabel,
        TextMeshProUGUI rightLabel)
    {
        lanes[0].holder = left; lanes[1].holder = right;
        lanes[0].placeholder = leftBase; lanes[1].placeholder = rightBase;
        lanes[0].caption = leftLabel; lanes[1].caption = rightLabel;
        catalog = Resources.Load<OpeningSplashCatalog>("OpeningSplashCatalog");
        if (catalog != null)
        {
            images = catalog.images ?? new string[0];
            videos = catalog.videos ?? new string[0];
        }
        if (!HasMedia) return;
        for (int side=0;side<2;side++)
        {
            var slot=lanes[side];
            slot.originalColor=slot.placeholder!=null?slot.placeholder.color:Color.white;
            if (slot.holder.GetComponent<RectMask2D>()==null)
                slot.holder.gameObject.AddComponent<RectMask2D>();
            for (int i=0;i<2;i++)
                slot.frames[i] = MakeFrame(slot.holder,side,i);
        }
        Debug.Log("[OpeningSplash] Available: " + images.Length + " sprites, " +
            videos.Length + " videos (one video decoder max).");
    }

    private static Frame MakeFrame(RectTransform parent,int lane,int index)
    {
        var go=new GameObject("Splash "+(lane==0?"Left":"Right")+" Shot "+index,
            typeof(RectTransform),typeof(CanvasRenderer),typeof(CanvasGroup),
            typeof(RectMask2D));
        var root=(RectTransform)go.transform;
        root.SetParent(parent,false);
        root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);
        root.pivot=new Vector2(.5f,.5f);
        root.anchoredPosition=Vector2.zero;
        root.sizeDelta=parent.sizeDelta;
        var frame=new Frame{rect=root,fade=go.GetComponent<CanvasGroup>()};
        frame.fade.alpha=0f;
        frame.fade.blocksRaycasts=false;
        var image=new GameObject("IMAGE",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        image.transform.SetParent(root,false);
        frame.picture=image.GetComponent<Image>();
        frame.picture.raycastTarget=false;
        frame.picture.preserveAspect=true;
        Fit((RectTransform)image.transform,root.sizeDelta);
        var film=new GameObject("VIDEO",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
        film.transform.SetParent(root,false);
        frame.video=film.GetComponent<RawImage>();
        frame.video.raycastTarget=false;
        frame.video.enabled=false;
        Fit((RectTransform)film.transform,root.sizeDelta);
        return frame;
    }

    private static void Fit(RectTransform rect,Vector2 size)
    {
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=Vector2.zero;
        rect.sizeDelta=size;
    }

    private void EnsurePlayer()
    {
        if (player != null) return;
        videoTexture = new RenderTexture(VideoWidth,VideoHeight,0,RenderTextureFormat.ARGB32);
        videoTexture.Create();
        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake=false;
        player.source=VideoSource.VideoClip;
        player.renderMode=VideoRenderMode.RenderTexture;
        player.targetTexture=videoTexture;
        player.audioOutputMode=VideoAudioOutputMode.None;
        player.isLooping=true;
        player.skipOnDrop=true;
        player.prepareCompleted += OnPrepared;
        player.errorReceived += OnVideoError;
    }
    private void OnPrepared(VideoPlayer source)
    {
        if (videoFrame == null || source != player) return;
        videoFrame.video.enabled=true;
        player.Play();
    }
    private void OnVideoError(VideoPlayer source,string error)
    {
        Debug.LogWarning("[OpeningSplash] Video unavailable: " + error);
        if(videoFrame!=null)
        {
            videoFrame.video.enabled=false;
            for(int i=0;i<2;i++)
                if(lanes[i].frames[0]==videoFrame || lanes[i].frames[1]==videoFrame)
                {
                    if(lanes[i].placeholder!=null)
                        lanes[i].placeholder.color=lanes[i].originalColor;
                    if(lanes[i].caption!=null)lanes[i].caption.gameObject.SetActive(true);
                }
        }
        StopVideo();
    }
    private void StopVideo()
    {
        if(player!=null) {player.Stop();player.clip=null;}
        if(videoFrame!=null)
        {
            videoFrame.video.enabled=false;
            videoFrame.video.texture=null;
        }
        videoFrame=null;
        videoLane=-1;
    }

    private struct Pick
    {
        public string key;
        public bool video;
    }

    private Pick Choose(int lane,string avoid)
    {
        // An H.264 decode stream is relatively costly on modified GPUs:
        // only allocate one at once, and prefer stills on the other side.
        bool videoAllowed=videoLane<0 || videoLane==lane;
        int count=images.Length+(videoAllowed?videos.Length:0);
        if(count==0)return new Pick();
        int choice=0;
        for(int tries=0;tries<6;tries++)
        {
            choice=Random.Range(0,count);
            string name=choice<images.Length?images[choice]:videos[choice-images.Length];
            if(name!=avoid && (lanes[1-lane].frames[lanes[1-lane].active]==null ||
                name!=lanes[1-lane].frames[lanes[1-lane].active].key))break;
        }
        return choice<images.Length
            ? new Pick{key=images[choice],video=false}
            : new Pick{key=videos[choice-images.Length],video=true};
    }

    private bool PutMedia(int lane,Frame target,string avoid)
    {
        var pick=Choose(lane,avoid);
        if(string.IsNullOrEmpty(pick.key))return false;
        target.picture.sprite=null;
        target.picture.enabled=false;
        target.video.enabled=false;
        target.key=pick.key;
        target.isVideo=pick.video;
        target.motion=Random.Range(0,4);
        if(pick.video)
        {
            VideoClip clip=Resources.Load<VideoClip>(pick.key);
            if(clip==null)return false;
            EnsurePlayer();
            // On video-to-video changes, a brief fade-to-background hides the
            // decoder switch. Still images are normally preferred meanwhile.
            if(videoFrame!=null)StopVideo();
            videoFrame=target;
            videoLane=lane;
            target.video.texture=videoTexture;
            player.clip=clip;
            player.Prepare();
        }
        else
        {
            var sprite=Resources.Load<Sprite>(pick.key);
            if(sprite==null)return false;
            target.picture.sprite=sprite;
            target.picture.enabled=true;
        }
        return true;
    }

    private void StartLane(int lane)
    {
        var slot=lanes[lane];
        if(slot.started)return;
        slot.started=true;
        slot.age=0f;
        slot.duration=Random.Range(MinimumHoldSeconds,MaximumHoldSeconds);
        slot.active=0;
        if(PutMedia(lane,slot.frames[0],null))
        {
            slot.frames[0].fade.alpha=1f;
            if(slot.placeholder!=null)
            {
                Color c=slot.originalColor;c.a=0f;slot.placeholder.color=c;
            }
            if(slot.caption!=null)slot.caption.gameObject.SetActive(false);
        }
    }
    private void ChangeLane(int lane)
    {
        var slot=lanes[lane];
        int next=1-slot.active;
        Frame old=slot.frames[slot.active], incoming=slot.frames[next];
        if(!PutMedia(lane,incoming,old.key))
        {
            slot.age=0f;
            slot.duration=Random.Range(MinimumHoldSeconds,MaximumHoldSeconds);
            return;
        }
        incoming.rect.SetAsLastSibling();
        incoming.fade.alpha=0f;
        slot.transitioning=true;
        slot.mix=0f;
    }

    private static float Smooth(float x)
    {
        x=Mathf.Clamp01(x); return x*x*(3f-2f*x);
    }

    private static void Animate(Frame frame,float age,int side)
    {
        if(frame==null || string.IsNullOrEmpty(frame.key))return;
        // Speed-ramped virtual camera: 1) dolly forward, 2) snap-in portrait,
        // 3) pull back, 4) slow lateral drift. Images and videos use the same rig.
        float u=Mathf.Clamp01(age/5.5f);
        float fast=Smooth(Mathf.Clamp01((u-.38f)/.18f));
        float scale;
        switch(frame.motion)
        {
            case 1: scale=1.02f+.12f*u+.30f*fast;break;
            case 2: scale=1.43f-.36f*Smooth(u);break;
            case 3: scale=1.09f+.17f*Smooth(u);break;
            default:scale=1.03f+.20f*Smooth(u);break;
        }
        frame.rect.localScale=new Vector3(scale,scale,1f);
        float drift=(frame.motion==2?-1f:1f)*(side==0?1f:-1f);
        float snap=(frame.motion==1?fast:Smooth(u));
        frame.rect.anchoredPosition=new Vector2(drift*(9f*u+12f*snap),3f*Smooth(u));
    }

    public void Tick(float elapsed,float dt,float visibleAlpha)
    {
        if(!HasMedia || visibleAlpha<=0f)return;
        if(!activated)
        {
            activated=true;
            StartLane(0);
            StartLane(1);
            // Offset so both sides do not cut exactly on the same beat.
            lanes[1].age=-1.65f;
        }
        for(int lane=0;lane<2;lane++)
        {
            var slot=lanes[lane];
            if(!slot.started)continue;
            slot.age+=dt;
            Animate(slot.frames[slot.active],Mathf.Max(0,slot.age),lane);
            if(slot.transitioning)
            {
                slot.mix=Mathf.Min(1f,slot.mix+dt/FadeSeconds);
                int incoming=1-slot.active;
                slot.frames[incoming].fade.alpha=Smooth(slot.mix);
                slot.frames[slot.active].fade.alpha=1f-Smooth(slot.mix);
                Animate(slot.frames[incoming],slot.mix, lane);
                if(slot.mix>=1f)
                {
                    Frame old=slot.frames[slot.active];
                    old.fade.alpha=0f;
                    if(old.isVideo && videoFrame==old)StopVideo();
                    slot.active=incoming;
                    slot.age=0f;
                    slot.duration=Random.Range(MinimumHoldSeconds,MaximumHoldSeconds);
                    slot.transitioning=false;
                }
            }
            else if(slot.age>=slot.duration)
                ChangeLane(lane);
        }
    }

    public void Resize()
    {
        if(!HasMedia)return;
        foreach(var lane in lanes)
        {
            if(lane.holder==null)continue;
            for(int i=0;i<2;i++)
            {
                var f=lane.frames[i];if(f==null)continue;
                Fit(f.rect,lane.holder.sizeDelta);
                Fit((RectTransform)f.picture.transform,lane.holder.sizeDelta);
                Fit((RectTransform)f.video.transform,lane.holder.sizeDelta);
            }
        }
    }

    public void Deactivate()
    {
        if(player!=null)StopVideo();
    }

    public void ResetShowcase()
    {
        Deactivate();
        activated=false;
        foreach(var lane in lanes)
        {
            lane.started=false;lane.transitioning=false;lane.age=0f;
            if(lane.placeholder!=null)lane.placeholder.color=lane.originalColor;
            if(lane.caption!=null)lane.caption.gameObject.SetActive(true);
            foreach(var frame in lane.frames)
                if(frame!=null)
                {
                    frame.fade.alpha=0f;frame.key=null;frame.isVideo=false;
                    frame.picture.sprite=null;frame.picture.enabled=false;
                    frame.video.enabled=false;frame.video.texture=null;
                    frame.rect.localScale=Vector3.one;
                    frame.rect.anchoredPosition=Vector2.zero;
                }
        }
    }
    private void OnDestroy()
    {
        Deactivate();
        if(player!=null)
        {
            player.prepareCompleted-=OnPrepared;
            player.errorReceived-=OnVideoError;
        }
        if(videoTexture!=null){videoTexture.Release();Destroy(videoTexture);}
    }
}
