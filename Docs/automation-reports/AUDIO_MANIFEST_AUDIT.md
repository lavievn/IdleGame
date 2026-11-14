# CP3 — Audio manifest

Bản ZIP đúng trong cache Drive được lưu dưới tên `new-confirm.html`, SHA-256 `650aa81e8cc3d9d4b6ad668e6338d7001c11a825a3c05d25be1cc2f80037918a`. Kiểm CRC, tên, SHA-256 ZIP member và asset Unity khớp 191/191. 191 .meta tồn tại. Manifest dùng 178 clip, GateThunder riêng cho Thiên Quan, 12 clip ngoài scheduler. Các bản cache có đuôi .zip và hash khác không được dùng. Chi tiết từng file ở [kiểm kê](ENV_MS_INVENTORY.csv), nhóm map ở [bảng mapping](ENV_MS_MAP_FAMILIES.csv).

- Coast ba nền và một one-shot mỗi 10–15 giây. Hai clip Rain và một Storm đã được thử với preset tối đa hai nguồn weather. Giới hạn RC3 cuối **NEEDS_REVIEW**, giữ tham số `coastWeatherLoopLimit`. Overlay toàn cục có slot riêng.
- Meadow khô hai loop cộng một cricket. Chỉ hai loop đổi sau 300 giây. Gain từng clip 0,30. Wet Meadow độc quyền trong family và chỉ dùng khi LightRain. Chọn riêng vai trò Meadow_light_rain/Meadow_rain và mức Storm **NEEDS_REVIEW**, giữ cùng pool LightRain.
- Morning chọn tối đa ba loop, đổi sau 120 giây, bỏ giới hạn giờ. Rừng khô có Morning, kể cả ban đêm khi Night overlay thêm. Giữ loại Morning khi mưa. **BLOCKED** cho 53 map đồi núi. Auto-review từ chối sửa bảng map vì ghi nhận chỉ dẫn trước loại Morning khỏi đồi núi, trong khi đặc tả RC3 yêu cầu dùng lại. Cần xác nhận ưu tiên giữa hai chỉ dẫn để áp thay đổi đó.
- Night, Primeval, Pebble, Rain, Thunder, WindRain giữ logic thời tiết riêng. Lodges hỗ trợ một loop và chưa có map được gán. Clip `Coast_near_water`, `Meadow_cricket2`, `Meadow_wind` và 9 clip `*_base` **NEEDS_REVIEW**.
- Gain theo file và theo family chỉnh được trong manifest. Hai nhóm xoay cho tối đa một tập cũ fade cùng tập mới. Sau fade, giới hạn ổn định lại là 2 + cricket hoặc 3. Fade không chồng Meadow khô và ướt. Pool AudioSource tái sử dụng.

Scheduler deterministic, hash audit và Unity AudioSource ghi riêng trong báo cáo tích hợp. Kiểm thử âm thanh nghe trực tiếp vẫn NOT VERIFIED. Bảng kiểm kê giữ metadata tại lần nhập bộ ZIP. Quy tắc Meadow/Morning mới được ghi ở đây và trong scheduler.
