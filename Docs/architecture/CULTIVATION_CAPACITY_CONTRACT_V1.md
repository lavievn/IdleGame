# Cultivation Capacity Contract V1

07/11/2026 · Implemented in v1.1. Current production configuration is `production:cultivation-v11-capacity`, version3, component schema3, World Qi `world-qi:atlas-v2` version2. Activation evidence and limitations are in [DEVELOPMENT_V11_REPORT](../automation-reports/DEVELOPMENT_V11_REPORT.md). This source activation is not a published release.

## Authority and pipeline

The canonical `CultivationGameplayBinding` retains its one session and the existing GameManager scaled-time clock. Environmental supply comes from the saved configuration's stable-node table. Capacity comes from **actual cultivation realm, current foundation-derived minor stage, injury and an explicit cultivation catalog**. Neither Total Level, EXP, general combat stats nor Blood's effective combat realm is an input.

For a fixed step of duration `dt`, evaluate capacity from the state at the **start** of that step:

```
supply = node Qi/s                         // historical density adapter coefficient2
absorbed = min(supply, absorptionCapacity) * dt
available = previousReserve + absorbed
input = min(available, refinementCapacity * dt)
gain = min(realmFoundationCeiling - previousFoundation, input * yield)
consumed = yield > 0 ? gain / yield : 0
newReserve = available - consumed
newFoundation = previousFoundation + gain
```

For config3, yield=1. Injury reduces the capacities, once; the old refinement-yield injury coefficient is0. Spiritual-energy rules are unchanged. Historical configs1/2 have no capacity catalog and retain their existing supply/refinement1 pipeline exactly. No second source of Qi, second debit, kill grant, offline clock or automatic breakthrough exists.

At a stage crossing, the crossing step keeps its starting capacities. The next step evaluates the new stage. A final-step ceiling can reduce actual refinement to0 while absorption continues into reserve. Low ambient supply never becomes free Qi. Existing reserve can feed refinement above ambient supply. Regression of foundation or injury/healing is reflected on the next evaluation; no permanent stage bonus or derived-capacity cache survives it.

## Versioned production catalog

`Assets/Resources/CultivationCapacityV1.json`: ID `capacity:cultivation-v1`, version1. The production envelope fingerprints its canonical UTF-8/LF text separately from the World Qi fingerprint. Rules receive the validated catalog as an explicit argument, outside the historical profile JSON shape; old profiles are not reinterpreted by missing-field defaults.

Production has only Luyện Khí:

| Stage | Foundation threshold | Absorption capacity Qi/s | Refinement input capacity Qi/s |
|---|---:|---:|---:|
|Sơ kỳ|0|1.2|1.00|
|Trung kỳ|900|1.35|1.08|
|Hậu kỳ|2100|1.5|1.16|
|Viên mãn/bình cảnh|3600|1.65|1.24|

Base capacities1.2/1.0 multiply stage factors `[1,1.125,1.25,1.375]` and `[1,1.08,1.16,1.24]`. Injury severity `s` multiplies absorption by `max(0,1-.25*s)` and refinement by `max(0,1-.5*s)`. Values are finite, nonnegative and limited to catalog maximum1000Qi/s; this validation bound is not a progression reward. Duplicate realms, nonfinite/overflow rates, invalid/decreasing stage factors and unsupported realms fail validation.

Fixture-only major realms use base3/2,6/4,12/8 for Trúc Cơ/Kim Đan/Nguyên Anh. They prove the catalog and runtime can support a major jump larger than minor growth. They are not production balance, are absent from Resources production realm catalogs, and unlock no breakthrough/trial/Blood.

## Cultivation-only extension

Immutable `CultivationCapacityModifier` accepts a stable ID and source `Root`, `CultivationArt`, or `EquipmentOrTreasure`. Production supplies no modifiers; no new root/item/art authority or bonus is fabricated.

At most64 modifiers are sorted by ordinal stable ID; duplicate IDs are rejected. Flat inputs are bounded[-1000,1000], individual multipliers[0,4]. Combination is deterministic:

```
combinedMultiplier = clamp(1 + sum(multiplier - 1), 0, 4)
capacity = min(catalogMax, max(0, base * stageFactor + sum(flat))
               * combinedMultiplier) * injuryFactor
```

It does not multiply an unbounded chain. Caller inputs and catalog arrays are copied/immutable. This extension currently has fixture coverage only; future owned modifiers need authority/save/config policy before gameplay integration. No effective-combat-realm/general buff adapter is provided.

## Save, upgrade and lifecycle

Config1 is `production:cultivation-v09`/1/component2/WorldQi1. Config2 is `production:cultivation-v11-qi-v2`/2/component3/WorldQi2 without capacities. Config3 is a separate identity and does not change either historical meaning. Known older Saves load with their original profiles; adding a catalog is never an implicit ordinary Save.

Forward1→2,1→3 and2→3 conversions use the same consent/preflight/immutable-backup/guarded-commit workflow. Actual realm, foundation, reserve, energy, injury, owner and clock are preserved exactly. Unsupported Blood/Burning/pending liability is rejected, not discarded. Other root JSON tokens remain exact. See [migration policy](CULTIVATION_SAVE_MIGRATION_POLICY_V1.md).

Component3 adds `clockConfigurationId/Version` to component2's remainder/elapsed/node. It stores the source of already elapsed fractional time, not derived capacities. An upgrade does not first award pending time or reinterpret it with the new rates. On the next active advance, the old fraction settles once under the recorded old rules/node, then new elapsed time uses current rules. Two upgrades without an intervening tick retain the oldest pending source. Save/Load/pause/restart do not award time; a new epoch prevents stale callbacks.

Travel validates permission then settles the old-source fraction before changing source. Reserve/foundation remain. A stage change happens only at a simulation boundary; elapsed time is never replayed with new capacity. New creates a fresh owner/zero state; Retry/Load reconstruct from the selected checkpoint. Manual Save rollback continues to select the saved snapshot/config, not the latest live progress.

## UI and evidence

Character keeps the existing panel and shows Level/EXP separately; actual realm/stage/bottleneck, foundation/bar, reserve, map Qi/s, absorption cap/actual rate, refinement cap/actual rate and spiritual energy. “Thực” means **the last successful simulation step**, explicitly labeled; it can be0 immediately after Load before a new step. Capacities are recomputed from current state. No ETA, fake art/item bonus or `Tu vi+5/s` claim is displayed.

Native verifier `CultivationV11CapacityVerifier.RunNative` runs the actual session and binding for3660 game-seconds at all8 environmental tiers,5 starting nodes, selected30/60/144FPS and checkpoint restore. Starts reach cap≈3600.1s. Hot5Qi reaches cap≈3304.3s but only absorbs1.2Qi/s initially; at3660s reserve≈1506.675 instead of retaining all environmental supply. Full measured rows are committed in the report data CSV.

MainGame and Windows Player proofs use real canonical Hero, Update, UI and existing Save/Load/Retry/Travel. Stage-boundary/high-Qi setups and Travel permissions are explicitly owned fixtures. Short unmodified farm evidence is separately recorded. Simulated60minutes, UI-handler calls and screenshots are not native-input proof. Burning, Heavenly Trial, new realms, offline progression, damage, loot, Forge, camera and world topology remain outside this change.
