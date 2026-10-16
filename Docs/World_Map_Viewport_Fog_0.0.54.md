# 0.0.54 — Camera bản đồ thế giới & sương mù

## Mục tiêu / quy ước

- Khi mở **Bản đồ**, căn ô nhân vật đang đứng vào vùng xem; lấy **chiều rộng hộp bao polygon bằng 40% chiều rộng vùng xem** (không phải 40% diện tích toàn hình). Giới hạn hệ số phóng 1×–16× để tránh phóng quá mức. Không thay đổi vị trí người chơi hay gameplay khi thao tác camera.
- Nếu map hiện tại thiếu polygon (TM-03, TM-13), định vị theo `WorldMapCatalog.PreviewPosition` và dùng bề rộng quy ước cho mức zoom; không tạo thêm polygon, không giả vờ diện tích 40% đã được đo.
- Giữ chuột trái trên nền bản đồ rồi kéo để pan; nếu di chuyển chưa đến **6 px**, thả chuột vẫn được coi là click để chọn map. Vùng xem có **RectMask2D** tránh vẽ/nhấn ra bên ngoài. Ngăn native Windows kéo cửa sổ khi đang pan bản đồ.
- Cuộn bánh xe trên vùng xem để phóng to/thu nhỏ quanh con trỏ. Thanh công cụ ở góc trên trái: **−**, **+**, **Về vị trí**; có nhãn hiển thị phần trăm zoom. Phím hoặc thanh zoom riêng có thể bổ sung sau.
- Fog: giữ nguyên ranh giới khám phá chính xác của polygon đã tới, thêm vùng nhìn thấy mờ dần **36 pixel nguồn** *ra bên ngoài* mép polygon rồi trở về fog đục. Vùng mở rộng chỉ có tác dụng **hiển thị**, không cập nhật `worldMapVisited`, liên kết di chuyển hoặc vùng click.
- Không chỉnh 131 polygon/texture/catalog; vẫn bỏ trống TM-03 và TM-13. Không sửa các chỗ polygon chồng lấn nhẹ đã được người thiết kế chấp nhận.

## Tệp đã thay

1. `Assets/Script/WorldMapPanelUI.cs`: thêm viewport bị cắt, camera zoom & pan, nút phóng to/thu nhỏ, tâm map mặc định và xây mask sương mù mềm.
2. `Assets/Script/UIManager.cs`: điều phối input chuột trong Unity Editor/đường non-native, bảo toàn quyền ưu tiên các nút UI.
3. `Assets/Script/TransparentWindow.cs`: native Windows giữ chuột trái để kéo map (không kéo cửa sổ); lăn chuột để zoom; bỏ thao tác nếu mất vị trí chuột hợp lệ.
4. `Tests~/UnityStub.cs`: bổ sung phép toán và InputSystem stub phục vụ test dựa trên mã nguồn.

## Checklist Play Mode trên Windows (chưa chạy tại đây)

- Mở map lần đầu sau khi đứng ở một ô có polygon: ô đó vào giữa vùng nhìn và rộng khoảng 40% chiều rộng vùng xem (nếu chưa chạm giới hạn 16×).
- Giữ chuột trái và rê: **chỉ di chuyển bản đồ**, không di chuyển cửa sổ Windows; thả chuột không đổi map đã chọn. Click đơn ở ô hợp lệ vẫn chọn; nút **Tới ô đã chọn** mới di chuyển nhân vật.
- Lăn bánh xe lên/xuống: zoom quanh vị trí con trỏ, không nhảy vị trí bất thường; nút +/− và **Về vị trí** hoạt động; không thể pan vượt ra khỏi cạnh bản đồ.
- Đổi preset kích thước cửa sổ khi map đang mở, thử zoom/pan lại: artwork, sương mù, nét viền và hitbox vẫn cùng vị trí. Thử ở 800×450 và độ phân giải màn hình lớn.
- Với map có nhiều ô đã đi qua: biên giới khám phá sáng rõ, mép fog chuyển mềm khoảng 20px ngoài polygon, ô chưa đi không bị lộ hoàn toàn hoặc được đánh dấu visited.
- Trên TM-03/TM-13 thiếu polygon: camera định vị theo seed, không phát sinh click giả. Kiểm thử cạnh sát UI (nút đóng, nút zoom, danh sách đường đi) để bảo đảm nút nhận sự kiện trước nền map.

**Giới hạn xác minh:** chưa thực hiện compile Unity, Unity Play Mode/Windows executable hay nghe/chạm chuột trực tiếp trong môi trường này; yêu cầu kiểm thử trên máy phát triển trước khi merge.

## Sửa lỗi ngày 16/10/2026 — Fog và chuyển map

- Bỏ hiển thị `WorldMapRevealedGraphic`: các polygon đã khám phá nay được hiển thị qua **một mặt nạ alpha duy nhất** của fog. Tránh vệt răng cưa/đường nối vì render lớp ảnh gốc lần hai theo từng đa giác.
- Feather ngoài viền tăng từ 20 lên **36 px nguồn**, texture alpha fog nâng lên 384×288 và áp dụng giá trị alpha thấp nhất giữa các ô đã khám phá để không tạo vạch mờ ở đường phân cách bên trong. **Không thay đổi hình học polygon, điều kiện click, hay dữ liệu visited.**
- Giữ **hero phụ B2 đang sống** khi di chuyển thủ công sang map kề: chụp danh sách trước khi chiến đấu reset, sau đó spawn lại và đăng ký từng hero vào `CombatManager`, bảo toàn chế độ tấn công và camera của hero chính. **Hero phụ vẫn là bản thử nghiệm; từ bản sửa Save/Load này chỉ lưu số lượng và chế độ tấn công, chưa lưu chỉ số/trang bị riêng** theo kiến trúc cũ.
- Sửa trường hợp **LoadAndContinue ngay trong gameplay** để tắt hoàn toàn các lớp cinematic nằm ngoài `preGameUI` (đặc biệt màn đen `Opening Black Curtain`). Khi quay về menu lần tiếp theo, `ResetForMenu` vẫn có thể dựng lại hoạt cảnh bình thường.
- Thêm hồi quy nguồn cho fog hợp nhất vùng đã mở, đồng đội B2 qua `SetupHeroInfo` và hoạt cảnh đen khi vào gameplay không qua transition.

**Kiểm tra thủ công cần làm:** chuyển map khi có 2–3 hero đang sống; chơi đủ 5 wave để mở map; lưu tay → trở về màn hình chính → Tiếp tục → hệ thống → Tải bản lưu; thử ngược lại việc quay về menu sau khi load; zoom/pan nhiều map đã mở để kiểm tra các đường ranh giới có còn hiện vệt hay không. Chưa chạy Unity Play Mode trong môi trường này.

## Chọn map — glow vàng trên sương mù (16/10/2026)

- Click vào polygon đang được phép chọn sẽ cập nhật `selectedMapId` **và** tạo hiệu ứng vàng mềm cho đúng vùng polygon đó, không cần click vào danh sách và không tự di chuyển. Click sang vùng khác sẽ thay hiệu ứng cũ, không để lại dấu trên map trước.
- Lớp `WorldMapSelectedCellGlow` là một `RawImage` độc lập, hiển thị **phía trên sương mù, dưới tên quốc gia**, cùng hệ tọa độ với ảnh map, nên pan/zoom vẫn bám chính xác vào vùng chọn. `raycastTarget=false`.
- Texture vùng sáng 384×288: phủ vàng nhạt phần trong, sáng mạnh hơn gần mép bên trong, quầng vàng mờ nhẹ trong 14 pixel ảnh nguồn phía ngoài. Không vẽ stroke sắc; không thay đổi `WorldMapPolygons.json`, `worldMapVisited`, fog mask hay điều kiện unlock.
- Khi mở map, ID mặc định vẫn là map hiện tại cho chức năng di chuyển, **nhưng chưa hiện glow**; glow chỉ xuất hiện sau click thực tế. Khi đóng map, tắt glow. Map thiếu polygon hoặc ảnh không khớp kích thước sẽ không tự phát sinh hình dạng highlight.
- Hàm `BuildSelectionGlowPixels(..., Color32 tint)` chấp nhận màu tùy chọn. **Hiện chỉ dùng màu vàng** (`255,190,50`); màu đỏ sẽ nối vào cơ chế đánh giá vượt cảnh giới khi được chốt, không suy đoán từ Level hiện tại.
- Kiểm thử nguồn: kiểm tra bên trong, mép sáng, quầng ngoài, ngoài vùng chọn không tô, màu đỏ có thể đổi độc lập và polygon không hợp lệ không tạo vùng sáng. **Chưa chạy Unity Play Mode trên máy phát triển**.

Checklist sau khi kéo code:
1. Đứng ở map vừa clear đủ wave, mở bản đồ: chưa có glow lúc mở.
2. Click map kề đã mở: chỉ polygon đó sáng vàng; click map khác đổi vùng sáng; không tự chuyển map.
3. Thử pan/zoom lên 400–1600%: glow bám vào polygon và không cản thao tác chuột.
4. Đóng mở lại bản đồ: ánh vàng cũ biến mất. Vùng fog chưa khám phá không bị mở khóa bởi việc chọn.

## B2 — sửa đồng đội biến mất khi tải bản lưu (16/10/2026)

### Nguyên nhân
- Trong bản cũ, chỉ có Hero chính nằm trong `EntityDataSO` được chuyển thành JSON; `ClearTestCompanions()` luôn chạy khi khởi động/tải lại. Vì vậy Save 1, Save 2, AutoSave đều **không lưu đội hình B2**.

### Phạm vi sửa
- `EntityDataSO.b2PartyVersion` và `b2CompanionModes`: lưu 0–2 kiểu tấn công của đồng đội B2 trong cùng JSON với Hero chính.
- `GameManager.CaptureB2Roster`: chụp roster lúc gọi/thu hồi Hero, lưu thủ công, autosave, về menu, thoát game. Chỉ thay đổi roster cũng đặt `isDirty` để autosave ghi được.
- `GameManager.RestoreB2Roster`: dựng lại các đối tượng đồng đội sau khi `CombatManager.SetupHeroInfo` và `heroController.SpawnHero` khởi tạo Hero chính. Được dùng trong `DeployHero` (tiếp tục, tải thủ công) và `OnRetryClicked`.
- `SaveManager.LoadGame`: xóa dữ liệu roster mẫu trước khi `FromJsonOverwrite`; bản lưu **cũ không có** các trường này sẽ có 0 B2 Hero, không đoán số lượng từ lưu cũ. Mã vai trò sai hoặc quá 2 người được chuẩn hóa.
- Thông tin **chưa** được lưu cho từng đồng đội: chỉ số/tăng cấp, trang bị, máu còn lại, danh tính riêng, vị trí hay lịch sử chiến đấu. Đồng đội được tạo lại bằng cơ chế B2 có sẵn, và nhận đầy máu như một lần triển khai mới. Không thay đổi cơ chế 131 polygon, sương mù hay điều kiện chuyển map.

### Test cần thực hiện trên Unity
1. Vào game, gọi 2 Hero phụ (tổng 3), **tạo một bản lưu thủ công mới** bằng Save 1, tiếp tục chơi rồi load Save 1. Phải có lại 3 Hero, đúng kiểu tấn công của hai người.
2. Ghi Save 2 với chỉ 1 Hero phụ, sau đó load Save 1 rồi Save 2 để xác nhận số lượng của từng slot không bị lẫn.
3. Có 3 Hero, quay về màn hình chính, bấm Tiếp tục để kiểm tra AutoSave có lại đội hình.
4. Thử Play → thoát ứng dụng → chạy lại → Tiếp tục; thử thua → Chơi lại.
5. Tạo nhân vật mới để xác nhận không tự nhận đội hình của nhân vật trước.
6. Tải một file save được tạo **trước bản sửa**: hero chính phải khôi phục bình thường, nhưng không thể lấy lại số Hero phụ do dữ liệu trước đó chưa ghi. Gọi lại Hero phụ và **lưu đè** slot cần sử dụng.
7. Unity Console không có lỗi biên dịch/runtime và `CombatManager.RegisteredHeroCount` bằng tổng số Hero đang hiện.

Đã bổ sung ca kiểm thử hồi quy mã nguồn cho cả ba loại slot, tương thích bản lưu cũ và tái tạo Hero B2 sau `LoadAndContinue`. **Chưa chạy Play Mode**.
