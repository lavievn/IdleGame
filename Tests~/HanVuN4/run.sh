#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
python3 Tests~/HanVuN4/prepare.py "$build_dir"
"${CSC:-mcs}" -langversion:7.2 -define:UNITY_EDITOR,HANVU_N4_TESTS -r:System.Web.Extensions.dll -out:"$build_dir/N4.exe" \
  "$build_dir"/*.cs Tests~/HanVuN4/{MemoryIO,EngineJson,Boundary,Runner,Regression}.cs \
  Tests~/{NestedEquipmentSaveStub,StoryOriginRegression,WorldMapRegression}.cs \
  Tests~/HanVuN3/Regression.cs \
  Assets/Script/{HanVuOrigin,HanVuNarrative,HanVuEncounters,UIManager.StoryOrigin,SaveSlot,TuTienEnums,WorldMapCatalog,CombatBalance,CombatStats,CombatDamage,WeaponBasicAttack,WeaponSkillRegistry,CombatSkillProgress,EquipmentFoundation,EquipmentRuntime,EquipmentInventory,EquipmentRefinement,EquipmentStatPreview,RealmProgressionAuthority,RealmProgression,CultivationIntegrationGate,MartialArtsRegistry,CultivationSaveJson}.cs \
  Assets/Script/{WorldQiDistribution,CultivationModel,CultivationEngine,CultivationCapacity,CultivationBreakthrough,CultivationPersistence,CultivationGameplayState,CultivationPlaytestProfile,CultivationProductionConfiguration,CultivationProductionConfiguration.Versions,CultivationPerformance,BloodEssenceTransactions,FoundationRecovery}.cs \
  Assets/Script/{HeavenlyGateModel,HeavenlyGatePenalty,HeavenlyTribulation}.cs \
  Assets/DataScript/EntityDataSO.cs
"${MONO:-mono}" "$build_dir/N4.exe"
