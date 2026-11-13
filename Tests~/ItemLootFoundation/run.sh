#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
"${CSC:-mcs}" -langversion:7.2 -r:System.Numerics.dll -define:ITEM_LOOT_FOUNDATION_TESTS -out:"$build_dir/ItemLootFoundation.exe" \
  Tests~/UnityStub.cs Tests~/NestedEquipmentSaveStub.cs Tests~/ItemLootFoundation/{Boundary,Regression}.cs \
  Assets/Script/{EquipmentFoundation,EquipmentRuntime,EquipmentInventory,EquipmentRefinement,EquipmentStatPreview,ProductionLootConfig,CombatLootRewards,TuTienEnums,WorldMapCatalog,CombatBalance,CombatStats,CombatDamage,WeaponBasicAttack,WeaponSkillRegistry,CombatSkillProgress,RealmProgressionAuthority,RealmProgression,CultivationIntegrationGate,MartialArtsRegistry,CultivationSaveJson}.cs \
  Assets/Script/{WorldClimate,ExperienceProgress,HanVuOrigin,HanVuNarrative,HanVuEncounters,WorldQiDistribution,CultivationModel,CultivationEngine,CultivationCapacity,CultivationBreakthrough,CultivationPersistence,CultivationGameplayState,CultivationPlaytestProfile,CultivationProductionConfiguration,CultivationProductionConfiguration.Versions,CultivationConfigurationUpgrade,CultivationPerformance,BloodEssenceTransactions,FoundationRecovery,HeavenlyGateModel,HeavenlyGatePenalty,HeavenlyTribulation}.cs \
  Assets/Script/MasteryFoundation.cs Assets/DataScript/EntityDataSO.cs
"${MONO:-mono}" "$build_dir/ItemLootFoundation.exe" "$@"
