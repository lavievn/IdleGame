# Tinh Huyết — Readiness RC3

**Gameplay LOCKED.** Chưa có đường gameplay từ Trúc Cơ tới Kim Đan. Không có nút, trigger hoặc gameplay buff.

Domain kiểm cap 1–4 ở Kim Đan, tích lũy qua đồng hồ chủ động, HP sống dưới 30%, hồi đầy đúng một lần, +1 cảnh giới tạm cho ability tương thích và x2 chỉ số tuyệt đối theo whitelist. Top-up chỉ thêm thời gian và một khoản nợ. Trừ 10% căn cơ tham chiếu mỗi charge. Thiếu căn cơ ghi thương tổn và giữ đại cảnh giới. Test RC3 bổ sung sát ranh giới 299/1000 và đúng ngưỡng 300/1000.

Đề xuất bắt đầu thử duration 30 giây ở fixture riêng, so với 20 giây qua các tình huống boss thường. Chốt theo số hành động có thể thực hiện trong một charge, phần trăm trận được buff và mức căn cơ phải trả. Chưa chọn thời lượng production, tốc độ tích charge, dưỡng thương hoặc giới hạn charge sau Kim Đan.

Policy cần chốt trước khi mở gameplay. Ghi heal, charge và nợ cùng candidate Save trước khi publish. Khi Load snapshot Active, đổi sang SettlementPending trên epoch mới, giữ nợ/receipt, dọn buff cũ và không tự hồi HP. Dùng transaction id để settle đúng một lần qua crash, đổi slot, Travel, Retry. Hiện các bước này có kiểm tra domain/codec nhưng chưa nối vào host SaveManager; chưa nghiệm thu Windows gameplay.
