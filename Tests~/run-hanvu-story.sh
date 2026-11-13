#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
# C# 7.2+ compiler; CSC can point to a Roslyn/Mono wrapper. No Editor or cache.
"${CSC:-mcs}" -langversion:7.2 -r:System.Numerics.dll -define:HANVU_STORY_TESTS -out:"$build_dir/HanVuStory.exe" \
  Tests~/{HanVuStoryRunner,UnityStub,NestedEquipmentSaveStub,StoryOriginRegression,WorldMapRegression}.cs \
  Assets/Script/{HanVuOrigin,HanVuNarrative,HanVuEncounters,TuTienEnums,WorldMapCatalog,CombatBalance,CombatStats,CombatDamage,WeaponBasicAttack,WeaponSkillRegistry,CombatSkillProgress,EquipmentFoundation,EquipmentRuntime,EquipmentInventory,EquipmentRefinement,EquipmentStatPreview,RealmProgressionAuthority,RealmProgression,CultivationIntegrationGate,MartialArtsRegistry,CultivationSaveJson}.cs \
  Assets/Script/{WorldClimate,ExperienceProgress,WorldQiDistribution,CultivationModel,CultivationEngine,CultivationCapacity,CultivationBreakthrough,CultivationPersistence,CultivationGameplayState,CultivationPlaytestProfile,CultivationProductionConfiguration,CultivationProductionConfiguration.Versions,CultivationConfigurationUpgrade,CultivationPerformance,BloodEssenceTransactions,FoundationRecovery,HeavenlyGateModel,HeavenlyGatePenalty,HeavenlyTribulation}.cs \
  Assets/DataScript/EntityDataSO.cs
"${MONO:-mono}" "$build_dir/HanVuStory.exe"
