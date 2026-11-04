# Group 01 — Unblock paused-combat Load regression

- Priority/scope: verify the existing Save/Load path currently blocked by stale test setup; no runtime changes.
- Start: `95a2456`, branch `feature/monster-awareness-ambush`, clean and equal to remote.
- Completion: create a real in-flight shot using the current weapon authority, preserve every cleanup/load assertion, run only the relevant fixture group.
- Evidence: primary attacks resolve from equipped weapon. The fixture toggled old stance while unarmed, so it could not create the required ranged shot.
- Change: equip the existing Bow through the real handler. Add `--manual-load-ui` to run the eight existing cases directly.
- PASS: Mono/UnityStub `--manual-load-ui`, **8/8 groups**, round 1. Includes repeated load, paused combat clearing shots/timer/wave, title/cinematic cancellation, Continue, ReturnTitle and legacy waiting load.
- FAIL: 0. Assertions unchanged. NOT RUN: native Unity/Player for this tests-only change.
- Save I/O: existing fixture uses a fresh temporary GUID directory and known-slot cleanup, not player Saves. No schema or runtime Save/Load code changed.
- Risk review: low; only test setup and test entry point changed. No unexplained Save/Load failure found. Safe to continue to a separate P1 group.
- Reports use canonical `Docs/automation-reports/`; on this Windows checkout `docs/automation-reports/` resolves to the same directory. Do not introduce a second case-only root into Git.
