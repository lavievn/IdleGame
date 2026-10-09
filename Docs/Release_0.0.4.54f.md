# IdleGame 0.0.4.54f — Giao diện không đè popup & cân bằng 3 lối đánh

Base: nhánh `feature/0.0.4.54e`, commit `55eadabf289cb134ada2992cfc3f4c2c01da27ad`. Giữ nguyên scene và không sửa logic tọa độ Ground/resize/wave của bản .54d.

## Giao diện
- Ẩn hoàn toàn HUD combat (chỉ số, log, Kiếm/Cung/Phép) khi đang ở màn hình bắt đầu, xác nhận, Game Over, hoặc bảng Info/DEVB/Menu hệ thống. Bật lại khi tiếp tục chơi.
- Ưu tiên hiển thị popup khởi đầu và Game Over lên trên.
- Đặt bảng chỉ số dưới log: tăng rộng và cao, tắt xuống dòng nội bộ và dùng dấu ba chấm thay cho tràn chữ; chỉnh cỡ chữ có phân biệt giữa bảng chỉ số và nhãn nút.
- Phân trang phần thông tin chi tiết thận trọng hơn ở các kích thước cửa sổ 600/800/1150; thêm chữ **Menu** ở nút đỏ bên dưới phải.
- Giữ nguyên sự kiện click nút Menu cũ, nút Kiếm/Cung/Phép, không đụng scene và không làm mới hệ thống cửa sổ trong patch này.

## Cân bằng thử nghiệm (đơn vị nội bộ Ground)
- Chỉ số gốc được lưu dưới tên `originHealth` (400 HP hiện chỉ là mẫu dùng kiểm tra). HP khi lên cấp được tính bằng tăng trưởng hiện có dựa trên originHealth, tránh ghi đè originHealth theo mỗi mode.
- Kiếm: HP nền x2 (tức 800 ở cấp 1 nếu gốc 400); +50–100 HP ở mỗi lần lên cấp trong pool **riêng của Kiếm**; khi giết địch với Kiếm hồi ngẫu nhiên 3–5 nhân (1 + 0,05 × (cấp hiện tại - 1)), làm tròn đến số nguyên.
- Cung: HP nền x0,6 (tức 240 nếu gốc 400); sát thương dạng đánh x0,7; tốc đánh Hero 150% so với Kiếm và Phép trước khi nhân các yếu tố tốc đánh khác (thời gian đòn cơ bản 1,4 / 1,5 giây).
- Phép: chỉ vận sức khi có mục tiêu sống hợp lệ nằm trong tầm; nếu mục tiêu rời tầm, thanh vận sức trở về 0. Hero dùng bán kính nổ **250** quanh vị trí nổ theo tọa độ Ground; giá trị mặc định trước patch trong mã thực tế là **100**, không phải 150. Quái vẫn giữ AoE 100 và thời gian vận phép cơ bản 2 giây; quái cũng không vận sức nếu không có mục tiêu.
- Thưởng +50–100 HP của Kiếm được tính lúc lên cấp **dù đang cầm cung hoặc vận phép**, lưu để khi quay lại Kiếm không mất thưởng hoặc gieo lại. Mức tăng trưởng Kiếm của nhân vật tạo từ save cũ được bù 75 HP/cấp đã đạt **một lần**.
- HP cộng do điểm chỉ số/trang bị tương lai tách khỏi hệ số máu nền. Chuyển dạng đánh giữ tỉ lệ HP hiện tại / tối đa, không cho hồi miễn phí.
- Không thay đổi công thức tốc đánh của quái ngoài điều kiện mục tiêu phép; sát thương cung hiện áp dụng thống nhất x0,7 cho kiểu tấn công (kể cả quái dùng cung).

## Kiểm thử
Đã cập nhật các bài kiểm thử cũ có kỳ vọng không còn phù hợp với yêu cầu (thanh phép tự vận sức, hồi 5–10 HP cho mọi dạng đánh, thời gian đòn, bán kính) và bổ sung 7 bài kiểm tra mới ở `Tests~/Patch54fRegression.cs`. Chưa có xác nhận chạy trên Unity Editor hoặc Windows EXE, vì vậy **không khẳng định các test đã thông qua**.

Checklist người dùng:
1. Chạy 600/800/1150; start/confirm/game-over không có HUD combat đè, không tràn text; nút Menu đỏ có chữ và bấm mở menu.
2. Cấp 1 với HP nền 400: Kiếm tối đa 800, Cung 240, Phép 400. Đổi dạng đánh khi mất nửa HP phải giữ tỷ lệ.
3. Lên cấp nhiều lần: +50–100 cho pool HP Kiếm mỗi cấp; thoát/chạy lại từ save không thay đổi số đã gieo.
4. Kiếm giết địch hồi HP theo cấp; Cung 150% tốc đánh và 70% sát thương; Phép đi một mình không vận sức, có mục tiêu mới vận sức và nổ trong vòng 250.
5. Đo khả năng vượt qua 5 wave đầu và 25 wave/5 map (không hứa tỉ lệ thắng nhất định). Resize trong lúc đạn đang bay và đang tạm dừng: không đổi tọa độ mục tiêu/wave.
