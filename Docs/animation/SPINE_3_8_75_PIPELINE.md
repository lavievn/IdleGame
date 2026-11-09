# Spine 3.8.75 animation integration plan — Bách Luyện Tiên Đồ

## Compatibility gate (verified 2026-11-10)
- User authoring tool: **Spine Editor 3.8.75**.
- Game: **Unity 6000.5.0f1**.
- Official Esoteric Software compatibility chart supports **spine-unity 3.8 only on Unity 2017.1–2020.3**. This Unity project is outside that range. Do **not** directly vendor the 3.8 Unity runtime into production and do not blindly use a 4.x runtime to read 3.8 exports.
- A matching Spine editor/runtime version is required for runtime skeleton exports. A Spine license is required for integration. Check the precise license grant and environment before distributing runtime.
- Official docs:
  - https://esotericsoftware.com/spine-unity-installation
  - https://esotericsoftware.com/spine-unity-download
  - https://esotericsoftware.com/spine-export
  - https://esotericsoftware.com/spine-using-runtimes

## Approved near-term path: export PNG sequences, preserve .spine sources
Spine 3.8 exports PNG frame sequences with alpha. Unity can display these using the existing sprite pipeline without embedding the unsupported 3.8 runtime. Tradeoffs: more texture memory/size, no skeletal blending or skin swaps at runtime. Optimize only after measuring real assets.

Provide a single animation abstraction for **Hero, companions/B2, and Monsters**. Do not build separate player and monster animator engines. Source state is still the existing Combat/Actor lifecycle, never the renderer.

### Animation contract (names are logical keys, do not overwrite current runtime aliases)
- `Idle` — loop
- `Move` — loop
- `BasicAttack` — one-shot, resolved by current weapon/monster profile
- `Cast` — loop or time-scaled for authoritative cast duration
- `SkillRelease` — one-shot
- `Hit` — interruptible per existing sprite-bridge rules
- `Death` — one-shot terminal; never resurrect through visual callback

Optional later: `Spawn`, `Victory`, `Special`. Missing clips use an explicit fallback; do not silently cancel gameplay.

### Bulk content pipeline
Each actor asset bundle should carry a stable actor/visual profile ID, skeleton source .spine file (stored as authoring source if user has rights), export version, image sequence/atlas references, scale, pixels-per-unit, pivot/feet origin, facing, animation name mapping, loop flags, FPS, hit/release event placement if authored, and optional socket anchors (weapon, chest, shoulder, muzzle, hit).
Use one configurable importer/editor helper only after checking existing project tooling; batch-validate file naming/frames before touching live prefabs. Avoid creating a new animation framework without tracing the existing sprite bridge.

Suggested source structure (adapt to existing repo conventions):
`SourceAssets~/Spine/<actor-id>/...` (authoring assets, optional)
`Assets/Resources/Actors/<actor-id>/...` (runtime PNG sequences/atlases and profiles)

Do not mass-import placeholder images or fake .spine project files. Never manufacture GUID/.meta entries manually.

### Runtime invariants
- Combat controls cast/release/damage/projectile timing. Spine animation or PNG sequence **never awards damage, consumes mana, or controls cooldown**.
- Weapon/facing changes only choose visual clips/anchors. Keep real BattleArea coordinates/colliders unchanged.
- Hit/attack timing, cancel/death/despawn/reuse, and UI layering preserve existing lifecycle and stale-callback protection.
- Avoid allocations per frame; cache sprites or atlas references, and reuse animation presentation components.
- Show hit/VFX/projectile anchors at proper shoulder/chest height; do not compensate by moving actor physics or world positions.

### Future native Spine runtime (blocked pending compatibility decision)
If later upgrading to a supported Spine Editor/runtime and Unity pair:
1. Back up and migrate source .spine projects; export with the exact matching runtime version.
2. Prototype one actor in an isolated Unity test branch/package (license confirmed).
3. Bind the same logical animation contract to Spine skeleton animations, events and slots; keep the PNG bridge as fallback.
4. Verify Unity compile, URP materials, scale/pivot, sorting, pause, events and player build **on actual Unity**, not through source-only tests.
Do not present this as ready with Spine 3.8.75 on Unity 6000.5.0f1.

## Development 0.1.8 acceptance (cloud)
1. Audit current actor/sprite bridge and actual animations, then propose minimal adapter changes; keep existing visuals working.
2. Prepare data contract, sample manifest, and import validation for **one real source actor only if assets are provided**.
3. Provide a scalable bulk-ingestion path for Hero and Monster profiles without copy/paste per class.
4. Test source-only checks that are possible in cloud; mark Unity Editor/Windows Player validation NOT RUN unless actually run.
5. No runtime Spine DLL/package import or claim of tested native Spine integration without resolving the compatibility gate.
