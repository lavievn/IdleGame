#if UNITY_EDITOR
using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEngine.Scripting;
using TuTienCore;

public static class CultivationV09PerformanceVerifier
{
    [Serializable] sealed class Result
    {
        public string unity,profile,failure="",allocationMethod;
        public double bytesPerAdvance,microsecondsPerAdvance,heapBefore,heapAfter,allocationCounterControl;
        public int advances,gen0Collections;public bool modeRestored;
    }
    public static void Run()
    {
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/cultivation-playtest-"+Guid.NewGuid().ToString("N")));
        var r=new Result{unity=Application.unityVersion,profile=root,advances=256};EntityDataSO hero=null;
        try {
            for(var p=new DirectoryInfo(root);p!=null;p=p.Parent)if(p.Exists && (p.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("No reparse paths");
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".owner"),"cultivation-playtest-v07");CultivationPlaytestBootstrap.VerifyOwnedProfile(root);
            CultivationPlaytestProfile profile;WorldQiDistribution qi;CultivationConfigurationReason why;string detail;
            if(!CultivationProductionConfiguration.TryLoad(out profile,out qi,out why,out detail))throw new InvalidOperationException(why+" "+detail);
            long count=GC.GetAllocatedBytesForCurrentThread();var control=new byte[1024*1024];GC.KeepAlive(control);r.allocationCounterControl=GC.GetAllocatedBytesForCurrentThread()-count;
            // This runtime reports0 for the per-thread counter even for the explicit1MiB allocation,
            // and Editor forbids GC mode changes. Use bounded heap deltas only when no collection occurred.
            hero=ScriptableObject.CreateInstance<EntityDataSO>();hero.worldMapId="TV-20";var b=new CultivationGameplayBinding(hero,profile,CultivationGameplayState.New(profile));
            for(int i=0;i<100;i++)b.Advance(hero,i,1d/60,false,"TV-20");
            GC.Collect();int beforeCollections=GC.CollectionCount(0);var timer=new Stopwatch();r.heapBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            timer.Start();for(int i=100;i<100+r.advances;i++)b.Advance(hero,i,1d/60,false,"TV-20");timer.Stop();
            r.heapAfter=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();r.gen0Collections=GC.CollectionCount(0)-beforeCollections;r.bytesPerAdvance=(r.heapAfter-r.heapBefore)/r.advances;r.microsecondsPerAdvance=timer.Elapsed.TotalMilliseconds*1000/r.advances;
            r.allocationMethod="Estimated managed heap growth /256 Advances with zero collection verified. Editor background allocation can contribute; not an exact per-thread/Player frame allocation measurement. No GC mode changed.";
            if(r.gen0Collections!=0 || r.bytesPerAdvance<=0)throw new InvalidOperationException("Allocation measurement is not valid.");b.Dispose();
        }catch(Exception e){r.failure=e.ToString();}
        finally {r.modeRestored=true;if(hero!=null)UnityEngine.Object.DestroyImmediate(hero);}
        File.WriteAllText(Path.Combine(root,"performance.json"),JsonUtility.ToJson(r,true));UnityEngine.Debug.Log("[CULTIVATION V09 PERF] "+(r.failure==""?"PASS ":"FAIL ")+root+" "+r.failure);EditorApplication.Exit(r.failure==""?0:2);
    }
}
#endif
