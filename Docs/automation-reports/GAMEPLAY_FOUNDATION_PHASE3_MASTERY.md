# Phase 3 — Mastery vũ khí và kỹ năng

- Runtime cộng một đơn vị đếm khi Basic Attack hoặc kỹ năng được thực thi. ID vũ khí theo family và ID kỹ năng lấy từ registry. Một đòn AoE hay ba mũi tên từ cùng một lần Release vẫn chỉ cộng một đơn vị kỹ năng. Các lượt hủy không được tính.
- Receipt có kiểm tra owner, đời Hero, lần spawn, phiên chiến đấu và số Release. Hành động mới sau khi Load được tính cho timeline mới. Mastery không cấp Level, sát thương, tu vi hoặc EXP.
- Snapshot v1 được gắn thành component tùy chọn của Hero. Save cũ thiếu field bắt đầu với bộ đếm trống. Save sai cấu trúc hoặc khác version bị từ chối trước khi nạp/ghi đè. Không tăng SaveVersion tổng.
- **PASS**. Domain 56/56, kiểm tra đổi vũ khí, nhiều skill, duplicate/stale/invalid, độc lập với EXP và tu vi. Log `D:\IdleGame\Logs\GameplayFoundation-Phase3-Domain.log`.
- **PASS**. Unity MainGame handler exit 0. Staff basic, Sword AoE, Bow ba projectile, Fireball, Save/Load, Save cũ, Death/Retry và Travel. Lượt thử đầu phát hiện fixture cũ còn đứng ở làng an toàn. Đã dùng Travel thật đến rừng. Đường về dùng trạng thái đã hoàn thành map trong hồ sơ thử. Log `D:\IdleGame\Logs\Unity-MainGame016-20261114-015724.log`.
