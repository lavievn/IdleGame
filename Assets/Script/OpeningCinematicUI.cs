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
    // Opening timeline: full black -> scenery fades in -> 3s pause ->
    // title and buttons fade in together. Splash keeps its 27s cue.
    public const float BlackFadeSeconds=1.8f;
    public const float MenuDelayAfterSceneSeconds=3f;
    public const float MenuRevealSeconds=1.5f;

    public static float BlackCurtainAlphaAt(float seconds)
    {
        float t=Mathf.Clamp01(seconds/BlackFadeSeconds);
        return 1f-t*t*(3f-2f*t);
    }
    public static float MenuAlphaAt(float seconds)
    {
        float start=BlackFadeSeconds+MenuDelayAfterSceneSeconds;
        float t=Mathf.Clamp01((seconds-start)/MenuRevealSeconds);
        return t*t*(3f-2f*t);
    }

    private GameManager game;
    private StartMenuUI menu;
    private RectTransform canvasArea, backdrop;
    private RectTransform sky, far, near, backCloud, cloudLeft, cloudRight;
    private Image baseImage;
    private TextMeshProUGUI skyPlaceholderLabel;
    private CanvasGroup menuFade, backdropFade;
    private Image blackCurtain;
    private OpeningArtTheme theme;
    private OpeningBackgroundVideo videoBackground;
    private OpeningHeroSplashVideo splashLeft, splashRight;
    private float elapsed, enterElapsed, menuAlphaBeforeEntry;
    private bool entering, finished, orderFixed, layoutEditing;
    private Vector2 backCloudBase, cloudLeftBase, cloudRightBase;

    // Stable identifiers for snapshot/restore. Must match GetLayoutRects().
    public static readonly string[] LayoutKeys={
        "sky","farMountains","nearMountains","backClouds",
        "leftClouds","rightClouds","splashLeft","splashRight",
        "gameTitle","continueButton","newGameButton","settingsButton","quitButton"
    };
    public bool IsLayoutEditing => layoutEditing;
    public OpeningArtTheme LayoutTheme => theme;

    // UIManager may reparent preGameUI into its runtime HUD after GameManager
    // created the opening siblings. Keep sky/splash/menu in ONE coordinate
    // space, rather than leaving the scenery under the old canvas.
    public void EnsureOpeningSiblingsParent()
    {
        if(canvasArea==null||backdrop==null||canvasArea.parent==null)return;
        var parent=canvasArea.parent;
        if(backdrop.parent!=parent)
        {
            backdrop.SetParent(parent,false);
            backdrop.SetAsLastSibling();
            canvasArea.SetAsLastSibling();
        }
        if(blackCurtain!=null&&blackCurtain.transform.parent!=parent)
        {
            blackCurtain.transform.SetParent(parent,false);
            blackCurtain.transform.SetAsLastSibling();
        }
    }

    // Refuse to serialize positions from a split/misaligned canvas.
    // A bad snapshot otherwise persists for every future Play session.
    public bool ValidateLayoutForSave(out string problem)
    {
        problem="";
        if(!layoutEditing||backdrop==null||canvasArea==null)
        {
            problem="Chưa bật chế độ căn chỉnh hoặc thiếu Canvas opening.";
            return false;
        }
        if(backdrop.parent!=canvasArea.parent)
        {
            problem="Các lớp opening và menu đang nằm dưới hai Canvas khác nhau.";
            return false;
        }
        float w=backdrop.rect.width,h=backdrop.rect.height;
        if(w<320f||h<240f||Mathf.Abs(w-canvasArea.rect.width)>1f||
           Mathf.Abs(h-canvasArea.rect.height)>1f||
           Mathf.Abs(backdrop.anchoredPosition.x-canvasArea.anchoredPosition.x)>1f||
           Mathf.Abs(backdrop.anchoredPosition.y-canvasArea.anchoredPosition.y)>1f)
        {
            problem="Root UI đang lệch hoặc kích thước hai Canvas không khớp. "+
                "Dừng Play, kiểm tra lại rồi mới lưu.";
            return false;
        }
        var rects=GetLayoutRects();
        if(rects.Length!=LayoutKeys.Length)
        {
            problem="Số đối tượng trong opening không khớp.";
            return false;
        }
        for(int i=0;i<rects.Length;i++)
        {
            var r=rects[i];
            if(r==null||r.rect.width<=1f||r.rect.height<=1f||
               float.IsNaN(r.anchoredPosition.x)||float.IsNaN(r.anchoredPosition.y)||
               float.IsNaN(r.sizeDelta.x)||float.IsNaN(r.sizeDelta.y)||
               float.IsInfinity(r.anchoredPosition.x)||
               float.IsInfinity(r.anchoredPosition.y)||
               float.IsInfinity(r.sizeDelta.x)||float.IsInfinity(r.sizeDelta.y))
            {
                problem="Khung "+LayoutKeys[i]+" có vị trí hoặc kích thước không hợp lệ.";
                return false;
            }
        }
        return true;
    }

    // Reference units make snapshots responsive across 600/800/1150 widths.
    public static void ApplySavedRect(OpeningArtTheme art,string key,
        RectTransform rect,float width,float height)
    {
        if(art==null||rect==null)return;
        var saved=art.FindLayout(key);
        if(saved==null||saved.size.x<=0f||saved.size.y<=0f)return;
        rect.anchoredPosition=new Vector2(saved.position.x*(width/800f),
            saved.position.y*(height/450f));
        rect.sizeDelta=new Vector2(saved.size.x*(width/800f),
            saved.size.y*(height/450f));
    }

    public RectTransform[] GetLayoutRects()
    {
        var title=menu!=null?menu.GetOpeningLayoutRects():
            new RectTransform[5];
        return new RectTransform[]{
            sky,far,near,backCloud,cloudLeft,cloudRight,
            splashLeft!=null?(RectTransform)splashLeft.transform:null,
            splashRight!=null?(RectTransform)splashRight.transform:null,
            title[0],title[1],title[2],title[3],title[4]
        };
    }

    // Captures the transforms the artist sees, not formulas or drifting
    // animation coordinates. Only call while layoutEditing is true.
    public OpeningLayoutEntry[] CaptureLayout()
    {
        string problem;
        if(!ValidateLayoutForSave(out problem))
        {
            Debug.LogWarning("[Opening Layout] Không thể lưu: "+problem);
            return null;
        }
        float w=Mathf.Max(1f,backdrop.rect.width);
        float h=Mathf.Max(1f,backdrop.rect.height);
        var rects=GetLayoutRects();
        var snapshot=new OpeningLayoutEntry[LayoutKeys.Length];
        for(int i=0;i<snapshot.Length;i++)
        {
            var r=rects[i];
            if(r==null)return null;
            snapshot[i]=new OpeningLayoutEntry{
                key=LayoutKeys[i],
                position=new Vector2(r.anchoredPosition.x*800f/w,
                    r.anchoredPosition.y*450f/h),
                size=new Vector2(r.sizeDelta.x*800f/w,
                    r.sizeDelta.y*450f/h)
            };
        }
        return snapshot;
    }

    // Enters a frozen preview where manual RectTransform edits are not
    // overwritten by frame animation or UIManager.LayoutReadableUI().
    public bool SetLayoutEditing(bool edit)
    {
        if(backdrop==null)return false;
        if(edit)
        {
            if(game==null||game.preGameUI==null||
                !game.preGameUI.activeInHierarchy||finished||entering)
                return false;
            // Do NOT call Layout(true) here: it resets the hand-adjusted
            // rectangles to hardcoded formulas, causing the entire opening
            // to jump to the corner before it can be saved.
            EnsureOpeningSiblingsParent();
            layoutEditing=true;
            if(blackCurtain!=null)blackCurtain.gameObject.SetActive(false);
            if(backdropFade!=null)backdropFade.alpha=1f;
            if(menu!=null)menu.PrepareLayoutEditing();
            if(menuFade!=null)menuFade.alpha=1f;
            float showAt=ShowcaseCueSeconds+OpeningHeroSplashVideo.RevealSeconds;
            if(splashLeft!=null)splashLeft.Tick(showAt,null);
            if(splashRight!=null)splashRight.Tick(showAt,
                splashLeft!=null?splashLeft.CurrentPath:null);
        }
        else if(layoutEditing)
        {
            layoutEditing=false;
            ResetForMenu(); // Replay the opening with newly saved layout.
        }
        return true;
    }

    public void ReloadSavedLayoutForEditing()
    {
        if(!layoutEditing)return;
        // Reset live transforms to the saved layout without restarting Play.
        if(menu!=null)menu.ForceLayoutForEditor();
        Layout(true);
    }

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
        opening.BuildBlackCurtain();
        opening.EnsureOpeningSiblingsParent();
        opening.Layout();
        opening.menu.SetOpeningMenuReady(false);
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
        // Only the artwork layers get a soft edge. The full-video layer,
        // sky and splash frames retain their original opacity.
        if(name.Contains("MOUNTAINS") || name.Contains("CLOUDS"))
            go.AddComponent<OpeningEdgeFeather>().Configure(image,
                theme!=null?theme.edgeFeatherPixels:48f);
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

    // A reference canvas preserves the existing aspect-responsive sizing.
    // Overrides are editable from OpeningArtTheme.asset in the Inspector.
    public static Vector2 ResolveLayerSize(Vector2 referenceSize,float width,
        float height,Vector2 fallback)
    {
        if(referenceSize.x<=0f || referenceSize.y<=0f)return fallback;
        return new Vector2(referenceSize.x*(width/800f),
            referenceSize.y*(height/450f));
    }

    private static void LabelLayerSize(RectTransform rect,string baseName)
    {
        if(rect==null)return;
        var size=rect.sizeDelta;
        string name=baseName+" ["+Mathf.RoundToInt(size.x)+"x"+
            Mathf.RoundToInt(size.y)+" UI]";
        if(rect.gameObject.name!=name)rect.gameObject.name=name;
    }

    private void Build(TextMeshProUGUI font)
    {
        var art=theme;
        Image ignored;
        // Explicit draw order: video, sky, hills, clouds, splash, main menu.
        videoBackground=OpeningBackgroundVideo.Install(backdrop);
        sky=MakeLayer("01 SKY - full background",new Color(.23f,.48f,.59f,1f),
            art!=null?art.sky:null,font,"BẦU TRỜI / SKY",out baseImage);
        skyPlaceholderLabel=sky.GetComponentInChildren<TextMeshProUGUI>(true);
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
        splashLeft=OpeningHeroSplashVideo.Install(backdrop,font,true);
        splashRight=OpeningHeroSplashVideo.Install(backdrop,font,false);
        // No duplicate game title here: logo is part of StartMenuUI instead.
    }

    // Sibling of the opening and menu (not a cloud child): the curtain
    // covers the ENTIRE window including any old HUD behind the opening.
    // No raycast interception; menu interactions are gated independently.
    private void BuildBlackCurtain()
    {
        var go=new GameObject("Opening Black Curtain - Full Window",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rect=(RectTransform)go.transform;
        rect.SetParent(canvasArea.parent,false);
        rect.anchorMin=Vector2.zero;
        rect.anchorMax=Vector2.one;
        rect.offsetMin=Vector2.zero;
        rect.offsetMax=Vector2.zero;
        blackCurtain=go.GetComponent<Image>();
        blackCurtain.color=Color.black;
        blackCurtain.raycastTarget=false;
        rect.SetAsLastSibling();
    }

    public void Layout(bool force=false)
    {
        if(backdrop==null||(layoutEditing&&!force))return;
        float w=Mathf.Max(320f,ManagementWindowLayout.CombatPixels(Screen.width));
        float h=Mathf.Max(240f,ManagementWindowLayout.CombatHeight(Screen.height));
        backdrop.anchorMin=backdrop.anchorMax=backdrop.pivot=Vector2.zero;
        backdrop.anchoredPosition=Vector2.zero;
        backdrop.sizeDelta=new Vector2(w,h);
        if(videoBackground!=null)videoBackground.Resize(w,h);
        Place(sky,Vector2.zero,new Vector2(w*1.1f,h*1.12f));
        Place(far,new Vector2(0f,-h*.13f),
            ResolveLayerSize(theme!=null?theme.farMountainsReferenceSize:Vector2.zero,
                w,h,new Vector2(w*1.18f,h*.62f)));
        Place(near,new Vector2(0f,-h*.33f),
            ResolveLayerSize(theme!=null?theme.nearMountainsReferenceSize:Vector2.zero,
                w,h,new Vector2(w*1.24f,h*.34f)));
        Place(backCloud,new Vector2(0f,h*.16f),
            ResolveLayerSize(theme!=null?theme.backCloudsReferenceSize:Vector2.zero,
                w,h,new Vector2(w*1.2f,h*.44f)));
        Place(cloudLeft,new Vector2(-w*.3f,-h*.04f),
            ResolveLayerSize(theme!=null?theme.leftCloudsReferenceSize:Vector2.zero,
                w,h,new Vector2(w*.67f,h*.91f)));
        Place(cloudRight,new Vector2(w*.3f,h*.04f),
            ResolveLayerSize(theme!=null?theme.rightCloudsReferenceSize:Vector2.zero,
                w,h,new Vector2(w*.67f,h*.91f)));
        LabelLayerSize(sky,"01 SKY - full background");
        LabelLayerSize(far,"02 FAR MOUNTAINS");
        LabelLayerSize(near,"03 NEAR MOUNTAINS");
        LabelLayerSize(backCloud,"04 BACK CLOUDS");
        LabelLayerSize(cloudLeft,"05 FRONT CLOUDS LEFT");
        LabelLayerSize(cloudRight,"06 FRONT CLOUDS RIGHT");
        if(splashLeft!=null)splashLeft.Layout(w,h);
        if(splashRight!=null)splashRight.Layout(w,h);
        var rects=GetLayoutRects();
        // Only scenery and splash are owned here; StartMenuUI handles
        // its logo/buttons after its own normal layout.
        for(int i=0;i<8;i++)
            ApplySavedRect(theme,LayoutKeys[i],rects[i],w,h);
        backCloudBase=backCloud.anchoredPosition;
        cloudLeftBase=cloudLeft.anchoredPosition;
        cloudRightBase=cloudRight.anchoredPosition;
        if(splashLeft!=null)splashLeft.RefreshMediaFit();
        if(splashRight!=null)splashRight.RefreshMediaFit();
        foreach(var layer in backdrop.GetComponentsInChildren<OpeningEdgeFeather>(true))
            layer.Refresh();
    }

    private void Update()
    {
        if(finished||backdrop==null)return;
        EnsureOpeningSiblingsParent();
        if(!orderFixed)
        {
            backdrop.SetAsLastSibling();
            if(canvasArea!=null)canvasArea.SetAsLastSibling();
            if(blackCurtain!=null)blackCurtain.transform.SetAsLastSibling();
            if(game!=null&&game.confirmationPopup!=null&&game.confirmationPopup.activeInHierarchy)
                game.confirmationPopup.transform.SetAsLastSibling();
            orderFixed=true;
        }
        float dt=Mathf.Max(0f,Time.unscaledDeltaTime);
        if(layoutEditing)return; // Freeze preview, preserve hand-edited rects.
        if(entering) { AnimateEntry(dt);return; }
        if(game==null||game.preGameUI==null||!game.preGameUI.activeInHierarchy)return;
        elapsed+=dt;
        if(blackCurtain!=null)
        {
            if(elapsed < BlackFadeSeconds)
            {
                // Keep the black curtain above any HUD that the scene may
                // have built on a later Start() than this opening layer.
                blackCurtain.transform.SetAsLastSibling();
                var color=Color.black;
                color.a=BlackCurtainAlphaAt(elapsed);
                blackCurtain.color=color;
            }
            else if(blackCurtain.gameObject.activeSelf)
            {
                blackCurtain.color=new Color(0f,0f,0f,0f);
                blackCurtain.gameObject.SetActive(false);
            }
        }
        bool showingBackgroundVideo=videoBackground!=null&&videoBackground.IsDisplaying;
        if(baseImage!=null)
        {
            var c=baseImage.color;
            c.a=showingBackgroundVideo?.18f:1f;
            baseImage.color=c;
        }
        if(skyPlaceholderLabel!=null && skyPlaceholderLabel.gameObject.activeSelf==showingBackgroundVideo)
            skyPlaceholderLabel.gameObject.SetActive(!showingBackgroundVideo);
        float menuAlpha=MenuAlphaAt(elapsed);
        if(menuFade!=null)menuFade.alpha=menuAlpha;
        if(menu!=null)menu.SetOpeningMenuReady(menuAlpha>=.999f);
        float w=backdrop.rect.width,h=backdrop.rect.height;
        // Drift AROUND saved centers instead of hard-coded positions.
        backCloud.anchoredPosition=backCloudBase+
            new Vector2(Mathf.Sin(elapsed*.09f)*w*.035f,0f);
        cloudLeft.anchoredPosition=cloudLeftBase+
            new Vector2(Mathf.Sin(elapsed*.12f)*w*.035f,
                Mathf.Sin(elapsed*.19f)*h*.014f);
        cloudRight.anchoredPosition=cloudRightBase+
            new Vector2(Mathf.Sin(elapsed*.11f+1.9f)*w*.032f,
                Mathf.Sin(elapsed*.16f)*h*.017f);
        far.localScale=new Vector3(1f+Mathf.Sin(elapsed*.13f)*.008f,
            1f+Mathf.Sin(elapsed*.13f)*.008f,1f);
        // Independent left/right media selections, with only one initial fade each.
        if(splashLeft!=null)splashLeft.Tick(elapsed,null);
        if(splashRight!=null)splashRight.Tick(elapsed,splashLeft!=null?splashLeft.CurrentPath:null);
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
        // Both oversized splash frames remain absolutely stationary;
        // its visibility follows the overall opening fade only.
        cloudLeft.anchoredPosition=cloudLeftBase+
            new Vector2(-w*1.2f*eased,0f);
        cloudRight.anchoredPosition=cloudRightBase+
            new Vector2(w*1.2f*eased,0f);
        backCloud.anchoredPosition=backCloudBase+
            new Vector2(0f,-h*.6f*eased);
        float scale=1f+.7f*eased;
        far.localScale=near.localScale=new Vector3(scale,scale,1f);
        sky.localScale=new Vector3(1f+.15f*eased,1f+.15f*eased,1f);
        if(t<1f)return;
        CompleteGameplayTransition();
    }

    // Shared by animated entry and direct Load; hide only opening-owned layers.
    public void CompleteGameplayTransition()
    {
        finished=true;
        entering=false;
        layoutEditing=false;
        if(splashLeft!=null)splashLeft.StopVideo();
        if(splashRight!=null)splashRight.StopVideo();
        if(videoBackground!=null)videoBackground.StopPlayback();
        if(blackCurtain!=null)blackCurtain.gameObject.SetActive(false);
        if(game!=null&&game.preGameUI!=null)game.preGameUI.SetActive(false);
        if(UIManager.Instance!=null)UIManager.Instance.SetPaused(false);
        if(backdrop!=null)backdrop.gameObject.SetActive(false);
    }

    public void ResetForMenu()
    {
        if(backdrop==null)return;
        layoutEditing=false;
        finished=entering=orderFixed=false;
        elapsed=enterElapsed=0f;
        if(backdropFade!=null)backdropFade.alpha=1f;
        if(blackCurtain!=null)
        {
            blackCurtain.gameObject.SetActive(true);
            blackCurtain.color=Color.black;
        }
        if(menu!=null)menu.SetOpeningMenuReady(false);
        if(splashLeft!=null)splashLeft.ResetToTitle();
        if(splashRight!=null)splashRight.ResetToTitle();
        // Reactivate the parent before calling VideoPlayer.Play().
        // VideoPlayer cannot reliably resume on an inactive GameObject.
        backdrop.gameObject.SetActive(true);
        if(videoBackground!=null)videoBackground.StartPlayback();
        backdrop.SetAsLastSibling();
        if(canvasArea!=null)canvasArea.SetAsLastSibling();
        if(blackCurtain!=null)blackCurtain.transform.SetAsLastSibling();
        if(menuFade!=null)menuFade.alpha=0f;
        sky.localScale=far.localScale=near.localScale=Vector3.one;
        // Restore BOTH opening layers and title/buttons. Without this,
        // unsaved manual edits to the buttons remain until Play Mode ends.
        if(menu!=null)menu.ForceLayoutForEditor();
        else Layout();
    }
}
