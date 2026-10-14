# Hệ thống luyện khí 0.0.52 — thiết kế thử nghiệm sửa ngày 14/10/2026

## Luật được chốt để kiểm thử
- Chọn trang bị → nút **Rèn** mở bảng phủ tối Inventory; **Đóng** trở lại Inventory (không thoát toàn bộ kho).
- **Nguyên liệu +100** cộng 100 `material:refinement` vào kho. Nâng cấp vật phẩm (+ cấp) và rèn thường (+ lượt) mỗi lần mất 1 nguyên liệu; rèn đặc biệt (+ lần) mất 10. Các dấu trừ không hoàn nguyên liệu.
- **Không chạy xác suất thất bại** trong bản thử.
- **Mỗi lần rèn thường +1** chỉ tăng lượt rèn. Chỉ khi đủ số lượt yêu cầu mới tăng **đúng một phẩm chất** và mở các dòng ẩn có ngưỡng phẩm chất vừa đạt. Tuyệt đối không từ Phổ thông nhảy thẳng đến giới hạn Địa.
- Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên. Bộ ngưỡng minh họa (chưa chốt cân bằng): **3 / 6 / 12 / 24 / 48** lượt cho từng lần thăng phẩm. Mỗi lượt rèn chỉ tốn 1 nguyên liệu.
- **Thanh tiến độ** chỉ phản ánh **phẩm chất hiện tại / phẩm chất trần tự nhiên**: `(rank hiện tại + 1)/(rank trần + 1)`. Rèn thêm nhưng chưa đổi phẩm chất thì thanh không thay đổi. Đạt trần thì 100% và không thể rèn thường tiếp.
- Kiếm thử bắt đầu **Phổ thông, trần tự nhiên Địa**. Dùng rèn thường lần lượt qua Hoàng (+3), Huyền (+9), Địa (+21). Rèn đặc biệt ở Địa tăng **chỉ một bậc** lên Thiên và thiết lập trần mới Tiên; không nhảy trực tiếp lên Tiên.
- Rèn đặc biệt trước trần vẫn **chỉ tăng 1 phẩm** và tốn 10 nguyên liệu; đây là hành động thiếu hiệu quả có chủ đích. Cơ chế thử hiện mở trần từ Địa lên Tiên (cấu hình chặng mới tăng hai bậc trần); cần thảo luận cân bằng chính thức sau.
- Giảm rèn thường hồi ngược đúng số lượt; nếu đang ở ranh giới phẩm chất thì hạ đúng một phẩm, giữ kiến thức đã khám phá. Giảm rèn đặc biệt trả trạng thái chặng trước, nhưng phải đảo ngược mọi lượt rèn thường / lần thăng phẩm của chặng sau trước.
- Dòng phụ giữ nguyên số lượng, loại và giá trị gốc từ lúc tạo item. Kiếm thử có 4 dòng, lần lượt có ngưỡng Hoàng / Huyền / Địa / Thiên. Các dấu + cấp item hoặc + lượt rèn không trực tiếp khám phá dòng. Chỉ khi **đổi phẩm chất thành công** mới gọi `RevealEligibleAffixes`.
- Khi tụt phẩm chất, dòng từng khám phá chuyển `KnownLocked` (mờ, không cộng chỉ số). Không mất nội dung đã học, không random lại khi load.
- Sức mạnh cơ bản của item có thể thay đổi theo cấp và luyện chế, nhưng hành động đó KHÔNG tự mở dòng phụ.

## Tương thích bản Save trước bản sửa
- Bản kiếm cũ (chưa có forge schema) vẫn giữ nguyên các chỉ số cũ; chỉ bổ sung các trường tiến trình trống.
- Kiếm đã lưu bằng bản forge schema 1 (bản 10 lượt nhảy thẳng đến trần) được chuyển sang schema 2 bằng cách **giữ nguyên phẩm chất, lượt rèn và dòng đã khám phá**, đồng thời đặt lại bộ đếm lượt trong phẩm cấp hiện tại. Các lần rèn kiểu cũ không được diễn giải lại thành phẩm cấp mới. Nên sao lưu Saves trước khi Pull.
- Các bản Save schema 2 sau khi tăng/giảm/rèn đặc biệt lưu cấp, phẩm, trần tự nhiên, lịch sử chặng, tài nguyên và toàn bộ dữ liệu dòng phụ.

## Giao diện và xử lý click
- `InventoryPanelUI.IsForgeOpen` và `ForgeRoot` xác định phạm vi modal.
- `UIManager.HitControl` bỏ qua toàn bộ nút Inventory phía dưới khi modal đang mở. Điều này sửa xung đột hitbox nút Đóng.
- Khi Đóng, chỉ tắt forge overlay, giữ Inventory, vị trí chọn trang bị, và tiếp tục Play Mode.

## Kiểm thử trực tiếp trong Unity
1. Pull `feature/0.0.52-refinement`, thử Play Mode và xem Console.
2. Thêm kiếm và 100 nguyên liệu; tăng cấp item, thử dòng ẩn vẫn ???; giảm cấp không hoàn nguyên liệu.
3. Rèn thường 1–2 lượt: vẫn Phổ thông, progress phẩm 25%; lượt thứ 3: Hoàng và xuất hiện một dòng.
4. Rèn đủ 6 lượt nữa: Huyền (+9), đủ 12 nữa: Địa (+21, thanh 100%); thử rèn thường tại trần phải bị từ chối, không tốn nguyên liệu.
5. Rèn đặc biệt 10 nguyên liệu tại Địa: lên Thiên (KHÔNG lên Tiên); trần mới Tiên. Giảm đặc biệt: về Địa, dòng Thiên hiển thị mờ.
6. Thử rèn đặc biệt từ Phổ thông để xác thực sử dụng sớm tốn 10 nhưng chỉ được Hoàng; dấu trừ không hoàn lại vật liệu.
7. Thử Đóng cửa sổ rèn khi không rèn và sau khi rèn, trở về kho, tiếp tục dùng các nút kho và thoát Inventory bình thường.
8. Save/Load kiểm tra nguyên liệu, phẩm chất, tên item, cột trạng thái ẩn/đã biết/active, giá trị chỉ số không sinh lại.

**Trạng thái kiểm thử:** Chưa có biên dịch Unity/Mono thực tế từ môi trường trợ lý; đã rà mã theo cấu trúc và bổ sung `Tests~/ForgeRegression.cs`. Đây là nhánh thử nghiệm, chưa merge vào nhánh ổn định.
