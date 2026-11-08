# Thiên Quan 0.1.5 — lựa chọn xử lý gián đoạn

## Hiện trạng đã kiểm tra

Một attempt bắt đầu khi snapshot nguồn và receipt Active đã ghi xong. Trong trận, Retry, New, Menu, Load và Travel cùng đi qua guard của attempt. Kết quả cuối đi qua candidate-before-swap và exact expected hash. Lỗi ghi giữ Hero/session hiện tại và cho phép thử ghi lại cùng candidate. Khi mở lại một AutoSave Active, fixture từ chối resume trước candidate swap và giữ bytes. Receipt hiện tại chứa định danh và provenance, chưa chứa HP, bước di chuyển, cooldown, warning, projectile hay trạng thái RNG cần để dựng lại trận.

Manual Save cũ là timeline riêng. Idempotency theo attempt và slot giữ kết quả ghi đúng một lần trong timeline đang chạy. Nếu luật product cho phép quay về Manual Save trước attempt, người chơi có thể chọn timeline cũ để tránh hậu quả của attempt. Một ledger vượt slot là quyết định sản phẩm riêng, cần xét tương thích backup, rollback và quyền sở hữu.

## Phương án cần duyệt

| Phương án | Thoát chủ động | Crash/khởi động lại | Rủi ro lách và chi phí |
| --- | --- | --- | --- |
| Giữ Active chờ xử lý như fixture | Guard chuyển vùng/menu | Hiển thị lý do và bảo toàn file | Bảo toàn chính xác, có thể chặn người chơi khỏi slot vô thời hạn |
| Cho restart cùng attempt | Xác nhận rồi bắt đầu lại trong cùng ID | Tải lại phase đầu với cùng ID | Chặn cấp kết quả trùng. Alt+F4 sát lúc chết vẫn có thể tránh một lần thua khi reset HP/timer; giữ nguyên seed chỉ giảm reroll |
| Resume từ checkpoint chiến đấu | Lưu Active hợp lệ rồi quay lại checkpoint | Khôi phục state sống gần nhất, đồng hồ đứng trong thời gian offline | Cần version mới, định danh logic cho Hero/target/projectile, trạng thái RNG/cooldown/warning và kiểm tra CAS mỗi mốc. Nếu checkpoint thưa, rollback damage trong cửa sổ giữa hai mốc vẫn có thể bị lợi dụng |
| Bỏ cuộc có xác nhận, crash giữ Active | Người chơi thấy lượng Foundation bị trừ trước khi xác nhận, nếu luật được duyệt | Giữ Active hoặc resume theo lựa chọn độc lập | Tách intent khỏi crash. Alt+F4 không tương đương xác nhận. Đòi duyệt bảng penalty và xử lý rõ quyền Load mốc cũ |

Ưu tiên xem xét resume có giới hạn rollback. Các điểm ghi cần có ít nhất HP hai phía, phase/step/strike ID và phần lẻ timer, lịch projectile, seed hoặc trạng thái RNG riêng, config fingerprint, owner/attempt và previous hash. Điểm chết và điểm thắng cần quyết toán thành snapshot terminal nguyên tử trước khi hiển thị kết quả. Nếu muốn chống Alt+F4 ngay sát từng strike, cần durable commit ở các mốc tác động hoặc journal đồng bộ, rồi đo chi phí I/O. Không suy ra luật phạt từ tình trạng process bị đóng.

Quyết định cần duyệt là hành vi cho nút thoát, hành vi crash, mức rollback có thể chấp nhận, quyền dùng Manual Save cũ, và cách xử lý slot Active khi cập nhật config. Các lựa chọn này là đề xuất. Gate hiện **OFF**.

[Hợp đồng snapshot v1](TRIBULATION_SAVE_TRANSACTION_V1.md)
