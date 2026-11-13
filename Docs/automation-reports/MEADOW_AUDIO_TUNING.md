# Điều chỉnh âm thanh Meadow

- Commit triển khai `cae39d77bf5384e22a7e644e9ba9846061485b8c`.
- [Danh sách 133 map](ENV_MS_MAP_FAMILIES.csv) có 19 map dùng Meadow. `TV-20` dùng Meadow khi là Hàn Vụ thôn của tuyến mở đầu. Smoke đối chiếu tập 19 map với catalog runtime.
- Gain của 17 clip đã duyệt giảm từ 0,50 xuống 0,30, tương đương giảm 40% so với mức trước khi nhân các thanh âm lượng. [Kiểm kê](ENV_MS_INVENTORY.csv).
- Toàn bộ prefix Meadow dùng tối đa một nguồn phát, tính cả clip đang fade. Khi khô, clip nền hoặc cricket dùng chung một slot và đổi sau 300 giây. Khi Rain, một biến thể mưa dùng slot đó. Trong Storm, Meadow vẫn im theo cấu hình hiện tại. Các overlay toàn cục giữ giới hạn riêng.

## Kiểm thử

- 35 kiểm tra domain PASS.
- MainGame Play Mode: PASS, gồm gain thực 0,30 trên AudioSource, giới hạn khi xoay clip, và chuyển khô → mưa → khô. Log `D:/IdleGame/Logs/Rc2OpeningAcceptance-MeadowOne.log`.
- Windows Development build: PASS, 0 lỗi, 58 cảnh báo. `D:/IdleGame/Builds/Rc2MeadowOne/build/IdleGameV10.exe`.
- Windows smoke D3D11: 9 nhóm gameplay PASS. Xác nhận 19 map, một nguồn Meadow lúc khô và mưa, gain mới và Rain overlay độc lập. Player gặp lại lỗi thoát `0xc0000005` sau khi ghi kết quả. Bằng chứng ở `D:/IdleGame/Builds/Rc2MeadowOne/profiles/idlegame-v10-292d3a4b23d64c2294ad283f87f0eae2`.
- Nghe phối âm trực tiếp: NOT RUN.
