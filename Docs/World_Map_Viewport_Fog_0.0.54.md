# 0.0.54 — Camera bản đồ thế giới & sương mù

## Mục tiêu / quy ước

- Khi mở **Bản đồ**, căn ô nhân vật đang đứng vào vùng xem; lấy **chiều rộng hộp bao polygon bằng 40% chiều rộng vùng xem** (không phải 40% diện tích toàn hình). Giới hạn hệ số phóng 1×–16× để tránh phóng quá mức. Không thay đổi vị trí người chơi hay gameplay khi thao tác camera.
- Nếu map hiện tại thiếu polygon (TM-03, TM-13), định vị theo `WorldMapCatalog.PreviewPosition` và dùng bề rộng quy ước cho mức zoom; không tạo thêm polygon, không giả vờ diện tích 40% đã được đo.
- Giữ chuột trái trên nền bản đồ rồi kéo để pan; nếu di chuyển chưa đến **6 px**, thả chuột vẫn được coi là click để chọn map. Vùng xem có **RectMask2D** tránh vẽ/nhấn ra bên ngoài. Ngăn native Windows kéo cửa sổ khi đang pan bản đồ.
- Cuộn bánh xe trên vùng xem để phóng to/thu nhỏ quanh con trỏ. Thanh công cụ ở góc trên trái: **−**, **+**, **Về vị trí**; có nhãn hiển thị phần trăm zoom. Phím hoặc thanh zoom riêng có thể bổ sung sau.
- Fog: giữ nguyên ranh giới khám phá chính xác của polygon đã tới, thêm vùng nhìn thấy mờ dần **20 pixel nguồn** *ra bên ngoài* mép polygon rồi trở về fog đục. Vùng mở rộng chỉ có tác dụng **hiển thị**, không cập nhật `worldMapVisited`, liên kết di chuyển hoặc vùng click.
- Không chỉnh 131 polygon/texture/catalog; vẫn bỏ trống TM-03 và TM-13. Không sửa các chỗ polygon chồng lấn nhẹ đã được người thiết kế chấp nhận.

## Tệp đã thay

1. `Assets/Script/WorldMapPanelUI.cs`: thêm viewport bị cắt, camera zoom & pan, nút phóng to/thu nhỏ, tâm map mặc định và xây mask sương mù mềm.
2. `Assets/Script/UIManager.cs`: điều phối input chuột trong Unity Editor/đường non-native, bảo toàn quyền ưu tiên các nút UI.
3. `Assets/Script/TransparentWindow.cs`: native Windows giữ chuột trái để kéo map (không kéo cửa sổ); lăn chuột để zoom; bỏ thao tác nếu mất vị trí chuột hợp lệ.
4. `Tests~/UnityStub.cs`: bổ sung phép toán và InputSystem stub phục vụ test dựa trên mã nguồn.

## Checklist Play Mode trên Windows (chưa chạy tại đây)

- Mở map lần đầu sau khi đứng ở một ô có polygon: ô đó vào giữa vùng nhìn và rộng khoảng 40% chiều rộng vùng xem (nếu chưa chạm giới hạn 16×).
- Giữ chuột trái và rê: **chỉ di chuyển bản đồ**, không di chuyển cửa sổ Windows; thả chuột không đổi map đã chọn. Click đơn ở ô hợp lệ vẫn chọn; nút **Tới ô đã chọn** mới di chuyển nhân vật.
- Lăn bánh xe lên/xuống: zoom quanh vị trí con trỏ, không nhảy vị trí bất thường; nút +/− và **Về vị trí** hoạt động; không thể pan vượt ra khỏi cạnh bản đồ.
- Đổi preset kích thước cửa sổ khi map đang mở, thử zoom/pan lại: artwork, sương mù, nét viền và hitbox vẫn cùng vị trí. Thử ở 800×450 và độ phân giải màn hình lớn.
- Với map có nhiều ô đã đi qua: biên giới khám phá sáng rõ, mép fog chuyển mềm khoảng 20px ngoài polygon, ô chưa đi không bị lộ hoàn toàn hoặc được đánh dấu visited.
- Trên TM-03/TM-13 thiếu polygon: camera định vị theo seed, không phát sinh click giả. Kiểm thử cạnh sát UI (nút đóng, nút zoom, danh sách đường đi) để bảo đảm nút nhận sự kiện trước nền map.

**Giới hạn xác minh:** chưa thực hiện compile Unity, Unity Play Mode/Windows executable hay nghe/chạm chuột trực tiếp trong môi trường này; yêu cầu kiểm thử trên máy phát triển trước khi merge.
