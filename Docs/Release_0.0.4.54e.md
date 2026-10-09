# IdleGame 0.0.4.54e — UI-only patch

**Base:** `main` / `9c8cbeaa07f5eef931becddd33d6036262c85943` (54d_prefinal).

## Thay đổi
- Kích thước cửa sổ: nhỏ 600×338, vừa 800×450, lớn 1150×647 (16:9 làm tròn). Menu có đúng 3 tùy chọn; các callback scene cũ chuyển về mức tương ứng.
- Đưa nút Kiếm/Cung/Phép ở x~1760 trong scene vào HUD, giữ nguyên sự kiện đổi chế độ; đặt hàng nút dưới log và hiển thị nút được chọn.
- Bảng chỉ số rút gọn HP/ATK/cấp/EXP, chuyển từ sát ground lên dưới log; tăng độ đậm nền và cỡ chữ theo kích thước.
- Log trên trận chỉ hiện 2 đòn sát thương cuối: `nguồn → đích - sát thương`. Chỉ số/Info vẫn có nhật ký chi tiết.
- Chỉ số đầy đủ bổ sung EXP hiện tại/EXP cần cấp tiếp; xem toàn bộ diễn biến và log phép tính đã ghi.
- Ghi định hướng chỉ số nhân vật có thể ngẫu nhiên theo cốt truyện/linh căn/tâm pháp/võ học/trang bị vào Project_info_dev.txt.
- Không thay damage/HP/EXP/AI/save/resize và logic chiến đấu trong 54e.

## Kiểm thử cần thực hiện trực tiếp trong Unity và Windows
1. Build Windows chạy ở 800×450 trên desktop 1920×1080: xem font, glyph tiếng Việt, nút chức năng, không đè ground.
2. Mở menu Size, đổi 600/800/1150, cả lúc pause và lúc đánh; nút Cận/Cung/Phép đều hiển thị và hoạt động.
3. Kéo cửa sổ từ nút mà không kích hoạt nút ngoài ý muốn; DPI 125%/150%, màn hình phụ.
4. Hero/monster đánh trúng: log gọn, số sau tính damage chính xác; Chỉ số và Info hiển thị dữ liệu đầy đủ.
5. Hạ quái, EXP tăng/lên cấp, mở Chỉ số và tải save: EXP hiện tại/EXP cần đúng.
6. DEVB, menu bắt đầu, game over, nút đóng popup; resize khi đạn đang bay không làm lệch tọa độ combat.

**Độ tin cậy:** chỉ kiểm tra tĩnh và xác nhận nội dung commit qua GitHub; chưa chạy Unity Editor hoặc build Windows. Chờ phản hồi người dùng trước khi làm 54f.
