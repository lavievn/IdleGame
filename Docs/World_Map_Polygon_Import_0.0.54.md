# Nhập dữ liệu polygon .54 (16/10/2026)

## Nguồn & trạng thái

- Nhánh riêng: `0.0.54_map_poly`, tạo từ `feature/0.0.54`; không thay đổi `main`.
- `Assets/Resources/WorldMapPolygons.json`: giữ nguyên dữ liệu người thiết kế gửi, ảnh nguồn `Assets/Resources/WorldMapArtwork.png` (1448 × 1086). Có **131/133 polygon**, **131 reviewed=true**, mỗi vùng 7–24 đỉnh. Không tự thay vị trí, thứ tự, cờ duyệt hay biên giới.
- Hai map cố ý chưa có vùng polygon: **TM-03, TM-13**; vẫn tồn tại trong `WorldMapCatalog.txt`, chưa xóa dữ liệu gameplay. Lớp mây mù không mở vùng trống này; **danh sách map kề vẫn phải kiểm tra riêng** vì graph hiện dựa trên dữ liệu catalog/seeds.
- 133 ID catalog/seeds đối chiếu hợp lệ. Catalog và seeds người thiết kế gửi **trùng nội dung** với bản trên nhánh gốc nên không tạo thay đổi mới.
- Các `.meta` đi cùng ảnh/catalog/seeds được giữ theo bản gửi để bảo tồn GUID.

## Kiểm tra hình học (ngoài Unity)

- Tất cả 131 polygon là đa giác đơn, hợp lệ theo Shapely, đỉnh chuẩn hóa trong [0,1]; không phát hiện ID trùng/ngoài catalog.
- **Chồng lấn diện tích:** có 220 cặp giao nhau với diện tích lớn hơn 1e-9 diện tích tọa độ chuẩn hóa. Nhiều trường hợp nhỏ quanh cạnh tiếp giáp, nhưng có cặp lớn cần rà lại.
- **TV-23 / TV-27:** giao nhau khoảng **4.762 px²**, tương đương **~99% diện tích TV-27**. Trong các trường hợp hai map cùng cho phép click, hit-test theo thứ tự JSON có thể chọn TV-23 thay vì TV-27. Không tự sửa hai polygon.
- **HT-14 / HT-23:** giao nhau khoảng **2.240 px²**, tương đương **~24% diện tích HT-14**. Cần rà lại khi nghiệm thu.
- Tất cả **6 map Boss** và **4 ID được đánh dấu xuất thân trong catalog** có polygon. **XD-11 không được đánh dấu là điểm xuất thân** ở cả file người dùng gửi *và* catalog gốc trên nhánh .54; thiết kế trước đó yêu cầu 5 điểm xuất thân. Đây là tồn đọng cũ, không phải do import polygon gây ra.
- Chưa thực hiện chạy Unity Play Mode/Windows build; các kết quả này là kiểm tra file/hình học, không phải chứng nhận hành vi Unity.

## Giới hạn đồng bộ tài sản

- `WorldMapArtwork.png` do người thiết kế gửi có mặt trong gói lưu kèm ở hội thoại, **chưa được push lên GitHub trong commit text** nếu không thấy blob PNG trong nhánh. Chỉ có `.meta` trên nhánh không thay thế được texture.
- Để máy khác nhận đúng hình ảnh, phải bổ sung **đúng file PNG 1448×1086 gốc** vào `Assets/Resources/WorldMapArtwork.png` bằng kênh Git có thể chuyển nhị phân. Không đổi ảnh sang bản đồ 1536×1024 vì khác bố cục các vùng.
