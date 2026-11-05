# Production Loot v0.1 — báo cáo bàn giao

Ngày: 2026-11-05. Cấu hình cân bằng **prototype**, chưa phải balance cuối cùng.

## 1. Repository và commit

- Branch: `feature/monster-awareness-ambush`; giữ nguyên worktree `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- HEAD ban đầu: `08a3e5d78050b6241805059149df5edd3cb449ab`; working tree ban đầu sạch.
- `cca9fcd90b6f0b1834046b080ad228282f000f07`: hai stream chung receipt, tích hợp death flow và kiểm thử.
- `c5591c7506cfd2a00259d9d8c6fbd6b67745eb75`: cấu hình MainGame và smoke probe cô lập. Đây là HEAD mã nguồn đã kiểm thử.
- Commit tài liệu chứa báo cáo này là HEAD bàn giao; SHA cuối ghi trong `outputs/loot-v01/summary.json` ngoài repository để tránh tự tham chiếu SHA.
- Không push, merge, đổi branch, sửa Firewall hoặc chạy Windows Player.

## 2. Hai reward stream

`ProductionLootConfig` dùng `MaterialRewardConfig` và `EquipmentRewardConfig`, thay bảng production một-result-per-kill trước đó. Hai nguồn RNG riêng, không dùng combat RNG. Cùng seed/config trong fixture tái lập outcome, quantity, template và chỉ số; GUID instance vẫn là định danh duy nhất, không thuộc cam kết tái lập.

`CombatLootRewards` giữ lifecycle/death receipt hiện có. Receipt được chốt trước khi lập kế hoạch/cấp thưởng; cả hai kết quả roll được quyết định trước khi commit từng nhánh. Material vào inventory nguyên liệu hiện có; gear qua factory/catalog và inventory instance hiện có. Không tạo hệ thưởng hoặc ledger Save song song.

Kết quả mỗi nhánh có outcome và lỗi riêng. Một nhánh thất bại không rollback phần đã commit, không roll lại và không khóa các lần chết tiếp theo. Callback trùng không cấp lại cả hai nhánh. Kết quả tổng có thể là Awarded kèm lỗi nhánh còn lại; CombatManager vẫn ghi lỗi cụ thể. Guard owner, encounter, spawn/pooling và save version được giữ.

Gear thành công tiếp tục dùng VFX trang bị hiện có; material chỉ cập nhật inventory và log lượng nhận, không tạo VFX kiếm. Profile A/controlled loot vẫn là đường thử riêng.

## 3. Prototype thực tế trong MainGame

Inspector: GameManager → Production Loot v0.1. `configured = true`, `controlledLootTest = false`.

| Nhóm | Enabled | Chance | Dữ liệu |
|---|---|---|---|
| Material | Có | 0,60 | `material:refinement`, lượng nguyên 1–2 |
| Equipment | Không | 0,08 | Policy Unconfigured, danh sách template trống |

Equipment khi được cấu hình hợp lệ tạo đúng 1 Common qua factory hiện có. Có hỗ trợ danh sách template và chọn Uniform; mỗi entry bắt buộc ID catalog, item level và main stat tường minh. Không thêm rarity, tiền tệ hoặc vật phẩm mới. Chance/ID/quantity/template sai được báo ở nhánh tương ứng; nhánh tắt không roll hoặc kiểm tra dữ liệu chưa dùng.

Scene chỉ thêm cấu hình trên; không sửa vị trí chiến đấu/camera/AI/EXP. SaveManager, EntityDataSO và công thức Forge không đổi.

## 4. Gear thực sự sử dụng

**Production: chưa sử dụng template nào — BLOCKED phần bật gear.** Catalog xác định template và loại main stat nhưng chưa cung cấp bộ level/main stat production được phê duyệt. Không lấy số demo/fixture làm mặc định.

Fixture dùng `sample:iron_sword` level 1/main stat 7 và `sample:basic_bow` level 2/main stat 11 để kiểm thử factory/selection/equip; các số này không nằm trong MainGame. Cần chốt template + level + main stat trước khi chọn Uniform và bật chance 0,08.

Nguyên liệu production đã có nguồn thu. Forge tiếp tục dùng `material:refinement`: Normal tiêu 1, Special tiêu 10 theo quy tắc cũ. Gear đã có trong save có thể dùng nguồn nguyên liệu này. Hai charm khởi đầu legacy không được thay bằng gear fixture; vì vậy chưa tuyên bố vòng gear/Forge production đầy đủ cho một nhân vật mới.

## 5. Tính nhất quán

Đã kiểm: callback chết trùng; encounter stale; đổi spawn/pooling; tắt một/cả hai stream; cấu hình sai; material sát/chạm giới hạn; nhánh material thất bại nhưng gear thành công; factory lỗi/trùng ID nhưng material thành công; context hết hiệu lực sau material commit; lần chết sau lỗi vẫn xử lý; seed tái lập và độc lập RNG theo cả hai chiều.

Thưởng material vượt giới hạn bị từ chối nguyên nhánh, không cấp một phần bằng cách clamp. Gear factory được kiểm tra contract trước khi thêm inventory. Receipt đã hoàn tất không retry hoặc cấp lại nhánh thành công. Save/Load cả hai reward dùng schema hiện tại, không thêm cơ chế khôi phục receipt qua Save.

## 6. Kiểm thử thực chạy

| Kiểm tra | Kết quả | Phạm vi/giới hạn |
|---|---|---|
| Mono `--production-rewards` | PASS 30/30 | Hai stream, lỗi/giới hạn, receipt, RNG, death flow, Equipment/Forge thật trong fixture |
| Mono `--loot-visuals` | PASS 23/23 | Hồi quy controlled loot, VFX/lifecycle, equip |
| Mono không định nghĩa Editor/Development, `--production-rewards death` | PASS 2/2 | Production không phụ thuộc cờ development; không phải Windows Player |
| Unity 6000.5.0f1 Play Mode: CombatLootTests | PASS 6/6 | Hồi quy reward/death hiện có |
| Unity Play Mode: ProductionV01BothStreamsForge_SaveLoadSupportsExistingSchema | PASS 1/1 | Cùng receipt cấp gear + material, chống trùng, equip/Forge/history, Save/Load ID/slot/stat/stock và dữ liệu cũ |
| Unity Editor MainGame material smoke | PASS | Combat.Update hạ quái thật, nhận 1 material; gear không tăng, không VFX kiếm, test A tắt |
| Metadata checker | PASS: 0 lỗi | 3 cảnh báo có sẵn, không sửa asset ngoài phạm vi |
| `git diff --check` | PASS | Không có lỗi khoảng trắng |
| Production gear trong MainGame | BLOCKED | Thiếu bộ template/level/main stat được phê duyệt |
| Native mouse/UI matrix, Windows Player | NOT RUN | Không sửa UI; không khởi chạy Player đang bị chặn |

Smoke xác nhận cấu hình scene là 0,60 và lượng 1–2, sau đó **ép chance thành 1 chỉ trong bộ nhớ profile thử** để kiểm tra xác định một lần chết. Không sửa chance trong asset; không coi đây là đo phân phối 60%. Probe dùng bản sao scene và thư mục GUID có marker, cấu hình Save trước Awake; không đọc/ghi save người chơi.

Lần compile smoke đầu gặp CS0136 do trùng tên biến local trong probe, đã đổi tên; lần thứ hai compile và smoke đạt. Không làm yếu assertion. Không còn test FAIL chưa xử lý trong phạm vi. Không chạy lại Magic/UI matrix đã đạt và không bị sửa. Các trường phase1–4 trong JSON của probe là NOT RUN vì smoke đi nhánh riêng, không phải nghiệm thu lại bốn pha foundation.

Bằng chứng ngoài Git: `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\loot-v01\` chứa `streams-round1.log`, `legacy-loot.log`, `release-streams.log`, `native-streams-round1.xml`, `main-game-smoke.log`, `main-game-smoke.json`, `metadata.json`, `summary.json`. Log Unity đầy đủ còn tại `work/loot-v01/native-streams-round1.log` và `work/loot-v01/main-game-material-smoke-round2.log` trong workspace Codex. Profile smoke thuộc thử nghiệm: `.utmp/foundation-8cce8ac78df949378f15bb033c5797c9/evidence/result.json` trong worktree.

## 7. Chưa hoàn thành

- Gear production chưa bật; vòng thưởng gear → Equipment/Forge từ nhân vật mới còn PARTIAL/BLOCKED theo dữ liệu thiếu ở mục 4.
- Chưa nghiệm thu bằng input native hoặc Windows Player trên mã mới. Không dùng kết quả Player cũ làm bằng chứng.
- Chưa đánh giá tốc độ progression/balance thực tế của 60% material; đây là prototype được phép chơi thử.

## 8. Đề xuất tiếp theo

1. Chốt danh sách gear, level/main stat và policy chọn; sau đó bật gear prototype 0,08 trong Inspector và kiểm tra luồng mới.
2. Chơi thử với material 0,60 để thu phản hồi lượng nguyên liệu; chưa tự điều chỉnh balance.
3. Nghiệm thu native/Windows Player trong tác vụ riêng khi môi trường cho phép.

Bản hiện tại có thể mở Unity để chơi thử **nguồn material production**; không tuyên bố đã hoàn thiện nguồn gear hoặc toàn bộ vòng production. Dừng sau bàn giao này.
