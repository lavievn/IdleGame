#if DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using TuTienCore;
using UnityEngine;
using Unity.Profiling;
public sealed partial class WindowsDevelopmentAcceptanceV10
{
    [Serializable] sealed class V12Window {public string mode;public int frames,gen0;public double seconds,globalBytesPerFrame;public CultivationPerformance.Row[] parts;public bool allocationSupported;public long allocationControl;}
    [Serializable] sealed class AllocationWindow {public string mode;public int samples,callsPerFrame=256;public double globalBytesPerFrame,estimatedIncrementalBytesPerCall;}
    [Serializable] sealed class AllocationControls {public string method="Global GC Allocated In Frame, four warm frames then64 steady frames; subtract paused empty-loop baseline. Estimates include host noise; not per-thread counters.";public System.Collections.Generic.List<AllocationWindow> windows=new System.Collections.Generic.List<AllocationWindow>();}
    IEnumerator AllocationControlsV12()
    {
        var evidence=new AllocationControls();double baseline=0;
        foreach(string mode in new[]{"empty-loop","tick-only","json-only"}){
            var session=CultivationGameplayState.New(gm.Cultivation.Profile);var window=new AllocationWindow{mode=mode};long step=0,total=0;
            allocation=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");Require(allocation.Valid,"Supported global allocation control");
            for(int frame=0;frame<68;frame++){
                if(frame>=4){total+=allocation.LastValue;window.samples++;}
                for(int i=0;i<256;i++){
                    if(mode=="tick-only")Require(session.Tick(session.OwnerId,session.Epoch,step++,.1,false,.5)==CultivationReason.None,"Allocation Tick control");
                    else if(mode=="json-only")GC.KeepAlive(CultivationGameplayState.Capture(session,gm.Cultivation.Profile));
                }
                yield return new WaitForEndOfFrame();
            }
            allocation.Dispose();window.globalBytesPerFrame=(double)total/window.samples;if(mode=="empty-loop")baseline=window.globalBytesPerFrame;
            window.estimatedIncrementalBytesPerCall=mode=="empty-loop"?0:(window.globalBytesPerFrame-baseline)/256;evidence.windows.Add(window);
        }
        File.WriteAllText(Path.Combine(Evidence,"allocation-controls.json"),JsonUtility.ToJson(evidence,true));
    }
    IEnumerator TribulationV12()
    {
        var checks=HeavenlyTribulationFixtures.Run();File.WriteAllLines(Path.Combine(Evidence,"tribulation-checks.txt"),checks);Pass("V12 B fixture domain assertions="+checks.Count+"; production factory locked, no live gameplay trial.");
        yield return CapacityV11();
    }
    IEnumerator PerformanceV12()
    {
        File.WriteAllText(Path.Combine(Evidence,"isolated-measurements.json"),JsonUtility.ToJson(CultivationV12Measurements.Run(),true));
        gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();yield return Resize(800,450);
        yield return AllocationControlsV12();
        // Both windows use the same paused canonical state: UI cost comparison has no combat/progression confound.
        int windowIndex=0;foreach(bool open in new[]{false,true,false,true}){
            if(open)ui.OpenHeroStats();else ui.CloseDamageInfo();yield return new WaitForSecondsRealtime(1);
            allocation=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");Require(allocation.Valid,"Global allocation recorder supported");
            var window=new V12Window{mode=open?"character-open-paused":"character-closed-paused"};int gen=GC.CollectionCount(0);long total=0;float begin=Time.realtimeSinceStartup;
            CultivationPerformance.Start();while(Time.realtimeSinceStartup-begin<8){yield return new WaitForEndOfFrame();window.frames++;total+=allocation.LastValue;}
            window.parts=CultivationPerformance.Stop();window.seconds=Time.realtimeSinceStartup-begin;window.gen0=GC.CollectionCount(0)-gen;window.globalBytesPerFrame=(double)total/window.frames;window.allocationSupported=CultivationPerformance.AllocationSupported;window.allocationControl=CultivationPerformance.AllocationControl;allocation.Dispose();
            File.WriteAllText(Path.Combine(Evidence,"ui-window-"+(windowIndex++)+"-"+(open?"open":"closed")+".json"),JsonUtility.ToJson(window,true));
        }
        ui.CloseDamageInfo();yield return ProfileFarm();
        Require(gm.TryManualSave(SaveSlot.ManualSave2),"Measured checkpoint Save");string exact=gm.HeroData.cultivationJson;Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Measured checkpoint Load");yield return Ready();Require(gm.HeroData.cultivationJson==exact,"Exact measured Save/Load");
        Pass("V12 A isolated Tick/JSON/binding three warmed repeats + real Character open/closed paused windows + unmodified natural farm + exact Save/Load. Per-call allocation unsupported remains -1; global UI delta is contextual. No checkpoint batching.");
    }
}
#endif
