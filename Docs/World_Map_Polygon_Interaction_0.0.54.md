# World Map .54 — vùng lục giác tương tác (đợt triển khai 1)

**Trạng thái:** Mã hình học, bảng UI và công cụ chỉnh tọa độ đã được thêm trên `feature/0.0.54`. **Chưa gán 6 đỉnh thật cho 133 map**, bởi ảnh bản đồ hiện không chứa quan hệ mã map ↔ lục giác ở dạng dữ liệu có thể đối chiếu chắc chắn. Không coi các đường nối trên đồ thị thử nghiệm là biên giới trên ảnh.

## Các tệp và vai trò

- `Assets/Script/WorldMapPolygons.cs`: mỗi map ID chứa 6 đỉnh `Vector2` chuẩn hóa theo nền ảnh, từ `(0,0)` góc trái dưới đến `(1,1)` góc phải trên. Kiểm tra điểm trong đa giác, đa giác lồi/lõm đơn, phát hiện hình tự cắt, tam giác hóa.
- `Assets/Editor/WorldMapPolygonEditorWindow.cs`: công cụ vẽ và kéo đỉnh trực tiếp trên ảnh map trong Unity Editor, cho phép trùng khít các đỉnh với ô bên cạnh (snap). Không xuất hoặc sửa ảnh.
- `Assets/Resources/WorldMapPolygons.json`: dữ liệu tọa độ vẽ được, khởi tạo rỗng; mỗi khi bấm **Lưu các đỉnh** trong Editor sẽ ghi tại đây và có thể commit.
- `Assets/Script/WorldMapPolygonGraphics.cs`: hiển thị lại **chính texture ảnh bản đồ** chỉ trong các lục giác đã khám phá; vẽ viền ô tiếp cận được/ô đã chọn, không cần tạo sprite cho từng ô.
- `Assets/Script/WorldMapPanelUI.cs` + `Assets/Script/UIManager.cs`: ô lục giác nhận click theo 6 đỉnh, không phải bounding rectangle. Click ô thì **chỉ chọn**, bấm **Tới ô đã chọn** hoặc nút danh sách map kề mới di chuyển. Các nút phía trên ảnh có ưu tiên click hơn vùng đa giác.
- `Assets/Resources/WorldMapCatalog.txt`: 133 mã, tên, địa hình, quái, Nhân tộc, loại map và điểm sinh. Cơ chế chơi và các map Boss vẫn theo quy định thế giới hiện tại.

## Các bước gán viền từ đúng ảnh gốc

1. Đưa **chính phiên bản ảnh bản đồ đã duyệt** (có các lục giác không đều) vào `Assets/Resources/WorldMapArtwork.png`. Import Type đặt **Sprite (2D and UI)**; không cần chỉnh sửa/generate ảnh. Khung runtime giữ **đúng tỷ lệ ảnh** để hitbox và sprite khớp.
2. Trong Unity, mở **Tools > Bach Luyen Tien Do > World Map Polygon Editor (.54)**. Kéo texture bản đồ vào **Ảnh map gốc**.
3. Chọn một mã map có vị trí đã đối chiếu từ danh mục; click theo đường viền ảnh lần lượt **đúng 6 góc**, cùng chiều kim hoặc ngược chiều kim đồng hồ. Đường màu vàng là viền map đang sửa; viền xanh là các map đã lưu.
4. Kéo chấm vàng để tinh chỉnh đỉnh; bật **Dính đỉnh ô kề** để hai map dùng chính xác cùng tọa độ ở góc chung. Map lõm hợp lệ, nhưng đa giác tự cắt sẽ bị chặn.
5. Nhấn **Lưu các đỉnh**. Tệp JSON trở thành dữ liệu chuẩn để render sương mù và thử hitbox. Thử trong Play Mode: hoàn thành 5 wave, mở nút Bản đồ, click vào ô kề trên ảnh để chọn, sau đó nhấn **Tới ô đã chọn** để đi.

## Hai trạng thái độc lập

- **Đã vượt qua map**: mở quyền đi qua các liên kết kề *tạm*; **không** chuyển map và **không** xóa mây mù ô chưa đến.
- **Đã đến/đã khám phá**: đúng khu vực đa giác đó được lộ ảnh; những vùng chưa được gán polygon **vẫn bị mây mù che kín**.
- Tên quốc gia, nếu đã nhập vào `WorldMapCountries.txt`, được vẽ phía trên fog; **không dựng đường biên quốc gia** trong lớp UI. Hiện tệp quốc gia là mẫu rỗng, chờ tên/tọa độ từ người thiết kế.

## Hạn chế còn mở

- **Hình học:** chưa có bộ tọa độ của từng map; JSON hiện **0/133 vùng**. Cần đối chiếu mã map với từng lục giác trên ảnh trước khi vẽ. Không tự phân ID bằng vị trí trái → phải hoặc theo con số vì có thể gán sai.
- **Đường kề:** hiện `WorldMapCatalog` dùng kết nối thử nghiệm để kiểm tra mở khóa. Cần chuyển sang đường kề suy ra **cạnh chung của hai đa giác** sau khi có toàn bộ dữ liệu; không coi đồ thị thử nghiệm là hình học thế giới đã chốt.
- **Bản đồ gốc:** ảnh không được tự động đưa vào repository trong đợt này. Nếu Sprite chưa có trong Resources, ảnh map không xuất hiện; không thể kiểm thử căn chuẩn tại runtime.
- **Kiểm thử:** đã thêm các ca hồi quy hình học vào `Tests~/MotionRegression.cs`. Chưa chạy Unity Play Mode/Windows hoặc `Tests~/run.sh` trong môi trường trợ lý (máy hiện không có `mcs`/`mono`). Kiểm tra Inspector, compile và click thực tế khi mở Unity trên máy phát triển.

**Không sửa gameplay của bản .53/`main`.** Hệ thống này nằm trên nhánh phát triển .54, giữ Save cũ chạy theo chế độ bản đồ cũ.
