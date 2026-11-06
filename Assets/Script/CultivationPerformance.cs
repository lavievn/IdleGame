using System;
using System.Diagnostics;
namespace TuTienCore
{
    // Opt-in main-thread diagnostics. No sampling, clocks or allocation probes while disabled.
    public static class CultivationPerformance
    {
        public enum Part { CheckpointJson, Tick, CharacterUI }
        [Serializable] public sealed class Row
        {public string part;public long calls;public double meanMicroseconds,maxMicroseconds,meanAllocatedBytes;}
        static readonly long[] calls=new long[3],ticks=new long[3],max=new long[3],bytes=new long[3];
        public static bool Enabled {get;private set;}
        public static bool AllocationSupported {get;private set;}
        public static long AllocationControl {get;private set;}
        public static void Start()
        {
            Enabled=false;Array.Clear(calls,0,3);Array.Clear(ticks,0,3);Array.Clear(max,0,3);Array.Clear(bytes,0,3);
            long before=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[65536];probe[0]=1;
            AllocationControl=GC.GetAllocatedBytesForCurrentThread()-before;GC.KeepAlive(probe);AllocationSupported=AllocationControl>=65536;Enabled=true;
        }
        public static Row[] Stop()
        {
            Enabled=false;var rows=new Row[3];for(int i=0;i<3;i++)rows[i]=new Row{part=((Part)i).ToString(),calls=calls[i],meanMicroseconds=calls[i]==0?-1:ticks[i]*1e6/Stopwatch.Frequency/calls[i],maxMicroseconds=max[i]*1e6/Stopwatch.Frequency,meanAllocatedBytes=AllocationSupported&&calls[i]>0?(double)bytes[i]/calls[i]:-1};return rows;
        }
        public static Scope Measure(Part part){return new Scope((int)part,Enabled);}
        public struct Scope:IDisposable
        {
            readonly int part;readonly bool active;readonly long began,allocated;
            internal Scope(int part,bool active){this.part=part;this.active=active;allocated=active&&AllocationSupported?GC.GetAllocatedBytesForCurrentThread():0;began=active?Stopwatch.GetTimestamp():0;
#if UNITY_5_3_OR_NEWER
                if(active)UnityEngine.Profiling.Profiler.BeginSample(part==0?"Cultivation.CheckpointJson":part==1?"Cultivation.Tick":"Cultivation.CharacterUI");
#endif
            }
            public void Dispose(){if(!active)return;
#if UNITY_5_3_OR_NEWER
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                long elapsed=Stopwatch.GetTimestamp()-began;calls[part]++;ticks[part]+=elapsed;if(elapsed>max[part])max[part]=elapsed;if(AllocationSupported)bytes[part]+=GC.GetAllocatedBytesForCurrentThread()-allocated;
            }
        }
    }
}
