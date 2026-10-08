using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static GameManager MenuManager(Field f, out SaveManager saves, out string dir)
    {
        UIManager.Instance=null;
        var gm=new GameObject().AddComponent<GameManager>();
        saves=new GameObject().AddComponent<SaveManager>();
        dir=Path.Combine(Path.GetTempPath(),"idle-menu-"+Guid.NewGuid());Directory.CreateDirectory(dir);Set(saves,"saveDirectory",dir);
        Set(gm,"saveManager",saves);Set(gm,"heroController",f.hero);Set(gm,"combatManager",f.combat);
        gm.heroDataSO=HeroAt(1);gm.preGameUI=new GameObject(true);gm.infoText=new GameObject(true).AddComponent<TextMeshProUGUI>();
        gm.confirmationPopup=new GameObject(true);gm.confirmationPopup.SetActive(false);
        Set(f.combat,"gameManager",gm);return gm;
    }
    static void MenuDifficultyTests()
    {
        Run("continue asks before loading, cancel preserves save, confirmation loads latest",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {
                var data=HeroAt(20);data.difficulty=1;Check(save.SaveGame(data,SaveSlot.ManualSave2),"save written");
                gm.OnContinueClicked();Check(gm.HasPendingConfirmation,"asked first");Check(Get<EntityDataSO>(gm,"runtimeHeroData")==null,"not loaded early");
                gm.OnCancelResetClicked();Check(save.HasSave(SaveSlot.ManualSave2),"cancel keeps file");
                gm.OnContinueClicked();gm.OnConfirmResetClicked();
                Check(Get<EntityDataSO>(gm,"runtimeHeroData").currentLevel==20&&gm.IsHardMode,"restored level and hard mode");
                Check(Get<bool>(gm,"hasDeployed"),"starts after confirmation");
            } finally { Directory.Delete(dir,true); }
        });
        Run("new game deletes all slots only after confirmation and resets hero",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {
                foreach(SaveSlot slot in Enum.GetValues(typeof(SaveSlot)))Check(save.SaveGame(HeroAt(20),slot),"old slot saved");
                gm.OnNewGameClicked();gm.OnCancelResetClicked();
                foreach(SaveSlot slot in Enum.GetValues(typeof(SaveSlot)))Check(save.HasSave(slot),"cancel preserves every slot");
                gm.OnNewGameClicked();gm.OnConfirmResetClicked();
                var data=Get<EntityDataSO>(gm,"runtimeHeroData");Check(data.currentLevel==1&&data.currentExp==0&&!gm.IsHardMode,"fresh hero normal mode");
                Check(!save.HasSave(SaveSlot.ManualSave1)&&!save.HasSave(SaveSlot.ManualSave2),"old manual slots removed");
                var loaded=HeroAt(99);Check(save.LoadGame(loaded,SaveSlot.AutoSave)&&loaded.currentLevel==1,"new autosave replaces old");
                gm.OnConfirmResetClicked();Check(data==Get<EntityDataSO>(gm,"runtimeHeroData"),"double confirmation ignored");
            } finally { Directory.Delete(dir,true); }
        });
        Run("missing or corrupt save never starts a phantom continue",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {
                Check(!gm.CanContinue,"no save unavailable");gm.OnContinueClicked();Check(!gm.HasPendingConfirmation,"no action");
                File.WriteAllText(Path.Combine(dir,"AutoSave.json"),"broken");gm.OnContinueClicked();gm.OnConfirmResetClicked();
                Check(!Get<bool>(gm,"hasDeployed")&&Get<EntityDataSO>(gm,"runtimeHeroData")==null,"corrupt save rejected");
            } finally {Directory.Delete(dir,true);}
        });
        Run("hard difficulty exactly doubles monster damage and triples kill EXP",()=>{
            foreach(int mode in new[]{0,1}) {
                var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
                try {
                    var hero=HeroAt(20);Set(gm,"runtimeHeroData",hero);f.combat.SetupHeroInfo(hero);gm.SetDifficulty(mode);
                    Set(f.combat,"currentHeroHP",200);
                    typeof(CombatManager).GetMethod("DealDamageToHero",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f.combat,new object[]{10});
                    Check(Get<int>(f.combat,"currentHeroHP")==200-(mode==1?20:10),"damage multiplier");
                    var m=f.Monster(-20);f.Battle(m);int expected=CombatBalance.KillExp(f.Enemies[0].data.currentLevel)*(mode==1?3:1);
                    typeof(CombatManager).GetMethod("HandleMonsterDeath",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f.combat,new object[]{f.Enemies[0]});
                    Check(hero.currentExp==expected,"EXP multiplier");
                    gm.SetDifficulty(0);Check(!gm.IsHardMode,"switch back normal");
                } finally {Directory.Delete(dir,true);}
            }
        });
        Run("map labels and click bindings select normal and hard",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {
                Set(gm,"runtimeHeroData",HeroAt(1));
                var ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;ui.mapMenu=new GameObject(true);
                var normal=new GameObject(true);normal.name="Map1";normal.transform.parent=ui.mapMenu.transform;normal.AddComponent<TextMeshProUGUI>();var n=normal.AddComponent<CustomInteractable>();
                var hard=new GameObject(true);hard.name="Map2";hard.transform.parent=ui.mapMenu.transform;hard.AddComponent<TextMeshProUGUI>();var h=hard.AddComponent<CustomInteractable>();
                Call(ui,"Start");Check(normal.GetComponent<TextMeshProUGUI>().text=="Bình thường"&&hard.GetComponent<TextMeshProUGUI>().text=="Khó","labels replaced");
                h.onClickEvent.Invoke();Check(gm.IsHardMode,"hard click connected");n.onClickEvent.Invoke();Check(!gm.IsHardMode,"normal click connected");
            } finally {UIManager.Instance=null;Directory.Delete(dir,true);}
        });
        Run("real save IO replaces existing file and clears legacy difficulty",()=>{
            var f=new Field();SaveManager save;string dir;MenuManager(f,out save,out dir);
            try {
                var data=HeroAt(5);data.difficulty=1;Check(save.SaveGame(data,SaveSlot.AutoSave),"first write");
                data.currentLevel=8;Check(save.SaveGame(data,SaveSlot.AutoSave),"replace existing file");
                var read=HeroAt(1);Check(save.LoadGame(read,SaveSlot.AutoSave)&&read.currentLevel==8&&read.difficulty==1,"read replaced content");
                File.WriteAllText(Path.Combine(dir,"AutoSave.json"),"{\"currentLevel\":2,\"currentExp\":0}");
                Check(save.LoadGame(read,SaveSlot.AutoSave)&&read.difficulty==0&&read.balanceVersion==0,"legacy absent fields reset");
            } finally {Directory.Delete(dir,true);}
        });
        Run("runtime menu builds two buttons and blocks empty continue",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {
                var canvas=new GameObject(true);gm.preGameUI.transform.parent=canvas.transform;
                var button=new GameObject(true);button.transform.parent=gm.preGameUI.transform;button.AddComponent<Button>();
                var label=new GameObject(true);label.transform.parent=button.transform;label.AddComponent<TextMeshProUGUI>().text="Start";
                var menu=StartMenuUI.Install(gm);Check(menu!=null,"built from scene templates");
                var choices=gm.preGameUI.GetComponentsInChildren<Button>();Check(choices.Length==2,"two visible choices");
                int disabled=0;foreach(var b in choices)if(!b.interactable)disabled++;Check(disabled==1,"continue disabled without save");
                Check(gm.confirmationPopup.GetComponentsInChildren<Button>(true).Length==2,"confirm and cancel exist");
            } finally {Directory.Delete(dir,true);}
        });
    }
}
