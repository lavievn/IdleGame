# IdleGame — Rà soát kiến trúc và lộ trình theo giai đoạn

**Thời điểm rà soát:** 11/10/2026. **Mốc mã:** nhánh `feature/0.0.4.54g`, trên nền commit `7341e5269f51ce66bfdb7b4d962d674958a0f369`; tinh chỉnh Deadzone 38% được phát triển sau mốc này.
**Trạng thái:** ĐỀ XUẤT THẢO LUẬN, không phải quyết định sản phẩm đã chốt hay nghiệm thu Unity.
**Cơ sở:** kiểm tra cây Git 233 tệp, 39 tệp C# (21 nguồn runtime/dữ liệu, 18 trong Tests~), README, Project_info_dev và các mô-đun quản lý màn hình, Hero, quái, chiến đấu, dữ liệu, lưu, camera, nền, UI, Windows overlay. Không thể thay thế việc chạy bản Unity/Windows ở máy phát triển.

## 1. Kết luận có căn cứ trong mã

| Phạm vi | Hiện trạng được xác nhận | Rủi ro/chỗ còn thiếu |
|---|---|---|
| Cửa sổ overlay | TransparentWindow thực hiện click-through, kéo, thay đổi kích thước Windows; UIManager điều phối giao diện | Cần thử bản Windows thật ở nhiều DPI/màn hình và khi mất focus; không được suy ra hoàn chỉnh từ test stub |
| Không gian chiến đấu | EnvironmentManager giữ Ground-local và Canvas ổn định; camera Deadzone + SmoothDamp; Ground/quái/đạn chung một pan | Kiểm chứng Hero chạy 700, đổi 600/800/1150, bắn khi dừng, quái tầm xa và projectile lúc resize |
| AI Hero/quái | Hero có ActiveHeroes; quái tìm Hero hợp lệ gần nhất trong các ứng viên tiến về phía trước/đang trong tầm, đổi mục tiêu khi không còn đủ điều kiện | Chưa có đội hình, không bảo đảm tuyệt đối khoảng cách cận/xa ≤ 300–400. Quái không luôn chọn lại mục tiêu gần nhất ở mọi khung |
| Chiến đấu | Ba thế Cận/Cung/Phép, sát thương và khắc hệ, timer đòn, đạn/hitbox, quái 1–3 con/đợt, pooling | CombatManager có duy nhất heroController, runtimeHeroData, currentHeroHP và heroAttackTimer; chưa thể hỗ trợ hai Hero độc lập chỉ bằng danh sách ActiveHeroes |
| Tiến trình | 5 wave/map, 5 map thường rồi map khó; EXP, cấp, chết/retry, ghi log | Chưa có item drop/boss, chưa đo cân bằng theo vòng chơi có đồ và kỹ năng |
| Dữ liệu/lưu | EntityDataSO có version cân bằng/tiến trình; SaveManager JSON + ghi file tạm/đổi tên; 3 slot (auto, 2 tay) | Schema hiện là một nhân vật, chưa có danh sách Hero, kho, item ID, trang bị hay kỹ năng; cần migration có kiểm thử trước khi mở |
| UI/công cụ | UIManager và GameManager tương đối lớn, có bảng DevBalance, menu, lựa chọn thế đánh | Nhiều trách nhiệm chung một lớp; tránh tiếp tục nhồi item/skill/đội hình trực tiếp vào UIManager, CombatManager, GameManager |
| Thử nghiệm | Tests~ có regression theo Mono/stub và tệp test-results lịch sử | Chưa có bằng chứng chạy lại bộ test mới trong Unity, chưa có CI trong cây Git; test-results cũ bao gồm camera thế hệ trước, không dùng để xác nhận Deadzone |

## 2. Nguyên tắc thực hiện

- Chia theo **lát cắt dọc nhỏ**, mỗi phiên bản có tính năng kiểm chứng được từ UI đến dữ liệu và save; không dựng trước cả hệ thống chưa dùng tới.
- **Chỉ bắt buộc hoàn thiện điểm giao tiếp (contract) giữa các hệ** trước khi chuyển bước; UI/VFX/art/balance có thể để bản thử để phát triển song song.
- Thay đổi cấu trúc mã nhưng không thay hành vi phải có kiểm thử chống thay đổi kết quả cũ.
- Không tự chốt số slot, cấp/độ hiếm item, công thức cộng dồn/crit/defense, số nhân vật đội hình tối đa hay hệ tiền tệ nếu chưa có đặc tả.
- Khi phát sinh lỗi P0 (mất tiến trình, sai target, wave kẹt, Crash, camera mất đối tượng), **tạm dừng tính năng mới để sửa nguyên nhân**. Lỗi VFX/UI nhỏ có thể ghi backlog và xử lý ở vòng polish.
- Phân biệt: có mã, mô phỏng toán học, chạy Mono/stub, Unity Editor Play Mode và Windows build thật.

## 3. Các cổng chuyển giai đoạn (đề xuất)

### Mốc A — Chốt nền 0.0.4.54g, nghiệm thu thay vì thêm cơ chế mới

**Phạm vi:** Camera Deadzone mặc định 38%, SmoothDamp giữ 0.2s; kiểm tra scene Inspector tương ứng. Chạy Hero đi/dừng/bắn rồi lại đi, riêng tốc độ 700 và cận→quái xa; quái sát Ground và không có đạn mất đích khi resize. Chạy đủ 25 wave (một cụm 5 map), chết/retry và load/save, thử UI/overlay Windows 600/800/1150, DPI và kéo/thu phóng. Sao lưu save trước thử migration.

**Đủ chuyển bước khi:** không còn lỗi Console nghiêm trọng, không có camera tự trôi khi Hero dừng, wave không kẹt, quái/đạn đúng Ground sau resize, save/load an toàn, người dùng chấp nhận cảm giác camera trong Unity và xác nhận có thể merge `.54g` vào `main`. Chưa cần polish ảnh, SFX, balance ba thế hoặc FPS tối ưu cuối cùng.

### Mốc B — Lõi combat có thể mở rộng, giữ nguyên gameplay 1 Hero

**Vấn đề:** Một Hero đang có HP, dữ liệu, đòn đánh, cooldown và vận động ràng buộc ở CombatManager/GameManager; MonsterController lại dùng danh sách Hero. Nếu không tách ở đây, mọi skill/gear/party sẽ phải vá nhiều lần.

**Phạm vi tối thiểu:** tách dữ liệu trạng thái chiến đấu theo từng nhân vật (định danh, HP, dữ liệu tính stat, đòn đang vận, mục tiêu); tách bước **chọn mục tiêu**, **xác nhận đủ điều kiện**, **tính sát thương** và **áp hiệu ứng** thành đường gọi có kiểm thử. Giữ trọn kết quả chiến đấu và UI một Hero hiện hành. Không xây sẵn hệ thống ECS mới, không đổi Unity framework.

**Đủ chuyển bước khi:** một Hero vẫn đánh, bắn, phép, chết, đổi thế, qua map và save như cũ; cùng đường xử lý có thể tạo **hai trạng thái nhân vật độc lập trong kiểm thử** mà không cần xây UI/đội hình thực. Không cần làm nhiều Hero ngay.

### Mốc C — Một lát cắt item/trang bị thật (chưa làm hệ đồ đầy đủ)

**Phạm vi tối thiểu:** schema item có ID ổn định, hiệu ứng chỉ số dạng cộng trực tiếp trước (ví dụ HP/ATK), một vị trí trang bị thử nghiệm và khung kho đơn giản; tách stat gốc + cấp/điểm + đồ, không viết ngược lên baseHealth/baseDamage. Một hoặc hai item mẫu qua equip/unequip/Save/Load; schema save có phiên bản/migration.

**Đủ chuyển bước khi:** tháo/lắp cập nhật chiến đấu và UI tức thì, HP hiện tại xử lý có quy tắc, reload giữ item và chỉ số, save cũ vẫn vào được. Chưa cần random rơi đồ, 5–10 loại slot, độ hiếm, set bonus, vô số stat, cửa hàng, sản xuất đồ hoặc UI bóng bẩy.

### Mốc D — Kỹ năng tự động ưu tiên dùng thật

**Phạm vi tối thiểu:** hai kỹ năng có phạm vi khác nhau (một gần, một xa) cùng thứ tự ưu tiên và cooldown; kiểm tra mục tiêu/tầm/điều kiện; tấn công cơ bản là dự phòng. Mỗi kỹ năng mô tả bằng dữ liệu để sau này đổi effect/animation không sửa logic chính.

**Đủ chuyển bước khi:** thứ tự dùng ổn định, không spam chiêu, đổi mục tiêu đúng, kỹ năng không bắn vào quái đã despawn/pool tái dùng, save/load giữ trạng thái học/slot kỹ năng. Chưa cần kỹ năng nhiều tầng, điểm tu luyện, buff/debuff/DOT/CC, hay năng lượng phức tạp.

### Mốc E — Đội hình hai Hero + camera nhóm

**Phạm vi:** spawn 2 Hero thực (ví dụ cận chiến/cung), mỗi người có HP, attack timer, target và skill/gear riêng; quái gây sát thương vào **Hero thực sự chọn** chứ không phải một heroController toàn cục. Dữ liệu Save gồm danh sách các nhân vật, xử lý Hero chết một mình và cả đội bị diệt. Camera lấy `(minHeroX + maxHeroX) / 2` cho hai đầu đội hình trong vùng an toàn khi đủ chứa cả hai.

**Đủ chuyển bước khi:** Hero đánh gần tiến trước/đánh xa theo sau trong tình huống mẫu, không đánh nhầm Hero, không dùng chung HP/cooldown, không làm camera/đạn lệch Ground, save/load khôi phục đúng từng Hero. **Điều kiện khoảng cách 300–400 chỉ là giả thiết tình huống**, không phải giới hạn code. Nếu đội hình rộng hơn viewport phải có quy tắc tập hợp hoặc ưu tiên người giao tranh, không cố camera bao hết với zoom tự động khi chưa chốt UX.

### Mốc F — Nguồn nội dung, hình ảnh và âm thanh theo cấu hình

**Phạm vi:** event SFX cho hit/skill/UI/wave, mẫu dữ liệu để gắn Sprite/Animator/VFX/Audio cho kỹ năng và thực thể; sau đó mới tool Unity Editor hỗ trợ import, kiểm tra thiếu asset, preview và lưu các tham chiếu. Tối ưu rendering, UI/cửa sổ theo bộ asset thực.

**Đủ chuyển bước khi:** thêm một quái, một kỹ năng hoặc một vũ khí cơ bản bằng cấu hình mà không phải sửa mã điều phối chiến đấu; swap asset không làm hỏng save/ID và trải nghiệm Windows overlay chạy ổn trong phiên kéo dài. Chưa cần hoàn thiện thư viện hình ảnh.

### Mốc G — Nội dung và chiều sâu hệ thống

Boss/elite, luật khắc sâu, crit/phòng thủ/buff/debuff, hiệu ứng theo hệ, thừa kế tộc/linh căn, luân hồi, ngoại tuyến và Lò Bát Quái. Chỉ triển khai một vòng đầy đủ mỗi lần sau khi vòng chiến đấu + dữ liệu + kỹ năng đã đủ ổn để đo cân bằng thật.

## 4. Kỹ thuật cần theo dõi xuyên suốt

- **Độ đúng dữ liệu:** version save, backup, migrator, atomic write và ID thực thể xuyên qua object pool; chú ý cập nhật tất cả nơi còn truy cập heroController đơn lẻ.
- **Độ ổn định theo thời gian:** đo phân bổ bộ nhớ/coroutine/projectile/UI; kiểm tra ít nhất một phiên chạy kéo dài ở Windows, ghi lỗi tăng dần theo thời gian thay vì chỉ xem FPS tức thời. Chưa có benchmark thực để kết luận hiện tại nhanh/chậm.
- **Khả năng kiểm thử:** bảo tồn Tests~ dùng stub/Mono, bổ sung đường kiểm thử Unity Play Mode/Windows smoke và CI khi điều kiện cho phép. Không khẳng định `Tests~/test-results.txt` cũ là kết quả dành cho camera mới.
- **Vị trí chỉnh camera:** giữ Deadzone đơn Hero ổn; khi đến Mốc E chỉ đổi bộ chọn **mục tiêu camera** sang tâm nhóm, không viết lại toàn bộ phép dịch thế giới và cơ chế đạn.
- **Giảm ghép nối:** các lớp lớn UIManager/GameManager/CombatManager chỉ nên điều phối những module chuyên trách; không ưu tiên tái cấu trúc mỹ thuật toàn bộ trước khi có use case.

## 5. Quyết định cần người dùng duyệt trước khi thực hiện mốc tiếp theo

1. Khi đã nghiệm thu camera .54g và gộp main, có đồng ý **Mốc B: tách lõi chiến đấu tối thiểu** (không đổi gameplay) trước khi xây item/skill hay muốn làm một demo item trước?
2. Với đội hình sau này, tạm chấp nhận camera lấy trung điểm `(minX+maxX)/2` khi toàn đội đủ nằm trong khung. Quy tắc đội hình tách quá rộng để chốt ngay trước Mốc E.
3. Số slot, loại item, bảng hiệu ứng vật phẩm, kiểu kỹ năng và hình ảnh/sound chưa cần chốt ở Mốc A/B; chỉ chốt contract khi triển khai lát cắt tương ứng.
