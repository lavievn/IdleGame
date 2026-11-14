# CP1 — Baseline RC3

Checkout độc lập `D:\IdleGame\Projects\RC3OvernightIntegration`, nhánh `feature/rc3-overnight-integration` từ `8aaff49397af73a8f705c27dd094317c7bcee1ae`. Local và remote của Draft PR #28 trùng SHA và checkout nguồn sạch. Phục hồi 67 file vendor vào RC3, 95 file theo manifest RC2 khớp. Không ghi đè bản build hay Save cá nhân.

- Opening đã có trạng thái quest, nút map, viền đích và fallback Travel. Hành trình có một click xác nhận Travel sau khi đã click đúng đích. Điều chỉnh nhánh hướng dẫn, giữ thao tác chọn đích thông thường và lore.
- Bộ audio đã thay từ ZIP 191 OGG, có manifest và parser theo prefix. Nguồn Drive đúng hash đã tìm thấy trong cache. RC3 yêu cầu lại Meadow 2 + 1 và Morning 3 cho rừng/đồi núi, khác rule mới nhất của PR #27. Giữ gain Meadow 0,30 đã duyệt. Auto-review chặn cập nhật map đồi núi, nên Morning tiếp tục chỉ áp dụng cho rừng. Giới hạn Coast weather đang chạy là preset thử và cần nghiệm thu.
- Baseline PR #28 Edit 50 PASS / 1 FAIL, Play 124 PASS / 10 FAIL, 0 SKIP. Danh sách lỗi từ XML được đối chiếu lại trong CP4.
- Dual Progression đã có Windows smoke từ PR #28. Tinh Huyết chưa có đường tới Kim Đan và khóa gameplay. Chuẩn bị readiness và kiểm thử biên ở checkpoint tích hợp.
