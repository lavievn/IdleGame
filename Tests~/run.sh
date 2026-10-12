#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
mcs -define:UNITY_EDITOR -out:"$build_dir/MotionRegression.exe" \
  Tests~/*.cs \
  Assets/Script/{DevBalanceUI,TransparentWindow,GroundPresentation,StartMenuUI,SaveMenuUI,UIManager,CustomInteractable,SaveManager,CombatBalance,BattleMotion,BattleEffects,EnvironmentManager,HeroController,MonsterController,MonsterSpawner,HeroCombatState,CombatManager,GameManager,SaveSlot,TuTienEnums}.cs \
  Assets/DataScript/EntityDataSO.cs
mono "$build_dir/MotionRegression.exe"
