using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

// Real UI/confirmation/save methods with UnityStub. No Win32 calls, renderer,
// EventSystem scheduling or player save access: those need Unity/Windows.
partial class MotionRegression
{
    sealed class DesktopHotfixFixture : IDisposable
    {
        internal readonly Field field;
        internal readonly GameManager manager;
        internal readonly UIManager ui;
        internal readonly StartMenuUI menu;
        internal readonly SaveManager saves;
        internal readonly TransparentWindow window;
        internal readonly string directory;
        readonly string previousStreaming;
        readonly int previousWidth=Screen.width,previousHeight=Screen.height,previousFrame=Time.frameCount;
        readonly float previousScale=Time.timeScale;
        readonly Mouse previousMouse=Mouse.current;

        internal DesktopHotfixFixture(int width,bool openingFirst)
        {
            Screen.width=width;Screen.height=Mathf.RoundToInt(width*9f/16);
            ManagementWindowLayout.SetCompact(Screen.width,Screen.height);
            previousStreaming=Application.streamingAssetsPath;
            field=new Field();manager=MenuManager(field,out saves,out directory);
            Check(Path.GetFileName(directory).StartsWith("idle-menu-"),"owned GUID fixture directory");
            Check(directory!=Path.Combine(Application.persistentDataPath,"Saves"),"never use player saves");
            Application.streamingAssetsPath=directory;
            try {
                var canvas=new GameObject("Desktop hotfix canvas",typeof(RectTransform));
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(width,Screen.height);
                manager.preGameUI.transform.SetParent(canvas.transform,false);
                var template=new GameObject("Template",typeof(RectTransform),typeof(Image),typeof(Button));
                template.transform.SetParent(manager.preGameUI.transform,false);
                var text=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(template.transform,false);
                ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;
                window=new GameObject().AddComponent<TransparentWindow>();ui.transparentWindow=window;
                ui.systemMenu=new GameObject("SystemMenu",typeof(RectTransform),typeof(Image),typeof(CustomInteractable));
                ui.systemMenu.transform.SetParent(canvas.transform,false);ui.systemMenu.SetActive(false);
                var trigger=new GameObject("MenuArea",typeof(RectTransform),typeof(Button));trigger.transform.SetParent(canvas.transform,false);
                if(openingFirst)menu=StartMenuUI.Install(manager);
                Call(ui,"Start");
                if(!openingFirst)menu=StartMenuUI.Install(manager);
                Set(manager,"startMenu",menu);
                Call(ui,"LateUpdate");
                var data=HeroAt(6);data.currentExp=123;
                Set(manager,"runtimeHeroData",data);Set(manager,"hasDeployed",true);
                field.combat.SetupHeroInfo(data);
                Check(saves.SaveGame(data,SaveSlot.ManualSave1),"seed manual 1 in temporary directory");
                data.currentExp=234;Check(saves.SaveGame(data,SaveSlot.ManualSave2),"seed manual 2");
                Check(saves.SaveGame(data,SaveSlot.AutoSave),"seed autosave");
                menu.EnterGameplayImmediately();Call(ui,"LayoutReadableUI");
                Check(!manager.preGameUI.activeInHierarchy && Get<bool>(menu,"entryLocked") &&
                    !Get<bool>(menu,"openingMenuReady"),"real hidden opening remains locked during gameplay");
            } catch { Dispose();throw; }
        }
        internal string Slot(string name) { return Path.Combine(directory,name+".json"); }
        internal CustomInteractable Control(Transform root,string name)
        {
            foreach(var c in root.GetComponentsInChildren<CustomInteractable>(true))
                if(c.gameObject.name==name)return c;
            throw new Exception("Missing control "+name);
        }
        internal void Click(CustomInteractable c,bool pointer)
        {
            Time.frameCount++;
            Vector2 point=ControlCenter(c.GetRect());
            Check(ui.HitControl(point)==c,"visible control wins hit test: "+c.gameObject.name);
            Mouse.current=new Mouse();Mouse.current.position.value=point;
            object[] args={false,false};
            typeof(TransparentWindow).GetMethod("CheckHitboxUI",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,args);
            Check((bool)args[0] && !(bool)args[1],"point is clickable, not a window-drag region");
            if(pointer)ui.HandlePointerClick(point,point);else ui.HandleMouseClick(point);
        }
        internal void OpenReset(bool pointer)
        {
            ui.ToggleSystemMenu();Click(Control(ui.systemMenu.transform,"Reset"),pointer);
            Call(ui,"LayoutReadableUI");
            Check(manager.HasPendingConfirmation && manager.confirmationPopup.activeInHierarchy &&
                !ui.systemMenu.activeSelf && !manager.preGameUI.activeInHierarchy,"gameplay reset shows sibling popup only");
            Check(!manager.confirmationPopup.transform.IsChildOf(manager.preGameUI.transform),"popup cannot inherit hidden/faded opening");
            Check(Array.IndexOf(window.modalUI,(RectTransform)manager.confirmationPopup.transform)>=0,"popup registered as native modal");
        }
        public void Dispose()
        {
            UIManager.Instance=null;Time.timeScale=previousScale;Time.frameCount=previousFrame;
            Mouse.current=previousMouse;Application.streamingAssetsPath=previousStreaming;
            Screen.width=previousWidth;Screen.height=previousHeight;
            ManagementWindowLayout.SetCompact(previousWidth,previousHeight);
            if(directory==null || !Directory.Exists(directory))return;
            foreach(string slot in new[]{"ManualSave1","ManualSave2","AutoSave"})
                foreach(string ext in new[]{".json",".tmp"}) {
                    string file=Path.Combine(directory,slot+ext);if(File.Exists(file))File.Delete(file);
                }
            Directory.Delete(directory,false); // Never recurse or remove unexpected files.
        }
    }

    static void DesktopMenuHotfixTests()
    {
        // Unity loads these Resources automatically. The headless double does
        // not; Reset must see the real catalog before its first cached lookup.
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        foreach(int width in new[]{600,800,1150})
        {
            Run("desktop hotfix "+width+" menu Close and every submenu Back fit and receive clicks",()=>{
                using(var f=new DesktopHotfixFixture(width,true)) {
                    var ui=f.ui;var root=(RectTransform)ui.systemMenu.transform;
                    for(int i=0;i<3;i++) {
                        ui.ToggleSystemMenu();
                        foreach(var c in ui.systemMenu.GetComponentsInChildren<CustomInteractable>())
                            Inside(c.GetRect(),root,"menu control inside panel");
                        var close=f.Control(ui.systemMenu.transform,"Close");
                        Check(close.GetComponentInChildren<TextMeshProUGUI>().text=="Đóng","visible Close label");
                        // A HUD sibling registered first must never steal the close area.
                        var blocker=new GameObject("Underlying HUD",typeof(RectTransform),typeof(CustomInteractable));
                        blocker.transform.SetParent(root.parent,false);blocker.transform.position=ControlCenter(close.GetRect());
                        int clicks=0;var click=blocker.GetComponent<CustomInteractable>();click.onClickEvent=new UnityEvent();click.onClickEvent.AddListener(()=>clicks++);
                        Get<System.Collections.Generic.List<CustomInteractable>>(ui,"interactables").Insert(0,click);
                        f.Click(close,true);Check(!ui.systemMenu.activeSelf && clicks==0,"Close does not activate HUD behind it");blocker.SetActive(false);
                    }
                    foreach(string page in new[]{"Save","Load","WindowSize","Overwrite"}) {
                        ui.ToggleSystemMenu();
                        f.Click(f.Control(root,page=="Overwrite"?"Save":page),false);
                        if(page=="Overwrite")f.Click(f.Control(root,"Save1"),false);
                        foreach(var c in ui.systemMenu.GetComponentsInChildren<CustomInteractable>())Inside(c.GetRect(),root,"submenu control inside panel");
                        string back=page=="Save"?"SaveBack":page=="Load"?"LoadBack":page=="WindowSize"?"SizeBack":"OverwriteBack";
                        f.Click(f.Control(root,back),true);
                        Check(Get<GameObject>(ui,"mainPage").activeInHierarchy,"Back returns to main");
                        f.Click(f.Control(root,"Close"),true);
                    }
                    ui.ToggleSystemMenu();f.Click(f.Control(root,"WindowSize"),true);
                    f.Click(f.Control(root,"Size"+width),true);
                    Check(!ui.systemMenu.activeSelf && Screen.width==width &&
                        Screen.height==Mathf.RoundToInt(width*9f/16),"each supported preset still applies and closes menu");
                }
            });
            foreach(bool pointer in new[]{false,true})
            {
                Run("desktop hotfix "+width+(pointer?" native-pointer":" editor-click")+" reset Cancel preserves RAM and all slots",()=>{
                    using(var f=new DesktopHotfixFixture(width,pointer)) {
                        var hero=f.manager.HeroData;string before=JsonUtility.ToJson(hero,false);
                        string one=File.ReadAllText(f.Slot("ManualSave1")),two=File.ReadAllText(f.Slot("ManualSave2")),auto=File.ReadAllText(f.Slot("AutoSave"));
                        f.OpenReset(pointer);
                        var cancel=f.Control(f.manager.confirmationPopup.transform,"Opening - Hủy");
                        Inside(cancel.GetRect(),(RectTransform)f.manager.confirmationPopup.transform,"Cancel fits popup");
                        f.Click(cancel,pointer);
                        Check(!f.manager.HasPendingConfirmation && !f.manager.confirmationPopup.activeSelf,"Cancel closes modal");
                        Check(f.manager.HeroData==hero && JsonUtility.ToJson(hero,false)==before && Get<bool>(f.manager,"hasDeployed"),"Cancel keeps current character playing");
                        Check(File.ReadAllText(f.Slot("ManualSave1"))==one && File.ReadAllText(f.Slot("ManualSave2"))==two && File.ReadAllText(f.Slot("AutoSave"))==auto,"Cancel never writes a slot");
                        f.ui.ToggleSystemMenu();f.Click(f.Control(f.ui.systemMenu.transform,"Close"),pointer);
                    }
                });
                Run("desktop hotfix "+width+(pointer?" native-pointer":" editor-click")+" reset Confirm replaces AutoSave only once",()=>{
                    using(var f=new DesktopHotfixFixture(width,pointer)) {
                        var old=f.manager.HeroData;string one=File.ReadAllText(f.Slot("ManualSave1")),two=File.ReadAllText(f.Slot("ManualSave2"));
                        f.OpenReset(pointer);
                        var confirm=f.Control(f.manager.confirmationPopup.transform,"Opening - Xác nhận");
                        Inside(confirm.GetRect(),(RectTransform)f.manager.confirmationPopup.transform,"Confirm fits popup");
                        f.Click(confirm,pointer);
                        var fresh=f.manager.HeroData;
                        Check(fresh!=old && fresh.currentLevel==1 && Get<bool>(f.manager,"hasDeployed"),"existing reset creates and deploys level 1 Hero");
                        Check(!f.manager.HasPendingConfirmation && !f.manager.confirmationPopup.activeSelf,"Confirm closes modal");
                        Check(File.ReadAllText(f.Slot("ManualSave1"))==one && File.ReadAllText(f.Slot("ManualSave2"))==two,"manual slots preserved byte for byte");
                        var loaded=HeroAt(1);Check(f.saves.LoadGame(loaded,SaveSlot.AutoSave) && loaded.currentLevel==1,"AutoSave contains new character");
                        confirm.GetComponent<Button>().onClick.Invoke();confirm.onClickEvent.Invoke();
                        Check(f.manager.HeroData==fresh,"EventSystem/native duplicate cannot reset again");
                    }
                });
            }
        }
    }
}
