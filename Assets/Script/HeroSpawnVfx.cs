using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Actor presentation only. GameManager owns the entry lock and wave activation.
public sealed class HeroSpawnVfx : MonoBehaviour
{
    public const int FrameCount=38;
    public const float FramesPerSecond=30f;
    public const float Duration=FrameCount/FramesPerSecond;
    [Min(.1f)] public float heightMultiplier=1.4f;
    public Texture2D sheetOverride;
    public bool IsPlaying { get; private set; }
    private RectTransform actor;
    private Image effect,glow;
    private Sprite[] frames;
    private Sprite glowSprite;
    private Texture2D glowTexture;
    private float elapsed;
    private readonly Dictionary<Graphic,bool> hiddenUI = new Dictionary<Graphic,bool>();
    private bool prepared;
    private HeroController owner;
    public Image EffectGraphic => effect;
    public Image GlowGraphic => glow;
    public float Elapsed => elapsed;
    public int DisplayedFrame { get; private set; }=-1;

    public static HeroSpawnVfx Prepare(HeroController hero)
    {
        if(hero==null || hero.heroRect==null)return null;
        var fx=hero.heroRect.GetComponent<HeroSpawnVfx>();
        if(fx==null)fx=hero.heroRect.gameObject.AddComponent<HeroSpawnVfx>();
        fx.Finish();
        fx.actor=hero.heroRect;fx.heightMultiplier=hero.spawnVfxScale;fx.owner=hero;
        fx.prepared=true;
        fx.HoldForCinematic();
        return fx;
    }
    // Include late-created charge bars and nested TMP/RawImage graphics. Do not
    // deactivate the actor, change layout, or alter a graphic's intended alpha.
    private void HideAttachedUI()
    {
        var bridge=actor.GetComponent<CultivationSpriteBridge>();
        var body=bridge!=null?bridge.BodyGraphic:actor.GetComponent<Image>();
        foreach(var graphic in actor.GetComponentsInChildren<Graphic>(true))
        {
            if(graphic==body || graphic==effect || graphic==glow)continue;
            if(!hiddenUI.ContainsKey(graphic))hiddenUI.Add(graphic,graphic.enabled);
            graphic.enabled=false;
        }
    }
    public void HoldForCinematic()
    {
        if(!prepared || actor==null)return;
        HideAttachedUI();
        CultivationSpriteBridge.SetBodyOpacity(actor,0f);
        if(effect!=null)effect.gameObject.SetActive(false);
        if(glow!=null)glow.gameObject.SetActive(false);
    }
    public static void Cancel(RectTransform rect)
    {
        if(rect==null)return;
        var fx=rect.GetComponent<HeroSpawnVfx>();
        if(fx!=null)fx.Finish();
    }
    public bool Begin()
    {
        TraceRender("VFX Begin before",2);
        if(!isActiveAndEnabled || actor==null) { Finish();return false; }
        var sheet=sheetOverride!=null?sheetOverride:Resources.Load<Texture2D>("Spawn/Appear");
        if(sheet==null || sheet.width!=4000 || sheet.height!=2500) { Finish();return false; }
        if(frames==null)
        {
            frames=new Sprite[FrameCount];
            for(int i=0;i<FrameCount;i++)
                frames[i]=Sprite.Create(sheet,new Rect((i%8)*500,2000-(i/8)*500,500,500),
                    new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect);
        }
        if(effect==null)effect=MakeImage("Hero Appear Frames");
        if(glow==null)
        {
            glow=MakeImage("Hero Appear Body Light");
            glowTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            var pixels=new Color32[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++) {
                float edge=Mathf.Max(Mathf.Abs((x-31.5f)/31.5f),Mathf.Abs((y-31.5f)/31.5f));
                pixels[y*64+x]=new Color32(180,255,255,(byte)(255*Mathf.Clamp01((1f-edge)/.25f)));
            }
            glowTexture.SetPixels32(pixels);glowTexture.Apply();
            glowSprite=Sprite.Create(glowTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect);
            glow.sprite=glowSprite;
        }
        elapsed=0f;IsPlaying=true;effect.gameObject.SetActive(true);glow.gameObject.SetActive(true);
        CultivationSpriteBridge.SetBodyOpacity(actor,0f);Advance(0f);
        TraceRender("VFX Begin after",2);return true;
    }
    private Image MakeImage(string name)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=gameObject.layer;
        go.transform.SetParent(actor,false);
        go.transform.SetSiblingIndex(1); // Above body, below HP bar and popup siblings.
        var image=go.GetComponent<Image>();image.color=Color.white;image.raycastTarget=false;return image;
    }
    // Called once per unscaled GameManager.Update, also while gameplay is paused.
    public bool Advance(float dt)
    {
        if(!IsPlaying)return false;
        elapsed+=Mathf.Max(0f,dt);
        if(elapsed>=Duration || actor==null || !actor.gameObject.activeInHierarchy) {
            TraceRender("VFX terminating before Finish",2);Finish();return false;
        }
        HideAttachedUI();
        DisplayedFrame=Mathf.Clamp((int)(elapsed*FramesPerSecond),0,FrameCount-1);
        effect.sprite=frames[DisplayedFrame];
        float p=elapsed/Duration;
        float reveal=Mathf.Clamp01((p-.18f)/.27f);
        CultivationSpriteBridge.SetBodyOpacity(actor,reveal*reveal*(3f-2f*reveal));
        float light=Mathf.Clamp01(p/.16f)*Mathf.Clamp01((.75f-p)/.30f);
        glow.color=new Color(1f,1f,1f,light*.9f);
        var bridge=actor.GetComponent<CultivationSpriteBridge>();
        Vector2 size=bridge!=null?bridge.VisibleBodySize:actor.rect.size;
        // Canvas has ~91% alpha height above its ground ring at source y=455.
        // Supplement the narrow beam with soft body light covering the silhouette.
        float side=Mathf.Max(size.y*heightMultiplier/.91f,size.x*1.2f/.80f);
        Place(effect.rectTransform,new Vector2(.5f,.09f),Vector2.zero,new Vector2(side,side));
        Place(glow.rectTransform,new Vector2(.5f,.5f),new Vector2(0,size.y*.5f),size*heightMultiplier);
        TraceRender("VFX Advance after layout",2);
        return true;
    }
    private static void Place(RectTransform rect,Vector2 pivot,Vector2 offset,Vector2 size)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,0f);rect.pivot=pivot;
        rect.anchoredPosition=offset;rect.sizeDelta=size;
    }
    public void Finish()
    {
        bool trace=prepared || IsPlaying;
        if(trace)TraceRender("VFX Finish before",12);
        IsPlaying=false;prepared=false;elapsed=0f;
        CultivationSpriteBridge.SetBodyOpacity(actor!=null?actor:(RectTransform)transform,1f);
        foreach(var saved in hiddenUI)if(saved.Key!=null)saved.Key.enabled=saved.Value;
        hiddenUI.Clear();
        if(effect!=null)effect.gameObject.SetActive(false);
        if(glow!=null)glow.gameObject.SetActive(false);
        if(trace)TraceRender("VFX Finish after",12);
    }
    private void TraceRender(string stage,int frames)
    {
        if(owner!=null)owner.TraceVisualPosition(stage,frames);
    }
    private void OnDisable() { Finish(); }
    private void OnDestroy()
    {
        Finish();
        if(frames!=null)foreach(var frame in frames)if(frame!=null)Destroy(frame);
        if(glowSprite!=null)Destroy(glowSprite);if(glowTexture!=null)Destroy(glowTexture);
        if(effect!=null)Destroy(effect.gameObject);if(glow!=null)Destroy(glow.gameObject);
    }
}
