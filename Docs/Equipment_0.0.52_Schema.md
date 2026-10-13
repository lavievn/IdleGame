# Thiết kế nền trang bị nhiều vị trí — 0.0.52

## Phạm vi đã mã hóa

- **8 ô**: Vũ khí (Weapon), Mũ (Head), Áo (Chest), Giày (Boots), Pháp bảo (Artifact), Dây chuyền (Necklace), Nhẫn trái và Nhẫn phải (RingLeft/RingRight).
- **Thứ bậc phẩm chất đúng bản chốt**: Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên.
- Hai xác suất cực hiếm được **lưu làm mốc cấu hình**, không chạy rơi đồ: Thiên **0,005%** (`0.00005` xác suất), Tiên **0,001%** (`0.00001`). Các cấp Hoàng/Huyền/Địa chưa có xác suất chính thức; chưa có hàm chọn random rarity nên không thể phát đồ hiếm ngoài dự tính.
- **Vật phẩm riêng biệt**: `EquipmentInstance` có `instanceId` (GUID), `templateId`, `level`, `rarity`, `refineLevel`, `mainStats`, `extraStats`, `isEquipped`, `equippedSlot`.
- **Tách chỉ số chính / chỉ số phụ**: `mainStats` lưu chỉ số gốc đặc trưng loại trang bị. `extraStats` chứa dòng tùy chọn (có thể bằng 0). Hạn mức thử nghiệm: cấp 1–10: 1 dòng phụ; 11–20: 2; tăng thêm mỗi 10 cấp cho tới tối đa 6. Đây là **quy tắc tạm để kiểm tra cấu trúc**, chưa chốt đường cong cân bằng cuối cùng; không cộng dòng chính vào mức trần.
- **Sức mạnh không sinh lại khi load**: giá trị thực tế của từng dòng được lưu trực tiếp, không tính lại từ rarity hay random khi tải. Phẩm chất cao ảnh hưởng quy tắc sinh món về sau; hiện chưa tung vòng quay vật phẩm.
- **Rèn và nghề phụ chỉ có cấu trúc dữ liệu**, không có nút thực thi: cấp rèn trên từng item; `RefinementRuleData` (vàng, nguyên liệu, yêu cầu nghề, xác suất thành công per-million, hậu quả thất bại); `SupportProfession` (Luyện khí/Rèn, Luyện đan, Chế phù, Phù phép), cấp/EXP nghề; sách công thức đã học và kỹ năng phụ trợ có cấp độ độc lập.
- **Kho**: `EntityDataSO.equipmentItems` là danh sách cá thể duy nhất, giữ trạng thái đang mặc tại chính bản ghi món đồ. Nguyên liệu và vật phẩm xếp chồng dùng `EntityDataSO.inventory` (đã sửa để giữ cả ID nguyên liệu chưa nằm trong catalog hai hộ phù).
- **Hiệu ứng**: `EquipmentRuntime` tạo các `CombatStatModifier` bằng prefix `equipment:instance:<GUID>:m/a:index`; tháo/đổi/tải lại gỡ và dựng đúng nguồn, không tích lũy vĩnh viễn lên chỉ số gốc. Chỉ số có điều kiện (stance/tag/element) vẫn dùng `CombatStats` 0.0.51.
- **Tương thích ngược**: hai hộ phù từ bản 0.0.52 cũ vẫn dùng API và UI cũ, chiếm ô Pháp bảo khi mặc. Hệ mới không cho phép mặc thêm Pháp bảo vào ô đã chiếm. SaveManager xóa trường template mới trước khi nạp save cũ, rồi normalize cả hai hệ.
- **Đồng đội thử B2**: không thừa hưởng cá thể trang bị hay Modifier của Hero chính.

## Nội dung thử trong giao diện

- Giữ nguyên nút **Kho đồ → Hộ phù HP/ATK**.
- Bổ sung **Nhận Thiết Kiếm thử (1 lần)** → **Trang bị Thiết Kiếm** → **Tháo vũ khí** để kiểm tra vòng đời item độc lập.
- Hiển thị trạng thái 8 ô ngay trong Kho đồ. Chỉ Kiếm và hai Hộ phù có nút thao tác trực tiếp; UI chọn mọi loại trang bị theo danh sách cá thể sẽ phát triển sau khi cơ chế dữ liệu được kiểm chứng.

## Kiểm thử

`Tests~/MultiSlotEquipmentRegression.cs` kiểm tra thứ tự độ hiếm, đơn vị xác suất 0,001–0,005%, giới hạn dòng phụ, hai chiếc nhẫn riêng biệt, cấm kiếm vào ô mũ, chiến đấu theo dạng (kiếm cận chiến), đổi/tháo không cộng dồn, một kiếm phẩm chất Thiên có dòng Hỏa, và vòng **lưu tay → lên cấp → tải lại cùng item + nghề + vàng**.

**Play Mode cần thử:** backup thư mục Saves, Pull nhánh, Chơi mới, vào Kho đồ nhận kiếm thử và trang bị; kiểm tra ATK cận chiến +10, đổi sang Cung không còn +10; lưu vào Ô 1; tháo kiếm, thay đổi cấp Hero, sau đó tải lại Ô 1 kiểm tra mã và cấp Hero cũng như trang bị quay lại đúng snapshot. Dùng lại Hộ phù HP và ATK cũ để kiểm tra hệ tương thích. Mã chưa được biên dịch hoặc chạy trong Unity Editor từ môi trường trợ lý.

## Giai đoạn sau (chưa triển khai)

- Bảng dữ liệu xác suất rơi cho tất cả phẩm chất, hệ số phẩm chất, khả năng xuất hiện dòng phụ, điều kiện level vật phẩm; cách tạo trang bị ở boss/quái.
- Nguyên liệu/vàng và thất bại trong luyện chế thật; sách mở khóa nghề/công thức; tác dụng chế thuốc, chế phù, buff.
- UI kho dạng lưới với ô/cửa sổ chi tiết, danh sách nhiều item một template, kéo thả, giới hạn chỗ chứa, chọn từng món để trang bị, và trang bị cho đội hình nhân vật cố định.
