# Heavenly Tribulation Foundation V1

07/11/2026 · Development v1.2 · **DOMAIN / FIXTURE IMPLEMENTED; PRODUCTION LOCKED**.

This contract extends the current foundation-based [Dual Progression V1](DUAL_PROGRESSION_CONTRACT_V1.md), [Capacity V1](CULTIVATION_CAPACITY_CONTRACT_V1.md) and [Save migration policy](CULTIVATION_SAVE_MIGRATION_POLICY_V1.md). Historical Level/entry-Level trial proposals are not authority. See [v1.2 report](../automation-reports/DEVELOPMENT_V12_REPORT.md) for executed tests and limitations.

## Authority and gates

`HeavenlyTribulationAttempt` is a plain in-memory domain object. No MonoBehaviour, GameManager caller, production Resources definition, trial UI, scheduler, actor spawn, camera change or Save field is added. `CultivationIntegrationGate.HeavenlyTribulationEnabled=false`; the production factory independently returns `Locked` with a null attempt. Changing the flag alone cannot enable trials.

Creation is available only through `TryCreateFixture` with a `TribulationFixturePolicy` whose ID starts with `fixture:`. Every number is required explicitly. The fixture's one-second endurance, two-second deadline, ten accepted damage and zero foundation transfer are test inputs, not approved balance. There is no RNG or probability API and no trial failure cost, injury, HP reset, reward, EXP or loot policy.

Eligibility delegates to the actual session's `CanAttemptBreakthrough`: actual foundation at its catalog ceiling, eligible injury and no Burning or pending liability. It requires an explicit known ascending successor and a valid explicit foundation transfer. Level, EXP and effective combat realm are not inputs. The production realm catalog remains Luyện Khí only with no successor.

## Two-phase fixture protocol

| State | Accepted operation | Result |
| --- | --- | --- |
| Endurance | Explicit bounded game-time steps and accepted incoming receipts | Complete only after the explicit fixture duration with living HP |
| Damage | Accepted outgoing receipts and explicit bounded game-time steps | Complete only when the explicit threshold is met before the fixture deadline |
| Resolving | Revalidate owner/epoch/attempt/life, actual realm revision, eligibility, living HP and pause | In-memory fixture success may call the existing `RealmBreakthroughTransition` |
| Committed / Failed / Cancelled | No progression/damage/time callbacks | Terminal; repeated success cannot apply twice |

No second clock is introduced. The host must supply monotonically increasing step IDs across phases; duplicate, invalid or oversized deltas do not change state. Pause consumes neither time nor receipts. Endurance completion resets only phase time. Incoming damage never fills the outgoing threshold. The fixture deadline is exclusive: time at or beyond the deadline fails; this ordering is not an approved production rule. Phase transitions are explicit and are not inferred from animations.

Each receipt contains owner, epoch, attempt, event ID, Hero life, target life, phase, nonnegative accepted damage and validated current/max HP. Duplicate events count once. Wrong owner/epoch/attempt/life/target/phase is rejected before mutation. Receipts are evidence supplied by an adapter, not cryptographic proof that combat happened. The future runtime adapter must own event provenance, accepted damage and pause ordering.

## Combat bridge

`TribulationFixtureCombatBridge` is compiled only for Editor/Development. It captures the exact `HeroCombatState`, data reference and `LifeVersion`; rebinding or respawning the same actor invalidates it. It reads actual current/max HP and accepts damage already resolved by the existing combat pipeline. It never writes HP, calculates attacks, adds modifiers or advances attack timers. Fixture checks use existing `EntityDataSO.GetCalculatedDamage`, `CombatStats` and `CombatDamage.ResolveBasicSnapshot` to demonstrate that Attack and Armor affect accepted evidence.

This is a fixture bridge, not an installed MainGame combat event subscriber, live trial target or two-phase encounter. Lifetime and canonical session provenance for a future production host still require integration acceptance.

## Commit, cancellation and persistence

Success requires both phases. Resolving does not mutate cultivation. `CommitFixtureSuccess` rechecks identity/revision, pause, living HP and current eligibility, then uses the existing transition with the attempt ID as the success event. A competing attempt becomes stale after the first realm revision changes. Reserve/energy are retained; no Level/EXP authority is touched. This transition is memory-only, never a claim of successful durable Save.

Death and the explicit fixture timeout mark `Failed` without applying an invented penalty. `Cancel` supports Load, NewGame, Retry, Travel, Menu, Disabled and Explicit causes and invalidates later callbacks. These are domain operations exercised by fixtures; they are not wired into production lifecycle. Cancellation remains possible after eligibility changes. No automatic retry or free-reset policy is inferred.

No schema/configuration version changes and no migration are needed for this foundation. Existing cultivation JSON and exact per-frame clock/checkpoint publication remain unchanged. No active attempt is serialized or silently resumed. No production attempt exists to bypass the unresolved mid-trial Save policy. Existing ordinary Save rollback and upgrade backup rules remain authoritative.

## Decisions required before production integration

1. Realm successor catalog, target realm thresholds/capacities and foundation/reserve transfer after success.
2. Endurance source/attack schedule, duration, survival criteria and eligible damage sources; damage-phase target, threshold/deadline and simultaneous death/timeout/success ordering.
3. Whether success has any randomness; if so its authored policy. No probability is assumed here.
4. Entry costs, consumption/refunds, failure penalties, HP/location/retry and cancellation policies; no zero-cost failure policy is approved by fixture non-mutation.
5. Save/Load, quit/crash, active-attempt resume/cancel and durable result transaction semantics. Failed disk commit must not announce a production breakthrough.
6. Interaction with Blood/Burning, injury, skills/modifiers and canonical combat lifetimes; source of difficulty must use approved combat rules.
7. MainGame entry/exit/pause/cancel/UI integration, Unity/Player regression and native-input acceptance before opening any production gate.

Fixture PASS does not approve these decisions or unlock Trúc Cơ, Blood, offline progress or any production trial.
