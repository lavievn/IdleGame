using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

partial class MotionRegression
{
    static void CheckOpeningButtonText(Button button)
    {
        var box=(RectTransform)button.transform;
        foreach(var text in button.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            var rect=(RectTransform)text.transform;
            Near(rect.anchorMin.x,0,"label stretches left");Near(rect.anchorMin.y,0,"label stretches bottom");
            Near(rect.anchorMax.x,1,"label stretches right");Near(rect.anchorMax.y,1,"label stretches top");
            Near(rect.rect.width,box.rect.width-16,"horizontal padding");
            Near(rect.rect.height,box.rect.height-8,"vertical padding");
            SamePoint(rect.localScale,Vector3.one,"label scale reset");
            Check(text.enableAutoSizing && text.fontSizeMin>=14 && text.fontSizeMax>=text.fontSizeMin,"bounded readable font");
            Check(!text.enableWordWrapping && !text.raycastTarget,"single-line caption, button owns hits");
            Near(text.margin.x,0,"no inherited TMP left margin");Near(text.margin.z,0,"no inherited right margin");
        }
    }
    static void OpeningFontTests()
    {
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        var previous=Resources.Load<OpeningArtTheme>("OpeningArtTheme");
        try
        {
            foreach(int width in new[]{600,800,1150})
            Run("authored tiny Home group grows without changing theme/logo/art at "+width,()=>{
                var theme=new OpeningArtTheme();
                var keys=new[]{"continueButton","newGameButton","settingsButton","quitButton"};
                theme.playModeLayouts=new OpeningLayoutEntry[5];
                for(int i=0;i<4;i++)theme.playModeLayouts[i]=new OpeningLayoutEntry{
                    key=keys[i],position=new Vector2(0,12.5f-i*14.375f),size=new Vector2(65,11.875f)};
                theme.playModeLayouts[4]=new OpeningLayoutEntry{key="gameTitle",position=new Vector2(0,113.4375f),size=new Vector2(350,71.875f)};
                Resources.Register("OpeningArtTheme",theme);
                using(var f=new DesktopHotfixFixture(width,true)) {
                    Check(f.manager.ReturnToMainMenu(),"show Home");
                    var rects=f.menu.GetOpeningLayoutRects();
                    var cinema=Get<OpeningCinematicUI>(f.menu,"cinematic");var art=cinema.GetLayoutRects();
                    var artPositions=new Vector2[8];var artSizes=new Vector2[8];
                    for(int i=0;i<8;i++) { artPositions[i]=art[i].anchoredPosition;artSizes[i]=art[i].sizeDelta; }
                    for(int pass=0;pass<3;pass++) {
                        Call(f.ui,"LayoutReadableUI");
                        for(int i=1;i<5;i++) {
                            var r=rects[i];Check(r.rect.width>=152 && r.rect.height>=34,"grown readable click target");
                            CheckOpeningButtonText(r.GetComponent<Button>());
                            if(i>1)Check(rects[i-1].anchoredPosition.y-rects[i-1].rect.height*.5f-
                                (r.anchoredPosition.y+r.rect.height*.5f)>=5.99f,"no overlap, six-pixel gap");
                            Check(r.anchoredPosition.y-r.rect.height*.5f>=-Screen.height*.5f+7.99f,"stays in window");
                        }
                    }
                    Near(rects[0].anchoredPosition.y,113.4375f*Screen.height/450f,"logo position untouched");
                    Near(rects[0].rect.height,71.875f*Screen.height/450f,"logo size untouched");
                    Check(rects[1].anchoredPosition.y+rects[1].rect.height*.5f<=
                        rects[0].anchoredPosition.y-rects[0].rect.height*.5f-5.99f,"does not overlap fixed logo");
                    for(int i=0;i<8;i++) {
                        Near(art[i].anchoredPosition.x,artPositions[i].x,"art X unchanged");Near(art[i].anchoredPosition.y,artPositions[i].y,"art Y unchanged");
                        Near(art[i].sizeDelta.x,artSizes[i].x,"art width unchanged");Near(art[i].sizeDelta.y,artSizes[i].y,"art height unchanged");
                    }
                    Near(theme.playModeLayouts[0].size.x,65,"theme never written");
                    Near(theme.playModeLayouts[0].position.y,12.5f,"authored values retained");
                }
            });
            foreach(int width in new[]{600,800,1150})foreach(bool openingFirst in new[]{false,true})
            Run("opening owns responsive button text/settings/popup across HUD relayout "+width+"/"+openingFirst,()=>{
                var theme=new OpeningArtTheme();
                var keys=new[]{"continueButton","newGameButton","settingsButton","quitButton"};
                theme.playModeLayouts=new OpeningLayoutEntry[5];
                for(int i=0;i<4;i++)theme.playModeLayouts[i]=new OpeningLayoutEntry{
                    key=keys[i],position=new Vector2(12,65-i*60),size=new Vector2(210,46)};
                theme.playModeLayouts[4]=new OpeningLayoutEntry{key="gameTitle",position=new Vector2(8,150),size=new Vector2(350,65)};
                Resources.Register("OpeningArtTheme",theme);
                using(var f=new DesktopHotfixFixture(width,openingFirst)) {
                    Check(f.manager.ReturnToMainMenu(),"return title uses installed opening");
                    var rects=f.menu.GetOpeningLayoutRects();
                    for(int i=1;i<5;i++) {
                        Near(rects[i].anchoredPosition.x,12f*width/800f,"saved center X");
                        Near(rects[i].anchoredPosition.y,(65-(i-1)*60)*Screen.height/450f,"saved center Y");
                        CheckOpeningButtonText(rects[i].GetComponent<Button>());
                    }
                    Near(rects[0].anchoredPosition.x,8f*width/800f,"logo authored position remains");
                    foreach(var button in Get<GameObject>(f.menu,"settingsPage").GetComponentsInChildren<Button>(true))CheckOpeningButtonText(button);
                    Call(f.menu,"OpenSettings");Call(f.ui,"LayoutReadableUI");
                    foreach(var button in Get<GameObject>(f.menu,"settingsPage").GetComponentsInChildren<Button>(true))CheckOpeningButtonText(button);
                    Call(f.menu,"CloseSettings");
                    f.manager.OnNewGameClicked();
                    var message=Get<TextMeshProUGUI>(f.menu,"message");
                    Check(f.menu.OwnsText(message),"sibling popup belongs to opening typography");
                    var p=(RectTransform)message.transform;float size=message.fontSizeMax;var position=p.anchoredPosition;
                    for(int n=0;n<3;n++)Call(f.ui,"LayoutReadableUI");
                    Near(message.fontSizeMax,size,"HUD never overwrites popup font");
                    Near(p.anchoredPosition.y,position.y,"HUD never moves popup message independently");
                    Check(message.enableAutoSizing && message.enableWordWrapping && message.fontSizeMin>=16,"message wrapping/font bounds retained");
                    f.menu.SetConfirmationLabels("Đồng ý nâng cấp","Hủy / quay lại");
                    Near(message.fontSizeMax,size,"changing consent button labels preserves message typography immediately");
                    Check(message.enableAutoSizing&&message.enableWordWrapping,"consent labels preserve message wrapping and autosizing");
                    Call(f.ui,"LayoutReadableUI");
                    var choices=f.manager.confirmationPopup.GetComponentsInChildren<Button>(true);
                    Check(Array.Exists(choices,b=>b.GetComponentInChildren<TextMeshProUGUI>(true).text=="Đồng ý nâng cấp") &&
                          Array.Exists(choices,b=>b.GetComponentInChildren<TextMeshProUGUI>(true).text=="Hủy / quay lại"),
                          "consent captions remain after opening layout");
                    foreach(var button in f.manager.confirmationPopup.GetComponentsInChildren<Button>(true)) {
                        CheckOpeningButtonText(button);
                        var b=(RectTransform)button.transform;
                        Check(b.anchoredPosition.y+b.rect.height*.5f<p.anchoredPosition.y-p.rect.height*.5f,"message and action rows separated");
                    }
                    f.manager.OnCancelResetClicked();
                    Check(!f.manager.confirmationPopup.activeSelf,"cancel preserves existing callback");
                }
            });
        }
        finally
        {
            if(previous!=null)Resources.Register("OpeningArtTheme",previous);else Resources.Unregister("OpeningArtTheme");
        }
    }
}
