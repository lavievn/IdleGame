# B1 — Tách trạng thái chiến đấu của Hero (chưa thêm đồng đội)

**Nhánh phát triển:** `feature/b1-hero-combat-state`. **Nền cố định:** checkpoint `.54g` tại `02801dd2b1fe4ca46ce2402ffdbe93de590a2b58` (commit tài liệu; gameplay cuối tại `c86201b364b21d6a195e3ebd51668067427d04eb`).

**Mục tiêu B1:** chuyển nguồn lưu trạng thái HP, dữ liệu runtime, thời gian/kiểu vận sức và tham chiếu Hero sang một đối tượng **`HeroCombatState` riêng cho từng Hero**. Dự án vẫn chỉ điều phối **một Hero** trong `CombatManager`; khả năng định tuyến đòn đánh nhiều Hero sẽ làm ở **B2**, không nằm trong bản này.

## Thay đổi mã

- `Assets/Script/HeroCombatState.cs`: thêm kiểu `HeroCombatState`, mỗi instance giữ `Controller`, `Data`, `CurrentHP`, `MaxHP`, `AttackTimer`, `WindupMode`. `SelectedMode` phản ánh `HeroController.attackMode` của đúng actor; `BeginLife(data,maxHP)` chuẩn bị lần triển khai/tải dữ liệu tiếp theo, `ResetAttack()` xóa nhịp đánh. Không có singleton/static HP, không lưu trạng thái giữa hai nhân vật và không ghi vào file save.
- `Assets/Script/CombatManager.cs`: bỏ các trường dữ liệu HP/timer/hero đơn lẻ, thay bằng `primaryHeroState` là nơi lưu duy nhất. Để giữ nguyên tính toán .54g, các tên cũ `heroController`, `runtimeHeroData`, `currentHeroHP`, `maxHeroHP`, `heroAttackTimer`, `heroWindupMode` chỉ là **thuộc tính chuyển tiếp** đọc/ghi vào `primaryHeroState`, không phải bản sao giá trị. `SetupHeroInfo` gọi `BeginLife`; bộ đếm dùng `ref primaryHeroState.AttackTimer` vì C# không cho phép `ref` vào thuộc tính.
- `CurrentHeroCombatState` được công khai chỉ để đọc/kiểm thử trong B1. Đây là **đối tượng trạng thái runtime có thể thay đổi**, không phải API save hay lời hứa hỗ trợ hai Hero đồng thời.
- Các phép tính sát thương, thời gian vận sức, hồi máu khi hạ quái, XP, chết/Retry, các nhánh đạn/tầm đánh, logic chọn mục tiêu, camera, UI, tốc chạy, bản đồ và bản lưu **không được cố ý thay đổi**.

## Kiểm thử và tương thích

- Bộ `Tests~/MotionRegression.cs` trước đây dùng reflection chạm trực tiếp các trường scalar cũ. Bộ helper `Get/Set` nay chuyển các tên đó sang đúng thuộc tính của `CurrentHeroCombatState` khi kiểm thử `CombatManager`. **Không giữ các biến HP/timer sao chép song song** chỉ để test cũ tiếp tục hoạt động.
- Thêm `Tests~/HeroCombatStateRegression.cs`: 6 trường hợp kiểm tra hai instance độc lập, triển khai lại chỉ một instance, hai thế đánh độc lập, tính nhất quán getter/HP/timer cũ, thay thế đánh giữ tỷ lệ HP, chết/Retry giữ instance và xóa vận sức.
- `Tests~/run.sh` bao gồm tệp trạng thái mới khi biên dịch Mono/stub. Lệnh ở gốc dự án: `bash Tests~/run.sh`. Trên máy Windows có thể chạy qua Git Bash/WSL có `mcs` và `mono`; hoặc dùng Unity Editor (không đồng nhất với Mono test doubles).
- Trong môi trường cập nhật GitHub từ xa hiện tại **chưa có biên dịch C# hoặc chạy Unity Play Mode/Windows**. Kiểm tra cấu trúc và hợp đồng mã chỉ phát hiện một số lỗi; **không chứng minh không hồi quy**.

## Điều kiện nghiệm thu B1

1. Mở Unity Editor bằng nhánh mới: **không có lỗi biên dịch mới**, `MainGame.unity` giữ nguyên, camera `.54g` vẫn như checkpoint.
2. Triển khai một Hero: đánh cận/cung/phép, đổi thế giữa chiến đấu, vận sức phép/cancel, kill/heal/EXP, ăn sát thương, chết → Retry đều cho kết quả như trước.
3. Không hỏng lưu/tải bản lưu của `.54g` và các bản lưu cũ đang dùng; B1 không thêm field nào vào EntityDataSO hoặc đổi format SaveManager.
4. Bộ hồi quy dùng stub chạy không có test mới thất bại. Hai `HeroCombatState` trong test thay đổi HP/timer/mode độc lập mà không có tác động qua lại.
5. Nếu không đạt một mục, **không gộp nhánh**; khắc phục trên nhánh B1, không sửa checkpoint `.54g`.

## Tiếp theo — B2 (chưa thực hiện)

Khi B1 đạt, chuyển từ `primaryHeroState` sang ánh xạ `HeroController → HeroCombatState`, ràng buộc `Projectile` với nguồn/đích có định danh sinh mệnh, quái tấn công đúng Hero đang chọn và điều phối chết từng Hero; chỉ lúc đó mới thực sự thử 2 Hero trong trận và lưu đội hình. Không thêm hệ trang bị/kỹ năng trong B1.
