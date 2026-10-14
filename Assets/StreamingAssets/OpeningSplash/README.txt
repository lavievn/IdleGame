LƯU Ý: thư mục cũ dùng làm dự phòng tương thích.

Ưu tiên sử dụng kho ngẫu nhiên mới:
Assets/StreamingAssets/OpeningSplashArt/
Thêm các tệp PNG/JPG/MP4 tùy ý vào đó, game sẽ chọn riêng cho
khung trái và phải (đều tràn ra ngoài mép cửa sổ).

Nếu kho mới trống, hệ thống vẫn nhận splash.mp4 trong thư mục cũ,
và phát cùng tệp trên cả hai bên.

Lịch sử: trước đây chỉ có một video splash ở giữa.

Thêm splash.mp4 vào:
Assets/StreamingAssets/OpeningSplash/splash.mp4

Định dạng: MP4/H.264, không sử dụng âm thanh riêng.
Mọi chuyển động của nhân vật, zoom, speed ramp, chuyển cảnh
được biên tập sẵn vào splash.mp4. Unity chỉ fade in một lần ~1.4s.
Video tự lặp trong màn hình chờ.
Không có video -> khung màu có nhãn để dễ kiểm thử.
Khi vào gameplay -> dừng decode. Quay lại menu -> chờ 27s từ đầu.

Video này KHÁC opening.mp4 nền dưới cùng:
Assets/StreamingAssets/OpeningBackground/opening.mp4
