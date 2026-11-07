# Thiên Kiếp 0.1.4 — môi trường, Thiên Quan và Thiên Lôi

## Phạm vi

Môi trường gắn với encounter đang chạy trong MainGame. Cấu hình production của Thiên Kiếp tiếp tục khóa. Owned fixture dùng cùng cấu hình chiến đấu và chính sách căn cơ với Development 0.1.3. Bản này giữ nguyên phiên bản receipt, dữ liệu Save và dấu vân tay của cấu hình encounter.

## Các lớp hiển thị

1. Bầu trời giông sét ở nền trong vùng gameplay. Video A mặc định, B và Still là tùy chọn của trường `trialSky` trong CombatManager. Hai MP4 dùng `StreamingAssets/Tribulation`.
2. Địa hình dùng PNG giữ tỷ lệ gốc. Núi và mặt đất đá đến từ cùng ảnh alpha. Hàng tiếp xúc trong ảnh ở 34,4% tính từ dưới, tại cao độ chân của trial. Các bản kề nhau phản chiếu để mép ảnh trùng khi cuộn và được cắt theo khung gameplay.
3. Hero và Thiên Quan trên BattleArea hiện hữu. Tâm ảnh đá ổn định tại 35% chiều ngang từ trái sau khi camera đưa tường vào khung. Chiều cao 864 đơn vị canvas, bằng 300% kích thước gốc 288 đơn vị. Hạ riêng ảnh tường 100 pixel màn hình. Hero đi thăm dò đủ 800 đơn vị BattleArea trước khi tường xuất hiện ngoài mép trái. Sau đó Hero tiếp tục áp sát từ bên phải bằng tầm đánh theo vũ khí đang trang bị. Điểm nhận đòn giữ cao độ chiến đấu và nằm ở phía phải trong thân đá. Target rect rộng 116 đơn vị và dùng cùng đường xác thực mục tiêu, tầm đánh và projectile của 0.1.3.
4. Lưới cảnh báo, sét và sparks của Combat được vẽ trên các actor, dưới HUD. Hiệu ứng vỡ phong ấn chỉ liên kết với đòn kết liễu và kết quả đã ghi. Lớp kết quả tồn tại ngắn qua chuyển cảnh.
5. HUD hiện hữu hiển thị Hero HP, Thiên Quan HP và kết quả. Các panel quản lý dùng đường render hiện có.

Hình nền thường được ẩn qua từng Image. Khi dọn trial, hệ thống bật lại đúng các Image đã ẩn. Pha thăm dò dùng camera và bộ đếm bước chân giống luồng vào wave. Sau khi đạt mốc 800, camera tiếp tục dịch tường và địa hình cùng một lượng, cho đến khi tường vào vị trí giao chiến. Tường cố định so với địa hình trong suốt pha này. Thay đổi kích thước cửa sổ chỉ đổi phép chiếu của BattleArea. Sau trial, entry/camera/spawn quay lại vòng đời MainGame.

## Video và asset

Sử dụng một decoder và một RenderTexture ARGB32 640×360 cho encounter chạy video. Clip loop theo game-time và tắt audio. `RawImage` crop giữa để phủ khung mà vẫn giữ tỷ lệ. PNG xuất hiện từ trước khi prepare đến lúc decoder giao frame đầu tiên. Nếu file thiếu, không có thiết bị đồ họa, decoder lỗi hoặc chờ quá hạn khi chạy, PNG tiếp tục hiển thị. Pause đóng băng decoder và cảnh báo. Dọn trial gỡ callback, dừng decoder và giải phóng RenderTexture.

Bốn tệp Desktop gốc giữ nguyên byte. PNG dự phòng được trích từ A ở 2,000 giây. SHA-256 và đường dẫn được lưu cùng asset trong [manifest của batch](TRIBULATION_014_ASSET_MANIFEST.json). Manifest này ghi nguồn nhập thực tế. Hình 640×360 nhìn mềm hơn ở cửa sổ lớn. Cảnh sét nằm trong video giữ nguyên màu đen và chi tiết mây.

## Nhạc riêng

Hai bài “逆天而行” và “逆天破劫” dùng Ogg Vorbis streaming từ WAV do người dùng cung cấp. Mỗi lần vào ải chọn ngẫu nhiên một bài với xác suất bằng nhau, rồi lặp bài đã chọn ở âm lượng nhạc chung, kể cả khi tạm dừng. Hai lần vào liên tiếp có thể chọn cùng bài. Bộ sinh ngẫu nhiên của nhạc tách riêng khỏi Combat. Hai kênh nhạc bản đồ tạm dừng tại vị trí phát hiện tại. Khi rời ải, bài riêng dừng và controller tiếp tục nhạc theo bản đồ.

## Hợp đồng strike và Save

- Khi Hero đi vào tầm Thiên Quan, bắt đầu tích lũy thời gian Combat cho lịch sét, hiện warning, sau đó chấp nhận impact. Cấu hình warning fixture mặc định 0,26 giây, tối đa nửa chu kỳ. Sau hitch bỏ backlog thay vì dồn nhiều lần trừ HP, và luôn dành một khoảng warning trước impact.
- Mỗi lịch có strike ID tăng dần. Impact dùng encounter ID, life và spawn của target, life và data của Hero cùng battle version đã đăng ký. Dữ liệu debug chỉ giữ receipt impact gần nhất và bộ đếm tổng. Khi life đổi, combat bị vô hiệu hóa, giữ liability và dọn môi trường.
- Combat lấy vị trí Hero tại impact. Sét thủ tục dùng seed từ strike ID, mảng đỉnh tái sử dụng và màu cảnh báo/va chạm. Video không tham gia lịch Combat. Sau khi phát lệnh vẽ, cùng bước Combat gọi đường `SynergyMath.EvaluateDamage` → `ApplyDamageToHeroState` một lần. Phòng ngự, map scale và HP vẫn qua pipeline hiện hữu.
- Hero tấn công và đạn cũ được xử lý trước bước phản kích trong cùng frame. Nếu impact đã tới hạn và cả hai về 0, outcome hiện hữu xét Hero chết trước. Save kết quả dùng candidate-before-swap và kiểm tra snapshot của 0.1.3. UI không ghi Save theo frame. Retry kết quả, Load và restart dùng lại quy tắc idempotency cũ. [Hợp đồng Save](TRIBULATION_SAVE_TRANSACTION_V1.md)
