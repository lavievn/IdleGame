# IdleGame — Rà soát kiến trúc và lộ trình theo giai đoạn

**Thời điểm rà soát:** 11/10/2026. **Mốc mã:** nhánh `feature/0.0.4.54g`, trên nền commit `7341e5269f51ce66bfdb7b4d962d674958a0f369`; tinh chỉnh Deadzone 38% được phát triển sau mốc này.
**Trạng thái:** `.54g` đã được người phát triển **chấp nhận sử dụng và tuyên bố kết thúc vòng phát triển tính năng** (11/10/2026). Phần camera *chưa đạt hoàn toàn cảm giác mong muốn*, nhưng kiểm thử thực tế do người phát triển báo đạt với Hero đánh gần/xa, tốc độ nhanh/chậm. Các mục chưa được xác nhận riêng (save cũ, DPI, Play Mode tự động, Windows dài giờ...) vẫn là kiểm thử còn mở. **Lộ trình sau `.54g` là đề xuất kỹ thuật, không phải đã được duyệt triển khai.** `main` không tự được merge.
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

**Cập nhật sau nghiệm thu B1 (11/10/2026):** người phát triển đã xác nhận B1 hoàn tất. Đây là checkpoint được chấp nhận; chưa có log xác nhận chi tiết từng trường hợp tự động. Nhánh đang thực hiện: `feature/b1-hero-combat-state`, bắt đầu từ checkpoint `.54g`. Chỉ tách trạng thái HP/timer/data của Hero ra khỏi `CombatManager`, **chưa thay gameplay một Hero**, chưa thực hiện B2, trang bị, kỹ năng hay đội hình. Xem `Docs/B1_HeroCombatState.md` để biết cấu trúc mã, test và điều kiện nghiệm thu. Các bước P1/P2/P3 chưa được duyệt triển khai.

## 3. Bảng ưu tiên sau khi chốt .54g — ngày 11/10/2026

| Thứ tự | Mức | Hạng mục | Chỉ làm đến mức nào trong lượt đầu | Phụ thuộc |
|---|---|---|---|---|
| **1** | **P0** | **Lõi chiến đấu theo từng thực thể** | Trạng thái HP/timer/đòn theo Hero; giữ nguyên hành vi 1 Hero và kiểm thử 2 trạng thái độc lập; không làm UI tổ đội ngay | Checkpoint gameplay .54g |
| **2** | **P1 (xuyên suốt)** | **Schema dữ liệu/lưu phiên bản hóa** | Định danh bền vững, đường nâng cấp bản lưu cũ, 2 ca save mới/cũ; chỉ thêm trường khi tính năng cần | Lõi combat và từng lát cắt item/skill |
| **3** | **P1** | **Trang bị + kho tối thiểu** | 1 slot + 1–2 item HP/ATK cộng trực tiếp, trang bị/tháo, save/load | Trạng thái combat + schema |
| **4** | **P1** | **Kỹ năng tự động** | 2 chiêu mẫu tầm gần/xa, thứ tự ưu tiên, hồi chiêu, đòn thường dự phòng | Combat + dữ liệu stat |
| **5** | **P2** | **2 Hero và đội hình** | HP/đòn/đích riêng; thử camera theo đội và xử lý cả đội chết | Combat + lưu từng Hero + skill tối thiểu |
| **6** | **P2** | **Kết nối nội dung/VFX/SFX và công cụ nhập asset** | 1 skill, 1 quái mẫu có thể thay asset bằng dữ liệu; không xây cả editor trước schema | Hệ kỹ năng và cấu hình ổn |
| **7** | **P3** | **Mở rộng cơ chế** | Thêm từng lát cắt: boss/elite, buff/debuff, drop/độ hiếm, luân hồi, ngoại tuyến | Có gameplay và schema đủ để thử |
| **8** | **P3** | **Polish UI, tối ưu camera, hiệu năng sâu** | Backlog cải thiện theo dữ liệu kiểm thử; không mở lại camera .54g nếu không có lỗi cản trở | Tính năng tích hợp thực |

**P0** = khóa kỹ thuật cần vượt trước khi mở gameplay khác; **P1** = trực tiếp mở vòng chơi hoàn chỉnh; **P2** = mở rộng quy mô và nội dung; **P3** = nâng chiều sâu/chất lượng. Đây là xếp hạng *phụ thuộc kỹ thuật*, không phải độ hấp dẫn người chơi.

**Mốc B1 — đã hoàn tất theo người phát triển; nội dung lịch sử:** tách **trạng thái chiến đấu của Hero** (máu hiện tại/tối đa, dữ liệu chỉ số, nhịp đánh, thế đánh) khỏi các biến đơn lẻ trong `CombatManager`, để bước tiếp theo có thể quản lý mỗi Hero một trạng thái. Làm từng phần và thêm kiểm thử trước khi thay logic gây sát thương; **không thêm Hero thứ hai vào Scene, không đổi UI/damage/balance/camera/save đang chạy ổn** trong B1.

**Điều kiện dừng B1:** code gameplay một Hero vẫn có cùng kết quả cận/cung/phép, hồi chiêu, chết/retry và wave; có thể tạo hai trạng thái chiến đấu **độc lập trong kiểm thử** mà không trùng HP/timer; các đường truy cập cũ vẫn tương thích. Sau đó mới thực hiện B2: chọn mục tiêu và áp sát thương đúng từng thực thể. Chưa có yêu cầu xây đầy đủ hệ thống đa nhân vật.

**Công việc hậu kiểm không chặn bắt đầu B1:** tiếp tục giữ danh sách cần thử ở Windows về DPI, resize, save/load, quái/đạn và phiên chạy dài. Nếu phát hiện lỗi P0 hoặc mất tiến trình, tạm dừng B1 để sửa.

## 4. Các cổng chuyển giai đoạn (đề xuất)

### Mốc A — 0.0.4.54g đã chốt theo nghiệm thu có giới hạn của người phát triển

**Phạm vi:** Camera Deadzone mặc định 38%, SmoothDamp lúc chạy giữ 0.2s; thử hồi mềm khi Hero dừng (mốc 40% từ trái, SmoothDamp 0.6s, chỉ sau khi đã chạy tới bên trái mốc). **Đang thử thêm `cameraUseCombatCenter`:** tâm trung bình Hero đang hoạt động + quái sống trong vùng màn hình; so sánh bật/tắt cờ trong Inspector, nhất là khi quái mới xuất hiện hoặc chết; kiểm tra scene Inspector tương ứng. Chạy Hero đi/dừng/bắn rồi lại đi, riêng tốc độ 700 và cận→quái xa; quái sát Ground và không có đạn mất đích khi resize. Chạy đủ 25 wave (một cụm 5 map), chết/retry và load/save, thử UI/overlay Windows 600/800/1150, DPI và kéo/thu phóng. Sao lưu save trước thử migration.

**Kết quả và giới hạn:** người phát triển đã xác nhận `.54g` dùng ổn với Hero cận/xa, tốc độ nhanh/chậm và muốn kết thúc phiên bản; không yêu cầu hoàn thiện camera trước khi làm tiếp. Các tình huống Console, DPI/Windows build, save/load cũ, wave đủ cụm, phép thử tự động và chạy lâu chưa được xác nhận trong lượt nghiệm thu này — giữ trong backlog QA. **Việc chốt phiên bản không phải yêu cầu merge `main` hoặc tạo release/tag**. Không cần polish ảnh/SFX/balance hoặc FPS tối ưu cuối cùng trước mốc B.

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

## 5. Kỹ thuật cần theo dõi xuyên suốt

- **Độ đúng dữ liệu:** version save, backup, migrator, atomic write và ID thực thể xuyên qua object pool; chú ý cập nhật tất cả nơi còn truy cập heroController đơn lẻ.
- **Độ ổn định theo thời gian:** đo phân bổ bộ nhớ/coroutine/projectile/UI; kiểm tra ít nhất một phiên chạy kéo dài ở Windows, ghi lỗi tăng dần theo thời gian thay vì chỉ xem FPS tức thời. Chưa có benchmark thực để kết luận hiện tại nhanh/chậm.
- **Khả năng kiểm thử:** bảo tồn Tests~ dùng stub/Mono, bổ sung đường kiểm thử Unity Play Mode/Windows smoke và CI khi điều kiện cho phép. Không khẳng định `Tests~/test-results.txt` cũ là kết quả dành cho camera mới.
- **Vị trí chỉnh camera:** bộ chọn camera đã có chế độ **tâm trung bình Hero + quái trong viewport** thử nghiệm ở Mốc A, với công tắc quay về theo Hero. Đây không đồng nghĩa đã triển khai camera tổ đội nhiều Hero; khi đến Mốc E sẽ cần đánh giá lại trọng số Hero/quái, giới hạn khoảng cách đội hình và bảo vệ các Hero. Không viết lại toàn bộ phép dịch thế giới và cơ chế đạn.
- **Giảm ghép nối:** các lớp lớn UIManager/GameManager/CombatManager chỉ nên điều phối những module chuyên trách; không ưu tiên tái cấu trúc mỹ thuật toàn bộ trước khi có use case.

## 6. Quyết định cần người dùng duyệt trước khi thực hiện mốc tiếp theo

1. `.54g` đã chốt; **B1 đã được người phát triển xác nhận hoàn tất** trên nhánh riêng. Đề xuất bước tiếp là B2 — xử lý nguồn/đích và sát thương theo từng Hero, cần phê duyệt trước khi viết mã. Chưa merge `main`; mọi hạng mục QA chưa có log vẫn được giữ lại.
2. Với đội hình sau này, tạm chấp nhận camera lấy trung điểm `(minX+maxX)/2` khi toàn đội đủ nằm trong khung. Quy tắc đội hình tách quá rộng để chốt ngay trước Mốc E.
3. Số slot, loại item, bảng hiệu ứng vật phẩm, kiểu kỹ năng và hình ảnh/sound chưa cần chốt ở Mốc A/B; chỉ chốt contract khi triển khai lát cắt tương ứng.

### Checkpoint B1 đã chốt

Người phát triển xác nhận B1 đã xong sau commit mã `2308f25`. Chỉ cập nhật tài liệu chốt mốc, không thay code gameplay B1. B2 là hướng kế tiếp, không tự coi là được phê duyệt. Xem `Docs/B1_HeroCombatState.md`.
