# 0.0.50 — B2 gameplay/hotfix (12/10/2026)

## Phiên bản

- Unity `ProjectSettings/ProjectSettings.asset`: `bundleVersion: 0.0.50`.
- Bản B2 `feature/b2-combat-target-routing` đã được hợp nhất vào `main` trong commit phát hành 0.0.50.
- Bản người phát triển xác nhận chạy trước đó: B2 có nút gọi tối đa hai đồng đội thử nghiệm và hình ảnh nền thị sai. Các lỗi dưới đây do người phát triển phát hiện trong Unity Editor.

## Ba điều chỉnh của 0.0.50

1. **Quái đánh xa không đánh ngoài màn hình.** `MonsterController.CanAttack` trả false khi quái Cung/Phép không nằm trong vùng màn hình theo X; AI di chuyển từ mép trái vào màn hình trước khi quyết định dừng lại đánh. `CombatManager` dùng lại điều kiện `CanAttack` cho bắn và tích lực, nên không tạo đạn ngoài khung hình. Quái cận chiến giữ nguyên.
2. **Hero chết biến mất hoàn toàn, không còn xác chạy theo camera.** `HeroController.Die()` rút Hero khỏi danh sách sống và danh sách hiển thị, ẩn `heroRect`. `IsCameraSubject` chỉ đúng với Hero đang sống/được triển khai. `EnvironmentManager` không còn nhánh đặc biệt cuộn xác. Khi Hero chính chết nhưng còn đồng đội, camera bám người còn sống; khi chết hết, không còn mục tiêu camera, dừng pan và mở Game Over theo GameManager cũ. Retry vẫn spawn bình thường.
3. **Camera tiếp tục tiến tới vùng đỏ phía phải khi đội đứng đánh.** `EnvironmentManager.advanceStationaryCombatToRight=true` (có thể tắt ở Inspector để so sánh) chỉ chạy khi tất cả Hero còn sống đứng yên và ít nhất một Hero đang đánh. Theo Hero ngoài cùng bên phải (người đi sau trong hướng đi sang trái), camera đẩy Ground + toàn bộ đối tượng tới `ZoneRightX`, qua hàm lò xo giảm chấn `BattleMotion.StationaryCombatPan` với `stationaryCombatSmoothTime=1.5` đã lưu trong Scene (mã nguồn vẫn đặt giá trị mặc định ban đầu `0.6` nếu tạo component mới). Khi đội di chuyển, giữ nguyên `DeadzoneCamera` cũ. Camera không vượt ZoneRightX và dừng khi tới mục tiêu. Không thay các tầm đánh, hitbox, hệ tọa độ ổn định hay nội suy điểm trúng đòn.

## Kiểm thử

- Thêm `Tests~/Version0050Regression.cs`: kiểm tra quái bắn từ ngoài màn hình, Hero chết ẩn hình và nhả camera, còn một đồng đội sống thì vẫn có camera, camera đứng đánh dần đưa Hero sau cùng về gần ZoneRightX, lò xo không vượt đích.
- Thay bài `Tests~/MotionRegression.cs` cũ có kỳ vọng "corpse follows camera" vì quy tắc này đã bỏ.
- **Người phát triển đã thử bằng Unity Editor và xác nhận:** quái đánh xa phải vào màn hình, Hero chết biến mất đúng yêu cầu; camera đứng đánh tạm ổn với thông số đã tinh chỉnh. **Chưa thử camera khi Hero chạy tốc độ cao.** Môi trường cập nhật GitHub chưa chạy Unity/Mono regression.

### Đề nghị test Unity

1. Quái Cung/Phép spawn trước mép trái, tầm đánh bao phủ Hero: không được phóng đạn/tích phép ở ngoài cửa sổ, tiến vào màn hình rồi mới bắt đầu đánh.
2. Gọi đủ ba Hero, gây chết từng người: người chết biến mất tức thì, các đồng đội còn sống đánh tiếp. Khi toàn bộ chết, camera ngừng cuộn và game over xuất hiện. Retry phải không có xác cũ và không giữ camera velocity.
3. Hero Cung/Phép đứng đánh khi còn khoảng trống bên phải: camera đưa **Hero còn sống nằm ngoài cùng bên phải** tới gần vùng đỏ phải với tốc độ tăng/giảm mượt; không nhảy, lắc, hoặc lố biên. Khi toàn đội đi tiếp, chuyển về thuật toán camera mềm hiện tại.
4. Kiểm tra Hero chạy tốc độ nhanh/chậm, pause/continue, các kích cỡ cửa sổ 600/800/1150 và hiện tượng quái thoát biên.

## Bộ thông số camera đã chốt tạm trong `Assets/Scenes/MainGame.unity`

- Hierarchy: `EnvironmentManager` → Inspector: `Environment Manager (Script)`.
- `Speed Zone Left = 0.196`; `Speed Zone Right = 0.91`; `Speed Zone Inset = 0.25`.
- `Camera Smooth Time = 0.2`; `Camera Deadzone Ratio = 0.38`; `Camera Idle Rest X = 0.4`; `Camera Idle Smooth Time = 0.6`.
- `Advance Stationary Combat To Right = true`; `Stationary Combat Smooth Time = 1.5`.
- `Camera Use Combat Center = true`; `Camera Combat Focus Smooth Time = 1.5`; `Camera Combat Focus Max Speed = 240`.
- Các mục có thể điều chỉnh trực tiếp trong Inspector khi tắt Play Mode; chưa chốt thông số camera cho Hero tốc độ cao.
