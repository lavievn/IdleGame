# v0.3 bounded farm checkpoint

`--stabilization-farm`: PASS1/1. 1000 accepted lethal/death lifecycles, 1000 encounters, one reused MonsterController; shared production death receipt and configured .60/.08 streams with separate seeded RNGs303/304. 73 unique gear, 925 material income minus220 Forge costs =705 remaining; 20 Normal+Special pairs; five owned SaveManager file roundtrips; receipt count at most1, final1. Duplicate callbacks after each death leave inventory/EXP unchanged. Rebuilding equipped modifiers twice leaves damage unchanged. Instance IDs, material, full gear JSON/history, points/allocations and derived HP/Physical survive each roundtrip. EXP9328 accumulated; fixture expToNextLevel1000000 intentionally prevents level changes, which are covered separately by Editor UI setup/Save snapshots.

This is **Mono/UnityStub**, with lethal damage injected by the test, not 1000 naturally timed fights or 1000 GameObjects. It does not replace the live Hero on each file roundtrip; actual Load/Retry/Travel continuation is tested separately in the Editor probe. No visual/native long-play acceptance claimed. No dev resource grant in farm loop; stock earned through death rewards.

`--stabilization-camera-soak`: PASS1/1, 100 move/stop/reversal cycles,108000 simulated frames60FPS, ten hidden-entry resets; max stationary viewport error7.947285E-09; original6s/.002 and moving.015 retained. Pure controller endurance, not100 actual Travel operations.

Existing exact material-limit case: PASS1/1, atomic1/2 quantities at9998/9999. It is an isolated cap check, not stock accumulation to cap in the long farm.

Fixture corrections: first draft used unsupported ScriptableObject.CreateInstance in Mono stub (compile FAIL); use existing new EntityDataSO convention. Next draft re-equipped the currently equipped latest item; real guard correctly rejected the repeated equip. Fixed test sequence to Unequip then Equip through real handlers, retaining assertions. Third farm run PASS; no runtime reward/equipment/save changes or relaxed assertions. Soak and cap each ran once.

Logs outside Git: `work/stabilization-v03/farm-round1.log`, `farm-round2.log`, `farm-round3.log`, `camera-soak.log`, `material-limit.log`. Owned temp Save directory removed by fixture finally; no player save access. No remaining fixture failures on final source.
