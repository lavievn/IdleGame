using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Opening: persistent clouds + optional full-window background video,
// one motionless splash-video frame at 27s, and UI fade/reveal.
// Camera and gameplay coordinates are never moved or scaled.
public sealed class OpeningCinematicUI : MonoBehaviour
{
    public const float ShowcaseCueSeconds=27f;
    public const float EntrySeconds=2.7f;
    private const float MenuRevealSeconds=1.5f;

    private GameManager game;
    private StartMenuUI menu;
    private RectTransform canvasArea, backdrop;
    private RectTransform sky, far, near, backCloud, cloudLeft, cloudRight;
    private Image baseImage;
    private CanvasGroup menuFade, backdropFade;
    private OpeningArtTheme theme;
    private OpeningBackgroundVideo videoBackground;
    private OpeningHeroSplashVideo heroVideo;
    private float elapsed, enterElapsed, menuAlphaBeforeEntry;
    private bool entering, finished, orderFixed;

    public static OpeningCinematicUI Install(GameManager gm, StartMenuUI startMenu,
        RectTransform menuRoot, CanvasGroup menuGroup, TextMeshProUGUI font)
    {
        if(gm==null||startMenu==null||menuRoot==null)return null;
        var layer=new GameObject("Opening Cinematic - Placeholder Layers",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(CanvasGroup));
        var rect=(RectTransform)layer.transform;
        rect.SetParent(menuRoot.parent,false);
        rect.SetAsLastSibling();
        menuRoot.SetAsLastSibling();
        var opening=layer.AddComponent<OpeningCinematicUI>();
        opening.game=gm;
        opening.menu=startMenu;
        opening.canvasArea=menuRoot;
        opening.menuFade=menuGroup;
        opening.backdrop=rect;
        opening.backdropFade=layer.GetComponent<CanvasGroup>();
        opening.backdropFade.blocksRaycasts=false;
        opening.theme=Resources.Load<OpeningArtTheme>("OpeningArtTheme");
        opening.Build(font);
        opening.Layout();
        return opening;
    }

    private RectTransform MakeLayer(string name,Color placeholder,Sprite sprite,
        TextMeshProUGUI template,string caption,out Image image)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rect=(RectTransform)go.transform;
        rect.SetParent(backdrop,false);
        image=go.GetComponent<Image>();
        image.sprite=sprite;
        image.color=sprite!=null?Color.white:placeholder;
        image.raycastTarget=false;
        image.preserveAspect=false;
        if(sprite==null&&template!=null&&!string.IsNullOrEmpty(caption))
        {
            var label=Instantiate(template.gameObject,rect);
            label.name="PLACEHOLDER - "+caption;
            label.SetActive(true);
            var text=label.GetComponent<TextMeshProUGUI>();
            if(text!=null)
            {
                text.text=caption;
                text.fontSize=14f;
                text.alignment=TextAlignmentOptions.Center;
                text.color=new Color(1f,1f,1f,.75f);
                text.raycastTarget=false;
                var box=(RectTransform)label.transform;
                box.anchorMin=box.anchorMax=box.pivot=new Vector2(.5f,.5f);
                box.anchoredPosition=Vector2.zero;
                box.sizeDelta=new Vector2(210,54);
            }
        }
        return rect;
    }

    private static void Place(RectTransform rect,Vector2 center,Vector2 size)
    {
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=center;
        rect.sizeDelta=size;
    }

    private void Build(TextMeshProUGUI font)
    {
        var art=theme;
        Image ignored;
        // Explicit draw order: video, sky, hills, clouds, splash, main menu.
        videoBackground=OpeningBackgroundVideo.Install(backdrop);
        sky=MakeLayer("01 SKY - full background",new Color(.23f,.48f,.59f,1f),
            art!=null?art.sky:null,font,"BẦU TRỜI / SKY",out baseImage);
        far=MakeLayer("02 FAR MOUNTAINS",new Color(.42f,.64f,.68f,1f),
            art!=null?art.farMountains:null,font,"NÚI XA / FAR MOUNTAINS",out ignored);
        near=MakeLayer("03 NEAR MOUNTAINS",new Color(.24f,.43f,.47f,1f),
            art!=null?art.nearMountains:null,font,"NÚI GẦN / NEAR MOUNTAINS",out ignored);
        backCloud=MakeLayer("04 BACK CLOUDS",new Color(.83f,.93f,.94f,.42f),
            art!=null?art.backClouds:null,font,"MÂY XA / BACK CLOUDS",out ignored);
        cloudLeft=MakeLayer("05 FRONT CLOUDS LEFT",new Color(.93f,.96f,1f,.84f),
            art!=null?art.leftClouds:null,font,"MÂY TRƯỚC / LEFT",out ignored);
        cloudRight=MakeLayer("06 FRONT CLOUDS RIGHT",new Color(.86f,.96f,1f,.84f),
            art!=null?art.rightClouds:null,font,"MÂY TRƯỚC / RIGHT",out ignored);
        heroVideo=OpeningHeroSplashVideo.Install(backdrop,font);
        // No duplicate game title here: logo is part of StartMenuUI instead.
    }

    public void Layout()
    {
        if(backdrop==null)return;
        float w=Mathf.Max(320f,ManagementWindowLayout.CombatPixels(Screen.width));
        float h=Mathf.Max(240f,ManagementWindowLayout.CombatHeight(Screen.height));
        backdrop.anchorMin=backdrop.anchorMax=backdrop.pivot=Vector2.zero;
        backdrop.anchoredPosition=Vector2.zero;
        backdrop.sizeDelta=new Vector2(w,h);
        if(videoBackground!=null)videoBackground.Resize(w,h);
        Place(sky,Vector2.zero,new Vector2(w*1.1f,h*1.12f));
        Place(far,new Vector2(0f,-h*.13f),new Vector2(w*1.18f,h*.62f));
        Place(near,new Vector2(0f,-h*.33f),new Vector2(w*1.24f,h*.34f));
        Place(backCloud,new Vector2(0f,h*.16f),new Vector2(w*1.2f,h*.44f));
        Place(cloudLeft,new Vector2(-w*.3f,-h*.04f),new Vector2(w*.67f,h*.91f));
        Place(cloudRight,new Vector2(w*.3f,h*.04f),new Vector2(w*.67f,h*.91f));
        if(heroVideo!=null)heroVideo.Layout(w,h);
    }

    private void Update()
    {
        if(finished||backdrop==null)return;
        if(!orderFixed)
        {
            backdrop.SetAsLastSibling();
            if(canvasArea!=null)canvasArea.SetAsLastSibling();
            if(game!=null&&game.confirmationPopup!=null&&game.confirmationPopup.activeInHierarchy)
                game.confirmationPopup.transform.SetAsLastSibling();
            orderFixed=true;
        }
        float dt=Mathf.Max(0f,Time.unscaledDeltaTime);
        if(entering) { AnimateEntry(dt);return; }
        if(game==null||game.preGameUI==null||!game.preGameUI.activeInHierarchy)return;
        elapsed+=dt;
        if(baseImage!=null)
        {
            var c=baseImage.color;
            c.a=videoBackground!=null&&videoBackground.IsDisplaying?.18f:1f;
            baseImage.color=c;
        }
        float m=Mathf.Clamp01((elapsed-.65f)/MenuRevealSeconds);
        if(menuFade!=null)menuFade.alpha=m*m*(3f-2f*m);
        float w=backdrop.rect.width,h=backdrop.rect.height;
        backCloud.anchoredPosition=new Vector2(Mathf.Sin(elapsed*.09f)*w*.035f,h*.16f);
        cloudLeft.anchoredPosition=new Vector2(-w*.30f+Mathf.Sin(elapsed*.12f)*w*.035f,
            -h*.04f+Mathf.Sin(elapsed*.19f)*h*.014f);
        cloudRight.anchoredPosition=new Vector2(w*.30f+Mathf.Sin(elapsed*.11f+1.9f)*w*.032f,
            h*.04f+Mathf.Sin(elapsed*.16f)*h*.017f);
        far.localScale=new Vector3(1f+Mathf.Sin(elapsed*.13f)*.008f,
            1f+Mathf.Sin(elapsed*.13f)*.008f,1f);
        // Fixed splash. Only one initial alpha fade is done by that component.
        if(heroVideo!=null)heroVideo.Tick(elapsed);
    }

    public bool IsEntering => entering;
    public void BeginEntering()
    {
        if(entering||finished)return;
        entering=true;
        enterElapsed=0f;
        menuAlphaBeforeEntry=menuFade!=null?menuFade.alpha:1f;
        if(menu!=null)menu.LockButtonsForEntry();
        if(menuFade!=null)menuFade.interactable=false;
    }

    private void AnimateEntry(float dt)
    {
        enterElapsed+=dt;
        float t=Mathf.Clamp01(enterElapsed/EntrySeconds);
        float eased=t*t*(3f-2f*t);
        float w=backdrop.rect.width,h=backdrop.rect.height;
        if(menuFade!=null)menuFade.alpha=menuAlphaBeforeEntry*(1f-Mathf.Clamp01(t*2.2f));
        if(backdropFade!=null)backdropFade.alpha=1f-eased;
        // The clouds and sky move out to reveal real gameplay underneath.
        // The splash VIDEO frame remains in exactly the same position;
        // its visibility follows the overall opening fade only.
        cloudLeft.anchoredPosition=new Vector2(-w*(.3f+1.2f*eased),-h*.04f);
        cloudRight.anchoredPosition=new Vector2(w*(.3f+1.2f*eased),h*.04f);
        backCloud.anchoredPosition=new Vector2(0f,h*(.16f-.6f*eased));
        float scale=1f+.7f*eased;
        far.localScale=near.localScale=new Vector3(scale,scale,1f);
        sky.localScale=new Vector3(1f+.15f*eased,1f+.15f*eased,1f);
        if(t<1f)return;
        finished=true;
        if(heroVideo!=null)heroVideo.StopVideo();
        if(videoBackground!=null)videoBackground.StopPlayback();
        if(game!=null&&game.preGameUI!=null)game.preGameUI.SetActive(false);
        if(UIManager.Instance!=null)UIManager.Instance.SetPaused(false);
        backdrop.gameObject.SetActive(false);
    }

    public void ResetForMenu()
    {
        if(backdrop==null)return;
        finished=entering=orderFixed=false;
        elapsed=enterElapsed=0f;
        if(backdropFade!=null)backdropFade.alpha=1f;
        if(heroVideo!=null)heroVideo.ResetToTitle();
        if(videoBackground!=null)videoBackground.StartPlayback();
        backdrop.gameObject.SetActive(true);
        backdrop.SetAsLastSibling();
        if(canvasArea!=null)canvasArea.SetAsLastSibling();
        if(menuFade!=null){menuFade.alpha=0f;menuFade.interactable=true;}
        sky.localScale=far.localScale=near.localScale=Vector3.one;
        Layout();
    }
}
