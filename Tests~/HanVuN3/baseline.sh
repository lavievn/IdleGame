#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
snapshot="$(mktemp -d)"
trap 'rm -rf "$snapshot"' EXIT
case "${1:-original}" in
  original) ref=41d376526b415eaffc7e0fe0f5bd03e41d87b6ff ;;
  retained) ref=1df0d46c5d117cb9bfc3cc865bf9c937e841e934 ;;
  *) echo 'Use original or retained' >&2; exit 2 ;;
esac
# Read-only extraction. Original runtime is exactly #17; retained is the first
# guard checkpoint before the two retained-delegate regressions were added.
git archive "$ref" Assets/Script Assets/DataScript Tests~ \
  Assets/Resources/WorldMapCatalog.txt Assets/Resources/WorldMapPolygonSeeds.txt \
  Assets/Resources/WorldMapPolygons.json | tar -x -C "$snapshot"
if [[ "${1:-original}" == retained ]]; then
  cp Tests~/HanVuN3/{Boundary,Regression}.cs "$snapshot/Tests~/HanVuN3/"
fi
# Expected exit1: original=20 PASS/6 FAIL, retained=28 PASS/2 FAIL.
bash "$snapshot/Tests~/HanVuN3/run.sh"
