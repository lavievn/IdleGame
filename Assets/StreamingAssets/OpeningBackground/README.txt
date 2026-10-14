Video nền opening đặt tại đây.

Tên file mặc định: opening.mp4
Vị trí: Assets/StreamingAssets/OpeningBackground/opening.mp4

Định dạng: MP4 / H.264, không cần âm thanh (nhạc opening.ogg do BGM quản lý).
Đề xuất bắt đầu bằng video 960x540 hoặc 1280x720, 24–30 fps, bitrate vừa phải.
Hệ thống chỉ tạo một VideoPlayer và một RenderTexture 960x540.
Nếu không có opening.mp4, game tự dùng lớp màu bầu trời như trước.
Video lặp khi chờ menu và dừng khi vào gameplay.

Video MP4 nằm trong StreamingAssets nên được chép nguyên vào bản build;
dung lượng game tăng bằng dung lượng file video gốc.
Không đưa file MP4 nặng hoặc video tham khảo vào git nếu chưa muốn.
