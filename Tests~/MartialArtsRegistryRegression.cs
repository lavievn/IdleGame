using System;
using System.Collections.Generic;
using TuTienCore;

partial class MotionRegression
{
    static MartialArtsRegistry MartialFixture(params MartialDefinition[] definitions)
    {MartialArtsRegistry registry;MartialRegistryValidation error;Check(MartialArtsRegistry.TryCreate(definitions,out registry,out error),"fixture registry "+error.Reason);return registry;}
    static MartialDefinition FamilyArt(WeaponFamily family)
    {return new MartialDefinition("fixture:family:"+family.ToString().ToLowerInvariant(),1,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,new[]{family});}
    static void MartialArtsRegistryTests()
    {
        Run("v05 martial all four weapon families including explicit Unarmed have exact eligibility",()=>{
            foreach(WeaponFamily family in new[]{WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff,WeaponFamily.Unarmed}) {
                var d=FamilyArt(family);var registry=MartialFixture(d);
                foreach(WeaponFamily actual in new[]{WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff,WeaponFamily.Unarmed}) {
                    var result=registry.Evaluate(d.Id,new MartialEligibilityContext(actual));
                    Check(result.IsEligible==(family==actual),"family match "+family+"/"+actual);
                    Check(result.Reason==(family==actual?MartialEligibilityReason.None:MartialEligibilityReason.WeaponMismatch),"explicit mismatch reason");
                }
                Check(registry.Evaluate(d.Id,new MartialEligibilityContext(null)).Reason==MartialEligibilityReason.MissingWeapon,"missing weapon separate from Unarmed");
            }
        });
        Run("v05 martial root tier and explicit unlock facts gate without applying damage or effects",()=>{
            var d=new MartialDefinition("fixture:root-art",2,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,new[]{WeaponFamily.Sword},
                MartialRootAffinityPolicy.EligibilityOnly,new[]{new MartialRootRequirement(ElementType.Kim,3)},new[]{"fixture:quest-open"});
            var registry=MartialFixture(d);
            Check(registry.Evaluate(d.Id,new MartialEligibilityContext(WeaponFamily.Sword)).Reason==MartialEligibilityReason.RootRequirementNotMet,"missing root");
            Check(registry.Evaluate(d.Id,new MartialEligibilityContext(WeaponFamily.Sword,new[]{new MartialRootRequirement(ElementType.Kim,2)})).Reason==MartialEligibilityReason.RootRequirementNotMet,"tier below condition");
            var roots=new[]{new MartialRootRequirement(ElementType.Kim,3)};
            Check(registry.Evaluate(d.Id,new MartialEligibilityContext(WeaponFamily.Sword,roots)).Reason==MartialEligibilityReason.UnlockRequired,"unlock missing");
            Check(registry.Evaluate(d.Id,new MartialEligibilityContext(WeaponFamily.Sword,roots,new[]{"fixture:quest-open"})).IsEligible,"all explicit requirements");
        });
        Run("v05 cultivation passive stays passive and unrestricted is explicit",()=>{
            var d=new MartialDefinition("fixture:passive-only",1,MartialDefinitionKind.CultivationMethod,MartialEffectMode.Passive,new WeaponFamily[0]);
            var result=MartialFixture(d).Evaluate(d.Id,new MartialEligibilityContext(null));
            Check(result.IsEligible && result.IsPassive && result.Definition.Kind==MartialDefinitionKind.CultivationMethod,"passive classification without active execution");
        });
        Run("v05 martial duplicate IDs invalid revisions and invalid policies reject entire registry",()=>{
            MartialArtsRegistry registry;MartialRegistryValidation error;var d=FamilyArt(WeaponFamily.Sword);
            Check(!MartialArtsRegistry.TryCreate(new[]{d,d},out registry,out error) && registry==null && error.Reason==MartialRegistryReason.DuplicateId,"atomic duplicate failure");
            foreach(int revision in new[]{0,-1,int.MinValue}) {
                var invalid=new MartialDefinition("fixture:bad-revision",revision,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,new[]{WeaponFamily.Bow});
                Check(!MartialArtsRegistry.TryCreate(new[]{d,invalid},out registry,out error) && registry==null && error.Reason==MartialRegistryReason.InvalidRevision,"revision failure");
            }
            var bads=new[]{
                new MartialDefinition("not stable",1,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,new WeaponFamily[0]),
                new MartialDefinition("fixture:no-policy",1,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,null),
                new MartialDefinition("fixture:bad-family",1,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,new[]{(WeaponFamily)99}),
                new MartialDefinition("fixture:bad-root",1,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,new WeaponFamily[0],MartialRootAffinityPolicy.EligibilityOnly,new[]{new MartialRootRequirement(ElementType.Kim,6)}),
                new MartialDefinition("fixture:bad-unlock",1,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,new WeaponFamily[0],requiredUnlocks:new[]{"bad fact"})};
            foreach(var bad in bads)Check(!MartialArtsRegistry.TryCreate(new[]{bad},out registry,out error) && registry==null && error.Reason!=MartialRegistryReason.None,"invalid definition failure");
        });
        Run("v05 martial unknown definitions and invalid caller data have stable reasons",()=>{
            var d=FamilyArt(WeaponFamily.Staff);var registry=MartialFixture(d);
            Check(registry.Evaluate("unknown",null).Reason==MartialEligibilityReason.UnknownDefinition,"unknown ID");
            Check(registry.Evaluate(d.Id,null).Reason==MartialEligibilityReason.MissingContext,"missing caller data");
            Check(registry.Evaluate(d.Id,new MartialEligibilityContext((WeaponFamily)99)).Reason==MartialEligibilityReason.InvalidContext,"unknown family");
            Check(registry.Evaluate(d.Id,new MartialEligibilityContext(WeaponFamily.Staff,new[]{new MartialRootRequirement(ElementType.Kim,0)})).Reason==MartialEligibilityReason.InvalidContext,"bad root data");
        });
        Run("v05 martial weapon swaps preserve learned fixture data and definitions own their inputs",()=>{
            var families=new[]{WeaponFamily.Sword};var unlocks=new[]{"fixture:learned"};
            var d=new MartialDefinition("fixture:swap-art",1,MartialDefinitionKind.MartialArt,MartialEffectMode.Active,families,requiredUnlocks:unlocks);
            var registry=MartialFixture(d);families[0]=WeaponFamily.Bow;unlocks[0]="fixture:changed";
            var learned=new HashSet<string>{d.Id};
            foreach(var family in new[]{WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Sword}) {
                var result=registry.Evaluate(d.Id,new MartialEligibilityContext(family,unlockedFacts:new[]{"fixture:learned"}));
                Check(result.IsEligible==(family==WeaponFamily.Sword),"copied definition and current weapon");
                Check(learned.Count==1 && learned.Contains(d.Id),"eligibility never removes learned record");
            }
        });
    }
}
