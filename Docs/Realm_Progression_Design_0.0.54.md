# 0.0.54 — Level, cảnh giới và Thiên Kiếp

**Ngày thiết kế:** 15/10/2026. **Trạng thái:** chọn **phương án B** (cảnh giới tính lại tiến trình tại Level đột phá thành công). Đây là tài liệu định hướng, **chưa triển khai gameplay, UI hoặc migration Save**.

## 1. Quyết định đã thống nhất

- **Level nhân vật** (EXP) **không bị khóa khi gặp bình cảnh**. Người chơi có thể tiếp tục farm, lên Level, nhận điểm thuộc tính và nâng sức mạnh.
- **Cảnh giới** là tiến trình độc lập với Level, chỉ thay đổi khi **Thiên Kiếp/đột phá thành công**. Không tự đổi cảnh giới theo phép chia Level như `RankDatabase.GetRankName(currentLevel)` hiện tại.
- Sau khi thành công, nhân vật bước vào cảnh giới mới với **cấp nội bộ thứ nhất** tính tại **Level hiện có tại thời điểm thành công**, bất kể đã farm vượt bình cảnh cũ bao nhiêu Level.
- Thời gian ở một cảnh giới không tự rút ngắn do Level tích lũy dư từ cảnh giới trước. Nếu chưa qua bình cảnh, cấp nội bộ của cảnh giới dừng tại mốc bình cảnh nhưng **Level toàn cục và EXP vẫn tăng**.
- Giữ bộ tên cảnh giới hiện có trong `TuTienEnums.cs`: Luyện Khí, Trúc Cơ, Kết Đan, Nguyên Anh, Hóa Thần, Luyện Hư, Hợp Thể, Đại Thừa, Độ Kiếp, Chân Tiên và nhánh sau đó Tiên Đế; mở rộng sau nếu cần. Không xóa danh sách này.
- **Khung tiến trình đã chọn cho thiết kế .54:** **30 cấp nội bộ / đại cảnh giới**: 1–9 Sơ kỳ, 10–19 Trung kỳ, 20–29 Hậu kỳ, cấp 30 là bình cảnh. Mốc 30 có thể tinh chỉnh khi cân bằng EXP, thời gian chơi và độ khó.

## 2. Quy tắc 30 cấp nội bộ — tránh lệch mốc 1 cấp

Dùng ví dụ cụ thể **30 cấp nội bộ**; đếm **Level lúc vào cảnh giới là cấp nội bộ 1** (không phải 0). Tạm đặt:

```text
realmEntryLevel = Level lúc đột phá thành công (hoặc Level khởi đầu cho cảnh giới đầu)
realmLocalLevel = min(30, max(1, currentLevel - realmEntryLevel + 1))
bottleneckReady = realmLocalLevel >= 30
```

Ở cảnh giới mở màn, `realmEntryLevel=1`: nội bộ 1..9 = Sơ kỳ, 10..19 = Trung kỳ, 20..29 = Hậu kỳ, nội bộ 30 = bình cảnh và mở Thiên Kiếp.

**ĐÃ CHỐT:** chuyển **Sơ kỳ → Trung kỳ** khi nội bộ đạt 10 và **Trung kỳ → Hậu kỳ** khi đạt 20 là **tự động**, không cần thử thách, màn chơi riêng hay nút Đột phá nhỏ. **Chỉ có một lần Thiên Kiếp khi vượt đại cảnh giới** sau khi đạt bình cảnh cấp nội bộ 30. Việc tự đổi *kỳ* không làm đổi *đại cảnh giới*.

**Ví dụ:** Luyện Khí đạt bình cảnh ở Level 30; tiếp tục farm tới 45 nhưng vẫn Luyện Khí; thắng Thiên Kiếp tại Level 45 → Trúc Cơ nội bộ 1, `realmEntryLevel=45`; Trúc Cơ nội bộ 30 tại **Level 74**. Nếu muốn mốc **Level 75**, phải chọn quy ước khác (30 lần lên Level thay vì 30 vị trí cấp); **không tự thay quy ước** khi viết mã.

**Không thêm hệ 9 tầng độc lập ở bản triển khai này.** Sơ/Trung/Hậu được tính trực tiếp từ cấp nội bộ như trên. Ý tưởng 9 tầng trước đó là một cách hiển thị có thể nghiên cứu về sau, **không phải dữ liệu bắt buộc, không cần chia tiếp 30 cấp thành 9 tầng**, tránh tạo một hệ cấp độ thứ ba không có chức năng. Khi ở cấp nội bộ 30 hoặc farm vượt quá, hiển thị **Hậu kỳ viên mãn / Bình cảnh** cho đến khi Thiên Kiếp thành công.

**Trạng thái bình cảnh** độc lập với việc đã mở Thiên Kiếp, đang thử hoặc đã thất bại. Nếu tiếp tục farm sau mốc 30, vẫn hiển thị bình cảnh hiện tại; tuyệt đối không chuyển cảnh giới do `currentLevel` vượt mốc.

## 3. Thiên Kiếp (ý tưởng thử nghiệm, chưa chốt cân bằng)

- Bản thử **20 giây chia hai giai đoạn 10 giây**: (1) nhân vật ngồi yên vận công, thiên lôi tấn công liên tục, phải sống sót; (2) nhân vật phản kích, phải gây đủ sát thương trong 10 giây để vượt thử thách.
- Giai đoạn phòng thủ kiểm tra HP, Giáp/Kháng, hồi phục và cơ chế phù hợp; giai đoạn phản công kiểm tra công vật lý/pháp thuật, chí mạng, chính xác, tốc độ tấn công/thi triển và linh lực. Cách di chuyển/né ở giai đoạn hai chưa chốt.
- Thất bại **không tự xóa EXP hoặc Level**; ý tưởng trọng thương/trừng phạt có thể thảo luận sau. Chưa chốt hình phạt, thưởng, độ khó hoặc mục tiêu phản kích, không tự code.
- Thiên Kiếp cần tạo động lực từ cả bốn hướng tu luyện. Nếu chỉ kiểm tra chống chịu sẽ bất lợi cho Công phạt.

## 4. Vượt cảnh giới khi dùng trang bị và võ học

- **Trang bị:** người chơi vẫn được mặc/dùng item vượt Level/cảnh giới, nhưng bị **giảm hiệu suất hoặc chịu debuff theo lý do cụ thể** (không đủ sức khống chế pháp bảo, phản phệ linh lực/HP, vũ khí quá nặng giảm tốc độ...), thay vì chặn tuyệt đối. **Loại/mức phạt chưa chốt.**
- **Võ học/công pháp:** một số khả năng có **điều kiện mở khóa cứng**; ví dụ võ học Thần thức chỉ thi triển từ Hóa Thần dù đã sở hữu bí tịch. Không áp cùng một luật với trang bị.
- **Thưởng cảnh giới:** tăng trưởng nền, mở ngưỡng/khả năng sử dụng võ học/pháp bảo và nội dung mới có thể là phần thưởng quan trọng hơn tăng điểm tu luyện. Loại và mức thưởng cụ thể **chưa chốt**.

## 5. Hệ quả kỹ thuật với mã hiện tại

- `Assets/Script/TuTienEnums.cs` có `RankDatabase.GetRankName(int level)` tính `(level-1)/10`, **không tương thích** một cảnh giới độc lập với Level. Khi triển khai phải lấy tên từ chỉ số cảnh giới thực thay vì phép chia Level, đồng thời xét đường gọi cũ để không hiển thị sai.
- `EntityDataSO` đang lưu `currentLevel/currentExp`, chưa có đủ dữ liệu cảnh giới độc lập. Dự kiến bổ sung **realmIndex** (ID cảnh giới ổn định), **realmEntryLevel** (mốc vào cảnh giới) và trạng thái đột phá nếu cần.
- Đừng chuyển đổi save cũ bằng cách suy đoán cảnh giới từ Level rồi tự động tặng phần thưởng. Phải xác định luật di trú save cũ **trước** khi bật tính năng, cũng như khóa tránh nhận thưởng đột phá hai lần.
- Công thức EXP hiện tăng nhanh theo Level (`CombatBalance.RequiredExp`), nên farm vượt bình cảnh lâu có thể làm 30 Level tiếp theo tốn quá nhiều thời gian. Cần cân bằng EXP/lượng EXP từ quái hoặc bổ sung tiến trình phù hợp trước khi nghiệm thu.
- `SynergyMath.GetLevelAdvantageMultiplier` hiện dùng lợi thế sát thương +5%/Level cao hơn (không bị chặn 5 cấp). **Không nhầm** với công thức chính xác ±2 điểm phần trăm/Level, tối đa 5 cấp đã chốt ở [Stat_System_Design_0.0.54.md](Stat_System_Design_0.0.54.md). Cân bằng lợi thế sát thương và chênh Level cần kiểm tra trước khi đưa Thiên Kiếp vào game.

## 6. Chưa quyết định

- 30 Level/cảnh giới là mốc cân bằng cuối hay cần thay; thuật toán EXP/tốc độ đạt cảnh giới.
- **Đã giải quyết:** không có bình cảnh nhỏ giữa Sơ/Trung/Hậu; chỉ cuối đại cảnh giới mới có Thiên Kiếp. Không bắt buộc hệ 9 tầng riêng. Nếu sau này muốn thêm 9 tầng thì đó là đề xuất tính năng mới, không phải phần còn thiếu của bộ khung này.
- Mức và dạng thưởng khi chuyển Sơ/Trung/Hậu kỳ, khi lên đại cảnh giới; hiệu ứng buff lâu dài.
- AI tự chống Thiên Kiếp trong game idle, hình phạt thất bại, quy tắc thời gian, độ khó.
- Cách di trú Save cũ và bổ sung dữ liệu Level/cảnh giới, trường hợp nhân vật Level rất cao ở cảnh giới thấp.

**Quy tắc triển khai:** chỉ sửa gameplay/Save sau khi thảo luận những điểm cần chốt; giai đoạn này lưu thiết kế trong tài liệu ở `feature/0.0.54`, không sửa `main`.
