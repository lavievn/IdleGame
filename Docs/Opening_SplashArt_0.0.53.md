# Opening 0.0.53 — Hai khung splash ngẫu nhiên + viền mềm mây núi

**Trạng thái:** mã đã commit lên `feature/0.0.53`; chờ kiểm thử Unity Play Mode / Windows. Đây là **thiết kế mới nhất**, thay cho bản một splash video ở giữa và bản slideshow pan/zoom tự động trước đó.

## 1. Đường dẫn và cách thêm tài nguyên

```text
Assets/
├── Resources/
│   ├── OpeningArtTheme.asset     # Các ô sky/mountain/cloud, edgeFeatherPixels
│   ├── OpeningSoftEdge.shader    # Shader mép mềm 48 px
│   └── OpeningTitleLogo.png      # Logo Bách Luyện Tiên Đồ
└── StreamingAssets/
    ├── OpeningBackground/
    │   └── opening.mp4           # Video nền toàn màn hình (tùy chọn)
    ├── OpeningSplashArt/
    │   ├── splash-01.png         # Người phát triển tự thêm
    │   ├── splash-02.jpg         # Người phát triển tự thêm
    │   └── hero-03.mp4           # Người phát triển tự thêm
    └── OpeningSplash/
        └── splash.mp4            # Đường dẫn CŨ, chỉ dùng nếu kho mới trống
```

**Thả ảnh PNG/JPG/JPEG hoặc MP4 vào `Assets/StreamingAssets/OpeningSplashArt/`.** Tên tùy ý, không bắt buộc `Left`/`Right`. Ảnh được đọc trực tiếp bằng byte PNG/JPG; video MP4 được `VideoPlayer` đọc theo đường dẫn. Không cần chỉnh Texture Type, tạo Catalog, kéo Sprite vào Inspector hoặc vào mục Tools → Rebuild.

Nếu thư mục mới trống nhưng vẫn có `Assets/StreamingAssets/OpeningSplash/splash.mp4` của lần trước, game phát video cũ trên cả hai bên (vì chỉ có một lựa chọn). Không có tài nguyên nào thì hai khung màu có nhãn vẫn hiện.

## 2. Vị trí và hoạt ảnh

- **Hai khung độc lập:** `07 SPLASH LEFT - OVERSCAN` và `08 SPLASH RIGHT - OVERSCAN`. Mỗi khung có chiều rộng **68%** và chiều cao **115%** của vùng mở đầu. Trung tâm ở khoảng **-41,5%/+41,5% chiều rộng** so với trung tâm cửa sổ: phần ngoài tràn qua mép trái/phải. Để trống phần giữa cho logo và bốn nút.
- **Từ giây 27:** hai khung đồng thời **fade in 1,4 giây**, sau đó **đứng yên**, không pan/zoom, không đổi ảnh/video, không có hiệu ứng mờ chồng các lượt. Chuyển động thực tế của nhân vật có thể được dựng **bên trong MP4**.
- **Ngẫu nhiên một lần mỗi lượt mở màn hình chờ:** bên trái chọn trước; bên phải tránh chọn trùng file nếu kho có từ hai file. Vào gameplay, video dừng và texture của ảnh được giải phóng. Khi từ System Menu trở lại màn hình chính, hai bên sẽ chọn lại ở giây 27.
- **Giữ nguyên:** logo Bách Luyện Tiên Đồ, video nền `opening.mp4` dưới cùng, mây trôi, menu bốn nút, nhạc opening và hiệu ứng dạt mây/zoom tổng thể khi chuyển sang gameplay.

**Lưu ý video:** MP4/H.264 thông thường **không chứa kênh alpha**; khi đặt video người ở hai bên, nền video có thể tạo mép hình chữ nhật. Nếu muốn nhân vật tách nền đẹp, nên dùng PNG trong suốt hoặc xử lý nền video nhất quán với phông opening. **Có thể phát đồng thời hai video splash cộng video nền**, nên kiểm tra mức tải GPU/CPU.

## 3. Làm mềm viền mây/núi

- Shader: `Assets/Resources/OpeningSoftEdge.shader` và script `OpeningEdgeFeather.cs`.
- Tự áp dụng cho **Núi xa**, **Núi gần**, **Mây xa**, **Mây trước trái** và **Mây trước phải**.
- Alpha giảm dần từ **mép của RectTransform đi vào trong**, mặc định **48 px**. Hiệu ứng hoạt động với ảnh có mép cứng cũng như ảnh có alpha; không chỉnh sửa file nguồn.
- Muốn đổi độ rộng viền: chọn `Assets/Resources/OpeningArtTheme.asset`, chỉnh **Edge Feather Pixels** (ví dụ 40–50) rồi chạy lại Play Mode. Không áp dụng shader này cho logo, video nền hay khung splash.
- Nếu núi/mây vẫn có đường nối **ở giữa ảnh** chứ không phải mép khung, feather hình chữ nhật không xóa được những đường nối nội bộ; khi đó cần PNG alpha/mask đúng hình hoặc thay cách bố cục.
- Đây là **48 đơn vị UI gần tương đương pixel ở tỉ lệ Canvas đang dùng**, có thể trông lớn/nhỏ khác nhau ở cửa sổ hoặc CanvasScaler khác. Cần đánh giá bằng Play Mode.

## 4. Các bước kiểm thử

1. Pull nhánh `feature/0.0.53`. Không cần nhập ảnh hoặc video ngay: chạy Play Mode, sau 27s phải xuất hiện **hai khung màu đứng yên** tràn ra hai bên; không còn khung màu lớn phủ toàn bộ giữa màn hình.
2. Thả **2 PNG hoặc JPG** vào `Assets/StreamingAssets/OpeningSplashArt`; dừng rồi mở lại Play Mode, đợi 27s: mỗi bên ngẫu nhiên một hình khác nhau, giữ nguyên hình đến khi vào gameplay.
3. Bổ sung MP4/H.264 vào cùng thư mục. Test ảnh trái / video phải, rồi test cả hai video; chỉ nhạc opening được nghe, không có tiếng MP4.
4. Chọn `OpeningArtTheme.asset`: thay **Edge Feather Pixels** qua 0 → 40 → 48 → 50 để xem viền núi/mây mềm dần, đặc biệt chỗ bị lộ cạnh cứng. Ảnh được feather theo khung, không phải feather riêng alpha biên dạng nhân vật.
5. Chuyển kích thước cửa sổ 600 / 800 / 1150: mỗi khung vẫn tràn ra ngoài theo tỷ lệ, vị trí khung không thay đổi theo thời gian, các nút menu vẫn nhận chuột.
6. Chơi mới/Tiếp tục → vào gameplay: video tắt; System Menu → Quay lại màn hình chính: hai khung lại chờ đúng 27s và chọn ảnh/video ngẫu nhiên mới.
7. Kiểm tra phiên bản Windows overlay (CMP 40HX): video có giải mã được không, CPU/GPU/VRAM có bị tăng bất thường khi có **tối đa ba luồng MP4** (nền + hai splash) không.

**Chưa biên dịch/kiểm thử Unity Play Mode tại môi trường trợ lý**; bộ test `Tests~/OpeningCinematicRegression.cs` chỉ kiểm tra cấu trúc và logic thời gian, không xác minh shader/codec/nhận chuột.
