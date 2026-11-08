using System;
using System.Collections.Generic;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static void FoundationCrossContractTests()
    {
        Run("v05 foundations consume snapshots while existing Hero roots EXP equipment and world remain unchanged",()=>{
            var hero=HeroAt(45);hero.currentExp=73;EquipmentInventory.InitializeNewRun(hero);EquipmentRuntime.InitializeNewRun(hero);
            hero.spiritRoots=new List<ElementType>{ElementType.Kim};hero.rootTiers=new List<int>{3};hero.rootWeights=new List<float>{1};
            var roots=hero.spiritRoots;var tiers=hero.rootTiers;var weights=hero.rootWeights;var mods=hero.combatStatModifiers;
            string before=JsonUtility.ToJson(hero,true);int physical=WeaponBasicAttack.Physical(hero),magical=WeaponBasicAttack.Magical(hero);
            var evaluator=RealmFixtureEvaluator();var evaluated=evaluator.Evaluate("fixture:luyen-khi",1,hero.currentLevel);
            Check(evaluated.InternalLevel==45 && evaluated.AtBottleneck,"raw progress no automatic realm");
            var transition=new LegacyLevelRealmBreakthroughTransition(evaluator,new LegacyLevelRealmProgressionState(evaluated.RealmId,1,hero.currentLevel,hero.currentExp));
            RealmTransitionReason realmReason;Check(transition.TryApplySuccess(Guid.NewGuid(),0,"fixture:truc-co",out realmReason),"separate explicit transition");
            var art=new MartialDefinition("fixture:cross-passive",1,MartialDefinitionKind.MartialArt,MartialEffectMode.Passive,new[]{WeaponFamily.Unarmed},
                MartialRootAffinityPolicy.EligibilityOnly,new[]{new MartialRootRequirement(ElementType.Kim,3)});
            var context=new MartialEligibilityContext(WeaponBasicAttack.Family(WeaponBasicAttack.Equipped(hero)),new[]{new MartialRootRequirement(hero.spiritRoots[0],hero.rootTiers[0])});
            Check(MartialFixture(art).Evaluate(art.Id,context).IsPassive,"martial passive remains classification only");
            var gate=BossFixtureGate();string worldBefore=WorldCatalogSnapshot();var boss=new BossEncounterCoordinator();var e=Guid.NewGuid();var a=Guid.NewGuid();BossCoordinatorReason bossReason;
            Check(boss.TryEnter(e,a,BossFixturePermit(gate),out bossReason) && boss.TryStartFighting(e,a,out bossReason) &&
                boss.TryResolve(e,a,BossEncounterOutcome.Won,out bossReason) && boss.TryCompleteResolution(e,a,true,out bossReason),"independent logical boss win");
            Check(JsonUtility.ToJson(hero,true)==before && hero.spiritRoots==roots && hero.rootTiers==tiers && hero.rootWeights==weights && hero.combatStatModifiers==mods,"canonical data and owned lists unchanged");
            Check(WeaponBasicAttack.Physical(hero)==physical && WeaponBasicAttack.Magical(hero)==magical,"no stat/effect injection");
            Check(WorldCatalogSnapshot()==worldBefore,"no world topology/clear change");
        });
    }
}
