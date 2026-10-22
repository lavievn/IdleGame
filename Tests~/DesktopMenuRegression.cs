using UnityEngine;
using UnityEngine.Events;
using TMPro;

partial class MotionRegression
{
    static void DesktopMenuTests()
    {
        Run("right menu trigger and scale panel get positive on-screen layout and desktop hitboxes",()=>{
            new Field();var ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;
            var window=new GameObject().AddComponent<TransparentWindow>();
            var canvas=new GameObject(true);((RectTransform)canvas.transform).sizeDelta=new Vector2(1920,1080);
            var trigger=new GameObject(true);trigger.name="MenuArea";trigger.transform.parent=canvas.transform;
            var button=trigger.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(ui.ToggleSystemMenu);
            ui.systemMenu=new GameObject(true);ui.systemMenu.name="SystemMenu";ui.systemMenu.transform.parent=canvas.transform;
            var oldControls=new System.Collections.Generic.List<CustomInteractable>();
            foreach(var name in new[]{"Btn_Scale200","Btn_Scale500","Btn_Scale1000"}) {
                var go=new GameObject(true);go.name=name;go.transform.parent=ui.systemMenu.transform;go.AddComponent<TextMeshProUGUI>();
                var c=go.AddComponent<CustomInteractable>();c.onClickEvent=new UnityEvent();ui.RegisterInteractable(c);
                oldControls.Add(c);
            }
            var save=new GameObject(true);save.name="SaveUi";save.transform.parent=ui.systemMenu.transform;
            var saveLabel=new GameObject(true);saveLabel.transform.parent=save.transform;saveLabel.AddComponent<TextMeshProUGUI>();
            ((RectTransform)saveLabel.transform).anchoredPosition=new Vector2(-1038,411);
            ui.systemMenu.SetActive(false);Call(ui,"Start");
            var click=trigger.GetComponent<CustomInteractable>();
            Check(click!=null&&!button.enabled,"Button-only scene trigger converted to one dispatch path");
            var root=(RectTransform)ui.systemMenu.transform;Check(root.rect.width>0&&root.rect.height>0,"positive menu geometry");
            Check(System.Array.IndexOf(window.clickableUI,click.GetRect())>=0,"red trigger registered for Windows");
            ui.HandleMouseClick(click.GetRect().position);Check(ui.systemMenu.activeSelf,"red click opens menu");
            var visible=ui.systemMenu.GetComponentsInChildren<CustomInteractable>();
            var actions=new System.Collections.Generic.Dictionary<string,CustomInteractable>();
            foreach(var item in visible) {
                Check(!actions.ContainsKey(item.gameObject.name),"no duplicate main action "+item.gameObject.name);
                actions.Add(item.gameObject.name,item);
            }
            var expectedActions=new System.Collections.Generic.HashSet<string>(new[]{
                "Exit","Save","Load","Reset","WindowSize","ReturnTitle","Close"});
            Check(expectedActions.SetEquals(actions.Keys),"six original functions plus the named Close action");
            foreach(var old in oldControls)
                Check(!old.gameObject.activeInHierarchy && System.Array.IndexOf(visible,old)<0,
                    "legacy control hidden: "+old.gameObject.name);
            Check(!save.activeInHierarchy,"legacy SaveUi hidden");
            foreach(var name in expectedActions) {
                var callback=Get<UnityAction>(actions[name].onClickEvent,"callback");
                Check(callback!=null && callback.GetInvocationList().Length==1,"one bound action for "+name);
                string method=name=="Exit"?"ConfirmExit":name=="Save"?"OpenManualSaveMenu":
                    name=="Load"?"OpenLoadMenu":name=="ReturnTitle"?"ReturnToTitle":
                    name=="Close"?"CloseSystemMenu":null;
                if(method!=null)Check(callback.Target==ui && callback.Method.Name==method,
                    name+" binds to "+method);
            }
            actions["Save"].onClickEvent.Invoke();
            Check(Get<GameObject>(ui,"savePage").activeInHierarchy &&
                !Get<GameObject>(ui,"mainPage").activeSelf,"Save opens manual save page");
            ui.CloseSystemMenu();ui.ToggleSystemMenu();
            actions["Load"].onClickEvent.Invoke();
            Check(Get<GameObject>(ui,"loadPage").activeInHierarchy &&
                !Get<GameObject>(ui,"mainPage").activeSelf,"Load opens load page");
            ui.CloseSystemMenu();ui.ToggleSystemMenu();
            var gm=new GameObject().AddComponent<GameManager>();
            gm.confirmationPopup=new GameObject(true);gm.confirmationPopup.SetActive(false);
            gm.preGameUI=new GameObject(true);gm.preGameUI.SetActive(false);
            actions["Reset"].onClickEvent.Invoke();
            Check(gm.HasPendingConfirmation && gm.confirmationPopup.activeSelf &&
                !ui.systemMenu.activeSelf,"Reset asks GameManager for confirmation and closes menu");
            gm.OnCancelResetClicked();ui.ToggleSystemMenu();
            foreach(var item in visible)if(item.gameObject.name=="WindowSize")ui.HandleMouseClick(item.GetRect().position);
            visible=ui.systemMenu.GetComponentsInChildren<CustomInteractable>();Check(visible.Length==4,"size page only three sizes and back");
            var sizeActions=new System.Collections.Generic.HashSet<string>();
            foreach(var item in visible)sizeActions.Add(item.gameObject.name);
            Check(sizeActions.SetEquals(new[]{"Size600","Size800","Size1150","SizeBack"}),
                "current sizes and back exist without legacy size buttons");
            foreach(var item in visible)if(item.gameObject.name=="Size600")ui.HandleMouseClick(item.GetRect().position);
            Check(!ui.systemMenu.activeSelf,"scale selection closes panel");
            Check(Screen.width==600 && Screen.height==338,"small preset applies 600 by 338 window");
            ui.ToggleSystemMenu();visible=ui.systemMenu.GetComponentsInChildren<CustomInteractable>();
            bool main=false;foreach(var item in visible)if(item.gameObject.name=="Load")main=true;
            Check(main,"reopening starts at main page");
            actions["ReturnTitle"].onClickEvent.Invoke();
            Check(gm.preGameUI.activeSelf && !ui.systemMenu.activeSelf &&
                !gm.HasPendingConfirmation,"ReturnTitle returns to title and closes menu");
            UIManager.Instance=null;
        });
        Run("scale child gets click before menu container irrespective of registration order",()=>{
            new Field();var ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;
            var parent=new GameObject(true);((RectTransform)parent.transform).sizeDelta=new Vector2(640,150);
            var root=parent.AddComponent<CustomInteractable>();root.onClickEvent=new UnityEvent();int rootClicks=0,childClicks=0;
            root.onClickEvent.AddListener(()=>rootClicks++);ui.RegisterInteractable(root);
            var child=new GameObject(true);child.transform.parent=parent.transform;var c=child.AddComponent<CustomInteractable>();c.onClickEvent=new UnityEvent();c.onClickEvent.AddListener(()=>childClicks++);ui.RegisterInteractable(c);
            ui.HandleMouseClick(child.transform.position);Check(childClicks==1&&rootClicks==0,"container cannot swallow child");UIManager.Instance=null;
        });
        Run("native clicks fire once per press and dragging preserves grab offset",()=>{
            bool previous=false;
            Check(TransparentWindow.ConsumePress(true,ref previous),"first press");
            Check(!TransparentWindow.ConsumePress(true,ref previous),"held button does not toggle again");
            Check(!TransparentWindow.ConsumePress(false,ref previous),"release does not click");
            Check(TransparentWindow.ConsumePress(true,ref previous),"next press works");
            var p=TransparentWindow.DragPosition(new Vector2(300,600),new Vector2(450,650),new Vector2(150,200));
            Near(p.x,0,"preserved horizontal offset");Near(p.y,150,"can drag away from bottom");
            p=TransparentWindow.DragPosition(new Vector2(300,600),new Vector2(450,650),new Vector2(-50,-100));
            Near(p.x,-200,"negative monitor coordinate");Near(p.y,-150,"negative vertical coordinate");
        });
        Run("native client coordinates match Unity game pixels after DPI or resolution scaling",()=>{
            var p=TransparentWindow.ClientToGamePosition(150,75,300,150,200,100);Near(p.x,100,"scaled x");Near(p.y,50,"inverted scaled y");
            p=TransparentWindow.ClientToGamePosition(0,0,500,282,500,282);Near(p.x,0,"left");Near(p.y,282,"top");
            p=TransparentWindow.ClientToGamePosition(500,282,500,282,500,282);Near(p.x,500,"right");Near(p.y,0,"bottom");
        });
    }
}
