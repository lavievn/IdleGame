#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
python3 Tests~/HanVuN3/extract_host.py "$build_dir/OriginHost.cs"
"${CSC:-mcs}" -langversion:7.2 -out:"$build_dir/HanVuN3.exe" \
  Tests~/{UnityStub,NestedEquipmentSaveStub,StoryOriginRegression,WorldMapRegression}.cs \
  Tests~/HanVuN3/{Boundary,Runner,Regression}.cs "$build_dir/OriginHost.cs" \
  Assets/Script/{HanVuOrigin,HanVuNarrative,HanVuEncounters,UIManager.StoryOrigin,TuTienEnums,WorldMapCatalog,CombatBalance,CombatStats,CombatDamage,WeaponBasicAttack,WeaponSkillRegistry,CombatSkillProgress,EquipmentFoundation,EquipmentRuntime,EquipmentInventory,EquipmentRefinement,EquipmentStatPreview,RealmProgressionAuthority,RealmProgression,CultivationIntegrationGate,MartialArtsRegistry,CultivationSaveJson}.cs \
  Assets/Script/{WorldQiDistribution,CultivationModel,CultivationEngine,CultivationCapacity,CultivationBreakthrough,CultivationPersistence,CultivationGameplayState,CultivationPlaytestProfile,CultivationProductionConfiguration,CultivationProductionConfiguration.Versions,CultivationConfigurationUpgrade,CultivationPerformance,BloodEssenceTransactions,FoundationRecovery}.cs \
  Assets/DataScript/EntityDataSO.cs
"${MONO:-mono}" "$build_dir/HanVuN3.exe"
