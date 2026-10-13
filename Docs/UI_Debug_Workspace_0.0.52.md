# 0.0.52 — Nhánh chuyên chỉnh sửa UI

**Nhánh:** `feature/0.0.52-ui-workspace`  
**Nguồn:** trạng thái đã commit mới nhất của `feature/0.0.52-inventory-equipment` tại thời điểm tạo nhánh. Nhánh nguồn và `main` giữ nguyên.

## Bật lớp đo UI

1. Chuyển sang nhánh UI bằng GitHub Desktop và Pull.
2. Mở `Assets/Scenes/MainGame.unity` (hoặc `SampleScene.unity`). `UIManager` đã có sẵn Theme và các thuộc tính debug.
3. Trong Inspector của `UIManager`, mở **0.0.52 UI WORKSPACE - DO KICH THUOC ASSET**.
4. Để **Inventory Layout Debug = true**; Play Mode → mở Kho đồ.
5. Các khung sẽ hiện viền xanh ngọc, nền xanh tối nhẹ, nhãn **Cao x Rộng** đo trực tiếp `RectTransform.rect` cùng gợi ý `9-slice`.
6. Tắt bằng checkbox **Inventory Layout Debug** hoặc menu ngữ cảnh trên `UIManager`: **0.0.52 UI - Bat/Tat khung do kich thuoc (Play Mode)**.
7. Để in toàn bộ kích thước không bị chữ chồng lên màn hình, trong Play Mode chọn menu **0.0.52 UI - In bang kich thuoc UI vao Console** trên `UIManager`. Tìm log tiền tố `[UI SIZE 0.0.52]`.

## Danh mục 12 vùng

| Mã | Vùng | Gợi ý |
|---|---|---|
| 01 | Inventory tổng | 9-slice nếu là khung viền co giãn |
| 02 | Thanh tiêu đề | 9-slice nếu muốn tái dùng nhiều chiều rộng |
| 03 | Khung trang bị | 9-slice |
| 04 | Khung túi đồ | 9-slice |
| 05 | Bảng chỉ số | 9-slice |
| 06 | Chi tiết vật phẩm | 9-slice |
| 07 | Vùng Hero / Map | Giữ tỉ lệ ảnh minh họa; không 9-slice |
| 08 | Tiêu đề Map | Giữ tỉ lệ nếu là banner trang trí cố định; không 9-slice |
| 09 | Một ô trang bị mẫu | Sprite vuông kích thước ổn định; không 9-slice |
| 10 | Một ô túi mẫu | Sprite vuông kích thước ổn định; không 9-slice |
| 11 | Tab | 9-slice |
| 12 | Nút Đóng | 9-slice nếu nút dạng viền co giãn |

`9-slice: NÊN/KHÔNG` là **gợi ý thiết kế**. Phần `Simple/Sliced/chưa gán` là **kiểu đang sử dụng** trong `UnityEngine.UI.Image`. Thực tế Sliced chỉ có tác dụng đúng khi Sprite được thiết lập Border trong Sprite Editor. Các ô nhỏ hiện thị nhãn rút gọn để tránh chiếm quá nhiều diện tích.

## Quy ước kích thước

- **Cao x Rộng** = `RectTransform.rect.height x RectTransform.rect.width`, tính theo đơn vị UI của Canvas.
- Canvas HUD hiện dùng `ScreenSpaceOverlay`, `scaleFactor=1`, vì vậy các số này tương ứng pixel UI trong phiên đang chạy. Trong các Canvas khác, hãy kiểm tra `Canvas.scaleFactor` trước khi chuyển sang kích thước thiết kế.
- Mọi giá trị lấy theo kích thước cửa sổ và tỉ lệ bố cục **thực tế lúc đang Play**, không phải giá trị cố định dự đoán.
- Có thể chụp ảnh màn hình debug hoặc copy log để thiết kế lại ảnh PNG, cài Border 9-slice và kiểm tra vùng trang trí tránh vị trí chữ/nút.

## Bảo vệ gameplay

Lớp debug chỉ tạo đối tượng UI con với `raycastTarget=false`; không đụng tới trang bị, số liệu Hero, map, logic combat hay Save/Load. Việc đo không tự sửa RectTransform của thành phần gốc. Mã mới chỉ nằm trên nhánh UI.

**Chưa biên dịch/kiểm thử bằng Unity Editor từ môi trường trợ lý.** Hãy kiểm tra Play Mode và Windows Build trước khi coi nhánh UI là ổn định.
