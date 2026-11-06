# Development v1.1 — Windows UI, Qi v2 and cultivation capacity

07/11/2026 · **IMPLEMENTED / VERIFIED / ACTIVE IN DEFAULT MAIN GAME SOURCE**. New Hero uses `production:cultivation-v11-capacity`/3, WorldQi2 and capacity catalog1. Older Saves retain their own rules until selected-slot consent. No release/push/deployment is claimed. **Native mouse/keyboard is BLOCKED / NOT RUN**, separately from successful native GameObject/TMP, MainGame, Windows gameplay and UI-handler tests.

## Baseline, authorization and commits

Repository `lavievn/IdleGame`, branch `feature/monster-awareness-ambush`, actual worktree `C:/Users/hason/.codex/worktrees/loot-vfx-validation/IdleGame`. Baseline HEAD `0c116df7e689cb71631b736517957f74bc50fb1a`, working tree CLEAN; six local v1.0 commits retained. No concurrent Unity/Player writer at baseline. Unity6000.5.0f1, Windows Build Support, Mono backend and MainGame-only build scene verified. Settings/packages unchanged. No fetch/pull/push/reset/rebase/merge or parallel agent/writer. User standing approval covered scoped work; **player migration consent remains mandatory**.

Relevant v0.8/v0.9/v1.0 reports/contracts, actual cultivation/Save/equipment/visual/world/UI source and existing verifiers were audited. Historical profiles/assets retained. Before edits:12 related Mono groups,188 PASS. Stages ran A→B→C→D with separate checkpoints.

| Commit | Change |
|---|---|
|`3255c22`|fix(ui): stabilize compact Inventory Forge captions and disable default layout guides|
|`15304ed`|feat(save): add consented versioned Qi upgrades with immutable history and clock provenance|
|`9c7016d`|feat(cultivation): add versioned actual-realm absorption and refinement capacities|
|`a58bb52`|feat(gameplay): bind capacity telemetry UI and verify canonical Hero lifecycle|
|`ab61678`|fix(save): retain transaction expected hash throughout rollback writes|

The final test/documentation commit adds this report, contracts and native test details; its SHA/final CLEAN status are in external `evidence/handoff.json` and `git log`. Final runtime build records HEAD `ab61678`; later changes are Editor-only test details/docs. Source and binary manifests are retained. Builds, logs, screenshots, test Saves/profiles/cache/.utmp are outside Git.

## A — UI and native-input status

`InventoryPanelUI.cs` reserves footer space when sizing equipment rows, chooses compact summary from its actual rectangle and applies bounded no-wrap/autosizing after generic styling. Visible captions shorten only; full names/details/data and hit rectangles remain. `UIManager.inventoryLayoutDebug` defaults false; MainGame serialized override changes exactly1→0. No artwork/prefab/item schema/Forge mechanics change.

Player assertions check summary above footer buttons, no active LayoutGuideLabel, default debug off, actual requested resolution and native TMP fit. Final UI-handler/screenshots pass600×338,800×450,1150×647,1360×680 for Character, Inventory/Equipment, Forge, Map and consent. Known Necklace/summary overflow and debug guides are resolved; final `uiIssues=[]`. MainGame verifies modal owns hit testing. This is not native-click evidence.

Computer Use initialization failed: `failed to write kernel assets: The system cannot find the path specified. (os error3)`. One reset/retry failed identically. No window was selected/clicked and no alternate input/security bypass was used. **Native mouse/keyboard: NOT RUN**. A prepared owned manual profile/checklist appears below; no development approval is pending.

## B — Frozen Qi v2 and supported compatibility

Resources WorldQi2 is byte-identical to `Tools/data/WorldQiDistributionV2.json`: ID `world-qi:atlas-v2`, version2, generator2, seed20261106; regenerated SHA256 `436ce1b997c56d0c8f93c0244b9595e56d602335bd198c5f09dcf31e7a8a93eb`. V1 remains `7a7a110f50453ba6730d5aef3e69155caff0e936a5c77510c01cd0a7bcc4eb31`.

133/133 stable IDs/coordinates remain unchanged. Pixels use top-left on1448×1086 art; normalized coordinates use bottom-left `(x/1448,1-y/1086)`. Pins/polygons/topology/artwork/RectTransforms unchanged. Provenance127 IMAGE_LABEL/6 INFERRED retained: TL-01/TL-17/TL-19/HT-13/TM-03/TM-13 are **not treated as author-approved**. Historical heatmap registration evidence is retained; v1.1 does not extract numerical Qi from colors or equate volcano temperature with Qi.

|Qi/s|0.6|0.8|1.0|1.3|1.8|2.5|3.5|5.0|
|---|---:|---:|---:|---:|---:|---:|---:|---:|
|V2 nodes|12|6|22|21|29|15|24|4|

TV-06/TL-18/TV-40/TM-25 are the four max-tier nodes. Five starts TV-20/TL-16/XD-11/TM-12/HT-17 remain1Qi/s with override provenance. Largest reported sampled near-pair difference1.7Qi/s. Full coordinates/rates/overrides/regions remain in Tools JSON/CSV and [WorldQi2 contract](../architecture/WORLD_QI_DISTRIBUTION_V2_PROPOSAL.md). No runtime reseeding/image sampling/Unity Random consumption, monster/loot/topology/travel change. Generator proposal/banner text remains historical inspection metadata; C# gates are runtime authority.

| Supported config | Component | Environment | Rules |
|---|---:|---|---|
|`production:cultivation-v09`/1|2|WorldQi1|historical absorption/refinement1|
|`production:cultivation-v11-qi-v2`/2|3|WorldQi2|historical absorption/refinement1|
|`production:cultivation-v11-capacity`/3|3|WorldQi2|capacity catalog1|

New/Legacy/Current/known older/unknown-different/future/malformed/template/failed commit/old-binary cases remain distinct. Resolver loads known older snapshots with their original rules. Forward1→2,1→3,2→3 reconstruct/validate sessions, not string-retag JSON. Actual realm/foundation/reserve/energy/injury/owner/fractional clock remain exact; other Level/EXP/points/roots/inventory/equipment/forge/map/wave JSON tokens remain exact. Unsupported nonzero Blood/Burning/pending settlement liability rejects safely rather than being deleted. No Level-derived cultivation or free Blood.

Character “Nâng cấp & tải ô lưu” targets the selected **saved snapshot**, with the existing two-button consent modal. It explains preservation/backup and that unsaved live progress is not silently saved. Preflight/Cancel do not write slot/backup/history or mutate canonical Hero. Accept rechecks disk/canonical, verifies immutable backup, archives old metadata, commits/validates then swaps Hero/binding. Other slots stay unchanged.

Legacy backup suffix `.legacy.bak` retained; config upgrades use `.snapshot.bak`, both original-SHA keyed. Receipt2 stores operation/source config and immutable prior receipt hash/name; schema1 remains readable. No backup expiry/overwrite. Rollback retains the transaction's expected upgraded hash throughout staging/replacement instead of adopting a competing newer file as overwrite permission. Manual Save rejects silent cross-config replacement; intentional manual rollback loads the selected old snapshot/config. AutoSave can mirror only the explicitly established canonical owner/config, including intentional rollback.

Native owned fixtures PASS: consent accept/reject, exact root/state/backup/history, repeated upgrade/Load, original recovery, candidate-before-swap, invalid/future/debt rejection, eight fault points, actual backup/temp blockers, mismatch, Windows locked Replace, orphan temp, source/metadata races, and post-Replace newer snapshot blocking rollback. Modeled interruption completes metadata only after snapshot/backup verification; **not universal power-loss/process-kill proof**. Separate Unity/Windows processes restore exact checkpoints without offline awards. Already shipped binaries that ignore fields cannot be universally repaired retroactively. No personal Save was a fixture or migrated by automation. See [migration policy](../architecture/CULTIVATION_SAVE_MIGRATION_POLICY_V1.md).

V2 activation followed native/data, MainGame default, Windows smoke and restart PASS, then a separate commit. Current ProductionEnabled/WorldQiV2Enabled/CapacityEnabled are true after their respective gates. Config3 is a new identity; config1/2 meaning remains unchanged. Burning/Trial/offline remain OFF. **Source activation is not RELEASED.**

## C — Capacity rules and native measurements

`CultivationCapacity.cs` implements immutable/versioned catalog and cultivation-only modifiers. `CultivationModel/Engine` accept the optional catalog; profile/config loader fingerprints it separately. Historical profile JSON shape stays unchanged; the catalog is passed separately to avoid native missing-field defaults. `CultivationGameplayState` exposes derived capacities/last-step telemetry and validates pending old-node provenance. `UIManager.Cultivation` uses the existing panel; MainGame/Windows verifiers extend the existing owned host.

Each fixed step evaluates actual realm/current foundation-derived stage/injury **at step start**. Absorb `min(environmental Qi/s, absorption capacity)*dt`; refine reserve **after absorption**, independently limited by refinement capacity and realm ceiling; debit consumed input once. Config3 yield1; injury affects capacities once. Historical no-catalog rules retain the old pipeline. Crossing step uses old-stage capacities, next step new stage. Foundation regression/healing recomputes capacities without stale bonuses. Bottleneck retains absorption/reserve but actual refinement0. Total Level/general combat buffs/Blood effective realm are not inputs.

|Luyện Khí stage|Absorption cap Qi/s|Refinement cap Qi/s|
|---|---:|---:|
|Sơ kỳ|1.2|1.00|
|Trung kỳ|1.35|1.08|
|Hậu kỳ|1.5|1.16|
|Viên mãn|1.65|1.24; actual0 at ceiling|

`capacity:cultivation-v1`/1 uses injury factors `max(0,1-.25*severity)` and `max(0,1-.5*severity)`. Modifier API: Root/Art/EquipmentOrTreasure, ordinal-ID sort, duplicate rejection, at most64 inputs, bounded flats/multipliers, additive multiplier combination clamped[0,4], maximum1000Qi/s validation bound. Production supplies none; no fabricated arts/items/root bonus. Arrays/input lists copied. Invalid/nonfinite/duplicate/decreasing/overflow catalogs reject.

Trúc Cơ3/2,Kim Đan6/4,Nguyên Anh12/8 are fixture bases only, not official balance/unlocked production realms. Native tests cover4stages each, major>minor growth, actual Trúc Cơ Tick, Blood combat-tier boost with unchanged capacities, debt-driven stage regression, injury/healing and deterministic modifiers. No new realm/Blood/Trial activation.

Full3660-game-second actual native session/binding simulations, zero reserve:

|Ambient Qi/s|Viên mãn time s|Foundation at3660s|Reserve at3660s|Conservation error|
|---|---:|---:|---:|---:|
|0.6|—|2196.000|0.000|0.00e+00|
|0.8|—|2928.000|0.000|0.00e+00|
|1.0|3600.1|3600.000|60.000|5.50e-11|
|1.3|3304.3|3600.000|1068.000|3.42e-09|
|1.8|3304.3|3600.000|1506.675|1.40e-09|
|2.5|3304.3|3600.000|1506.675|1.40e-09|
|3.5|3304.3|3600.000|1506.675|1.40e-09|
|5.0|3304.3|3600.000|1506.675|1.40e-09|

Five starts independently reach≈3600.1s; thresholds `[0,900,2100,3600]`/initial0 unchanged. Selected hotspot30/60/144FPS have identical final foundation/reserve, error<1e-6, native checkpoint restore included. The0.1s crossing observation is floating point, not an epsilon grant/threshold change. Full node/FPS rows: [native simulation CSV](data/CULTIVATION_V11_NATIVE_SIMULATIONS.csv).

Hot5Qi absorbs only1.2/s initially,1.5/s at Hậu. Rates≥1.8 share the no-modifier capacity bottleneck here. Existing reserve can support refinement in low ambient. Old-config stockpiles survive upgrade; no reserve wipe/cap/decay/new realm unlock. Future stockpile economy is not claimed fully balanced.

Schema3 adds pending clock source config ID/version to remainder/elapsed/node. First active Advance after upgrade settles old elapsed fraction once with old node/rules, then new time uses current rules. Consecutive1→2→3 before a tick retains the oldest pending source. Derived capacities are not Save authority. GameManager remains the sole scheduler. Pause/menu/death/entry/offline do not grant time; Save/Load/Travel/Retry do not replay it.

Character shows map Qi, absorption cap/actual, refinement cap/actual, reserve, foundation/bar, actual realm/stage/bottleneck, energy, locked Blood/Trial, independent Level/EXP. “Thực” is explicitly the **last successful step**,0 after Load before a new step. Two decimals preserve1.35/1.08. No ETA or map5→foundation5 claim. Existing panel fits all4sizes.

## Real MainGame and Windows evidence

MainGame B default4phases PASS: New/start, V1 Load asV1, real Character action/modal4sizes, Cancel, accepted failure/retry, exact owner/resources/.037old clock, selected-slot-only upgrade, real Update accounting/no EXP grant, Save/Load/manual V1 rollback and Retry/camera/combat identity. Separate Unity process restores exact V2.

MainGame C default4phases PASS: canonical New1.2/1, actual starting source. Unmodified5s TL-16 farm advances≈5.006 active seconds, absorption1, no actor overrides; no damage occurred in this short interval. Separately labeled owned899.95foundation/100reserve snapshot in a real5Qi node crosses Trung via **real Update**, capacities1.2→1.35 and1→1.08, no EXP grant. UI4sizes, exact Save/Load/fresh epoch, Travel with explicit clear-permission fixture, old fraction/destination source, Retry/New/no stale binding/camera identity PASS. Separate Unity process restores exact V3. Fixture progress is not naturally earned progress.

Final Windows build: MainGame/Mono2x/Development+debugging, **Succeeded,0errors,56warnings**, 674116298 bytes. Existing `IdleGameV10.exe` name is reused for the current v1.1 binary. Matrix11records PASS, capacity smoke PASS, restart PASS, `uiIssues=[]`. Final unmodified30s farm:≈29.995 active seconds,23damage events,Level1/EXP14,survived; no gear/HP/EXP/monster/loot/speed overrides during interval. Later explicit sample-gear/forced-wave fixtures prove Sword12damage events, Bow6+primary projectile, Staff1+primary projectile, Unarmed8, Sword2; same Swordsman_Normal Hero sprite and distinct attack modes. Repeated/future/config/malformed Load, legacy consent/backup, panels4sizes, Travel/SaveLoad/Retry/MenuContinue/New and canonical/camera identity pass. Actions remain **method-driven**, not native input.

Executable SHA256 `e3c3a181dcb8bab365f7dae4788c8f8164dd1dfa8a5e51d5a717c04b1fc3f7e5`; Assembly-CSharp SHA256 `740cd832c483b487913fb9d1de5a31bac587e78bac0734fb9069966f06ba2728`. Warnings are retained, not reported as zero; existing shutdown ComputeBuffer disposal warning is observed. No MissingReference/NullReference acceptance failure. No retail/IL2CPP/package release claim.

## D — Regression, performance and source protection

One final related Mono sweep188 PASS plus18 Inventory comparison executions. Groups: balance3, gameplay5, dual-progression19, realm-related16, progression-ui4, cultivation-related20, weapon-basic32, sprites28, archer-animation5, enemy-sprites6, movement11, multi-zone39. Cultivation-related includes Inventory/Equipment/multi-slot/Save rollback/Forge; realm-related includes manual-load UI/balance. Overlapping groups are not presented as206 unique assertions. Native V09 migration/clock/133-node history PASS, new native compatibility/capacity PASS, real MainGame and Player PASS.

Protected Git comparison PASS: Hero/Monster AI, combat/formulas/stats/balance, camera/environment/movement, sprite bridge, Equipment/Forge/loot, EntityDataSO/core GameManager, world IDs/polygons/coordinates/topology/artwork, v1 config/balance profile, frozen generators/overrides/v2 data, Unity settings/packages. Scene exception exactly inventoryLayoutDebug1→0. No assertion changed gameplay to force PASS; numerical1.2×1.125 uses1e-12 IEEE754 tolerance. Independent issues stayed within5fix/test cycles.

30s Player comparison, **whole game; not controlled per-component attribution**:

|Metric|Stage A|Final3|
|---|---:|---:|
|Frames|4321|4318|
|Mean frame /p95 ms|6.943 /6.938|6.948 /7.017|
|Mean CPU ms|6.937|6.941|
|Total allocation bytes/frame|9781|12895|
|Gen0 collections|36|50|
|Mean Advance µs|31.58|34.53|
|Snapshot writes|3|3|
|Heap before/after bytes|9031680 /10043392|9285632 /9416704|

Global allocation rises≈31.8%, Advance time≈9.3% in this single comparison. GC per-thread allocation control returns0, so per-call attribution is unsupported, recorded-1 rather than zero allocation. Native V09's0-byte figure shares that limitation. ProfilerRecorder global counters are valid. Checkpoint still publishes each accepted frame for exact Save state. Schema/provenance, UI and evaluations are plausible contributors, not isolated measured shares. Three writes are gameplay auto-snapshots, not per-frame file writes. Short heap snapshots do not prove leak freedom.

No persistence rewrite/cache/batched publication or timing optimization was made. Measure with supported markers before optimizing; any later batching must synchronously flush before Save and retain exact clock/checkpoint. This performance work remains PARTIAL, not hidden by functional PASS.

Final metadata878files,0errors,3 preexisting missing folder-meta warnings Lake/Sea/Steppe. Only15 import-generated metadata paths and TMP fallback atlas were cleaned using path/hash allowlists; feature metadata committed. No recursive deletion/original asset removal/personal Save access. No verifier process remains at handoff.

## Evidence and native manual handoff

External root: `C:/Users/hason/Documents/Codex/2026-11-06/ki-m-tra-phi-n-ki/v11/evidence`.

Key files: regression-summary.json; native-v09.json; native-v11-compatibility.json; native-v11-capacity.json; capacity-simulations.csv; stage-b/MainGame-default.json and MainGame-restart.json; stage-c/MainGame-default.json and MainGame-restart.json; player-matrix.json; player-capacity.json; player-restart.json; build-result.json; player-performance.json; player-binary-hashes.json; source-manifest.json; scope.json; metadata.json; removed-artifacts.json; commits.json; handoff.json. Raw B smoke/replay: v11/windows-b; C intermediate: v11/windows-c; final runs: v11/windows-final. No runtime dependency on these files.

Native MainGame Character captures: `stage-c/v11-capacity-character-600.png`, `...-800.png`, `...-1150.png`, `...-1360.png`. Final capacity Player profile has corresponding captures; final matrix has Character/Inventory/Forge/consent/weapons. The600px MainGame screenshot was visually inspected: separate5.0map/1.35abs/1.08ref rows with no footer/stat overlap. Screenshots do not imply native clicks.

Prepared manual profile, **not launched**: `C:\Users\hason\Documents\Codex\2026-11-06\ki-m-tra-phi-n-ki\v11\windows-final\profiles\idlegame-v10-295eb9bf578b4831860ea00b869e1268`. Exact build: `v11/windows-final/build/IdleGameV10.exe`, SHA above. Slot1 is a hash-copied owned V2 stage fixture; slot2 a hash-copied owned legacy fixture. Their hashes/sources are in manual-handoff.json; neither is personal/naturally earned progress.

Run `evidence/Launch-Owned-Manual.ps1` for that profile in observe-only mode. It installs owned Save isolation/non-personal default balance before Awake; no method-driven gameplay sequence runs. Manual checklist:

1. Native Menu/New/Continue, Pause/Resume, Character/stat allocation and all4sizes, distinct source/cap/actual rows.
2. Inventory/Equip/Unequip/Forge buttons/details, no overlap/debug labels/modal click-through at4sizes.
3. Load slot2 legacy: Cancel leaves file unchanged; Accept creates backup. Load slot1 V2: old rules stay until Character selected-slot consent; Cancel/Accept and backup similarly.
4. Map/Travel when allowed, Save/Load, Retry, Menu, exit/restart same profile, retained backups/checkpoint and one Hero body. Fixture stage/travel is not naturally farmed progress.

Native-input remains NOT RUN until human/working supported Computer Use performs it. No user approval is pending for completed phases.

## Status and next three tasks

IMPLEMENTED/VERIFIED: UI fixes,133-node Qi2, non-destructive upgrades/history/rollback, capacity engine, canonical gameplay/UI, regression. ACTIVE IN DEFAULT SOURCE: new-Hero3; old Saves opt-in. PARTIAL: long-run performance attribution/manual coverage. BLOCKED/NOT RUN: native mouse/keyboard due kernel initialization. NOT RUN: arbitrary power loss/kill timing, long leak study, retail/IL2CPP release. RELEASED: not claimed. Burning/Trial/offline/new realms/new skills/VFX/art/loot remain OFF/outside scope.

1. Complete prepared native-input checklist in a working Windows interaction environment, retaining cancel/backup/hash evidence.
2. Instrument allocation by component; optimize repeated UI/checkpoint work only after attribution, with exact Save flush/clock tests.
3. Review legacy stockpiles/capacity with authored cultivation modifiers/new-realm policy before real arts/items/breakthroughs; obtain author review of six inferred coordinates.
