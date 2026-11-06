# Cultivation refinement models v1 — bounded native comparison

06/11/2026 · **IMPLEMENTED / VERIFIED offline fixtures; production remains Model A.** Model B/C are not selected or activated for production. No existing root, equipment, EXP, technique, combat or realm-breakthrough rule is changed.

## Policy/API and authorities

`Assets/Editor/CultivationRefinementModelsV10.cs` exposes `ResolveInputCapacity(model,ambientQi,characterCapacity,techniqueMultiplier)` only to Editor tools. Parameters are finite/bounded and unsupported models are rejected. `Tools/data/CultivationRefinementModelsV1.json` identifies the versioned fixture policies. The production CultivationEngine, GameManager scheduler, config and Save schema do not call this API.

Every baseline simulation uses a new level1 Hero, no item/art effects, zero foundation/reserve, the existing Luyện Khí catalog[0,900,2100,3600], efficiency1, step0.1s and the actual domain/session/gameplay binding. Qi supply is absolute environmental Qi/s. At a fixed source:

`absorbed = sourceQi × dt`

`refined input ≤ min(available reserve, input capacity × dt)`

`foundation gain ≤ remaining foundation to3600`

Domain code, not a projected time formula, owns these operations. Blood/Trial/offline remain locked; Level/EXP never enter the policy. Reserve remains portable and continues accumulating at bottleneck.

| Model | Fixture capacity rule | Meaning |
|---|---|---|
|A — reserve-only|1Qi/s|Current production: source quality increases reserve; baseline refinement remains1/s|
|B — limited ambient|`1 × [1 +0.2 × (1-exp(-max(0,q-1)/1.5))]`|At most20% asymptotic ambient capacity bonus, never5×; source still limits actual input|
|C — capacity-driven|`characterCapacity × techniqueMultiplier`|Baseline1×1. Future character/art/root policy would provide explicit capacity, with map supplying input; no ambient bonus in this fixture|

B's formula and20% are experimental parameters, not approval of a production economy. C does not invent a mapping from current roots/Level to capacity: baseline capacity is1. A hypothetical capacity1.5 sensitivity illustrates a future parameter only, without granting a technique or altering a player.

## Native simulation and clock boundaries

`WorldCultivationBalanceV10Verifier.RunNative` runs45 full3660-game-second simulations: six rates×three models at30/60FPS, native checkpoint restore every997 frames on60FPS,1/5Qi extremes at144FPS, plus three C-capacity1.5 sensitivity runs. Each30FPS run exports a60-second foundation/reserve timeline. All five v2 starting rates are validated as1; the1Qi simulation uses a representative actual node with those identical rules, not a claim of five separate activated-v2 New sessions.

Cross-FPS terminal resources agree within1e-6Qi; cap crossing agrees within0.11s fixed-step sampling. Native domain code can observe thresholds at900/2100.1/3600.1 due to floating-point accumulation; no epsilon resource grant or assertion relaxation is used. A zero crossing field in raw output means not reached by3660s, not instantaneous breakthrough.

Each model also checks pause no-award, native fractional0.037s capture/restore, fresh epoch/retired callback rejection and old-source Travel settlement. For the offline B experiment the immutable source-specific rules are rebuilt only after settling the old binding, restoring the same snapshot into a fresh epoch with the same full policy identity. The new source then pays its own elapsed time exactly once. This fixture boundary is not a production dynamic-refinement adapter or config migration.

Policy fixture identity includes world/policy content hash and character capacity, so changing a sensitivity parameter is not a silent configuration retag. Current v1 Save compatibility and wrong-v2 Load/write rejection are tested separately. Actual durable filesystem migration evidence remains the v0.9/v1.0 Player/native migration matrix; JSON model roundtrips are not presented as independent disk durability proof.

## Baseline results

Times below are game seconds at30FPS, rounded to0.1s. `—` means not reached within3660s. A and baseline C have identical results.

| Qi/s | A/C Trung | A/C Hậu | A/C Viên mãn | B Trung | B Hậu | B Viên mãn | Reserve at60min, all baseline models |
|---:|---:|---:|---:|---:|---:|---:|---:|
|0.6|1500.1|3500.1|—|1500.1|3500.1|—|0|
|1.0|900.0|2100.1|3600.1|900.0|2100.1|3600.1|0|
|1.8|900.0|2100.1|3600.1|831.3|1939.7|3325.2|2880|
|2.5|900.0|2100.1|3600.1|799.0|1864.4|3196.0|5400|
|3.5|900.0|2100.1|3600.1|774.4|1806.9|3097.6|9000|
|5.0|900.0|2100.1|3600.1|758.8|1770.6|3035.2|14400|

Foundation at60min is≈2160 for0.6Qi/s and≈3600 for every other baseline case. By3660s, low foundation≈2196; other cases are capped at3600. Start reserve≈60 and5Qi reserve≈14700 at3660s. B reaches cap earlier (5Qi:≈50.59min), yet its60-minute reserve is the same because all capped models consumed the same3600Qi total. Faster cap does not remove stockpiling.

C sensitivity with capacity1.5 still needs≈3600.1s at1Qi/s because supply is limiting; at2.5/5Qi it reaches Trung≈600.1s, Hậu≈1400s and Viên mãn≈2400s. It does not reach cap in12minutes merely because source is5Qi/s. This is the distinction between capacity and supply.

Exact45-run results/checkpoint counts/accounting are committed in [the summary CSV](../../Tools/data/CultivationRefinementSimulationV1Summary.csv). Full native result/timelines are external evidence under the task's `v10/evidence`. No player Save is a simulation input.

## Recommendation and risks

Keep A in production. Prefer C as the future extension direction because it preserves the role of authored character/technique capacity and leaves rich maps as a supply advantage. Baseline C intentionally equals A until such progression is designed. This is a recommendation, not a user selection or implementation of arts/roots. B can be evaluated as a bounded synergy only after explicit model selection and capacity/Save policy review; it should not be enabled merely to make hotspot time-to-cap look better.

Unbounded portable reserve is the largest economy risk. A5Qi node banks≈14400Qi in one hour at current cap. B does not solve that stockpile, and future higher capacity/new realms could consume it rapidly. Traveling from a rich node to a safer one with reserve is currently permitted behavior; no new danger, drop, travel restriction, reserve cap, decay or reset is introduced here.

Any future capacity change must preserve resource conservation, fractional old-source settlement and one clock; save the authoritative character/technique inputs, never derive them from Level or transient UI. Consider feedback loops where foundation unlocks capacity that produces more foundation, repeated equip/Load or source changes, and new-realm entry with large inherited reserve. Design and fixture-test the intended policy before activating B/C or v2. The existing production profile remains its own immutable identity/version.
