# Camera recovery v0.3 — checkpoint

Branch `feature/monster-awareness-ambush`; source `c6db586`. No push.

Two runtime corrections: directional movement feed-forward plus bounded positional feedback in `MultiZoneCamera`; sample Hero **after** shared pan in `EnvironmentManager.LateUpdate`. MainGame selects primary-Hero directional framing, disables group centroid. Existing green/yellow/red widths, acceleration/deceleration, pan cap, projection/zoom, actor speeds, AI, ranges and spawn remain unchanged. Other callers keep legacy Pan defaults.

Original assertions retained: 12s moving, normalized tolerance .015; stop **6s**, tolerance **.002**. Mono 8/8: left .6539574/.6519592/.6516105 at30/60/144; right .3461309/.3480339/.3483578; stop .650/.350. Pause/reversal/step bound and shared relative distances pass. Legacy reproduction viewport .1316516.

Unity6000.5.0f1 owned profile `foundation-c0133bd248c541f9babd661427eca509`: measured FPS30.069/59.997/143.885/60.000; actual Hero viewport .6539444/.652087/.6516552/.6521693 at600/800/1150/1360. Pause holds position/velocity. Profile `foundation-3cd60b9333694229a4443fe91f592b4d`: actual combat stop6=.650; no frame displacement above48.01 logical units. This stop fixture holds both sides at10000 HP **only in memory** to ensure a continuous observation, without changing speed/AI/range/damage. Right-running/reversal verified in pure controller fixture; MainGame normally explores left.

Task02 natural baseline sample (no HP override): first visible viewport .00255, distance1247.26; suspicious .48701/distance316.94; engaged after .516s at .60811/distance84.61; first damage .61957/distance67.20, Hero .65457. Group alert may exceed500 on peers by design. Melee attack range67.2; physical default691.2 means .65-.36=.29 viewport minimum at range, visible with this framing. Personal DEVB can override ranges; no personal settings read/written in these fixtures. No evidence requiring AI/spawn/range edits.

Awareness CSV filters SpawnVersion so pooled objects do not contaminate old-life telemetry. Initial draft CSV after first life reuse was unsuitable; use final owned-profile CSV. No claims of Player/native-input or all weapon/mode acceptance from these measurements.

Other direct Mono dependencies: MultiZone39, camera-entry38, weapon-basic31, loot-visuals23, all exit0. Runtime round1 passed pure math but MainGame failed .511 (sample before pan); round2 fixes sample, passes actual moving matrix. Subsequent verifier corrections address canonical references/counters and test HP lifetime; no tolerance/window change. No further camera runtime rounds required.

Camera Load/Retry/Travel binding and UI-native acceptance are recorded in the final batch report. Evidence outside Git: `work/stabilization-v03` logs; owned `.utmp/foundation-*` profiles; selected images/CSV copied into Docs for review.
