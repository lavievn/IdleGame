# Morning chỉ phát ở Rừng rậm khi trời khô

- Commit mã `9a65963d1b765b696178b64e1e92aaa34484e7d6`.
- Tối đa một clip `Morning_` trong khoảng 05:00–12:59. Chọn ngẫu nhiên clip khác sau 120 giây nếu có. Clip cũ fade hết trước khi clip mới vào.
- Chỉ 27 map Rừng rậm đủ điều kiện. Mưa nhẹ và mưa lớn loại Morning khỏi lịch phát. Clip đang phát fade theo thời lượng hiện có, mặc định 2 giây.
- 53 map Đồi núi hoặc Cao nguyên trước đây có Morning giờ dùng Primeval theo điều kiện đã có. Huyền Ảnh Cốc (`TV-21`) thuộc nhóm này.
- [Kiểm kê clip](ENV_MS_INVENTORY.csv) và [bảng map](ENV_MS_MAP_FAMILIES.csv) đã phản ánh quy tắc mới.

## Xác minh

- 45 kiểm tra domain PASS, gồm rừng khô, mưa nhẹ, mưa lớn và đồi núi.
- MainGame Play Mode PASS. Cô Hồn Lâm không có Morning khi mưa lớn, có đúng một nguồn khi bình minh quang. [Log](<D:/IdleGame/Logs/Rc2OpeningAcceptance-MorningDryForest.log>).
- Windows Development build PASS, 0 lỗi, 58 cảnh báo. [Launcher hồ sơ riêng](<D:/IdleGame/Builds/Rc2MorningDryForest/build/Play-Climate-Audio.cmd>).
- Windows smoke Hàn Vụ PASS cho các bước gameplay và AudioSource, gồm Cô Hồn Lâm mưa nhẹ/mưa lớn, Huyền Ảnh Cốc khô/mưa lớn. Player ghi kết quả rồi tái hiện lỗi thoát `UnityPlayer.dll 0xc0000005`. Bằng chứng tại `D:/IdleGame/Builds/Rc2MorningDryForest/profiles/idlegame-v10-d8fd0bec88c64f63b1f9647a7a650fd9`.
- Nghe phối âm trực tiếp: NOT RUN.
