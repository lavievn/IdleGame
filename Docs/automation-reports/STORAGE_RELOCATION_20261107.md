# IdleGame storage relocation — 07/11/2026

Moved the active development worktree, historical clone, automation/build evidence and TT1 user-data directory off C. The active project is **D:/IdleGame/Project**. Baseline feature HEAD was `8155439c92c4415bd836ddfd4e3c5ec7e6bf2c5b`; Git registered worktree path was repaired to D without reset/rebase/merge/push. The main/common Git repository remains on G, as before; the other G/E worktrees were not relocated. The independent historical clone retains HEAD `a13aebd2cbd53e2ddb5460a1ade604f4354ab08e` and all10 preexisting untracked meta files.

## Actual locations

| Data | Canonical location |
|---|---|
|Active feature worktree, Library, project fixtures|D:/IdleGame/Project|
|Historical GitHub clone (kept separate)|D:/IdleGame/Archives/GitHub-IdleGame|
|Current automation, v0.9/v1.0/v1.1 evidence/builds|D:/IdleGame/Automation/2026-11-06|
|Previous automation|D:/IdleGame/Automation/2026-11-05|
|TT1 player files, including original backup folders|D:/IdleGame/PlayerData/PeG/TT1|
|Future builds|D:/IdleGame/Builds/WindowsAcceptance|
|Future process temp / logs|D:/IdleGame/Temp and D:/IdleGame/Logs|
|UPM cache|D:/IdleGame/Caches/upm|
|New Unity project creation default|D:/IdleGame/Projects|

Copied **101,265 files / 9.967 GiB logical data**, comparing source/destination SHA256 for every file before cutover. Then Git state and D launch tests passed before C backup cleanup. Observed C free-space increase was 11.238 GiB; observed remaining space 18.983 GiB. Volume changes include filesystem allocation/background activity, not only logical byte counts.

Old C project/clone/prior-workspace/TT1 paths are NTFS junctions to D. The active Codex conversation holds its original workspace root open, preventing a root rename. All subdirectories there were moved and replaced by junctions; only 23 small root files remain (322,694 bytes) plus the routing AGENTS.md. New work uses D directly, avoiding owned-fixture rejection of junction ancestors. No large duplicate project/build remains on C. Small existing shared UPM cache (~9MiB), Unity installation/licensing/system app data are not deleted or moved blindly.

## Future startup and configuration

Desktop shortcut **IdleGame (D)** launches `D:/IdleGame/Launch.ps1 -Mode Editor`. The repository tool resolves an old C junction to the canonical D project, sets process TEMP/TMP/TMPDIR and UPM cache to D, and defaults build output to D. It supports Editor/Check/Mono/Native/Build/Player/Replay/Observe. Examples:

```powershell
pwsh -File D:/IdleGame/Launch.ps1 -Mode Editor
pwsh -File D:/IdleGame/Launch.ps1 -Mode Build
pwsh -File D:/IdleGame/Launch.ps1 -Mode Player -PlayerMode Capacity
pwsh -File D:/IdleGame/Launch.ps1 -Mode Mono -Group cultivation-related
```

`IDLEGAME_HOME` and `UPM_CACHE_ROOT` user environment variables are set to D. The UPM setting affects subsequent Unity processes for this Windows user; the launcher also explicitly sets it. Global Windows TEMP/TMP are unchanged; only IdleGame child processes use the D temp directory. Existing ~9MiB package cache was copied to D, not destructively cleared. Unity Hub's active worktree entry and relocated historical clone were updated, other existing project entries retained, and new-project directory changed to D. Hub JSON originals are backed up in the migration directory. Unity Editor remains6000.5.0f1 in Program Files on C; no editor/package/backend upgrade was made. Cache mechanism: [Unity Package Manager configuration](https://docs.unity.com/en-us/engine/6000.6/manual/packages-list/package-manager-caches/upm-config-cache).

Current operational automation/helpers/Mono wrappers and manual launcher were repointed to D. Historical reports/hash manifests/raw evidence retain original C provenance. Project AGENTS.md and the old/current automation workspace routing notes direct future agents to D. These compatibility links do not bypass sandbox permissions; future sessions must open D as their workspace or use normal authorized escalation.

## Preservation and verification

- Git active HEAD/branch/index/source remained unchanged during cutover. Only the new storage launcher, AGENTS instructions and this report are committed afterward; no gameplay/Save schema migration.
- Historical clone's10 untracked files preserved, never merged/deleted. Full .git/history was copied and validated. Active worktree's common Git remains at G; D is not a standalone replacement for that common repository.
- TT1's original25 files were byte-exact after copy. Save/backup/dev settings and other non-Unity files (10) remain byte-exact after launch tests. No Save was parsed/loaded/upgraded as a fixture. Unity itself updated/removed 10 analytics files while running owned validation; the complete original25-file snapshot is preserved byte-exact at `D:/IdleGame/Migration/2026-11-07/Original-TT1-before-relocation`, outside runtime fixture paths.
- Mono cultivation-related20 PASS from D with temp on D. Launcher PowerShell AST parse PASS.
- Unity batch compilation PASS/exit0; log explicitly records project D:/IdleGame/Project, package resolution PASS. Native CultivationV11CapacityVerifier PASS with newly created owned `.utmp` profile on D.
- Actual Windows Development Player binary on D replayed a relocated owned V3 snapshot in a fresh owned D profile; exact checkpoint/no offline grant PASS, uiIssues empty. Method-driven replay; native mouse/keyboard NOT RUN.
- No personal Save used for verification. No Unity/Player writer left active. Only known15 import-generated environment metas/TMP atlas changes were cleaned; original assets/scene/Guid/configs are preserved.

Migration audit/evidence/backups of small metadata are at **D:/IdleGame/Migration/2026-11-07**: plan.json, five per-file hash manifests, copy logs, worktree-gitdir-before.txt, workspace-shell.json, operational-path-changes.json, original Hub JSON, mono-check.log, native-launch.txt, player-replay.txt, personal-data-byte-exact.json, generated-import-cleanup.json, completed.json and final-state.json. Unity check/native logs are under D:/IdleGame/Logs. Full redundant C copies were deleted only after source hash recheck and D launch validation; no recursive deletion targeted a junction or computed path outside the five explicitly approved backups.
