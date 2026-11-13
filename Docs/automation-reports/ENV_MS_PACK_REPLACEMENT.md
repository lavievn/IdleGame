# Thay bộ âm thanh môi trường bằng env_ms.zip

- Commit mã và asset `320fceeceed5864d430621ef2718da6dd531179d`.
- Nhánh `feature/rc2-audio-opening-quest`, [Draft PR #27](https://github.com/lavievn/IdleGame/pull/27).
- SHA-256 của ZIP `650aa81e8cc3d9d4b6ad668e6338d7001c11a825a3c05d25be1cc2f80037918a`.

Thư mục `Assets/Resources/IntegratedAmbience` có đúng 191 OGG theo tên gốc và SHA-256 trong ZIP. 18 tệp đổi tên mang theo `.meta` cùng GUID cũ. 173 tệp đã có đúng tên và checksum. [Bảng kiểm kê](ENV_MS_INVENTORY.csv) trỏ tới đường dẫn hiện tại.

Scheduler nạp trực tiếp tên file trong manifest. Đã gỡ catalog phát theo nhóm cũ và các đường phát fallback. Công cụ restore bỏ qua ambience của freeze khi thấy manifest mới, đồng thời vẫn kiểm tra asset HDR, Rain Maker và BGM theo manifest RC2.

178 clip được scheduler phát theo quy tắc đã duyệt. 12 clip trong ZIP còn chờ quy tắc và được giữ trong bộ asset. `GateThunder.ogg` nằm trong ZIP và vẫn là tiếng sấm riêng của Thiên Quan.

## Kiểm thử

- Tên file, CRC và SHA-256 của 191 OGG khớp ZIP. 18 cặp OGG và `.meta` được Git nhận diện là rename 100%.
- Checker Unity báo 0 lỗi, 0 cảnh báo trên 1.730 tệp.
- Restore kiểm tra 95 mục tài nguyên RC2 liên quan, 0 file cần khôi phục.
- Parser/scheduler: 35 PASS.
- MainGame Play Mode: 14 nhóm PASS, gồm khí hậu, mưa, fade audio, Travel và Save/Load/Retry. Log `D:/IdleGame/Logs/Rc2OpeningAcceptance-NewPackWeather-r2.log`.
- Windows build: PASS, 0 lỗi, 58 cảnh báo. EXE `D:/IdleGame/Builds/Rc2NewEnvironmentPack/build/IdleGameV10.exe`.
- Windows smoke D3D11: 9 nhóm gameplay PASS, gồm tên clip mới, Rain Maker, fade lúc Travel, Save/Load/Retry và sấm Thiên Quan. Player ghi kết quả rồi gặp lại lỗi thoát `UnityPlayer.dll 0xc0000005`. Bằng chứng tại `D:/IdleGame/Builds/Rc2NewEnvironmentPack/profiles/idlegame-v10-0996d8a0cc8041ceb22cc2af9c888d0e`.
- Nghe trực tiếp: NOT RUN.
