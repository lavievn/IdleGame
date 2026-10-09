using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static void Patch54aTests()
    {
        Run("void is standalone, never in hybrid, old mixed saves preserve remaining root grades",()=>{
            bool seen=false;
            for(int n=0;n<2000;n++) {var d=HeroAt(1);d.race=(RaceType)(n%5);SynergyMath.GenerateRootProfile(d);
                if(d.spiritRoots.Contains(ElementType.Vo)){seen=true;Check(d.race!=RaceType.ConLai&&d.spiritRoots.Count==1,"void invariant");}}
            Check(seen,"standalone void can spawn");
            var old=HeroAt(1);old.spiritRoots=new List<ElementType>{ElementType.Hoa,ElementType.Vo,ElementType.Thuy};old.rootTiers=new List<int>{2,5,4};old.rootWeights=new List<float>{.2f,.3f,.5f};old.NormalizeRoots();
            Check(old.spiritRoots.Count==2&&old.rootTiers[0]==2&&old.rootTiers[1]==4,"migration keeps grade alignment");Near(old.rootWeights[0],2f/7,"reweighted");
            old.race=RaceType.ConLai;old.spiritRoots=new List<ElementType>{ElementType.Vo};old.NormalizeRoots();Check(!old.spiritRoots.Contains(ElementType.Vo),"invalid hybrid repaired");
            List<ElementType> child;var none=new List<ElementType>{ElementType.Vo};var fire=new List<ElementType>{ElementType.Hoa};
            Check(SynergyMath.TryResolveVoidInheritance(none,fire,out child)&&child.Count==1&&child[0]==ElementType.Hoa,"one void parent");
            Check(SynergyMath.TryResolveVoidInheritance(fire,none,out child)&&child[0]==ElementType.Hoa,"symmetric parent order");
            Check(SynergyMath.TryResolveVoidInheritance(none,none,out child)&&child.Count==1&&child[0]==ElementType.Vo,"both void parents");
        });
        Run("five maps share a region and five waves per map remain independent",()=>{
            var d=HeroAt(1);d.NormalizeMapProgress();
            for(int group=0;group<12;group++) {
                var theme=d.regionTheme;
                for(int map=0;map<5;map++) {
                    Check(d.regionIndex==group&&d.regionTheme==theme,"stable group");Check(WorldNames.AllowsTerrain(theme,d.mapTerrain),"themed terrain");
                    for(int wave=0;wave<5;wave++)d.CompleteWave();
                }
                Check(d.regionIndex==group+1,"new group after exactly 25 waves");
            }
            Check(d.mapNumber==61,"visit count unaffected");
        });
        Run("habitat excludes forest fish and sea land beasts, desert large cats stay rare",()=>{
            int rare=0;
            for(int i=0;i<10000;i++) {
                foreach(TerrainType terrain in Enum.GetValues(typeof(TerrainType))) {
                    string animal=WorldNames.RandomAnimal(terrain);Check(WorldNames.AnimalAllowed(terrain,animal),"eligible species");
                    if(terrain==TerrainType.RungRam)Check(WorldNames.ClassifyAnimal(animal)!=MonsterClass.ThuySinh,"no forest aquatic animal");
                    if(terrain==TerrainType.Bien)Check(WorldNames.ClassifyAnimal(animal)==MonsterClass.ThuySinh||WorldNames.ClassifyAnimal(animal)==MonsterClass.Chim||animal=="Long","sea roster");
                    if(terrain==TerrainType.SaMac&&(animal=="Hổ"||animal=="Báo"))rare++;
                }
            }
            Check(rare>40&&rare<170,"approximately one percent combined desert cats");
            var f=new Field();var hero=HeroAt(1);hero.mapTerrain=TerrainType.Bien;f.combat.SetupHeroInfo(hero);
            for(int i=0;i<30;i++){f.Battle(f.Monster(-200));var m=f.Enemies[0].data;Check(m.race!=RaceType.NhanToc&&WorldNames.AnimalAllowed(TerrainType.Bien,m.monsterAnimal),"actual sea spawn limited to aquatic dragon or bird");}
        });
        Run("hero never reverses right and monster never reverses left at varied range scale and long frames",()=>{
            foreach(float scale in new[]{.2f,1f,2f})foreach(float dt in new[]{1f/60f,.1f,3f})foreach(AttackMode mode in Enum.GetValues(typeof(AttackMode))) {
                var f=new Field(scale);f.hero.ChangeAttackMode((int)mode);var m=f.Monster(-400,mode);
                for(int i=0;i<20;i++){float h=f.space.Position(f.hero.heroRect).x,x=f.space.Position(m.Rect).x;f.hero.TickMovement(f.space,dt);m.TickMovement(f.space,dt);
                    Check(f.space.Position(f.hero.heroRect).x<=h+.0001f,"hero only left");Check(f.space.Position(m.Rect).x>=x-.0001f,"monster only right");Check(f.space.Position(m.Rect).x<=f.space.Position(f.hero.heroRect).x,"no crossing");}
                f.space.SetPosition(m.Rect,new Vector2(400,0));float before=f.space.Position(m.Rect).x;m.TickMovement(f.space,dt);Near(f.space.Position(m.Rect).x,before,"passed monster does not retreat");
                float heroBefore=f.space.Position(f.hero.heroRect).x;f.hero.TickMovement(f.space,dt);Check(f.space.Position(f.hero.heroRect).x<=heroBefore,"hero does not chase behind");
            }
        });
        Run("hero stats include HP, root attack splits, movement and actual attack frequency",()=>{
            var f=new Field();f.hero.atkStatusText=new GameObject(true).AddComponent<TextMeshProUGUI>();var d=HeroAt(1);d.baseDamage=100;
            d.spiritRoots=new List<ElementType>{ElementType.Hoa,ElementType.Thuy};d.rootTiers=new List<int>{3,4};d.rootWeights=new List<float>{.4f,.6f};
            f.hero.UpdateStats(d,123,400);string text=f.hero.FullStatDetails;
            Check(text.Contains("123/400")&&text.Contains("EXP:")&&text.Contains("ATK Hỏa: 40")&&text.Contains("ATK Thủy: 60")&&text.Contains("Di chuyển:")&&text.Contains("Tốc đánh:"),"full stats");
            f.hero.ChangeAttackMode(1);f.hero.UpdateStats(d,123,400);Check(f.hero.FullStatDetails!=text,"mode changes displayed frequency");
        });
        Run("pause and info work through native-compatible dispatcher and pause freezes movement and launched shots",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {
                var canvas=new GameObject(true);((RectTransform)canvas.transform).sizeDelta=new Vector2(1920,1080);
                gm.eventLog=new GameObject(true).AddComponent<Image>();gm.eventLog.transform.parent=canvas.transform;gm.eventLogText=new GameObject(true).AddComponent<TextMeshProUGUI>();gm.eventLogText.transform.parent=gm.eventLog.transform;
                var ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;ui.transparentWindow=new GameObject().AddComponent<TransparentWindow>();
                var buff=new GameObject("BuffIcon",typeof(RectTransform),typeof(Image));buff.transform.parent=canvas.transform;Call(ui,"Start");
                var d=HeroAt(1);f.combat.SetupHeroInfo(d);f.hero.ChangeAttackMode(1);var m=f.Monster(-200);f.Battle(m);f.hero.TickMovement(f.space,0);Time.deltaTime=.7f;Call(f.combat,"Update");Check(f.combat.PendingProjectileCount==1,"launched");
                float x=f.space.Position(f.hero.heroRect).x;int hp=f.Enemies[0].currentHP;
                ui.HandleMouseClick(buff.transform.position);Check(ui.IsPaused&&Time.timeScale==0,"pause through click");
                f.Step(1,true);Near(f.space.Position(f.hero.heroRect).x,x,"frozen despite test positive delta");Check(f.Enemies[0].currentHP==hp&&f.combat.PendingProjectileCount==1,"no paused projectile impact");
                ui.HandleMouseClick(buff.transform.position);Check(!ui.IsPaused&&Time.timeScale==1,"resume click");
                gm.RecordDamage("first formula");gm.RecordDamage("second formula");ui.OpenDamageInfo();var panel=Get<GameObject>(ui,"damagePanel");var detail=Get<TextMeshProUGUI>(ui,"damageText");
                Check(panel.activeSelf&&detail.text.Contains("second formula"),"latest first");
                foreach(var click in panel.GetComponentsInChildren<CustomInteractable>())if(click.gameObject.name=="DamageOlder")ui.HandleMouseClick(click.transform.position);
                Check(detail.text.Contains("first formula"),"pagination");
                foreach(var click in panel.GetComponentsInChildren<CustomInteractable>())if(click.gameObject.name=="DamageClose")ui.HandleMouseClick(click.transform.position);
                Check(!panel.activeSelf,"close button");
                Check(Array.IndexOf(ui.transparentWindow.clickableUI,buff.GetComponent<RectTransform>())>=0,"native pause hitbox registered");
            } finally {Time.timeScale=1;UIManager.Instance=null;Directory.Delete(dir,true);}
        });
        Run("damage trace snapshots actual formula and only records projectile after impact",()=>{
            var f=new Field();SaveManager save;string dir;var gm=MenuManager(f,out save,out dir);
            try {
                Set(f.combat,"gameManager",gm);var d=HeroAt(1);d.baseDamage=100;d.spiritRoots=new List<ElementType>{ElementType.Thuy};d.rootTiers=new List<int>{1};d.rootWeights=new List<float>{1};d.mapNumber=6;d.difficulty=1;Set(gm,"runtimeHeroData",d);f.combat.SetupHeroInfo(d);f.hero.ChangeAttackMode(1);
                var m=f.Monster(-200);f.Battle(m);var enemy=f.Enemies[0].data;enemy.spiritRoots=new List<ElementType>{ElementType.Hoa};enemy.rootTiers=new List<int>{5};enemy.rootWeights=new List<float>{1};
                f.hero.TickMovement(f.space,0);Time.deltaTime=.7f;Call(f.combat,"Update");Check(gm.DamageHistory.Length==0,"launch not logged as impact");
                d.baseDamage=999;Time.deltaTime=.2f;Call(f.combat,"Update");Check(gm.DamageHistory.Length>0&&gm.DamageHistory[0].Contains("ATK cơ bản: 100")&&gm.DamageHistory[0].Contains("1.075"),"snapshot remains launch values");
                for(int i=0;i<40;i++)gm.RecordDamage(i.ToString());Check(gm.DamageHistory.Length==30,"history bounded");
            } finally {Directory.Delete(dir,true);}
        });
        Run("pause freezes pending wave even with zero configured delay",()=>{
            var f=new Field();var gm=new GameObject().AddComponent<GameManager>();Set(gm,"heroController",f.hero);Set(gm,"hasDeployed",true);gm.waveDelay=0;
            var wait=(System.Collections.IEnumerator)typeof(GameManager).GetMethod("WaitAndCallNextWave",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(gm,null);
            Check(wait.MoveNext(),"initial yield");Time.timeScale=0;Time.deltaTime=1;
            try {for(int i=0;i<10;i++)Check(wait.MoveNext(),"zero delay does not bypass pause");}
            finally {Time.timeScale=1;}
            Check(!wait.MoveNext(),"zero delay finishes after resume");
        });
        Run("region and habitat data survive save and load alongside root repair",()=>{
            var f=new Field();SaveManager save;string dir;MenuManager(f,out save,out dir);
            try {var d=HeroAt(1);d.mapNumber=8;d.mapProgressVersion=1;d.NormalizeMapProgress();string name=d.mapName;var theme=d.regionTheme;
                Check(save.SaveGame(d,SaveSlot.AutoSave),"region saved");var read=HeroAt(1);Check(save.LoadGame(read,SaveSlot.AutoSave),"region loaded");
                Check(read.mapName==name&&read.regionTheme==theme&&read.regionIndex==1,"region exact round trip");
            } finally {Directory.Delete(dir,true);}
        });
    }
}
