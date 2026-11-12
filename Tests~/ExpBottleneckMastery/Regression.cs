using System;
using System.Numerics;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static ExperienceSnapshot Xp(EntityDataSO h)
    {Check(ExperienceProgress.TryRead(h.experienceJson,out var xp),"valid EXP component");return xp;}
    static BigInteger Bank(EntityDataSO h) => ExperienceProgress.Amount(Xp(h));
    static EntityDataSO BankHero(out CultivationSession s,out CultivationGameplayBinding b,double foundation=3600,int level=1)
    {
        var p=HeavenlyGateV13Fixtures.Profile();s=HeavenlyGateV13Fixtures.Session(p,foundation);
        var h=HeroAt(level);b=new CultivationGameplayBinding(h,p,s,HeavenlyGateV13Fixtures.Configuration());return h;
    }
    static CultivationReason Release(EntityDataSO h,CultivationSession s,Guid id,long revision=0) =>
        ExperienceProgress.ApplyBreakthroughSuccess(h,s,HeavenlyGateV13Fixtures.Profile(),HeavenlyGateV13Fixtures.Configuration(),id,revision);
    static void EquipForMastery(EntityDataSO h,string template)
    {
        EquipmentRuntime.InitializeNewRun(h);
        var item=EquipmentFoundation.CreateSkillSample(template) ?? EquipmentFoundation.CreateCommon(template,1,10);
        Check(EquipmentRuntime.AddInstance(h,item) && EquipmentRuntime.Equip(h,item.instanceId,EquipmentSlot.Weapon),"equip fixture");
    }
    static MasteryExecutionEvidence Evidence(MasteryExecutionReceipts r,long n,WeaponFamily family,string skill=null) =>
        new MasteryExecutionEvidence(r.Epoch,n,1,1,0,skill==null?MasteryExecutionKind.BasicWeapon:MasteryExecutionKind.Skill,family,skill);

    static partial void ExpMasteryTests()
    {
        Run("EXP normal awards preserve existing Level points and seeded growth",()=>{
            var h=HeroAt(1);UnityEngine.Random.InitState(325);int expected=UnityEngine.Random.Range(50,101);
            UnityEngine.Random.InitState(325);Check(h.AddExp(100) && h.currentLevel==2 && h.currentExp==0 &&
                h.statPoints==1 && h.meleeGrowthBonus==expected && h.meleeGrowthThroughLevel==2 &&
                h.baseHealth==416 && h.baseDamage==11 && h.expToNextLevel==CombatBalance.RequiredExp(2) && Bank(h)==0,"existing authority");
        });
        Run("REGRESSION actual eligible bottleneck banks new EXP without moving existing EXP or Qi",()=>{
            var h=BankHero(out var s,out var b);h.currentExp=37;var before=s.Current;
            Check(b.ShouldBankExperience(h),"actual catalog cap and configured ascending trial");
            Check(!h.AddExp(100) && h.currentLevel==1 && h.currentExp==37 && Bank(h)==100 &&
                s.Current==before && s.Current.Reserve.Amount==1234 && h.statPoints==0 && h.meleeGrowthBonus==0,"independent bank");
            b.Dispose();
        });
        Run("EXP bank uses actual state at any Level and ignores temporary injury readiness",()=>{
            var p=HeavenlyGateV13Fixtures.Profile();var h=HeroAt(82);
            var s=HeavenlyGateV13Fixtures.Session(p,injury:p.Rules.MaximumInjuryForTrial+1);
            using(var b=new CultivationGameplayBinding(h,p,s,HeavenlyGateV13Fixtures.Configuration()))
            {h.AddExp(150);Check(h.currentLevel==82 && Bank(h)==150,"no Level 45 or injury-based escape");}
        });
        Run("EXP terminal production and final configured realm keep Level advancing",()=>{
            var p=AuditProfile();var h=HeroAt(1);
            var s=new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p.Catalog,p.Rules,
                new CultivationSnapshot(new CultivationFoundation(p.StartingRealm,3600),new UnrefinedQiReserve(0),new SpiritualEnergy(0),new BloodEssenceState(0,0),new FoundationInjuryState(0)));
            using(var b=new CultivationGameplayBinding(h,p,s)){h.AddExp(100);Check(h.currentLevel==2 && Bank(h)==0,"production terminal");}
            p=HeavenlyGateV13Fixtures.Profile();h=HeroAt(1);
            s=new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p.Catalog,p.Rules,
                new CultivationSnapshot(new CultivationFoundation("fixture:truc-co",4800),new UnrefinedQiReserve(0),new SpiritualEnergy(0),new BloodEssenceState(0,0),new FoundationInjuryState(0)));
            using(var b=new CultivationGameplayBinding(h,p,s,HeavenlyGateV13Fixtures.Configuration()))
            {h.AddExp(100);Check(h.currentLevel==2 && Bank(h)==0,"final DEV-style realm, no successor");}
        });
        Run("EXP a successor without a matching valid executable trial does not bank",()=>{
            foreach(int variant in new[]{0,1,2,3})
            {
                var h=BankHero(out var s,out var old);var p=old.Profile;old.Dispose();
                var c=HeavenlyGateV13Fixtures.Configuration();if(variant==0)c=null;
                if(variant==1)c.targetRealm="missing:realm";if(variant==2)c.cultivationConfiguration="other";if(variant==3)c.hp=0;
                using(var b=new CultivationGameplayBinding(h,p,s,c)){h.AddExp(100);Check(h.currentLevel==2 && Bank(h)==0,"invalid path "+variant);}
            }
        });
        Run("EXP entering a minor stage neither banks early nor releases a prior bank",()=>{
            var h=BankHero(out var s,out var b,899);h.experienceJson=ExperienceProgress.Capture(new ExperienceSnapshot{reserve="400"});
            Check(b.Advance(h,0,.5,false,h.worldMapId)==CultivationReason.None,"real Qi tick");
            h.AddExp(100);Check(h.currentLevel==2 && Bank(h)==400 && s.Current.Foundation.ActualRealmId==b.Profile.StartingRealm,"minor stage is no major success");b.Dispose();
        });
        Run("EXP accepted major success releases multiple levels with exact points growth and remainder",()=>{
            var h=BankHero(out var s,out var b);h.AddExp(400);
            UnityEngine.Random.InitState(70);int growth=UnityEngine.Random.Range(50,101)+UnityEngine.Random.Range(50,101)+UnityEngine.Random.Range(50,101);
            UnityEngine.Random.InitState(70);Check(Release(h,s,Guid.NewGuid())==CultivationReason.None,"accepted transition");
            Check(h.currentLevel==4 && h.currentExp==400-CombatBalance.RequiredExp(1)-CombatBalance.RequiredExp(2)-CombatBalance.RequiredExp(3) &&
                h.statPoints==3 && h.meleeGrowthBonus==growth && h.meleeGrowthThroughLevel==4 && Bank(h)==0 &&
                s.Current.Reserve.Amount==1234 && Xp(h).lastReleaseRealm=="fixture:truc-co","single existing leveling loop");b.Dispose();
        });
        Run("EXP failure keeps bank and below-cap awards never release it",()=>{
            var h=BankHero(out var s,out var b);h.AddExp(300);
            Check(s.ApplyGatePenalty(s.OwnerId,s.Epoch,Guid.NewGuid(),0,b.Profile.StartingRealm,HeavenlyGateV13Fixtures.Penalty())==CultivationReason.None,"failure");
            h.AddExp(100);Check(h.currentLevel==2 && h.currentExp==0 && Bank(h)==300,"old bank intact; new outside-cap EXP normal");b.Dispose();
        });
        Run("EXP duplicate success and stale restored source cannot spend a bank again",()=>{
            var h=BankHero(out var s,out var b);h.AddExp(500);string source=h.cultivationJson;var id=Guid.NewGuid();
            Check(Release(h,s,id)==CultivationReason.None,"first release");int points=h.statPoints,growth=h.meleeGrowthBonus,level=h.currentLevel;
            Check(Release(h,s,id)==CultivationReason.DuplicateCommand,"same receipt");
            Check(CultivationGameplayState.TryRestore(source,b.Profile,out var stale,out var why),"old source restore "+why);
            Check(Release(h,stale,Guid.NewGuid())==CultivationReason.StaleAttempt && h.currentLevel==level && h.statPoints==points && h.meleeGrowthBonus==growth && Bank(h)==0,"stale source rejected without reroll");b.Dispose();
        });
        Run("EXP wrong revision early success and invalid component leave both authorities unchanged",()=>{
            foreach(int variant in new[]{0,1,2,3,4})
            {
                var h=BankHero(out var s,out var b,variant==1?3599:3600);h.experienceJson=ExperienceProgress.Capture(new ExperienceSnapshot{reserve="500"});
                if(variant==2)h.experienceJson="{\"version\":99}";
                if(variant==3)h.statPoints=int.MaxValue;
                if(variant==4)h.meleeGrowthBonus=int.MaxValue;
                int points=h.statPoints,growth=h.meleeGrowthBonus;
                string json=h.experienceJson,cult=h.cultivationJson;var before=s.Current;
                Check(Release(h,s,Guid.NewGuid(),variant==0?9:0)!=CultivationReason.None && s.Current==before &&
                    h.experienceJson==json && h.cultivationJson==cult && h.currentLevel==1 && h.statPoints==points && h.meleeGrowthBonus==growth,"atomic rejection "+variant);b.Dispose();
            }
        });
        Run("REGRESSION Level 999 conserves overflow and arbitrarily large banks exactly",()=>{
            var h=HeroAt(999);h.currentExp=h.expToNextLevel-1;h.AddExp(int.MaxValue);h.AddExp(int.MaxValue);
            Check(Bank(h)==(BigInteger)int.MaxValue*2 && h.currentLevel==999 && h.statPoints==998,"cap retains all earned EXP");
            h=BankHero(out var s,out var b,level:999);string huge="9999999999999999999999999999999999999999";
            h.experienceJson=ExperienceProgress.Capture(new ExperienceSnapshot{reserve=huge});h.AddExp(int.MaxValue);
            var earned=BigInteger.Parse(huge)+int.MaxValue;int growth=h.meleeGrowthBonus,points=h.statPoints;
            Check(Bank(h)==earned && Release(h,s,Guid.NewGuid())==CultivationReason.None &&
                Bank(h)==earned-(h.expToNextLevel-1) && h.currentExp==h.expToNextLevel-1 && h.statPoints==points && h.meleeGrowthBonus==growth,"cap release neither destroys EXP nor repeats growth");b.Dispose();
        });
        Run("EXP cross-Level-999 release awards final level once and retains remainder",()=>{
            var h=BankHero(out var s,out var b,level:998);h.AddExp(int.MaxValue);int points=h.statPoints;
            Check(Release(h,s,Guid.NewGuid())==CultivationReason.None && h.currentLevel==999 && h.statPoints==points+1 &&
                (BigInteger)h.currentExp+Bank(h)==(BigInteger)int.MaxValue-CombatBalance.RequiredExp(998),"cap conservation");b.Dispose();
        });
        Run("EXP data component roundtrip retains bank and persisted release receipt",()=>{
            var h=BankHero(out var s,out var b);h.AddExp(300);string root="{\"experienceJson\":"+auditJson.Serialize(h.experienceJson)+"}";
            Check(ExperienceProgress.TryReadHeroJson(root,out var saved),"root preflight");var loaded=HeroAt(1);loaded.experienceJson=saved;
            Check(Bank(loaded)==300,"component roundtrip only, not SaveManager");
            var id=Guid.NewGuid();Release(h,s,id);root="{\"experienceJson\":"+auditJson.Serialize(h.experienceJson)+"}";
            Check(ExperienceProgress.TryReadHeroJson(root,out saved) && ExperienceProgress.TryRead(saved,out var xp) && xp.lastReleaseAttempt==id.ToString("N") && xp.lastReleaseRealm=="fixture:truc-co","durable dedup identity");b.Dispose();
        });
        Run("EXP legacy absence gives zero bank and never moves existing EXP",()=>{
            Check(ExperienceProgress.TryReadHeroJson("{\"currentLevel\":9,\"currentExp\":17}",out var component) && component=="","legacy boundary");
            var h=HeroAt(9);h.currentExp=17;h.experienceJson=component;Check(Bank(h)==0 && h.currentExp==17 && h.currentLevel==9,"no migration of EXP");
        });
        Run("EXP future malformed negative partial and typed payloads fail closed",()=>{
            foreach(string component in new[]{null,"{\"version\":99}","{\"version\":1}",
                "{\"version\":1,\"reserve\":\"-1\",\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\"}",
                "{\"version\":1,\"reserve\":42,\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\"}",
                "{\"version\":1,\"reserve\":\"01\",\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\"}",
                "{\"version\":1,\"reserve\":\"1e4\",\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\"}",
                "{\"version\":1,\"reserve\":\"\",\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\"}",
                "{\"version\":1,\"reserve\":null,\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\"}",
                "{\"version\":1,\"reserve\":\"0\",\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\",\"future\":1}",
                "{\"version\":1,\"version\":1,\"reserve\":\"0\",\"lastReleaseAttempt\":\"\",\"lastReleaseRealm\":\"\"}",
                "{\"version\":1,\"reserve\":\"0\",\"lastReleaseAttempt\":\"bad\",\"lastReleaseRealm\":\"x\"}"})
            {
                var h=HeroAt(1);h.experienceJson=component;Check(!ExperienceProgress.TryRead(component,out _),"component refused");
                Check(!ExperienceProgress.TryReadHeroJson("{\"experienceJson\":"+auditJson.Serialize(component)+"}",out _),"pre-mutation root guard");
                h.AddExp(100);Check(h.currentLevel==1 && h.statPoints==0 && h.currentExp==0 && h.experienceJson==component,"no half award");
            }
            Check(!ExperienceProgress.TryReadHeroJson("{\"experienceJson\":null}",out _),"null is not legacy absence");
        });
        Run("EXP data lifecycle dispose rebind travel pause and repeated restore award nothing",()=>{
            var h=BankHero(out var s,out var b);h.AddExp(400);string json=h.experienceJson;
            Check(b.Advance(h,0,.1,true,h.worldMapId)==CultivationReason.Paused,"pause");b.Dispose();
            for(int i=0;i<3;i++)
            {
                Check(CultivationGameplayState.TryRestore(h.cultivationJson,HeavenlyGateV13Fixtures.Profile(),out s,out _),"restore source");
                using(var next=new CultivationGameplayBinding(h,HeavenlyGateV13Fixtures.Profile(),s,HeavenlyGateV13Fixtures.Configuration()))
                {Check(next.SettlePending()==CultivationReason.None,"travel settlement");Check(h.experienceJson==json && h.currentLevel==1 && h.statPoints==0,"lifecycle does not award");}
            }
            Check(h.experienceBinding==null && h.experienceJson==json,"retired gate reference");
        });
        Run("Mastery stable weapon IDs follow equipped family and separate weapon records",()=>{
            var h=HeroAt(1);string[] templates={EquipmentFoundation.DemoSword,EquipmentFoundation.DemoBow,EquipmentFoundation.DemoStaff};
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var template in templates){EquipForMastery(h,template);ids.Add(MasteryFoundation.WeaponId(WeaponBasicAttack.Family(WeaponBasicAttack.Equipped(h))));}
            Check(ids.Count==3 && ids.Contains("weapon-family:sword") && ids.Contains("weapon-family:bow") && ids.Contains("weapon-family:staff"),"family keys independent from instance and stance");
            Check(MasteryFoundation.WeaponId((WeaponFamily)99)==null,"unknown family");
        });
        Run("Mastery data contract serializes stable skill IDs independently of cooldown and Level",()=>{
            var snapshot=new MasterySnapshot();snapshot.skills.Add(new MasteryRecord{id=WeaponSkillRegistry.Thrust,units=7});
            snapshot.weapons.Add(new MasteryRecord{id="weapon-family:sword",units=3});
            var loaded=auditJson.Deserialize<MasterySnapshot>(auditJson.Serialize(snapshot));
            Check(MasteryFoundation.IsValid(loaded) && loaded.skills[0].id==WeaponSkillRegistry.Thrust && loaded.skills[0].units==7 && loaded.weapons[0].units==3,".NET DTO roundtrip only; no player Save installation");
            loaded.version=99;Check(!MasteryFoundation.IsValid(loaded),"future");loaded.version=1;
            loaded.skills.Add(new MasteryRecord{id=WeaponSkillRegistry.Thrust,units=1});Check(!MasteryFoundation.IsValid(loaded),"duplicate");
        });
        Run("Mastery valid equipment-granted execution evidence works at bottleneck without awarding progression",()=>{
            var h=BankHero(out var s,out var b);EquipForMastery(h,EquipmentFoundation.SkillSampleIds[0]);
            var r=new MasteryExecutionReceipts(h,Guid.NewGuid(),1,1,0);string xp=h.experienceJson;
            Check(r.TryAccept(h,Evidence(r,1,WeaponFamily.Sword),true) && r.TryAccept(h,Evidence(r,2,WeaponFamily.Sword,WeaponSkillRegistry.Thrust),true),"basic and equipment-granted skill execution");
            Check(h.experienceJson==xp && h.currentLevel==1 && h.statPoints==0 && h.combatSkills.cooldowns.Count==0,"no mastery award, Level or cooldown side effect");b.Dispose();
        });
        Run("Mastery duplicate callback stale owner epoch life spawn and battle are rejected",()=>{
            var h=HeroAt(1);var r=new MasteryExecutionReceipts(h,Guid.NewGuid(),1,1,0);var e=Evidence(r,1,WeaponFamily.Unarmed);
            Check(r.TryAccept(h,e,true) && !r.TryAccept(h,e,true),"same execution once");
            Check(!r.TryAccept(HeroAt(1),Evidence(r,2,WeaponFamily.Unarmed),true),"owner");
            foreach(int v in new[]{0,1,2,3})
            {
                var stale=new MasteryExecutionEvidence(v==0?Guid.NewGuid():r.Epoch,2,v==1?2:1,v==2?2:1,v==3?1:0,MasteryExecutionKind.BasicWeapon,WeaponFamily.Unarmed);
                Check(!r.TryAccept(h,stale,true),"retired context "+v);
            }
            Check(r.TryAccept(h,Evidence(r,2,WeaponFamily.Unarmed),true),"invalid callbacks did not consume sequence");
        });
        Run("Mastery failed or unavailable execution selection and post-swap evidence never qualify",()=>{
            var h=HeroAt(1);EquipForMastery(h,EquipmentFoundation.DemoSword);var r=new MasteryExecutionReceipts(h,Guid.NewGuid(),1,1,0);
            var basic=Evidence(r,1,WeaponFamily.Sword);
            Check(!r.TryAccept(h,basic,false),"uncommitted action");
            Check(!r.TryAccept(h,Evidence(r,1,WeaponFamily.Sword,WeaponSkillRegistry.Thrust),true),"known or learned ID alone cannot grant equipment skill");
            EquipForMastery(h,EquipmentFoundation.SkillSampleIds[0]);var cast=Evidence(r,1,WeaponFamily.Sword,WeaponSkillRegistry.Thrust);
            EquipForMastery(h,EquipmentFoundation.DemoBow);
            Check(!r.TryAccept(h,cast,true) && !r.TryAccept(h,basic,true),"swap cancels captured identity");
            Check(r.TryAccept(h,Evidence(r,1,WeaponFamily.Bow),true),"new family execution only");
        });
    }
}
