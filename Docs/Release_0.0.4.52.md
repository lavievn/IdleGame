# 0.0.4.52 — đối chiếu thiết kế, thay đổi và cách thử

Nền mã: `main` tại `f6140282`, sau PR #1. Bản mới trên `feature/0.0.4.52`.
Ngày đối chiếu: 09/10/2026, giờ Việt Nam. Chưa nghiệm thu Unity/Windows thực tế.

Nguồn: `AI_Prompt_Master.txt` lịch sử ở `c6be85d8`, `Project_info_dev.txt`
(tổng hợp ba chat Gemini G1/G2/G3 đã đọc ở phiên trước), mã/scene hiện tại,
và yêu cầu mới cùng hai lựa chọn người dùng trả lời trong phiên này.
Các khẳng định AI cũ “đã hoàn thiện tương khắc” không khớp mã: hàm cũ trả 1.
Không có bảng hệ số chiến đấu riêng cho năm tộc được xác nhận trong các nguồn này.

| Hạng mục | Trước 0.0.4.52 | 0.0.4.52 |
|---|---|---|
| Tên | Chọn nguyên một tên trong 10 tên nam hoặc 10 tên nữ | Ghép 1–3 âm tiết, một âm tiết/tên nguồn khác nhau; giữ nhóm giới tính |
| Random tộc | Hero/quái giữ tộc từ dữ liệu mẫu | Cả hero mới và từng đời quái chọn đều năm tộc; tạm 20% mỗi tộc |
| Linh căn | Hero 1–2 căn trong 7 hệ, có thể trùng; quái chưa random căn riêng | 1/2/3/4 căn theo 60/25/10/5%; con lai luôn 2; không trùng; chỉ sinh ngũ hành |
| Khắc hệ | Hàm luôn trả 1, chưa nối chiến đấu | Khắc ×1,25; bị khắc ×0,8; còn lại ×1; cả hai bên và mọi kiểu đánh |
| Màu | Hero theo giới tính, quái theo mẫu | Pha 50% màu ngũ hành với màu gốc; nhiều căn trung bình màu; giữ độ đục |
| Log trên | Một thông báo, chủ yếu EXP khi hạ quái; text có offset lệch | Tên/tộc/căn/cấp/map/đợt luôn hiện, kèm hai sự kiện mới nhất, tên quái và sát thương/EXP |
| Menu hệ thống | Các nút scale/Exit/Save/Load cùng hiện | Trang chính 4 mục; trang tải và trang kích thước riêng, có Quay lại |
| Tải ô lưu | Tải rồi về màn hình chờ | Chọn Save 1/Save 2/Auto thành công thì chơi ngay; lỗi giữ nguyên trận hiện tại |
| Reset | Luồng Chơi mới có hỏi xác nhận | Mục Đặt lại nhân vật → Có/Không; Có xóa ba save, tạo hero mới; Không giữ nguyên |
| Scale/startup | 1000/500/200; startup 1000×563 | 800/500/250; startup 800×450, cao theo 9/16 |
| Độ khó | Người chơi bấm Bình thường/Khó | Bỏ nút, mỗi map 5 đợt, 5 map thường rồi 1 map khó; lưu tiến độ |

## Hệ số và giới hạn thực hiện

Tộc và linh căn là hai trường riêng: Nhân tộc, Yêu thú, Ma tộc, Linh thể, Con lai
khác với Kim, Mộc, Hỏa, Thủy, Thổ. Tất cả tộc hiện có hệ số gây/nhận sát thương
riêng **×1**; chưa đặt thêm bonus chủng tộc, chưa có dữ liệu cha/mẹ hay nguồn gốc
linh thể. Random đều các tộc là lựa chọn triển khai tạm, không phải tỷ lệ lịch sử.

| Hệ tấn công | Khắc: ×1,25 | Bị khắc: ×0,8 | Màu |
|---|---|---|---|
| Kim | Mộc | Hỏa | Vàng |
| Mộc | Thổ | Kim | Xanh lá |
| Hỏa | Kim | Thủy | Đỏ |
| Thủy | Hỏa | Thổ | Xanh biển |
| Thổ | Thủy | Mộc | Nâu |

Cùng hệ hoặc các cặp khác ×1. Nhiều căn: lấy trung bình hệ số của toàn bộ cặp
căn công–thủ, không cộng dồn sát thương theo số căn. Ví dụ Kim+Hỏa đánh Mộc:
(1,25+1)/2 = **1,125**. Đây là cách gộp triển khai cho bản này.

Sát thương cơ sở vẫn theo CombatBalance: ATK × hệ số kiểu đánh (phép1,8,
kiểu khác1) × ngẫu nhiên0,85–1, làm tròn tối thiểu1. Nhân hệ số ngũ hành rồi
làm tròn; quái ở map khó nhân2 khi đòn tới đích. Đạn phép tính riêng từng nạn
nhân, không dùng hệ số của mục tiêu chính cho mọi quái.

Bảng hiệu suất buff đa căn75/70/60% và ví dụ buff Mộc/Hoả trong G2 được giữ
làm định hướng; không áp lên ATK nền vì hiện chưa có hệ buff/skill/trang bị để
chia. Phòng thủ, crit, độc theo thời gian, khống chế và ưu thế tộc chưa tích hợp.
Độc/Băng trong save cũ giữ ID và dữ liệu, tạm trung tính; không sinh mới hai hệ
này. Tên/tộc/căn đã lưu không random lại khi load, retry hoặc lên cấp.

Màu dùng trung bình RGB các căn rồi trộn50% với màu gốc của Image, giữ alpha.
Mỗi đời quái trong pool tính từ màu gốc, không pha chồng lên màu của đời trước.

## Menu và tiến độ map

- Menu chính: Thoát; Tải bản lưu; Đặt lại nhân vật; Kích thước cửa sổ.
- Tải: Save1, Save2, Auto, Quay lại. Slot trống/hỏng báo log; không phá trận.
- Đặt lại: hỏi rõ xóa cả ba save và tạo hero cấp1, Có/Không. Hủy không xóa gì.
- Kích thước:800×450,500×281,250×141 và Quay lại. Mở lại bắt đầu trang chính.
- Thoát dùng autosave hiện có; API lưu tay vẫn còn nhưng menu mới không có nút
  Save theo danh sách người dùng yêu cầu. Các slot tay cũ vẫn tải được.

**Một map = 5 đợt**, một đợt có1–3 quái. Chỉ hạ hết đợt mới cộng tiến độ.
Map1–5 thường, map6 khó; map7–11 thường, map12 khó, tiếp tục lặp. Map khó dùng
màu xanh sẵn có, quái damage×2, EXP×3; map thường×1. Không thêm boss/asset map
mới. Tốc độ sinh đợt theo tốc độ hero vẫn giữ công thức trước đó.

Save thêm `mapNumber`, `completedWavesInMap`, `mapProgressVersion`. Chết/retry
không tính thắng; load chơi lại đợt đang dở, giữ số đợt đã thắng. Save cũ chưa có
trường map bắt đầu map1/đợt1, giữ level/EXP/tên/tộc/căn; bỏ cờ khó tay cũ.

## Kiểm chứng và thứ tự thử

78 regression đạt với Unity test doubles, gồm tên/căn,25/30/55/60 đợt đổi map,
load danh tính/tiến độ, slot hỏng giữ trận, reset hủy, tint qua pool, click thật
vào trang menu/scale và sát thương phép riêng theo từng hệ của mục tiêu.
Nhánh Windows biên dịch riêng bằng stubs; không phải build Unity hoặc thử WinAPI.
Mô phỏng cân bằng chỉ là mẫu, không phải chứng minh game cân bằng ở mọi cấu hình.

1. Mở bản mới: cửa sổ800×450; menu chỉ4 mục. Thử Tải/Quay lại, Kích thước,
   từng800/500/250; mở lại menu, kéo cửa sổ, kiểm tra click không rơi xuống desktop.
2. Thử reset → Không: tên/chỉ số/save giữ nguyên. Thử Có trên bản sao save:
   hero mới cấp1, tên1–3 chữ và tộc/căn mới, map1, các save tay cũ bị xóa.
3. Load các save cũ và mới; xác nhận giữ tên/tộc/căn/level/EXP, map/đợt mới
   được giữ; save cũ bắt đầu map1. Slot trống/hỏng không đổi nhân vật đang chơi.
4. Thử log hiển thị trên ở ba scale; không bị menu che/ẩn. Thử quái tái dùng
   nhiều lần: màu mới không chồng màu cũ; tên/tộc/hệ quái xuất hiện trong log.
5. Thử cùng ATK với cặp Kim→Mộc và Mộc→Kim, cả melee/physical/magic; nhiều
   đạn phép đánh các hệ khác nhau phải cho hệ số khác nhau; map khó nhân2.
6. Kiểm map5→6→7: mỗi map đủ5 đợt mới chuyển, khó xanh/EXP×3, về thường
   trả màu và multiplier; chết ở đợt dở, retry/load không bỏ qua đợt.

Gói đầy đủ có Assets/.meta, Packages, ProjectSettings, Docs, Tests~; Unity bỏ
qua Tests~. Nên mở như project riêng hoặc checkout nhánh mới để tránh ghi đè
scene local đang chỉnh. Bản build cũ không đổi: cần build lại Windows.
Rollback mã về main trước PR mới hoặc bản sao local; trước khi thử reset hãy
sao lưu thư mục Saves vì thao tác Có đúng thiết kế sẽ xóa bản lưu.
