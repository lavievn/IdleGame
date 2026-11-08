# Thiên Quan 0.1.5 — điều kiện mở production

## Trạng thái

Production **OFF**. Profile đang phát hành nhận Luyện Khí và catalog chỉ có một cảnh giới. Factory Thiên Quan yêu cầu config fixture, Save directory sở hữu riêng và profile non-production. Các tham số hiện dùng trong battle fixture và mô phỏng ở tài liệu đề xuất.

## Các điều kiện còn mở

| Điều kiện | Hiện trạng | Tiêu chí trước khi bật |
| --- | --- | --- |
| Successor và capacity | Catalog production v3 chỉ nhận Luyện Khí, config fingerprint theo version | Duyệt ID/threshold/capacity Trúc Cơ, thêm version config mới với fixture đọc v1/v2/v3 và migration có backup |
| Thiên Quan | Combat thật chạy trên MainGame và Windows | Duyệt thông số, đánh giá Hero theo gear/defense và kiểm thử thời gian trận ở nhiều realm |
| Penalty | Đã có trần lùi tiểu cảnh giới và penalty fixture | Duyệt số Foundation thực trừ, thời gian luyện lại, và cách xử lý quit có xác nhận |
| Interruption | Active giữ nguyên và khóa resume trong fixture | Duyệt cancel/restart/resume, rollback window và chính sách Manual Save trước attempt |
| Transaction | Native I/O và Windows restart có chứng cứ | Test migration mới, lỗi ghi ở mọi phase được phê duyệt, ngắt process thật tại các ranh giới ghi |
| Input và môi trường | Matrix bốn cỡ theo fixture | Nghiệm thu native theo phạm vi trong báo cáo, xác nhận các cấu hình Windows và độ phân giải mục tiêu |

Trình tự triển khai đề nghị sau khi duyệt là version hóa catalog/Save config, hiện lý do khóa cho các profile cũ, chạy phép thử upgrade và Active liability trên profile sở hữu riêng, rồi bật qua gate tường minh. Gate phải từ chối nếu thiếu bất cứ profile, successor, penalty, policy hoặc provenance nào. Foundation về 0 ở realm mới. Reserve, các field ngoài tu luyện và phần lẻ đồng hồ hợp lệ được giữ qua candidate. Quyền tiến hóa realm dựa vào căn cơ và trận đánh, không suy ra từ Level.

- [Phương án cân bằng và Trúc Cơ](TRIBULATION_BALANCE_PROPOSAL_015.md)
- [Phương án xử lý gián đoạn](TRIBULATION_INTERRUPTION_POLICY_OPTIONS_015.md)
- [Báo cáo Development 0.1.5](../automation-reports/DEVELOPMENT_015_REPORT.md)
