# Thiên Quan 0.1.5 — đề xuất cân bằng

Các giá trị dưới đây dành cho duyệt và thử nghiệm. Production gate **OFF**. Luyện Khí production v3 đang dùng Foundation `[0, 900, 2100, 3600]`, World Qi v2 và Capacity v1.

## Thiên Quan

| Tham số | A · Mốc đối chứng đã chạy | B · Thêm thời gian đọc cảnh báo |
| --- | ---: | ---: |
| HP | 300 | 260 |
| Armor / Magic Resistance | 20 / 20 | 16 / 16 |
| Lightning raw damage | 12 | 12 |
| Strike interval | 1,00 s | 1,25 s |
| Warning duration | 0,26 s | 0,35 s |
| Tầm encounter | 200 | 200 |
| Bước đi trước reveal | 800 | 800 |

A dùng làm mốc cho một lượt pilot. B giảm số hit cần để phá tường và kéo dài khoảng giữa các strike, nhằm kiểm tra độ dễ đọc và khả năng sống sót ở nhóm trang bị phổ thông. Cần chạy cùng ma trận Hero trước khi chọn B. Thời gian trận nên được đo từ khi vào tầm đến HP terminal, tách khỏi thời gian đi bộ/camera. Dùng khoảng trung vị và đuôi phân phối qua nhiều seed cho vòng duyệt kế tiếp.

Windows Player đo các Hero từ config mặc định, seed mở đầu 15015, map TL-16. Mỗi Hero dùng Level đã ghi và vũ khí demo với template ID trong CSV. Không gán trực tiếp HP, attack, kết quả hay xác suất đột phá. Một quan sát mỗi mẫu.

| Level | Vũ khí | HP / attack | Armor / MR | Interval Hero | Kết quả | Giây trong tầm | Strike |
| ---: | --- | ---: | ---: | ---: | --- | ---: | ---: |
| 1 | Unarmed | 800 / 10 | 0 / 0 | 1.4 | Success | 51.819 | 51 |
| 1 | Sword | 800 / 20 | 0 / 0 | 1.4 | Success | 26.605 | 26 |
| 10 | Bow | 326 / 29 | 0 / 0 | 0.893 | Success | 16.388 | 16 |
| 10 | Staff | 544 / 10 | 0 / 0 | 1.34 | Success | 50.152 | 50 |
| 25 | Unarmed | 3368 / 34 | 0 / 0 | 1.25 | Success | 13.754 | 13 |
| 25 | Sword | 3368 / 44 | 0 / 0 | 1.25 | Success | 10.002 | 10 |
| 40 | Bow | 614 / 59 | 0 / 0 | 0.781 | Success | 7.236 | 7 |
| 40 | Staff | 1024 / 10 | 0 / 0 | 1.172 | Success | 43.975 | 43 |

Lượt đầu dừng ở cửa sổ 45 s trên mẫu Level 1 Unarmed. Lượt trên nới giới hạn quan sát tới 120 s và dùng pacing 60 FPS để Hero di chuyển ở nhịp kiểm thử ổn định. Kết quả ghi theo HP và receipt terminal thật.

Với A, khoảng thời gian thắng quan sát 7.236–51.819 s. Mẫu tay không Level 1 có 800 HP, mất 6 HP/strike và đạt 5.79 DPS hữu hiệu. Mẫu Staff Level 10 gần 50 giây dù sống sót, trong khi Bow Level 40 gần 7 giây. A phù hợp làm đối chứng cho tốc độ/độ khó. B chỉ đáng thử nếu mục tiêu duyệt là rút ngắn trận trang bị phổ thông hoặc tăng thời gian đọc cảnh báo. Ma trận này chưa đo B và chưa đủ để suy ra ngưỡng thất bại production.

Điều kiện thử nghiệm ở đây là DPS hữu hiệu sau phòng thủ đủ phá HP trước khi Hero về 0. Với damage mỗi strike `d`, HP `H`, interval `I` và bỏ qua hồi máu, trần sống xấp xỉ `ceil(H/d) × I`, còn DPS hòa vốn xấp xỉ `gateHP / trần sống`. Lần impact trùng frame Hero chết xử thua, nên cần biên an toàn cho roll Combat, windup và travel projectile. Dùng HP/giáp/tầm đánh/vũ khí thật để kiểm tra, Level chỉ mô tả fixture. Thành bại lấy từ Combat.

## Hai phương án trừ Foundation

- **P1 · 10% chiều dài đại cảnh giới nguồn.** Luyện Khí ở bình cảnh trừ yêu cầu 360 trên tổng 3.600.
- **P2 · 40% độ dài khoảng Foundation của tiểu cảnh giới liền trước bình cảnh.** Luyện Khí có khoảng 2.100–3.600 dài 1.500, nên trừ yêu cầu 600.

Cả hai đề xuất đi qua trần lùi tiểu cảnh giới đã duyệt. Luyện Khí và Trúc Cơ tối đa 1 bậc. Kim Đan trở lên tối đa 2 bậc. Phạt giảm Foundation đã luyện hóa, giữ Unrefined Qi, giữ đại cảnh giới, hồi sinh và yêu cầu lên lại bình cảnh để thử.

Mô phỏng lấy tất cả Qi tier từ World Qi v2 và rate Hậu kỳ từ Capacity v1. Absorption cap 1,5 Qi/s, refinement cap 1,16/s, hiệu suất 1, bước 0,1 s. Foundation lúc bắt đầu là 3.600 trừ loss. Mỗi tick thêm `min(Qi môi trường, absorption cap) × dt` vào reserve rồi luyện `min(reserve, refinement cap × dt, Foundation còn thiếu)`. Cột reserve 1.234 dùng ngân hàng đủ bù phần thiếu của toàn lượt. Số liệu là giây gameplay hoạt động, giả định nhân vật không Injury và giữ nguyên node. Thời gian chơi thực tế phụ thuộc pause, node, injury và reserve đầu vào.

| Tier | Qi/s | P1 reserve 0 | P1 reserve 1.234 | P2 reserve 0 | P2 reserve 1.234 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 0.6 | 600.0 | 310.4 | 1000.0 | 517.3 |
| 1 | 0.8 | 450.0 | 310.4 | 750.0 | 517.3 |
| 2 | 1.0 | 360.0 | 310.4 | 600.0 | 517.3 |
| 3 | 1.3 | 310.4 | 310.4 | 517.3 | 517.3 |
| 4 | 1.8 | 310.4 | 310.4 | 517.3 | 517.3 |
| 5 | 2.5 | 310.4 | 310.4 | 517.3 | 517.3 |
| 6 | 3.5 | 310.4 | 310.4 | 517.3 | 517.3 |
| 7 | 5.0 | 310.4 | 310.4 | 517.3 | 517.3 |

## Successor Trúc Cơ để duyệt

| Thuộc tính | Đề xuất A · Độ dốc vừa | Đề xuất B · Refine nhanh hơn |
| --- | --- | --- |
| Realm catalog ID | `realm:truc-co`, tier 1, successor của `realm:luyen-khi` | Như A |
| Sơ kỳ / Trung kỳ / Hậu kỳ / Viên mãn | 0 / 1.200 / 2.900 / 5.100 | Như A |
| Base absorption / refinement | 1,8 / 1,25 | 2,2 / 1,45 |
| Stage factors Absorption | 1 / 1,125 / 1,25 / 1,375 | Như A |
| Stage factors Refinement | 1 / 1,08 / 1,16 / 1,24 | Như A |

Dữ liệu fixture Trúc Cơ 3/2 chỉ dùng để đối chứng hành vi. Cả hai đề xuất tôn trọng trần supply ở node thấp và cần mô phỏng economy trên World Qi v2 trước khi chốt. Khi thắng, candidate đặt Foundation đích về 0, chuyển nguyên reserve, bảo toàn phần lẻ đồng hồ hợp lệ và đổi capacity theo realm thực trước tick tiếp. Giữ ID/profile lịch sử v1/v2/v3 và tạo version mới có fingerprint, migration consent/backup và quy tắc đối với receipt Active. Nhận diện bản lưu cũ theo config gốc, không diễn giải lại dưới bảng threshold mới.

[Điều kiện production](TRIBULATION_PRODUCTION_READINESS_015.md)
