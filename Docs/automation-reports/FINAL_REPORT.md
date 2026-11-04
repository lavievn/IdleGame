# Overnight Foundation Completion — 05/11/2026

## 1. Repository

- Branch: `feature/monster-awareness-ambush`, worktree `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- HEAD ban đầu: `58476a70e8b481c36f1ea72887469d9491635bbb`; working tree sạch.
- HEAD cuối của mã/test: `a7a762ae6247a44ceb2974cb4fc82f03ba5739c2`.
- HEAD bàn giao: commit chứa báo cáo này, tra chính xác bằng `git log -1 --format=%H -- Docs/automation-reports/FINAL_REPORT.md`. SHA đầy đủ sau commit cũng được ghi ngoài Git trong `outputs/overnight-foundation/repository-state.json`; tránh ghi SHA tự tham chiếu vào chính commit.

| Commit cục bộ | Nội dung |
|---|---|
| `5a9181e44915b51a6d5793a8e4f535fdda0a84cf` | Trạng thái ban đầu và kiểm MainGame UI |
| `450c4b4b3ca5a305c196b19ea90b693250e2dcc6` | Verifier MainGame cô lập, kiểm loot/trang bị/tích hợp |
| `a7a762ae6247a44ceb2974cb4fc82f03ba5739c2` | Entry Forge riêng và kiểm Save/Load |
| Commit chứa báo cáo này | Chốt bàn giao, không sửa mã |

Không push, merge, đổi nhánh/base PR, reset hoặc clean. Không truy cập save người chơi. Không sửa scene sản phẩm, schema, loot rate/cờ production, balance, AI, camera hoặc artwork.

## 2. Kết quả từng giai đoạn

| Giai đoạn | Kết luận nghiệm thu | Bằng chứng và giới hạn |
|---|---|---|
| 1 — UI/HUD | PARTIAL | MainGame thật: handler/hit-test mở/đóng panel, Pause, log, tools, Inventory mở rộng và nền mặc định đạt. Chưa input native/ảnh 600 và 1150 trong phiên này. |
| 2 — Loot/Inventory/Equipment | PARTIAL | Combat thật cấp gear đúng một lần; VFX/Pause/fallback; equip/unequip, chỉ số và damage trace đạt. Loot có thưởng chỉ trong controlled-loot profile hiện có. |
| 3 — Forge/Save-Load | PASS trong phạm vi đã chạy | Forge thật thiếu/đủ nguyên liệu; equipped stats/history; Auto/Manual/rollback/New reset; JSON legacy/current/future guard đạt trong profile cô lập. Nguồn nguyên liệu gameplay chưa được tự thiết kế. |
| 4 — Tích hợp | PARTIAL | Cùng một phiên Editor đã chạy hết chuỗi với gear thật, Forge, save/load và combat sau Load. Windows Player/chuột thật và vòng thưởng production chưa nghiệm thu. |

`result.json` của verifier có 4 pha PASS cho phạm vi kiểm thử tự động của nó; bảng trên đánh giá phạm vi sản phẩm rộng hơn. Không coi PASS script/handler là PASS chơi bằng chuột hay Windows Player.

## 3. Những gì đã sửa/bổ sung

- `Assets/Script/MainGameFoundationProbe.cs` và `.meta`: verifier CLI chỉ có trong Editor; chạy MainGame thật trên scene copy ignored, SaveManager inactive và profile GUID/marker trước Awake. Từ chối scene bẩn/Play đang chạy/reparse. Không tạo dữ liệu Hero riêng cho UI, không startup hook.
- `Tests~/MotionRegression.cs`: thêm `--forge` gọi nguyên bộ Forge hiện có; không thay assertion.
- `Docs/automation-reports/OVERNIGHT_INITIAL.md`, `OVERNIGHT_PHASE_01_UI.md` đến `OVERNIGHT_PHASE_04_INTEGRATION.md`: phạm vi, kết quả, rủi ro và cách tái lập.
- Chưa xác nhận lỗi chức năng production cần sửa trong các tình huống đã chạy. Sửa hai giả định của verifier: raw legacy metadata phải qua normalizer hiện có; đồ đang đeo phải chọn equipment slot. Giữ assertion riêng bảo toàn gear nghiệp vụ và so full canonical snapshot. Không sửa SaveManager hoặc làm yếu kiểm tra để qua test.

## 4. Kiểm thử thực chạy

Unity ghim **6000.5.0f1**. Lần tích hợp cuối dùng `.utmp/foundation-56a543746eb545d9a6d1ec768723ed18`, không failure:

- **PASS** MainGame Update thật: auto combat/kill → unique loot → Inventory/equip/unequip → damage source **1010** → Forge đồ đang đeo → source **1012** → lưu/tải → combat sau Load. Kill với NoDrop không tăng gear. Duplicate death callback được thực sự gọi và bị chặn cấp lại.
- **PASS** VFX rơi/chờ/thu nhỏ, detach ground, Pause và fallback; kết thúc không thưởng thêm. Forge thường tiêu 1, đặc biệt tiêu 10; cấp 100 bằng nút dev sẵn có, còn 89.
- **PASS** Load trong khi Forge mở đóng modal cũ; khôi phục map/XP/gold, IDs/count, equipped/stock, rolls/history/stats. AutoSave coroutine 10 giây thực ghi file tải được. Manual1/2, rollback, ReturnTitle AutoSave/Load; New chỉ reset Auto của profile, hai file Manual giữ nguyên byte.
- **PASS 12/12** native Play Mode: `SaveLoadPlayModeTests` 3, `InventoryComparisonTests` 9.
- **PASS 51/51** native Edit Mode: `JsonUtilitySaveLoadTests`, `EquipmentVersionGuardTests`.
- **PASS 4/4** Mono `--forge`; **PASS 23/23** `--loot-visuals`. Mono dùng test doubles; native JSON và MainGame ở trên xác minh phần Unity thật.
- **PASS** `git diff --check`; metadata checker 0 lỗi, 3 cảnh báo folder Lake/Sea/Steppe có sẵn. Font cache và 15 meta tự sinh trong lượt chạy đã dọn khỏi thay đổi Git.
- **FAIL đã xử lý, chỉ thuộc verifier:** so raw/canonical metadata và chọn đồ đang đeo như bag. Replay đối chiếu read-only rồi lần chạy đầy đủ cuối đạt; không có lỗi Save/Load chưa rõ nguyên nhân. Không quá hai vòng sửa–kiểm thử cho từng vấn đề.
- **NOT RUN:** Windows Player, chuột native, trực quan 600×338/1150×647, đầy đủ B2/tên dài/Retry/Travel/mọi trường hợp resize và VFX. Hai lỗi Mono distance-wave/magic-persistence lịch sử ngoài phạm vi không chạy lại, không gán PASS cho mã hiện tại.

Ảnh trực tiếp và bằng chứng nằm ngoài Git ở `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\overnight-foundation`: HUD 800×450, Inventory/Forge 1360×680, drop/land/flight/equipped/loaded, `result.json`, XML test và tóm tắt tích hợp. Đã xem ảnh HUD/Inventory, loot và Forge; không suy diễn toàn bộ animation từ ảnh tĩnh.

## 5. Vấn đề còn tồn tại

- **P0:** không phát hiện trong các ca đã chạy; không có kết luận audit toàn bộ dự án.
- **P1 / quyết định sản phẩm:** cờ `controlledLootTest` production vẫn Off, khi tắt là NoDrop. Vòng thưởng bình thường chưa có đặc tả để tự triển khai. Nguyên liệu Forge được thử qua dev tool; không tự thêm nguồn farm.
- **P1 / giới hạn nghiệm thu:** input native/Windows Player và ma trận UI trực quan còn NOT RUN như trên.
- **P2:** cảnh báo metadata 3 folder và log Editor.Search/video pause/TLS đã có từ trước; không sửa module ngoài phạm vi. Không coi các cảnh báo này là lỗi gameplay đã chứng minh.

## 6. Công việc tiếp theo

1. Chốt đặc tả loot production và nguồn nguyên liệu Forge; không bật cờ thử để thay luật thưởng.
2. Nghiệm thu bằng chuột thật trong profile cô lập tại 600/800/1150, compact/expanded và resize khi loot bay.
3. Kiểm Windows Player cùng chuỗi lưu/tải, Pause/modal và UI hit-test.
4. Kiểm phần còn lại của ma trận B2/tên dài/Retry/Travel trên trạng thái đã tải.
5. Xử lý riêng lỗi Mono distance-wave/magic-persistence nếu tái hiện trên mã hiện tại và được ưu tiên.

## 7. Trạng thái bàn giao

Có thể mở Unity **6000.5.0f1** và **mở rõ `Assets/Scenes/MainGame.unity`** để chơi thử; Last Scene có thể đang là bản sao verifier CLI, không dùng nó làm scene sản phẩm. Verifier đã đóng phiên Editor do nó tạo. Muốn lặp kiểm có thưởng phải dùng profile cô lập theo báo cáo pha 4, không dùng Saves thật hoặc bật cờ production.

Đã xác minh vòng kỹ thuật bằng MainGame thật trong môi trường thử; chưa tuyên bố vòng gameplay production hoàn thiện. Chỉ giữ commit cục bộ cho người dùng kiểm tra. Hoàn tất giai đoạn 4 và dừng.
