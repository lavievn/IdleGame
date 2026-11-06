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
    IEnumerator PerformanceV12()
    {
        File.WriteAllText(Path.Combine(Evidence,"isolated-measurements.json"),JsonUtility.ToJson(CultivationV12Measurements.Run(),true));
        gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();yield return Resize(800,450);
        // Both windows use the same paused canonical state: UI cost comparison has no combat/progression confound.
        foreach(bool open in new[]{false,true,false,true}){
            if(open)ui.OpenHeroStats();else ui.CloseDamageInfo();yield return new WaitForSecondsRealtime(1);
            allocation=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");Require(allocation.Valid,"Global allocation recorder supported");
            var window=new V12Window{mode=open?"character-open-paused":"character-closed-paused"};int gen=GC.CollectionCount(0);long total=0;float begin=Time.realtimeSinceStartup;
            CultivationPerformance.Start();while(Time.realtimeSinceStartup-begin<8){yield return new WaitForEndOfFrame();window.frames++;total+=allocation.LastValue;}
            window.parts=CultivationPerformance.Stop();window.seconds=Time.realtimeSinceStartup-begin;window.gen0=GC.CollectionCount(0)-gen;window.globalBytesPerFrame=(double)total/window.frames;window.allocationSupported=CultivationPerformance.AllocationSupported;window.allocationControl=CultivationPerformance.AllocationControl;allocation.Dispose();
            File.WriteAllText(Path.Combine(Evidence,"ui-window-"+(open?"open":"closed")+"-"+window.frames+".json"),JsonUtility.ToJson(window,true));
        }
        ui.CloseDamageInfo();yield return ProfileFarm();
        Require(gm.TryManualSave(SaveSlot.ManualSave2),"Measured checkpoint Save");string exact=gm.HeroData.cultivationJson;Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Measured checkpoint Load");yield return Ready();Require(gm.HeroData.cultivationJson==exact,"Exact measured Save/Load");
        Pass("V12 A isolated Tick/JSON/binding three warmed repeats + real Character open/closed paused windows + unmodified natural farm + exact Save/Load. Per-call allocation unsupported remains -1; global UI delta is contextual. No checkpoint batching.");
    }
}
#endif
