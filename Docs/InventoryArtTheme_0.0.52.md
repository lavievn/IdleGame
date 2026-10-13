# 0.0.52 — Tự thay Sprite bằng Unity Inspector

## Hai nơi chọn ảnh (không sửa mã, không thay Scene hierarchy khi đang chơi)

1. Trong **Project**, chọn `Assets/InventoryArtTheme_Default.asset`. Đây là **ScriptableObject** mẫu được commit cùng mã và có 4 dòng map (mapNumber 1–4). Có thể nhân đôi asset bằng Ctrl+D để tạo các bộ giao diện khác nhau; cũng có thể tạo mới bằng **Assets → Create → Tu Tien → UI → Inventory Art Theme**.
2. Trong **Hierarchy**, chọn GameObject đang chứa **UIManager** của scene chính. Trong Inspector kéo asset `InventoryArtTheme_Default` vào ô **0.0.52 - ASSET KHO DO / TU TIEN → Inventory Art Theme**. Đây là tham chiếu bền vững của scene; không cần gắn trực tiếp ảnh vào `InventoryPanelUI` vì giao diện này chỉ được sinh lúc Play.
3. Chọn asset theme trong Project, kéo từng ảnh vào các trường Sprite. Ảnh có thể ở bất kỳ thư mục Assets nào. File PNG nền trong suốt là lựa chọn phù hợp với các khung.
4. Mỗi file ảnh mới: trong Inspector của **texture**, chọn **Texture Type: Sprite (2D and UI)** rồi Apply. Nếu dùng `Sliced Resizable Panels`, phải khai báo **Border** trong Sprite Editor theo 9-slice; **không bật** lựa chọn này cho ảnh minh họa/splash.
5. Bấm **Play → Kho đồ** để kiểm tra. Có thể đổi ảnh ngay lúc đang Play và chọn menu ba chấm của UIManager → **0.0.52 - Ap dung anh Inventory (Play Mode)**; hoặc đóng/mở lại kho để làm mới.
6. Sau khi ổn định, lưu Scene và build lại Windows. **Inspector không sửa trực tiếp ảnh của tệp .exe đã build trước đó**.

## Bảng ghép vị trí và ảnh

| Vùng giao diện trong ảnh mẫu | Trường trong `InventoryArtTheme` | Hành vi |
| --- | --- | --- |
| Toàn bộ khung kho bên phải | `inventoryWindowFrame` | Gắn Image vào root, nằm phía sau các nút |
| Dải tiêu đề trên cùng | `inventoryHeader` | Nằm phía sau tên kho |
| Phần 8 ô trang bị | `equipmentPanel` | Nền panel trang bị |
| Nền lưới túi đồ | `bagPanel` | Nền panel túi |
| Khung chi tiết vật phẩm | `itemDetailsPanel` | Khung phía sau chữ chỉ số/vật phẩm |
| Khung tóm tắt chỉ số nhỏ | `miniStatsPanel` | Khung phía sau tóm tắt HP/ATK |
| Một ô trống hoặc có item | `bagSlot` | Lặp lại cho mọi ô túi |
| Một ô đang chọn | `selectedBagSlot` | Ưu tiên hơn bagSlot khi chọn |
| Ô vũ khí/mũ/giáp/pháp bảo/nhẫn | `equipmentSlot` | Dùng chung cho 8 ô |
| Các tab thường và đang chọn | `normalTab`, `selectedTab` | Đổi theo tab đang chọn |
| Nút đóng, lật trang, trang bị/tháo | `actionButton` | Dùng chung |
| Nền khu vực trên phần gameplay | `heroAreaBackground` | Nền chung khung phía trái |
| Tranh map mặc định | `defaultMapSplash` | Dùng khi map chưa được cấu hình |
| Splash Hero mặc định | `defaultHeroSplash` | Có thể để trống, thay bằng Spine sau này |
| Khung lồng splash Hero | `heroSplashFrame` | Lớp trên ảnh Hero |
| Bảng tên map | `mapTitlePlate` | Khung dưới chữ khu vực |
| Dải/biển treo dọc | `hangingOrnament` | Trang trí dọc góc trên trái |
| Ảnh theo từng map | `mapArts` → `mapNumber`, `mapSplash`, `heroSplash`, `titlePlate`, `ornament` | Tự chọn theo **HeroData.mapNumber** khi mở kho |

**Lưu ý:** `scrollbarTrack` và `scrollbarHandle` hiện là trường *để dành*. UI hiện tại vẫn phân trang thay vì có ScrollRect kéo thật, vì vậy gán hai ảnh đó **chưa thay đổi giao diện**. Sẽ nối vào ScrollRect khi xử lý kho 50+ ô trong bước sau.

## Ground 2 / Ground 3: gắn ảnh trên EnvironmentManager

- Chọn **EnvironmentManager** trong Hierarchy → khu **0.0.52 - SPRITE NEN XA (GROUND 2 / GROUND 3)**.
- `customDistantMountains`: ảnh lớp **núi xa** (parallax sau cùng); `customFarTrees`: ảnh lớp **cây/cảnh trang trí phía trước núi xa**.
- Để trống thì game giữ bộ ảnh xem thử đang có. Hai lớp được nhân bản/lặp bởi `GroundPresentation` như cũ; chỉ thay ảnh, không sửa hệ tọa độ/map/combat. **Nên chọn ảnh có nền trong suốt** và quan sát crop khi chạy Play.
- Các Sprite này được lấy lúc `GroundPresentation.Install()`. Muốn xem thay đổi nên **dừng/chạy lại Play**. Không cần thay file TextAsset preview sẵn có.

## Chú ý khi dùng hình đã tạo

- Một **ảnh concept tổng hợp** có sẵn cả lưới và chữ thường không phù hợp với `inventoryWindowFrame` vì các nút thực sẽ đè lên lưới vẽ giả. Ưu tiên PNG khung trống/hoạ tiết tách lớp, riêng ô vật phẩm dùng ảnh vuông, riêng banner dùng ảnh ngang.
- `Sliced Resizable Panels` mặc định **OFF**. Tranh viền có hoa văn ở cả 4 góc không thích hợp bị kéo quá khác tỷ lệ gốc; chỉ dùng 9-slice khi được thiết kế có Border rõ.
- Mọi Sprite để trống đều quay về nền màu/ngăn khung nguyên bản, không bắt buộc phải điền hết để chạy.
- Ảnh và lựa chọn UI nằm trong asset/scene, **không được ghi vào ba ô save**. Thay theme không thay chỉ số, danh sách trang bị, cấp/EXP hoặc trạng thái combat.

## Bài kiểm thử tối thiểu

1. Không gán ảnh: giao diện kho cũ vẫn hiện đủ nút.
2. Gán chỉ `bagSlot`: mọi ô túi hiển thị đúng một ảnh mà vẫn chọn và đổi trang như trước.
3. Gán `equipmentSlot`, `inventoryWindowFrame`, `itemDetailsPanel`: kiểm tra chữ dễ đọc và bấm được các nút.
4. Gán splash vào `mapArts[0]` (mapNumber 1), đổi từ map 1 sang map 2 rồi mở lại Kho đồ: map 1 nhận ảnh riêng, map 2 về ảnh mặc định nếu chưa gán.
5. Gán `EnvironmentManager.customDistantMountains` và `customFarTrees`, chạy lại Play: hai lớp parallax có hình mới, gameplay không bị kéo lệch.
6. Thử lại Save/Load cùng các bản lưu cũ, không được có thay đổi dữ liệu.

**Trạng thái:** Đã cập nhật mã GitHub và kiểm tra cấu trúc tĩnh. Chưa biên dịch và chạy Play Mode thực tế trong Unity, nên cần xác nhận trên máy người phát triển trước khi gộp vào main.
