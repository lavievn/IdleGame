# CP2 — Opening Quest UX

Lần đầu ở Hàn Vụ sau khi chọn Nhận lời, hướng dẫn trên bản đồ nêu rõ nhấn ô Cô Hồn Lâm được đánh dấu để lên đường. Click ô này gọi đúng handler Travel đang có, đóng bản đồ và để entry tự triển khai combat. Click ô khác vẫn chọn đích. Khi đã từng đến Cô Hồn Lâm hoặc ẩn gợi ý, dùng thao tác Travel thông thường. Mở map và click đích khác không nhận quest hay cấp lá bạc.

Hội thoại, lựa chọn và kết quả giữ nguyên. HUD ẩn nút Đóng dư thừa khi phần đang hiện là gợi ý map. Tiến độ dùng quest state và lịch sử map đã lưu, không thêm trường Save.

Baseline MainGame xác nhận vòng cũ và Hàn Vụ. Test domain RC3 gồm Story 12/12, N3 31/31, N4 36/36, 0 SKIP. Lượt smoke đầu của nhánh mới bắt được assertion đọc HUD cùng frame deploy. Smoke chờ một frame để đọc mục tiêu đã vẽ. Kết quả Unity/Windows cuối và giới hạn chuột thật ở báo cáo tích hợp.
