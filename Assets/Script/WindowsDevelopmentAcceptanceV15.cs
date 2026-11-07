#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TuTienCore;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Video;
using Profiler = UnityEngine.Profiling.Profiler;

public sealed partial class WindowsDevelopmentAcceptanceV10
{
    private const double TrialWindowSeconds = 6;

    [Serializable]
    private sealed class TrialMeasurements
    {
        public int width, height, targetFps, decoderErrors;
        public string gpu, driver, graphicsApi, musicClip;
        public bool pauseVerified, corruptFallbackVerified;
        public List<TrialSample> samples = new List<TrialSample>();
    }

    [Serializable]
    private sealed class TrialSample
    {
        public string sky;
        public bool mesh, bgm;
        public int frames, gen0, videoPlayers, renderTargets, vfxObjects, meshBuilds, peakVertices;
        public long strikes, monoBefore, monoAfter, memoryBefore, memoryAfter, videoFrameBefore, videoFrameAfter;
        public double seconds, frameMeanMs, frameP95Ms, frameP99Ms, mainMeanMs, mainP95Ms, mainP99Ms;
        public double playerLoopMeanMs, processCpuMsPerFrame, gcBytesPerFrame, meshMeanUs, strikeVisualMeanUs;
    }

    private static double Percentile(List<double> sorted, double p) =>
        sorted.Count == 0 ? -1 : sorted[(int)Math.Ceiling(sorted.Count*p)-1];

    private IEnumerator PrepareTrialV15(bool recordOpening = false)
    {
        gm.OnNewGameClicked();
        if(recordOpening)
        {
            yield return Resize(800,450);
            var message=(TMPro.TextMeshProUGUI)Get(gm.GetComponent<StartMenuUI>(),"message");
            Require(message.enableAutoSizing && message.enableWordWrapping &&
                message.fontSizeMin==16 && message.fontSizeMax==18,"opening confirmation typography in Player");
            yield return Capture("opening-confirmation");
        }
        gm.OnConfirmResetClicked();
        yield return Ready();
        DevelopmentV13GameplayScenario.SeedHero(gm,saves,"sword",WeaponFamily.Sword);
        yield return Ready();
        yield return Resize(800,450);
        Require(result.uiIssues.Count==0 && gm.TribulationLockedReason=="", "isolated eligible trial at 800×450");
    }

    private IEnumerator ProfileTrialV15()
    {
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        Application.runInBackground=true;
        yield return PrepareTrialV15();
        Set(combat,"trialSky",HeavenlyTrialView.SkyVariant.Still);
        Set(MapBgmController.Instance,"trialTrackRandom",new System.Random(42));
        Require(gm.RequestTribulation(),"start measured encounter");
        combat.CurrentHeroCombatState.AttackTimer=1000;
        yield return Until(()=>combat.HeavenlyGateActive && !combat.HeavenlyGateApproaching,40,"measured combat range");

        var view=combat.TrialView;
        var visual=view.Lightning;
        var audio=(AudioSource)Get(MapBgmController.Instance,"trialSource");
        Require(audio!=null && audio.isPlaying,"owned BGM playing");
        var data=new TrialMeasurements
        {
            width=Screen.width,height=Screen.height,targetFps=Application.targetFrameRate,
            gpu=SystemInfo.graphicsDeviceName,driver=SystemInfo.graphicsDeviceVersion,
            graphicsApi=SystemInfo.graphicsDeviceType.ToString(),musicClip=audio.clip.name
        };
        var still=HeavenlyTrialView.SkyVariant.Still;
        var a=HeavenlyTrialView.SkyVariant.A;
        var b=HeavenlyTrialView.SkyVariant.B;
        var skies=new[]{still,a,b,a,a,a,a,b,a,still};
        var renderMesh=new[]{false,false,false,true,true,true,true,false,false,false};
        var playMusic=new[]{false,false,false,false,true,true,false,false,false,false};
        audio.Pause();
        for(int i=0;i<skies.Length;i++)
        {
            visual.enabled=renderMesh[i];
            if(playMusic[i] && !audio.isPlaying)audio.UnPause();
            if(!playMusic[i] && audio.isPlaying)audio.Pause();
            view.SetProbeSky(skies[i]);
            if(skies[i]!=still)
                yield return Until(()=>view.IsShowingVideo,8,"decoded benchmark sky "+skies[i]);
            yield return new WaitForSecondsRealtime(1.25f);
            var row=new TrialSample{sky=skies[i].ToString(),mesh=renderMesh[i],bgm=playMusic[i]};
            Require(audio.isPlaying==row.bgm,"BGM controlled independently");
            yield return SampleTrialWindow(view,row);
            data.samples.Add(row);
        }

        view.SetProbeSky(a);
        yield return Until(()=>view.IsShowingVideo,8,"pause validation first frame");
        ui.SetPaused(true);
        yield return Until(()=>view.VideoPaused,4,"decoder paused");
        long pausedFrame=view.VideoFrame, pausedStrikes=combat.TrialStrikeCount;
        yield return new WaitForSecondsRealtime(.35f);
        Require(view.VideoFrame==pausedFrame && combat.TrialStrikeCount==pausedStrikes,"pause freezes video and strikes");
        ui.SetPaused(false);
        yield return Until(()=>view.VideoFrame!=pausedFrame,4,"decoder resumed");
        data.pauseVerified=true;

        string invalid=Path.Combine(Evidence,"invalid-sky.mp4");
        File.WriteAllBytes(invalid,new byte[]{0,1,2,3,4});
        view.SetProbeSky(a,invalid);
        yield return Until(()=>view.IsShowingStill && !view.ProbeHasRenderTarget,8,"corrupt source falls back");
        yield return null;
        Require(view.GetComponents<VideoPlayer>().Length==0,"corrupt source releases its decoder");
        data.decoderErrors=view.ProbeVideoErrors;
        data.corruptFallbackVerified=true;
        File.WriteAllText(Path.Combine(Evidence,"trial-performance.json"),JsonUtility.ToJson(data,true));

        visual.enabled=true;
        audio.UnPause();
        combat.CurrentHeroCombatState.AttackTimer=0;
        yield return Until(()=>!gm.TribulationFighting,20,"finish measured battle");
        yield return Ready();
        Require(view==null && !audio.isPlaying &&
            TribulationSaveTransaction.Read(gm.HeroData.tribulationJson).outcome=="Success",
            "normal terminal Save and presentation cleanup after measurement");
        Pass("Ten paired windows, same live Hero and encounter; all windows kept real lightning damage. Corrupt MP4, pause/resume and terminal cleanup verified.");
    }

    private IEnumerator SampleTrialWindow(HeavenlyTrialView view, TrialSample row)
    {
        var visual=view.Lightning;
        bool video=row.sky!="Still";
        row.videoPlayers=view.GetComponents<VideoPlayer>().Length;
        row.renderTargets=view.ProbeHasRenderTarget?1:0;
        row.vfxObjects=visual==null?0:1;
        Require(row.videoPlayers==(video?1:0) && row.renderTargets==(video?1:0) && row.vfxObjects==1,
            "one trial visual and only the requested video resources");
        var frameTimes=new List<double>(1024);
        var mainTimes=new List<double>(1024);
        double allocated=0,loopTime=0;
        using(var process=Process.GetCurrentProcess())
        using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
        using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
        using(var loop=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"PlayerLoop"))
        {
            Require(gc.Valid && main.Valid,"GC and main thread counters");
            var frameBoundary=new WaitForEndOfFrame();
            visual.ResetProbeTiming();
            visual.ProbeTiming=true;
            int collections=GC.CollectionCount(0);
            long firstStrike=combat.TrialStrikeCount;
            long firstWrite=saves.SuccessfulSnapshotWrites;
            row.videoFrameBefore=view.VideoFrame;
            row.monoBefore=Profiler.GetMonoUsedSizeLong();
            row.memoryBefore=Profiler.GetTotalAllocatedMemoryLong();
            double cpuAtStart=process.TotalProcessorTime.TotalMilliseconds;
            double begin=Time.realtimeSinceStartupAsDouble;
            do
            {
                yield return frameBoundary;
                Require(gm.TribulationFighting && Time.timeScale>0,"live combat in each measurement");
                frameTimes.Add(Time.unscaledDeltaTime*1000);
                mainTimes.Add(main.LastValue/1e6);
                allocated+=gc.LastValue;
                if(loop.Valid)loopTime+=loop.LastValue/1e6;
            } while(Time.realtimeSinceStartupAsDouble-begin<TrialWindowSeconds);
            row.seconds=Time.realtimeSinceStartupAsDouble-begin;
            row.processCpuMsPerFrame=(process.TotalProcessorTime.TotalMilliseconds-cpuAtStart)/frameTimes.Count;
            visual.ProbeTiming=false;
            row.strikes=combat.TrialStrikeCount-firstStrike;
            row.gen0=GC.CollectionCount(0)-collections;
            row.monoAfter=Profiler.GetMonoUsedSizeLong();
            row.memoryAfter=Profiler.GetTotalAllocatedMemoryLong();
            row.videoFrameAfter=view.VideoFrame;
            row.playerLoopMeanMs=loop.Valid?loopTime/frameTimes.Count:-1;
            Require(saves.SuccessfulSnapshotWrites==firstWrite && row.strikes>=4,
                "fixed active workload with no Save write during measurement");
            Require(!video || view.IsShowingVideo && row.videoFrameBefore!=row.videoFrameAfter,"actual video frame advance");
        }
        row.frames=frameTimes.Count;
        row.gcBytesPerFrame=allocated/row.frames;
        row.frameMeanMs=Mean(frameTimes);
        row.mainMeanMs=Mean(mainTimes);
        frameTimes.Sort();
        mainTimes.Sort();
        row.frameP95Ms=Percentile(frameTimes,.95);
        row.frameP99Ms=Percentile(frameTimes,.99);
        row.mainP95Ms=Percentile(mainTimes,.95);
        row.mainP99Ms=Percentile(mainTimes,.99);
        row.meshBuilds=visual.ProbeMeshBuilds;
        row.peakVertices=visual.ProbePeakVertices;
        row.meshMeanUs=row.meshBuilds==0?0:visual.ProbeMeshTicks*1e6/Stopwatch.Frequency/row.meshBuilds;
        row.strikeVisualMeanUs=visual.ProbeStrikeTicks*1e6/Stopwatch.Frequency/row.strikes;
        Require(row.mesh?(row.meshBuilds>0 && row.peakVertices>0):row.meshBuilds==0,"only enabled renderer builds the mesh");
    }

    private IEnumerator ManualTrialV15()
    {
        yield return PrepareTrialV15(true);
        Pass("Native mode ready with seeded owned Hero. Open Character and click the trial button.");
        File.WriteAllText(Path.Combine(Evidence,"native-ready.txt"),"800x450; waiting for Windows input");
        yield return Until(()=>gm.HasTribulationLiability,120,"Windows input started trial");
        yield return Until(()=>combat.TrialStrikeCount>0 || !gm.HasTribulationLiability,45,"live native-started encounter");
        yield return Capture("native-trial");
        yield return Until(()=>!gm.HasTribulationLiability,55,"native-started result");
        yield return Ready();
        Require(TribulationSaveTransaction.Read(gm.HeroData.tribulationJson).outcome=="Success","native-started result persisted");
        yield return Capture("native-result");
        result.nativeInput="Observed native-started trial and terminal result; see native input session.";
        Pass("Owned fixture prepared the Hero; native UI started the trial and real combat completed.");
    }

    private IEnumerator BalanceTrialV15()
    {
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=60;
        Application.runInBackground=true;
        UnityEngine.Random.InitState(15015);
        gm.OnNewGameClicked();
        gm.OnConfirmResetClicked();
        yield return Ready();
        yield return Resize(800,450);
        Set(combat,"trialSky",HeavenlyTrialView.SkyVariant.Still);
        var levels=new[]{1,1,10,10,25,25,40,40};
        var weapons=new[]{WeaponFamily.Unarmed,WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff,
            WeaponFamily.Unarmed,WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff};
        var rows=new List<string>{"level,weapon,weapon_template,map,hp,attack,armor,magic_resistance,attack_interval_s,outcome,fight_s,strikes,first_incoming_hp,hero_hp_end,gate_hp_last,accepted_hits"};
        for(int i=0;i<levels.Length;i++)
        {
            int level=levels[i];
            var family=weapons[i];
            DevelopmentV13GameplayScenario.Weapon(gm,family);
            var h=Instantiate(gm.HeroData);
            h.currentLevel=level;
            h.currentExp=0;
            h.tribulationJson="";
            h.ApplyHeroBalance();
            using(var binding=new CultivationGameplayBinding(h,saves.CultivationProfile,
                HeavenlyGateV13Fixtures.Session(saves.CultivationProfile)))
                Require(binding.Advance(h,0,.037,false,h.worldMapId)==CultivationReason.None,"balance fixture cultivation");
            Require(saves.SaveGame(h,SaveSlot.ManualSave2),"save balance Hero");
            Destroy(h);
            Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"load balance Hero");
            yield return Ready();
            var data=gm.HeroData;
            int hp=combat.CurrentHeroHP, attack=WeaponBasicAttack.DamageSource(data);
            var mode=WeaponBasicAttack.Resolve(data);
            string template=WeaponBasicAttack.Equipped(data)?.templateId ?? "unarmed";
            float armor=CombatStats.Get(data,CombatStatId.Armor,0);
            float mr=CombatStats.Get(data,CombatStatId.MagicResistance,0);
            float interval=WeaponBasicAttack.Interval(mode,CombatStats.Get(data,CombatStatId.AttackSpeed,data.baseAttackSpeed,
                ElementType.Vo,new[]{CombatStats.StanceTag(mode)}));
            long strikes=combat.TrialStrikeCount,hits=combat.AcceptedHeroHitCount;
            Require(gm.RequestTribulation(),"balance sample starts actual encounter");
            yield return Until(()=>combat.HeavenlyGateActive && !combat.HeavenlyGateApproaching,40,"balance sample approach");
            double started=Time.timeAsDouble;
            int firstDamage=0, gateHP=combat.HeavenlyGateHP;
            while(gm.TribulationFighting)
            {
                gateHP=combat.HeavenlyGateHP;
                if(Time.timeAsDouble-started>=120)
                {
                    rows.Add(FormattableString.Invariant($"{level},{family},{template},{data.worldMapId},{hp},{attack},{armor:0.###},{mr:0.###},{interval:0.###},Censored,120,{combat.TrialStrikeCount-strikes},{firstDamage},{combat.CurrentHeroHP},{gateHP},{combat.AcceptedHeroHitCount-hits}"));
                    File.WriteAllLines(Path.Combine(Evidence,"trial-balance.csv"),rows);
                    throw new InvalidOperationException("Balance sample remained Active at 120 s. The observation was preserved.");
                }
                if(firstDamage==0 && combat.TrialStrikeCount>strikes)
                    firstDamage=combat.LastTrialStrike.HPBefore-combat.LastTrialStrike.HPAfter;
                yield return null;
            }
            double fight=Time.timeAsDouble-started;
            if(firstDamage==0 && combat.TrialStrikeCount>strikes)
                firstDamage=combat.LastTrialStrike.HPBefore-combat.LastTrialStrike.HPAfter;
            yield return Ready();
            var receipt=TribulationSaveTransaction.Read(gm.HeroData.tribulationJson);
            Require(gm.Cultivation.Current.Reserve.Amount==1234 && gm.HeroData.currentLevel==level &&
                gm.HeroData.currentExp==0 && !gm.HasTribulationLiability,"balance outcome real and preserves progression");
            if(receipt.outcome=="Success")gateHP=0;
            rows.Add(FormattableString.Invariant($"{level},{family},{template},{data.worldMapId},{hp},{attack},{armor:0.###},{mr:0.###},{interval:0.###},{receipt.outcome},{fight:0.###},{combat.TrialStrikeCount-strikes},{firstDamage},{gm.LastTribulationHeroHP},{gateHP},{combat.AcceptedHeroHitCount-hits}"));
            File.WriteAllLines(Path.Combine(Evidence,"trial-balance.csv"),rows);
        }
        Pass("Eight normal stat/weapon samples used real combat and terminal Save. Level and demo weapon are fixture inputs; outcomes are observed.");
    }
}
#endif
