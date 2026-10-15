# IdleGame — Rà soát kiến trúc và lộ trình theo giai đoạn

### 15/10/2026 — 0.0.54: UI asset đang triển khai, chuyển sang Equipment

- **Inventory / Information / UI liên quan:** asset thiết kế chưa hoàn thiện, tiếp tục chuẩn bị. Giao diện hiện tại vẫn hoạt động nhưng chưa chốt mỹ thuật; tạm hoãn việc tinh chỉnh và mở rộng công cụ layout Play Mode cho các UI này. Phương pháp chỉnh trong Play Mode đã được nghiệm thu tại Opening 0.0.53, sẽ tái dùng khi đủ asset.
- **Đầu việc đang chuyển sang:** rà soát và hoàn thiện hệ thống chỉ số trang bị theo loại sát thương vật lý/pháp thuật, kiểm tra phần cộng chỉ số và preview mặc/tháo, đảm bảo tương thích Save cũ. Sau đó mới thảo luận bộ sinh item, nguồn drop và rèn mạo hiểm; chưa tự bổ sung tỷ lệ ngẫu nhiên hoặc rủi ro mất item.
- **Nhánh:** `main` đã chốt ở 0.0.53; tiếp tục chỉ trên `feature/0.0.54`. Xem [Development_0.0.54.md](Development_0.0.54.md).


### Bổ sung 0.0.53 — Opening tiên cảnh (prototype, chưa nghiệm thu)

- **Đã commit trên `feature/0.0.53`:** scene opening được dựng bằng mã trên Canvas hiện có với 9 ô asset tham chiếu trong `Assets/Resources/OpeningArtTheme.asset`, dùng khối màu khi chưa có hình; bốn nút Tiếp tục/Chơi mới/Cài đặt/Thoát, thiết lập âm lượng trong opening, lưu nguyên luồng xác nhận Save.
- **Hiệu ứng đã lập trình:** mây trôi, parallax núi, splash hai bên hiện sau 27s, fade + tách lớp + zoom giả lập 2,7s khi vào gameplay. Gameplay thật chuẩn bị phía dưới và pause tới khi kết thúc chuyển cảnh; không scale mặt phẳng logic combat.
- **Chưa làm:** tên game, artwork cuối sạch watermark, khớp cue theo nhạc thay vì thời gian, shader mây/particle/motion polish và xác minh click-through/hiệu năng Windows.
- **Lần kiểm thử tiếp theo:** Unity Play Mode không lỗi; menu có đủ 4 nút; splash sau 27s; Continue/NewGame giữ đúng save; chuyển cảnh không phá camera, wave, âm thanh; resize các kích thước 600/800/1150. Sau nghiệm thu mới cân nhắc merge `main`.
- **Hướng dẫn thay Sprite và bài kiểm thử:** [Opening_0.0.53.md](Opening_0.0.53.md).



## Cập nhật hiện hành — 14/10/2026 (0.0.52-refinement → chuẩn bị BGM)

> **Nguồn trạng thái:** mã của nhánh `feature/0.0.52-refinement` và phản hồi Play Mode của người phát triển. Đây là checkpoint công việc, **mã BGM đã được viết nhưng asset nhạc chưa thể tải lên GitHub bằng kết nối hiện tại**. Các mục lịch sử .54g/B1/B2 ở phía dưới vẫn được giữ để tra cứu, nhưng không phải tóm tắt mới nhất.

### A. Giao diện (UI) — đang hoàn thiện

- **Đã có:** HUD và menu nền, kho đồ, 8 vị trí trang bị thử, ô xem chi tiết vật phẩm, bảng rèn, màu phẩm chất và hiển thị dòng phụ ẩn/đã biết/khóa. Có công cụ kiểm tra kích thước bố cục.
- **Đang làm:** nâng cấp mỹ thuật trên khung UI đã hoạt động; đã có một vài asset thử nghiệm, các asset giao diện còn đang vẽ và thay thế dần. Chưa chốt toàn bộ hình ảnh giao diện/hiệu ứng.
- **Sau này:** bảng võ học với 5 ô ưu tiên từ trái qua phải, thao tác kéo/đổi vị trí, nút **Auto** tự sắp xếp; công tắc **Chiến đấu thông minh** chỉ hiện/sử dụng khi được mở khóa bằng sự kiện.

### B. Gameplay & hệ thống sát thương — lõi đã hoạt động, thiết kế võ học đang chuẩn bị

- **Đã có trong mã:** Hero/quái tự di chuyển, chiến đấu, wave/map, các đòn cận chiến/cung/phép thử, sát thương/linh căn/kháng, điều kiện cộng thuộc tính, hồi chiêu tấn công thử, camera và save. Có nền định tuyến nhiều Hero nhưng đội hình và kỹ năng thực chưa hoàn thiện.
- **Đã kiểm thử một phần:** một số cơ chế sát thương từ trang bị và chuyển phương thức tấn công hiện tại. Ba nút Kiếm–Cung–Phép **chỉ là công cụ thử**, sẽ bỏ khi kỹ năng và đòn đánh thường theo vũ khí thay thế; không coi chúng là định hướng cuối.
- **Đang thiết kế, CHƯA triển khai:** tách **công kích vật lý cơ bản** và **công kích pháp thuật cơ bản**; mỗi đòn/kỹ năng tự khai báo loại sát thương (vật lý/pháp thuật), thuộc tính ngũ hành và cách tung (gần/xa/diện rộng) độc lập. Đòn đánh thường vật lý là Vô hệ; vũ khí pháp thuật dùng đòn thường phép Vô hệ. Chỉ số trang bị đóng góp theo **loại sát thương**, không theo khoảng cách đánh; dòng cộng sát thương vật lý không tự tăng sát thương phép.
- **Võ học tương lai:** năm ô ưu tiên; chiêu chỉ thi triển khi đủ linh lực, hồi chiêu, mục tiêu, tầm đánh; khi không có chiêu dùng được, luôn đánh thường tương ứng vũ khí (kiếm/đao/thương chém/đâm, cung/nỏ bắn, trượng/bùa/ấn phát pháp lực). Nhân vật có thể dùng cả võ học vật lý và pháp thuật, lựa chọn đầu tư quyết định hiệu quả.
- **Auto cơ bản (chưa làm):** tự động sắp xếp thứ tự hoặc người chơi tự chỉnh ưu tiên trái→phải; chiến đấu xét lần lượt các chiêu khả dụng, di chuyển để vào tầm nhưng không để chiêu đang theo đuổi mục tiêu vô thời hạn chặn các chiêu sau.
- **Chiến đấu thông minh (chưa làm):** **phần thưởng vĩnh viễn** sau chuỗi sự kiện/tàn quyển tâm pháp qua các mốc tiến trình, không tiêu hao vàng/vật liệu để vận hành. Đạt viên mãn mới mở khóa; người chơi được bật/tắt tùy ý. Khi bật có thể lựa chọn chiêu theo tình huống, phản đòn/né/khống chế hoặc phối hợp tốt hơn, nhưng cần cân bằng để không phủ nhận thứ tự thủ công. Tên tâm pháp, số chương, mốc mở khóa và thuật toán chưa chốt.

### C. Vũ khí, trang bị & luyện khí — thử nghiệm logic đạt, chưa có vòng vật phẩm đầy đủ

- **Đã có:** nhiều ô trang bị, kiếm thử, chỉ số chính/phụ, định danh từng item, kho vật phẩm cộng dồn, nâng cấp cấp item, rèn thường/đặc biệt, phẩm chất Phổ thông→Hoàng→Huyền→Địa→Thiên→Tiên, trần phẩm tự nhiên, mở khóa dòng theo **lần tăng phẩm chất**, hạ phẩm khóa lại dòng đã biết nhưng không quên và không tạo lại chỉ số. Save/Load metadata.
- **Phản hồi kiểm thử 14/10:** người phát triển xác nhận các logic rèn, lên phẩm, level, mở/khóa dòng và hạ cấp đang ổn, **chưa phát hiện lỗi trong các trường hợp đã thử**. Không suy rộng thành toàn bộ hệ thống đã hoàn thiện.
- **Chưa có/chưa chốt:** rơi đồ thật từ quái, bảng ngẫu nhiên/cân bằng chỉ số trên toàn bộ loại trang bị, giao dịch/phân rã, rèn thất bại, các phương pháp rèn rủi ro, nguồn vật liệu chính thức, tương tác sâu với võ học. Mốc 3/6/12/24/48 lượt thăng phẩm mới là cấu hình thử nghiệm.

### D. Âm thanh — đang chuẩn bị tích hợp nhạc nền

- **Đã có trên `main` ngày 14/10/2026:** 19 file OGG nhạc map + 1 file opening cùng `.meta` và `Assets/Resources/MapBgmCatalog.asset`. **BGM4 thiếu bài thứ tư**. Nhánh `feature/0.0.53` đang thử nút Âm lượng và lưu âm lượng; chưa nghiệm thu Unity/Windows. Xem `Docs/BGM_0.0.52.md` và `Docs/Release_0.0.53.md`.
- **Mã tích hợp đã bổ sung:** 2 `AudioSource`, phát ngẫu nhiên không lặp liền, chuyển âm 10 giây, nhạc `opening` ở màn hình Chơi mới/Tiếp tục, playlist cố định trong cụm 5 map. Nhạc có trong GitHub; cần kiểm thử Unity/Windows. 0.0.53 bổ sung giao diện âm lượng tách module.
- **Ánh xạ đã đối chiếu Suno:** BGM1 Đồng bằng/Bình Nguyên; BGM2 Cao nguyên/Cao Sơn (tạm); BGM3 Đầm lầy/U Trạch; BGM4 Rừng rậm/Sơn Lâm; BGM5 Sa mạc/Hoang Mạc. Hải Vực chưa có nhóm riêng nên im lặng. Game có 6 `RegionTheme` và 8 `TerrainType`; khóa nhạc theo `RegionTheme` để tránh đổi nhóm liên tục trong 5 map nhỏ.
- **Chưa có:** hiệu ứng âm thanh (SFX), tiếng giao diện/đánh trúng/chiêu thức, thuyết minh/lồng tiếng (voice-over), tinh chỉnh âm lượng và độ trễ trong Windows build.

### E. Các bước tiếp theo

1. Nhận và **xác minh tệp BGM thực tế** cùng cách đặt tên; ánh xạ chủ đề chính xác, phát nhạc ngẫu nhiên với chuyển âm 10 giây, không reset khi đi qua từng map nhỏ trong cùng cụm.
2. Kiểm thử Editor Play Mode, Windows overlay, pause/new game/load và chuyển chủ đề; tránh tạo nhiều AudioSource/khởi động lại bài liên tục; đánh giá lỗi nhập tệp dài và mức âm lượng.
3. Hoàn thiện vòng trang bị + chỉ số vật lý/pháp thuật theo nguồn sát thương, sau đó tạo nền dữ liệu võ học và đòn đánh thường tương ứng vũ khí.
4. Triển khai 5 ô ưu tiên với chế độ Auto sắp xếp và AI cơ bản; chuỗi tâm pháp mở khóa chiến đấu thông minh là giai đoạn tiến triển sau.
5. Tiếp tục UI art, SFX, hiệu ứng võ học, boss/event/map khó, rồi cân bằng và kiểm thử dài hạn.

**Cổng nghiệm thu trước khi gộp main:** thấy đủ file BGM đã push; đối chiếu nhóm đúng; các trường hợp thử map 1→5 (không đổi nhóm), map 5→6 (đổi nhóm), hết bài/chuyển bài 10 giây, ngừng/tiếp tục màn hình game, không gây lỗi Unity; đồng thời so sánh khác biệt nhánh và xác nhận không bỏ sót thay đổi trang bị đã nghiệm thu.

---


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

**B2 bắt đầu (12/10/2026):** người phát triển đã cho phép triển khai B2 trên nhánh `feature/b2-combat-target-routing`. Phần định tuyến nhiều Hero, chọn mục tiêu và định danh đạn đã có mã + 14 test, **chưa nghiệm thu Unity**. Chưa thêm đồng đội vào Scene, UI hay save; xem `Docs/B2_CombatRouting.md`. `main`, B1, `.54g` vẫn giữ checkpoint.

**Quay lại B2 sau hotfix được chấp nhận (12/10/2026):** người phát triển đã thử ổn bản sửa camera và Y trên nhánh hotfix `.54g`. Đã đồng bộ có chọn lọc mã vào B2, **không ghi đè logic chuyển camera sang đồng đội sống**; giữ bộ lọc 0.7s/240 đơn vị/s, Hero không có mục tiêu giữ Y. Có 21 bài camera, 4 bài lane, 14 bài định tuyến B2. **Chờ chạy Unity/Mono với mã B2 tích hợp**, chưa gộp `main`. Xem `Docs/B2_CombatRouting.md`.

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

1. `.54g` đã chốt, **B1 đã nghiệm thu** và **B2 đã được duyệt bắt đầu**. B2 hiện chờ chạy Mono/Unity và kiểm chứng chiến đấu nhiều Hero; chưa được phép merge `main` hay triển khai hệ tuyển đồng đội, item/skill/save đội.
2. Với đội hình sau này, tạm chấp nhận camera lấy trung điểm `(minX+maxX)/2` khi toàn đội đủ nằm trong khung. Quy tắc đội hình tách quá rộng để chốt ngay trước Mốc E.
3. Số slot, loại item, bảng hiệu ứng vật phẩm, kiểu kỹ năng và hình ảnh/sound chưa cần chốt ở Mốc A/B; chỉ chốt contract khi triển khai lát cắt tương ứng.

### Checkpoint B1 đã chốt

Người phát triển xác nhận B1 đã xong sau commit mã `2308f25`. Chỉ cập nhật tài liệu chốt mốc, không thay code gameplay B1. B2 là hướng kế tiếp, không tự coi là được phê duyệt. Xem `Docs/B1_HeroCombatState.md`.

### Cập nhật 0.0.53 — trang Cài đặt và trở lại opening

- **Đã bổ sung trên nhánh:** trang Cài đặt hai cột với 3 lựa chọn cửa sổ 600/800/1150, thanh âm lượng kéo, khôi phục mặc định (800×450; âm lượng 60%) và Quay lại; nối chung bộ resize/BGM có sẵn.
- **System Menu khi chơi:** nút Quay lại màn hình chính, lưu AutoSave thành công trước khi dừng wave/xóa đối tượng chiến đấu và tái hiện opening. Trường hợp lưu không thành công phải giữ nguyên màn chơi.
- **Chưa kiểm thử Play Mode/Windows:** vận hành thực của thanh kéo với chuột native, layout sau resize, AutoSave/Continue sau quay về menu. Chưa gộp vào `main`.

### 0.0.53 — Kho ảnh/video splash mở rộng

- **Đã viết và commit:** danh mục kho `Assets/Resources/OpeningSplashArt` tự quét PNG/JPG Sprite và MP4/H.264; hai vị trí splash chọn tài nguyên ngẫu nhiên, tối ưu một VideoPlayer, hiệu ứng zoom có tốc độ thay đổi và crossfade.
- **Đang chờ:** người phát triển bổ sung ảnh/video thật, kiểm thử codec Windows, GPU/VRAM và độ mượt, điều chỉnh thời gian/khung nhân vật, phân loại splash, shader/particle sau khi nghiệm thu. Thư mục còn trống khi vừa pull, tiếp tục thấy các khối màu trước đó.
- **Hướng dẫn:** [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md).

### 0.0.53 — Chốt hướng opening mới nhất

- Tên game: **Bách Luyện Tiên Đồ**. Đã có logo nền trong suốt và vị trí logo trên cùng, căn giữa. Không đưa font TTF vào repo.
- Video nền tràn cửa sổ (một lớp dưới cùng), tùy chọn MP4 `Assets/StreamingAssets/OpeningBackground/opening.mp4`; núi/mây hoạt động phía trên.
- Hủy hiệu ứng slideshow splash tự động; một khung splash tĩnh duy nhất phát `Assets/StreamingAssets/OpeningSplash/splash.mp4`, **chỉ fade in ban đầu ở giây 27**. Tác giả tự dựng pan/zoom/cut/fade nhân vật bên trong video.
- Còn thiếu: hai tệp MP4 từ tác giả, đo hiệu suất/tương thích codec, nghiệm thu Unity Play Mode/Windows/resize. Mã đã commit nhưng chưa merge `main`. Tài liệu: [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md).

### 0.0.53 — Chỉnh theo phản hồi kiểm thử opening (mới nhất)

- **Đã hoàn thiện mã trên nhánh:** hai khung splash trái/phải chỉ tràn mép ngang, giới hạn chiều cao 76% phía dưới cửa sổ, random PNG/JPG/MP4 tại lần mở menu; khung cố định, chỉ fade in từ giây 27, không pan/zoom/crossfade/slideshow; video nền và logo giữ nguyên.
- **Đã thêm:** shader feather alpha 48 px ở mép 5 khung núi/mây; thông số chỉnh được trực tiếp ở `Assets/Resources/OpeningArtTheme.asset`.
- **Chưa xác minh:** hình ảnh thực tế tại ba độ phân giải, codec đồng thời hai splash video + một background, hiệu năng 40HX, mức feather theo CanvasScaler; cần người phát triển thử trong Play Mode.
- **Hướng dẫn:** [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md).

### 15/10/2026 — Opening black reveal và nhận diện tên game

- **Đã commit trên `feature/0.0.53`:** mở từ màn đen hoàn toàn, sáng toàn bộ nền và mây/núi trong 1,8s, đợi 3s rồi logo/nút fade in 1,5s; khóa click cho đến lúc menu hiện đầy đủ.
- **Đã sửa file PNG logo trong repo:** pixel nét chữ **trắng trên alpha gốc**, tô đỏ sẫm `#4A1214` tại Unity, lớn gần gấp đôi. Nút dùng chữ trắng, bố cục tránh chồng.
- **Chưa kiểm thử trực quan:** Play Mode/Windows, mở/thoát và về lại title, fade trên cả viewport khi resize, tương phản logo trước nền sáng. Giữ `main` nguyên trạng đến khi nghiệm thu.

### 15/10/2026 — Kích thước Inspector và phân loại splash trái/phải

- **Đã triển khai trên `feature/0.0.53`:** thêm 5 cặp kích thước `Vector2` cho hai núi/ba lớp mây vào `OpeningArtTheme.asset`, đơn vị UI tham chiếu ở cửa sổ 800×450; giữ nguyên số đo mặc định và scale theo kích thước cửa sổ. Tên Hierarchy của trời, mây/núi và hai khung splash tự thêm `[rộngxcao UI]`.
- **Splash phân bên nghiêm ngặt:** chỉ chọn ảnh/video PNG/JPG/JPEG/MP4 có **tên file** mở đầu bằng `L` cho khung trái và `R` cho khung phải, không phân biệt hoa/thường; không fallback sang `splash.mp4` cũ. Chưa có file hợp lệ sẽ hiển thị placeholder và cảnh báo.
- **Kiểm thử cần làm:** rename những MP4 đã push (hiện không có `L`/`R`), thử chỉnh kích thước từ Inspector, xác nhận click/menu và 3 độ phân giải không bị lệch. Chưa nghiệm thu Unity/Windows, chưa merge `main`.

### 15/10/2026 — Căn chỉnh opening bằng Unity Play Mode (không dùng Prefab)

- **Đã implement trên `feature/0.0.53`:** cửa sổ công cụ Editor ở Tools/Bach Luyen Tien Do/Opening Layout (Play Mode), bật chế độ giữ nguyên Scene thực đang chạy, chọn 13 `RectTransform`, kéo/sửa trong Scene View, lưu vị trí/kích thước vào `OpeningArtTheme.asset` khi đang Play.
- **Đã sửa:** layout call và animation không ghi đè đối tượng đang sửa; chuyển động mây tính quanh tâm đã lưu; sau khi lưu, preview chơi lại từ đầu; không đổi logic splash L/R hay GameManager. Các giá trị scale từng trục theo cửa sổ mẫu 800×450.
- **Cần nghiệm thu:** ghi asset thực qua AssetDatabase trong Unity, vị trí Scene/Game View khi Play, đủ 3 độ phân giải, video crop theo khung splash và UI native overlay. Không đưa vào `main` trước khi xác nhận.
- **Hướng dẫn:** [Opening_PlayMode_Layout_0.0.53.md](Opening_PlayMode_Layout_0.0.53.md).
