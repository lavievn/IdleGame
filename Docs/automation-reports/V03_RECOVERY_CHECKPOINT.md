# v0.3 recovery checkpoint

Unity6000.5.0f1 profile `foundation-b869075a092d4bf98e841e304fddf7a9`: PASS two checks, failure empty. Copy only ManualSave1 from marker-owned UI profile into a fresh GUID before Awake; source file never rewritten. Reset only process-static DEVB; no personal settings/save access.

Real GameManager.LoadAndContinue→entry→Combat.Update produces a natural damage trace with restored Physical source22 and current camera/Hero binding. SaveManual2→LoadManual2 snapshot matches level/EXP/points/allocations, inventory, gear/forge JSON, roots/map and derived HP/damage. Fixture defeat then actual Retry returns to natural attacks with same equipment/allocated data and current binding. No direct attack handler used for either post-Load/post-Retry attack proof; defeat itself is explicit fixture damage.

| Requested state | Newly executed evidence |
|---|---|
| Save after Level / allocation | UI fixture level9 and four Physical allocations, snapshot roundtrip; second world snapshot also includes HP allocation6 |
| Save after gear / equipped / Forge | Bounded farm five real SaveManager file roundtrips including equipped forged reward items and complete JSON/history; Player AutoSave contains one natural gear |
| Save after wave clear / Travel | UI/world fixture clears two nodes by accepted lethal callbacks, explicit Travel/re-entry, real ManualSave2/Load snapshot |
| Load→Combat / Load→Retry | This recovery run, actual timed attacks and binding checks |
| Old-version compatibility | NOT RUN again; existing compatibility tests/source unchanged; no schema/migration/checkpoint policy edits |

No claim that one fresh native Player input session exercised this entire matrix; HUD targeting remains BLOCKED. Live HP/attack timers/projectiles are not newly serialized. Does not generalize future-version guards to all future schemas. Logs: `work/stabilization-v03/recovery-round1.log`; owned evidence result/snapshots remain outside Git. Only Editor verifier changed, so the completed Player build still represents runtime HEAD.
