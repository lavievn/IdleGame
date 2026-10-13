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
