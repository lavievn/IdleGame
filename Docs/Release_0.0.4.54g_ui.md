# IdleGame 0.0.4.54g_ui — Khôi phục bảng chỉ số và hiệu ứng hiển thị

**Cơ sở**: `feature/0.0.4.54f` tại `8ab08b64016fad4f57cc9dc38153bb4bc1b4cad4`. Bản này CHỈ sửa hiển thị, không đổi chỉ số cân bằng, đòn đánh, vị trí world hay save.

## Bảng chỉ số / log

- Bảng HUD lấy lại **toàn bộ** `HeroController.FullStatDetails` thay vì chỉ 3 dòng HP/ATK/EXP.
- Giữ cấp, dạng đánh, HP, EXP, sát thương nền và cộng, chi tiết sát thương theo từng linh căn, tốc độ di chuyển, tốc đánh, thời gian mỗi đòn và tầm đánh.
- Bảng neo trên **bên phải**, bên dưới Chỉ số–DEVB–Tạm dừng; log rút gọn neo trên **bên trái** và bắt đầu cùng cao độ. Ba nút Cận chiến/Cung/Phép đặt dưới log, không đè lên bảng chỉ số.
- Bố cục có chiều cao thay đổi theo số dòng dữ liệu; đọc lại bố cục khi số dòng linh căn/chỉ số thay đổi, không chỉ khi resize.
- Giữ trạng thái modal của .54f: không đè lên màn hình Bắt đầu, xác nhận, Game Over hoặc menu chi tiết. Cửa sổ 600/800/1150 đều sử dụng cùng nguyên tắc chia cột.
- Khung chỉ số được giữ nguyên trên giao diện để theo dõi cân bằng trong giai đoạn thử logic; về sau dự kiến chuyển vào nút riêng.

## Tên bản đồ

- Xuất hiện chữ lớn **giữa cửa sổ** ngay khi vào game và mỗi lần tiến sang bản đồ mới.
- Đứng giữa **3 giây**, sau đó đồng thời thu chữ nhỏ và dịch chuyển trong **0,85 giây** (nội suy tăng tốc/giảm tốc mượt).
- Vị trí đích tạm: nhãn nhỏ phía trên vùng chơi, **cột trái dưới log và nút chọn dạng đánh**; không đè bảng chỉ số cột phải. Nhãn nhỏ còn hiện cho đến lần chuyển map tiếp theo.
- Đóng/ẩn khi quay về màn hình bắt đầu, khi Hero chết hoặc khi giao diện modal đang mở. Không chạm vào map progress, camera hoặc Ground.

## Chữ sát thương Hero và quái

- Chữ hiển thị ngay lúc nhận damage, bay **ra xa hướng xuất phát của đòn đánh** (quái đánh Hero / Hero đánh quái; nổ phép theo tâm vùng nổ) và nhô lên thành quỹ đạo cong.
- Độ mờ giảm đều và kết thúc trong **1 giây**; không sửa sát thương thực nhận, tầm đánh, đạn hoặc HP.
- Vị trí chữ được khôi phục khi hiệu ứng kết thúc hoặc quái tái sử dụng trong pool.
- Dùng quy tắc chuyển động chỉ dành cho giao diện trong `BattleMotion.cs` để bảo vệ các tính toán di chuyển/hitbox của game.

## Đối chiếu và kiểm thử

- Thêm `Tests~/Patch54gRegression.cs` (5 kiểm tra): đủ trường chỉ số, chia cột không chồng nhau, 3 giây giữ tên map và thời gian di chuyển, hiển thị tên map trong giao diện giả lập, hướng nảy + độ mờ 1 giây.
- Đây là kiểm tra nguồn/mô phỏng; **chưa chạy thành công Unity Editor hoặc Windows build** trong môi trường này. Chưa thể bảo đảm hiển thị thực tế không còn tràn ở tất cả trường hợp nhiều linh căn.
- Khi thử trên máy: kiểm tra 600/800/1150; khởi đầu/Game Over/DEVB/Menu; tăng cấp và nhiều linh căn; lần đầu deploy, sau 5 wave chuyển map; bị đánh từ hai phía; tần suất nhiều lần trúng sát thương; resize đang bay đạn.
- Nếu cần cải thiện cân bằng sau đó, làm phiên bản khác, **không pha vào bản UI này**.
