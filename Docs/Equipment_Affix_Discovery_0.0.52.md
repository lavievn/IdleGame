# 0.0.52 — Trang bị ngũ hành, tiềm năng và 3 trạng thái dòng phụ

Tài liệu này ghi lại cơ chế đã thống nhất, và giới hạn của phần mã đã commit trên nhánh `feature/0.0.52-equipment-stats`.

## Quy tắc chốt

1. **Số dòng phụ cố định cho từng cá thể vật phẩm** khi item được sinh ra. Việc thay đổi phẩm chất hiện tại, cấp trang bị hay rèn đúc không tự thêm/xóa/tạo lại dòng. Số lượng ban đầu gắn với cơ chế phẩm chất/tiềm năng; **chưa chốt bảng mỗi phẩm chất có bao nhiêu dòng**.
2. **Ngũ hành**: mỗi loại trang bị có thể có biến thể Kim, Mộc, Thủy, Hỏa, Thổ. Danh sách các hiệu ứng hợp lệ cho từng hệ và phạm vi sức mạnh sẽ là bảng dữ liệu riêng, **chưa chốt và chưa tạo ngẫu nhiên**. `EquipmentInstance.itemElement` hiện đã có chỗ lưu.
3. **Ba trạng thái**: `Hidden` (khóa/không thấy nội dung), `KnownLocked` (từng khám phá, khóa lại do không đạt yêu cầu), `Active` (đạt điều kiện và cộng chỉ số).
4. Mỗi dòng lưu `revealed`, `requiredItemLevel`, `requiredRefineLevel`; `AffixState()` tính trạng thái từ các biến này. Khi tụt cấp/cường hóa, `revealed` không tự chuyển về false.
5. Các dòng ẩn có dữ liệu chỉ số thực tế được lưu trong `extraStats`, nhưng **không được cộng Modifier hoặc hiển thị nội dung** cho người chơi. Sau khi đạt điều kiện, hành động thành công trong tương lai gọi `RevealEligibleAffixes()`; việc Load/Normalize không tự tiết lộ.
6. **Rèn thường** không được phá hủy trang bị; hỏng có thể làm mất nguyên liệu/tụt nhẹ tùy bảng cân bằng. **Luyện mạo hiểm** được phép vượt giới hạn tiềm năng tự nhiên (`naturalRefineLimit`) và có thể phá hủy vĩnh viễn. Phạm vi tài nguyên, tỷ lệ thành công, mức tụt cấp/chuyển phẩm và cách thức reroll **chưa chốt**.
7. **Tiềm năng ẩn khác phẩm chất hiện tại**: `potentialRarity` và `rarity` tách riêng. Chẳng hạn một pháp khí có thể đang Phổ thông nhưng mang tiềm năng Tiên.
8. **Lịch sử luyện chế**: `highestRefineLevel` không mất theo cấp hiện tại; `hasBeenRefined` là cờ để giao diện tương lai quyết định có hiển thị thanh ước lượng. `naturalRefineLimit=0` biểu thị item đời cũ/demo chưa được sinh giới hạn cá thể, **không có nghĩa giới hạn thực bằng 0**.

## Đã mã hóa

- `EquipmentFoundation.Version=3`, `AffixSchemaVersion=1`: các field mới để Unity JsonUtility lưu cùng snapshot Hero.
- `EquipmentFoundation.AffixState(item,line)`: chỉ đọc, suy ra 3 trạng thái; `RevealEligibleAffixes` dành cho **hành động nâng cấp thành công** tương lai.
- `EquipmentRuntime.Normalize` tự chuyển vật phẩm đời 0.0.52 (`affixSchemaVersion=0`) thành các dòng **đã khám phá và đang có hiệu lực**, không tạo lại chỉ số và không làm rơi trang bị đang mặc.
- `EquipmentRuntime.RebuildModifiers`: chỉ cộng dòng đang Active; `EquipmentStatPreview`: so sánh chỉ tính dòng Active.
- `InventoryPanelUI`: dòng Hidden hiển thị `???`; KnownLocked hiện chỉ số nhưng ghi `[Khóa]`; Active hiện bình thường. Chỉ thay **cách hiển thị văn bản chi tiết**, không đụng bố cục giao diện.
- `RefinementMethod` / `RefinementRuleData`: cấu hình hai hướng rèn, `IsSafeRefinementRule` từ chối quy tắc rèn **thường** có hủy item hoặc vượt tiềm năng. **Chưa tồn tại hàm thực thi luyện chế**.
- `Tests~/AffixDiscoveryRegression.cs`: thử trạng thái, rèn thất bại, giữ Modifier đúng, bảo toàn schema cũ, lưu/tải, đặc tính luyện mạo hiểm.

## Còn phải quyết định trước khi hiện thực luyện chế

- Chính xác mỗi mức phẩm chất/tiềm năng tương ứng bao nhiêu dòng; việc hiện tại bị tụt phẩm chất **không thay đổi** số dòng đang sở hữu.
- Ngưỡng cần đạt để từng dòng xuất hiện (cấp item, cấp cường hóa, điều kiện bổ sung).
- Danh sách hiệu ứng hợp lệ và nhóm chỉ số chung theo từng hệ; quy tắc không lặp các dòng quá mạnh.
- Phân bố ngẫu nhiên giới hạn `naturalRefineLimit`, phạm vi nâng tiềm năng, thẩm định tiến trình chỉ mang tính ước lượng cho người chơi.
- Công thức tài nguyên, vàng, phần thưởng/thất bại thường và mạo hiểm; xác suất phá hủy đồ và thông báo xác nhận rủi ro.
- Khi triển khai rèn thật phải cập nhật `refineLevel`, `highestRefineLevel`, `hasBeenRefined`, gọi `RevealEligibleAffixes` sau **thành công**, rồi `RebuildModifiers` và cập nhật sức mạnh Hero. **Không gọi Reveal từ Load/Normalize**.

## Tương thích, bảo vệ dữ liệu

Không bật RNG, không đổi tỷ lệ rơi, không có nút rèn hoặc hủy đồ. Các item đã có từ 0.0.52 được chuyển đổi có chủ đích khi tải; cần sao lưu Saves trước khi thử chuyển từ nhánh UI hoặc build cũ sang nhánh schema 3. Không nên dùng cùng một bản save đã lưu bằng schema mới với bản game cũ, nếu chưa thử khả năng hạ phiên bản.

**Chưa kiểm thử bằng Unity Editor hoặc C# compiler trong môi trường hiện tại.** Hãy dùng Play Mode, kiểm tra Console và chạy toàn bộ kiểm thử hồi quy trước khi gộp các nhánh.
