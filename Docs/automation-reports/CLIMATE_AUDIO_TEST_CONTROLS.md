# Nút thử thời gian và thời tiết

- Commit mã `59dab0d0930913b3c4f8720cd7ff4cc3a7d518ad`.
- Hai nút nằm ở góc dưới bên trái của HUD Unity Editor và Windows Development. Nút dùng hệ xử lý click hiện có.
- **Giờ** xoay giữa Tự động, Bình minh (06:30), Ban ngày (12:00), Hoàng hôn (18:30) và Ban đêm (22:00).
- **Trời** xoay giữa Tự động, Quang, Nhiều mây, Sương, Mưa nhẹ và Mưa lớn.
- Hai lựa chọn độc lập. Chọn Tự động trên từng nút để trả quyền điều khiển về khí hậu tự nhiên. Preview giữ qua Travel để so âm thanh các map và trở về mặc định ở màn hình chính. Save vẫn ghi giờ mô phỏng. Nút ẩn trong Thiên Quan, và thời tiết trong nhà vẫn theo quy tắc trong nhà.

## Kiểm thử

- Unity 6000.5.0f1 MainGame PASS. Hit-test qua tọa độ màn hình ở 600×338, 800×450, 1150×647. Test xác nhận Night và Rain thay đổi theo nút, âm thanh Meadow giữ giới hạn một clip và Save/Load giữ giờ mô phỏng.
- Log MainGame `D:/IdleGame/Logs/Rc2OpeningAcceptance-ClimateButtons.log`. Ảnh `climate-test-controls.png` trong `D:/IdleGame/Projects/Rc2AudioOpeningValidation/.utmp/foundation-7072899d78dc462091acb6444da354cb/evidence`.
- Windows Development build PASS, 0 lỗi, 58 cảnh báo. Launcher profile thử riêng `D:/IdleGame/Builds/Rc2ClimateButtons/build/Play-Climate-Audio.cmd`.
- Windows smoke D3D11 PASS cho các bước gameplay. Player tái hiện lỗi `UnityPlayer.dll 0xc0000005` sau khi ghi kết quả. Bằng chứng tại `D:/IdleGame/Builds/Rc2ClimateButtons/profiles/idlegame-v10-ba45f929192b4585b58de648f8790bf8`.
- Chuột/phím vật lý và nghe phối âm trực tiếp: NOT RUN.
