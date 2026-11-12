#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
"${CSC:-mcs}" -langversion:7.2 -r:System.Web.Extensions.dll -r:System.Numerics.dll -out:"$build_dir/DualProgressionAudit.exe" \
  Tests~/UnityStub.cs Tests~/NestedEquipmentSaveStub.cs \
  Tests~/{CultivationCoreRegression,BloodEssenceRegression,CultivationBreakthroughRegression,CultivationPersistenceRegression}.cs \
  Tests~/DualProgressionAudit/{Boundary,Runner,Regression,Simulation}.cs \
  Assets/Script/{EquipmentFoundation,EquipmentRuntime,EquipmentInventory,EquipmentRefinement,EquipmentStatPreview,TuTienEnums,WorldMapCatalog,CombatBalance,CombatStats,CombatDamage,WeaponBasicAttack,WeaponSkillRegistry,CombatSkillProgress,RealmProgressionAuthority,RealmProgression,MartialArtsRegistry}.cs \
  Assets/Script/{WorldQiDistribution,CultivationModel,CultivationEngine,CultivationCapacity,CultivationBreakthrough,CultivationPersistence,CultivationGameplayState,CultivationPlaytestProfile,CultivationSaveJson,CultivationProductionConfiguration,CultivationProductionConfiguration.Versions,CultivationConfigurationUpgrade,CultivationIntegrationGate,CultivationPerformance,CultivationBalanceSimulation,BloodEssenceTransactions,FoundationRecovery}.cs \
  Assets/Script/{ExperienceProgress,HeavenlyGateModel,HeavenlyGatePenalty,HeavenlyTribulation}.cs \
  Assets/DataScript/EntityDataSO.cs
"${MONO:-mono}" "$build_dir/DualProgressionAudit.exe" "$@"
