# B2 visual hotfix — Y-depth và nền xa thị sai

**Nhánh kiểm thử:** `fix/b2-y-depth-parallax`, tách từ `feature/b2-combat-target-routing`.
Giữ nguyên `main`, checkpoint B1, nhánh B2 và thuật toán Combat/Camera/Save.

## Sửa thứ tự che khuất

- Trong cùng Canvas/Ground, **tọa độ chân Y cao được vẽ trước (ở xa)**; Y thấp được vẽ sau (ở gần), tức đối tượng gần che đối tượng xa.
- Thứ tự động áp dụng cho vật trang trí, tất cả Hero đã spawn còn đang hiển thị (gồm Hero đã chết nhưng chưa ẩn) và quái sống.
- Bộ sắp xếp chỉ đổi sibling order khi thứ tự chiều sâu thay đổi, nhằm tránh bắt Canvas dựng lại thứ tự ở mọi khung hình.
- Phần nền, các dải cỏ, HUD và lớp đạn/VFX không nằm trong nhóm sắp xếp theo Y. HP/text sát thương vẫn là con của actor.
- Lấy trục chân/vị trí RectTransform làm mốc; có thể tinh chỉnh lại pivot của asset sau khi dùng hình nhân vật mới.

## Nền xa thị sai

- Thay ảnh xám đứng yên `Ground Fixed 1.2` bằng **3 ảnh `Ground Far Parallax 1–3`** dùng cùng sprite nền và tông xám trước đây.
- Mỗi ảnh rộng/gấp `1.6` lần đơn vị Ground (và cao `1.6` lần) theo cấu hình mặc định `farLayerScale`.
- `farParallaxSpeed = 0.3`: lớp xa dịch chuyển **30% quãng dịch của Ground** ở mỗi lần pan, theo cùng hướng.
- Cuộn lặp 3 ảnh, bù nối mép 1 đơn vị để tránh hở; không thay đổi kích thước hay tọa độ logic chiến đấu.
- Chỉ là hiệu ứng trình bày: không tác động hitbox, vị trí Hero/quái, tốc độ Camera, tầm đánh và thứ tự wave.

## Kiểm thử bắt buộc trong Unity Editor

1. Chạy 1 Hero và nhiều quái ở Y khác nhau: **Y thấp che Y cao**, cả khi Hero đổi vị trí Y; quái với quái, quái với vật trang trí.
2. Thử 2 Hero có tọa độ Y khác nhau nếu đã dùng API đăng ký B2; hạ một Hero: xác vẫn giữ đúng thứ tự sâu, Hero còn lại tiếp tục chơi.
3. Cuộn nền dài, quan sát `Ground Far Parallax`: đi cùng hướng lớp Ground chính nhưng chậm ~70% (quãng đường còn 30%). Không có vệt trống khi lớp xa cuộn qua mép.
4. Resize preset 600 / 800 / 1150; kiểm tra vật trang trí, nền xa và hiệu ứng sát thương, đạn, thanh HP không bị thứ tự mới che khuất bất ngờ.
5. Chạy tiếp wave, chết/Retry, New/Continue và pause: không làm đổi vị trí Ground logic hay tình trạng lưu.

**Giới hạn:** chỉ đã đối chiếu tĩnh mã nguồn; chưa chạy biên dịch hoặc Unity Play Mode cho nhánh này. Nếu sprite nền xám gần như đồng màu, thị sai vật lý vẫn tồn tại nhưng khó quan sát; có thể cần họa tiết/texture nền xa khác trong giai đoạn hoàn thiện hình ảnh.
