# Opening 0.0.53 — Hai video độc lập và logo Bách Luyện Tiên Đồ

> **Quy tắc mới nhất ngày 14/10/2026 (thay thế tài liệu slideshow cũ).** Không còn random ảnh/video, không zoom/pan splash tự động, không crossfade giữa các splash. Chuyển động nhân vật do người phát triển dựng trực tiếp trong tệp video.

## 1. Cấu trúc Assets

```text
Assets/
├── Resources/
│   ├── OpeningTitleLogo.png        # Đã commit; logo chữ thư pháp nền trong suốt
│   ├── OpeningTitleLogo.png.meta   # Unity import Sprite / UI
│   └── OpeningArtTheme.asset       # Các lớp mây/núi cố định
└── StreamingAssets/
    ├── OpeningBackground/
    │   └── opening.mp4              # TỰ THÊM; video nền bao toàn bộ cửa sổ
    └── OpeningSplash/
        └── splash.mp4               # TỰ THÊM; video splash trong khung tĩnh
```

**Tên file phải đúng `opening.mp4` và `splash.mp4`.** Video chưa có trong repository, game vẫn hiển thị các khối màu dự phòng. `StreamingAssets` đóng gói nguyên file trong build, không cần `Resources.Load` hay gán vào Inspector. Cần chạy lại game sau khi thêm file.

Không cần kéo font từ tệp TTF đính kèm: logo đã được tách nền thành PNG 480×89 từ hình do người phát triển gửi và commit vào `Resources`. Trong `StartMenuUI`, ảnh logo thay dòng giới thiệu cũ, được căn giữa phía trên bốn nút Chơi mới/Tiếp tục/Cài đặt/Thoát; có fallback chữ thường nếu import Sprite lỗi. Có thể thay logo bằng Sprite khác tên `OpeningTitleLogo.png` hoặc gán `OpeningArtTheme.gameTitle`.

## 2. Quy tắc video

| Lớp | Vị trí | Thời điểm | Chuyển động của UI |
|---|---|---|---|
| **Background** | Thấp nhất, phủ 100% viewport opening | Ngay khi vào màn hình chờ | Không pan/zoom; bản thân video có thể chuyển động; lặp |
| Sky / mây / núi | Phía trên video nền | Ngay lúc mở | Các chuyển động bồng bềnh đã có giữ nguyên |
| **Splash** | Một khung cố định, giữa, 90% chiều rộng × 79% chiều cao viewport | Giây **27** | Duy nhất một lần **fade in 1,4s**; không pan/zoom/đổi cảnh; video lặp |
| Logo và 4 nút | Trên các lớp video/mây | Theo fade-in menu cũ | Nút tương tác như trước |

Đường chuyển **vào gameplay** vẫn sử dụng mây dạt/nhạt màu toàn opening trong 2,7s. Phần splash không tự di chuyển, chỉ bị lớp opening tổng làm mờ khi đóng. Video dừng khi gameplay bắt đầu và được chuẩn bị lại khi quay về màn hình chính.

## 3. Dung lượng & mã hóa

- Ưu tiên **MP4/H.264**, 24–30 fps, không cần tiếng: bộ phát tắt tiếng cả hai video để không chồng lên `opening.ogg`.
- Đầu tiên thử 960×540 hoặc 1280×720 cho nền; với splash thử khung hình phù hợp tỷ lệ rộng khoảng 90% × 79% cửa sổ. Nếu video có nền màu không khớp cảnh, lưu ý **MP4/H.264 thường không mang kênh alpha**. Khi đó nên biên tập bố cục splash với nền phù hợp, hoặc sau này dùng codec/đường render hỗ trợ alpha.
- Mỗi video có một `VideoPlayer` và `RenderTexture` 960×540 khi tệp tồn tại; splash chỉ khởi tạo decoder từ mốc 27 giây. Đây là mức thử nghiệm, chưa có đo GPU/VRAM/giật hình trên CMP 40HX.
- `StreamingAssets` khiến dung lượng build tăng theo kích thước nguyên gốc của MP4, **RenderTexture không làm nhẹ tệp build**. Tránh video dài có bitrate quá cao.
- Nếu codec không phát được, Console ghi cảnh báo và vẫn dùng lớp màu dự phòng.

## 4. Checklist Play Mode và Windows

1. Pull `feature/0.0.53`, mở Unity: kiểm tra không có lỗi biên dịch; `Assets/Resources/OpeningTitleLogo.png` được nhận dưới dạng Sprite.
2. Chưa có MP4: thấy logo chữ đúng tên phía trên, các khối màu mây/núi; sau 27s xuất hiện khung màu splash cố định.
3. Thêm `Assets/StreamingAssets/OpeningBackground/opening.mp4`, thử video lặp toàn màn hình, nhưng menu/mây vẫn ở phía trước.
4. Thêm `Assets/StreamingAssets/OpeningSplash/splash.mp4`, sau giây 27 khung fade in 1,4s, video chạy; đợi lâu xác nhận KHÔNG tự pan/zoom/cắt clip.
5. Ở màn Cài đặt đổi độ phân giải 600/800/1150: video nền luôn full viewport; splash ở giữa giữ nguyên tỉ lệ khu vực, không lệch nút.
6. Chơi mới/Tiếp tục: video dừng, hiệu ứng lộ gameplay vẫn đúng, không sai tọa độ chiến đấu.
7. System Menu → Quay lại màn hình chính: opening trở lại, logo đúng, video nền phát lại; splash lại chờ từ giây 27 rồi mới fade in.
8. Kiểm tra bộ nhớ/video decoder và kích thước bản Windows trước khi coi tính năng hoàn thiện.

**Giới hạn xác minh:** mã đã commit và rà soát tĩnh; chưa có Unity Editor, Windows Build hoặc tệp MP4 chính thức trong môi trường chạy kiểm thử.
