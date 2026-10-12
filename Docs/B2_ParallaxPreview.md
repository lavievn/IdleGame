# B2 — Bản thử 3 lớp nền thị sai và sắp xếp chiều sâu

Nhánh tích hợp trực tiếp: `feature/b2-combat-target-routing` (12/10/2026). Người phát triển đã chạy thử B2 trong Unity Editor trước khi thêm bản vá hình ảnh này. Bản vá mới **chưa được thử trực tiếp trên Unity**.

## Các lớp từ dưới lên

1. **Núi đen**: `Ground Mountain Parallax 1–3`, dữ liệu ảnh `Assets/Resources/Parallax/ParallaxMountainsTest.txt`. Tốc độ cuộn = 10% Ground, kích thước = 169% Ground (130% so với lớp cây).
2. **Dãy cây xám**: `Ground Tree Parallax 1–3`, dữ liệu ảnh `Assets/Resources/Parallax/ParallaxTreesTest.txt`. Tốc độ cuộn = 30% Ground, kích thước = 130% Ground.
3. **Ground hiện tại**: ba dải nền lặp, tốc độ 100%.
4. **Vật trang trí / Hero / quái**: thứ tự theo Y; Y cao vẽ trước ở xa, Y thấp vẽ sau ở gần.

Hai ảnh nháp được giảm còn 768 x 256 và nén thành PNG, sau đó mã hóa base64 trong TextAsset cho tiện lưu trên GitHub. Lúc khởi tạo `GroundPresentation`, `LoadPreviewSprite` đọc TextAsset trong `Resources/Parallax`, giải mã PNG và tạo sprite runtime. Đây là giải pháp tạm cho kiểm thử; để thay asset chính thức nên nhập PNG/sprite trực tiếp và tạo asset tham chiếu rõ ràng.

## Giới hạn và kiểm thử

- **Không sửa** `EnvironmentManager`, camera, đường đi, combat, tọa độ logic, save hoặc hệ thống wave.
- **Có sửa** `GroundPresentation.cs` và `HeroController.cs` để bổ sung depth sorting (giữ cả xác Hero còn hiện trên màn hình).
- Mỗi lớp 3 bản sao; cạnh nối của hai ảnh nháp chưa được thiết kế seamless, có thể thấy đường nối lặp.
- Xác nhận trong Unity: 3 lớp hiển thị đúng; hai sprite PNG được giải mã, không báo `Parallax preview not found`; cuộn Ground 100%/cây 30%/núi 10%; Hero và quái Y thấp che Y cao; 600/800/1150; dừng/chạy, resize, đạn/vận sức, chết/Retry.
- Sau khi thử hiệu ứng 3 lớp, quay lại nghiệm thu B2 **nhiều Hero** (có API runtime nhưng chưa tự spawn/UI đội hình) thay vì tiếp tục hoàn thiện hình ảnh.

Nếu mất ảnh nháp, mã sẽ tạm dùng sprite Ground cũ đã tô xám/đen và ghi cảnh báo để gameplay vẫn khởi chạy; không được coi đó là nghiệm thu của asset mới.

## Sửa ảnh bị chìm sau Ground khi chạy Unity (12/10/2026)

Người phát triển đã gửi ảnh Unity Editor: chỉ thấy một chút đỉnh núi, chưa thấy cây xám.
Đã kiểm tra hai tệp PNG nháp và thấy viền trong suốt chiếm phần lớn chiều cao.
Trước đây `LayoutParallaxTiles` co cả khung PNG 768×256 về `Ground.height * scale`, làm phần ảnh có alpha còn ít hơn rất nhiều; đồng thời đặt gốc tại Y=0 nên bị dải Grass phía trước che.

- `VisibleSpriteBounds` tìm vùng có alpha > 32 rồi truyền `Rect` cắt vào `Sprite.Create`. Chỉ **cắt vùng trống khi render**, không sửa tệp PNG nguồn.
- `parallaxBaseHeightRatio = 0.25f`: neo đáy silhouette cao hơn, tính theo `grassY + grassHeight * 0.25`, vẫn trong hệ hiển thị và không thay đổi Ground logic.
- Scale cây **1.3× Ground**, núi **1.69× Ground**; tốc độ vẫn cây **0.3×**, núi **0.1×**.
- Chưa chạy Unity Editor sau sửa; cần Pull và thử trực quan. Kiểm tra cả 600/800/1150, khi cuộn lâu và khi pause.
- Nếu sprite bị thay bằng tài nguyên khác (không có alpha), kết quả bố cục cần đánh giá lại; thông số hiện tại dành cho 2 PNG nháp.
