# Bách Luyện Tiên Đồ — Bốn giai đoạn Gameplay Foundation

**Phase 1–3 đã kiểm thử trên Unity và Windows. Phase 4 có bằng chứng domain và Unity native cho dữ liệu, còn PENDING_DESIGN ở runtime.**

- Base `a1b9e64f19c748499bf66277c7569f136625903a` là HEAD Draft PR #27 và của bản local đã nghiệm thu. Remote freeze kiểm tra lúc bắt đầu là `9813b3e6a0b70485fc04e58218e19b8e40846a93`. Nhánh `feature/gameplay-foundation-four-phases`, checkout `D:\IdleGame\Projects\GameplayFoundationFourPhases`.
- Checkout thử đã phục hồi 67 tệp vendor cùng .meta và xác minh 95 tệp theo manifest có sẵn. Asset vendor, Save, log, cache và Windows build không nằm trong commit. Checkout đã nghiệm thu vẫn ở `a1b9e64` cùng thay đổi cục bộ trước đó. RC2 và các bản rollback không bị ghi đè.
- SHA của code được Unity và Windows kiểm thử là `113baa489f3b9068e317b99ef7d235745841c5b7`. Commit bàn giao sau đó chỉ cập nhật báo cáo này và danh sách nguồn của năm runner domain.

## Checkpoint

| Phase | Commit | Kết quả |
|---|---|---|
| [1 — Thiên Kiếp và EXP](GAMEPLAY_FOUNDATION_PHASE1_EXP.md) | `ccb96ddee648ce29aa5591484173b8be58fe1feb` | Domain 52/52 ở checkpoint. Unity và Windows xác nhận thắng giải phóng một lần, thua giữ nguyên, ghi lỗi không áp nửa chừng, khởi động lại có thể tiếp tục xử lý. Hai Gate liên tiếp chỉ có fixture vì game mới có một Gate. |
| [2 — Linh khí môi trường](GAMEPLAY_FOUNDATION_PHASE2_QI.md) | `fcaf2eaa0243875026f465c0595a23cec54633a3` | Domain 54/54. Hàn Vụ ở 1 Qi/s đạt 3.600 tu vi sau 3.600,1 giây chủ động. Map chiến đấu dùng ba dải Y và mức nền từng map. Unity/Windows kiểm tra đồng hồ thật từ New Game và chạm cap từ hồ sơ thử gần bình cảnh. |
| [3 — Mastery](GAMEPLAY_FOUNDATION_PHASE3_MASTERY.md) | `dfd2e80c2646795b99514d6a7987be71f44571b6` | Domain 56/56. Unity/Windows xác nhận các đòn và skill, có một đơn vị đếm mỗi lần thực thi, không tăng chỉ số chiến đấu. Save/Load, Retry và Travel giữ số đếm. |
| [4 — Tinh Huyết và căn cơ](GAMEPLAY_FOUNDATION_PHASE4_BLOOD.md) | `113baa489f3b9068e317b99ef7d235745841c5b7` | Domain 57/57, Unity native JsonUtility và tệp fixture PASS. Bỏ thời lượng hardcode, chưa mở kích hoạt trên Player. |

## Kiểm thử cuối

| Lượt chạy | Kết quả |
|---|---|
| Domain | EXP/Qi/Mastery/Blood 57/57, DualProgressionAudit 30/30, Hàn Vụ N3 30/30, N4 35/35, ItemLoot 19/19, EnvironmentAudio 45 kiểm tra. Runner cũ `Tests~/run.sh --cultivation-related` chưa compile do danh sách nguồn WorldClimate thiếu sẵn. Nhóm Inventory/Forge/Save có bằng chứng từ runner riêng, Unity và Windows bên dưới. |
| Unity 6000.5.0f1 Edit Mode | 50 PASS, 1 FAIL. Trùng case derived health đã biết (kỳ vọng 1554, thực tế 1058). |
| Unity Play Mode | 124 PASS, 10 FAIL. Danh sách test lỗi trùng RC2 lượt owned-save, gồm spawn VFX, HUD và monster awareness. 4/4 SaveLoad PlayMode PASS. |
| Unity MainGame | `MainGame015`, `MainGame016` và `DualProgressionV06Verifier.RunNative` thoát 0 trên hồ sơ thử. |
| Windows Development build | Mono2x, 0 lỗi, 58 cảnh báo. `Playable015` xác nhận Kiếm/Cung/Trượng thắng, tay không thua. `Playable015Replay` mở tiến trình mới, giữ checkpoint khi hủy hoặc ghi lỗi, rồi khôi phục đúng một lần. Cả hai thoát 0. |
| Windows regression | `Playable016` xác nhận Mastery, tài nguyên, Save/Load, Retry và Travel. `Integrated` xác nhận Rain Maker, ngày/đêm, âm thanh môi trường và sấm Thiên Quan. `HanVuOrigin` xác nhận mở đầu, kỳ ngộ, 5 wave, phần thưởng Elite và tương thích Save. `Development017` xác nhận loot, equip, Forge, Save/Load và Gate. Các lượt gọi handler này thoát 0. |
| Windows teardown | Một lượt đóng cửa sổ bằng WM_CLOSE trên RTX 2080 thoát 0. Event Log không có lỗi trùng đường dẫn EXE ở lượt này. Chuột/phím thật và nghiệm thu hình ảnh/âm thanh trực tiếp còn NOT RUN. |

## Mở bản Windows

Mở `D:\IdleGame\Builds\GameplayFoundationFourPhases\build\IdleGameV10.exe` cùng thư mục `IdleGameV10_Data`. Chọn Chơi mới, mở bản đồ từ HUD để sang rừng đánh tự động và nhận EXP. Khi tu luyện tới Viên mãn, mở Nhân vật → Độ Kiếp. Mô phỏng 60 phút dùng thời gian chơi chủ động, hồ sơ thử gần cap chỉ dùng trong các smoke test.

Build SHA-256 `E3C3A181DCB8BAB365F7DAE4788C8F8164DD1DFA8A5E51D5A717C04B1FC3F7E5`. Kết quả build, XML Unity, hồ sơ Windows và `windows-native-close.json` nằm dưới `D:\IdleGame\Builds\GameplayFoundationFourPhases`. Log Editor và domain ở `D:\IdleGame\Logs`.

Cấu hình DEV mới dùng fingerprint riêng theo cơ chế dự án, nên lần đầu mở build có thể thấy hồ sơ mới. Các hồ sơ DEV cũ vẫn ở đường dẫn cũ. Save Mastery tự tương thích với slot thiếu field trong cùng profile. Git diff chỉ chứa source, config DEV, test và tài liệu. File font cache Unity tự ghi đã được tách khỏi diff.

## Thiết kế còn cần chốt

- Gate và bảng progression từ Trúc Cơ lên Kim Đan. Các hệ số mật độ DEV 0,70/1/1,35 và đường biên 25%/75% cần nghiệm thu khi chơi.
- Tinh Huyết cần thời lượng, tốc độ cô đọng, tốc độ dưỡng thương, capacity các cảnh giới cao và policy ghi HP/nợ nguyên tử khi đang Thiêu Đốt hoặc đổi slot/crash. Các giới hạn x2 hiện theo whitelist của hợp đồng, chưa áp runtime. Bản Windows này chưa có hành động Tinh Huyết.
- Mastery hiện đếm số lần dùng. Mốc Mastery và bonus chiến đấu chờ thiết kế. Không thay đổi cân bằng Trúc Cơ, nhịp/HP/damage của Gate, hay gameplay ngoài phạm vi.
