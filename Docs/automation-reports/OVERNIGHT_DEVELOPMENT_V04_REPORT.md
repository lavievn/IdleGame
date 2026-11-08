# Overnight Development v0.4 — bàn giao

## Kết quả và Git

Phiên 05–06/11/2026 (Asia/Saigon). Hoàn thành rà soát A1–A4 và thiết kế B1–B4. Không tự triển khai các hệ B sau thiết kế.

- Repository: `lavievn/IdleGame`; worktree `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Branch: `feature/monster-awareness-ambush`; đầu phiên `ba863680ad6bcef5d174aaa2f97d0feb5688c368`, worktree sạch. Các commit v0.3 còn nguyên trong ancestry. Phiên cũ báo idle/completed; không thấy tác vụ v0.4 khác ghi worktree trước khi bắt đầu.
- HEAD trước commit báo cáo: `24a1c59`; commit chứa báo cáo này là HEAD bàn giao, xem `git log -1`.
- Không push/pull/merge/rebase/reset hoặc đổi nhánh. Upstream local đầu phiên ahead 7; cuối phiên dự kiến ahead 11 sau bốn commit v0.4, không fetch để suy trạng thái remote mới.
- Camera v0.3, scene MainGame, AI/range/spawn, damage formula, bảng tương khắc, HP theo mode, drop configuration và Save schema không thay đổi.

| Commit | Nội dung |
|---|---|
| `e1fe9e9` | Nhãn nguồn damage basic Physical/Magical trong trace; test nguồn stat và snapshot |
| `0b66288` | Verifier Editor có cổng CLI riêng cho tên dài, loot resize, modal, external inventory change, Load/Retry |
| `24a1c59` | Bốn tài liệu hợp đồng/backlog B1–B4, chỉ đề xuất |
| Commit báo cáo này | Kết quả, bằng chứng và giới hạn v0.4; không thêm runtime |

## Phân loại bàn giao

| Task | Trạng thái | Kết quả và giới hạn |
|---|---|---|
| A1 Character/stat clarity | **VERIFIED — đọc mã hiện hành** | UIManager.RefreshCharacterProgression có Level/EXP, points, HP từ đúng HeroCombatState; rank ghi legacy/prototype, DEVB Hero/Monster/baseline và profile thử rõ. HeroController.UpdateStats phân Physical/Magical, allocation Physical ghi đúng. Không thấy thiếu/sai cần sửa; không chạy lại matrix Character đã đạt v0.3. |
| A2 Loot/inventory feedback | **VERIFIED — đọc đường gọi và test source** | Material/gear/both/none dùng outcome riêng; chỉ nhánh Awarded báo nhận thành công. Cap reject nguyên quantity; invalid gear/ID không sinh ID thay thế, lỗi nhánh không rollback thành công nhánh kia. NoDrop không giả nhận vật phẩm. ID/quantity đi từ grant canonical tới Inventory/Equipment. Không sửa hệ thưởng; không tuyên bố đã chạy lại reward suite mới. |
| A3 Combat readability | **IMPLEMENTED + VERIFIED — Mono** | Sửa thông tin gây hiểu nhầm: trace basic trước ghi raw ATK/cộng điểm Physical cho cả đòn phép. Nay ghi nguồn Physical Attack hoặc MagicalAttack, tổng đã gồm modifier, giữ metadata khi projectile đánh giá lại tại impact. Không thay phép tính. |
| A4 Targeted UI edges | **VERIFIED — Unity Editor cô lập** | Tên fallback dài, resize lúc loot rơi/bay, modal routing, stock/item Forge thay ngoài UI, binding sau Load/Retry đạt các assertion mới. Không tái hiện lỗi cần sửa UI runtime. Native HUD giữ BLOCKED; không thử lại hoặc vượt targeting. |
| B1 Realm contract | **PROPOSED** | State tối thiểu, công thức nội bộ/mốc 1-based, hai pha/commit/cancel, failure decisions, migration và test plan. Không thêm state/Save/gameplay. |
| B2 Martial/cultivation contract | **PROPOSED** | Stable IDs, learned/rank/loadout/eligibility, passive ownership, active lifecycle, source school/root một lần, persistence và ba ví dụ không production. |
| B3 Boss architecture | **PROPOSED** | Kiểm kê 133 node/sáu boss; gate hiện chặn giữ nguyên. Entry/exit/lifecycle/reward transaction, Save/crash/duplicate và quyết định cần duyệt. Không prefab/content/encounter thật. |
| B4 Next backlog | **PROPOSED** | 12 task N01/R01–R03/M01–M05/B01–B03, mỗi task có file, tái sử dụng, thay đổi, phụ thuộc, rủi ro, PASS và quyết định còn thiếu. Không task nào được triển khai tiếp tự động. |

### A1/A2 — bằng chứng tĩnh và điều không suy rộng

Nguồn chính: `UIManager.cs` (`CharacterSourceStatus`, `RefreshCharacterProgression`), `HeroController.cs` (`UpdateStats`), `CombatLootRewards.cs` (`CompleteProduction`, `FinishProduction`), `CombatManager.cs` (`HandleMonsterDeathBy`), `EquipmentInventory.Add`, `EquipmentRuntime.AddInstance`, `InventoryPanelUI.Refresh/FindSelected/RefreshForge`. `ProductionRewardRegression.cs` đã có các ca hai stream/none/cap/invalid-instance/duplicate/stale; v0.4 đọc assertion để xác định coverage, không chạy lại chỉ để tăng PASS.

Material cap hiện là 9999, gear chưa có capacity cap. Gear nhận thành công hiện phản hồi tên trong log/Inventory, material phản hồi tên+số lượng; lỗi báo chung trong nhật ký và chi tiết Console. Log không hiển thị instance ID không có nghĩa ID đã sai: grant trả ID canonical, Equipment dùng ID đó. Chưa có yêu cầu thêm UI debug ID nên giữ nguyên. Không thêm VFX kiếm giả cho Bow/material hoặc thưởng NoDrop.

### A3 — thay đổi thực tế

`DamageTrace.attackSource` là metadata tạm của trace, không nằm trong EntityDataSO hoặc Save. Chỉ thay `Describe` cho weaponBasic; các giá trị damage/root/roll/boost/defense không đổi. Giữ đoạn `; tổng: ...;` để verifier recovery sẵn có vẫn nhận nguồn stat theo format cũ. Trace legacy giữ cách trình bày cũ.

Không thêm log Console mỗi frame hoặc hệ combat text. Tái sử dụng chi tiết damage mở theo nhu cầu qua UI; GameManager.RecordDamage lưu tối đa 30 trace ở sự kiện damage. Cặp root/tier/weight/hệ số đã có trong trace. `?`/`!` và log hit/surprise ×2 đã có, không cần thay AI/awareness để làm lại phản hồi này.

Kiểm thử chuẩn bị chứng minh lỗi nhãn bằng FAIL `physical source label`. Vòng sửa 1 sửa nhãn, nhưng fixture FAIL vì kỳ vọng Magical=17 bỏ qua MagicalAttack đã có trên Staff. Vòng sửa 2 lấy MagicalAttack trước khi thêm modifier rồi kiểm tra tăng đúng 17; giữ assert không dùng Physical và snapshot không đổi damage/roll. Không sửa công thức hoặc nới tolerance. Tổng hai vòng sửa–kiểm thử, dưới giới hạn năm vòng.

## Kiểm thử thực chạy trong v0.4

| Môi trường | Thực chạy | Kết quả |
|---|---|---|
| Mono/UnityStub | `Tests~/run.sh --weapon-basic` qua Mono của Unity 6000.5.0f1; một ca mới trong nhóm | **31 ca PASS** ở lượt cuối; không coi đây là 31 ca mới |
| Unity Editor 6000.5.0f1 | `MainGameFoundationProbe.RunIsolated -foundationValidationRun -foundationDevelopmentV04` | Một lượt mới, bốn nhóm check đạt, `failure` rỗng, Editor hoàn tất/thoát |
| Metadata/GUID checker chỉ đọc | `unity-csharp-scripting/scripts/check_unity_project.py ... --json` | 764 file, **0 error**, 3 warning folder-meta Lake/Sea/Steppe có sẵn trong baseline |
| Git/tài liệu | `git diff --check`, staged check; source diff; links/fences bốn contract; catalog count | PASS; 133 ID duy nhất, 6 boss. Không đổi protected runtime/scene/schema |

Không dùng 145 PASS lịch sử v0.3 làm kết quả v0.4. Không chạy full Mono/full Save suite, build Windows Player hoặc native acceptance mới vì thay đổi hẹp/không cần thiết hoặc công cụ đang BLOCKED. B1–B4 không chạy Unity hoặc runtime tests riêng: hoàn toàn tài liệu.

### Bằng chứng Unity A4 và phạm vi chính xác

Profile sở hữu: `.utmp/foundation-ba9c1125f9d24c658fefdd108c6a53d1`; SaveManager được cấu hình đường riêng trước Awake, owner inactive, GUID/marker; scene copy ignored. Không đọc/ghi/xóa save người chơi. DEVB chỉ reset static trong process thử, không đổi file cá nhân.

1. Tên dài dùng stack fixture có ID/tên fallback gồm 28 token `TrườngDanh0…27`, quantity=2. Inventory path thật, đủ token truy cập được qua trang, không overflow ở detail, các nút vẫn trong bounds, ID/quantity không đổi. Không thêm item production hoặc đổi tên catalog. Ảnh từ đầu vào 600×338 thực tế Inventory mở rộng thành **1160×680**; ảnh đầu vào 1150×647 thực tế **1150×647** theo fallback layout. Không gán kết quả ảnh mở rộng cho Inventory compact 600×338, không tuyên bố arbitrary renamed gear/Forge title đã được kiểm mọi độ dài.
2. Khi Forge mở, HitControl tại tâm nút InventoryClose phía dưới trả modal, pointer routing không đóng Inventory hoặc equip item. Đây là Unity handler/routing, **không phải chuột OS-native**. Stock đổi bên ngoài được cập nhật bằng polling unscaled; gear đang chọn bị xóa khỏi fixture thì Forge tự đóng, không giữ reference cũ.
3. Loot fixture chỉ cosmetic: resize Game View thật 800→600 lúc rơi, 600→1150 khi đang bay; tính lại X theo mép phải mới khớp trong 0,1 px. Receipt retire, Inventory không mutation. Verifier gọi `Advance` với dt tường minh khi paused để đo; không gọi đây natural playback hoặc timing OS/native. Không giả cấp thưởng từ animation.
4. Load khi Forge đang mở đóng modal cũ, một click allocation tác động Hero mới đúng một điểm và không sửa Hero trước Load. Fixture defeat→Retry bind camera/combat hiện tại; Inventory sau Retry equip đúng item canonical. Không chạy lại camera framing matrix đã đạt v0.3.

Đã quan sát ba PNG của lượt mới: tên dài hai layout, loot giữa lần resize. Không thấy nút chức năng bị che ở các ảnh đó. TLS/Curl35/certificate7 và cảnh báo obsolete/editor có sẵn xuất hiện trong log; không đổi security policy hoặc che exception gameplay. Không có runtime fault mới được verifier ghi nhận.

Unity sinh dynamic font cache và 15 import `.meta` chưa tracked từ baseline. Sau Editor thoát, chỉ dọn đúng 15 file đã có whitelist/hash manifest; font được khôi phục bytes từ HEAD và hash-object chứng minh bằng blob HEAD trước làm mới index stat. Không đưa font/import artifacts hoặc save/log/build vào commit. Scene/camera không thay.

## Tài liệu và quyết định cần xem xét về sau

- [REALM_PROGRESSION_CONTRACT](../architecture/REALM_PROGRESSION_CONTRACT.md): entry Level/mốc nội bộ, realm catalog/cuối/cap, migration Level cao, failure/Retry và checkpoint Thiên Kiếp.
- [MARTIAL_ARTS_CONTRACT](../architecture/MARTIAL_ARTS_CONTRACT.md): definition/slots/learn-upgrade policy, root/element/rounding, weapon swap/cancel và nội dung skill cụ thể. Ba ví dụ chỉ proposal, chưa có resource/mana/cooldown mới.
- [BOSS_ENCOUNTER_CONTRACT](../architecture/BOSS_ENCOUNTER_CONTRACT.md): unlock/spec/failure/exit, first-clear/repeat/reward/EXP/cap, transaction và Save timeline/downgrade. Guard equipment không thay thế guard các feature mới.
- [NEXT_IMPLEMENTATION_BACKLOG](../architecture/NEXT_IMPLEMENTATION_BACKLOG.md): thứ tự dependency và 12 task giao riêng; không là quyền triển khai tự động.

Đề xuất quan trọng: Level tổng hiện cap 999; không gọi nó vô hạn. Realm internal 1-based và migration save Level cao vẫn là phương án cần duyệt. Boss reward durable hiện chưa có; manual slot Load là rollback timeline, không có global ledger chống sao chép file. Không dùng các thiết kế này để tuyên bố realm/võ học/boss hoạt động.

## BLOCKED / NOT RUN và nghiệm thu thủ công

**BLOCKED:** native HUD targeting từ v0.3 giữ nguyên; v0.4 không lặp click bị từ chối, không dùng injection/Administrator hoặc direct handler để gọi native PASS.

Khi có thể thao tác chuột thật trong đúng Player/profile thử: New/Confirm → Character/phân một điểm → Inventory/equip/unequip → Forge → ManualSave/Load → Pause/Resume → Travel hợp lệ → defeat/Retry. Kiểm modal không click xuyên và resize lúc loot bay. Ghi nhận từng thao tác/ảnh; không dùng slot cá nhân hoặc chỉnh drop rate production. Bảng N01 mô tả phạm vi công việc này.

**NOT RUN:** Windows Player build/acceptance v0.4; tự nhiên chạy đủ animation và resize bằng chuột; tên gear tùy ý trong Forge; implementation/tests realm/skill/boss, schema migration mới và crash transaction boss. Các phần này không được gán PASS từ Editor routing hoặc từ tài liệu.

## Vị trí bằng chứng

Workspace phiên này: `C:\Users\hason\Documents\Codex\2026-11-05\ti-p-t-c-d-n`.

- `work/trace-before.log`, `trace-after.log`, `trace-round2.log`: FAIL chuẩn bị, FAIL fixture và PASS cuối Mono.
- `work/v04-editor-round1.log`: log Unity đầy đủ; `work/v04-metadata-check.json`, `work/v04-generated-imports.json`: checker và manifest dọn artifacts.
- `outputs/v04/evidence/`: result.json và ba ảnh đã quan sát; `outputs/v04/logs/`: log Mono và tóm tắt Unity/checker dùng để bàn giao.
- `outputs/v04/contracts/`: bản sao bốn tài liệu; `outputs/v04/OVERNIGHT_DEVELOPMENT_V04_REPORT.md`: bản báo cáo để mở từ chat.

Đã dừng sau A1–A4, B1–B4 và bàn giao. Không tự mở backlog triển khai tiếp.
