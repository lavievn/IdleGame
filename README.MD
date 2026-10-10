# IdleGame — game tu tiên tự động trên desktop Windows

**Trạng thái hiện tại: 0.0.4.54g — ĐÃ CHỐT VÒNG PHÁT TRIỂN (11/10/2026), theo nghiệm thu có giới hạn của người phát triển.** Nhánh `feature/0.0.4.54g` vẫn lưu mốc này; **chưa gộp `main`**.
**Người phát triển xác nhận gameplay/camera đang sử dụng được** với Hero cận/xa, tốc độ chạy nhanh/chậm; cảm giác camera chưa đạt 100% nhưng đã đủ để chuyển sang tính năng mới. Điều này **không đồng nghĩa mọi hệ thống, save, DPI và Windows build đều đã được kiểm thử đầy đủ**.

Game idle RPG tu tiên 2D/2.5D dạng **desktop overlay trong suốt**, chơi tự động trong một cửa sổ nhỏ. Hero đi tìm quái, chiến đấu theo các dạng cận chiến/cung/phép, tăng cấp, lưu tiến độ và chuyển bản đồ. Game hiện ở giai đoạn **xây dựng khung gameplay, độ ổn định, UI và thông số thử nghiệm**; chưa phải sản phẩm hoàn thiện.

## Nhánh tính năng đang phát triển: B1 — trạng thái chiến đấu Hero

**Nguồn:** checkpoint `feature/0.0.4.54g` đã được chấp nhận sử dụng; gameplay .54g không đổi. **Nhánh làm B1:** `feature/b1-hero-combat-state` (chưa merge main hoặc .54g). B1 **chưa được nghiệm thu** bằng Unity/Windows.

- Thêm `HeroCombatState` làm nơi lưu duy nhất cho Controller, dữ liệu runtime, HP hiện tại/tối đa, nhịp đánh và thế vận sức của mỗi Hero. `CombatManager` vẫn điều phối một Hero qua các thuộc tính chuyển tiếp, chưa có đội hình thật.
- Test `HeroCombatState` độc lập cho 2 actor và cập nhật helper reflection của các test cũ, không làm bản sao HP/timer. `Tests~/run.sh` bổ sung script nguồn mới.
- **Không đụng tới camera, UI, Scene, định dạng save, damage/balance, quái, projectile hay tiến trình**. B2 mới chuyển luồng đánh/trúng/đích sang nhiều actor.
- Xem [Docs/B1_HeroCombatState.md](Docs/B1_HeroCombatState.md) để biết kiểm thử phải đạt và ranh giới B1/B2. Có thể chuyển về nhánh `feature/0.0.4.54g` để đối chiếu.

## Phiên bản hiện tại và đường dẫn

- **Nhánh đã chốt mốc gameplay:** [feature/0.0.4.54g](https://github.com/lavievn/IdleGame/tree/feature/0.0.4.54g). Không tự merge vào `main` hoặc đặt tag/release; phiên bản tiếp theo sẽ được tách nhánh riêng khi thống nhất phạm vi.
- [Ghi chú giao diện và sửa lỗi .54g](Docs/Release_0.0.4.54g_ui.md).
- [Ghi chú cân bằng .54f](Docs/Release_0.0.4.54f.md) · [Ghi chú .54e](Docs/Release_0.0.4.54e.md) · [Nền ổn định tọa độ .54d](Docs/Release_0.0.4.54.d_prefinal.md).
- [Project_info_dev.txt](Project_info_dev.txt) là tài liệu **bàn giao kỹ thuật và định hướng**. Các mục .54a/.54b/.54c/.54d trong đó là lịch sử nếu xung đột với mục mới.

### Chức năng hiện có trong mã và giới hạn

| Hệ thống | Hiện trạng của nhánh .54g |
| --- | --- |
| Windows overlay | Cửa sổ trong suốt/không viền, nhập chuột Windows, kéo cửa sổ từ Ground và UI, click xuyên vùng trống. Menu kích thước **600 / 800 / 1150** theo chiều ngang (tỷ lệ 16:9), không còn menu 200/500/1000 hay 250/500/800 trước đây. |
| Tọa độ trận đấu | **Ground logic và Canvas chiến đấu ổn định** khi resize; hệ tọa độ đạn, vị trí Hero/quái, tầm đánh và camera không lấy theo kích thước cửa sổ mới. Cần kiểm thử thật trong bản build. |
| Hero/quái và camera | Hero tự di chuyển về bên trái; quái tự di chuyển về bên phải hoặc dừng đánh khi đúng tầm riêng. **Deadzone + SmoothDamp (.54g mới nhất):** vùng chết thử nghiệm mở rộng lên **38% chiều rộng Ground** quanh tâm (mặc định 50%). Camera **đứng yên** khi Hero di chuyển trong vùng này; khi Hero vượt mép vùng chết, camera bám mượt với `smoothTime = 0,2 giây`, Hero đi trước camera một chút. Hero sau khi **đã chạy tới gần mép trái** rồi dừng sẽ được camera đưa nhẹ về **40% chiều ngang**, thời gian làm mượt hồi vị trí **0,6 giây**; Hero chậm dừng gần tâm và Hero đứng yên từ đầu không bị kéo lại. **Vùng đỏ ngoài** chỉ tăng độ nhạy đuổi khi vượt biên, không khóa Hero. Camera, Ground, quái và đạn cùng dùng một độ dịch hình ảnh; chuyển động riêng của Hero/quái vẫn được tính độc lập. |
| Chiến đấu | Có cận chiến, cung, phép, đánh 1–n, đòn có thời gian bay, nổ diện rộng, bảng chi tiết tính damage, hụt đòn, thanh vận sức phép. Hero phép chỉ vận sức khi đã có mục tiêu hợp lệ; bán kính nổ của Hero là **250 đơn vị Ground**. Các giá trị là thử nghiệm. |
| Cân bằng thử nghiệm .54f | HP khởi tạo mẫu **400**, được lưu như dữ liệu riêng chứ không là luật cố định cho nhân vật tương lai. Cận chiến nhân HP nền ×2, +50–100 HP riêng mỗi cấp, hồi 3–5 HP khi hạ quái có tăng 5% theo cấp; cung HP nền ×0,6, damage dạng đánh ×0,7, tần suất đánh gốc 150%; phép cần mục tiêu mới vận sức. Đổi thế giữ tỷ lệ máu. Cần thực chiến và điều chỉnh sau. |
| Linh căn | Ngũ hành/linh căn Vô, nhiều hệ có tỷ trọng, cấp căn, tính toán sát thương theo hệ, tương khắc; chưa có hệ trang bị/skill hoàn thiện. |
| Hành trình | **5 wave/map, 5 map/cụm**, tên Hán Việt, địa hình (đồng bằng, hồ nước, đồi núi...), loài quái theo môi trường; map khó và hệ số thử nghiệm. Sau mỗi wave cập nhật số wave; chuyển map sau khi hoàn thành wave 5. |
| Lưu và khôi phục | Lưu tay và tự động, New/Continue có xác nhận, lưu EXP/cấp/balance. Quái thoát hẳn mép phải thì chơi lại wave dở. Thất bại trở lại **map đầu cụm 5 map** và wave 1; giữ nhân vật và checkpoint. |
| HUD | Bảng **chỉ số đầy đủ** phía trên phải dưới ba nút Chỉ số–DEVB–Tạm dừng; log damage dạng “nguồn → đích - sát thương” phía trái; nút Kiếm/Cung/Phép dưới log; EXP hiện tại/cần lên cấp, tốc đánh, tốc di chuyển, tầm đánh, linh căn. Log chi tiết có trong Info. |
| Hiệu ứng hiển thị | Tên map chữ lớn ở giữa trong **3 giây**, sau đó thu nhỏ và di chuyển thành nhãn cố định trong **0,85 giây**. Nhãn gọn kèm **Bản đồ N, Wave hiện tại/5, địa hình** để debug. Chữ damage Hero/quái được đặt vào chính đối tượng nhận sát thương, nảy ngược hướng đòn đánh và mờ trong **1 giây**. |
| DEVB | Bảng kiểm tra thông số Hero và quái, cân bằng nền/growth/range và dữ liệu cài đặt riêng. Đây là công cụ debug, không phải UI hoàn chỉnh cho người chơi. |

**Cần phân biệt:** người phát triển đã kiểm thử ổn **các chế độ Hero cận/xa và tốc độ di chuyển nhanh/chậm** và chốt `.54g`. Chưa có xác nhận mọi tổ hợp tính năng, save cũ, DPI, cửa sổ Windows kéo dài hoặc tất cả hiệu ứng đều đã được nghiệm thu trong EXE.

## Vấn đề đã sửa trong chuỗi .54d → .54g

1. **.54d:** tách Canvas chiến đấu logic khỏi kích thước cửa sổ; chống wave kẹt khi quái thoát phải; chết quay về đầu cụm 5 map.
2. **.54e:** đưa ba nút chuyển thế đánh vào HUD; log damage rút gọn; thêm EXP, ba preset 600/800/1150 và khung chỉ số. Có commit sửa lỗi biên dịch `IdentityDisplay`.
3. **.54f:** UI không hiện đè màn hình bắt đầu/Game Over, thêm chữ Menu, bảng chỉ số bố cục lại; hệ số HP/tốc đánh/sát thương ba thế, vận sức phép và tăng trưởng HP có dữ liệu lưu.
4. **.54g:** khôi phục toàn bộ chỉ số để theo dõi cân bằng; chia cột log và chỉ số; hiệu ứng tên map; chữ sát thương nảy và mờ 1 giây.
5. **Bổ sung cuối .54g:** số wave và địa hình dưới tên map, cập nhật mỗi wave; cố định điểm bắt đầu text damage vào thân Hero/quái thay vì vị trí thiết kế cũ/HP bar; sửa bù dịch chuyển của quái dừng đánh theo Ground thực; cập nhật tài liệu.
6. **Lịch sử camera mềm cuối .54g (đã thay thế):** ranh giới đỏ chỉ kích hoạt tăng tốc đuổi Hero trong 1 giây, không ép Hero đứng tại biên. Sau khi trở về x≈62%, camera trả lại tốc độ cuộn nền qua giai đoạn giảm/chuyển tốc 0,4 giây, **không khóa vị trí Hero**; dùng chung bước dịch với quái/Ground trong mỗi khung hình. Đã bỏ và hoàn tác cách thử sai trước đó là chờ đứng yên 1 giây rồi mới hồi camera. **Cơ chế này đã được thay bằng Deadzone + SmoothDamp, không còn là trạng thái hiện tại.**

Không lấy việc mã chạy trong test doubles làm bằng chứng đã hết lỗi Unity. Nếu quái còn trôi, cần quay video ở trường hợp **Hero cận chiến tiến lại gần quái cung/phép đứng đánh** và kiểm tra pan của Ground so với actor.

7. **Camera Deadzone hiện hành (.54g):** bỏ máy trạng thái cuộn nền/đuổi/release; dùng vùng chết giữa Ground (**38%**, từ 10% ở bản trước) với SmoothDamp trục X (0,2s), không có vận tốc tự cuộn khi Hero đứng bắn. Khi Hero ra khỏi Deadzone mới bám; ra khỏi vùng đỏ thì bám nhạy hơn. Đồng bộ `backgroundPan = cameraPan` cho toàn bộ Ground/quái/đạn, bảo vệ tọa độ logic ổn định khi resize.

## Kiểm thử và cách lấy nhánh

1. GitHub Desktop → **Fetch origin** → **Current Branch** → `feature/0.0.4.54g` → **Switch Branch**; nếu đang ở chính nhánh đó và GitHub báo có commit mới thì **Pull origin**. Không nhấn `Discard` đối với tệp Unity đang sửa cục bộ.
2. Mở đúng thư mục đó trong Unity Hub, phiên bản theo `ProjectSettings/ProjectVersion.txt` (tại thời điểm cập nhật: `6000.5.0f1`), mở `Assets/Scenes/MainGame.unity`, đợi biên dịch xong và kiểm tra **Console**.
3. Test UI ở **600, 800, 1150**: màn hình Start/Confirm/Game Over, nút đổi thế, chữ chỉ số/linh căn/EXP, menu và log không tràn/đè Ground. Test nhãn map ở giữa 3s → thu nhỏ, sau đó `Bản đồ / Wave x/5 / Địa hình` cập nhật liên tục.
4. Test combat: chữ damage bật ra **ngay trên thân nạn nhân** rồi mờ trong 1s; quái đánh xa đứng yên khi Hero cận tiến tới nhưng vẫn trôi **bằng Ground**, không trôi chậm theo camera. Thử đổi thế, đạn đang bay, nhiều quái, resize và pause.
   **Camera Deadzone:** kiểm tra Hero đứng yên từ đầu (camera và Ground đều đứng), chạy khoảng 150–180 đơn vị quanh tâm (camera vẫn đứng), gần biên đỏ mới bắt đầu theo (camera bám trễ 0,2s), Hero đang chạy rồi dừng để bắn (camera giảm tốc và dừng), lặp hai lượt đi–dừng, hồi vị trí 40% khi đứng bắn, Hero chậm dừng gần tâm không bị camera kéo, chạy nhanh vượt vùng đỏ, quái cung đứng bắn, pause/Retry và resize 600/800/1150. Không được tự cuộn đến biên đỏ hay lắc trái–phải khi Hero đứng.
5. Test luồng 5 wave/5 map, chết và retry về đầu cụm, autosave/manual save, tải save cũ/mới; quan sát lỗi Console trước khi build Windows mới.
6. **Build Windows ra thư mục mới**, thử transparency, kéo/thu phóng, DPI 100/125/150%, màn hình phụ, taskbar, chạy nhiều giờ, bộ nhớ và CPU/GPU. Unity Editor không xác minh được các hành vi WinAPI.

`Tests~/` dùng bản mô phỏng API Unity, **không phải Unity Test Framework/Editor Play Mode thực**. Kết quả thử lịch sử của .54d (139 tình huống logic, 720 mô phỏng) chỉ áp dụng đúng phiên bản đó; việc có thêm bài kiểm tra ở .54e/.54f/.54g **không có nghĩa chúng đã chạy thành công**. Xem `Tests~/test-results.txt` để xác định kết quả thực sự đã ghi.

**Tinh chỉnh sau phản hồi Hero tốc độ 700:** giữ `cameraSmoothTime=0.2`, tăng `cameraDeadzoneRatio=0.38` để Hero đến khoảng x=-190 trước khi camera bám (với Ground 1.000, tâm x=0, biên đỏ trái x≈-227,5); giá trị hiệu lực được giới hạn không vượt vùng đỏ. `Assets/Scenes/MainGame.unity` có giá trị tường minh để Unity không giữ thiết lập cũ. Đây là mốc thử nghiệm, không phải camera nhiều Hero. 

**Tinh chỉnh tham khảo video gameplay (11/10/2026):** chạy giữ Deadzone 38% + SmoothDamp 0,2 giây. Khi Hero đang đi rồi **dừng ở phía trái vị trí 40%**, camera tiếp tục dịch Ground/Hero nhẹ về phải, nội suy SmoothDamp **0,6 giây**, rồi dừng. Hero xuất hiện đứng yên hoặc Hero chậm dừng gần tâm không bị ép sang 40%; nếu đi trở lại giữa lúc đang hồi, hủy hồi và dùng lại Deadzone. Cấu hình `cameraIdleRestX=0.4`, `cameraIdleSmoothTime=0.6` ghi cả trong scene; đây là thông số thử, chưa nghiệm thu bản Windows/Unity. 

**Camera tâm chung (.54g, đã chốt mức dùng được, còn dư địa cải thiện):** bật `cameraUseCombatCenter=true` trong `EnvironmentManager` để dùng **trung bình X của các Hero đang hoạt động + quái còn sống nằm trong viewport** làm mục tiêu bám. Không tính Ground/cỏ, vật trang trí, đạn, VFX, quái chết/ngoài cửa sổ. Cùng thuật toán **Deadzone 38%, SmoothDamp 0,2s và hồi mốc 40%/0,6s khi Hero dừng**; Hero thật vẫn được bảo vệ tại mép màn hình. **Tắt `cameraUseCombatCenter`** để so sánh lại bản camera theo Hero trước đó, không cần revert. Có nhiều quái ở một phía thì phép trung bình sẽ bị nghiêng về phía đông quái — mục đích hiện tại là thử cảm giác, chưa phải quy tắc camera đội hình đã chốt.

**Ưu tiên sau khi chốt `.54g` (B1 ĐANG TRIỂN KHAI Ở NHÁNH RIÊNG):** xem [Docs/Development_Roadmap.md](Docs/Development_Roadmap.md). **P0 trước tiên: tách trạng thái chiến đấu theo từng Hero (mốc B1) nhưng chưa thay gameplay một Hero**, rồi P1: save/schema → trang bị tối thiểu → kỹ năng tự động; P2: hai Hero + đội hình → asset/VFX/SFX; P3: boss/luân hồi/ngoại tuyến/polish camera. **B1 đã được người phát triển yêu cầu bắt đầu**, nhưng chưa có nghiệm thu Unity; các mục B2/P1–P3 vẫn là kế hoạch cần duyệt.

## Hướng tiếp theo — CHƯA triển khai

**Trước tiên (đề xuất phiên bản tiếp theo):** tách riêng trạng thái chiến đấu Hero (HP/timer/dữ liệu thế đánh), giữ nguyên hành vi hiện tại và kiểm thử hai trạng thái độc lập mà không tạo đội hình thật. **Song song:** để lại danh sách hậu kiểm UI/damage/di chuyển, save, toàn cụm 25 wave và Windows; chỉ ngắt phát triển nếu gặp lỗi nghiêm trọng. Việc đo lại cân bằng sẽ có ích sau khi có trang bị/kỹ năng đầu tiên.

**Tiếp theo, theo ưu tiên dự kiến:**

- **Item/trang bị:** định nghĩa dữ liệu item với cộng HP/ATK/phòng thủ hoặc giảm sát thương, tăng sát thương, bonus theo từng linh căn và vũ khí; hiệu lực là tầng riêng trên HP/ATK gốc, tránh ghi đè dữ liệu nhân vật khởi tạo.
- **Slot placeholder:** khung vị trí trang bị/kho và nơi kéo–thả sau này; hiện chưa cam kết số ô hay từng chủng loại.
- **Kỹ năng:** placeholder hiển thị kỹ năng, dữ liệu tầm gần/xa và theo hệ, chi phí/hồi chiêu, tự động sử dụng **theo thứ tự ưu tiên**; một nhân vật có thể phối hợp kỹ năng nhiều tầm. Chưa có hệ tự động vận hành đầy đủ.
- **Âm thanh:** slot placeholder cho SFX đòn đánh/trúng đòn/skill/map/UI, thêm cơ chế gắn asset và âm lượng về sau.
- **Công cụ biên tập nội dung:** tool giao diện để nhập/gắn asset, animation, hiệu ứng, âm thanh cho Hero/quái/vũ khí/skill; mục tiêu cấu hình qua dữ liệu, không phải sửa code từng asset. Cần thiết kế schema, kiểm tra tài nguyên và đường xuất Unity trước khi làm.
- **Xa hơn:** khởi tạo ngẫu nhiên xuất thân/cốt truyện/linh căn/tâm pháp/võ học/chỉ số, trang bị quyết định phần lớn tăng trưởng; boss/elite/đồng đội, luân hồi, ngoại tuyến, UI hoàn chỉnh và hình ảnh theo chủ đề.

**Nguyên tắc kiến trúc:** HP gốc 400 và ba thế đánh hiện tại là dữ liệu **kiểm thử**, không được hardcode như giới hạn thiết kế tương lai. Chỉ số phải tách nhân vật gốc, tiến trình cấp, linh căn, trang bị, kỹ năng, buff/debuff và hệ số kiểu đánh; lưu kết quả ngẫu nhiên vĩnh viễn vào save.

## Hướng dẫn vá Windows cũ .51f và vì sao xóa tệp cũ

`WINDOWS_OVERLAY_FIX_0.0.4.51f.txt` đã lỗi thời và được thay bằng README này cùng lịch sử Git.

Từ bản vá .51f có ba bài học còn áp dụng:
- `MenuArea` thực tế có thể là **uGUI Button**, không được chỉ tìm `CustomInteractable`; tránh gửi hai click khi có cả hai luồng.
- Tương tác chuột ở Windows overlay phải thống nhất hệ tọa độ WinAPI và hitbox UI; khi kéo giữ điểm nắm, thả ngoài cửa sổ phải ngừng kéo, không snap trở lại mép màn hình.
- Sau thay đổi kích thước phải giữ trạng thái transparency/topmost theo chức năng đang có, không dùng PlayerSettings cũ để suy ra menu/size runtime.

**Không làm theo hướng dẫn .51f** như chép đè hai script từ gói cũ, chọn 200/500/1000, hoặc khởi động ở 1000×563: những điều đó là **lịch sử bản vá**, không phải đặc tả của .54g. Muốn phục hồi một mốc cũ, dùng lịch sử commit GitHub thay vì tệp hướng dẫn cũ.

## Quy mô dự án và mức hoàn thiện

Ước lượng **cũ**: phần đồ họa/hoạt ảnh/hiệu ứng chiếm 60–70% khối lượng, đã hoàn thành 10–15% (ước lượng do người phát triển cung cấp); phần còn lại chiếm 30–40%, hoàn thành khoảng 35–45% (ước lượng sơ bộ). Với trọng số cộng bằng 100%, tổng khoảng **17,5–27%, trung tâm khoảng 22%**. Đây là ước lượng tại mốc .54d, **chưa được đo lại cho .54g**, và không phản ánh độ ổn định hay mức sẵn sàng phát hành.

Mọi thay đổi kỹ thuật phải có tài liệu phiên bản và kiểm thử hồi quy; phân biệt **có mã / kiểm thử mô phỏng / chạy Unity Editor / chạy Windows thật**.