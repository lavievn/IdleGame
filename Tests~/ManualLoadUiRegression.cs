using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

// Real UI/state methods with Unity doubles, NOT a rendering or Play Mode test.
partial class MotionRegression
{
    sealed class ManualLoadUiFixture : IDisposable
    {
        internal readonly Field field;
        internal readonly GameManager manager;
        internal readonly SaveManager saves;
        internal readonly StartMenuUI menu;
        internal readonly OpeningCinematicUI opening;
        internal readonly UIManager ui;
        internal readonly GameObject unrelated;
        readonly string directory, previousStreaming;
        readonly float previousScale, previousDelta;

        internal ManualLoadUiFixture()
        {
            previousScale=Time.timeScale;
            previousDelta=Time.unscaledDeltaTime;
            previousStreaming=Application.streamingAssetsPath;
            field=new Field();
            manager=MenuManager(field,out saves,out directory);
            try
            {
                // MenuManager uses a fresh GUID under Path.GetTempPath(), never
                // Awake/default saves. Media discovery is isolated there too.
                Check(directory!=Path.Combine(Application.persistentDataPath,"Saves"),
                    "fixture does not use default saves");
                Application.streamingAssetsPath=directory;
                var canvas=new GameObject("Load UI test canvas",typeof(RectTransform));
                ((RectTransform)canvas.transform).sizeDelta=new Vector2(1000,563);
                manager.preGameUI.transform.SetParent(canvas.transform,false);
                var template=new GameObject("Template",typeof(RectTransform),typeof(Image),typeof(Button));
                template.transform.SetParent(manager.preGameUI.transform,false);
                var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
                label.transform.SetParent(template.transform,false);
                menu=StartMenuUI.Install(manager);
                Check(menu!=null && menu.IsCinematic,"install real opening flow");
                Set(manager,"startMenu",menu);
                opening=Get<OpeningCinematicUI>(menu,"cinematic");
                ui=new GameObject("Load UI manager").AddComponent<UIManager>();
                UIManager.Instance=ui;
                ui.systemMenu=new GameObject("SystemMenu",typeof(RectTransform));
                ui.systemMenu.transform.SetParent(canvas.transform,false);
                Call(ui,"ConfigureSystemMenu");
                ui.systemMenu.SetActive(false);
                unrelated=new GameObject("Unrelated visible UI",typeof(RectTransform));
                unrelated.transform.SetParent(canvas.transform,false);

                var first=HeroAt(2);
                var second=HeroAt(6);
                Check(saves.SaveGame(first,SaveSlot.ManualSave1),"create slot 1");
                Check(saves.SaveGame(second,SaveSlot.ManualSave2),"create slot 2");
                Set(manager,"runtimeHeroData",second);
                Set(manager,"hasDeployed",true);
                field.combat.SetupHeroInfo(second);
                Check(menu.BeginGameplayTransition(),"initial entry starts");
                Tick(OpeningCinematicUI.EntrySeconds);
                Check(!manager.preGameUI.activeInHierarchy &&
                      !Curtain.gameObject.activeInHierarchy,"initial cinematic completed");
            }
            catch { Dispose(); throw; }
        }

        internal Image Curtain => Get<Image>(opening,"blackCurtain");
        internal RectTransform Backdrop => Get<RectTransform>(opening,"backdrop");
        internal void Tick(float seconds)
        {
            Time.unscaledDeltaTime=seconds;
            Call(opening,"Update");
            if(manager.IsEnteringGameplay)
            {
                // Opening completion now hands off after the paused camera's
                // LateUpdate. Keep the original UI assertions unchanged.
                Call(field.space,"LateUpdate");Call(manager,"LateUpdate");
            }
        }
        internal void ShowOpening()
        {
            manager.preGameUI.SetActive(true);
            menu.RestoreMenu();
        }
        internal void Click(string name)
        {
            foreach(var button in ui.systemMenu.GetComponentsInChildren<CustomInteractable>(true))
                if(button.gameObject.name==name)
                { button.onClickEvent.Invoke(); return; }
            throw new Exception("Missing real menu callback: "+name);
        }
        internal void CheckPlaying(int level)
        {
            Check(Get<bool>(manager,"hasDeployed") && field.hero.IsDeployed,
                "loaded Hero is deployed");
            Check(manager.HeroData.currentLevel==level &&
                  field.combat.CurrentHeroCombatState.Data==manager.HeroData,
                "combat uses the selected snapshot");
            Check(manager.IsGameplayHUDVisible && !manager.preGameUI.activeInHierarchy,
                "gameplay is visible, title is hidden");
            Check(!Curtain.gameObject.activeInHierarchy && !Backdrop.gameObject.activeInHierarchy,
                "no opening curtain or backdrop covers gameplay");
            Check(!opening.IsEntering && Get<bool>(opening,"finished"),
                "opening cannot resume after immediate entry");
            Check(!menu.BeginGameplayTransition(),"completed opening cannot start another entry without a menu reset");
            Check(Time.timeScale==1f,"load resumes gameplay");
            Check(unrelated.activeInHierarchy,"unrelated UI remains active");
        }
        public void Dispose()
        {
            UIManager.Instance=null;
            Time.timeScale=previousScale;
            Time.unscaledDeltaTime=previousDelta;
            Application.streamingAssetsPath=previousStreaming;
            // No recursive deletion: only the three known fixture slots.
            foreach(string slot in new[]{"AutoSave","ManualSave1","ManualSave2"})
                foreach(string extension in new[]{".json",".tmp"})
                {
                    string path=Path.Combine(directory,slot+extension);
                    if(File.Exists(path))File.Delete(path);
                }
            Directory.Delete(directory,false);
        }
    }

    static void ManualLoadUiRegressionTests()
    {
        Run("W1.9a actual load menu callbacks support repeated immediate loads",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                f.ui.ToggleSystemMenu();f.Click("Load");f.Click("LoadSave1");
                f.CheckPlaying(2);
                Check(!f.ui.systemMenu.activeSelf,"successful UI load closes system menu");
                f.ui.ToggleSystemMenu();f.Click("Load");f.Click("LoadSave2");
                f.CheckPlaying(6);
                f.ui.ToggleSystemMenu();f.Click("Load");f.Click("LoadSave1");
                f.Tick(.5f);f.CheckPlaying(2);
            }
        });
        Run("W1.9a paused combat load clears old shots and pending wave",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                f.field.hero.ChangeAttackMode(1);
                var monster=f.field.Monster(-200);
                f.field.Battle(monster);f.field.hero.TickMovement(f.field.space,0);
                CombatStep(f.field,1);
                Check(f.field.combat.PendingProjectileCount>0,"old battle has an in-flight shot");
                var routine=(System.Collections.IEnumerator)typeof(GameManager).GetMethod(
                    "WaitAndCallNextWave",System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(f.manager,null);
                var pending=f.manager.StartCoroutine(routine);
                Set(f.manager,"nextWaveCoroutine",pending);
                f.ui.SetPaused(true);
                Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"paused load succeeds");
                f.CheckPlaying(2);
                Check(pending.stopped && Get<Coroutine>(f.manager,"nextWaveCoroutine")==null,
                    "old pending wave is cancelled");
                Check(f.field.combat.PendingProjectileCount==0 && f.field.Enemies.Count==0,
                    "old combat shots and monsters are cleared");
                Check(f.field.combat.CurrentHeroCombatState.AttackTimer==0f,
                    "loaded attack timer is fresh");
            }
        });
        Run("W1.9a immediate load dismisses an active title opening",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                f.ShowOpening();Set(f.manager,"hasDeployed",false);f.field.hero.HideHero();
                Check(f.Curtain.gameObject.activeInHierarchy,"title curtain is active before load");
                Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"title-time API load succeeds");
                f.CheckPlaying(2);f.Tick(8f);f.CheckPlaying(2);
            }
        });
        Run("W1.9a immediate load cancels an in-progress cinematic entry",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                f.ShowOpening();Check(f.menu.BeginGameplayTransition(),"entry starts");
                f.Tick(.2f);f.ui.SetPaused(true);
                Check(f.opening.IsEntering,"entry is in progress");
                Check(f.manager.LoadAndContinue(SaveSlot.ManualSave2),"entry-time API load succeeds");
                f.CheckPlaying(6);f.Tick(8f);f.CheckPlaying(6);
            }
        });
        Run("W1.9a Continue retains the animated opening transition",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                f.ShowOpening();Set(f.manager,"hasDeployed",false);f.field.hero.HideHero();
                f.Tick(7f);Set(f.manager,"continueSlot",SaveSlot.ManualSave1);
                Set(f.manager,"manualSelection",true);
                f.manager.OnContinueClicked();
                Check(f.manager.HasPendingConfirmation,"Continue still requires confirmation");
                f.manager.OnConfirmResetClicked();
                Check(f.opening.IsEntering && f.manager.preGameUI.activeInHierarchy &&
                      f.Backdrop.gameObject.activeInHierarchy && Time.timeScale==0f,
                    "Continue animates with gameplay paused under the opening");
                f.Tick(OpeningCinematicUI.EntrySeconds);f.CheckPlaying(2);
            }
        });
        Run("W1.9a return to title replays opening and restores menu interaction",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                Check(f.manager.LoadAndContinue(SaveSlot.ManualSave1),"load before returning");
                Check(f.manager.ReturnToMainMenu(),"return to title succeeds");
                Check(!Get<bool>(f.manager,"hasDeployed") && !f.field.hero.IsDeployed &&
                      f.manager.preGameUI.activeInHierarchy,"title waits without a deployed Hero");
                Check(f.Curtain.gameObject.activeInHierarchy && f.Backdrop.gameObject.activeInHierarchy &&
                      !f.opening.IsEntering,"title opening restarts");
                Near(f.Curtain.color.a,1f,"title starts black by design");
                f.Tick(7f);
                Check(!f.Curtain.gameObject.activeInHierarchy && f.Backdrop.gameObject.activeInHierarchy &&
                      f.manager.preGameUI.GetComponent<CanvasGroup>().interactable,
                    "title reveal completes and menu becomes interactive");
            }
        });
        Run("W1.9a legacy ForceManualLoad preserves waiting-for-deploy behaviour",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                f.manager.ForceManualLoad((int)SaveSlot.ManualSave1);
                Check(!Get<bool>(f.manager,"hasDeployed") && !f.field.hero.IsDeployed &&
                      f.manager.HeroData.currentLevel==2 && f.manager.preGameUI.activeInHierarchy,
                    "legacy load restores data but does not deploy");
                f.Tick(7f);
                Check(!f.opening.IsEntering && !f.Curtain.gameObject.activeInHierarchy &&
                      f.Backdrop.gameObject.activeInHierarchy,"legacy load reveals the waiting menu");
            }
        });
        Run("W1.9a legacy waiting load cancels an earlier entry animation",()=>{
            using(var f=new ManualLoadUiFixture())
            {
                f.ShowOpening();Check(f.menu.BeginGameplayTransition(),"entry starts before legacy load");
                f.Tick(.2f);
                f.manager.ForceManualLoad((int)SaveSlot.ManualSave1);f.Tick(7f);
                Check(!Get<bool>(f.manager,"hasDeployed") && !f.field.hero.IsDeployed &&
                      f.manager.preGameUI.activeInHierarchy && !f.opening.IsEntering,
                    "legacy load stays at menu instead of completing an old entry");
            }
        });
    }
}
