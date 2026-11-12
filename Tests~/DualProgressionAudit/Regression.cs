using System;
using System.Linq;
using TuTienCore;

partial class MotionRegression
{
    static void DualProgressionAuditTests()
    {
        Run("all three production profiles validate their actual source data and retain distinct identities",()=>{
            for(int version=1;version<=3;version++)
            {
                var p=AuditProfile(version);CultivationRealmDefinition realm;
                Check(p.IsProduction && p.Version==version && p.Catalog.TryGet(p.StartingRealm,out realm),"validated production profile");
                Check(p.ExplicitSuccessor(p.StartingRealm)=="" && p.Rules.BloodAccumulationRate==0 &&
                    p.Catalog.Evaluate(p.StartingRealm,0).BloodCapacity==0,"current production gates, not multi-realm/Blood acceptance");
                Check((p.Rules.CapacityCatalog!=null)==(version==3),"historical profiles do not inherit new capacities");
            }
        });
        Run("battle EXP and production Qi remain independent at the cultivation bottleneck",()=>{
            var h=HeroAt(1);var p=AuditProfile();
            var s=new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p.Catalog,p.Rules,
                new CultivationSnapshot(new CultivationFoundation(p.StartingRealm,3600),new UnrefinedQiReserve(20),
                    new SpiritualEnergy(0),new BloodEssenceState(0,0),new FoundationInjuryState(0)));
            var before=s.Current;h.AddExp(h.expToNextLevel);
            Check(h.currentLevel==2 && s.Current==before,"current AddExp keeps leveling; it does not implement an EXP bank");
            int level=h.currentLevel,exp=h.currentExp;
            Check(s.Tick(s.OwnerId,s.Epoch,0,.1,false,p.Density("TV-20"))==CultivationReason.None &&
                s.Current.Foundation.Amount==3600 && s.Current.Reserve.Amount>20 && h.currentLevel==level && h.currentExp==exp,
                "only Qi reserve grows at cap");
            Console.WriteLine("GAP EXP is not reserved/released: current AddExp continues Level at realm bottleneck; no EXP reserve exists.");
        });
        Run("World Qi table preserves all map rates and source seed without consuming combat RNG",()=>{
            var p=AuditProfile();var w=auditWorld;
            Check(w.nodes.Length==133 && w.nodes.Select(n=>n.qiPerSecond).Distinct().Count()==8,"all authored rates");
            foreach(var n in w.nodes)
                Check(n.seed==w.seed && Math.Abs(p.EnvironmentalQiPerSecond(n.nodeId)-n.qiPerSecond)<1e-12,"immutable node source");
            UnityEngine.Random.InitState(817);int expected=UnityEngine.Random.Range(0,100000);
            UnityEngine.Random.InitState(817);for(int i=0;i<100;i++)p.Density(w.nodes[i].nodeId);
            Check(UnityEngine.Random.Range(0,100000)==expected,"lookup does not reroll the world");
            Check(p.EnvironmentalQiPerSecond(w.nodes.First(n=>n.qiTier==0).nodeId)!=
                p.EnvironmentalQiPerSecond(w.nodes.First(n=>n.qiTier==7).nodeId),"low and high map sources differ");
        });
        Run("schema3 clock round trip keeps fractional time and retires old callbacks without offline awards",()=>{
            var p=AuditProfile();var h=HeroAt(1);h.worldMapId="TV-20";
            var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));
            Check(b.Advance(h,0,.037,false,h.worldMapId)==CultivationReason.None,"fraction");string checkpoint=h.cultivationJson;
            Check(b.Advance(h,1,.4,true,h.worldMapId)==CultivationReason.Paused && h.cultivationJson==checkpoint,"Pause is not elapsed active time");
            CultivationSession restored;string reason;
            Check(CultivationGameplayState.TryRestore(checkpoint,p,out restored,out reason),"data restore: "+reason);
            var old=b;b.Dispose();b=new CultivationGameplayBinding(h,p,restored);
            Check(b.OwnerId==old.OwnerId && b.Epoch!=old.Epoch && Math.Abs(b.PendingSeconds-.037)<1e-12 &&
                b.Current.Foundation.Amount==0 && b.Current.Reserve.Amount==0,"no credit for restart/offline gap");
            Check(old.Advance(h,2,.1,false,h.worldMapId)==CultivationReason.OwnerMismatch,"stale binding retired");
            Check(b.Advance(h,0,.063,false,h.worldMapId)==CultivationReason.None && Math.Abs(b.Current.Foundation.Amount-.1)<1e-12 &&
                b.Advance(h,0,.1,false,h.worldMapId)==CultivationReason.DuplicateCommand,"one fixed-step award");b.Dispose();
        });
        Run("travel boundary settles original node fraction once before using new supply",()=>{
            var p=AuditProfile();var h=HeroAt(1);h.worldMapId="TV-20";
            var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p));
            b.Advance(h,0,.037,false,h.worldMapId);
            Check(b.SettlePending()==CultivationReason.None && b.SettlePending()==CultivationReason.None &&
                Math.Abs(b.Current.Foundation.Amount-.037)<1e-12,"fraction settled once at start source");
            string hot=auditWorld.nodes.First(n=>n.qiTier==7).nodeId;h.worldMapId=hot;
            Check(b.Advance(h,1,.1,false,hot)==CultivationReason.None &&
                Math.Abs(b.Current.Foundation.Amount+b.Current.Reserve.Amount-.157)<1e-10,"old .037 plus new .12 absorbed, not full5Qi/s");b.Dispose();
        });
        Run("Kim Dan capacity is one charge at entry and increments at every minor stage",()=>{
            var catalog=CultivationCatalog();double[] foundations={0,300,600,1000};
            for(int i=0;i<foundations.Length;i++)
            {
                Check(catalog.Evaluate("fixture:kim-dan",foundations[i]).BloodCapacity==i+1,"explicit entry + minor-stage capacity");
                var s=CultivationFixture(foundations[i],rules:CultivationFixtureRules(refine:0,bloodRate:1));
                Check(s.Tick(s.OwnerId,s.Epoch,0,10,false,0)==CultivationReason.None && s.Current.Blood.Charges==i+1,"passive fill capped");
            }
            var locked=CultivationFixture(realm:"fixture:luyen-khi",rules:CultivationFixtureRules(bloodRate:1));double hp;
            locked.Tick(locked.OwnerId,locked.Epoch,0,10,false,0);var before=locked.Current;
            Check(locked.ActivateBlood(locked.OwnerId,locked.Epoch,Guid.NewGuid(),29,100,out hp)!=CultivationReason.None &&
                locked.Current==before && hp==29,"before unlock no activation/heal");
        });
        Run("production component absence remains legacy while partial future and Blood payloads fail closed",()=>{
            var p=AuditProfile();CultivationSession candidate;string reason;
            Check(CultivationGameplayState.TryRestore("",p,out candidate,out reason) && candidate==null,"absence alone does not initialize from Level/EXP");
            Check(!CultivationGameplayState.TryRestore("{\"version\":99}",p,out candidate,out reason) && candidate==null,"future component rejected");
            Check(!CultivationGameplayState.TryRestore("{\"version\":3}",p,out candidate,out reason) && candidate==null,"partial component rejected");
            var s=CultivationGameplayState.New(p);var dto=CultivationComponentCodec.Capture(s,p.Id);dto.configurationVersion=p.Version;
            Check(dto.bloodCharges==0 && !dto.hasBurn && dto.settledTransactions.Count==0,"production starts without free Blood/debt");
            var payload=auditJson.Deserialize<CultivationGameplayComponentV3>(CultivationGameplayState.Capture(s,p));
            payload.bloodCharges=1;
            Check(!CultivationGameplayState.TryRestore(auditJson.Serialize(payload),p,out candidate,out reason) && candidate==null,
                "gameplay does not silently accept or discard a Blood payload");
        });
        Run("REGRESSION reusable DTO capture clears the previous burn after settlement",()=>{
            var s=CultivationFixture(1000,charges:1);var id=ActivateFixtureBlood(s);
            var target=CultivationComponentCodec.Capture(s,CultivationFixtureConfiguration);
            s.EndBlood(s.OwnerId,s.Epoch,id,BurningEndReason.ExplicitCancel);s.SettleBlood(s.OwnerId,s.Epoch,id);
            CultivationComponentCodec.Capture(s,CultivationFixtureConfiguration,target);
            Check(!target.hasBurn && target.burnId==null && target.burnOwnerId==null && target.burnActualRealmId==null &&
                target.burnReference==0 && target.burnDebtRatio==0 && target.burnChargesUsed==0 && target.burnRemaining==0 &&
                target.burnStatus==0 && target.burnEndReason==-1,"snapshot must not retain a settled transaction as live/hidden liability");
            var slot=new CultivationCandidateSlot(null);
            Check(slot.TryLoad(target,s.Realms,s.Rules,CultivationFixtureConfiguration,s.OwnerId,Guid.NewGuid())==CultivationReason.None &&
                slot.Current.SettleBlood(s.OwnerId,slot.Current.Epoch,id)==CultivationReason.AlreadySettled,"paid penalty cannot recur");
        });
        Run("REGRESSION repeated capture into the same DTO never duplicates settlement receipts",()=>{
            var s=CultivationFixture(1000,charges:1);var id=ActivateFixtureBlood(s);
            s.EndBlood(s.OwnerId,s.Epoch,id,BurningEndReason.Menu);s.SettleBlood(s.OwnerId,s.Epoch,id);
            var target=CultivationComponentCodec.Capture(s,CultivationFixtureConfiguration);
            CultivationComponentCodec.Capture(s,CultivationFixtureConfiguration,target);
            Check(target.settledTransactions.Count==1 && target.settledTransactions[0]==id.ToString("N"),"capture replaces receipt snapshot rather than appending it again");
            target.settledTransactions=null;
            Check(CultivationComponentCodec.Capture(s,CultivationFixtureConfiguration,target).settledTransactions.Count==1,"reused caller DTO may have a null list");
        });
    }
}
