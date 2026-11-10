# Hero PNG bridge boundary checks (cloud)

Run from the repo root with Python 3, Mono and mcs on PATH:

```bash
bash Tests~/HeroPng/run.sh
python3 Tools/validate_hero_png.py --source-zip /path/to/hero.zip
```

The focused runner compiles the **real CultivationSpriteBridge** against the
existing UnityStub object/transform/Image/Resources doubles. `Boundary.cs` models
only the Hero/Monster inputs and flat JSON decoding; it does **not** execute real
HeroController, equipment handlers, combat, Unity JsonUtility, native lifecycle,
import, rendering, Canvas or Windows. It never creates SaveManager or accesses saves.
The extraction is isolated in a temporary directory and removed on exit.

Nine scenarios cover ordering/counts, configured FPS, all three looping motions,
pause, no Resources calls during playback, Attack/Hit priority, .16s legacy attack
frame, full one-shot Hit, telemetry selection/stale movement, weapon-independent
binding, body/root/HP ownership, opacity, missing-clip fallback, cleanup/reuse and
unchanged monster presentation. The static validator optionally compares every
shipped PNG to the original upload byte for byte.

Unity native import checks are in
`Assets/Tests/Presentation/PlayMode/HeroPngImportTests.cs` (four TestCases).
Run that class in an empty-scene Test Runner after a licensed Unity 6000.5.0f1
import; it only reads Resources. These tests have **NOT RUN** on cloud because
Editor launch exits 198 without a valid license. The full historical
`Tests~/run.sh --sprites` is separately blocked by 12 pre-existing compile errors;
the focused result must not be presented as a full regression PASS.
