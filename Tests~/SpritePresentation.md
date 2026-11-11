# Character and sprite presentation regression — v0.9

The primary Hero now uses the existing `Swordsman_Normal` bitmap for every weapon/action. Old Archer sample animation assertions have been replaced by unified-identity tests; the historical `--archer-animation` flag remains an alias for harness compatibility. Enemy pose assets and fallback behavior are unchanged.

With Mono/mcs on PATH:

```bash
bash Tests~/run.sh --unified-hero
bash Tests~/run.sh --sprites
bash Tests~/run.sh --weapon-basic
bash Tests~/run.sh --enemy-sprites
```

Unified tests assert the exact same Image and Sprite instance across real Sword/Bow/Staff/Unarmed equipment handlers, death/reuse and missing legacy samples. Sprite tests still assert actual Attack/Hit/Normal request state and deadlines, damage/popups, geometry, root/UI ownership and camera behavior. For Hero the expected bitmap stays Normal; Monster keeps its exact existing role/pose texture. No combat assertion is replaced by a bitmap comparison.

These are Mono/UnityStub tests, not native importer/rendering/input proof. Native MainGame v0.9 separately checks primary-owned projectiles, real monster HP reduction, one stable body and lifecycle. See `Docs/architecture/UNIFIED_CHARACTER_VISUAL_CONTRACT.md` and the v0.9 report for evidence classification.


## 0.1.8 Hero PNG (cloud)

With the new `HeroAnimation/SharedHero` Resources profile, the existing bridge now
plays Idle/Walk/Run/Hit sequences independently of the weapon. Unsupported actions
retain the prior bitmap. Historical fixtures above do not register this profile,
so they exercise the legacy fallback, not native PNG import. The full runner has
pre-existing compile blockers at this branch baseline. See
[focused checks](HeroPng/README.md) and the
[cloud report](../Docs/automation-reports/DEVELOPMENT_018_HERO_PNG_REPORT.md)
for actual results and outstanding Unity/Windows acceptance.
