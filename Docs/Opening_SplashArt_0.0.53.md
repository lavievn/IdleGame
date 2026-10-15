# Opening 0.0.53 — Hai khung splash ngẫu nhiên + viền mềm mây núi

**Trạng thái:** mã đã commit lên `feature/0.0.53`; chờ kiểm thử Unity Play Mode / Windows. Đây là **thiết kế mới nhất**, thay cho bản một splash video ở giữa và bản slideshow pan/zoom tự động trước đó.

## 1. Đường dẫn và cách thêm tài nguyên

```text
Assets/
├── Resources/
│   ├── OpeningArtTheme.asset     # Sprite, kích thước mây/núi, feather
│   ├── OpeningSoftEdge.shader    # Shader mép mềm 48 px
│   └── OpeningTitleLogo.png      # Logo Bách Luyện Tiên Đồ
└── StreamingAssets/
    ├── OpeningBackground/
    │   └── opening.mp4           # Video nền toàn màn hình (tùy chọn)
    ├── OpeningSplashArt/
    │   ├── L_kiemkhach.png       # Chỉ chọn cho khung trái
    │   ├── L_chuongmon.mp4       # Chỉ chọn cho khung trái
    │   ├── R_nu_hiep.jpg        # Chỉ chọn cho khung phải
    │   └── R_phapsu.mp4         # Chỉ chọn cho khung phải
    └── OpeningSplash/
        └── splash.mp4            # Đường dẫn cũ, KHÔNG còn tự tải
```

**Thả ảnh PNG/JPG/JPEG hoặc MP4 vào `Assets/StreamingAssets/OpeningSplashArt/`. Bắt buộc tên file bắt đầu bằng `L` (khung trái) hoặc `R` (khung phải), không phân biệt chữ hoa/thường.** Ví dụ `L_hero.png`, `R_hero.mp4`. Các file không có tiền tố tương ứng sẽ bị bỏ qua. Ảnh được đọc trực tiếp bằng byte PNG/JPG; video MP4 dùng `VideoPlayer` theo đường dẫn; không cần Catalog, Inspector hay Tools → Rebuild.

**Không còn fallback sang `OpeningSplash/splash.mp4` cũ**, vì file đó không bắt đầu bằng `L`/`R`. Nếu một bên không tìm thấy file đúng tiền tố thì bên đó giữ khối màu placeholder và Console có cảnh báo. Hai video bạn đã push trước đây đang có tên `Pin on нри.mp4` và `Rainstorm Warrior Live Wallpaper.mp4`, cần đổi tên có tiền tố theo vị trí mong muốn (đừng quên Unity `.meta` nếu rename ngoài Unity).

## 2. Vị trí và hoạt ảnh

- **Hai khung độc lập:** `07 SPLASH LEFT - OVERSCAN [WxH UI]` và `08 SPLASH RIGHT - OVERSCAN [WxH UI]` (tên tự ghi kích thước thực tế trong Hierarchy). Mỗi khung rộng **62%**, cao **76% chiều cao** vùng mở đầu và **căn sát đáy**. Tâm nằm tại **−56%/+56% chiều ngang** tính từ tâm cửa sổ. Khung chỉ tràn ra ngoài **mép trái/phải**, tuyệt đối không tràn lên trên hay xuống dưới: phần trên khoảng **24% chiều cao** vẫn để trống cho trời, núi và logo. Mép trong của mỗi khung nằm ở khoảng 25%/75% bề ngang để chừa phần giữa cho bốn nút.
- **Từ giây 27:** hai khung đồng thời **fade in 1,4 giây**, sau đó **đứng yên**, không pan/zoom, không đổi ảnh/video, không có hiệu ứng mờ chồng các lượt. Chuyển động thực tế của nhân vật có thể được dựng **bên trong MP4**.
- **Ngẫu nhiên một lần mỗi lượt mở màn hình chờ:** trái chọn trong nhóm `L`, phải chọn trong nhóm `R`; hai nhóm không thể trùng nhau. Không tự đổi nội dung khi người chơi đứng chờ. Vào gameplay, video dừng và texture của ảnh được giải phóng. Khi từ System Menu trở lại màn hình chính, hai bên sẽ chọn lại ở giây 27.
- **Giữ nguyên:** logo Bách Luyện Tiên Đồ, video nền `opening.mp4` dưới cùng, mây trôi, menu bốn nút, nhạc opening và hiệu ứng dạt mây/zoom tổng thể khi chuyển sang gameplay.

**Lưu ý video:** MP4/H.264 thông thường **không chứa kênh alpha**; khi đặt video người ở hai bên, nền video có thể tạo mép hình chữ nhật. Nếu muốn nhân vật tách nền đẹp, nên dùng PNG trong suốt hoặc xử lý nền video nhất quán với phông opening. **Có thể phát đồng thời hai video splash cộng video nền**, nên kiểm tra mức tải GPU/CPU.

## 3. Làm mềm viền mây/núi

- Shader: `Assets/Resources/OpeningSoftEdge.shader` và script `OpeningEdgeFeather.cs`.
- Tự áp dụng cho **Núi xa**, **Núi gần**, **Mây xa**, **Mây trước trái** và **Mây trước phải**.
- Alpha giảm dần từ **mép của RectTransform đi vào trong**, mặc định **48 px**. Hiệu ứng hoạt động với ảnh có mép cứng cũng như ảnh có alpha; không chỉnh sửa file nguồn.
- Muốn đổi độ rộng viền: chọn `Assets/Resources/OpeningArtTheme.asset`, chỉnh **Edge Feather Pixels** (bản đã push trước đó đang đặt **65,4**, không reset về 48) rồi chạy lại Play Mode. Không áp dụng shader này cho logo, video nền hay khung splash.
- Nếu núi/mây vẫn có đường nối **ở giữa ảnh** chứ không phải mép khung, feather hình chữ nhật không xóa được những đường nối nội bộ; khi đó cần PNG alpha/mask đúng hình hoặc thay cách bố cục.
- Đây là **48 đơn vị UI gần tương đương pixel ở tỉ lệ Canvas đang dùng**, có thể trông lớn/nhỏ khác nhau ở cửa sổ hoặc CanvasScaler khác. Cần đánh giá bằng Play Mode.

## 4. Các bước kiểm thử

1. Pull nhánh `feature/0.0.53`. Không cần nhập ảnh hoặc video ngay: chạy Play Mode, sau 27s phải xuất hiện **hai khung màu đứng yên** tràn ra hai bên; không còn khung màu lớn phủ toàn bộ giữa màn hình.
2. Thả một ảnh `L_hero.png` và một ảnh `R_hero.jpg` vào `Assets/StreamingAssets/OpeningSplashArt`; dừng rồi mở lại Play Mode. Sau 27s, đúng từng bên hiển thị hình có tiền tố tương ứng; một file `test.png` không tiền tố không được chọn.
3. Bổ sung MP4/H.264 vào cùng thư mục. Test ảnh trái / video phải, rồi test cả hai video; chỉ nhạc opening được nghe, không có tiếng MP4.
4. Chọn `OpeningArtTheme.asset`: giờ có năm trường **Far/Near Mountains Reference Size**, **Back/Left/Right Clouds Reference Size**, theo đơn vị UI của cửa sổ mẫu 800×450. Giá trị mặc định trùng bố cục hiện tại: lần lượt **944×279**, **992×153**, **960×198**, **536×409,5**, **536×409,5**. Tại các cửa sổ khác, kích thước được nhân theo tỉ lệ chiều rộng/chiều cao. Hierarchy tự ghi kích thước thực sau tên layer, ví dụ `02 FAR MOUNTAINS [944x279 UI]`. Đổi Edge Feather Pixels để kiểm tra viền mềm. Ảnh được feather theo khung, không phải feather riêng alpha biên dạng nhân vật.
5. Chuyển kích thước cửa sổ 600 / 800 / 1150: mỗi khung chỉ tràn ngang ra ngoài mép trái/phải; đáy bám sát đáy cửa sổ, đỉnh nằm tại khoảng 76% chiều cao tính từ dưới lên. Không được tràn qua mép trên/dưới; các nút menu vẫn nhận chuột.
6. Chơi mới/Tiếp tục → vào gameplay: video tắt; System Menu → Quay lại màn hình chính: hai khung lại chờ đúng 27s và chọn ảnh/video ngẫu nhiên mới.
7. Kiểm tra phiên bản Windows overlay (CMP 40HX): video có giải mã được không, CPU/GPU/VRAM có bị tăng bất thường khi có **tối đa ba luồng MP4** (nền + hai splash) không.

**Chưa biên dịch/kiểm thử Unity Play Mode tại môi trường trợ lý**; bộ test `Tests~/OpeningCinematicRegression.cs` chỉ kiểm tra cấu trúc và logic thời gian, không xác minh shader/codec/nhận chuột.
