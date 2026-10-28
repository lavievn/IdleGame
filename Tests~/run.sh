#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
mcs -define:UNITY_EDITOR -out:"$build_dir/MotionRegression.exe" \
  Tests~/*.cs \
  Assets/Script/{OpeningArtTheme,OpeningEdgeFeather,OpeningBackgroundVideo,OpeningHeroSplashVideo,OpeningCinematicUI,MapBgmCatalog,MapBgmController,MusicVolumePanelUI,CombatLootRewards,EquipmentFoundation,EquipmentRuntime,EquipmentInventory,EquipmentRefinement,EquipmentStatPreview,InventoryPanelUI,InventoryArtTheme,ManagementWindowLayout,CombatStats,CombatDamage,DevBalanceUI,TransparentWindow,GroundPresentation,StartMenuUI,SaveMenuUI,UIManager,CustomInteractable,SaveManager,CombatBalance,BattleMotion,BattleEffects,EnvironmentManager,HeroController,MonsterController,MonsterSpawner,HeroCombatState,CombatManager,GameManager,SaveSlot,TuTienEnums,WorldMapCatalog,WorldMapPolygons,WorldMapPolygonGraphics,WorldMapPanelUI}.cs \
  Assets/Script/CultivationSpriteBridge.cs \
  Assets/Script/HeroSpawnVfx.cs \
  Assets/Script/HeroRenderProbe.cs \
  Assets/Script/HeroFormation.cs \
  Assets/Script/HeroLaneMotion.cs \
  Assets/Script/HeroCombatSfx.cs \
  Assets/Script/AudioVolumeSettings.cs Assets/Script/AudioVolumeBar.cs \
  Assets/Script/ExplorationProgressHUD.cs \
  Assets/DataScript/EntityDataSO.cs
mono "$build_dir/MotionRegression.exe" "$@"
