# Development v1.0 — Windows verification and world cultivation balance

06/11/2026 · BÁCH LUYỆN TIÊN ĐỒ · Stages executed sequentially: A concluded PARTIAL, then B, then final cross-stage regression. An additional owned-context DEVB isolation fix was found during final review and verified with a new Player run/restart and targeted UI regression. A and B were not run in parallel.

## Outcome

| Area | Classification | Actual result |
|---|---|---|
|Windows Development build|IMPLEMENTED / VERIFIED|MainGame Windows x64 Mono2x, Development+Script Debugging, actual Player executed|
|Native mouse/keyboard|NOT RUN / BLOCKED|Computer Use kernel initialization failed, including reset/retry; no physical input proof|
|Save migration|VERIFIED fixtures|Native Windows filesystem tests plus actual Player method-driven consent, immutable backup, candidate Load and fresh-process restart|
|Natural gameplay|PARTIAL|Actual unmodified30-second Player farm after method-driven New; native launch/menu inputs unverified|
|UI|PARTIAL|Four actual client sizes captured; Character footer fixed; Inventory TMP flags remain|
|Performance|MEASURED, bounded|Actual Player main-thread/frame/GC/memory/Advance/save metrics; exact per-Advance allocation unsupported|
|World Qi v2|IMPLEMENTED / VERIFIED data|133 entries, four geographically spread hotspots, deterministic exports/native validator|
|V2 activation|BLOCKED intentionally|No supported v1→v2 configuration migration; production stays v1|
|A/B/C refinement|IMPLEMENTED / VERIFIED offline|45 native full3660s simulations and clock/replay cases; production remains A|
|Final functional regression|VERIFIED|188 Mono, native v0.9 nine groups, MainGame four phases; targeted UI4 after final Player-only safety fix|
|Release/deployment|NOT RELEASED|No upload, distribution, signing or deployment; prototype status unchanged|

Existing v0.9 cultivation remains enabled under its validated v1 identity. No new release gate, v2, B/C model, Burning, Heavenly Trial or offline progression was activated. Standing authorization covers scoped development operations; explicit in-game legacy migration consent remains implemented and required.

## Repository, baseline and commits

Actual root `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`, repository `lavievn/IdleGame`, branch `feature/monster-awareness-ambush`. Initial HEAD `53df3ff750ebd7f90da0ffa6e5db0acf2f49f9f7`, CLEAN and aligned with the existing local tracking reference. Unity6000.5.0f1 and current packages/backend were retained. No other Unity/Mono/Python/bash writer/build lock was found at baseline; Codex host processes were identified separately. No pull/fetch/push/reset/rebase/merge occurred in v1.0.

Required v0.9 report, World Qi, migration, visual, balance and dual-progression contracts, current verifiers, MainGame probe and source were audited. Historical v0.8 playtest descriptions are not current production authority. Baseline related regression was188 PASS; native v0.9 nine-group verifier also passed before v1.0 edits. Baseline logs are retained separately from final regression.

| Commit | Scope |
|---|---|
|`de08192792cc0dd8889262f6d156e9f9d9afb4c3`|Stage A owned Windows build/launcher, runtime acceptance/profiling and manual checklist|
|`58038dbebbc7b899d35d2cd2df55d180f7b6999d`|Stage A Character footer overflow fix|
|`ea6e16ce481bcb1a2a37d1666f3b18a4aaa2741f`|Stage B separate v2 field/generator/data/proposal|
|`a193362ecb8987ea06a3f3e399a76c14d853fc68`|Stage B native policy simulator, result CSV and contract|
|`2ddbebe5f23ec84813b12455b9e9dd7e6b5dff80`|Final safety review: isolate owned Player from personal DEVB preferences|
|Following documentation commit|This final report; final HEAD/CLEAN status recorded externally in `v10/evidence/handoff.json`|

Functional source HEAD is `2ddbebe5f23ec84813b12455b9e9dd7e6b5dff80`. The final documentation commit necessarily follows this report's source stamp. Verification artifacts record the actual source commits/hashes without a self-referential report hash. Final handoff checks enforce CLEAN status and no pushed changes.

## Stage A — build readiness and owned Player

Windows Build Support is present in Unity6000.5.0f1. Global scene list enables only `Assets/Scenes/MainGame.unity`; the build method checks this list rather than silently replacing it. Build uses StandaloneWindows64, Mono2x, `BuildOptions.Development|AllowDebugging`; no auto-connect/deep profiling or release distribution. Build code is Editor-only; runtime acceptance has no Assets/Editor dependency.

Current Player settings: company PeG/product TT1,800×450 default, windowed mode3, resizableWindow0, runInBackground1, activeInputHandler1 (Input System, package1.19.0). The existing TransparentWindow native controller still owns transparency, pointer routing and asynchronous resize. No Player settings, Unity version, package, camera or native window-controller code was changed.

Final build result: **Succeeded**,0errors,55warnings,674009902bytes,10.594s incremental build. Warnings include existing obsolete Unity/TMP APIs and development diagnostics; this is not a zero-warning project. Output:

`C:\Users\hason\Documents\Codex\2026-11-06\ki-m-tra-phi-n-ki\v10\windows\build\IdleGameV10.exe`

EXE SHA256 `e3c3a181dcb8bab365f7dae4788c8f8164dd1dfa8a5e51d5a717c04b1fc3f7e5`; managed Assembly-CSharp SHA256 `5a7aadb11af0849ffae8e0b040f33da68654af5c09f8c832256d8795e7b42214`. The full build stays outside source/Git. No new package/toolchain installation or Unity Hub intervention was needed.

`Tools/Run-WindowsAcceptanceV10.ps1` builds/launches only explicit owned output profiles. It refuses an unowned nonempty output root, checks markers/reparse paths and creates fresh GUID profiles. `WindowsDevelopmentAcceptanceV10.ConfigureBeforeAwake` is called before SaveManager's Awake initialization **only in Windows Development builds**. Requested invalid contexts quit/fail before default Save I/O; normal product path remains `Application.persistentDataPath/Saves`. The test does not switch the product's permanent Save location.

Final matrix profile:

`v10/windows/profiles/idlegame-v10-6f0038f0ece342ba8c2fd6eacba8cd7a`

Actual Save directory is that profile's `saves` child, logged as `SAVE_DIRECTORY_BEFORE_AWAKE`. The profile and its parent have owner markers; root/saves/evidence ancestors reject reparse paths. An actual Player launched with an intentionally invalid fixture marker exited2, never configured a SaveDirectory and created no saves folder. No personal Save was used or migrated.

Final review found the ordinary UI startup also reads `persistentDataPath/dev_balance.jsonl`. Earlier Player runs could inherit those preferences and are excluded from the final default-balance baseline. The safety fix skips that loader and resets static DEVB profiles before UI Awake in the **explicit owned Player context**, leaving ordinary product/Editor behavior and CombatBalance source unchanged. Final Player log confirms `OWNED_DEFAULT_BALANCE no personal DEVB read/write`; final Character/Inventory show baseline Physical10, not the earlier preferences-dependent1000. No personal DEVB file was inspected/edited/deleted by the fix.

## Native input and UI boundary

The Computer Use skill was read and its supported node kernel initialized. Both the initial call and one reset/retry failed: `failed to write kernel assets: The system cannot find the path specified. (os error 3)`. This occurred before app/window selection. No native clicks were attempted, no Chrome Legacy Window was mistaken for Player, and no alternate OS/helper/security bypass was used. **NATIVE INPUT NOT RUN.**

The committed [manual acceptance checklist](WINDOWS_V10_MANUAL_ACCEPTANCE.md) provides an observe-only owned Player launch and the requested menu/New/Stats/Inventory/Equipment/Forge/WorldMap/Travel/Pause/Save/Load/Menu/Continue/legacy/error/restart input matrix. It remains unchecked, not a PASS certificate.

Player fixture actions invoke existing GameManager methods and, for panel controls, existing UI hit-test/click handlers. They are explicitly programmatic support, not native mouse proof. Actual Player frames were captured at600×338,800×450,1150×647,1360×680. Menu, Character, consent, Inventory/Equipment, Forge and World Map images exist for each size. Fixtures wait for the existing native resize to settle before setting client resolution and refresh the canvas. Earlier asynchronous harness timing failures were corrected without modifying window/camera/gameplay code or dropping hit-test assertions.

Two existing Character footer captions wrapped/clipped in24px controls. The UI-only fix makes their12px captions single-line; final Character images at all four sizes have no flagged overflow. No damage/stat allocation behavior was changed.

**Remaining UI debt:** final Player reports eight TMP flags, representing the necklace-slot caption at800/1150/1360 and the Inventory summary at800, repeated while Forge is open over its Inventory underlay. The default Inventory debug layout labels/boxes also remain visible. These observations are retained, not filtered away to claim a clean UI. Inventory/Forge readability and a complete native no-click-through/no-missing-buttons/error-layout matrix are not accepted. Dedicated invalid-load feedback bounds are not separately certified. Stage A therefore remains PARTIAL even though the method-driven lifecycle matrix completed.

## Actual Player gameplay, migration and lifecycle

Final unmodified farm after method-driven New:30.005863 real seconds,30.001357 active game seconds,30 damage events, Level1/EXP16, survived. No gear/HP/monster/EXP/root/speed manipulation occurs inside that interval. EXP came from real combat; cultivation came from the sole existing GameManager clock. A level-up was not reached during this short final run. This is real Player gameplay, but not a fully native-input play session or60-minute realtime test.

Afterward, explicit sample-gear/forced-wave fixtures verify actual primary-Hero damage and primary-owned projectiles:

| Sequence | Resolved mode | Damage events | Primary projectile | Body |
|---|---|---:|---|---|
|Sword|Melee|10|No|Same Swordsman_Normal|
|Bow|RangedPhysical|1|Yes|Same|
|Staff|RangedMagic|1|Yes, staffBasic|Same|
|Unarmed|Melee|2|No|Same|
|Sword|Melee|8|No|Same|

These are fixture observations, not DPS/balance claims. Staff's existing `magic=false` flag selects ordinary basic flight; `staffBasic=true`/RangedMagic is the actual magical basic path. Existing combat owns range/cooldown/target/damage; no formula/AI/source was changed. One body persists through the matrix, with old native Hero destruction, retired bindings and correct combat/camera identities on Load/Retry/Travel/New.

| Save/lifecycle case | Evidence and result |
|---|---|
|New/current/repeated Load|Player PASS; zero New and exact fractional checkpoint/full normalized snapshot; native Hero retired|
|Legacy preflight/cancel/accept|Player method-fixture PASS; actual modal rendered; cancel no-write/live-state mutation; verified commit before swap|
|Duplicate consent/Load|Player PASS; one component, zero migration awards, immutable backup|
|Future/malformed/different config|Player PASS; candidate rejected without replacing active Hero/session|
|Old config/stale template/backup mismatch/downgrade|Native v0.9 owned Windows fixtures PASS; no unsupported converter added|
|Actual temp/backup creation blocker/locked Replace/source mismatch|Native v0.9 actual Windows filesystem fixtures PASS; original/newer slot preserved|
|Orphan temp and interrupted receipt|Native PASS; modeled interruption explicitly distinct from actual process-kill timing|
|Pause/Travel/Save/Load/Retry/Menu/Continue/New|Player PASS; old-source settlement, correct source after active tick, fresh clock on New and exact chosen snapshots|
|Manual rollback|Native/MainGame/Player snapshots retain chosen-slot semantics|
|Separate OS process restart|Actual Windows Player PASS with migrated ManualSave2/metadata/backup; byte-identical checkpoint, no offline award|
|Mid-syscall process kill/power loss|NOT RUN; no universal durability guarantee|

Final Player backup SHA256 `d1c823a42b6b2a8e9c119c6b8484162493508b6a52d0cea0a19987503c986822`; receipt is Committed with production v1 identity. Backup retains the full componentless legacy snapshot; repeated Load does not change it. Restart profile is `idlegame-v10-6692968d9d914f56bb1e41233e0593c8`, a separate Player process. Raw Save fixtures/receipts are not committed.

One duplicate fixture launch caused by a shell quoting failure was stopped only after verifying its exact owned PID/executable/profile; it is recorded as ABORTED, not acceptance or mid-write crash proof. No user Unity Editor/unrelated process was killed. Bounded fixes corrected fixture ordering and reused existing semantics; assertions were not relaxed.

## Player performance and stability

[Unity ProfilerRecorder](https://docs.unity.com/en-us/engine/6000.0/script-reference/unity/profiling/profilerrecorder) supports Player counters; the values below are actual instrumented Development Player observations, not the older Editor microbenchmark.

| Final30-second window | Measurement |
|---|---:|
|Sampled frames / actual Advance calls|4322 /4322|
|Mean frame wall time|6.943ms|
|Frame p95|6.938ms|
|Mean Main Thread marker|6.935ms|
|Mean timed actual Cultivation.Advance|32.344µs|
|Whole-frame GC Allocated In Frame mean|10801.61bytes|
|Gen0 collections|37|
|Managed heap before/after farm|9228288 /10113024bytes|
|Successful snapshot writes during farm|3, existing10-second cadence|

The Main Thread marker includes normal frame waiting; this is not isolated active CPU utilization. Whole-frame allocations include the game/probe/development environment and are not attributed entirely to cultivation. `GC.GetAllocatedBytesForCurrentThread` returned0 for an explicit1MiB control allocation in this Player, so exact per-Advance allocation is unsupported (`-1` in evidence), not zero. No unsupported counter is used to certify allocation-free behavior.

The existing per-accepted-frame cultivation JSON checkpoint publish is retained. No batching/cache/performance optimization was made: Advance time is measured, but allocation attribution and longer stability data are insufficient for a safe persistence rewrite. No before/after speedup is claimed. A future optimization must flush canonical state before Save, retain fractional time, settle Travel at the old source and retire stale epochs.

Player lifecycle observations cover3Loads/Retry/New: managed heap9297920–12165120bytes at samples, EntityData object counts4–6 including templates/fixtures, exactly one Hero body, snapshot-write counts4→5→5→5→13→16. Old loaded native Hero objects are destroyed; the data are not a leak-free long-session certificate. Native/GPU/private-process memory and a long profiler soak were not measured.

Logs also contain existing-style disabled VideoPlayer pause messages, Unity background TLS errors and a ComputeBuffer cleanup warning at shutdown. These were not fixed by changing security/rendering/gameplay settings. Accepted runs have no unhandled fixture exception; the messages and lack of a long soak keep stability/release conclusions bounded.

No Save writer/rollback algorithm or gameplay performance result was changed. SaveManager additions are a Development-owned path hook and measurement counter; the existing migration/checkpoint contracts remain applicable without a schema update.

## Stage B — data, geography and compatibility

See [World Qi v2 proposal](../architecture/WORLD_QI_DISTRIBUTION_V2_PROPOSAL.md) and [refinement models](../architecture/CULTIVATION_REFINEMENT_MODELS_V1.md).

The supplied heatmap and registered map coordinate frame are reused as regional visual guidance. All133 current coordinates,127 IMAGE_LABEL/6 INFERRED provenance and five starts remain unchanged. No inferred coordinate was treated as user-approved. No numerical Qi was extracted from terrain/volcano colors.

V2: ID `world-qi:atlas-v2`, version2, generator2, seed20261106; SHA256 `436ce1b997c56d0c8f93c0244b9595e56d602335bd198c5f09dcf31e7a8a93eb`. Four proposed max-tier centers: TV-06 northwest, TL-18 central lake, TV-40 southwest, TM-25 southeast. They span three catalog regions, versus all four v1 hotspots in Tử Vân. These are coordinate-based proposal choices, not official long-mai/lore rewards.

Tier counts0→7: v1 `[31,10,32,23,8,16,9,4]`; v2 `[12,6,22,21,29,15,24,4]`. Min0.6/max5Qi/s, four max-tier nodes (3.01%); every start remains1Qi/s. Largest sampled near-pair delta1.7Qi/s is reported, not claimed identical to v1's1.5. Unweighted mean1.461654→1.917293Qi/s indicates a larger potential reserve supply and requires economic review, not automatic activation.

Generator uses continuous seeded background/medium Gaussians, four coordinate-selected hot clusters, cold basins and smooth start buffers. No Unity Random/global gameplay seed, runtime generation or second scheduler. Two fresh exports match the reviewed bytes; versioned manual overrides survive regeneration. Unknown IDs/noninteger seed/NaN field/same-version reviewed changes reject. V1 files remain byte-identical. Full133-node JSON/CSV, region summary, inputs, proposed config, tool and native validator are committed; the debug overlay is external and visually inspected.

`CultivationProductionV2.proposal.json` validates only when explicitly supplied to owned native fixtures. Default Resources and `CultivationProductionConfiguration.ResourceName` still select v1. Native v1 Save/Load succeeds; wrong v2 component Load/write is refused before mutation. Stable node IDs provide the geographic mapping, but a supported dual-config resolver plus consent/backup/rollback/fractional-boundary/restart upgrade transaction is not implemented. **V2 READY — ACTIVATION BLOCKED.** No fingerprint/config version was changed in place.

## A/B/C economy comparison

All45 full3660s native simulations passed, including six rates per model at30/60FPS with native checkpoints,1/5Qi extremes at144FPS and three C-capacity1.5 sensitivities. Pause,0.037s replay, stale owner and old/new Travel attribution pass per model. Baseline Hero has no item/art capacity bonus; Level/EXP/Blood/realm do not change from Qi. Foundation/reserve timelines and exact stage times are available; no3600s outcome is extrapolated from the short Player farm.

| Model | At1Qi/s | At5Qi/s | Reserve at5Qi/s after60min |
|---|---:|---:|---:|
|A, refinement cap1|≈3600.1s to cap|≈3600.1s|≈14400|
|B, bounded ambient bonus≤20%|≈3600.1s|≈3035.2s|≈14400|
|C, baseline capacity1|≈3600.1s|≈3600.1s|≈14400|
|C sensitivity capacity1.5|≈3600.1s|≈2400s|≈14400|

B medium1.8/2.5/3.5Qi cap times≈3325.2/3196.0/3097.6s. All baseline models at0.6Qi have foundation≈2160 at60min and no cap by3660s. At3660s,5Qi reserve≈14700. Earlier cap does not remove stockpiling because capped foundation consumes the same3600Qi. C sensitivity illustrates capacity versus supply, not a technique granted to a player.

Recommend keeping A now and considering C as the future capacity-driven extension once character/art/root authority and reserve policy are designed. B remains an optional experiment requiring explicit model selection, not enabled by default authorization. The editor-only policy API is implemented for fixtures, not a production dynamic refinement framework. No root/technique formula, reserve cap/decay, monster/drop/danger/encounter/loot/travel reward was added. Risk/reward dimensions remain separate; portable reserve/new-realm stockpile feedback needs a future policy decision.

## Final regression, source protection and evidence

Final full related Mono run:188 PASS (same group counts as baseline). Native v0.9 nine groups PASS covers migration/version/schema/clock/Qi. MainGame real Play Mode four phases/12 records PASS: idle/moving/combat absorption, pause, UI rates, Save/Load/Retry/Travel/Menu/New and unified body/three combat modes. The final DEVB safety fix affects only explicit Windows Development contexts; after it, Player was rebuilt and its matrix/guard/restart and targeted UI4 rerun passed. Editor's preprocessed loader behavior remains unchanged. The extra4 are reruns, not192 unique cases.

Protected comparison is PASS for Engine/Model, AI/combat/formulas/camera/movement/root/Equipment/Forge/loot, core GameManager, EntityDataSO/schema, visual bridge/assets, world IDs/topology/coordinates/artwork, v1 configs/generator/overrides, Unity settings/version and packages. No protected behavior was changed to make a fixture pass. Final metadata check:854files,0errors,3 preexisting missing folder-meta warnings for Lake/Sea/Steppe. Generated import-only metadata, TMP fallback atlas changes and Python bytecode were removed by exact path/hash allowlists; raw Saves/logs/cache/builds/screenshots are not committed.

External evidence root:

`C:\Users\hason\Documents\Codex\2026-11-06\ki-m-tra-phi-n-ki\v10\evidence`

Key files: `build-result.json`, `player-binary-hashes.json`, `player-result.json`, `player-restart-result.json`, `player-performance.json`, `player-lifecycle.csv`, `player-file-integrity.json`, `owned-guard.json`, `stage-a-status.json`, `world-balance-native-result.json`, `v2-reproducibility.json`, `refinement-v10-timeline.csv`, `world-qi-v2-debug-overlay.png`, `final-native-v09.json`, `final-main-game-result.json`, `stage-c-status.json`, `scope.json`, `commits.json`, `handoff.json`. Captures include actual Player panels/weapons and separately prefixed Editor images. Re-runnable build/native/simulator code is committed; inspection data are not runtime dependencies.

## Blockers and next three tasks

1. Restore a reliable supported Computer Use/native input environment or execute the owned manual checklist. Resolve the remaining Inventory captions/summary/debug-layout presentation and verify all four sizes, modal routing and error views with actual Player input.
2. Implement and test an opt-in, lossless v1→v2 configuration upgrade/dual resolver with immutable backup/guarded rollback/restart, then review the v2 supply increase and regional progression before activation.
3. Select the future capacity/stockpile model with authored root/technique inputs and new-realm reserve policy; run a longer Player allocation/GC/memory profile before deciding whether checkpoint serialization batching is justified.

Stage A is PARTIAL, native input BLOCKED/NOT RUN, UI/stability acceptance remains bounded. Stage B data/simulation are VERIFIED fixtures with activation BLOCKED. Final functional regression is PASS. **No RELEASED claim, push or deployment.**
