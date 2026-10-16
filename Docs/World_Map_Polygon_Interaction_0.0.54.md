# World Map .54 — vùng lục giác tương tác (đợt triển khai 1)

**Trạng thái dữ liệu 16/10/2026:** đã lưu **133 vùng nhấn 6 đỉnh ứng viên (0/133 được duyệt)** lấy vị trí mã từ ảnh có nhãn `imagegen.png` kích thước **1448×1086**. Cả 133 đường viền mới là đa giác nội suy quanh tâm map, **không khớp chính xác đường trắng** và bắt buộc hiệu chỉnh/duyệt từng ô bằng công cụ. Sáu tâm có nhãn ảnh trùng hoặc thiếu được **suy luận**: `TL-01`, `TL-17`, `TL-19`, `HT-13`, `TM-03`, `TM-13`. Ảnh thứ hai `irregular_hexes_of_the_fantasy_realms.png` kích thước **1536×1024** có hệ chia ô khác: chỉ là nguồn tham chiếu, chưa dùng chung tọa độ. Không coi đồ thị thử nghiệm là biên giới thật.

## Các tệp và vai trò

- `Assets/Script/WorldMapPolygons.cs`: mỗi map ID ban đầu chứa 6 đỉnh `Vector2` (runtime hỗ trợ 3–24 đỉnh) chuẩn hóa theo nền ảnh, từ `(0,0)` góc trái dưới đến `(1,1)` góc phải trên. Kiểm tra điểm trong đa giác, đa giác lồi/lõm đơn, phát hiện hình tự cắt, tam giác hóa.
- `Assets/Editor/WorldMapPolygonEditorWindow.cs`: công cụ vẽ và kéo đỉnh trực tiếp trên ảnh map trong Unity Editor, cho phép trùng khít các đỉnh với ô bên cạnh (snap), chèn góc bằng Shift+click, xóa góc bằng chuột phải, chuyển nhanh ô chưa duyệt và hiện mã ô. Không xuất hoặc sửa ảnh.
- `Assets/Resources/WorldMapPolygons.json`: hiện có **133 đa giác 6 đỉnh chưa duyệt**, gắn nhãn nguồn/độ tin cậy; khi bấm **Lưu các đỉnh** và **Duyệt map này** trong Editor sẽ cập nhật tại đây và có thể commit. Không đánh đồng đã có hitbox với đã nghiệm thu viền.
- `Assets/Resources/WorldMapPolygonSeeds.txt`: tâm map 133 mã theo pixel trên ảnh có nhãn, được dùng làm vị trí tham khảo và để lập đường kề thử nghiệm theo khoảng cách địa lý; không phải đo đạc biên giới.
- `Assets/Script/WorldMapPolygonGraphics.cs`: hiển thị lại **chính texture ảnh bản đồ** chỉ trong các lục giác đã khám phá; vẽ viền ô tiếp cận được/ô đã chọn, không cần tạo sprite cho từng ô.
- `Assets/Script/WorldMapPanelUI.cs` + `Assets/Script/UIManager.cs`: ô lục giác nhận click theo 6 đỉnh, không phải bounding rectangle. Click ô thì **chỉ chọn**, bấm **Tới ô đã chọn** hoặc nút danh sách map kề mới di chuyển. Các nút phía trên ảnh có ưu tiên click hơn vùng đa giác.
- `Assets/Resources/WorldMapCatalog.txt`: 133 mã, tên, địa hình, quái, Nhân tộc, loại map và điểm sinh. Cơ chế chơi và các map Boss vẫn theo quy định thế giới hiện tại.

## Các bước gán viền từ đúng ảnh gốc

1. Giải nén gói `WorldMap_0.0.54_Unity_Import.zip` vào project. Tạm thời **`WorldMapArtwork.png` phải là ảnh có mã 1448×1086** để khớp hệ tọa độ bản nháp. Ảnh lục giác bất quy tắc được giữ ở `WorldMapIrregularReference.png`; không đổi ảnh này vào chỗ `WorldMapArtwork.png` nếu chưa vẽ lại toàn bộ tọa độ. Import Texture Type đặt **Sprite (2D and UI)** và Sprite Mode **Single**. Khung runtime giữ đúng tỷ lệ ảnh.
2. Trong Unity, mở **Tools > Bach Luyen Tien Do > World Map Polygon Editor (.54)**. Kéo texture bản đồ vào **Ảnh map gốc**.
3. Chọn mã map và so đường vàng hiện có với đường trắng trên **ảnh cùng nguồn**. Kéo các đỉnh để căn lại, dùng Shift+click sát cạnh để thêm đỉnh; hoặc chọn **Vẽ lại map này** rồi click 3–24 góc cùng một chiều và **Chốt viền**. Đường xanh là các ô đã ghi, **không có nghĩa đã được duyệt**. Bật Dính đỉnh ô kề để khớp góc chung.
4. Kéo chấm vàng để tinh chỉnh đỉnh; bật **Dính đỉnh ô kề** để hai map dùng chính xác cùng tọa độ ở góc chung. Map lõm hợp lệ, nhưng đa giác tự cắt sẽ bị chặn.
5. Nhấn **Duyệt map này** khi đã căn chính xác, sau đó **Lưu các đỉnh**. Unity hiển thị bộ đếm **đã ghi/đã duyệt** tách biệt. Tệp JSON được sử dụng để render sương mù và thử hitbox. Thử trong Play Mode: hoàn thành 5 wave, mở nút Bản đồ, click vào ô kề trên ảnh để chọn, sau đó nhấn **Tới ô đã chọn** để đi.

## Hai trạng thái độc lập

- **Đã vượt qua map**: mở quyền đi qua các liên kết kề *tạm*; **không** chuyển map và **không** xóa mây mù ô chưa đến.
- **Đã đến/đã khám phá**: đúng khu vực đa giác đó được lộ ảnh; những vùng chưa được gán polygon **vẫn bị mây mù che kín**.
- Tên quốc gia, nếu đã nhập vào `WorldMapCountries.txt`, được vẽ phía trên fog; **không dựng đường biên quốc gia** trong lớp UI. Hiện tệp quốc gia là mẫu rỗng, chờ tên/tọa độ từ người thiết kế.

## Hạn chế còn mở

- **Hình học:** có **133/133 vùng ứng viên nhưng 0/133 vùng đã duyệt**. Tâm khớp tương đối với ảnh có mã; 6 góc được nội suy hình học, **không bảo đảm trùng đường trắng**. Cần đối chiếu và tinh chỉnh từng ID. Có 6 vị trí bị suy luận, xem danh sách đầu tài liệu.
- **Đường kề:** hiện `WorldMapCatalog` ưu tiên kết nối thử nghiệm theo **bốn tâm gần nhất** và nối các đại khu có tiếp giáp, không phải từ cạnh chung đã duyệt. Cần chuyển sang đường kề suy ra từ **các cạnh chung được duyệt** sau khi căn viền; không coi đồ thị thử nghiệm là hình học thế giới đã chốt.
- **Ảnh nguồn nhị phân:** do ảnh chưa được đưa lên GitHub trực tiếp, đã đóng gói cả hai PNG gốc trong `WorldMap_0.0.54_Unity_Import.zip` (đồng bộ trong Library `IdleGame/0.0.54/Drafts`). Cần giải nén vào project; nếu thiếu Sprite, các vùng đa giác không thể thử đúng. Game chặn click theo đa giác nếu kích thước Sprite không khớp bộ tọa độ.
- **Kiểm thử:** đã thêm các ca hồi quy hình học vào `Tests~/MotionRegression.cs`. Chưa chạy Unity Play Mode/Windows hoặc `Tests~/run.sh` trong môi trường trợ lý (máy hiện không có `mcs`/`mono`). Kiểm tra Inspector, compile và click thực tế khi mở Unity trên máy phát triển.

**Không sửa gameplay của bản .53/`main`.** Hệ thống này nằm trên nhánh phát triển .54, giữ Save cũ chạy theo chế độ bản đồ cũ.

## Cập nhật công cụ và kiểm toán ngày 16/10/2026

- **Hình học:** `WorldMapPolygons.ValidPolygon` hỗ trợ 3–24 đỉnh; `ValidSixVertices` được giữ để tương thích với code/test cũ. Hitbox và mesh cho sương mù/viền đều dùng validator mới. 133 ứng viên cũ không bị chỉnh tọa độ hay tự duyệt.
- **Editor:** kéo chấm vàng để sửa vị trí; **Shift+click gần cạnh** để thêm đỉnh; **chuột phải gần đỉnh** để xóa; **Vẽ lại map này** → click 3–24 góc → **Chốt viền**; **Ô chưa duyệt kế** để duyệt nhanh và **Hiện mã ô** để đối chiếu. Đổi mã map khi đang vẽ sẽ tự dừng chế độ vẽ, không mất các đỉnh cũ.
- **Kiểm toán sơ bộ bằng Python/Shapely:** 133/133 đa giác ứng viên hợp lệ, 133 mã duy nhất, 0 cặp có diện tích chồng lấn. Tổng diện tích vùng nhấn ứng viên bằng khoảng 51,82% *toàn khung ảnh* (gồm biển, khung và chú giải); con số này **không chứng minh đường viền đã căn chính xác**. Đã duyệt vẫn là 0/133.
- **Hai hình tham chiếu:** JSON đang định vị trên **ảnh có mã 1448×1086**. Ảnh bất quy tắc **1536×1024** có bố cục lục giác khác, chưa được dùng thay thế trực tiếp. Muốn sử dụng ảnh thứ hai trong giao diện gameplay phải có một bộ tọa độ và bảng ánh xạ ID riêng; không đổi PNG và dùng lại polygon cũ.
- **Kiểm thử:** đã thêm ca kiểm tra 3/7 đỉnh, đa giác tự cắt, tam giác hóa; **chưa chạy được Unity Play Mode hoặc Mono build trong môi trường này**, nên chưa coi là đã pass trên Unity. Giữ toàn bộ phần này trên nhánh `feature/0.0.54`, không merge vào `main`.
