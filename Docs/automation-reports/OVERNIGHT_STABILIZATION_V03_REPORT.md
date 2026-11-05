# Overnight Stabilization v0.3 — 05/11/2026

## Git / phạm vi

- Worktree: `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Branch `feature/monster-awareness-ambush`; đầu phiên `c6db586b8130424be6601e8460a258697a15232f`, sạch. HEAD nghiệm thu trước commit báo cáo cuối: `21988b1`; commit chứa báo cáo này là HEAD bàn giao (xem `git log -1`).
- Runtime cuối thay đổi ở `10ebdf8`; các commit sau chỉ Editor verifier/tests/tài liệu. Không đổi Combat/AI/Save schema/checkpoint/Forge/drop config, asset nghệ thuật, zoom hoặc tốc độ nhân vật. Không push/merge/reset/rebase/force-push; không can thiệp Firewall/Security. Không có thay đổi chưa commit của người dùng ở đầu phiên.

| Commit | Nội dung |
|---|---|
| `10ebdf8` | Camera directional framing, hội tụ khi dừng; scene + pure regression |
| `a54fe4f` | Verifier MainGame camera/awareness/UI/world và ảnh thực |
| `baa0b24` | Farm1000kill, Save roundtrip và camera soak100chu kỳ |
| `df73096` | Pause interval và hai thứ tự chết cùng frame |
| `e42e530` | Bằng chứng Player/native targeting BLOCKED |
| `21988b1` | Load→combat thật→Save/Load→Retry→combat thật; CSV chỉ giữ đời đầu |
| Commit báo cáo này | Bàn giao cuối, không đổi runtime |

## Kết quả 10 task

PASS luôn giới hạn ở môi trường/ca đã chạy; không dùng PASS lịch sử thành PASS mới.

| Task | Trạng thái | Bằng chứng / phần chưa nghiệm thu |
|---|---|---|
| 01 Camera recovery | **PASS — Mono + Editor** | 30/60/144FPS, trái/phải/dừng6s/reversal/pause trong model; actual MainGame4kích thước, đứng đánh6s, rebindLoad/Travel/Retry. Hướng phải trong model, chưa có luồng MainGame tự chạy phải để chơi thử |
| 02 Camera × Awareness | **PASS — mẫu Editor** | Đo viewport, khoảng cách, visible→?→!→damage; baseline tự nhiên và HP-hold riêng. Không đổi AI/range/spawn. Chưa đo mọi vũ khí/DEVB cá nhân |
| 03 Native UI | **BLOCKED** | New/Confirm native PASS; HUD targeting bị công cụ từ chối sau activate/reobserve/retry. Không thay bằng handler |
| 04 Responsive UI | **PARTIAL** | Routing4size, tên Hero dài, allocation/equip/unequip/Forge/Map, modalLoad và stale binding PASS. Arbitrary long item name, native modal click-through và resize lúc loot bay chưa nghiệm thu mới |
| 05 World exploration | **PASS — cạnh fixture đã chọn** | BlockedTravel không mutation; spawn/accepted death10wave, clear2node, legalTravel, re-entry clearednode, Save/Load, Retry và camera binding. Không đo lại quãng đường/timing tự nhiên đã có coverage không đổi |
| 06 Combat lifecycle | **PARTIAL** | Weapon/projectile31, loot23, camera-entry38; thêm3ca Pause/death ordering PASS. Không tuyên bố đã chơi mọi windup/Travel/projectile permutation bằng native |
| 07 Progression/equipment | **PASS — Editor/fixture** | EXP fixture cấp điểm, bốn allocation đúng một mutation, equip/unequip, allocation sauLoad tác động Hero mới, dữ liệu/chỉ số roundtrip. DEVB/zero-point/realm/New cũ không chạy lại khi code không đổi |
| 08 Long-run farm | **PASS — bounded Mono** | 1000death/encounter, một actor pooling,73gear duy nhất,20cặpForge,5Save/Load; camera108000frame/100chu kỳ; một ca kho gầntrần PASS. Không phải1000trận có thời gian thật/native |
| 09 Save/Load recovery | **PARTIAL** | Các snapshot level/allocation/reward/equip/Forge/world/Travel; actual Load→damage22→Save/Load→defeat/Retry→combat PASS. Old-version compatibility không rerun; policy/schema không đổi |
| 10 Build/handoff | **PASS — build, tài liệu** | Một Windows64 build mới,0errors/48warnings; ảnh/log/profile sở hữu; diff/metadata checks; các process thử đã đóng. Native acceptance toàn bộ vẫn BLOCKED |

## Camera và awareness

Nguyên nhân: camera cũ bám tâm và thiếu feed-forward khi Hero chạy; near-center crawl không loại đủ drift trong6s. Candidate cũ còn centroid làm lệch mục tiêu Hero. Vòng runtime đầu của phiên đạt toán học nhưng MainGame=.5112112 do lấy mẫu Hero trước pan; chuyển mẫu về **cuối LateUpdate sau shared pan** khắc phục. Positional feedback nằm trong speed/acceleration/deceleration/pan cap cũ. MainGame bật directional .65/.35, transition.35s, tắt combat centroid; không tăng camera speed để che sai hướng.

| FPS | Mono chạy trái | Mono chạy phải | Dừng6s trái/phải |
|---|---:|---:|---:|
| 30 | .6539574 | .3461309 | .650/.350 |
| 60 | .6519592 | .3480339 | .650/.350 |
| 144 | .6516105 | .3483578 | .650/.350 |

Moving tolerance **.015**, stationary **.002**, observation **6s** giữ nguyên. MainGame600/800/1150/1360: .6539444/.652087/.6516552/.6521693; FPS đo30.069/59.997/143.885/60.000. Actual combat stop6=.650. Pause không đổi vị trí/velocity. Soak100chu kỳ/10reset model: maxstoperror7.947285E-09.

Mẫu baseline không HP override: visible .00255/distance1247.26; suspicious .48701/distance316.94; engaged sau.516s tại.60811/distance84.61; firstdamage .61957/distance67.20,Hero.65457. Quái đã hiện rõ khi bị đánh. Group alert có thể báo đồng đội ở ngoài500 theo đặc tả. Default melee67.2; physical691.2, nên tạiHero.65 thì mục tiêu ở mép tầmphysical vẫn khoảng.29viewport. Đây là giải thích hình học, không phải phép đo Bow/DEVB. Framing, spawn, sight và attack range không được trộn làm một nguyên nhân.

Stop fixture giữ HP hai bên10000 chỉ trong process test để đủ mốc6s; không dùng nó làm bằng chứng balance/combat tự nhiên. Telemetry cuối lọc SpawnVersion; CSV baseline trích tới hết đời đầu, loại phần pooling nhiễm identity của draft. Hai sửa runtime và các sửa fixture/counter đều có log FAIL/PASS, không nới assertion hoặc cửa sổ quan sát. Xem [checkpoint camera](CAMERA_V03_CHECKPOINT.md).

## Kiểm thử thực chạy

**Mono/UnityStub145ca PASS**, từng selector chỉ chạy lại khi có lỗi/thay đổi liên quan:

| Selector | PASS |
|---|---:|
| `--camera-framing` | 8 |
| `--multi-zone` | 39 |
| `--camera-entry` | 38 |
| `--weapon-basic` | 31 |
| `--loot-visuals` | 23 |
| `--stabilization-farm` | 1 |
| `--stabilization-camera-soak` | 1 |
| `--stabilization-lifecycle` | 3 |
| `--production-rewards "exact inventory limit"` | 1 |

Final source không còn FAIL trong các ca này. FAIL lúc chuẩn bị được giữ: samplecamera sai; stop verifier thiếu body cập nhật counter; fixture UI kiểm factory input thay canonical copy; Mono thiếu CreateInstance; re-equip cùng item bị guard hợp lệ từ chối. Sửa fixture/cách gọi đúng contract, không sửa assertion hợp lệ hoặc production để ép PASS. Không chạy Magic/fullUI/fullSave suite không liên quan.

**Unity6000.5.0f1**, `MainGameFoundationProbe.RunIsolated`, scene copy ignored, SaveManager inactive và GUID/marker trướcAwake; không save thật:

- `foundation-c0133bd248c541f9babd661427eca509`: moving matrix/awareness baseline.
- `foundation-3cd60b9333694229a4443fe91f592b4d`: stop6s/HP-hold, boundedframepan và awareness.
- `foundation-cfd3181ed0004ed5b4849f96714f1934`: UI4size, modalLoad, blockedTravel,2node/re-entry/Retry.
- `foundation-b869075a092d4bf98e841e304fddf7a9`: copied owned slot, actual postLoad damage22, snapshotSaveLoad, defeat/Retry, actual combat tiếp.

Farm dùng1000lethal callbacks qua CombatManager, không tạo1000GameObjects hoặc giả là natural combat. Chance production .60/.08 giữ nguyên; seed303/304 riêng trong fixture. Thu925material, tiêu220, còn705;73IDgear duy nhất,20Normal+Special,5file roundtrip,receipt cuối1/max1; EXP9328, expToNext1000000 fixture nên long-farm không đo leveling. Level/allocation đo riêng trongEditor. Soak làmodel, không phải100Travel thật. Xem [farm](V03_FARM_CHECKPOINT.md), [lifecycle](V03_LIFECYCLE_CHECKPOINT.md), [UI/world](V03_EDITOR_CHECKPOINT.md), [recovery matrix](V03_RECOVERY_CHECKPOINT.md).

## Player/native

Build duy nhất mới: Windows64/Mono2x,**Succeeded0errors/48warnings**,604818888bytes; runtime `10ebdf8`, không có runtime sửa saubuild. Product `Run-38fc92d35f044f929a5392074e9ecfe9`; kiểm app.info/marker, chỉ profile thử. Native New và Confirm thành công, AutoSave mới cólevel1/EXP90/wave3/XD-11/gear1/material7. Không gán PASS ManualLoad/Equip/Forge từ dữ liệu AutoSave.

800×450,boundsorigin560,291; focusedpane Player; AppliedDPI96 đọc-only. Local750,114→global1310,405 khớp ảnh. Công cụ nhận điểm đó trên `ChatGPT.exe "Chrome Legacy Window"`; activate/fresh screenshot/retry một lần vẫn reject trước input. Không đủ chứng minh Unity callback hỏng; layered/click-through là giả thuyết cần xác minh. Không inject vòng tránh targeting hoặc sửa UI không có bằng chứng. Character/allocation/Inventory/equip/Forge/SaveLoad/Pause/Travel/Retry native **NOT RUN/BLOCKED**. Xem [native checkpoint](V03_NATIVE_CHECKPOINT.md).

## Artifact và Git checks

- Log mới: `C:\Users\hason\Documents\Codex\2026-11-04\truy\work\stabilization-v03\`: camera-round1, camera-editor(-round2), camera-combat-final, ui-world-round1/2, recovery-round1, farm-round1/2/3, camera-soak, lifecycle-round1, material-limit, camera-entry,weapon-basic,loot-visuals,windows-build `.log`.
- Player executable: `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\player-validation\final-player\IdleGameValidation.exe`; build-summary cùng thư mục. Native record/PNG: `outputs/stabilization-v03`. Player.log trong đúng GUID profile trên.
- Ảnh MainGame trực tiếp: [Character600](evidence/stabilization-v03/v03-character-600.png), [800](evidence/stabilization-v03/v03-character-800.png), [1150](evidence/stabilization-v03/v03-character-1150.png), [1360](evidence/stabilization-v03/v03-character-1360.png); [Forge600expanded](evidence/stabilization-v03/v03-forge-600.png), [1360expanded](evidence/stabilization-v03/v03-forge-1360.png); [Playernative](evidence/stabilization-v03/player-native.png). Đã quan sát các ảnh Character4size và Forge600/1360, không thấy nút chính bị tràn trong các ảnh này.
- `git diff --check` và staged checks PASS. Metadata checker0errors/3missingfolder-meta warnings có sẵn. Chỉ dọn font/import/buildhelper/serialization artifacts do phiên tạo; giữ các profile evidence ignored. Không commit output/log/save. Worktree bàn giao sạch sau commit báo cáo.

## Còn lại / tối đa3bước tiếp

1. **P1 native targeting:** cần người dùng bấm chuột thật ở Player hoặc công cụ hỗ trợ hợp lệ cửa sổ transparent, rồi nghiệm thu Character→allocation→Inventory→Forge→ManualLoad→Pause→Travel/Retry. Không dùng Administrator để che vấn đề.
2. Nghiệm thu thực tế resize khi loot bay, modal click-through và tên item dài; các ca/mode chưa đo ghi NOT RUN, không suy rộng từ routing/Mono.
3. Chơi farm tự nhiên dài hơn với cấu hình nguyên bản trên máy khác khi được phép push ở nhiệm vụ sau; so sánh camera/awareness và quan sát TLS/Curl35/certificate7 cùng lỗi Editor Search có sẵn. Không tự đổi security policy/balance để xóa cảnh báo.

Không phát hiện P0 mất dữ liệu trong phạm vi đã chạy; không coi đây là chứng nhận mọi save cũ/tương lai. MaxHealth theo mode vẫnprototype; cảnh giới vẫnnhãnlegacy, không có ThiênKiếp/Savefield mới. Bản runtime đủ mởUnity/chơi thử camera và vòng hiện có; **chưa hoàn tất native acceptance**, không tuyên bố production hoàn thiện. Dừng batch tại Task10.
