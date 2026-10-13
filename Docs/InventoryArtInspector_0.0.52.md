# 0.0.52 — Tự thay giao diện bằng Sprite trong Unity Inspector

**Mục tiêu:** không phải sửa C# để đổi hình khung, nền, ô túi đồ hoặc cảnh splash. Ảnh PNG là placeholder, có thể thay bằng artwork hoàn chỉnh bất kỳ lúc nào.

## Bắt đầu

1. Trong Unity, chép các ảnh PNG của bộ asset vào `Assets/Art/UI/Inventory/` (hoặc thư mục riêng theo sở thích).
2. Chọn từng PNG ở cửa sổ Project → Inspector → **Texture Type: Sprite (2D and UI)** → **Sprite Mode: Single** → Apply. Nên để Alpha Is Transparency cho ảnh có nền trong suốt.
3. Chọn **Assets → Create → Tu Tien → UI → Inventory Art Theme**, đặt tên ví dụ `XianxiaInventoryTheme`.
4. Chọn Theme: Inspector mới có nhóm **01–05**, kéo Sprite vào các trường. Nếu trống, giao diện giữ màu mặc định.
5. Chọn GameObject mang `UIManager` ở Scene → trường **Inventory Art Theme** → kéo Theme vừa tạo vào.
6. Play → mở **Kho đồ**. Để thay ảnh khi đang Play, đổi Sprite trong Theme rồi chọn menu ba chấm của `UIManager` → **0.0.52 - Ap dung anh Inventory (Play Mode)**. Thay đổi Theme asset nên lưu ngoài Play Mode.

## Gợi ý ghép bộ hình đã tạo

| Inspector | Loại ảnh |
|---|---|
| Inventory Window Frame | Khung cửa sổ kho tổng (phần bên phải) |
| Inventory Header | Thanh tiêu đề, có thể để trống nếu khung tổng đã chứa tiêu đề |
| Equipment Panel | Nền sau các ô mặc trên Hero |
| Bag Panel | Nền bảng lưới vật phẩm |
| Item Details Panel | Khung chi tiết vật phẩm và dòng thuộc tính |
| Mini Stats Panel | Khung thông số ngắn cạnh ô trang bị |
| Bag Slot | Một ô vuông trống dùng chung cho toàn lưới |
| Selected Bag Slot | Ô đang chọn; bỏ trống nếu muốn ô thường tự đổi màu |
| Equipment Slot | Một khung đồ mặc, lặp lại cho 8 ô |
| Normal / Selected Tab | Tab thường và tab được chọn |
| Action Button | Nút Trang bị / Tháo / Đóng |
| Hero Area Background | Lớp nền vùng trên-trái của cửa sổ mở rộng |
| Hero Splash Frame | Khung trong suốt đặt **đè trên** splash Hero |
| Map Title Plate | Nền khung ghi tên bản đồ |
| Hanging Ornament | Dải hoa văn/thẻ treo đứng |
| Default Hero Splash | Hình Hero mặc định (sau này thay bằng Spine/ảnh thật) |
| Default Map Splash | Splash map mặc định |

## Đặt ảnh riêng cho bản đồ

Ở **04 · Splash theo Map Number**, thêm phần tử trong `Map Arts`. Mỗi phần tử có:
- `Map Number`: đúng `EntityDataSO.mapNumber` (ví dụ 1, 2, 3, 4).
- `Display Name`: chỉ là chú thích để bạn phân biệt trong Inspector.
- `Map Splash`: cảnh nền riêng của map.
- `Hero Splash`: ảnh Hero riêng nếu muốn.
- `Title Plate`, `Ornament`: khung/hoa văn riêng của map.

Các ô trống sẽ lấy hình mặc định ở nhóm 03. Tránh trùng Map Number; Inspector cảnh báo.

## Chất lượng và bố cục

- **Không bật `Sliced Resizable Panels`** nếu bạn đang dùng ảnh vẽ nguyên tấm nhiều họa tiết: Unity sẽ co giãn Simple Sprite. Chỉ bật khi đã đặt **Border (9-slice)** trong Sprite Editor cho panel cần kéo rộng; Inspector báo cảnh báo nếu Sprite khung chưa có Border.
- Có thể giữ `Preserve Splash Aspect` để tránh làm méo Hero/splash.
- Nếu hình quá sáng, thay `Text Color` và `Accent Text Color` để chữ dễ đọc.
- Dùng PNG trong suốt cho viền và khung Hero; dùng hình có nền cho Splash Map.
- `Scrollbar Track`, `Scrollbar Handle` đã có chỗ chọn trong Theme, **chưa được sử dụng**: bản hiện tại vẫn dùng phân trang. Chỉ khi thêm ScrollRect thật mới áp dụng hai hình này.
- Không cần gán Sprite trong GameObject `052_ExpandedInventory` được tạo lúc chạy: lớp đó là động, sẽ bị tạo lại. Chỉ chỉnh **Theme asset** trên `UIManager`.

**Phạm vi:** lớp hình ảnh, không chỉnh dữ liệu vật phẩm, chỉ số hoặc lưu/tải. Vẫn cần chạy kiểm thử trong Unity trước khi chốt.
