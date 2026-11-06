#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
namespace TuTienCore
{
    // Shared assertions run under Mono doubles, native Unity and Windows Player separately.
    public static class HeavenlyTribulationFixtures
    {
        static List<string> checks;
        static void R(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
        static CultivationSession Session(double foundation=10,double injury=0)
        {
            CultivationRealmEvaluator realms;CultivationReason why;
            if(!CultivationRealmEvaluator.TryCreate(new[]{new CultivationRealmDefinition("fixture:a",0,new double[]{0,2,5,10}),new CultivationRealmDefinition("fixture:b",1,new double[]{0,3,6,12})},out realms,out why))throw new Exception("fixture catalog");
            var rules=new CultivationRules(.5,1,1,1,1,100,0,1,0,0,1,0,0,0,BloodCapacityOverflowPolicy.HoldExistingStopAccumulation);
            return new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),realms,rules,new CultivationSnapshot(new CultivationFoundation("fixture:a",foundation),new UnrefinedQiReserve(7),new SpiritualEnergy(9),new BloodEssenceState(0,0),new FoundationInjuryState(injury)));
        }
        static TribulationFixturePolicy Policy()=>new TribulationFixturePolicy("fixture:trial","fixture:b",1,2,10,.5,0);
        static HeavenlyTribulationAttempt Create(CultivationSession session)
        {HeavenlyTribulationAttempt a;R(HeavenlyTribulationAttempt.TryCreateFixture(session,Policy(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),out a)==TribulationReason.None,"fixture eligible actual bottleneck");return a;}
        static TribulationCombatReceipt Receipt(HeavenlyTribulationAttempt a,int damage=10,int hp=20,Guid? eventId=null,Guid? target=null,Guid? life=null,Guid? epoch=null,TribulationPhase? phase=null)
        =>new TribulationCombatReceipt(a.Owner,epoch??a.Epoch,a.Id,eventId??Guid.NewGuid(),life??a.HeroLife,target??a.TargetLife,phase??a.Phase,damage,hp,20);
        static TribulationReason Advance(HeavenlyTribulationAttempt a,long step,double dt=.5,bool paused=false)=>a.Advance(a.Owner,a.Epoch,a.Id,a.HeroLife,step,dt,paused);
        static TribulationReason Complete(HeavenlyTribulationAttempt a)=>a.CompletePhase(a.Owner,a.Epoch,a.Id,a.HeroLife,20,20,false);
        static CultivationReason Commit(HeavenlyTribulationAttempt a)=>a.CommitFixtureSuccess(a.Owner,a.Epoch,a.Id,a.HeroLife,20,20,false);
        static void DamagePhase(HeavenlyTribulationAttempt a){R(Advance(a,0)==TribulationReason.None&&Advance(a,1)==TribulationReason.None&&Complete(a)==TribulationReason.None&&a.Phase==TribulationPhase.Damage,"ordered endurance game-time then damage");}
        public static List<string> Run()
        {
            checks=new List<string>();HeavenlyTribulationAttempt blocked;
            R(!CultivationIntegrationGate.HeavenlyTribulationEnabled&&HeavenlyTribulationAttempt.TryCreateProduction(out blocked)==TribulationReason.Locked&&blocked==null,"production factory locked without balance or Save policy");
            foreach(double f in new[]{0d,2,5,9.99})R(HeavenlyTribulationAttempt.TryCreateFixture(Session(f),Policy(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),out blocked)==TribulationReason.Ineligible,"minor stage cannot bypass actual bottleneck "+f);
            R(HeavenlyTribulationAttempt.TryCreateFixture(Session(10,1),Policy(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),out blocked)==TribulationReason.Ineligible,"injury rejects without mutation");
            R(HeavenlyTribulationAttempt.TryCreateFixture(Session(),null,Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),out blocked)==TribulationReason.MissingPolicy,"missing policy fails closed");
            R(HeavenlyTribulationAttempt.TryCreateFixture(Session(),Policy(),Guid.Empty,Guid.NewGuid(),Guid.NewGuid(),out blocked)==TribulationReason.InvalidIdentity,"empty attempt rejected");
            foreach(string target in new[]{"fixture:a","unknown"})R(HeavenlyTribulationAttempt.TryCreateFixture(Session(),new TribulationFixturePolicy("fixture:bad",target,1,2,10,.5,0),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),out blocked)==TribulationReason.MissingPolicy,"explicit known ascending successor "+target);
            bool invalid=false;try{new TribulationFixturePolicy("production:unapproved","fixture:b",1,2,10,.5,0);}catch(ArgumentException){invalid=true;}R(invalid,"fixture numbers cannot be labeled production");
            foreach(double n in new[]{double.NaN,double.PositiveInfinity,-1,0}){invalid=false;try{new TribulationFixturePolicy("fixture:bad","fixture:b",n,2,10,.5,0);}catch(ArgumentException){invalid=true;}R(invalid,"invalid fixture duration rejects "+n);}
            var s=Session();var before=s.Current;var a=Create(s);
            R(Complete(a)==TribulationReason.NotReady&&Commit(a)==CultivationReason.ProgressNotReady,"no skip to success");
            R(Advance(a,0,.5,true)==TribulationReason.Paused&&a.PhaseSeconds==0,"paused clock does not consume step");
            foreach(double n in new[]{double.NaN,double.PositiveInfinity,-.1,.6})R(Advance(a,0,n)==TribulationReason.InvalidValue&&a.PhaseSeconds==0,"invalid dt atomic "+n);
            R(a.Observe(Receipt(a),true)==TribulationReason.Paused&&a.AcceptedIncoming==0,"paused receipt ignored without mutation");
            R(a.Observe(Receipt(a,epoch:Guid.NewGuid()),false)==TribulationReason.Stale&&a.Observe(Receipt(a,life:Guid.NewGuid()),false)==TribulationReason.Stale,"stale epoch/life ignored");
            R(a.Observe(Receipt(a,target:Guid.NewGuid()),false)==TribulationReason.InvalidIdentity&&a.Observe(Receipt(a,phase:TribulationPhase.Damage),false)==TribulationReason.WrongPhase,"wrong target/phase ignored");
            R(a.Observe(Receipt(a,damage:-1),false)==TribulationReason.InvalidValue&&a.Observe(Receipt(a,hp:21),false)==TribulationReason.InvalidValue,"invalid accepted damage/HP atomic");
            var receipt=Receipt(a,damage:3);R(a.Observe(receipt,false)==TribulationReason.None&&a.Observe(receipt,false)==TribulationReason.Duplicate&&a.AcceptedIncoming==3,"accepted combat event counted once");
            R(Advance(a,0)==TribulationReason.None&&Advance(a,0)==TribulationReason.Duplicate&&a.PhaseSeconds==.5,"duplicate clock step ignored");
            R(Advance(a,1)==TribulationReason.None&&Complete(a)==TribulationReason.None&&a.PhaseSeconds==0,"phase boundary resets phase timer only");
            R(Advance(a,1)==TribulationReason.Duplicate,"step identity spans phases");
            R(Complete(a)==TribulationReason.NotReady&&a.AcceptedOutgoing==0,"incoming damage does not satisfy outgoing threshold");
            R(a.Observe(Receipt(a,damage:9),false)==TribulationReason.None&&Complete(a)==TribulationReason.NotReady,"insufficient accepted outgoing waits");
            R(a.Observe(Receipt(a,damage:1),false)==TribulationReason.None&&Complete(a)==TribulationReason.None&&a.Phase==TribulationPhase.Resolving,"two completed phases only resolve");
            R(s.Current==before,"resolving has no cultivation reward or cost");
            R(a.CommitFixtureSuccess(a.Owner,a.Epoch,a.Id,a.HeroLife,20,20,true)==CultivationReason.Paused&&a.CommitFixtureSuccess(a.Owner,a.Epoch,a.Id,a.HeroLife,0,20,false)==CultivationReason.HpCondition&&s.Current==before,"commit rechecks pause and living HP");
            R(Commit(a)==CultivationReason.None&&a.Phase==TribulationPhase.Committed&&s.Current.Foundation.ActualRealmId=="fixture:b"&&s.Current.Foundation.Amount==0&&s.Current.Reserve.Amount==7&&s.Current.Energy.Current==9,"explicit fixture success transfer preserves resources");
            var after=s.Current;R(Commit(a)==CultivationReason.DuplicateCommand&&s.Current==after,"duplicate success cannot transition twice");
            foreach(bool deathPhase in new[]{false,true}){s=Session();before=s.Current;a=Create(s);if(deathPhase)DamagePhase(a);R(a.Observe(Receipt(a,hp:0),false)==TribulationReason.None&&a.Phase==TribulationPhase.Failed&&Commit(a)!=CultivationReason.None&&s.Current==before,"death terminal no invented penalty phase="+deathPhase);}
            s=Session();before=s.Current;a=Create(s);DamagePhase(a);for(int i=2;i<6;i++)R(Advance(a,i)==TribulationReason.None,"fixture damage deadline clock "+i);R(a.Phase==TribulationPhase.Failed&&a.Observe(Receipt(a),false)==TribulationReason.Terminal&&s.Current==before,"exclusive fixture timeout no late success or penalty");
            foreach(TribulationCancelCause cause in Enum.GetValues(typeof(TribulationCancelCause))){s=Session();before=s.Current;a=Create(s);R(a.Cancel(a.Owner,a.Epoch,a.Id,a.HeroLife,cause)==TribulationReason.None&&a.CancelCause==cause&&Advance(a,0)==TribulationReason.Terminal&&Commit(a)!=CultivationReason.None&&s.Current==before,"cancel invalidates callback without penalty "+cause);}
            s=Session();a=Create(s);var second=Create(s);DamagePhase(a);a.Observe(Receipt(a),false);Complete(a);R(Commit(a)==CultivationReason.None&&Advance(second,0)==TribulationReason.Stale,"competing attempt cannot reuse old realm revision");
            // Existing combat pipeline, explicit fixture entities: mitigation changes accepted damage.
            var attacker=UnityEngine.ScriptableObject.CreateInstance<EntityDataSO>();var defender=UnityEngine.ScriptableObject.CreateInstance<EntityDataSO>();
            try{
                defender.combatStatModifiers=new List<CombatStatModifier>();
                attacker.baseDamage=100;attacker.combatStatModifiers=new List<CombatStatModifier>();
                int raw=CombatDamage.ResolveBasicSnapshot(attacker.GetCalculatedDamage(),0,defender,AttackMode.Melee,new CombatResistanceSettings());
                defender.combatStatModifiers.Add(new CombatStatModifier{sourceId="fixture:armor",stat=CombatStatId.Armor,flat=100});
                int armored=CombatDamage.ResolveBasicSnapshot(attacker.GetCalculatedDamage(),0,defender,AttackMode.Melee,new CombatResistanceSettings());
                R(armored<raw&&armored>0,"existing combat armor changes accepted damage, no trial formula");
                s=Session();a=Create(s);DamagePhase(a);R(a.Observe(Receipt(a,armored),false)==TribulationReason.None&&a.AcceptedOutgoing==armored,"trial accounts actual accepted combat result");
                var actor=new HeroCombatState();actor.BeginLife(attacker,20);var bridge=new TribulationFixtureCombatBridge(a,actor);
                R(bridge.ObserveAccepted(Guid.NewGuid(),a.TargetLife,a.Phase,armored,false)==TribulationReason.None&&a.AcceptedOutgoing==2*armored,"fixture bridge reads actual HeroCombatState HP and accepted result");
                attacker.combatStatModifiers.Add(new CombatStatModifier{sourceId="fixture:attack",stat=CombatStatId.Attack,flat=100});
                int stronger=CombatDamage.ResolveBasicSnapshot(attacker.GetCalculatedDamage(),0,defender,AttackMode.Melee,new CombatResistanceSettings());
                R(stronger>armored,"existing Attack modifier influences trial damage evidence");
                actor.BeginLife(attacker,20);R(!bridge.IsCurrent&&bridge.ObserveAccepted(Guid.NewGuid(),a.TargetLife,a.Phase,stronger,false)==TribulationReason.Stale&&bridge.CommitSuccess(false)==CultivationReason.StaleAttempt,"same actor new life invalidates old bridge");
                actor=new HeroCombatState();actor.BeginLife(attacker,20);s=Session();var death=Create(s);bridge=new TribulationFixtureCombatBridge(death,actor);actor.CurrentHP=0;
                R(bridge.ObserveAccepted(Guid.NewGuid(),death.TargetLife,death.Phase,20,false)==TribulationReason.None&&death.Phase==TribulationPhase.Failed,"bridge observes actual HP death without mutating HP");
                R(s.Current.Foundation.ActualRealmId=="fixture:a"&&s.Current.Blood.Charges==0&&s.Current.Burning==null,"combat evidence cannot auto-grant realm or Blood");
            }finally{UnityEngine.Object.DestroyImmediate(attacker);UnityEngine.Object.DestroyImmediate(defender);}
            return checks;
        }
    }
}
#endif
