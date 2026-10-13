using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

partial class MotionRegression
{
    static CustomInteractable ManualMenuButton(GameObject menu,string name)
    {
        foreach (var c in menu.GetComponentsInChildren<CustomInteractable>(true))
            if (c.gameObject.name==name) return c;
        throw new Exception("Missing menu action "+name);
    }

    static void ManualSaveMenuTests()
    {
        Run("0.0.51 menu saves slot 1, confirms overwrite and protects cancellation",()=>{
            var f=new Field();
            SaveManager saves; string dir;
            var gm=MenuManager(f,out saves,out dir);
            try
            {
                var hero=HeroAt(8);
                Set(gm,"runtimeHeroData",hero);
                Set(gm,"hasDeployed",true);
                var ui=new GameObject().AddComponent<UIManager>();
                UIManager.Instance=ui;
                var canvas=new GameObject(true);
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(900,500);
                ui.systemMenu=new GameObject(true);
                ui.systemMenu.transform.parent=canvas.transform;
                var caption=new GameObject(true);
                caption.transform.parent=ui.systemMenu.transform;
                caption.AddComponent<TextMeshProUGUI>();
                ui.systemMenu.SetActive(false);
                Call(ui,"Start");

                ui.ToggleSystemMenu();
                ManualMenuButton(ui.systemMenu,"Save").onClickEvent.Invoke();
                ManualMenuButton(ui.systemMenu,"Save1").onClickEvent.Invoke();
                Check(saves.HasSave(SaveSlot.ManualSave1),"first slot created");
                Check(!ui.systemMenu.activeSelf,"successful save closes menu");

                var loaded=HeroAt(1);
                Check(saves.LoadGame(loaded,SaveSlot.ManualSave1)&&loaded.currentLevel==8,
                    "first save contains current character");

                hero.currentLevel=9;
                ui.ToggleSystemMenu();
                ManualMenuButton(ui.systemMenu,"Save").onClickEvent.Invoke();
                ManualMenuButton(ui.systemMenu,"Save1").onClickEvent.Invoke();
                var popup=ManualMenuButton(ui.systemMenu,"OverwriteYes");
                Check(popup.gameObject.activeInHierarchy,"existing save requires confirmation");
                Check(saves.LoadGame(loaded,SaveSlot.ManualSave1)&&loaded.currentLevel==8,
                    "selecting occupied slot does not overwrite");
                ManualMenuButton(ui.systemMenu,"OverwriteNo").onClickEvent.Invoke();
                Check(saves.LoadGame(loaded,SaveSlot.ManualSave1)&&loaded.currentLevel==8,
                    "cancelling preserves file");

                ManualMenuButton(ui.systemMenu,"Save1").onClickEvent.Invoke();
                ManualMenuButton(ui.systemMenu,"OverwriteYes").onClickEvent.Invoke();
                Check(!ui.systemMenu.activeSelf,"confirmed save closes menu");
                Check(saves.LoadGame(loaded,SaveSlot.ManualSave1)&&loaded.currentLevel==9,
                    "confirmed overwrite replaces correct slot");
                Check(!saves.HasSave(SaveSlot.ManualSave2),"other manual slot remains untouched");
                UIManager.Instance=null;
            }
            finally
            {
                UIManager.Instance=null;
                Directory.Delete(dir,true);
            }
        });
    }
}
