# W2.2c — Đội hình di chuyển tối đa 3 Hero

Nền của phần này: commit A `18568949a738c50aa0b5c89286b716de7ca6cad1`.

## Quy tắc và phạm vi

`HeroFormation` được gắn vào cùng GameObject với EnvironmentManager khi Hero
di chuyển lần đầu. Có thể thêm component này trước Play để chỉnh Inspector.
Nó chụp vị trí/tốc độ Hero sống đã đăng ký trong CombatManager một lần mỗi frame, rồi HeroController
áp dụng bước đi tương ứng. Không sửa EnvironmentManager, BattleMotion,
DeadzoneCamera, camera focus, Ground/parallax, MonsterController hay scheduler.

- Một Hero: chạy đúng nhánh di chuyển cũ bằng EffectiveMoveSpeed của mình.
- Có quái hợp lệ theo điều kiện chọn mục tiêu cũ: trả quyền di chuyển cho từng
  Hero; giữ nguyên chọn mục tiêu, tầm đánh, tiếp cận, cooldown và sát thương.
- Không có quái: giữ thứ tự không gian trái → phải, người trước chờ, người sau
  đi tới bằng tốc độ riêng. Không đổi chỗ theo class để tránh đi xuyên đồng đội.
  Cận chiến vẫn tự vượt lên khi tiếp cận quái; đổi thế đánh đổi vai trò giao chiến
  và tốc độ thực tế, không teleport về một slot class mới.
- Khi cự ly đạt ngưỡng, tất cả hành quân bằng min(EffectiveMoveSpeed) của đội.
  Tập hợp chỉ tiến trái hoặc đứng yên; bước đi tối đa = tốc độ riêng × deltaTime.
  Người sau không vượt người trước. Chỉ X thay đổi, giữ Y hiện có.
- Hero chết/ẩn không tham gia; thay roster hoặc kiếp sống lập lại thứ tự.
  ActiveHeroes còn được dùng cho chủ thể camera; đối tượng chỉ phục vụ hình ảnh
  mà chưa đăng ký B2 không làm thay đổi tốc độ của tổ đội giao chiến.
  Quái mới ngắt tập hợp ngay frame di chuyển tiếp theo. Không chờ tập hợp để
  sinh wave. Hero có tốc độ 0 không bị tăng tốc; đội có thể phải chờ cho tới
  khi chiến đấu/tốc độ/roster đổi, nhưng scheduler vẫn theo quy tắc cũ.
- Mốc tập hợp suy ra từ mép trước và khoảng cách slot, tự đi cùng pan chung;
  không đưa tâm đội hình hay mốc này vào camera.

Thông số Ground mặc định: `spacing=100`, `tolerance=5`, `regroupHysteresis=10`.
Sai số tập hợp được giới hạn không quá 25% spacing để tránh slot bị coi là
trùng nhau. Hysteresis tránh đổi trạng thái liên tục khi có sai lệch nhỏ.
Với sprite mặc định, khoảng cách 100 lớn hơn bề rộng thân khoảng 72.
Nếu thay scale/asset cần nghiệm thu lại spacing để tránh chồng phần vũ khí.
Không cưỡng chế tách các nhân vật khi đang giao chiến.

## Tốc độ thử và B2

Trước đây HeroController có một `moveSpeed` dùng chung cho ba thế: mặc định C#
150, MainGame serialize 300; SetupHeroInfo/ApplyDevBalance ghi đè từ DEVB nếu có.
Save JSON không chứa base moveSpeed của controller. Modifier MovementSpeed
trong EntityDataSO vẫn được CombatStats áp dụng như cũ.

`GameManager.useW2StanceMoveSpeeds=true` bật profile thử cho Hero chính tại
Start/Deploy/Retry và cho B2 trước khi kích hoạt rig. Trên HeroController:
`meleeMoveSpeed=450`, `physicalMoveSpeed=450`, `magicMoveSpeed=200`.
`useStanceMoveSpeeds` mặc định false trên controller độc lập; chỉ các đường
khởi tạo gameplay/B2 nói trên bật nó. Tắt cờ của GameManager trước khi vào game
để dùng base moveSpeed cũ. Không chuyển đổi hoặc ghi lại save để áp thông số.
CombatStats cộng flat/percent/điều kiện sau khi chọn base; DEVB có sẵn vẫn
ưu tiên base được SetupHeroInfo/ApplyDevBalance gán, không bị profile thử che đi.
Các tốc độ hiển thị có thể khác 450/450/200 do modifier hoặc DEVB hợp lệ.

B2 giữ giới hạn 2 đồng đội, dữ liệu riêng và cách loại modifier trang bị đã có.
Clone nhận profile theo mode, xuất hiện sau người cuối một spacing ngay ở
SpawnHero(false, initialPosition), không đổi vị trí Hero đang sống hoặc camera.
Các lần tập hợp sau đó không gọi SpawnHero/SetPosition ngoài bước đi giới hạn.
Không thêm dữ liệu đội hình hay đồng đội vào save.

## Xác minh và giới hạn

`PATH=/workspace/.onboarding/bin:$PATH bash Tests~/run.sh --formation` kiểm tra
1/2/3 Hero, 600/800/1150, thứ tự tick, modifier, đổi thế, mục tiêu, tốc độ riêng,
tập hợp/hysteresis, chờ người chậm, Hero chết/ẩn, tuyển thêm, tốc độ 0, pan cứng
và mềm, B2 thật qua fixture, scheduler trong lúc tập hợp và quái tái dùng.
Runner gọi thêm nhóm B2 cũ, không thay assertion cũ. UnityStub chỉ clone public
instance fields (không copy hằng số static); không thay serializer Unity thật.
Các kiểm tra này là Mono/UnityStub, không chứng minh render hay Unity lifecycle.
Chạy lại `--spawn`, `--desktop-ui`, `--sprites` để kiểm tra tương thích.

Chưa có Unity Editor hoặc Windows Player trong Cloud. Cần nghiệm thu bằng
Play Mode/Player: New/Continue, Load trong trận, ReturnTitle→Continue, Reset,
Retry; gọi 1→2→3 Hero ở ba kích thước; quan sát tập hợp giữa wave, đổi thế/chết,
thanh HP/charge/damage popup, Attack/Hit, VFX và camera. Không dùng save quan trọng.
Không coi bộ Mono đầy đủ PASS khi còn blocker cũ GroundPresentation.fixedRect.

Kết quả Cloud: nhóm đội hình/B2 39 PASS (22 ca mới + 17 B2 cũ), spawn 16 PASS,
UI 59 PASS, sprite 28 PASS, camera/lane/B1 31 PASS, rollback save/thế đánh 6 PASS.
Toàn bộ runner đạt 304 PASS rồi dừng ở MissingMemberException
`GroundPresentation.fixedRect` trong TerrainRegression.cs; các nhóm sau chưa chạy
qua runner đầy đủ. Biên dịch Mono nhánh UNITY_EDITOR: 16 cảnh báo cũ; nhánh
UNITY_STANDALONE_WIN: 3 cảnh báo cũ, chỉ kiểm tra điều kiện C# với UnityStub.
Chưa xác nhận biên dịch Unity hoặc chạy gameplay/Windows Player cho thay đổi này.
