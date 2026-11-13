# Điều chỉnh âm nền Morning

- Commit mã `299a8f85a570758a1542959874a76c721777a85b`.
- Các map rừng rậm và đồi núi phát tối đa một clip `Morning_` từ 05:00 đến trước 13:00 theo đồng hồ game. Áp dụng cho cả giờ preview.
- Giữ nhịp xoay 120 giây. Scheduler chọn clip khác khi có lựa chọn và để nguồn hiện tại fade hết trước khi phát nguồn kế tiếp. Khi ra khỏi khung giờ, nguồn Morning dừng ngay.
- 19 clip Morning đã duyệt giữ gain hiện tại. Các lớp mưa, gió, sấm, Night và Primeval tiếp tục dùng quy tắc riêng.

## Kiểm thử

- Domain PASS, 41 bài gồm ranh giới 05:00/13:00, đổi clip và Rain độc lập.
- Unity MainGame Play Mode PASS. Cô Hồn Lâm mưa lớn lúc 22:00 có Rain và Morning im. Bình minh cùng điều kiện mưa có một nguồn Morning. Log `D:/IdleGame/Logs/Rc2OpeningAcceptance-MorningHours.log`.
- Windows Development build PASS, 0 lỗi. [Launcher hồ sơ riêng](<D:/IdleGame/Builds/Rc2MorningHours/build/Play-Climate-Audio.cmd>).
- Windows smoke D3D11 PASS các bước gameplay, gồm đêm và bình minh mưa lớn tại Cô Hồn Lâm. Lỗi thoát `0xc0000005` vẫn tái hiện. Bằng chứng tại `D:/IdleGame/Builds/Rc2MorningHours/profiles/idlegame-v10-8128e6e6594249139b570bd27ae5995c`. Nghe trực tiếp NOT RUN.
