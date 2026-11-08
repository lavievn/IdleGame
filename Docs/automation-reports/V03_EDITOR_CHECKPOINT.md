# v0.3 Editor / lifecycle checkpoint

Source runtime camera `10ebdf8`, branch unchanged, local commits only.

| Task | New evidence | Scope / limits |
|---|---|---|
| 02 Awareness | CSV natural baseline + bounded HP-hold scene; visible/suspicious/engaged/first damage measured | Actual Editor play, not native input; no AI/range/spawn/DEVB-file edits |
| 04 UI | 600×338,800×450,1150×647,1360×680: Character long name, single allocation mutation, Inventory expanded, equip/unequip, Forge and Map close | Real hit-test/click **routing**, not SendInput. Gear/material/EXP seeded in owned fixture; no production income claim. Item names remain catalog names; arbitrary long item title NOT RUN |
| 05 World | Blocked neighbor Travel leaves Hero JSON unchanged; two nodes cleared by accepted lethal callbacks; explicit adjacent Travel, cleared-node re-entry, Save/Load, fixture defeat/Retry | Ten wave lifecycles, not natural distance/time-to-clear. No boss/SuggestedLevel/EXP formula change |
| 06 Lifecycle | New run camera-entry38/38, weapon-basic31/31, loot-visuals23/23 | Includes stale/dead/pooled projectile, equipment replacement snapshot, owner death/cancel, death receipt and visual cleanup. No full unrelated Magic run. Simultaneous Hero/Monster death and all windup edges not separately instrumented this run |
| 07 Progression | Four resolution passes each spend exactly one point; nonzero allocation/level/gear roundtrip; post-Load allocation affects new canonical Hero only | APIs/formulas unchanged; historical DEVB/realm boundary/New Reset coverage reviewed, not relabeled as new PASS |
| 09 Recovery | Save after level/Physical allocation/gear, Load from Forge modal; Save after HP allocation/two clear nodes/Travel, Load→combat binding→fixture defeat→Retry | Camera references match current Hero after Load/Travel/Retry. Old-save compatibility/checkpoint-policy suites unchanged and not rerun |

Unity6000.5.0f1 complete profile: `.utmp/foundation-cfd3181ed0004ed5b4849f96714f1934/evidence/result.json`; empty failure and six checks. Four Character and four Forge screenshots saved. New GUI run uses owned GUID/marker and inactive SaveManager configuration before Awake; no personal save or DEVB file access. No runtime UI/Save/Combat fixes required by this evidence.

Fixture preparation failures preserved in logs: UI draft incorrectly checked factory input object after `EquipmentRuntime.AddInstance` (which stores a copy); fixed lookup by instance ID, retaining equip assertion. Camera telemetry originally followed pooled old life; now filters SpawnVersion. Stop fixture originally lacked counter body due text replacement; actual unchanged6s/.002 assertion finally executed and passed. Runtime camera root causes and pure tests are in CAMERA_V03_CHECKPOINT.

Evidence paths: `work/stabilization-v03/ui-world-round1.log` (fixture FAIL), `ui-world-round2.log` (PASS), `camera-entry.log`, `weapon-basic.log`, `loot-visuals.log`; selected screenshots/CSV committed under `Docs/automation-reports/evidence/stabilization-v03`. No native Player acceptance implied.
