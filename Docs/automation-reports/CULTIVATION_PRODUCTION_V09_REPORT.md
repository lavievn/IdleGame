# Cultivation Production v0.9 — acceptance and handoff

06 November 2026 · BÁCH LUYỆN TIÊN ĐỒ · Full consolidated batch.

## Standing authorization update

After the initial v0.9 handoff, the user granted standing approval for confirmation requests within the agreed development scope and requested that actions be recorded in this report instead of asking again. Development and verification can proceed under that authorization; applicable tool access controls remain in effect. The explicit player-facing legacy Save migration consent remains part of the implemented game flow. This authorization update changes documentation only; the accepted code and test results below are unchanged.

## Result and evidence boundaries

**IMPLEMENTED / VERIFIED in Unity Editor:** default MainGame production cultivation, explicit legacy-slot migration with verified immutable backup and guarded rollback, a deterministic coordinate-derived table for all133 nodes, and one default Hero body across weapon/attack modes. `CultivationIntegrationGate.ProductionEnabled=true` was enabled after preactivation native and MainGame checks; the final default-production MainGame run also passed. Startup still requires valid production configuration and legacy Saves still require consent.

**NOT RELEASED:** no Windows Development Player was built or accepted in this batch. Unity native Edit Mode, real MainGame Play Mode, pure Mono, and Windows Player results are separated below. Personal player Saves were not fixtures and were not migrated by automation. Burning, Heavenly Trial and offline cultivation remain OFF. No new artwork, animation system, skills, encounters, loot or monster-strength feature was added.

**PARTIAL:** geographic authorship remains127 IMAGE_LABEL/6 INFERRED source centers; runtime coordinate registration is verified for all133. Performance numbers are bounded Editor observations, not a Player profiler certification. The table is an experimental v0.9 environmental distribution, not a completed world-economy balance pass.

## Repository and baseline

- Repository: `lavievn/IdleGame`.
- Actual worktree: `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Branch: `feature/monster-awareness-ambush`.
- Baseline HEAD: `1de4e7919b07a6d6f8443e65d3e31a9b9837a89f`; initially CLEAN, ahead5 of the existing local tracking reference. No remote fetch was needed.
- Functional source HEAD: `804a73fb1ca69fca637007fd8c24f4fc2d832df7`. The following documentation commit contains this report. Its actual hash and final CLEAN status are recorded in the external `handoff.json`, avoiding a self-referential report hash.
- Unity: `6000.5.0f1`; project version/package configuration retained. No concurrent Unity/Mono/Python/bash/Git writer was found during baseline audit; installation was blocked while Unity was running and guarded by source/stage hashes.
- No push, pull, fetch, merge, reset or rebase. No preexisting uncommitted user changes were overwritten.

The v0.6/v0.7/v0.8 reports and progression, blood, balance and migration contracts were read. Source confirms the v0.8 clock/schema2, candidate load, independent Level/EXP and owned playtest isolation. Historical balance/profile identities were retained. The baseline production gate was OFF because consent/backup/rollback and a production configuration were absent.

Baseline checklist, executed before source edits:

| Check | Baseline result |
|---|---|
| Branch/HEAD/working tree/writers/Unity configuration | Verified as above |
| Canonical Hero and slot authority | GameManager Hero, independent EntityDataSO component, SaveManager candidate before swap |
| Component version/config guards and clock remainder | Existing v0.8 behavior retained |
| Production activation | OFF at baseline, owned playtest only |
| Balance | Existing Luyện Khí thresholds[0,900,2100,3600], no automatic successor |
| Related Mono tests |188 PASS before edits; raw `Baseline-*.log` retained outside Git |

## Functional commits and source changes

| Commit | Scope |
|---|---|
| `7cd25517388b23eb299685a058fe3f05ef72f355` | Versioned production configuration,133-node Qi data, offline generator/overrides, source adapter |
| `94578ba02b01075f7e5717ebb4a136a38cd72eac` | Explicit migration, verified backup, guarded recovery, consent integration and bounded safe writer handling |
| `2c2c167084ff6a5987c568ebde0b27788924d2fb` | One default Hero body, presentation regressions and test documentation |
| `804a73fb1ca69fca637007fd8c24f4fc2d832df7` | Native/Play Mode integration, production gate, UI and performance verifiers |
| Documentation commit following the above | This report, World Qi contract, unified visual contract and tested migration policy |

Main changes:

- `Assets/Script/WorldQiDistribution.cs`, `CultivationProductionConfiguration.cs`; `Assets/Resources/WorldQiDistributionV1.json`, `CultivationProductionV1.json`; `Tools/generate_world_qi.py`, `Tools/data/WorldQiOverridesV1.json`.
- `CultivationPlaytestProfile.cs` retains historical profiles and permits an explicit independent production profile. `CultivationGameplayState.cs` resolves the production source without adding another clock.
- `CultivationMigrationTransaction.cs`, `SaveManager.CultivationMigration.cs`, `GameManager.CultivationMigration.cs`; necessary candidate/consent/writer integration in SaveManager, GameManager and their cultivation partials, and `StartMenuUI.cs`.
- `CultivationSpriteBridge.cs` detaches Hero identity from WeaponFamily/attack mode. No Hero/Monster AI, combat formula or scene/prefab/artwork change.
- `CultivationIntegrationGate.cs`, `UIManager.Cultivation.cs`; native verifiers `CultivationV09Verifier.cs`, `CultivationV09PerformanceVerifier.cs`; MainGameFoundationProbe core and v0.9 cultivation/integration/UI-cap partials.
- Relevant Mono regressions/stubs/runner updated. The old Archer prototype animation assertions are replaced explicitly by five unified-body tests; they are not silently counted as unchanged animation requirements.

Protected-source comparison against baseline is PASS: CultivationEngine/Model, HeroController, MonsterController/Spawner, CombatManager/damage/stats/balance, movement/camera/environment, Equipment/Forge, root/weapon behavior, production loot, world catalog/polygons/seeds/artwork/topology, MainGame scene/Monster prefab, historical balance/profile assets, Unity version and packages are unchanged. There are no texture/animation/scene/prefab additions. Exact protected paths are in external `scope.json`.

## Production configuration and gate

Configuration ID `production:cultivation-v09`, version1; component schema2, starting realm `realm:luyen-khi`. Foundation/reserve/energy start at0; thresholds[0,900,2100,3600]. Absorption coefficient2, efficiency1, refinement input cap1Qi/s, refinement efficiency1. No successor realm, automatic breakthrough, free Blood, injury or offline award.

The config references `world-qi:atlas-v1` version1 and a line-ending-normalized SHA256 fingerprint. Production version and world-data version must agree. Changing seed, generator, tiers or mapping requires a new data/config version; old Saves are not silently retagged. Unsupported old/different/future configurations fail closed.

Configuration reasons include MissingAsset, InvalidEnvelope, WorldQiInvalid, WorldQiVersionMismatch, InvalidBalance and InvalidSource. World-table validation separately diagnoses schema/identity/tier/rate/coordinate/duplicate/unknown/missing/start errors. The133-node table is not hardcoded in CultivationEngine.

Gate evidence: validated config/table; baseline starts; native migration/rollback/filesystem tests; actual MainGame consent, lifecycle and visuals; one clock; no Qi-to-Level/EXP award. The final default-production Play run overrides only the owned Save directory before Awake: the config is loaded through the normal default production path, not an injected playtest profile. An invalid config cannot silently create a playable componentless production Hero.

## Migration, consent and durability

See [the tested policy](../architecture/CULTIVATION_SAVE_MIGRATION_POLICY_V1.md).

Read-only preflight parses and validates the selected legacy slot, constructs an independent candidate and initializes production cultivation at zero. Every unrelated root JSON value is copied from the source tokens, preserving Level/EXP/stat points/roots/equipment/inventory/forge/map/wave and other legacy data. Existing normal Load normalization of old gear metadata remains unchanged. Realm is never inferred from Level; the native fixture deliberately uses Level45.

The existing two-button confirmation modal explains preservation of progress and creation of a backup, with **Đồng ý nâng cấp** and **Hủy / quay lại**. No modal framework was added. It pauses gameplay and owns hit testing. Cancel does not write a file or mutate canonical Hero/session; the player can return to the menu/select another slot. Viewing slot summaries does not upgrade a Save.

Accept rechecks canonical identity/full snapshot and source SHA. It creates/reuses only an exact verified immutable backup, stages/flushed bytes, commits atomically, validates the committed snapshot, records compatibility completion, then swaps canonical Hero/session. Only the chosen slot is upgraded. Repeated consent/Load cannot grant a second component or resources.

Backup names include the original SHA: `<slot>.cultivation-<sha>.legacy.bak`. CreateNew prevents accidental overwrite; existing identical backup is reusable, existing mismatched backup blocks migration. Backup hashes/JSON are re-read. Backups remain after success; no automatic retention deletion is introduced.

`<slot>.cultivation-migration.json` stores versioned compatibility transaction state and hashes, not a resource ledger. Prepared/Committed/RollbackPrepared/RolledBack support bounded recovery. A committed migration followed by an old binary stripping the cultivation component is rejected instead of silently initialized again. This cannot prevent an already-distributed old executable from writing; it prevents the new binary from interpreting that downgrade as untouched legacy progress.

The ordinary writer and migration share a checksum-bounded replacement helper after a real intermittent Windows1175 File.Replace failure was observed. It makes at most4 attempts with20ms spacing (maximum60ms delay), validating source/temp hashes each time. Ambiguous completion succeeds only for exact intended bytes; a newer/different slot is never overwritten. Identical ordinary Save still attempts a real commit, preserving explicit-save timestamp/latest-slot semantics. Persistent lock failure remains a failure with the committed original intact.

Post-write rollback is allowed only while the target equals this transaction's exact upgraded hash. `SaveManager.RestoreCultivationBackup(plan,expectedCurrentHash,...)` provides verified recovery and refuses a newer Save. Existing ordinary Manual Save rollback policy is retained.

| Owned fixture / scenario | Result and boundary |
|---|---|
| New / legacy / current / stale template | PASS; zero New, explicit legacy consent, current restore, stale template ignored |
| Old/different/future config; invalid/partial/truncated component | PASS; reject before canonical swap/overwrite |
| Consent reject / accept / repeated upgrade and Load | PASS; no reject writes, one component, no repeated bonus |
| Backup creation and existing exact/mismatched backup | PASS; byte-exact original, immutable, collision refusal |
| Temp creation failure / locked Windows Replace / orphan temp | PASS with actual filesystem blockers; original retained, next valid retry succeeds |
| Injected faults before/after backup/temp/replace/verification | PASS; six bounded fault points, original retained or exact guarded rollback |
| Source/snapshot mismatch; changed newer target | PASS; no overwrite/rollback over newer bytes or canonical mutation |
| Partial commit after Replace/before completion metadata | PASS modeled interrupted state; exact snapshot/backup hashes complete metadata only, without rewriting player bytes |
| Separate Unity process restart | PASS reloading the actually migrated ManualSave2, including its metadata/backups |
| Save while paused; fractional checkpoint; Travel; Retry/New | PASS in native and/or MainGame lifecycle checks |
| Manual rollback | PASS; selected snapshot semantics unchanged |
| Arbitrary process kill within syscalls / power-cut durability | NOT RUN; no universal power-loss guarantee |

No slot was deleted and no legacy player was forced to New Game. Tests use marker-owned directories configured before SaveManager Awake. Fault injection is unavailable for the default player directory. Raw fixture Saves, receipts, caches, logs and `.utmp` are not committed.

## World coordinates and Qi distribution

See [World Qi Distribution V1](../architecture/WORLD_QI_DISTRIBUTION_V1.md).

The actual indexed artwork is1448×1086. `WorldMapPolygonSeeds.txt` has top-left pixel coordinates for all133 stable IDs; `WorldMapCatalog.PreviewPosition` is `(sourceX/1448,1-sourceY/1086)`, bottom-left normalized origin. The current UI displays/selects polygon cells rather than133 independent pin GameObjects. Exact-aspect artwork, fog, polygons and outlines share one zoom/pan subtree. Screen-to-local conversion uses the actual image RectTransform; no hidden crop or extra normalized offset was applied.

Native checks verified all133 seed positions against runtime PreviewPosition, imported sprite dimensions and containment in their own actual click polygon. Export columns are `nodeId,sourceX,sourceY,normalizedX,normalizedY,coordinateStatus`. There are no missing/duplicate IDs. Provenance remains127 IMAGE_LABEL and6 INFERRED: TL-01, TL-17, TL-19, HT-13, TM-03, TM-13. All historical polygons remain `reviewed=false`; correct runtime registration is not a claim of manual geographic approval. No source position/topology was changed to fit Qi.

At initial handoff the user's unlabelled/illustrative heatmap images were not found in the task workspace. The debug overlay therefore uses the existing indexed map, without changing production artwork. After handoff the user supplied the illustrative heatmap; the additional audit below updates that input status without reinterpreting the accepted v1 data.

### Post-handoff supplied heatmap audit

The supplied `15378.png` is1280×960, SHA256 `35adb6412db808d8a9a44ec629449c5db754d71a76fdf78c1deeb472f636a6bb`, matching the source map's4:3 aspect. Top-left coordinate registration scales both axes by1280/1448 (960/1086). SIFT/affine RANSAC excluding frame/compass finds1290 interior inliers from1295 ratio matches across all four quadrants; median uniform-scale residual is0.633px. This supports spatial registration of the illustrated map while retaining the original six inferred-node provenance labels. Matched-feature accuracy is not a per-node error guarantee.

The reference now supplies visual regional guidance: warm palace/settlement clusters in the northwest, southwest, southeast and central lake area. No calibrated color legend exists and volcanic terrain color is not independently treated as Qi. No numerical Qi values were extracted from pixels. All133 stable IDs have been projected into reference pixel space in external `evidence/heatmap-reference-node-registration.csv`; current v1 tier/rate columns are explicitly separate from image interpretation (`qiExtractedFromImage=false`). Analysis/mapping statistics are in `evidence/heatmap-reference-registration.json`.

Only documentation changed in this follow-up. Runtime table/config versions, fingerprint, source coordinates, production gate, Save schema and accepted code/test results remain unchanged. The procedural v1 distribution is not claimed to match the newly supplied illustration. Applying the illustrated regional design to production requires an explicit new version and a compatible supported-v1 Save path; silently replacing v1 would violate the accepted configuration contract. See the updated World Qi contract for registration details.

Generator version1, seed20261106 uses private Python RNG, two-scale sinusoidal spatial fields, nine medium Gaussian clusters, two strong hotspots at actual TV-35/TV-03 coordinates, and three cold pockets TL-14/TL-29/TM-23. Smooth start-radius attenuation0.19 plus explicit provenance-bearing start overrides maintains natural local transitions. Values are clamped then quantized to the configured tiers. No per-node independent random rolls, Unity global Random consumption, New/Load/Travel reseeding, per-frame generation or runtime image reading.

| Tier | Qi/s | Label | Nodes |
|---|---:|---|---:|
|0|0.6|Khô cạn|31|
|1|0.8|Loãng|10|
|2|1.0|Bình thường|32|
|3|1.3|Khá|23|
|4|1.8|Dồi dào|8|
|5|2.5|Đậm đặc|16|
|6|3.5|Linh địa|9|
|7|5.0|Hotspot / Long mạch|4|
|Total|||133|

Min0.6, max5; four hotspots are3.01% of nodes. All133 entries include stable ID, normalized coordinates, tier/rate, manual override flag/provenance, seed and generator/config version. No NaN/Infinity/negative/missing fallback entry. Largest sampled near-pair rate difference for normalized distance<0.085 is1.5Qi/s, the5↔3.5 transition. All four5Qi/s nodes currently cluster in Tử Vân: this is explicit experimental distribution, not a claim that volcanic temperature implies Qi.

All five starts TV-20/TL-16/XD-11/TM-12/HT-17 are tier2,1Qi/s, with no hotspot override. Starting selection, links, waves, travel requirements and monsters are unchanged.

Reproduction: two fresh full exports are byte-identical to the runtime JSON; SHA256 `7a7a110f50453ba6730d5aef3e69155caff0e936a5c77510c01cd0a7bcc4eb31`. A version2 manual override survives two regenerations without rewriting its input. Unknown ID and changed data under an existing same-version export are rejected. Line endings are deterministic LF. Runtime reload preserves rates and consumes no Unity global Random values.

The committed generator exports complete coordinate/Qi CSVs, summary, generator parameters and optional debug PNG. Runtime data and the reproducible tool are committed; generated inspection artifacts are retained externally rather than installed into production art.

## Absorption, refinement and clock accounting

NodeQiRate is absolute environmental Qi/s. The adapter supplies `density=NodeQiRate/(2×1)` to the retained domain pipeline;5Qi/s therefore absorbs5, not10. Refinement stays capped at1Qi/s. Historical v0.8 playtest density1 absorbed2Qi/s; that historical profile/ID is untouched. Production start1Qi/s still reaches the same foundation thresholds in approximately60minutes but accumulates less reserve before bottleneck. This is an explicit new production source configuration, not a historical Save identity change.

There is one existing CultivationSession/clock owner. Travel settles pending fractional time against the old node before source transition; Load restores schema2 remainder exactly once under a fresh epoch. Pause/menu/entry/death/offline do not grant active time. Neither Level/EXP nor kills supply foundation. High-node excess Qi is retained and reserve continues growing at bottleneck.

Full3660-game-second native runtime simulations, not extrapolated30-second gameplay:

| Source | Absorption | Refinement cap | Viên mãn | Foundation at3660s | Reserve at3660s |
|---|---:|---:|---:|---:|---:|
| Each of the five starts |1/s|1/s|≈3600.1s|3600|≈60|
| TV-35 hot fixture |5/s|1/s|≈3600.1s|3600|≈14700|
| TM-24 cold fixture |0.6/s|1/s|Not reached|≈2196|0|

TV-20 is also tested at30/60/144FPS with deterministic repeat. The60FPS run performs1114 native checkpoint roundtrips; maximum listed accounting error across these runs is below1e-6Qi. Intermediate thresholds occur at≈900s/2100.1s. No automatic Trúc Cơ, Blood award or Level/EXP change. Hot-node refinement acceleration was not enabled or proposed as an approved economy change.

## Unified Hero visual and preserved combat

See [Unified Character Visual Contract](../architecture/UNIFIED_CHARACTER_VISUAL_CONTRACT.md).

Actual old swapping source: `CultivationSpriteBridge.AttachHero → Init(false,ToClass(controller.attackMode))`, selecting Swordsman/Archer/Mage and prototype Normal/Attack/Hit frames. Equipment presentation refresh, ApplyAttackMode and SpawnHero rebound it on Equip/Unequip/New/Load/Retry.

There is no separate Hero prefab here. MainGame's serialized Hero UI Image and the reused child `Cultivation Sprite Body` are the visual authority; no SpriteRenderer/Animator supplies the body. Character details are textual and no alternate class portrait was found. Combat does not require changing that bitmap.

Default selected: existing `CultivationSprites/Swordsman_Normal.png`, the existing New/Unarmed/Melee appearance. AttachHero now retains that exact Image/Sprite for Sword→Bow→Staff→Unarmed→Sword. Hero Attack/Hit presentation requests retain their deadlines and existing root feedback/SFX, while body pixels remain fixed. No new framework, controller, rig, spritesheet, Save field, renderer, visual class tint or artwork.

All old Archer/Mage/Swordsman sample assets remain, including Monster/fallback/fixture references. Monster presentation, projectile and hit/VFX assets are unchanged. Future action metadata can enter through the existing Attack/Hit/ResetPose boundary (WeaponFamily/type/optional skill/direction/target/state); body identity must remain independent and future animation must not silently become damage timing authority.

Actual primary-Hero native combat observations:

| Weapon | Existing resolved mechanics | Range in verifier layout | Damage events | Primary-owned projectile |
|---|---|---:|---:|---|
| Sword |Melee|67.2|2|No|
| Bow |RangedPhysical|691.2|6|Yes|
| Staff |RangedMagic|806.4|1|Yes, staffBasic|
| Unarmed |Melee|67.2|2|No|
| Sword again |Melee|67.2|2|No|

These are short fixture observations, not balance/DPS claims. The verifier measures actual monster HP loss and primary-owned projectile snapshots while the existing Combat.Update owns targeting/cooldown/damage. Staff basic `magic=false` is the existing legacy charged-flight flag; `staffBasic=true`/RangedMagic is the correct basic magical path. Combat code was not changed to satisfy the test.

Real Unity checks retain the same body Sprite/Image and exactly one body through weapons, Save/Load, Retry, Travel and New, with old object/binding disposal and correct logical Hero/combat/camera identity. No MissingReference/NullReference occurred in accepted runs. Mono visual assertions supplement, rather than replace, these GameObject checks.

## Acceptance matrix and evidence

All raw logs/evidence remain under the task workspace `C:\Users\hason\Documents\Codex\2026-11-06\ki-m-tra-phi-n-ki\v09`. This location is external to the source repository and is not a runtime dependency. Re-runnable native and MainGame verifiers are committed.

| Layer | Final result | Evidence |
|---|---|---|
| Pure domain / Mono |188 related regression cases PASS, matching baseline group counts;5 unified cases separately PASS (alias, not193 unique); targeted writer16+20 rerun PASS|`Regression-*.log`, `Mono-*.log`, `evidence/mono-counts.json`|
| Unity native Edit Mode |V09 nine acceptance groups PASS; historical V08 seven compatibility/filesystem groups PASS|`evidence/native-result.json`, `native-v08-result.json`|
| MainGame real Play Mode |All four phases and12 acceptance records PASS using default production config|`Play-final.log`, `evidence/play-result.json`|
| Fresh Unity process |Migrated ManualSave2 reload PASS, byte-identical checkpoint/no offline grant; unrelated phases correctly NOT RUN|`Play-restart-final.log`, `evidence/restart-result.json`|
| MainGame hot-cap UI/memory fixture |Phases1–2 PASS; unrelated phases3–4 NOT RUN|`Play-uicap-final.log`, `evidence/uicap-result.json`|
| Generator |Two full exports/hash equality/override/version/unknown guards PASS|`evidence/reproducibility.json`|
| Unity metadata/static check |0errors;3 preexisting missing folder-meta warnings for Lake/Sea/Steppe|Final metadata command output|
| Windows Development Player |NOT RUN; neither build nor native mouse acceptance|No Player evidence claimed|

The related Mono groups are cultivation balance3, gameplay5, dual progression19, realm-related16, progression UI4, cultivation-related20, weapon basic32, sprite28, unified/historical Archer flag5, enemy sprites6, basic movement11 and multi-zone39. Existing gear/forge/stat identity and camera/movement/world behavior are covered by these related tests, actual snapshot/lifecycle checks and protected-source comparison; no claim of an exhaustive unrelated gameplay suite.

Latest native profiles (all marker-owned beneath source `.utmp`, excluded from Git):

- V09: `cultivation-playtest-d014d00c018a473ab2c1dade7d5f27ff`.
- V08 compatibility: `cultivation-playtest-7ba109776c794af08a5e4789b6737ef6`. Its inherited messages about a closed production gate describe the historical profile scenario; they are not evidence that the current global gate is OFF.
- Full MainGame: `foundation-c9d2eef584f44598a2f385ae762c9efc`.
- Fresh process: `foundation-1cf7e985bfea4cac9fcf8b62fe3a70e3`.
- Hot-cap/lifecycle: `foundation-b6046f1ae3cd4b328c0d9e7aa9b7a9ce`.
- Performance: `cultivation-playtest-c0f31f8858f249d1b0be02884feb385c`.

Play result `sourceHead` is the baseline Git commit because implementation was verified as uncommitted source before making the requested functional commits. Hash-checked staged source was installed and then committed unchanged; it is not a claim that unmodified v0.8 passed v0.9 tests.

The full MainGame natural farm interval at actual HT-17 lasts30.00088 real seconds,30.006666611589 active/sampled seconds,10 damage events, Level1/EXP14, survived,3 snapshot writes. No Hero/gear/enemy/EXP manipulation occurs in that interval. Separate explicit fixtures clear combat for idle/moving checks, permit Travel for attribution, supply sample weapons, and inject writer failures. Their outcomes are not represented as natural progression or Windows mouse proof.

Save/Load retains normalized full equipment/inventory/forge snapshots and fractional cultivation checkpoint. Future-load failure preserves active Hero/session. Real defeat/Retry, menu/Continue/New and Travel pass. Travel attribution verifies old-source fractional settlement and the actual destination table entry; no double scheduler/tick. The expectation helper uses the existing legacy gear normalization and first asserts authoritative GearCore identity; a random legacy-metadata loot drop is not hidden by weakening combat/gear assertions.

UI was checked at600×338,800×450,1150×647,1360×680: actual TMP no-overflow checks and captured images for ordinary Character, consent and hot-cap states. Modal hit-test ownership prevents click-through. UI shows map Qi/s, absorption/s and refinement limit separately, foundation/bar/reserve/realm/stage/bottleneck, actual Level/EXP and Spiritual Energy, with Blood/Trial locked. No misleading Tu vi+5/s or unsupported ETA.

The hot-cap fixture is generated by the full3660s domain run then saved/loaded through actual MainGame, showing5Qi/s versus refinement≤1/s, foundation3600 and reserve≈14700. A real MainGame second continues reserve growth at cap. This is explicitly a fixture, not a claim of natural travel to a hotspot.

Inspection artifacts include `world-coordinates.csv`, `world-coordinates-native.csv`, `world-qi-v1.csv`, `qi-summary.json`, `generator-spec.json`, `world-qi-debug-overlay.png`, `simulation-*.json`, twelve Character/consent/hot-cap screenshots and four weapon screenshots in `v09/evidence`. Debug overlay and representative small-resolution/hot-cap/consent/Staff captures were visually inspected. It is a debug overlay, not production map art.

## Performance review

The v0.8 runtime publishes/serializes cultivation checkpoint every accepted frame. This was measured before optimizing. Existing behavior is retained to keep fractional Save/Load snapshots current; no speculative persistence rewrite was made.

`CultivationV09PerformanceVerifier.Run` measures256 Advances on Unity's main thread: approximately24.5316µs/Advance and1104bytes/Advance estimated managed-heap growth, with0GC collections in that bounded window. Editor background allocations can contribute. `GC.GetAllocatedBytesForCurrentThread` returned0 even for an explicit1MiB control allocation on this runtime, so its zero result is unusable. The older V09 aggregate result's `allocatedBytesPerFrame=0` is that unsupported counter, not evidence of zero allocation; the standalone controlled `performance.json` supersedes it. Its initial4.79µs timing is a different microbenchmark window, not the final allocation-window timing.

No GC mode was changed. These are Editor loop estimates, not whole-frame CPU time, exact thread allocation, a Player GC-pressure profile or long-session performance certification. The observed JSON publish cost merits later profiling/cache/batched-boundary evaluation. Any such change must flush before Save and preserve fractional state/source/epoch consistency; it was not needed to change gameplay in this batch.

The binding simulation writes no disk Saves. Actual natural MainGame writes3 snapshots in30seconds (existing10-second cadence), not every frame. Lifecycle CSV covers3Loads/Retry/New: heap range732–801MB including Editor/pools, runtime EntityData counts1–4, exactly1Hero body, writes1→1→1→1→2→3. Native retired Hero objects are destroyed, old bindings disposed and epochs fresh. The observations do not establish a memory leak or a leak-free Player guarantee.

## Remaining boundaries and v1.0 priorities

- BLOCKED: no unresolved blocker for the implemented Editor-verified production gate. Unsupported Save configurations intentionally remain rejected, not destructively migrated.
- PARTIAL: six inferred coordinate centers/all draft polygon review provenance; experimental hotspot economy/geographic review; Editor-only bounded performance/memory observations.
- NOT RUN: Windows Player build/native mouse acceptance, long Player profiler soak, arbitrary power-cut or mid-syscall process-kill injection. Existing UI method/hit tests do not replace those results.
- RELEASED: no. Default MainGame code is activated and verified in Unity; there is no Player/release deployment claim.

The three highest-value next tasks:

1. Build and accept a Windows Development Player: real mouse/keyboard consent and lifecycle matrix at the four resolutions, plus long-session CPU/allocation/GC/memory profiling using owned Saves.
2. Review the six inferred map centers and regional Qi economy/hotspot placement with the map author; evaluate absorption/refinement techniques in a simulator before any new versioned economy change.
3. Add a player-facing verified-backup recovery/retention workflow and broader supported-version migration fixtures; test real process-kill boundaries. If checkpoint profiling justifies batching, retain an explicit Save flush and exact fractional checkpoint contract.

Final handoff checks require a clean worktree, protected-source equality, exact functional/documentation commits and no pushed changes. Generated import-only metadata/TMP fallback changes were removed by a path/hash-checked allowlist; no player Save/cache/log/`.utmp` content is in the commits.
