#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
snapshot="$(mktemp -d)"
trap 'rm -rf "$snapshot"' EXIT
# Test-only checkpoint: production files are identical to PR #24.
git archive e03445760a6c33e57c2c194cf597657681c33aaa \
  Assets/Script Assets/DataScript Tests~ \
  Assets/Resources/WorldMapCatalog.txt Assets/Resources/WorldMapPolygonSeeds.txt \
  Assets/Resources/WorldMapPolygons.json | tar -x -C "$snapshot"
# Expected exit1: 15 PASS/8 FAIL. No branch/worktree or player Save is changed.
bash "$snapshot/Tests~/HanVuN4/run.sh"
