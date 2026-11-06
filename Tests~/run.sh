#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
defines=()
if [[ -n "${IDLEGAME_TEST_DEFINES-UNITY_EDITOR}" ]]; then defines=("-define:${IDLEGAME_TEST_DEFINES-UNITY_EDITOR}"); fi
mcs "${defines[@]}" -out:"$build_dir/MotionRegression.exe" \
  Tests~/*.cs \
  Assets/Script/{OpeningArtTheme,OpeningEdgeFeather,OpeningBackgroundVideo,OpeningHeroSplashVideo,OpeningCinematicUI,MapBgmCatalog,MapBgmController,MusicVolumePanelUI,WeaponBasicAttack,CombatLootRewards,EquipmentFoundation,EquipmentRuntime,EquipmentInventory,EquipmentRefinement,EquipmentStatPreview,InventoryPanelUI,InventoryArtTheme,ManagementWindowLayout,CombatStats,CombatDamage,DevBalanceUI,TransparentWindow,GroundPresentation,StartMenuUI,SaveMenuUI,UIManager,CustomInteractable,SaveManager,CombatBalance,BattleMotion,BattleEffects,EnvironmentManager,HeroController,MonsterController,MonsterSpawner,HeroCombatState,CombatManager,GameManager,SaveSlot,TuTienEnums,WorldMapCatalog,WorldMapPolygons,WorldMapPolygonGraphics,WorldMapPanelUI}.cs \
  Assets/Script/CultivationSpriteBridge.cs \
  Assets/Script/HeroSpawnVfx.cs \
  Assets/Script/LootDropPresentation.cs \
  Assets/Script/ProductionLootConfig.cs \
  Assets/Script/HeroRenderProbe.cs \
  Assets/Script/HeroFormation.cs \
  Assets/Script/HeroLaneMotion.cs \
  Assets/Script/HeroCombatSfx.cs \
  Assets/Script/AudioVolumeSettings.cs Assets/Script/AudioVolumeBar.cs \
  Assets/Script/ExplorationProgressHUD.cs Assets/Script/HudW3Layout.cs Assets/Script/HudW3View.cs Assets/Script/UIManager.HudW3.cs \
  Assets/Script/RealmProgressionAuthority.cs \
  Assets/Script/RealmProgression.cs \
  Assets/Script/RealmBreakthroughTransition.cs \
  Assets/Script/MartialArtsRegistry.cs \
  Assets/Script/BossEncounterCoordinator.cs \
  Assets/Script/HeavenlyTribulation.cs Assets/Script/HeavenlyTribulationFixtures.cs Assets/Script/TribulationFixtureCombatBridge.cs \
  Assets/Script/CultivationPerformance.cs \
  Assets/Script/CultivationCapacity.cs \
  Assets/Script/CultivationModel.cs \
  Assets/Script/CultivationEngine.cs \
  Assets/Script/BloodEssenceTransactions.cs \
  Assets/Script/FoundationRecovery.cs \
  Assets/Script/CultivationBreakthrough.cs \
  Assets/Script/CultivationPersistence.cs \
  Assets/Script/CultivationIntegrationGate.cs \
  Assets/Script/WorldQiDistribution.cs Assets/Script/CultivationConfigurationUpgrade.cs Assets/Script/CultivationProductionConfiguration.Versions.cs Assets/Script/CultivationProductionConfiguration.cs Assets/Script/CultivationMigrationTransaction.cs Assets/Script/SaveManager.CultivationMigration.cs Assets/Script/GameManager.CultivationMigration.cs \
  Assets/Script/CultivationPlaytestProfile.cs Assets/Script/CultivationSaveJson.cs Assets/Script/CultivationGameplayState.cs Assets/Script/CultivationBalanceSimulation.cs Assets/Script/SaveManager.Cultivation.cs Assets/Script/GameManager.Cultivation.cs Assets/Script/UIManager.Cultivation.cs \
  Assets/DataScript/EntityDataSO.cs
mono "$build_dir/MotionRegression.exe" "$@"
