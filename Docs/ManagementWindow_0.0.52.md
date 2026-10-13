# 0.0.52 — Cửa sổ quản lý động + Kho đồ tu tiên

## Định hướng chốt ngày 2026-10-13

- Theo phản hồi tham khảo hình ảnh, **không dùng giao diện pixel** cho quản lý trang bị. Kết cấu gợi ý là mực tối / ngọc thanh / kim loại ấm, ưu tiên chữ rõ, chừa chỗ cho trang sức cổ, linh khí và họa tiết phù văn ở vòng làm đồ họa.
- **Khung chiến đấu** luôn giữ kích thước logic, tỷ lệ và góc dưới trái. Giao diện quản lý mở thêm vùng bên phải và phía trên, không tái khởi tạo Map, Hero/Monster hay projectiles.
- **Hai trạng thái**: chiến đấu gọn; cửa sổ quản lý (tạm dừng gameplay trong lúc mở). Không có cơ chế resize tự do.
- **Lưới túi 5 × 4**, phân trang; tab Tất cả / Trang bị / Vật phẩm. Mỗi món trang bị có mã độc lập. Hộ phù mẫu vẫn hiển thị và trang bị như phiên bản đã thử.
- **8 vị trí trang bị** ở khu trái. Bấm ô đang mặc để xem và tháo; bấm món trong túi để xem thuộc tính chính/phụ, cấp/phẩm chất, cấp rèn, số dòng phụ. Nút Trang bị, Nhẫn phải, Tháo.
- Khi mở thêm chiều cao, **góc trên trái** có vùng thông tin tu sĩ, còn màn chơi cũ giữ nguyên phía dưới. Đây mới là khung giao diện/typography bằng các phần tử Unity, **chưa phải bộ sprite/texture hoàn chỉnh** của hình tham khảo.
- UI sau này cho Tâm pháp/Võ học/Luyện chế có thể dùng cùng `ManagementWindowLayout`, nhưng chưa có menu nghiệp vụ cho chúng trong đợt này.

## Kích thước / cách chọn vùng

- Gọn mặc định **800 × 450** (theo `TransparentWindow.startupWidth/startupHeight`) hoặc một kích thước gọn hiện tại 600 × 338 / 1150 × 647.
- Nếu màn hình đủ chỗ, thêm **560px chiều ngang** phía phải; mở rộng chiều cao **đến 680px** (nếu diện tích màn hình cho phép), vẫn giữ vùng chiến đấu gốc.
  - Ví dụ **800 × 450 → 1360 × 680**; khung chiến đấu vẫn 800 × 450 góc dưới trái.
- Không có ít nhất **420px** ngang bổ sung thì **không resize**, Inventory trở thành lớp phủ giới hạn trong cửa sổ gọn, vẫn tự tạm dừng combat.
- Windows standalone: `TransparentWindow` tìm vùng làm việc của màn hình, đổi kích thước và chỉnh top-left của outer window để giữ **bottom-left của phần game** tại vị trí cũ, trừ khi bị giới hạn bởi mép màn hình. Nếu đóng trong lúc đang resize, yêu cầu thu về được xếp hàng, tránh mất lệnh.
- Unity Editor: không sử dụng Win32 popup; `Screen.SetResolution` có thể bị giới hạn bởi Game View. **Phải thử Windows build thực tế** để xác minh hiệu ứng nở cửa sổ. Nếu Game View chưa thay cỡ, fallback phủ trong cửa sổ cũ.

## Bảo vệ gameplay

- `EnvironmentManager.SyncBattleProjection` chỉ thay tỷ lệ hiển thị và góc neo Canvas dựa vào **chiều rộng gọn**, không thay `battleArea.sizeDelta`, sinh quái hay hệ tọa độ chiến đấu. `Canvas.scaleFactor` giữ như trước trong lúc nở.
- `UIManager` chuyển HUD chiến đấu vào `Compact HUD Viewport`, neo góc dưới trái; inventory nằm ở Canvas ngoài cùng. Chuyển đổi giao diện không tái tạo combat objects.
- `ManagementWindowLayout` nhớ kích thước gọn độc lập với kích thước đang đổi, tránh học nhầm kích thước mở rộng khi thu nhỏ và tránh lỗi lúc SetResolution chạy bất đồng bộ.
- Trước khi mở Inventory ghi lại trạng thái Pause hiện tại. Lúc đóng chỉ khôi phục trạng thái cũ; không cưỡng ép game chạy nếu trước đó đã tạm dừng.
- Mọi thay đổi chỉ nằm trên `feature/0.0.52-inventory-equipment`; `main` và 0.0.51 không bị gộp.

## Kiểm tra

- `Tests~/ManagementWindowRegression.cs`: hình học 800×450 → 1360×680, giữ tỷ lệ Canvas, offset X, chuyển trạng thái chờ resize, 600×338, 1150×647, thiếu không gian màn hình, thay đổi kích thước gọn khi SetResolution đang chờ.
- `Tests~/MotionRegression.cs` đăng ký bài test mới. Chưa có môi trường Unity/C# compiler để chạy test, Play Mode hoặc Windows build. Rà soát tĩnh cú pháp dấu ngoặc và đường gọi tích hợp đã xong.

**Bài thử Windows build đề xuất (nên sao lưu thư mục Saves trước):**

1. Vào màn chơi ở 800 × 450, quan sát Hero/quái/đòn đánh. Nhấn Kho đồ, cửa sổ dự kiến thành khoảng **1360 × 680** trên màn 1920 × 1080; vị trí và tỷ lệ khung game **800 × 450** không đổi.
2. Chọn 8 ô trang bị và chuyển tab, chọn vật phẩm, xem chỉ số, trang bị kiếm và hộ phù, tháo; thử hai nhẫn nếu file save đang có.
3. Khi mở kho, combat dừng; đóng cửa sổ, game tiếp tục. Nếu đang Tạm dừng từ trước, đóng kho vẫn phải giữ Tạm dừng.
4. Mở–đóng liên tục, nhất là ngay lúc cửa sổ đang nở, không được kẹt ở độ phân giải lớn.
5. Thử thay đổi từ 600 × 338 và 1150 × 647; thử máy có kích thước màn hình nhỏ / thanh tác vụ chiếm chỗ để kiểm tra phương án phủ.
6. Khi đóng kho, Hero/quái còn nguyên vị trí và mục tiêu; save/load level, map, và đồ không đổi.
