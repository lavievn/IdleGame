#!/usr/bin/env bash
# Focused bridge boundary tests: no gameplay managers, SaveManager or save I/O.
set -euo pipefail
cd "$(dirname "$0")/../.."
build_dir="$(mktemp -d)"
trap 'rm -rf "$build_dir"' EXIT
python3 - "$build_dir/UnityBoundary.cs" <<'PY'
from pathlib import Path
import sys
s=Path('Tests~/UnityStub.cs').read_text()
# Reuse the existing transform, object, Sprite, Resources and clock test doubles.
# Exclude the full runner's gameplay/save/TMP dependencies, which this test never calls.
parts=[s[:s.index('    public static class PlayerPrefs')],
       s[s.index('    public class SerializeField'):s.index('namespace UnityEngine.UI')],
       s[s.index('namespace UnityEngine.UI'):s.index('namespace TMPro')],
       s[s.index('namespace UnityEngine {\n public enum FindObjectsInactive'):s.index('namespace UnityEngine.Video')]]
Path(sys.argv[1]).write_text(''.join(parts))
PY
mcs -langversion:latest -out:"$build_dir/HeroPngTests.exe" \
  "$build_dir/UnityBoundary.cs" Tests~/HeroPng/Boundary.cs Tests~/HeroPng/Regression.cs \
  Assets/Script/CultivationSpriteBridge.cs
mono "$build_dir/HeroPngTests.exe"
