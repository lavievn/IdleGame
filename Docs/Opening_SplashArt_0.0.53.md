# Opening .53 — Kho splash art ngẫu nhiên (ảnh / video)

**Trạng thái:** mã đã commit trên `feature/0.0.53`; chưa biên dịch hoặc xem trực tiếp trong Unity Play Mode/Windows. Không đưa video tham chiếu của người phát triển vào build.

## Thêm tài nguyên

**Thư mục duy nhất để thêm:** `Assets/Resources/OpeningSplashArt/`.

1. Pull nhánh `feature/0.0.53`, mở Unity Editor, tìm thư mục trên trong cửa sổ Project.
2. Thêm bất kỳ số lượng **PNG/JPG** hoặc **MP4/H.264** vào đó. Không cần đặt tên `Left`/`Right`; hệ thống tự chọn ngẫu nhiên.
3. **Với ảnh:** chọn từng texture trong Inspector, `Texture Type: Sprite (2D and UI)`, `Sprite Mode: Single`, nhấn Apply. Cân nhắc ảnh tối đa khoảng 1024–1536 px cạnh dài cho splash; nên sử dụng nén sprite được Unity hỗ trợ. Ảnh PNG nền trong suốt phù hợp splash nhân vật cắt nền, JPG phù hợp ảnh toàn khung.
4. **Với video:** ưu tiên `.mp4` mã hóa H.264, độ phân giải **720×1280 trở xuống**, 24–30 fps, bitrate vừa phải, thời lượng ngắn (ví dụ 6–12 giây). Video có tiếng vẫn bị tắt tiếng trong opening để tránh đè nhạc `opening.ogg`. Không trông đợi alpha trong MP4; phần video được hiển thị trong ô chữ nhật và có thể che lớp núi/mây bên dưới.
5. Unity tự chạy `OpeningSplashCatalogBuilder` sau khi import. Trong Console sẽ có log `[OpeningSplash] Catalog rebuilt: X images, Y videos`. Có thể làm mới thủ công tại **Tools → IdleGame → Rebuild Opening Splash Catalog**.
6. Kiểm tra `Assets/Resources/OpeningSplashCatalog.asset` có danh sách `images` và `videos`. Nếu vẫn là 0, thường do ảnh chưa để Sprite, video import lỗi hoặc để sai thư mục.
7. Khi push cho máy khác cần đưa lên GitHub **các ảnh/video, tệp .meta tương ứng VÀ `OpeningSplashCatalog.asset` đã được Unity tự cập nhật**.

Không chỉnh `OpeningArtTheme.asset` để thêm ảnh ngẫu nhiên: asset đó chỉ giữ **các lớp cảnh cố định** (bầu trời, mây, núi, ảnh dự phòng khi chưa có kho, logo). Kho hình động quản lý tách biệt.

## Hoạt động

- Opening giữ nguyên mây trôi, bốn nút, menu Cài đặt, âm thanh và cơ chế chuyển vào gameplay đã có.
- Sau **27 giây** mới bắt đầu showcase. Nếu kho trống, hai placeholder splash cũ vẫn hoạt động; không báo lỗi hoặc hiện ô trắng.
- Có dữ liệu: chọn ngẫu nhiên ở hai bên, cố tránh trùng liên tiếp và tránh hai bên dùng cùng một tài nguyên nếu đủ lựa chọn.
- Mỗi lượt ảnh/clip giữ khoảng **4,8–7,4 giây** (ngẫu nhiên); chuyển lượt bằng **mờ chồng ~0,7 giây**. Hai bên lệch nhịp khoảng 1,65 giây để không đổi đồng loạt.
- Mỗi shot chọn ngẫu nhiên một trong **4 kiểu máy quay giả lập**: zoom nhẹ, zoom nhanh vào chi tiết (speed ramp), kéo từ gần ra xa, trôi ngang kết hợp zoom. Phóng to được cắt trong ô splash riêng, không thay đổi camera chiến đấu.
- Video chọn ngẫu nhiên từ kho, chạy bằng `VideoPlayer` vào `RenderTexture` 512×896; **tối đa một luồng video** ở cùng thời điểm. Khi bên trái đang phát video, bên phải ưu tiên ảnh; nếu kho toàn video, hai bên luân phiên. Hiện tại playback không yêu cầu chạy hết clip nếu thời gian trưng bày ngắn hơn; video có thể lặp nếu ngắn.
- Khi người chơi Chơi mới/Tiếp tục, showcase tiếp tục đi theo động tác mây dạt hai bên; kết thúc chuyển vào gameplay thì ngừng phát video. Khi chọn **Quay lại màn hình chính**, trạng thái showcase làm mới.

Các chuyển động là **bản prototype theo cảm giác** từ video tham khảo dài khoảng 11,4 giây, không phải sao chép chính xác chuyển động hoặc sử dụng hình nhân vật từ video đó.

## Giới hạn và lưu ý

- `Resources` đóng gói **toàn bộ ảnh và video** trong thư mục vào build. File MP4 lớn sẽ **làm tăng dung lượng game** dù chỉ giải mã một video khi phát. Khi kho quá lớn, nên cân nhắc chuyển video sang `StreamingAssets`/Addressables, nhưng không cần tăng độ phức tạp này ngay ở bản thử.
- Nếu video không được giải mã trên Windows/driver đang sử dụng, mã sẽ ghi cảnh báo và trả về khung dự phòng. Nên có một vài PNG/JPG để bảo đảm luôn có splash art.
- RenderTexture 512×896 chỉ là độ phân giải trình chiếu, không giảm kích thước các video gốc trong build.
- Ảnh và video chỉ được quét **khi khởi động scene/bộ opening**. Thêm file khi game đã chạy cần dừng Play Mode, cho Unity import và chạy lại.
- Không có camera 3D, shader đặc biệt, mô phỏng tóc/áo hay dựng video offline; ảnh chỉ được di chuyển/phóng to bằng UI. Chuyển cảnh video→video có thể có nhịp chớp ngắn khi một luồng giải mã được gán clip mới, sẽ đánh giá sau khi có asset thật.

## Kiểm thử

1. **Kho trống:** chạy opening, sau 27s vẫn có hai khối màu cũ.
2. **Kho 3 PNG/JPG:** đặt 3 Sprite, Rebuild, chạy, kiểm tra cả hai bên chọn khác nhau (phần lớn các lượt), đổi ảnh và zoom mà không đè bốn nút.
3. **Kho 1 MP4 + ảnh:** sau mốc 27s video có hình/mượt và không phát tiếng riêng. Kiểm tra Console cảnh báo về codec.
4. **Kho toàn MP4:** vẫn chỉ có một video đang chạy, hai bên lần lượt thay nhau trưng bày; kiểm tra CPU/GPU và tình trạng giật.
5. **Thay đổi kích thước cửa sổ** 600/800/1150 ngay lúc showcase; vùng ảnh giữ trong màn hình và không phá tọa độ combat.
6. **Vào game:** video dừng, opening fade và BGM chuyển đúng; trở lại title rồi chờ 27s, kho random lại và không làm nhân đôi `VideoPlayer`.
7. **Build Windows trên CMP 40HX:** kiểm tra codec MP4/H.264, cảm giác speed-ramp và độ mượt khi 2 bên chuyển ảnh. Đây là bước nghiệm thu bắt buộc; môi trường trợ lý không có Unity/Windows để chạy thử.
