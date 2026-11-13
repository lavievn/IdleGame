using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using TuTienCore;

partial class MotionRegression
{
    // Driver follows the existing CultivationV11CapacityVerifier's bounded
    // Advance/LastStepFlow measurement. No rate or progression formula replaces
    // the production binding/session/capacity implementation.
    sealed class AuditMeasurement
    {
        public int version,fps,checkpoints;public string node;
        public double qi,trung,hau,vien,f60,r60,f61,r61,absorbed,error,active,processed,pending,wall;
    }
    static AuditMeasurement MeasureCultivation(int version,string node,int fps,bool checkpoints)
    {
        var p=AuditProfile(version);var hero=HeroAt(1);hero.worldMapId=node;
        var b=new CultivationGameplayBinding(hero,p,CultivationGameplayState.New(p));
        var m=new AuditMeasurement {version=version,node=node,fps=fps,qi=p.EnvironmentalQiPerSecond(node)};
        var stopwatch=Stopwatch.StartNew();
        try
        {
            for(int frame=0;frame<3660*fps;frame++)
            {
                long ticks=b.TickCount;
                Check(b.Advance(hero,frame,1d/fps,false,node)==CultivationReason.None,"simulation frame accepted");
                if(b.TickCount!=ticks)
                {
                    Check(b.TickCount==ticks+1,"selected FPS processes at most one fixed step");
                    var flow=b.LastStepFlow;m.absorbed+=flow.Absorbed;
                    Check(flow.AbsorptionPerSecond<=Math.Min(m.qi,flow.AbsorptionCapacity)+1e-8 &&
                        flow.RefinementPerSecond<=flow.RefinementCapacity+1e-8,"actual production capacity bounds");
                }
                double foundation=b.Current.Foundation.Amount,time=b.ProcessedSeconds;
                if(m.trung==0 && foundation>=900)m.trung=time;
                if(m.hau==0 && foundation>=2100)m.hau=time;
                if(m.vien==0 && foundation>=3600)m.vien=time;
                if(frame==3600*fps-1){m.f60=foundation;m.r60=b.Current.Reserve.Amount;}
                if(checkpoints && frame>0 && frame%997==996)
                {
                    CultivationSession restored;string why;
                    Check(CultivationGameplayState.TryRestore(hero.cultivationJson,p,out restored,out why),"in-memory checkpoint: "+why);
                    var old=b;b.Dispose();b=new CultivationGameplayBinding(hero,p,restored);m.checkpoints++;
                    Check(old.OwnerId==b.OwnerId && old.Epoch!=b.Epoch,"checkpoint keeps identity and replaces epoch");
                }
            }
            m.f61=b.Current.Foundation.Amount;m.r61=b.Current.Reserve.Amount;
            m.error=Math.Abs(m.f61+m.r61-m.absorbed);m.active=b.ActiveSeconds;m.processed=b.ProcessedSeconds;m.pending=b.PendingSeconds;
            Check(m.error<1e-6 && Math.Abs(m.active-3660)<1e-6,"Qi and active-time conservation");
            Check(hero.currentLevel==1 && hero.currentExp==0 && b.Current.Foundation.ActualRealmId==p.StartingRealm &&
                b.Current.Blood.Charges==0 && b.Current.Burning==null,"no invented EXP/realm/Blood progress");
            return m;
        }
        finally {stopwatch.Stop();m.wall=stopwatch.Elapsed.TotalSeconds;b.Dispose();}
    }
    static string N(double number) {return number.ToString("G17",CultureInfo.InvariantCulture);}
    static void AuditSimulations(string output)
    {
        AuditProfile();var rows=new List<AuditMeasurement>();
        foreach(var rate in auditWorld.tiers.OrderBy(t=>t.tier))
            rows.Add(MeasureCultivation(3,auditWorld.nodes.First(n=>n.qiPerSecond==rate.qiPerSecond).nodeId,10,false));
        foreach(string node in new[]{"TV-20","TL-16","XD-11","TM-12","HT-17"})
            rows.Add(MeasureCultivation(3,node,10,true));
        rows.Add(MeasureCultivation(3,"TV-20",30,true));
        rows.Add(MeasureCultivation(3,auditWorld.nodes.First(n=>n.qiTier==7).nodeId,30,true));
        rows.Add(MeasureCultivation(1,"TV-20",10,true));
        rows.Add(MeasureCultivation(2,"TV-20",10,true));
        var lines=new List<string>{"configuration_version,node,environment_qi_per_second,fps,checkpoints,trung_active_seconds,hau_active_seconds,vien_man_active_seconds,foundation_at_3600,reserve_at_3600,foundation_at_3660,reserve_at_3660,absorbed_3660,accounting_error,active_seconds,processed_seconds,pending_seconds,execution_wall_seconds"};
        foreach(var m in rows)
        {
            lines.Add(string.Join(",",new[]{m.version.ToString(),m.node,N(m.qi),m.fps.ToString(),m.checkpoints.ToString(),
                N(m.trung),N(m.hau),N(m.vien),N(m.f60),N(m.r60),N(m.f61),N(m.r61),N(m.absorbed),N(m.error),N(m.active),N(m.processed),N(m.pending),N(m.wall)}));
            Console.WriteLine("SIM config="+m.version+" node="+m.node+" qi="+N(m.qi)+" fps="+m.fps+
                " vien_man_active_seconds="+N(m.vien)+" foundation_at_3600="+N(m.f60)+" error="+N(m.error));
        }
        if(output!=null)File.WriteAllLines(output,lines);
        foreach(var m in rows.Where(r=>r.version==3 && r.qi==1))Check(Math.Abs(m.vien-3600)<=.15,"starting map target within one configured fixed step");
        Check(rows.Count==17,"all requested bounded measurements produced");
        Console.WriteLine("SIMULATION PASS 17 measurements; accelerated active-gameplay seconds, no wall-clock/offline or Weather time conversion.");
    }
}
