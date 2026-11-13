# RC2 — Nghiệm thu hướng dẫn nhiệm vụ Hàn Vụ

- Nhánh `feature/rc2-audio-opening-quest`, [Draft PR #27](https://github.com/lavievn/IdleGame/pull/27).
- Điểm bắt đầu `17ddb9359e38c73a74e1e5f6dc4412d6831d52cf`.
- Opening Quest `7cfc80d051beb8112992782afe3d9060b0a8e866`.
- Kiểm thử nguồn audio `7bddeb259df312225e5a4d9662c9e79635b2fd47`.
- [Manifest và báo cáo audio](RC2_AUDIO_OPENING_REPORT.md) · [Kiểm kê OGG](ENV_MS_INVENTORY.csv) · [Bảng map](ENV_MS_MAP_FAMILIES.csv).

## Audit và thay đổi

| Bước | Trạng thái đã kiểm tra |
| --- | --- |
| New Game và thoại mở đầu | Sáu lượt thoại có số trang. Người chơi có thể đọc tiếp hoặc để sau. Nhận lời và từ chối vẫn là thao tác riêng. Bản lưu giữa thoại giữ trạng thái quest và lời kể đã ghi nhận. |
| Đóng thoại | Nút Xong đóng hội thoại thường. Lựa chọn truyện đóng phần chọn sau khi ghi nhận. Nền panel không nhận raycast. Thử nút tên map và Nhật ký qua `UIManager.HandlePointerClick`. |
| HUD và World Map | Sau khi nhận quest, HUD tô nút tên map và hiện lời nhắc. World Map thay lời nhắc HUD bằng đích có thể Travel. Nếu đường đi tiếp chưa mở, HUD chỉ rõ yêu cầu 5 đợt, còn bản đồ nêu đích và lý do khóa. |
| Chọn đích | Chọn polygon khác vẫn cho Travel theo luật hiện có và giữ lời hướng dẫn quest. Chọn Cô Hồn Lâm hiện chỉ dẫn bấm Travel, vị trí Hero giữ nguyên đến khi xác nhận. |
| Travel và chiến đấu | Đóng bản đồ theo đường Travel hiện có, bỏ highlight và hiện số lá bạc `0/5`. Combat tự động vẫn chạy sau entry. |
| Phục hồi | Load lấy lại đích từ trạng thái quest. Ẩn/hiện gợi ý dùng tùy chọn đã lưu. Travel, Retry, callback cũ, ghi nhận thảo dược và manh mối dùng guard hiện có. |

Bản đồ vẫn tạm dừng bằng cơ chế sẵn có trong lúc mở và khôi phục trạng thái trước đó khi đóng. Chỉ hướng dẫn quest có thể đi ngay được tô vàng. Quyết định nội dung truyện, điều kiện Travel và phiên bản Save giữ nguyên.

## Kiểm thử

| Nhóm | Kết quả |
| --- | --- |
| Unity 6000.5.0f1, MainGame Play Mode | PASS, 9 nhóm. Gồm Hàn Vụ, Save/Load, Retry, lựa chọn truyện, xử lý pointer, chọn sai/đúng map và mục tiêu sau Travel. |
| Độ phân giải | PASS ở 600×338, 800×450, 1150×647 qua harness. Footer đích vừa khung, ảnh bằng chứng ở thư mục Play Mode. |
| Audio | PASS 35 bài parser/scheduler đã xác minh ở PR #27. Play Mode lần này kiểm tra nguồn thật và RNG cố định, gồm 3 Coast, 4 Night, 2 Pebble, 1 Rain, 1 WindRain, Rain ↔ Storm, Clear và Travel khi đang fade. Reset tái sử dụng pool. |
| N3 và N4 | PASS 30/30 và 35/35, bằng Mono/UnityStub và ranh giới I/O giả lập của repo. Khớp baseline RC2. |
| Item/Loot/Forge và EXP Bank | PASS 19/19 và 50/50 domain. Khớp baseline RC2. |
| Unity Edit Mode và Presentation | NOT RUN toàn bộ. Baseline đã ghi 1 lỗi Edit Mode và 10 lỗi Presentation. |
| Windows Development build | PASS, 0 lỗi và 58 cảnh báo, Mono x64. |
| Windows Hàn Vụ | 9 nhóm PASS, WARP, gồm pointer và nguồn audio. Tiến trình FAIL khi thoát. |
| Windows tích hợp | 9 nhóm PASS, D3D11, gồm Hero, Rain Maker, Travel/BGM, EXP Bank, Thiên Quan và Save/Load/Retry. Tiến trình FAIL khi thoát. |
| Windows Inventory/Forge | 5 nhóm PASS, WARP, gồm loot/equip/compare, Forge, Save/Load/Retry và Thiên Quan. Tiến trình FAIL khi thoát. |
| Chuột/phím vật lý và nghe phối âm | NOT RUN. Kiểm thử pointer đi qua tọa độ màn hình và handler Unity. |

Windows Development Player `D:/IdleGame/Builds/Rc2OpeningAcceptance/build/IdleGameV10.exe`.

Log Play Mode `D:/IdleGame/Logs/Rc2OpeningAcceptance-PlayMode-r3.log`. Ảnh ở `D:/IdleGame/Projects/Rc2AudioOpeningValidation/.utmp/foundation-890344916cd443c09921f832c1c8c0ca/evidence`.

Ba lượt Windows đều ghi kết quả gameplay trước khi gặp lỗi `UnityPlayer.dll 0xc0000005` tại offset `0x1dd029`, exit `-1073741819`. Lỗi cùng dấu hiệu với các build trước của PR #27. RC2 báo exit 0 ở những lượt baseline đã lưu. Chưa xác định nguyên nhân hoặc xác minh đóng cửa sổ bằng thao tác native.

Profile thử nằm trong `D:/IdleGame/Builds/Rc2OpeningAcceptance/profiles`. Mỗi profile có `player.log`, `evidence/player-result.json` và ảnh.

- Hàn Vụ `idlegame-v10-4b62dd4e6974466586c2cf90989b19f3`.
- Tích hợp `idlegame-v10-5537a6e0e68c445aac2eb42d7d775361`.
- Inventory/Forge `idlegame-v10-4dcb766a074c44d59ed077e713c16658`.

SHA-256 trùng manifest đã lưu cho 375 tệp RC2 và 375 tệp của từng bản rollback. `git diff --check` PASS.

178 mục manifest trỏ đến audio đã khớp SHA-256 trong ZIP. Cấu hình chờ duyệt vẫn gồm 12 tên file, các map Lodges và map thiếu family, Meadow rain trong Storm, nhịp Thunder thử nghiệm và phối âm cuối. Danh sách cụ thể trong báo cáo audio.
