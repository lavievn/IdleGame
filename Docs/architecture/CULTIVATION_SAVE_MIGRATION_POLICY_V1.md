# Cultivation Save Migration Policy V1 — v0.9 implementation

06/11/2026. Production configuration and consent migration are implemented. Default MainGame activation follows native/Play Mode gates; release/Windows Player status is stated separately in the v0.9 report. Personal Saves are never test fixtures. The original v0.8 OFF/blocked policy remains historical context in its report, not the current activation state.

## Classification and authority

`EntityDataSO.cultivationJson` remains an independent component. Level/EXP/stat points/roots/inventory/equipment/forge/world retain their existing authority. No cultivation is inferred from Level, and migration adds no Blood, injury, breakthrough or offline time.

| Source | Current behavior |
|---|---|
| New Hero | Ignore stale template component; new owner, Luyện Khí foundation/reserve/energy0 under validated production config |
| Legacy absent/empty component | Preview/read-only preflight allowed; playable Load blocked until explicit upgrade consent and verified commit |
| Current schema/config | Validate candidate first, restore state/remainder under fresh epoch, no repeated grants |
| Old or different configuration | Refuse; no ID/version retagging or absolute-foundation reinterpretation |
| Future component/config | Refuse Load/overwrite; preserve source and live state |
| Malformed/partial/null/duplicate/unknown cultivation fields | Reject before candidate swap; do not repair by deleting fields |
| Stale template | New discards; Load uses the validated source component, not template values |
| Prepared metadata with exact upgraded snapshot | Validate snapshot/backup hashes, complete compatibility metadata only; player file is not rewritten on Load |
| Committed migration followed by a componentless old-binary snapshot | Downgrade guard rejects reinitialization; preserve files for guarded recovery |

Historical v0.7 component1/profile and v0.8 component2/balance IDs remain distinct. Production uses component2 with its own `production:cultivation-v09` version1. No implicit converter for other configurations or future versions exists.

## Consent flow

Load/Continue detects a valid legacy slot, reads it without mutation, validates a candidate with existing SaveManager rules, creates an upgraded JSON candidate and prepares a cultivation binding. Every unrelated JSON root value is copied from the original token; only `cultivationJson` is added/replaced. Existing normal Load normalization rules for legacy equipment metadata are unchanged, not a new Forge/stat migration.

The existing StartMenu confirmation modal explains that Level/EXP/stats/equipment/progress stay, cultivation starts from0 and a backup is created. Buttons are **Đồng ý nâng cấp** and **Hủy / quay lại**. It owns hit testing and pauses time while pending. Only this selected slot is upgraded. Decline destroys the candidate and restores prior pause state without disk writes or dirtying the live Hero. Viewing slot summaries never starts a migration.

Accept rechecks both live canonical identity/snapshot and disk source hash, verifies backup, commits the candidate, verifies committed bytes and semantic validity, records completion, and only then swaps canonical Hero/session. Failures retain the running Hero/session. A later click cannot reuse a retired callback or duplicate a component. Existing title/manual-load/AutoSave behavior resumes after successful commit; other manual slots are not mass upgraded.

## Files and retention

For slot `ManualSave1`, backup is `ManualSave1.cultivation-<originalSHA256>.legacy.bak` in its Save directory. It is written with CreateNew and flushed before replacement. Existing exact backup can be reused after failure; mismatched backup is never overwritten. Re-read SHA and JSON validation prove it represents the already-validated original. Backup bytes remain immutable and are not deleted on success. No automatic backup cleanup/rotation/retention expiry is introduced.

`ManualSave1.cultivation-migration.json` is a versioned compatibility transaction record: slot, config ID/version, original/upgraded hashes, backup filename and state. It contains no resources, clock awards or alternate progression timeline. States are Prepared/Committed/RollbackPrepared/RolledBack. Unknown/future/malformed metadata fails closed. Normal manual Save rollback remains slot-local; this record protects migration compatibility rather than selecting a newer gameplay snapshot.

New Game retains existing ManualSave1/2 semantics. AutoSave reset still owns the automatic slot only; retained migration backup/metadata are not silently deleted. A later different snapshot cannot be overwritten by an old rollback plan because its expected hash no longer matches.

## Atomic commit and recovery

The current source SHA is checked before commit and again at the replacement boundary. Temp bytes are flushed, then Windows File.Replace/File.Move commits. Migration verifies final bytes/semantic candidate before marking Committed. Ordinary Save keeps its existing schema guards/temp writer and shares the checksum-checked replacement helper, added after observed transient Windows1175 errors. This is a bounded reliability fix, not a persistence rewrite.

Replacement retries at most4 times with20ms spacing (60ms total delay), rechecking target/staged hashes. An ambiguous error is considered completed only if the exact desired snapshot is present; a different/newer target is never overwritten. Identical ordinary snapshots still attempt a real commit so explicit Save timestamp/latest-slot behavior remains as before; an existing lock cannot be disguised as success merely because bytes already match. Persistent failure returns failure with the prior committed file retained.

If a post-Replace verification step fails, rollback writes the exact original **only if the current target still equals this transaction's upgraded hash**. A newer/different target blocks automatic rollback; immutable backup remains. `SaveManager.RestoreCultivationBackup(plan,expectedCurrentHash,...)` is the guarded recovery API, tested against both exact originals and newer snapshots. It is not an automatic overwrite of a player's later progress.

On restart, Prepared metadata plus the exact upgraded snapshot and exact backup proves replacement finished before completion metadata. Load can complete metadata without changing the player snapshot; a modeled interruption test covers this state. Real process restart verifies a migrated slot/metadata/backup in a new Unity process. This does not prove arbitrary power loss or kill timing within every filesystem syscall, and an old binary that ignores new fields cannot be made universally safe retroactively.

## Clock and configuration

Component2 keeps clockRemainder/clockElapsed/clockNodeId with canonical resources. Every accepted frame publishes its checkpoint, including fractions smaller than0.1s; Save sees the latest published state. Load gets a fresh epoch but restores remainder once. Travel settles pending time at its original source before changing map. Pause/menu/entry/death/offline do not grant time. Bounds and invariant-token double parsing from v0.8 remain.

Production world Qi data is versioned and fingerprinted; production config version advances with world Qi table version. Seed/generator/tier/source changes must not keep the same Save identity/version. Unsupported historical/future configurations are refused instead of auto-rebased. Burning/debt/charges/receipts remain unavailable in this gameplay profile; no outstanding liability is dropped to make a Save load.

Native owned fixtures cover consent reject/accept, byte-exact backup/rollback, repeated upgrade/load, config/future/invalid/template, source mismatch, injected failures, real backup/temp blockers and locked Replace, orphan temp and interrupted metadata. MainGame verifies the actual modal, candidate-before-swap, failure/retry, pause/Travel/Retry/New and independent Level/EXP. All test directories are marker-owned, configured before Awake; no player data is read, written or deleted by automation.
