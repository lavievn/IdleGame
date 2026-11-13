# Dual progression — Cloud data audit

```bash
bash Tests~/DualProgressionAudit/run.sh
bash Tests~/DualProgressionAudit/run.sh --simulate /tmp/dual-progression.csv
```

C# 7.2+, Mono and its System.Web.Extensions assembly are required. `CSC` and
`MONO` may point to existing executable/compiler wrapper paths. No Unity Editor,
license, package installation or player save is required.

The runner compiles the production cultivation engine/model, capacities,
breakthrough transition, Blood/debt/recovery, component codec, gameplay binding,
configuration validation, World Qi, EntityDataSO EXP and stat dependencies.
It reuses the existing Core/Blood/Breakthrough/Persistence tests (19 groups) and
adds 10 audit groups, including two failing-before/fixed-after DTO reuse cases.
No production progression formula is substituted with a mock. The environment
adapter only satisfies an unrelated signature and throws if crossed.

Configuration JSON is read from Resources by .NET JavaScriptSerializer into the
actual production DTOs, then passed through WorldQiDistribution.TryCreate and
CultivationProductionConfiguration.TryCreate. The Unity Resources/JsonUtility
loader is not executed. Component JSON uses the repository UnityStub; candidate
and binding logic is production. These results do not certify native Unity
serialization, SaveManager disk transactions, actual combat or Player lifecycle.

`--simulate` drives the same Advance/LastStepFlow pipeline measured by the native
CultivationV11CapacityVerifier. It records all 8 existing WorldQi tiers, all 5
starts, 10/30 FPS start/hot cases, repeated in-memory checkpoints and historical
config1/2 starts. Each measurement simulates 3660 active gameplay seconds. CSV
time-to-cap is 0 when the cap was not reached inside that window. Execution wall
seconds are the accelerated test duration, not player time or an offline award.
Only the optional CSV is written; no SaveManager or file-based save fixture runs.

PASS describes verified existing behavior. Explicit `GAP` output and the report
keep absent EXP bank/release, Mastery, retail successors/Blood/trials, within-node
spatial Qi and automatic injury treatment separate from implemented features.
New realm/Blood numeric values in domain cases are labeled fixtures and do not
modify Resources, production thresholds/rates or combat balance.

See `Docs/automation-reports/CLOUD_DUAL_PROGRESSION_AUDIT_REPORT.md` for the
requirement matrix, source locations, discrepancies and native acceptance work.
