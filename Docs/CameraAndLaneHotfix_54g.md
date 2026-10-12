# 0.0.4.54g — Hotfix camera tâm nhóm và trục Y của Hero

**Ngày triển khai:** 12/10/2026. **Nhánh thử riêng:** `fix/0.0.4.54g-smooth-focus-free-y`, bắt đầu từ `feature/0.0.4.54g` tại `02801dd2b1fe4ca46ce2402ffdbe93de590a2b58`. Không chỉnh gameplay trên `feature/0.0.4.54g`, B1, B2 hoặc `main` trước khi người phát triển thử và xác nhận.

## Lỗi 1 — Camera giật / lao nhanh khi quái vừa vào màn hình

**Nguyên nhân xác định trong mã:** `EnvironmentManager.CombatCenterX` lấy **trung bình cộng X** của Hero đang hoạt động và quái còn sống nằm trong viewport. Tại thời điểm một quái bước qua rìa, số hạng/ước số của trung bình cộng thay đổi đột ngột. Camera .54g nhận **tọa độ trung bình thô** trực tiếp qua `DeadzoneCamera.Pan`, nên có thể tăng tốc mạnh vì tâm nhảy xa. Phép trung bình đó vẫn là phép chọn tâm hiện tại; hotfix không đổi thành trung điểm hai đầu.

**Cách sửa:** thêm `CombatFocusSmoother` trong `BattleMotion.cs`. Nó làm mượt **độ lệch X của trung bình nhóm so với Hero đang được camera theo**, không lọc toàn bộ tọa độ Hero:

`rawOffset = CombatCenterX(heroX) - heroX`

`filteredFocusX = heroX + CombatFocusSmoother.Step(rawOffset, dt, smoothTime, maxSpeed)`

Việc dùng độ lệch tương đối giữ khả năng camera bắt Hero chạy nhanh như trước, trong khi biến động thành phần quái (vào viewport, chết, despawn) được lọc. Giữ nguyên `CurrentCameraFocusX` là **tâm thô** cho việc kiểm tra; thêm `CurrentCameraSmoothedFocusX` hiển thị **tâm đã lọc** thực tế đưa vào Deadzone. Lõi Deadzone/SmoothDamp và một lần pan chung cho Hero/quái/đạn/Ground **không thay đổi**.

- `cameraCombatFocusSmoothTime = 0.7` giây: thời gian phản hồi mượt của độ lệch nhóm.
- `cameraCombatFocusMaxSpeed = 240` đơn vị/giây: ngưỡng tốc độ tối đa **riêng cho biến thiên độ lệch tâm nhóm**, không phải giới hạn vận tốc chạy Hero hay tốc độ kéo camera để bảo vệ mép thật.
- Đã ghi hai trường này trực tiếp trong `Assets/Scenes/MainGame.unity`; người phát triển chỉnh ngay trong Inspector nếu muốn nhanh/chậm.
- Khi đổi chế độ camera, Hero spawn/retry, hoặc không còn chủ thể, trạng thái lọc và vận tốc được reset; pause không làm tăng đà. Không ép về một phía khi Hero đứng yên.
- Giữ `cameraUseCombatCenter=true` (có thể OFF để so sánh camera một Hero), Deadzone=38%, cameraSmoothTime=0.2s, cameraIdleRestX=40%, cameraIdleSmoothTime=0.6s và toàn bộ phép tính nhóm/quái ngoài màn hình như .54g.

## Lỗi 2 — Hero không có mục tiêu tự tụt xuống mép Y=0

**Nguyên nhân xác định:** trong `HeroController.TickMovement`, khi `CurrentTarget == null`, mã cũ có `p.y = Mathf.MoveTowards(p.y, 0f, laneSpeed * dt)`. Vì vậy Hero luôn tự tìm về Y=0 dù không hề đuổi quái.

**Cách sửa:** nhánh không mục tiêu chỉ thực hiện `p.x -= moveSpeed*dt`; giữ nguyên `p.y`. Nhánh có mục tiêu vẫn giữ cơ chế cận chiến tiến gần quái theo Y **khi đang ở ngoài tầm ngang**; đánh xa vẫn không cần căn Y để gây sát thương (quy tắc tầm đánh theo X của .54g). `SpawnHero` đặt nhân vật tại HomeX,Y=0 như logic spawn trước đó; hotfix không đổi vị trí hồi sinh.

## Kiểm thử / nghiệm thu

- Bộ kiểm thử `Tests~/CameraFollowRegression.cs` cập nhật những điều kiện từng kỳ vọng camera giật ngay frame đầu, thêm hai bài: tốc độ và hội tụ bộ lọc (30/60/144 FPS), quái chuyển từ ngoài vào trong viewport mà **tâm thô vẫn là trung bình cũ**, camera mượt trước/sau quái chết.
- `Tests~/CameraAndLaneHotfixRegression.cs`: bốn bài: không quái giữ Y ở 30/60/144 FPS; cận chiến đổi Y khi áp sát rồi giữ Y sau khi quái chết; đánh xa giữ lane; hai Hero không mục tiêu giữ lane Y độc lập. Bộ `Tests~/MotionRegression.cs` gọi suite bổ sung. `Tests~/run.sh` vốn gom `Tests~/*.cs`, không cần đổi.
- Mô phỏng số học độc lập của bộ lọc với cú nhảy -350 đơn vị: tốc độ thay đổi độ lệch đỉnh khoảng 233–238 đơn vị/giây ở 30/60/144 FPS; sau 3 giây hội tụ gần -348, sau khi mục tiêu về 0 thì dịch từ từ về 0. **Chưa chạy C#/Unity/Windows**, không gọi là nghiệm thu.
- Kiểm thử Unity quan trọng: (1) 1 quái lẻ vào màn hình, (2) 3 quái vào cùng lúc, (3) quái chết/hết wave liên tiếp, (4) Hero tốc độ 700 cận/xa đi rồi dừng, (5) Hero đang ở lane Y bất kỳ khi hết quái rồi bắt đầu wave mới, (6) 600/800/1150px và thời gian dừng bắn, (7) xác nhận camera không giật/không mất đạn và HP bình thường.

## Tách hotfix khỏi B1/B2

Không tự merge `feature/b2-combat-target-routing` hoặc `feature/b1-hero-combat-state`. Sau khi bản sửa từ checkpoint .54g được chấp nhận, có thể **đồng bộ thay đổi tương ứng sang B1 và B2** trên nhánh riêng, giải quyết xung đột tường minh vì cả ba đều dùng `EnvironmentManager.cs`/ `HeroController.cs` nhưng được phát triển độc lập. Không phát triển tiếp tính năng B2 trên hotfix này.
