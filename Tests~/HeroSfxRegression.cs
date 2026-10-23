using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    sealed class SfxResources : IDisposable
    {
        readonly List<string> paths=new List<string>();
        public SfxResources()
        {
            for(int group=0;group<3;group++) {
                int count=group==0?18:group==1?3:8;
                for(int i=0;i<count;i++) {
                    string path="CombatSfx/"+new[]{"Sword","Shoot","Magic"}[group]+"/test"+i;
                    Resources.Register(path,new AudioClip {name=path,length=.8f});paths.Add(path);
                }
            }
        }
        public void Dispose() {foreach(var path in paths)Resources.Unregister(path);}
    }
    static HeroCombatSfx Bus(Field f) {return Get<HeroCombatSfx>(f.combat,"heroSfx");}
    static void ReadySoundAttack(Field f,HeroController hero)
    {
        var actor=f.combat.StateFor(hero);actor.WindupMode=hero.attackMode;
        actor.AttackTimer=hero.attackMode==AttackMode.RangedMagic?1000:0;
    }
    static void SfxTick(Field f,float dt=.01f)
    {
        Time.deltaTime=dt;Time.time+=dt;Call(f.combat,"Update");Call(Bus(f),"Update");
    }
    static AudioSource[] Sources(Field f) {return f.combat.GetComponentsInChildren<AudioSource>(true);}

    static void HeroSfxTests()
    {
        float oldVolume=AudioVolumeSettings.Get(GameAudioChannel.Sfx);
        using(var resources=new SfxResources())using(var sprites=new SpriteResources())using(var frames=new ArcherFrames())
        try
        {
            foreach(int mode in new[]{0,1,2})
            Run("SFX valid attack emits once in matching group at impact/release / "+mode,()=>{
                var f=new Field();f.hero.ChangeAttackMode(mode);var enemy=f.Monster(-20);f.Battle(enemy);
                var bus=Bus(f);ReadySoundAttack(f,f.hero);
                int hp=f.Enemies[0].currentHP;SfxTick(f);
                Check(bus.StartedCount==1,"one valid event, not per sprite frame");
                Check(bus.LastStartedClip.name.StartsWith("CombatSfx/"+new[]{"Sword","Shoot","Magic"}[mode]+"/"),"strict mode group");
                Check((int)bus.LastStartedMode==mode,"release identity");
                if(mode==0)Check(f.Enemies[0].currentHP<hp && f.combat.PendingProjectileCount==0,"melee sound at damage event");
                else Check(f.combat.PendingProjectileCount==1 && f.Enemies[0].currentHP==hp,"sound at launch, before impact");
                f.hero.heroRect.gameObject.AddComponent<UnityEngine.UI.Image>();CultivationSpriteBridge.AttachHero(f.hero);
                f.hero.PlayAttackFeedback();var bridge=f.hero.heroRect.GetComponent<CultivationSpriteBridge>();
                for(int i=0;i<30;i++) {Time.time+=.01f;Call(bridge,"Update");}
                Check(bus.StartedCount==1,"visual frames and visual attack callbacks are silent");
            });
            Run("SFX absent target/windup/pause cannot start sound",()=>{
                var f=new Field();f.hero.ChangeAttackMode(2);var enemy=f.Monster(-20);f.Battle(enemy);
                SfxTick(f);Check(Bus(f).StartedCount==0,"magic charging silent");
                f.space.SetPosition(enemy.Rect,new Vector2(-10000,0));ReadySoundAttack(f,f.hero);SfxTick(f,10);
                Check(Bus(f).StartedCount==0,"no valid target");
                Time.timeScale=0;Check(!Bus(f).TryPlay(f.combat.CurrentHeroCombatState,f.hero.attackMode),"paused action rejected");
                Time.timeScale=1;
            });
            Run("SFX B2 has three independent owners and only Hero events make sound",()=>{
                var f=new Field();var archer=B2Hero(f,0,AttackMode.RangedPhysical);var mage=B2Hero(f,0,AttackMode.RangedMagic);
                f.combat.RegisterAdditionalHero(archer,B2Data("Archer"));f.combat.RegisterAdditionalHero(mage,B2Data("Mage"));
                var enemy=f.Monster(-20,AttackMode.RangedPhysical);f.Battle(enemy);
                foreach(var hero in new[]{f.hero,archer,mage})ReadySoundAttack(f,hero);
                SfxTick(f);Check(Bus(f).StartedCount==3 && Bus(f).ActiveVoiceCount==3,"one voice per real Hero event");
                int owned=0;foreach(var source in Sources(f))if(source.isPlaying)owned++;
                Check(owned==3,"three actual pooled sources playing");
                archer.HideHero();Call(Bus(f),"Update");Check(Bus(f).ActiveVoiceCount==2,"only removed actor is silenced");
                Check(f.combat.PendingProjectileCount>=2,"audio does not remove launched projectiles");
            });
            Run("SFX cap drops surplus without allocating or stealing; caches are not reloaded",()=>{
                var f=new Field();var bus=Bus(f);var state=f.combat.CurrentHeroCombatState;
                int loads=Resources.LoadCallCount,all=Resources.LoadAllCallCount;
                int objects=UnityEngine.Object.objects.Count;var sources=Sources(f);
                Check(sources.Length==4,"fixed voice pool");
                for(int i=0;i<4;i++)Check(bus.TryPlay(state,AttackMode.Melee),"voice available");
                var first=sources[0].clip;
                for(int i=0;i<100;i++)Check(!bus.TryPlay(state,AttackMode.Melee),"surplus dropped");
                Check(bus.ActiveVoiceCount==4 && bus.DroppedCount==100,"bounded polyphony");
                Check(sources[0].clip==first && sources[0].isPlaying,"no abrupt stealing");
                Check(UnityEngine.Object.objects.Count==objects,"no new audio object per attack");
                Check(Resources.LoadCallCount==loads && Resources.LoadAllCallCount==all,"no Resources call per attack");
                sources[0].Stop();Call(bus,"Update");Check(bus.ActiveVoiceCount==3,"finished source released");
                Check(bus.TryPlay(state,AttackMode.Melee) && Sources(f).Length==4,"reuse existing source");
            });
            Run("SFX shared settings/headroom/pause preserve position without replay",()=>{
                var f=new Field();var bus=Bus(f);AudioVolumeSettings.Preview(GameAudioChannel.Sfx,.7f);
                for(int i=0;i<4;i++)Check(bus.TryPlay(f.combat.CurrentHeroCombatState,AttackMode.Melee),"fill pool");
                float sum=0;foreach(var source in Sources(f))sum+=source.volume;
                Check(sum<=bus.sfxVolume*.7f+.00001f,"bounded summed gain for simultaneous attacks");
                var sources=Sources(f);sources[0].time=.25f;
                Time.timeScale=0;Call(bus,"Update");
                foreach(var source in sources)Check(!source.isPlaying,"pause audio");
                Near(sources[0].time,.25f,"pause retains playhead");
                AudioVolumeSettings.Preview(GameAudioChannel.Sfx,0);Time.timeScale=1;Call(bus,"Update");
                foreach(var source in sources)Check(source.isPlaying && source.volume==0,"resume muted without new attack");
                Near(sources[0].time,.25f,"resume did not Play from zero");
                Check(bus.StartedCount==4,"resume/volume do not trigger events");
                AudioVolumeSettings.Preview(GameAudioChannel.Sfx,.3f);Call(bus,"Update");
                Near(sources[0].volume,bus.sfxVolume*.3f*bus.swordGain/4,"live volume follows existing setting");
            });
            foreach(string cause in new[]{"Death","Hide","Mode","Life","Spawn","Disable"})
            Run("SFX cancels stale owner / "+cause,()=>{
                var f=new Field();var bus=Bus(f);var actor=f.combat.CurrentHeroCombatState;
                Check(bus.TryPlay(actor,AttackMode.Melee),"start sound");
                if(cause=="Death")f.hero.Die();
                else if(cause=="Hide")f.hero.HideHero();
                else if(cause=="Mode")f.hero.ChangeAttackMode(1);
                else if(cause=="Life")actor.BeginLife(actor.Data,actor.MaxHP);
                else if(cause=="Spawn")f.hero.SpawnHero();
                else Call(f.combat,"OnDisable");
                Call(bus,"Update");Check(bus.ActiveVoiceCount==0,"stale source stopped");
                foreach(var source in Sources(f))Check(!source.isPlaying && source.clip==null,"no stuck clip");
            });
            Run("SFX rebind/load/retry and clear-to-menu cancel all active voices",()=>{
                var f=new Field();var bus=Bus(f);
                bus.TryPlay(f.combat.CurrentHeroCombatState,AttackMode.Melee);
                f.combat.SetupHeroInfo(B2Data("Loaded"));Check(bus.ActiveVoiceCount==0,"fresh life resets sound");
                bus.TryPlay(f.combat.CurrentHeroCombatState,AttackMode.Melee);
                f.combat.ForceClearAllMonsters();Check(bus.ActiveVoiceCount==0,"return/reset clear cancels sound");
            });
            Run("SFX dedicated RNG cannot change damage rolls or active attack timer",()=>{
                var f=new Field();var bus=Bus(f);UnityEngine.Random.InitState(774);
                float expected=UnityEngine.Random.Range(.85f,1f);UnityEngine.Random.InitState(774);
                float timer=f.combat.CurrentHeroCombatState.AttackTimer;
                var picked=new HashSet<AudioClip>();AudioClip last=null;
                for(int i=0;i<40;i++) {
                    bus.StopAll();Check(bus.TryPlay(f.combat.CurrentHeroCombatState,AttackMode.Melee),"valid sound");
                    Check(bus.LastStartedClip!=last,"no consecutive repeat when alternatives exist");
                    last=bus.LastStartedClip;picked.Add(last);
                    foreach(var source in Sources(f))Near(source.pitch,1,"pitch opt-out by default");
                }
                Check(picked.Count>1,"random group variants used");
                Near(UnityEngine.Random.Range(.85f,1f),expected,"gameplay RNG untouched");
                Near(f.combat.CurrentHeroCombatState.AttackTimer,timer,"cooldown untouched");
                bus.pitchVariation=.03f;bus.StopAll();bus.TryPlay(f.combat.CurrentHeroCombatState,AttackMode.Melee);
                foreach(var source in Sources(f))Check(source.pitch>=.97f && source.pitch<=1.03f,"optional slight pitch bounded");
            });
            Run("SFX invalid/unregistered actor rejected",()=>{
                var f=new Field();var bus=Bus(f);var outsider=B2Hero(f,0,AttackMode.Melee);
                var fake=new HeroCombatState {Controller=outsider};fake.BeginLife(B2Data("Unregistered"),1000);
                Check(!bus.TryPlay(fake,AttackMode.Melee),"not registered");
                Check(!bus.TryPlay(f.combat.CurrentHeroCombatState,AttackMode.RangedMagic),"wrong mode");
                Check(!bus.TryPlay(null,AttackMode.Melee),"null actor");
            });
        }
        finally {AudioVolumeSettings.Preview(GameAudioChannel.Sfx,oldVolume);Time.timeScale=1;}
        Run("SFX missing clips leaves real combat functional",()=>{
            var f=new Field();f.hero.ChangeAttackMode(1);var enemy=f.Monster(-20);f.Battle(enemy);
            ReadySoundAttack(f,f.hero);SfxTick(f);
            Check(Bus(f).StartedCount==0 && f.combat.PendingProjectileCount==1,"silent fallback still fires projectile");
        });
    }
}
