# IdleGame — game tu tiên tự động trên desktop Windows

**Trạng thái hiện tại: 0.0.4.54g (bản thử nghiệm; đang chuẩn bị gộp từ nhánh `feature/0.0.4.54g` vào `main`).**  
**Đã có mã ≠ đã kiểm thử hoàn chỉnh trong Unity/Windows.** Các thay đổi mới cần được chạy thực tế ở Unity Editor và bản build Windows trước khi nghiệm thu.

Game idle RPG tu tiên 2D/2.5D dạng **desktop overlay trong suốt**, chơi tự động trong một cửa sổ nhỏ. Hero đi tìm quái, chiến đấu theo các dạng cận chiến/cung/phép, tăng cấp, lưu tiến độ và chuyển bản đồ. Game hiện ở giai đoạn **xây dựng khung gameplay, độ ổn định, UI và thông số thử nghiệm**; chưa phải sản phẩm hoàn thiện.

## Phiên bản hiện tại và đường dẫn

- **Nhánh đang hoàn thiện:** [feature/0.0.4.54g](https://github.com/lavievn/IdleGame/tree/feature/0.0.4.54g). `main` chỉ được cập nhật khi người phát triển chủ động gộp nhánh.
- [Ghi chú giao diện và sửa lỗi .54g](Docs/Release_0.0.4.54g_ui.md).
- [Ghi chú cân bằng .54f](Docs/Release_0.0.4.54f.md) · [Ghi chú .54e](Docs/Release_0.0.4.54e.md) · [Nền ổn định tọa độ .54d](Docs/Release_0.0.4.54.d_prefinal.md).
- [Project_info_dev.txt](Project_info_dev.txt) là tài liệu **bàn giao kỹ thuật và định hướng**. Các mục .54a/.54b/.54c/.54d trong đó là lịch sử nếu xung đột với mục mới.

### Chức năng hiện có trong mã và giới hạn

| Hệ thống | Hiện trạng của nhánh .54g |
| --- | --- |
| Windows overlay | Cửa sổ trong suốt/không viền, nhập chuột Windows, kéo cửa sổ từ Ground và UI, click xuyên vùng trống. Menu kích thước **600 / 800 / 1150** theo chiều ngang (tỷ lệ 16:9), không còn menu 200/500/1000 hay 250/500/800 trước đây. |
| Tọa độ trận đấu | **Ground logic và Canvas chiến đấu ổn định** khi resize; hệ tọa độ đạn, vị trí Hero/quái, tầm đánh và camera không lấy theo kích thước cửa sổ mới. Cần kiểm thử thật trong bản build. |
| Hero/quái và camera | Hero tự di chuyển về bên trái; quái tự di chuyển về bên phải hoặc dừng đánh khi đúng tầm riêng. Quái luôn được bù chênh lệch Ground–camera, cả khi đang đứng đánh. **Camera mềm .54g:** trong vùng đỏ giữ tốc độ cuộn bình thường; khi Hero *vượt* mép đỏ, camera tăng/điều chỉnh tốc độ **từ từ trong 1 giây** để đuổi tới vị trí gần 62% chiều ngang, **không ghim Hero vào biên đỏ**. Khi đuổi kịp, camera giữ nhịp theo Hero (Hero dừng thì camera dừng). Chỉ mép cửa sổ thật mới là giới hạn an toàn. |
| Chiến đấu | Có cận chiến, cung, phép, đánh 1–n, đòn có thời gian bay, nổ diện rộng, bảng chi tiết tính damage, hụt đòn, thanh vận sức phép. Hero phép chỉ vận sức khi đã có mục tiêu hợp lệ; bán kính nổ của Hero là **250 đơn vị Ground**. Các giá trị là thử nghiệm. |
| Cân bằng thử nghiệm .54f | HP khởi tạo mẫu **400**, được lưu như dữ liệu riêng chứ không là luật cố định cho nhân vật tương lai. Cận chiến nhân HP nền ×2, +50–100 HP riêng mỗi cấp, hồi 3–5 HP khi hạ quái có tăng 5% theo cấp; cung HP nền ×0,6, damage dạng đánh ×0,7, tần suất đánh gốc 150%; phép cần mục tiêu mới vận sức. Đổi thế giữ tỷ lệ máu. Cần thực chiến và điều chỉnh sau. |
| Linh căn | Ngũ hành/linh căn Vô, nhiều hệ có tỷ trọng, cấp căn, tính toán sát thương theo hệ, tương khắc; chưa có hệ trang bị/skill hoàn thiện. |
| Hành trình | **5 wave/map, 5 map/cụm**, tên Hán Việt, địa hình (đồng bằng, hồ nước, đồi núi...), loài quái theo môi trường; map khó và hệ số thử nghiệm. Sau mỗi wave cập nhật số wave; chuyển map sau khi hoàn thành wave 5. |
| Lưu và khôi phục | Lưu tay và tự động, New/Continue có xác nhận, lưu EXP/cấp/balance. Quái thoát hẳn mép phải thì chơi lại wave dở. Thất bại trở lại **map đầu cụm 5 map** và wave 1; giữ nhân vật và checkpoint. |
| HUD | Bảng **chỉ số đầy đủ** phía trên phải dưới ba nút Chỉ số–DEVB–Tạm dừng; log damage dạng “nguồn → đích - sát thương” phía trái; nút Kiếm/Cung/Phép dưới log; EXP hiện tại/cần lên cấp, tốc đánh, tốc di chuyển, tầm đánh, linh căn. Log chi tiết có trong Info. |
| Hiệu ứng hiển thị | Tên map chữ lớn ở giữa trong **3 giây**, sau đó thu nhỏ và di chuyển thành nhãn cố định trong **0,85 giây**. Nhãn gọn kèm **Bản đồ N, Wave hiện tại/5, địa hình** để debug. Chữ damage Hero/quái được đặt vào chính đối tượng nhận sát thương, nảy ngược hướng đòn đánh và mờ trong **1 giây**. |
| DEVB | Bảng kiểm tra thông số Hero và quái, cân bằng nền/growth/range và dữ liệu cài đặt riêng. Đây là công cụ debug, không phải UI hoàn chỉnh cho người chơi. |

**Cần phân biệt:** những dòng trên mô tả mã đã sửa. Nhánh .54g vẫn cần người phát triển xác nhận bằng Play Mode và bản Windows; chưa có bằng chứng toàn bộ bảng chỉ số, hiệu ứng, kéo cửa sổ, DPI và di chuyển được nghiệm thu trong EXE thực.

## Vấn đề đã sửa trong chuỗi .54d → .54g

1. **.54d:** tách Canvas chiến đấu logic khỏi kích thước cửa sổ; chống wave kẹt khi quái thoát phải; chết quay về đầu cụm 5 map.
2. **.54e:** đưa ba nút chuyển thế đánh vào HUD; log damage rút gọn; thêm EXP, ba preset 600/800/1150 và khung chỉ số. Có commit sửa lỗi biên dịch `IdentityDisplay`.
3. **.54f:** UI không hiện đè màn hình bắt đầu/Game Over, thêm chữ Menu, bảng chỉ số bố cục lại; hệ số HP/tốc đánh/sát thương ba thế, vận sức phép và tăng trưởng HP có dữ liệu lưu.
4. **.54g:** khôi phục toàn bộ chỉ số để theo dõi cân bằng; chia cột log và chỉ số; hiệu ứng tên map; chữ sát thương nảy và mờ 1 giây.
5. **Bổ sung cuối .54g:** số wave và địa hình dưới tên map, cập nhật mỗi wave; cố định điểm bắt đầu text damage vào thân Hero/quái thay vì vị trí thiết kế cũ/HP bar; sửa bù dịch chuyển của quái dừng đánh theo Ground thực; cập nhật tài liệu.
6. **Camera mềm cuối .54g:** ranh giới đỏ chỉ kích hoạt tăng tốc đuổi Hero trong 1 giây, không ép Hero đứng tại biên. Khi quay về x≈62%, camera theo cùng tốc độ Hero; dùng chung bước dịch với quái/Ground trong mỗi khung hình. Đã bỏ và hoàn tác cách thử sai trước đó là chờ đứng yên 1 giây rồi mới hồi camera.

Không lấy việc mã chạy trong test doubles làm bằng chứng đã hết lỗi Unity. Nếu quái còn trôi, cần quay video ở trường hợp **Hero cận chiến tiến lại gần quái cung/phép đứng đánh** và kiểm tra pan của Ground so với actor.

## Kiểm thử và cách lấy nhánh

1. GitHub Desktop → **Fetch origin** → **Current Branch** → `feature/0.0.4.54g` → **Switch Branch**; nếu đang ở chính nhánh đó và GitHub báo có commit mới thì **Pull origin**. Không nhấn `Discard` đối với tệp Unity đang sửa cục bộ.
2. Mở đúng thư mục đó trong Unity Hub, phiên bản theo `ProjectSettings/ProjectVersion.txt` (tại thời điểm cập nhật: `6000.5.0f1`), mở `Assets/Scenes/MainGame.unity`, đợi biên dịch xong và kiểm tra **Console**.
3. Test UI ở **600, 800, 1150**: màn hình Start/Confirm/Game Over, nút đổi thế, chữ chỉ số/linh căn/EXP, menu và log không tràn/đè Ground. Test nhãn map ở giữa 3s → thu nhỏ, sau đó `Bản đồ / Wave x/5 / Địa hình` cập nhật liên tục.
4. Test combat: chữ damage bật ra **ngay trên thân nạn nhân** rồi mờ trong 1s; quái đánh xa đứng yên khi Hero cận tiến tới nhưng vẫn trôi **bằng Ground**, không trôi chậm theo camera. Thử đổi thế, đạn đang bay, nhiều quái, resize và pause.
   **Camera mềm:** kiểm tra Hero chạy nhanh hơn cuộn nền, đứng yên khi đánh và chạy chậm. Hero phải vượt vùng đỏ tự do, camera đuổi tăng dần tốc trong khoảng 1 giây (không chờ 1 giây rồi mới chạy), đưa Hero về gần 62% chiều ngang rồi giữ nhịp theo Hero. Quái đứng bắn, đạn và Ground phải không bị giật, kể cả lúc đổi 600/800/1150, pause và Retry.
5. Test luồng 5 wave/5 map, chết và retry về đầu cụm, autosave/manual save, tải save cũ/mới; quan sát lỗi Console trước khi build Windows mới.
6. **Build Windows ra thư mục mới**, thử transparency, kéo/thu phóng, DPI 100/125/150%, màn hình phụ, taskbar, chạy nhiều giờ, bộ nhớ và CPU/GPU. Unity Editor không xác minh được các hành vi WinAPI.

`Tests~/` dùng bản mô phỏng API Unity, **không phải Unity Test Framework/Editor Play Mode thực**. Kết quả thử lịch sử của .54d (139 tình huống logic, 720 mô phỏng) chỉ áp dụng đúng phiên bản đó; việc có thêm bài kiểm tra ở .54e/.54f/.54g **không có nghĩa chúng đã chạy thành công**. Xem `Tests~/test-results.txt` để xác định kết quả thực sự đã ghi.

## Hướng tiếp theo — CHƯA triển khai

**Trước tiên:** nghiệm thu UI/damage/di chuyển; sau đó **đo lại cân bằng chiến đấu** bằng thực chiến, đặc biệt 5 wave đầu và cụm 25 wave, thay vì điều chỉnh chỉ dựa vào cảm nhận.

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