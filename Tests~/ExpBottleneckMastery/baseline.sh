#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
probe="$PWD/Tests~/ExpBottleneckMastery/Baseline.cs"
snapshot="$(mktemp -d)"
trap 'rm -rf "$snapshot"' EXIT
# Read-only Git extraction; never switch the shared working tree or merge.
git archive 2b88eb59c0c0d2c5c1620a0ffd2fd9191bdbcefd \
  Assets/Script Assets/DataScript Tests~ \
  Assets/Resources/CultivationProductionV1.json Assets/Resources/CultivationProductionV2.json \
  Assets/Resources/CultivationProductionV3.json Assets/Resources/CultivationCapacityV1.json \
  Assets/Resources/WorldQiDistributionV1.json Assets/Resources/WorldQiDistributionV2.json \
  Assets/Resources/WorldMapCatalog.txt Assets/Resources/WorldMapPolygonSeeds.txt | tar -x -C "$snapshot"
cp "$probe" "$snapshot/Tests~/DualProgressionAudit/Baseline.cs"
python3 - "$snapshot" <<'PY'
import sys
from pathlib import Path
root=Path(sys.argv[1])
p=root/'Tests~/DualProgressionAudit/Runner.cs'
p.write_text(p.read_text().replace('DualProgressionAuditTests();','DualProgressionAuditTests();ExpBaselineProbe();'))
p=root/'Tests~/DualProgressionAudit/run.sh'
s=p.read_text().replace('-langversion:7.2','-langversion:7.2 -define:UNITY_EDITOR')
s=s.replace('{Boundary,Runner,Regression,Simulation}', '{Boundary,Runner,Regression,Simulation,Baseline}')
s=s.replace('  Assets/DataScript/EntityDataSO.cs',
    '  Assets/Script/{HeavenlyGateModel,HeavenlyGatePenalty,HeavenlyTribulation,HeavenlyGateV13Fixtures}.cs \\\n  Assets/DataScript/EntityDataSO.cs')
p.write_text(s)
PY
# Expected exit 1: the two new requirements fail on the real base implementation.
bash "$snapshot/Tests~/DualProgressionAudit/run.sh"
