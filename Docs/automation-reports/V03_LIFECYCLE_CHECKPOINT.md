# v0.3 lifecycle checkpoint

New `--stabilization-lifecycle`: PASS3/3, first run. Primary Bow interval .4→.3, Pause2s leaves .3 and no projectile; Resume.31 releases exactly one. This is primary basic-attack countdown, not a newly added casting mechanic. Both same-frame callback orders exercised: Monster death before owner defeat commits exactly one reward; owner defeat first invalidates later victim damage/death, commits none. Repeated callbacks cannot add gear; outgoing projectiles are cleared. No runtime Combat/receipt changes.

Direct camera-affected existing suites newly executed once: camera-entry38, weapon-basic31, loot-visuals23, all PASS. Existing released-shot target-death/pool-reuse/cancel/map-change/owner-death, equipment swap snapshots, duplicate receipt and effect cleanup are reused rather than duplicated. Historical awareness Pause and manual Load projectile cleanup coverage was inspected; unchanged groups not rerun. No claim that every input permutation or all combat modes were played natively.

`work/stabilization-v03/lifecycle-round1.log` retains output. `Tests~/StabilizationFarmRegression.cs` and selector in `Tests~/MotionRegression.cs` only; no damage formula, AI, save schema or loot config edits.
