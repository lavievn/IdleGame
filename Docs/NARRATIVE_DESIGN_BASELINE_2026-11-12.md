# Narrative Design Baseline — 12/11/2026

**Dự án:** Bách Luyện Tiên Đồ / IdleGame  
**Trạng thái:** Quyết định thiết kế được chốt + mục đề xuất phân tách rõ; chưa là lệnh triển khai.  
**Nguồn chuẩn:** đầu [README tổng](../README.md), [AGENTS](../AGENTS.md); khi có khác biệt, xác minh task hiện tại và mã trên nhánh được giao.

## A. Mục tiêu cảm xúc (ĐÃ CHỐT)
Người chơi bắt đầu với **mục tiêu nhỏ** rồi **được quyền thay đổi lý tưởng** trên con đường tu tiên. Một kết thúc chưa như ý phải để lại nuối tiếc có căn cứ và động lực thử quyết định/hướng phát triển khác. Một kết thúc đúng mong muốn phải tạo cảm giác đạt thành tựu, không được ban phát dễ dàng. Không mặc định kết thúc buồn là tệ hay sống bất tử là tốt.

Nên có một cốt truyện xương sống, lựa chọn rẽ nhánh rồi hội tụ tại các biến cố, giữ trạng thái nhân quả. Người chơi là một thành phần tác động lên thế giới, thế giới không tồn tại chỉ để chờ họ.

## B. Hành vi đã chốt
| Chủ đề | Quy tắc bắt buộc ở tầng thiết kế |
| --- | --- |
| Thể loại | Idle/auto-battle, không chuyển sang điều khiển side-scrolling |
| Tương tác | Chiến đấu/tích lũy tự động; lựa chọn lớn thuộc về người chơi |
| Xây dựng nhân vật | Không class cố định; build có tác dụng giải quyết thử thách |
| Nhánh truyện | Trục chính có nhánh và hội tụ; hậu quả NPC/thế lực/bản đồ tồn tại |
| Thời gian | **Phương án B**: môi trường và tu luyện hoạt động theo đồng hồ riêng; truyện chỉ chuyển mốc ở sự kiện |
| AFK | Không tự kích hoạt lựa chọn lớn, cái chết truyện, lỡ nội dung chính vì vắng mặt |
| Thọ nguyên protagonist | **Phương án B**: khủng hoảng/thọ tận có thể là kết cục nhưng chỉ ở mốc hợp lệ, có cảnh báo và cơ hội chuẩn bị |
| Chiến đấu chết | HP=0 / Retry không đồng nghĩa tử vong vĩnh viễn |
| Kết thúc | Phản ánh nhân vật + người xung quanh + thế giới; các giá trị có thể xung đột |
| Tiến trình | Gameplay chuẩn bị → hành động → hậu quả; không chỉ farm để đọc thoại |

## C. Chưa được chốt — KHÔNG TỰ CODE
- Tên ngôi làng Thanh Khê, NPC Lâm Thanh và cảnh cứu người/linh dược, chuỗi 5 hồi, tên gọi các hồi, ba ending ví dụ.
- Niên biểu 3/20/60/100 năm, con số tuổi thọ mỗi cảnh giới, điều kiện tử vong tự nhiên, tỷ lệ bế quan và phần thưởng.
- Cơ chế Hồi Tưởng, Luân Hồi/New Game+, kế thừa tài nguyên/tri thức, Di Sản/Sổ Nhân Quả; giai đoạn đầu chỉ cần thiết kế khả năng lưu branch history an toàn nếu task yêu cầu.
- Nguồn gốc Thiên Đạo/Thiên Kiếp, hệ giá trị chính đạo/ma đạo, NPC nào có thể chết, ngã rẽ kết cục chính thức.
- AI NPC tự chủ, mô phỏng từng ngày/năm, thế giới phát triển trong offline. Không đưa vào kế hoạch như tính năng đã duyệt.
- Cốt truyện **Hàn Vụ** đang có trên PR #13/#17 cần được xem là nội dung làm việc hiện hữu; không ghi đè bằng ví dụ Thanh Khê.

## D. Đặc tả ba đồng hồ (mục tiêu thiết kế, chưa khẳng định runtime)
1. **EnvironmentClock**: ngày/đêm, thời tiết, ánh sáng, âm thanh; không cấp tuổi NPC hay chọn ending. Chu kỳ 13 phút/ngày trong PR #16 là cấu hình môi trường thử nghiệm, không phải một ngày lịch sử.
2. **ProgressionClock**: frame active, Qi, EXP, combat/loot, pause/travel/retry; không mặc nhiên cấp offline progress. Production start ~60 phút là mô phỏng Qi theo profile riêng, khác preset DEV.
3. **StoryTimeline**: năm/đời nhân vật, biến cố và các mốc chuyển; chỉ thay đổi bằng sự kiện được phê duyệt, xác nhận và lưu. Không dùng timeScale/wall-clock để chuyển niên đại.

Bế quan tương lai là transaction qua StoryTimeline: kiểm điều kiện/tài nguyên → dự báo rủi ro → player confirm → giải quyết những biến cố thuộc khoảng thời gian ấy → lưu trạng thái atomically. **Không chạy vòng lặp từng tick theo từng năm**, không nhận lợi ích vô hạn từ việc nhập số năm. Đây là khuyến nghị kiến trúc, không có quyền bổ sung hệ thống bế quan nếu task chưa duyệt.

## E. Trạng thái NPC
Mỗi NPC chủ chốt nên có ID ổn định, điều kiện sống/chết, trạng thái nghề/nghiệp/lý tưởng, quan hệ, giai đoạn câu chuyện và các trigger thay đổi. Chỉ update ở các mốc hợp lệ, không tăng tuổi theo weather/gameplay clock. Cần có hành trình riêng ngay cả khi người chơi không can thiệp; không nhất thiết ai cũng biết ơn protagonist.

Hệ quả nên cụ thể (`npcAlive`, `sectStanding`, `villageState`, `promiseState`, `secretKnown`) thay vì một thanh good/evil. Mỗi key là minh họa schema logic; **không tự thêm field Save hoặc thiết lập enum từ ví dụ**.

## F. Nhánh kết thúc và chơi lại
Kết thúc có ba chiều: protagonist, quan hệ, thế giới. Lựa chọn chỉ có ý nghĩa khi điều kiện và hậu quả hợp lý, có manh mối nhận biết; không tạo “true ending” dễ dàng bằng cách chọn đáp án tốt. Một nhánh sau hội tụ vẫn giữ ký ức/hệ quả. Kiến thức của người chơi không mặc nhiên là ký ức tiền kiếp của nhân vật.

Muốn thử lại không nên phải AFK lại 150 giờ nguyên vẹn. Cần giải pháp tăng tốc hợp lý hoặc nhánh lưu riêng, nhưng **chưa chốt chọn Hồi Tưởng hay Luân Hồi**, chưa chốt điều kiện khai mở.

## G. Vertical slice cần kiểm chứng trước khi mở rộng
Phạm vi thử nghiệm kỹ thuật Hàn Vụ đã được chủ dự án duyệt trong task Local
Integration Baseline ngày12/11: một biến cố hiện có, nhận/từ chối thuốc, close
trung lập, có thể đổi ý, M0→M1 logic sau sắc thuốc, không tăng tuổi/năm hoặc thêm
NPC/thoại/reward/ending/Save field. Xem [đặc tả slice](story/HAN_VU_VERTICAL_SLICE_SPEC_2026-11-12.md).
Đây chưa phải giao triển khai Event System/migration hoặc nghiệm thu build tổng hợp.
Tiêu chí:
- hai quyết định dẫn đến hệ quả quan sát được;
- idle vẫn chạy khi quyết định truyện chờ;
- ngày/đêm không làm già NPC;
- Save/Load lặp lại không áp tác động hai lần;
- một người chơi mới có thể giải thích “tôi làm X nên Y xảy ra”;
- không cần build/đánh quái hàng chục giờ để test các nhánh.

## H. Quy trình chuẩn của Codex
- Đầu phiên đọc AGENTS + README đầu trang + file này + tài liệu task + mã hiện hành. Liệt kê **đã kiểm chứng / tồn tại nhưng chưa nghiệm thu / thiếu / ý tưởng / bị chặn**.
- Xem trạng thái PR #12 (animation), #13/#17 (Hàn Vụ), #16 (time/weather/sky), #18 (item/loot), #19/#20 (cultivation/EXP); không coi chúng đã được tích hợp vào một bản chung.
- Không tự ghép Hàn Vụ với Thanh Khê, không đổi các phương án B đã chốt, không đưa thọ nguyên vào real-time countdown, không tạo auto-choice.
- Task triển khai phải ghi file/scene/branch, yêu cầu nghiệm thu, phạm vi cấm sửa. Cần task riêng cho migration, cân bằng, merge và native Player validation.
- Report theo cấp test: code/domain/UnityStub/Unity Editor/Windows build/player; không nâng PASS domain thành PASS Player.
- Không thay đổi `main`, trạng thái PR hoặc save của người dùng khi không có quyền; sau sửa báo SHA, diff, regression, rủi ro và bằng chứng.

## I. Câu hỏi thiết kế còn mở
1. Xung đột trung tâm và quy luật Thiên Đạo, Thiên Kiếp trong lore?
2. Khởi đầu chính thức là Hàn Vụ, Thanh Khê, hay tên/khu vực khác?
3. Quy luật tuổi thọ từng đại cảnh giới và bế quan có các giới hạn nào?
4. NPC chủ chốt nào, điều kiện chuyển tuổi/giai đoạn, kết cục nào?
5. Chơi lại theo Hồi Tưởng, Luân Hồi hay kết hợp, và tiến trình nào kế thừa?
6. Story-state schema và ranh giới với SaveManager hiện hành?

**Mọi mục ở I phải chờ quyết định của chủ dự án; Codex được phép audit/đề xuất, không tự “chốt bằng code”.**
