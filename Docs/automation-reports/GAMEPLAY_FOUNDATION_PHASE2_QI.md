# Phase 2 — Linh khí môi trường

- DEV lấy tốc độ nền theo từng map từ Atlas v2. Map chiến đấu dùng ba dải theo độ sâu Y của chân Hero trong BattleArea. 25% gần là thưa (×0,70), 50% giữa là chuẩn, 25% xa là đậm (×1,35). Hàn Vụ thôn dùng mức chuẩn vì Hero đứng yên. Hệ số là cấu hình DEV. Chưa gắn mật độ với spawn hoặc sức mạnh quái.
- Cùng bộ đếm cultivation xử lý thời gian chủ động. Khi đổi vùng, phần thời gian lẻ được xử lý ở nguồn cũ trước khi đổi nguồn. Save tiếp tục lưu ID nguồn và phần thời gian lẻ bằng trường có sẵn. Không thưởng thời gian offline. Không đổi Atlas production, Weather hay Audio.
- Dùng lại mốc Luyện Khí 0 / 900 / 2100 / 3600. Ở Hàn Vụ mức chuẩn 1 Qi/s, mô phỏng từ Sơ kỳ tới Viên mãn mất **3.600,1 giây**, lệch 0,1 giây do bước cố định và làm tròn. EXP vẫn bằng 0. Trúc Cơ và Gate HP/damage giữ cấu hình DEV hiện có.
- **PASS**. Domain 54/54. Kiểm tra ranh giới, nguồn thưa/đậm, pause, Save/Load, đổi map và bảo toàn Qi. Log `D:\IdleGame\Logs\GameplayFoundation-Phase2-Domain.log`.
- **PASS**. Unity MainGame với config mới: New Game sinh tu vi, hồ sơ thử gần bình cảnh tiếp tục tới cap qua đồng hồ thật, Kiếm thắng và tay không thua Gate. Log `D:\IdleGame\Logs\Unity-MainGame015-20261114-015229.log`. Một lượt trước phát hiện Unity JsonUtility tạo object lồng trống khi field thiếu. Đã dùng cờ kích hoạt rõ trong config và kiểm tra lại.
- Cân bằng DEV mới có fingerprint riêng theo cơ chế hiện hành. Save DEV cũ và các hồ sơ đã nghiệm thu vẫn còn ở đường dẫn ban đầu. Không tự di chuyển chúng sang cân bằng mới.
