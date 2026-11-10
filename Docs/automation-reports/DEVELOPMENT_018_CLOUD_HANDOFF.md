# Development 0.1.8 — Visual, Animation & UI Foundation (Cloud handoff)

Source baseline: `development_0.1.7` @ `fafc02d613a2e6a15c72a8dd48b935d2faa54f0e`.

## Ground truth from 0.1.7
- Eight equipment slots, instance IDs, rarity rules (Common primary stat only; Hoàng/Uncommon may grant a SkillID), Inventory Compare, Forge, loot, and Save/Load are implemented.
- Report: `Docs/automation-reports/DEVELOPMENT_017_REPORT.md`.
- Five groups of automated MainGame/Windows Player smoke tests reportedly passed **on the developer's Windows machine with WARP**. This is report evidence, not a fresh Cloud validation.
- Native mouse and default GPU shutdown behavior remain unverified.

## Goal
Improve the readability and replaceability of existing 2D presentation *without changing combat mechanics, item economics, or Save data*.

## First cloud implementation slice (code changes still pending)
1. Audit current sprite/animation binding for Hero, Monster, item loot, equipped weapon and skill slots. Identify the actual script paths, sprites and anchors. Avoid introducing a second animation framework.
2. Separate visual configuration/asset references from gameplay damage and positional state only where necessary. Preserve animation timing, Combat damage, skill casting/cooldown and projectile identity.
3. Make equipped items visually distinguishable in Inventory/Compare and on the HUD through existing icon slots and fallback sprites. Ensure missing sprite does not hide item details or break interaction.
4. Ensure weapon and effect anchors follow hero stance/facing. Do not change collider, hitbox, BattleArea or camera coordinates to compensate for art.
5. Improve clarity of selection/unavailable/active states using current UI style, without redesigning Inventories or opening new menus.
6. Add targeted non-visual code tests or static checks for selection/asset fallback; attach before/after screenshots only if an actual renderer is available.

## Deliberately out of scope
- Generating production anime/chibi sprites or model animations without reference assets.
- New skill/item content, new rarity tiers, damage/balance, story, Save migrations.
- Claims of Unity Play Mode or Windows build PASS from a Linux/source-only Cloud environment.

## Acceptance criteria for a Codex Cloud task
- All changed lines trace to the above scope and use the existing project patterns.
- Missing image fallback is safe and distinguishable; weapon icons match actual equipped item.
- No gameplay or Save contract changes; compilation warnings are attributed if testing is available.
- All smoke results explicitly name the environment. Any unavailable Unity/Windows checks are marked **NOT RUN**.
- Commit only targeted files on this branch and open a PR; do not merge or force-push.
- User will still perform final sprite/VFX and native input acceptance on Windows.

## Agent guidelines
Read `AGENTS.md`, `.agents/skills/idle-game-project-rules/SKILL.md`, `.agents/skills/karpathy-guidelines/SKILL.md`, `.agents/skills/unity-csharp-scripting/SKILL.md` and `.agents/skills/create-game-assets/SKILL.md` before source changes.

## Cloud status at handoff
This document and the Karpathy skill are committed using GitHub actions. **No Unity source code, runtime assets, build, or player verification has yet been implemented for 0.1.8.** A Codex Cloud execution session still needs to be started from this branch.

## Spine 3.8.75 authoring decision (2026-11-10)
User will create Hero and Monster animations with **Spine Editor 3.8.75**, aiming for batch reuse across all actors. Read `Docs/animation/SPINE_3_8_75_PIPELINE.md` before changing animation code. Esoteric's official chart marks spine-unity 3.8 unsupported on the game's Unity 6000.5.0f1. Do not install a mismatched runtime in the game. First prepare a shared, data-driven actor visual bridge and PNG-sequence ingestion compatible with Spine 3.8 export; skeleton runtime integration is explicitly gated pending a supported editor/runtime/version strategy. No new production art has been supplied; do not fabricate assets or claim native Spine/Unity PASS.

## Superseding task — Hero PNG animation experiment (2026-11-10)
The user has supplied `hero.zip` **in the ChatGPT conversation** and requests that its frame sequences replace **Hero presentation only**, temporarily reused for all equipped weapon families. This task supersedes the preceding Spine runtime plan **for this experiment**. Do not install Spine or build native skeleton integration for this delivery.

**Confirmed ZIP inventory (read from the user-provided file, 115 RGBA PNG frames):**
| Folder | Frames | Canvas |
| --- | ---: | --- |
| `hero/sword_idle` | 48 | 360x500 |
| `hero/sword_walk` | 17 | 360x500 |
| `hero/sword_run` | 11 | 360x500 |
| `hero/sword_hit` | 27 | 360x500 |
| `hero/archer_run` | 12 | 360x640 |

**Important:** The ZIP includes **no** BasicAttack, Cast, SkillRelease or Death. Preserve existing clips/visual fallbacks for those states. Do not substitute Hit as Attack. Do not load archer_run in the initial shared-weapon experiment because its canvas/pose is different.

### Implementation contract for Codex Cloud
1. Get actual `hero.zip` attachment bytes in the Work session; **this branch currently does not contain those binary images**. Do not pretend it does. If unavailable, implement only a data-driven import adapter and mark asset integration BLOCKED.
2. Keep Hero logic, colliders, HP bar, VFX, attack timing, mana, skill scheduler, hit/damage and Save unchanged.
3. Align all PNG frames by stable logical pivot/feet marker; do **not** shift Hero gameplay position to hide per-frame jitter. Sample PNGs contain transparent alpha and should retain alpha. Avoid per-frame crop changing visual anchor.
4. Create one shared Hero animation profile used regardless of Sword/Bow/Staff/Unarmed, binding Idle/Walk/Run/Hit. Reuse existing sprite bridge instead of replacing its attack/hit precedence rules.
5. Map gameplay exploration movement to Walk or Run using **actual movement state**. Do not infer a speed threshold without checking controller code; existing speed values and semantics govern.
6. Explicit per-clip loop and configurable playback FPS. Idle, Walk and Run loop; Hit one-shot. Death and skill visuals fall back.
7. Keep actual frame ordering from trailing 5-digit numeric sequence; make imports deterministic, avoid extra GUID churn. Optimize texture use only if observed and keep full-resolution source originals available.
8. Tests: static frame count/order, sprite binding and fallback; Unity/Player runtime tests only if an actual compatible environment exists. Report NOT RUN otherwise.
9. After implementing, provide screenshot/video evidence of Hero holding Sword, Bow and Staff displaying the **same** selected animations when states match; animation change must not change Combat/Save.

**User intent:** quick playable visual integration, not a new system, no new game content, no need for Spine install now.
