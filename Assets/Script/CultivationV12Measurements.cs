#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
namespace TuTienCore
{
    public static class CultivationV12Measurements
    {
        [Serializable] public sealed class Batch {public string mode;public int repeat,iterations;public bool allocationSupported;public long allocationControl;public CultivationPerformance.Row[] parts;}
        [Serializable] public sealed class Result {public string unity;public List<Batch> batches=new List<Batch>();}
        static void R(bool ok){if(!ok)throw new InvalidOperationException("v1.2 measurement/checkpoint invariant");}
        public static Result Run()
        {
            CultivationPlaytestProfile profile;WorldQiDistribution qi;CultivationConfigurationReason code;string why;
            R(CultivationProductionConfiguration.TryLoadVersion(3,out profile,out qi,out code,out why));
            var result=new Result{unity=Application.unityVersion};
            foreach(string mode in new[]{"tick-only","json-only","binding-exact-checkpoint"})for(int repeat=0;repeat<3;repeat++){
                var hero=ScriptableObject.CreateInstance<EntityDataSO>();hero.worldMapId="TV-20";
                var session=CultivationGameplayState.New(profile);var binding=new CultivationGameplayBinding(hero,profile,session);
                const int warm=256,count=10000;
                try{
                    for(int i=0;i<warm+count;i++){
                        if(i==warm)CultivationPerformance.Start();
                        if(mode=="tick-only")R(session.Tick(session.OwnerId,session.Epoch,i,.1,false,.5)==CultivationReason.None);
                        else if(mode=="json-only")GC.KeepAlive(CultivationGameplayState.Capture(session,profile));
                        else R(binding.Advance(hero,i,1d/144,false,hero.worldMapId)==CultivationReason.None);
                    }
                    var parts=CultivationPerformance.Stop();result.batches.Add(new Batch{mode=mode,repeat=repeat,iterations=count,allocationSupported=CultivationPerformance.AllocationSupported,allocationControl=CultivationPerformance.AllocationControl,parts=parts});
                    if(mode=="binding-exact-checkpoint"){
                        string checkpoint=hero.cultivationJson;CultivationSession restored;R(CultivationGameplayState.TryRestore(checkpoint,profile,out restored,out why));
                        using(var rebound=new CultivationGameplayBinding(hero,profile,restored)){
                            R(hero.cultivationJson==checkpoint&&rebound.ActiveSeconds==binding.ActiveSeconds&&rebound.PendingSeconds==binding.PendingSeconds&&rebound.Epoch!=binding.Epoch);
                            double active=rebound.ActiveSeconds;R(rebound.Advance(hero,0,.01,true,hero.worldMapId)==CultivationReason.Paused&&rebound.ActiveSeconds==active&&hero.cultivationJson==checkpoint);
                        }
                    }
                }finally{CultivationPerformance.Stop();binding.Dispose();UnityEngine.Object.DestroyImmediate(hero);}
            }
            return result;
        }
    }
}
#endif
