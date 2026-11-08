using System;

namespace TuTienCore
{
    [Serializable] public sealed class CultivationBalanceMeasurement
    {
        public string node;public int fps;public double trungSeconds,hauSeconds,vienManSeconds,activeSeconds,processedSeconds,foundation,reserve,accountingError;
        public int checkpoints;public double pendingSeconds;
    }
    // Bounded acceptance helper. Uses the SAME canonical binding, scheduler, domain session and rules as MainGame.
    // Never called by normal gameplay; no approximation formula grants cultivation here.
    public static class CultivationBalanceSimulation
    {
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        public static CultivationBalanceMeasurement Run(CultivationPlaytestProfile profile,EntityDataSO hero,string node,int fps,bool checkpoints=false)
        {
            Require(profile!=null && hero!=null && fps>=2 && fps<=240,"Explicit bounded simulation context required.");
            var m=new CultivationBalanceMeasurement{node=node,fps=fps};hero.worldMapId=node;
            var b=new CultivationGameplayBinding(hero,profile,CultivationGameplayState.New(profile));
            int level=hero.currentLevel;float exp=hero.currentExp;CultivationRealmDefinition realm;profile.Catalog.TryGet(profile.StartingRealm,out realm);
            double duration=3660; // Full path plus one minute of reserve growth at the bottleneck.
            for(int frame=0;frame<(int)(duration*fps);frame++) {
                Require(b.Advance(hero,frame,1d/fps,false,node)==CultivationReason.None,"Simulation clock rejected frame "+frame);
                var s=b.Current;double time=b.ProcessedSeconds;
                if(m.trungSeconds==0 && s.Foundation.Amount>=realm.Thresholds[1])m.trungSeconds=time;
                if(m.hauSeconds==0 && s.Foundation.Amount>=realm.Thresholds[2])m.hauSeconds=time;
                if(m.vienManSeconds==0 && s.Foundation.Amount>=realm.Thresholds[3])m.vienManSeconds=time;
                if(checkpoints && frame%97==96) {
                    CultivationSession restored;string reason;
                    Require(CultivationGameplayState.TryRestore(hero.cultivationJson,profile,out restored,out reason),"Simulation checkpoint: "+reason);
                    b.Dispose();b=new CultivationGameplayBinding(hero,profile,restored);m.checkpoints++;
                }
            }
            m.activeSeconds=b.ActiveSeconds;m.processedSeconds=b.ProcessedSeconds;m.pendingSeconds=b.PendingSeconds;
            m.foundation=b.Current.Foundation.Amount;m.reserve=b.Current.Reserve.Amount;
            double absorbed=profile.Density(node)*profile.Rules.AbsorptionRate*profile.Rules.AbsorptionEfficiency*m.processedSeconds;
            m.accountingError=Math.Abs(profile.InitialFoundation+(absorbed+profile.InitialReserve-m.reserve)*profile.Rules.RefinementEfficiency-m.foundation);
            Require(m.trungSeconds>0 && m.hauSeconds>m.trungSeconds && m.vienManSeconds>m.hauSeconds,"All minor-stage crossings observed.");
            Require(Math.Abs(m.vienManSeconds-3600)<=180,"60-minute acceptance exceeded +/-5%.");
            Require(Math.Abs(m.trungSeconds-900)<.15 && Math.Abs(m.hauSeconds-2100)<.15 && Math.Abs(m.vienManSeconds-3600)<.15,"Expected 15/20/25-minute distribution drifted.");
            Require(m.accountingError<1e-6 && Math.Abs(m.activeSeconds-duration)<1e-6,"Qi/time accounting drift.");
            Require(m.foundation==realm.Thresholds[3] && m.reserve>3600 && b.Current.Foundation.ActualRealmId==profile.StartingRealm && b.Current.Burning==null,"Bottleneck caps foundation, retains reserve and does not break through.");
            Require(hero.currentLevel==level && hero.currentExp==exp,"Qi must not grant Level/EXP.");b.Dispose();return m;
        }
    }
}
