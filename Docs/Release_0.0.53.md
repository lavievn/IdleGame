# 0.0.53 — Điều chỉnh âm lượng nhạc nền (14/10/2026)

**Nhánh:** `feature/0.0.53` tách từ `main` tại commit `9caaf09ce5176ced487b6db6f47289fd2d737c97`. **Chưa gộp main**; cần thử trên Unity Editor và bản Windows.

## Tài nguyên nhạc nền đã xác nhận trên main

- `Assets/BGM`: 19 bài BGM trong năm nhóm + `opening.ogg` = **20 bản âm thanh**, có đầy đủ `.meta`.
- `Assets/Resources/MapBgmCatalog.asset` đã được commit, tham chiếu `opening` và nhóm 4 / 4 / 4 / 3 / 4 bài.
- Nhóm BGM4 hiện có **3 bài**; vẫn chờ bài thứ tư. Không thay đổi ánh xạ chủ đề hoặc mã chuyển âm 10 giây.
- Không có bằng chứng đã kiểm thử nghe được trong Windows build của thay đổi 0.0.53; sự tồn tại của clip và catalog không tự xác nhận phát đúng.

## Chức năng mới

- Thêm nút **Âm lượng** cùng kiểu với nút **Kho đồ** (62 × 24) trên hàng công cụ phía trên bên phải, **ngay bên phải Kho đồ**.
- Để không đè nút Chỉ số, DEVB, Tạm dừng: Kho đồ dịch sang trái một ô; Âm lượng lấy vị trí Kho đồ cũ. Các nút khác không đổi.
- Nhấp nút Âm lượng mở/đóng bảng nhỏ có **giá trị %**, **−10%**, **+10%**, và **Tắt nhạc / Bật nhạc**. Mức 0–100%, mặc định 60%. Nút âm lượng không nằm trong System Menu; có thể chuyển sau.
- Nút và bảng ẩn khi mở kho, các cửa sổ modal, màn hình bắt đầu hoặc Game Over. Bảng không bắt gameplay phải dừng.
- Dùng `PlayerPrefs` khóa `IdleGame.BgmVolume` lưu thiết lập máy, **không** gắn vào file Save và không bị New Game xóa. Mở lại bảng có % thực tế; tắt/bật không mất mức vừa sử dụng trong phiên.
- Âm lượng tác động **toàn bộ nhạc BGM kể cả opening**, áp dụng ngay với bài đang phát **và trong pha chuyển âm 10 giây**; không restart nhạc, không thay đổi thể loại bản đồ.
- **SFX, voice over chưa có**. Bảng hiện chỉ điều khiển âm lượng nhạc, không phải master volume của mọi âm thanh tương lai.

## Tệp thay đổi

- `Assets/Script/MusicVolumePanelUI.cs` và `.meta`: UI riêng dùng `CustomInteractable`, dễ chuyển sang System Menu.
- `Assets/Script/UIManager.cs`: nút 62 × 24, mở bảng, phối hợp hiển thị, bố cục khi đổi kích thước.
- `Assets/Script/MapBgmController.cs`: API đọc/đặt âm lượng, lưu `PlayerPrefs`, áp dụng ngay cho 2 kênh crossfade.
- `ProjectSettings/ProjectSettings.asset`: `bundleVersion: 0.0.53`.
- README/tài liệu tiến trình: cập nhật trạng thái nhạc đã push và mốc mới.

## Kiểm thử Unity/Windows cần người phát triển thực hiện

1. Pull `feature/0.0.53`, đợi Unity nhập rồi mở Play Mode, kiểm tra Console không có lỗi biên dịch.
2. Tại màn hình Chơi mới/Tiếp tục, opening phát như bản trước. Vào game kiểm tra 4 nút Kho đồ → Âm lượng → Chỉ số → DEVB không đè nhau, vẫn nhìn được Tạm dừng (thử chiều rộng 600/800/1150).
3. Bấm **Âm lượng** mở/đóng bảng; **−/+/Tắt nhạc/Bật nhạc** đều nhận click trong Editor và Windows overlay, ngay cả sau đổi kích thước cửa sổ.
4. Khi nhạc đang phát, bấm xuống 30%, 0%, rồi lên 60%: thay đổi ngay, không cần đợi chuyển bài; không ngắt hoặc phát lại từ đầu.
5. Khi gần kết thúc bài hoặc chuyển sang chủ đề map khác, chỉnh âm lượng **trong lúc chuyển âm 10 giây**: cả hai kênh đổi mức hợp lý, không bật nhạc bất ngờ sau khi tắt.
6. Đóng/mở game: mức volume giữ nguyên. Tạo nhân vật mới, Load save cũ: thiết lập âm lượng **không reset**.
7. Mở Kho đồ/System Menu/Game Over: bảng và nút âm lượng ẩn hoặc bảng tự đóng; không gây chồng click với UI kho/rèn.
8. Không làm thay đổi logic trang bị, lưu nhân vật, chiến đấu, BGM1–5 hoặc opening.

**Giới hạn xác minh:** Không có Unity Editor/Windows Play Mode trong môi trường trợ lý; kiểm tra tại đây mới là mã/tài nguyên trên GitHub và rà soát tĩnh.
