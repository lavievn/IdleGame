# Bản thử nghiệm 0.0.52 — Kho đồ và một ô trang bị

Nhánh `feature/0.0.52-inventory-equipment` kế thừa trạng thái kiểm thử của `feature/0.0.51-combat-stats`; không sửa `main`.

## Chức năng trong đợt đầu

- **Kho đồ theo phiên chơi**: `EntityDataSO.inventory` là danh sách vật phẩm có ID/ số lượng, có thể dùng về sau cho nhiều Hero trong cùng phiên. Hiện chỉ Hero chính có ô trang bị; hai đồng đội thử nghiệm B2 không được lưu và chưa dùng trang bị.
- **Ô trang bị duy nhất**: một ô **Hộ phù**. Vật phẩm mẫu:
  - **Hộ phù Sinh Mệnh** (`sample:health_charm`) tăng 100 HP tối đa.
  - **Hộ phù Công Kích** (`sample:attack_charm`) tăng 20 ATK.
- **Mỗi nhân vật mới** bắt đầu với 1 món mỗi loại ở kho. Save 0.0.50/0.0.51 không tự nhận món nào; mở **Kho đồ → Nhận 2 vật phẩm thử (1 lần)** để nhận.
- Trang bị lấy 1 món khỏi kho; đổi món trả món cũ về kho; tháo trả món về kho. Không có mua bán, rơi đồ, cường hóa hay giới hạn số ô kho ở đợt đầu.
- **Không cộng thẳng** vào `baseHealth`, `addedHealth`, `baseDamage`, `addedDamage`. Trang bị tạo **một** `CombatStatModifier` có `sourceId = equipment:primary-trinket`, tận dụng `CombatStats` phiên bản 0.0.51.
- `CombatManager.RefreshRegisteredHeroStats` tính lại HP tối đa, giữ phần trăm HP khi trang bị và tháo, cập nhật HUD, reset đòn đánh đang tích.
- **Save**: danh sách kho, ID đang trang bị và cờ đã nhận quà thử được ghi vào cùng bản lưu Hero. `SaveManager.LoadGame` xóa giá trị template, nạp JSON, sau đó `EquipmentInventory.Normalize` khôi phục đúng một Modifier hoặc xóa Modifier trang bị cũ. Bản lưu cũ sẽ có kho trống, ô trang bị trống.
- AutoSave tiếp tục dùng `isDirty` từ thao tác kho/trang bị. Hai ô lưu tay vẫn chỉ thay đổi khi người chơi chọn lưu đè theo quy tắc 0.0.51.

## Các tệp chính

- `Assets/Script/EquipmentInventory.cs`: định nghĩa catalog, kho, thao tác equip/swap/unequip/normalize.
- `Assets/Script/InventoryPanelUI.cs`: dựng cửa sổ Kho đồ tự động từ UI đang có.
- `Assets/DataScript/EntityDataSO.cs`, `Assets/Script/SaveManager.cs`, `Assets/Script/GameManager.cs`, `Assets/Script/UIManager.cs`: lưu/tải, điều khiển, hiển thị.
- `Tests~/InventoryEquipmentRegression.cs`: bài kiểm thử logic, chỉ số, HP, save tay và tương thích save cũ; `Tests~/UnityStub.cs` bổ sung mô phỏng JSON cho danh sách InventoryStack và cờ bool.
- `ProjectSettings/ProjectSettings.asset`: `bundleVersion: 0.0.52`.

## Kiểm thử Unity Play Mode đề nghị

**Trước khi thử, hãy sao lưu thư mục `Application.persistentDataPath/Saves` có dữ liệu quan trọng.**

1. Pull nhánh 0.0.52 và mở Unity, kiểm tra Console **không có lỗi biên dịch**.
2. **Chơi mới** → bấm **Kho đồ** trên HUD: 2 hộ phù, mỗi món số lượng 1; ô mặc trống.
3. **Trang bị: HP +100**. Kiểm tra Max HP tăng đúng 100 và tỷ lệ HP hiện tại không tự hồi đầy nếu đang mất máu.
4. **Trang bị: ATK +20**. Hộ phù Sinh Mệnh trở lại kho, HP trở về baseline và ATK tăng đúng 20, kể cả sau nhiều lượt đổi.
5. **Tháo về kho**. ATK trở về baseline, Hộ phù Công Kích quay lại kho.
6. Trang bị một món, **Menu → Lưu thủ công → Ô 1**; đổi món, **Tải Ô 1**. Kiểm tra cả vật phẩm trong kho, món mặc và chỉ số quay lại đúng như lúc lưu.
7. Lưu ô 2, **Đặt lại nhân vật**: AutoSave và kho của phiên mới về ban đầu; ô 1/2 vẫn giữ nguyên, tải lại bình thường.
8. Dùng **save 0.0.51 có sẵn** (không có trường inventory), mở Kho đồ thấy trống; nút **Nhận 2 vật phẩm thử (1 lần)** cho 2 vật phẩm, bấm lần hai không nhân đôi. Lưu rồi tải: món và trạng thái chỉ nhận một lần được bảo toàn.
9. Kiểm tra cửa sổ Kho đồ có thể bấm bằng chuột trong cả **Unity Editor** và bản build Windows, kể cả khi thay kích thước cửa sổ.

## Phạm vi chưa thực hiện

Chưa có hệ thống vật phẩm ngẫu nhiên, rơi đồ, phân loại trang bị, nâng cấp, điều kiện sử dụng, chuyển đồ giữa nhiều Hero cố định hoặc trang bị trên nhân vật B2. Chưa thể xác nhận biên dịch/chạy Play Mode trực tiếp trong môi trường thực thi này; cần phản hồi kiểm thử từ Unity.

## Hotfix — Khôi phục toàn bộ tiến trình, không chỉ trang bị

Báo lỗi từ Unity: lưu khi nhân vật cấp thấp và đang trang bị vật phẩm, sau đó lên cấp/đổi chỉ số và tải lại — trang bị khôi phục nhưng cấp độ/sức mạnh vẫn là trạng thái mới. Đây là sai nghiêm trọng đối với checkpoint/cốt truyện. **HP khi tải có thể chủ ý hồi đầy**, vì `HeroCombatState.BeginLife` khởi tạo lại trạng thái chiến đấu, nhưng tiến trình bền vững phải quay về snapshot đã lưu.

Đã thêm biện pháp chống tiếp tục ở trạng thái pha trộn:

- `SaveManager.LoadGame` kiểm tra các trường tiến trình có mặt trong JSON (cấp, EXP, điểm chỉ số chưa dùng, chỉ số cộng thêm, số map/số đợt) **khớp dữ liệu vừa giải mã** trước khi chấp nhận tải. Nếu chỉ phục hồi một phần, thao tác tải trả về `false`, không thay thế nhân vật hiện tại bằng một Hero nửa cũ nửa mới.
- `GameManager.LoadAndContinue` và đường **Tiếp tục** từ menu đầu xác minh cấp độ nạp bằng cấp độ trong tệp. Lúc lưu và tải, Unity Console ghi cụ thể ô/cấp độ/EXP/map, giúp xác định tệp có lưu đúng snapshot không.
- Khi mở **Menu → Tải bản lưu**, ba nút **Ô 1, Ô 2, Tự động** hiển thị **cấp độ và map đọc từ tệp thực tế**. Quan trọng: AutoSave có thể là cấp 10 trong khi ô lưu tay là cấp 1. Chỉ bấm Ô 1 mới tải checkpoint Ô 1; bấm Tự động sẽ tải snapshot mới hơn nếu AutoSave đã cập nhật.
- Thêm `Tests~/SaveRollbackRegression.cs`: lưu thủ công ở cấp 1 với hộ phù HP, lên cấp và tăng ATK, đổi đồ, sau đó tải đúng Ô 1; xác nhận cấp/EXP/điểm cộng/map/ATK/Hộ phù đều trở về thời điểm lưu, `CombatManager` trỏ tới Hero mới nạp, HP có thể hồi đầy. Test riêng xác minh AutoSave cấp 10 không thể lẫn vào thao tác tải thủ công Ô 1 cấp 1.

**Lưu ý về chẩn đoán:** Chưa có tệp `ManualSave1.json` tại thời điểm xảy ra lỗi nên chưa thể khẳng định lỗi do nhầm ô tải, ghi đè snapshot hay lỗi Unity-runtime khác. Bản sửa tăng tính quan sát và chặn nạp một phần, không giả định nguyên nhân khi chưa có bằng chứng. Nếu vẫn tái hiện, kiểm tra nhãn cấp trên ba nút Tải và nhật ký `[SaveLoad]` trong Unity Console; gửi bản sao tệp `ManualSave1.json` (sao lưu trước).

## Giai đoạn 2 của 0.0.52 — Nền nhiều vị trí trang bị (chưa chạy Unity)

Sau khi kiểm thử Save/Load cấp/EXP/map ổn định, nền được mở rộng từ **1 ô Hộ phù** sang **8 ô trang bị**. Tài liệu đầy đủ: [Equipment_0.0.52_Schema.md](Equipment_0.0.52_Schema.md).

- Thêm `EquipmentFoundation.cs` và `EquipmentRuntime.cs`, với lớp `EquipmentInstance` có mã độc lập, cấp, phẩm chất, rèn, chỉ số chính/phụ và ô đang trang bị.
- Đã chốt phẩm chất **Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên**. Các xác suất 0,005% (Thiên) và 0,001% (Tiên) chỉ là mốc cho bảng rơi đồ tương lai, chưa có rơi đồ.
- Thêm `ProfessionProgress`, `LearnedProfessionRecipe`, `LearnedSupportSkill`, `CraftingRecipeData`, `RefinementRuleData` làm **dữ liệu nền**, chưa chế tạo hay tiêu vàng thật.
- `SaveManager` reset phần dữ liệu mới khi đọc save trước 0.0.52 và xây lại Modifier từ đúng các trang bị đang mặc, không sinh lại giá trị ngẫu nhiên.
- Sửa kho cộng dồn cũ để nguyên liệu ID mới không bị xóa bởi bước normalize.
- UI Kho đồ có thử **Nhận Thiết Kiếm → Trang bị Thiết Kiếm → Tháo vũ khí** và bảng 8 ô. Hai hộ phù hiện có không đổi.
- Kiểm thử hồi quy tại `Tests~/MultiSlotEquipmentRegression.cs`: tất cả 8 ô, 2 nhẫn độc lập, dữ liệu nghề, vật phẩm Thiên có 1 dòng Hỏa, ràng buộc kiểu trang bị, lưu/tải cá thể trang bị + vàng + cấp nghề, giữ chỉ số khác như cũ.

**Lưu ý:** Chưa có lần biên dịch hoặc chạy kiểm thử trực tiếp trong Unity Editor. Hãy sao lưu dữ liệu Saves trước khi Pull và Play Mode.

## Bổ sung Inspector — Gắn asset nghệ thuật không cần sửa mã

- Thêm `Assets/Script/InventoryArtTheme.cs` (ScriptableObject) và `Assets/InventoryArtTheme_Default.asset` (4 ô map mẫu) cùng đầy đủ Unity `.meta`.
- **UIManager Inspector** có trường `Inventory Art Theme`. Khi chạy, `InventoryPanelUI` sinh từ mã sẽ đọc theme này và gán Sprite trực tiếp cho khung inventory, ô trang bị, ô túi, trạng thái được chọn, nút, chi tiết món, khung chỉ số, banner map, trang trí treo, splash Hero và ảnh nền từng map.
- **EnvironmentManager Inspector** có `customDistantMountains` / `customFarTrees` để thay riêng hai lớp parallax Ground, giữ nguyên ảnh preview nếu để trống.
- Hệ thống chỉ gán artwork, không thay cấp Hero, các Modifier, SaveManager, tọa độ chiến đấu hay logic rơi đồ. Có thể thay toàn bộ ảnh về sau bằng cách chọn Sprite khác.
- Các ảnh trống không bắt buộc: panel dùng lại nền cũ; artwork chỉ xuất hiện khi có Sprite hợp lệ.
- Các trường `scrollbarTrack` / `scrollbarHandle` là **chỗ dự phòng**, chưa kết nối ScrollRect (kho vẫn phân trang).
- Chi tiết gán ảnh: [InventoryArtTheme_0.0.52.md](InventoryArtTheme_0.0.52.md). Chưa có kiểm thử Unity Play Mode và Windows Build thực tế; cần xác nhận trên máy phát triển.
