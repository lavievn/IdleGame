using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static void ModelCanvasProjection(Canvas canvas)
    {
        // Model the engine-driven Canvas transform; not a rendering test.
        var root=(RectTransform)canvas.transform;
        root.pivot=new Vector2(.5f,.5f);
        root.sizeDelta=new Vector2(Screen.width/canvas.scaleFactor,Screen.height/canvas.scaleFactor);
        root.localScale=new Vector3(canvas.scaleFactor,canvas.scaleFactor,1);
        root.position=new Vector3(Screen.width*.5f,Screen.height*.5f,0);
    }
    static Canvas StableCanvasFixture(Field f)
    {
        var root=new GameObject("Original canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        root.GetComponent<Canvas>().additionalShaderChannels=25;
        var rt=(RectTransform)root.transform;rt.sizeDelta=new Vector2(1920,1080);
        f.ground.SetParent(rt,false);f.ground.anchorMin=Vector2.zero;f.ground.anchorMax=new Vector2(1,0);f.ground.sizeDelta=new Vector2(0,100);
        Call(f.space,"InstallStableBattleCanvas");
        var canvas=Get<Canvas>(f.space,"battleCanvas");Check(canvas!=null,"separate presentation Canvas");
        Check(canvas.additionalShaderChannels==25,"keeps vertex channels needed by existing text/effects");
        ModelCanvasProjection(canvas);f.hero.SpawnHero();return canvas;
    }
    static GameManager WaveFixture(Field f,out SaveManager save,out string dir)
    {
        var gm=MenuManager(f,out save,out dir);
        Set(gm,"environmentManager",f.space);Set(gm,"monsterSpawner",f.spawner);
        var hero=HeroAt(20);hero.mapProgressVersion=1;hero.mapNumber=8;hero.completedWavesInMap=3;hero.NormalizeMapProgress();
        Set(gm,"runtimeHeroData",hero);f.combat.SetupHeroInfo(hero);Set(gm,"hasDeployed",true);gm.currentMonsterDataSO=new EntityDataSO();
        // The pool test double deliberately supplies live controller instances.
        Set(f.spawner,"monsterPool",new UnityEngine.Pool.ObjectPool<GameObject>(
            ()=>f.Monster(-600).gameObject,m=>{m.SetActive(true);},m=>m.SetActive(false),m=>{},5,20));
        return gm;
    }
    static void FinishWaveWait(GameManager gm)
    {
        var c=Get<Coroutine>(gm,"nextWaveCoroutine");Check(c!=null,"wait scheduled");
        Time.deltaTime=10;while(c.routine.MoveNext()){};
    }
    static void Patch54dTests()
    {
        Run("legacy stretched Ground changes opposing anchors and leaves shot endpoint behind",()=>{
            var parent=(RectTransform)new GameObject(true).transform;parent.sizeDelta=new Vector2(800,450);
            var ground=(RectTransform)new GameObject(true).transform;ground.parent=parent;ground.anchorMin=Vector2.zero;ground.anchorMax=new Vector2(1,0);ground.sizeDelta=new Vector2(0,100);
            var hero=(RectTransform)new GameObject(true).transform;hero.parent=ground;hero.anchorMin=hero.anchorMax=new Vector2(1,0);
            var monster=(RectTransform)new GameObject(true).transform;monster.parent=ground;monster.anchorMin=monster.anchorMax=Vector2.zero;
            float gap=ground.InverseTransformPoint(hero.position).x-ground.InverseTransformPoint(monster.position).x;
            float end=ground.InverseTransformPoint(monster.position).x;parent.sizeDelta=new Vector2(250,141);
            Check(Math.Abs((ground.InverseTransformPoint(hero.position).x-ground.InverseTransformPoint(monster.position).x)-gap)>500,"relative distance jumps");
            Check(Math.Abs(ground.InverseTransformPoint(monster.position).x-end)>200,"fixed shot no longer matches stationary target");
        });
        Run("stable battle coordinates ranges target and decorations survive every resize and aspect ratio",()=>{
            var f=new Field();var canvas=StableCanvasFixture(f);var m=f.Monster(-30);f.hero.TickMovement(f.space,0);m.TickMovement(f.space,0);
            RectTransform[] decor;var terrain=TerrainField(f,out decor);
            var hp=f.space.Position(f.hero.heroRect);var mp=f.space.Position(m.Rect);var dp=f.space.Position(decor[0]);
            float range=f.space.AttackRange(AttackMode.Melee,true),phase=terrain.TilePhase;
            foreach(var size in new[]{new Vector2(800,450),new Vector2(500,281),new Vector2(250,141),new Vector2(900,700),new Vector2(800,450)}) {
                Screen.SetResolution((int)size.x,(int)size.y,FullScreenMode.Windowed);f.space.SyncBattleProjection();ModelCanvasProjection(canvas);terrain.Scroll(0);
                Near(f.space.Width,1920,"logical width fixed");Near(canvas.scaleFactor,size.x/1920,"projection only");
                Near(f.space.Position(f.hero.heroRect).x,hp.x,"hero X");Near(f.space.Position(m.Rect).x,mp.x,"monster X");Near(f.space.Position(m.Rect).y,mp.y,"monster Y");
                Near(f.space.Position(decor[0]).x,dp.x,"decoration not randomized");Near(f.space.Position(decor[0]).y,dp.y,"decoration height unchanged");Near(terrain.TilePhase,phase,"loop phase unchanged");
                Near(f.space.AttackRange(AttackMode.Melee,true),range,"range fixed");Check(f.hero.CurrentTarget==m&&m.CurrentTarget==f.hero&&f.hero.CanAttack(m,f.space),"targets remain eligible");
            }
        });
        Run("resize preserves bow and magic endpoints flight time damage and monster life",()=>{
            foreach(var mode in new[]{AttackMode.RangedPhysical,AttackMode.RangedMagic}) {
                var f=new Field();var canvas=StableCanvasFixture(f);f.hero.ChangeAttackMode((int)mode);var m=f.Monster(-200);f.Battle(m);Tough(f);
                CombatStep(f,mode==AttackMode.RangedMagic?2:.01f);Check(ShotList(f).Count==1,"hero shot launched");
                var shot=ShotList(f)[0];var start=ShotField<Vector2>(shot,"start");var end=ShotField<Vector2>(shot,"end");float elapsed=ShotField<float>(shot,"elapsed");
                foreach(int width in new[]{500,250,800}){Screen.SetResolution(width,width*9/16,FullScreenMode.Windowed);f.space.SyncBattleProjection();ModelCanvasProjection(canvas);}
                Check(ShotList(f)[0]==shot&&m.IsAlive,"same flight and target life");Near(ShotField<Vector2>(shot,"start").x,start.x,"start unchanged");Near(ShotField<Vector2>(shot,"end").x,end.x,"end unchanged");Near(ShotField<float>(shot,"elapsed"),elapsed,"no cooldown reset");
                CombatStep(f,1.3f);Check(f.Enemies[0].currentHP<10000,"stationary target still hit after resize");
            }
        });
        Run("paused projection resize changes no HP motion charge or pending projectiles",()=>{
            var f=new Field();var canvas=StableCanvasFixture(f);f.hero.ChangeAttackMode(2);var m=f.Monster(-200);f.Battle(m);CombatStep(f,1);
            float charge=Get<float>(f.combat,"heroAttackTimer"),x=f.space.Position(m.Rect).x;int hp=f.combat.CurrentHeroHP;
            Time.timeScale=0;Screen.SetResolution(250,141,FullScreenMode.Windowed);Call(f.space,"Update");ModelCanvasProjection(canvas);Call(f.combat,"Update");
            Near(canvas.scaleFactor,250f/1920,"paused projection updates");Near(f.space.Position(m.Rect).x,x,"no movement");Near(Get<float>(f.combat,"heroAttackTimer"),charge,"charge preserved");Check(f.combat.CurrentHeroHP==hp,"HP preserved");Time.timeScale=1;
        });
        Run("escaped wave clears old lives shots and targets without granting completion or HP",()=>{
            var f=new Field();SaveManager save;string dir;var gm=WaveFixture(f,out save,out dir);
            try {
                var m=f.Monster(-200);var n=f.Monster(-240);f.Battle(m,n);gm.activeMonsters.Add(m.gameObject);gm.activeMonsters.Add(n.gameObject);Set(gm,"waveSpawnCount",2);
                f.hero.ChangeAttackMode(1);f.hero.TickMovement(f.space,0);CombatStep(f,.01f);Check(f.combat.PendingProjectileCount>0,"has old shot");
                Set(f.combat,"currentHeroHP",100);int exp=gm.HeroData.currentExp,visits=gm.HeroData.MapVisits;
                f.space.SetPosition(m.Rect,new Vector2(600,0));Call(gm,"LateUpdate");
                Check(!m.IsAlive&&!n.IsAlive&&gm.activeMonsters.Count==0&&f.combat.PendingProjectileCount==0,"clears both monsters and old projectile");
                Check(gm.HeroData.mapNumber==8&&gm.HeroData.completedWavesInMap==3&&gm.HeroData.currentExp==exp&&gm.HeroData.MapVisits==visits,"no completion XP or map entry");
                Check(f.combat.CurrentHeroHP==100&&f.hero.CurrentTarget==null,"no healing and target reset");Near(f.space.Position(f.hero.heroRect).x,f.space.HomeX,"hero re-centered for replay");
                var pending=Get<Coroutine>(gm,"nextWaveCoroutine");Call(gm,"LateUpdate");Check(Get<Coroutine>(gm,"nextWaveCoroutine")==pending,"only one retry scheduled");
                FinishWaveWait(gm);Check(gm.activeMonsters.Count==2&&f.Enemies.Count==2,"same wave size returns");Check(Get<Coroutine>(gm,"nextWaveCoroutine")==null,"wait cleared");
                Check(gm.HeroData.completedWavesInMap==3,"replay is unfinished wave");
            } finally{Directory.Delete(dir,true);}
        });
        Run("edge guard waits for entire body and does not reset spawn-left visible or paused monsters",()=>{
            var f=new Field();SaveManager save;string dir;var gm=WaveFixture(f,out save,out dir);
            try {
                var m=f.Monster(-650);f.Battle(m);gm.activeMonsters.Add(m.gameObject);
                Call(gm,"LateUpdate");Check(Get<Coroutine>(gm,"nextWaveCoroutine")==null,"left spawn valid");
                f.space.SetPosition(m.Rect,new Vector2(530,0));Check(!f.space.HasEscapedRight(m.Rect),"partly visible or padding");Call(gm,"LateUpdate");Check(m.IsAlive,"not despawned early");
                f.space.SetPosition(m.Rect,new Vector2(700,0));Time.timeScale=0;Call(gm,"LateUpdate");Check(m.IsAlive,"paused guard does not replay");Time.timeScale=1;
                Set(gm,"hasDeployed",false);Call(gm,"LateUpdate");Check(m.IsAlive,"pre-game/death guard off");
            }finally{Time.timeScale=1;Directory.Delete(dir,true);}
        });
        Run("cancel or death during escaped-wave wait cannot spawn a stale retry",()=>{
            var f=new Field();SaveManager save;string dir;var gm=WaveFixture(f,out save,out dir);
            try {
                var m=f.Monster(700);f.Battle(m);gm.activeMonsters.Add(m.gameObject);Call(gm,"LateUpdate");var wait=Get<Coroutine>(gm,"nextWaveCoroutine");
                f.hero.Die();gm.OnHeroDied();Check(wait.stopped&&Get<Coroutine>(gm,"nextWaveCoroutine")==null,"pending retry stopped");Check(Get<int>(gm,"retryWaveSize")==0,"size cannot leak into next run");
                Check(!wait.routine.MoveNext()&&gm.activeMonsters.Count==0,"even stale enumerator cannot spawn after death");
            } finally{Directory.Delete(dir,true);}
        });
        Run("defeat restarts first map of current five-map region and wave one, preserving character",()=>{
            foreach(int map in new[]{1,5,6,8,10,11,100,int.MaxValue}) {
                var d=HeroAt(20);d.mapProgressVersion=1;d.mapNumber=map;d.completedWavesInMap=4;d.NormalizeMapProgress();
                int index=d.regionIndex,exp=d.currentExp,visits=d.MapVisits;var theme=d.regionTheme;var roots=new List<ElementType>(d.spiritRoots);
                d.RestartRegionAfterDefeat();Check(d.mapNumber==((map-1)/5)*5+1&&d.completedWavesInMap==0,"checkpoint and wave");
                Check(d.regionIndex==index&&d.regionTheme==theme&&WorldNames.AllowsTerrain(theme,d.mapTerrain),"same themed region");Check(d.currentLevel==20&&d.currentExp==exp&&d.spiritRoots.Count==roots.Count,"same hero");
                Check(d.mapVisits==(visits==int.MaxValue?visits:visits+1)&&d.difficulty==(d.mapNumber%6==0?1:0),"visits saturate and difficulty reflects restarted map");
            }
        });
        Run("death autosaves checkpoint before retry or application restart, with idempotent death",()=>{
            var f=new Field();SaveManager save;string dir;var gm=WaveFixture(f,out save,out dir);
            try {
                var before=gm.HeroData;f.hero.Die();gm.OnHeroDied();int visits=before.mapVisits;
                Check(before.mapNumber==6&&before.completedWavesInMap==0&&!Get<bool>(gm,"hasDeployed"),"ready at first map of current region");
                var loaded=HeroAt(1);Check(save.LoadGame(loaded,SaveSlot.AutoSave)&&loaded.mapNumber==6&&loaded.completedWavesInMap==0&&loaded.currentLevel==20,"checkpoint persists before retry");
                gm.OnHeroDied();Check(before.mapVisits==visits,"duplicate callback ignored");gm.OnRetryClicked();Check(Get<bool>(gm,"hasDeployed")&&gm.HeroData.mapNumber==6&&gm.HeroData.completedWavesInMap==0,"retry deploys checkpoint");
                Check(f.combat.CurrentHeroHP==f.combat.MaxHeroHP&&gm.activeMonsters.Count>0,"retry heals and spawns");
            } finally{Directory.Delete(dir,true);}
        });
        Run("old saves migrate visits and replay does not rewind damage progression",()=>{
            var f=new Field();SaveManager save;string dir;var gm=WaveFixture(f,out save,out dir);
            try {
                File.WriteAllText(Path.Combine(dir,"AutoSave.json"),"{\"currentLevel\":20,\"mapProgressVersion\":1,\"mapNumber\":80,\"completedWavesInMap\":4}");
                var d=HeroAt(1);d.mapVisits=999;Check(save.LoadGame(d,SaveSlot.AutoSave)&&d.mapVisits==80,"missing field never leaks template state");
                float scale=WorldNames.MonsterDamageScale(d.MapVisits);d.RestartRegionAfterDefeat();Check(d.mapNumber==76&&WorldNames.MonsterDamageScale(d.MapVisits)>=scale,"damage tier does not rewind");
                Check(save.SaveGame(d,SaveSlot.AutoSave),"new field saved");var loaded=HeroAt(1);Check(save.LoadGame(loaded,SaveSlot.AutoSave)&&loaded.mapNumber==76&&loaded.mapVisits==81,"both counters round-trip");
                d.completedWavesInMap=4;d.CompleteWave();Check(d.mapVisits==82&&d.mapNumber==77,"normal entry increments once");
            }finally{Directory.Delete(dir,true);}
        });
        Screen.SetResolution(800,450,FullScreenMode.Windowed);Time.timeScale=1;
    }
}
