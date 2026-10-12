# B2 — Nền tảng kỹ năng nhiều tầng, linh lực và khống chế (bản thử nghiệm)

**Nhánh dự kiến:** `feature/b2-skill-foundation`, tách từ `feature/b2-combat-target-routing` tại commit `c3352abab572345add4f4967442e3cbafd491ce1`.

**Tình trạng nghiệm thu:** mã nền tảng và bài kiểm thử nguồn được bổ sung, chưa có kết quả chạy Unity Editor, Windows EXE hay `mcs/mono` trên máy phát triển. **Không tự chốt B2 hoặc đưa vào game mặc định.**

## 1. Quy tắc thiết kế đã thống nhất

- Một võ học có nhiều **tầng**, mỗi tầng là biến thể kỹ năng với hình thức chọn mục tiêu, số đạn, khả năng chuyển mục tiêu và hiệu suất riêng.
- Mỗi tầng có **9 cấp thành thạo độc lập**: luyện tầng 1 không làm tầng 2 lên cấp. Đổi tầng giữ nguyên thành thạo của tầng cũ.
- Một sách riêng **chỉ mở đúng tầng tương ứng**, không tự mở các tầng thấp hơn. Chỉ sách **toàn bản** mở được toàn bộ các tầng.
- Người chơi **chủ động chọn tầng**; thiếu linh lực thì tiếp tục đánh thường, **không tự hạ tầng**. Cùng một võ học dùng chung bộ đếm hồi chiêu để không lách hồi chiêu bằng đổi tầng.
- Linh lực mỗi Hero độc lập, tự hồi. Các nguồn tăng tối đa/hồi linh lực của cấp độ, vũ khí, trang bị chưa được nối tự động vào bộ stat hiện hữu; hàm `ConfigureMana` hiện phục vụ thử nghiệm.
- **Vận chiêu cố định** theo định nghĩa tầng/kỹ năng; **hồi chiêu riêng**, có thể thay theo cấp thành thạo. Hồi chiêu bắt đầu lúc **xuất chiêu**. Thời gian bay của đạn là thông số thứ ba, không phải vận chiêu.
- Sát thương kỹ năng lấy chỉ số tấn công của Hero, áp **hệ số kỹ năng**, sau đó dùng hệ thống tương tác linh căn của người gây và người nhận. Chưa bổ sung các chỉ số trang bị/vũ khí mới vào `EntityDataSO`; khi có sẽ dùng đường tổng hợp chỉ số đúng quy tắc.
- Tuyệt kỹ đặc biệt chỉ được cấu hình **miễn sát thương và khống chế từ đầu vận chiêu tới hết chuỗi xuất chiêu**, không kéo dài theo đạn còn bay. Hiện chỉ cho phép gắn bảo hộ cho **tầng cao nhất** của võ học. Hủy thi triển, chết hay hồi sinh đều gỡ bảo hộ ngay.

## 2. Phản ứng trúng đòn và khống chế

Đòn đánh thường mặc định **không gây khựng thật**, chỉ gọi sự kiện `HitReactionRequested` để hoạt ảnh/VFX phản ứng; hệ thống hiển thị số sát thương và hiệu ứng va chạm cũ vẫn tồn tại. **Chưa có hoạt ảnh riêng** cho phản ứng hoặc Tam Bộ Kiếm; chỉ có móc nối `HitReactionRequested` cho nhà phát triển.

`HitControlEffect` là hồ sơ **tùy chọn** gồm cơ hội trúng, thời gian khựng và khoảng kháng khựng. `HitControlState` thuộc riêng từng Hero/quái đang sống, độc lập với HP và sát thương. Thông số thử nghiệm:

- Mỗi lần **khựng nhẹ thực sự** tối đa `0,25 giây`.
- Sau khựng có thời gian kháng khựng tối thiểu `0,6 giây` **kể từ khi kết thúc khựng**.
- Không cộng dồn khựng, không làm mới thời gian khựng khi bị trúng nhiều đòn nhanh.
- `StaggerResistance` để mở rộng cho quái tinh anh/boss, trang bị chống khống chế. Hiện **chưa gắn boss tier, passive hoặc item dữ liệu thật**.
- `HeroCombatState.BasicAttackControl` cho phép một vũ khí/võ học nội tại (khi hệ thống item/passive có mặt) cung cấp khả năng khựng lên **đòn đánh thường**, mặc định bằng không. Quái có `ActiveMonsterInfo.BasicAttackControl` dành cho đòn thường của quái.
- Kỹ năng tự có `SkillTierDefinition.ControlOnHit` **riêng**, không kế thừa hiệu ứng khựng của đòn thường. Mỗi lần trúng đòn yêu cầu kiểm tra xác suất và kháng của nạn nhân.
- **Miễn khống chế tuyệt kỹ** được kiểm tra trước khi nhận hiệu ứng. Hiện B2 chưa có hệ làm choáng/đóng băng/đẩy lùi tổng quát; lớp mới chỉ quản lý **khựng nhẹ**, `IsControlImmune` là điểm tích hợp sau này, **không phải bằng chứng đã miễn mọi hiệu ứng tương lai**.

## 3. Tệp mã và API

- `Assets/Script/SkillCombatCore.cs`: `SkillTierDefinition`, `MartialArtKnowledge`, `MartialArtTierProgress`, `HeroSkillRuntime`, `SkillCastContext`. `Equip(artId,tier)` yêu cầu đã học tầng đó. `AutoCast=false` mặc định; không can thiệp bản chơi một Hero hiện tại khi chưa cài kỹ năng.
- `Assets/Script/HitControlCore.cs`: `HitControlEffect`, `HitControlState` và quy tắc chống khóa liên tục.
- `HeroCombatState.cs`: sở hữu `Skills`, `HitControl`, `BasicAttackControl` riêng; `BeginLife` xóa vận chiêu/khống chế tạm thời; giữ trạng thái học runtime để chuẩn bị lưu sau này.
- `HeroController.cs` / `MonsterController.cs`: ngừng di chuyển khi đang khựng thật; phát `HitReactionRequested` độc lập; quái được tái sử dụng không kế thừa khống chế cũ.
- `CombatManager.cs`: `TryCastEquippedSkill(hero)`; phiên bản Hero/quái trong đạn; các projectile kiếm mẫu có thể tự chuyển mục tiêu nếu tầng cho phép. Sát thương và `ControlOnHit` được giải quyết riêng. Ngăn đánh thường trong lúc Hero đang vận/xuất chiêu.
- `TuTienEnums.cs`: `SynergyMath.EvaluateSkillDamage` nhận hệ số riêng của tầng và giữ tính toán linh căn hiện có.
- `Tests~/SkillFoundationRegression.cs`: **20 tình huống bổ sung**, chạy từ `Tests~/MotionRegression.cs`; `Tests~/run.sh` bổ sung 2 tệp runtime mới.

## 4. Những gì CHƯA làm

- **Chưa có** sách võ học thực tế trong inventory, phân phối loot/shop, UI học tầng/đổi tầng, lưu tiến độ lâu dài hoặc tự cộng chỉ số Mana theo cấp/trang bị.
- **Chưa có** hoạt ảnh nhân vật bay lên, kiếm triệu hồi từ hư không hay bản kỹ năng Thiên Kiếm thật. Đòn thử mới dùng projectile hiển thị đơn giản và các mốc giai đoạn, nhằm kiểm chứng tuyến dữ liệu.
- **Chưa có** kỹ năng theo đường thẳng, đa vùng, mọi dạng điều hướng, buff/debuff tổng quát hoặc hệ thống chống khống chế đầy đủ.
- **Chưa có** phân biệt boss/tank trong dữ liệu, hệ thống tổng hợp nhiều nguồn hiệu ứng khựng từ đồng thời item, nội tại; hiện mới có **điểm cắm** cho một hiệu ứng hiệu lực trên mỗi hit.
- **Chưa biên dịch/chạy** mới được trong Unity/Mono/Windows tại môi trường này. Kiểm tra tĩnh mã **không** đồng nghĩa `PASS` hồi quy.

## 5. Cách thử và tiêu chí nghiệm thu

1. GitHub Desktop → Fetch origin → chuyển sang `feature/b2-skill-foundation` → Pull. Chạy `bash Tests~/run.sh` nếu có Mono; kiểm tra Unity Console và Play Mode thật.
2. Chơi **một Hero không trang bị kỹ năng mới**: mọi hành vi .54g/B2 gồm camera, đòn cận/cung/phép, đạn hụt, wave, save cũ, retry phải còn như trước.
3. Trong mã thử, cấp cho Hero `SkillTierDefinition`, gọi `LearnSingleTier`, `Equip`, `ConfigureMana`, rồi `TryCastEquippedSkill`. Kiểm tra hai Hero có mana và nhịp độc lập; kỹ năng cấp thấp không tự đổi tầng.
4. Kiểm tra tuyệt kỹ có `ProtectedUltimate=true` khi `Tier==MaximumTier`: từ bắt đầu vận chiêu đến kết thúc loạt bắn hoàn toàn miễn sát thương và khựng; sau khi hết thời gian phải nhận đòn bình thường.
5. Kiểm tra nhiều hit liên tiếp không khóa Hero/quái vĩnh viễn, tỉ lệ stun chỉ hoạt động khi kỹ năng hoặc vũ khí/nội tại chủ động cung cấp. Kiểm tra pool quái, chết, hủy thi triển và retry.

**Không merge** nhánh thử nghiệm vào `feature/b2-combat-target-routing`, B1, `.54g` hoặc `main` khi chưa được người phát triển thử và nghiệm thu.
