# v0.3 Windows Player checkpoint

Build6000.5.0f1 Windows64 Mono2x: Succeeded,0 errors,48 warnings,604818888 bytes. Runtime camera source `10ebdf8`; later commits change only Editor verifier/tests/docs. Built once because previous Player did not contain camera fix. Output outside repo: `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\player-validation\final-player\IdleGameValidation.exe`.

Unique product `Run-38fc92d35f044f929a5392074e9ecfe9`, company IdleGameValidation. app.info/marker checked before launch; initially only marker. One visible launch via sky, PID24568. Profile under AppData/LocalLow/IdleGameValidation/product; no product save read/write. Firewall prompt did not appear; no Firewall/UAC/Security/registry policy modifications.

Native New and Confirm clicks delivered. Auto combat/new camera/EXP/loot and owned AutoSave observed: level1,EXP90,wave3/5,XD-11,one gear,seven refinement material,points0. Last screenshot HP524/800. This is ordinary .60/.08 Player flow, no fixture chance override. No manual equip/Forge/Load acceptance from AutoSave observation.

HUD native **BLOCKED**. Focused accessibility pane is owned Player. Screenshot bounds800×450,origin560,291; Character local750,114 converts exactly to global1310,405. AppliedDPI read-only96 (100% baseline; per-monitor scale not independently measured). Click preflight rejects `point (1310,405) is over ChatGPT.exe "Chrome Legacy Window", not target window IdleGameValidation.exe`. Activate, fresh screenshot and one retry produced same rejection. Inputs not delivered; no fallback injection, direct handler, Win32 forcing, security-policy bypass or further launch attempted. Possible layered-window/click-through interaction remains a hypothesis, not a proven Unity callback failure. Editor hit routing at four sizes passes separately.

Character allocation, Inventory, Equip/Unequip, Forge, Manual Save/Load, Pause/Resume,Travel,Retry: native NOT RUN/BLOCKED. Do not count Editor routing as native PASS. Need human mouse acceptance or tooling that can legitimately target this transparent Player. No UI code changed based solely on this rejection.

Stopped only owned executable/PID after evidence. Existing TLS/Curl error35/certificate7 remains in Player.log; no gameplay exception found in short sample, no claim Console universally clean. Artifacts: `work/stabilization-v03/windows-build.log`, output build-summary; `outputs/stabilization-v03/native-input.json`, `player-native.png`; owned profile Player.log. Screenshot copied to Docs evidence for review. No Player save committed.
