# W2.2k B — điểm chân và chuyển làn Hero đánh xa

## Bằng chứng và giới hạn chẩn đoán

Mã nền b844334 có các đường ghi Y sau:

- GameManager.DeployHero truyền spawn Y=0; HeroController.SpawnHero mặc định
  cũng dùng HomeX,0. Mỗi map/Load/Retry đi qua khởi tạo này.
- HeroController.TickMovement chỉ đổi Y theo mục tiêu cho Melee; cung/phép
  giữ Y hiện có cả khi khám phá và bắn. Formation và camera pan chỉ đổi X.
- GameManager.TryTestCompanionPosition ép toàn bộ khung sprite vào sàn.
  Khung 100 đơn vị cao trong sàn 100 chỉ để lại chân gần đáy (khoảng 1.667),
  dù phần alpha phía trên có thể vượt sàn theo thiết kế đã được duyệt.
- Opening reveal đổi X, giữ Y; EnvironmentManager.Position/SetPosition đổi
  qua cùng hệ BattleArea. Awake đổi pivot nhưng bảo toàn world foot. Các
  hiệu ứng đổi transform con/scale quanh foot; không có phép trừ Y tích lũy
  cho cung/phép trong các đường này. MonsterSpawner random Y chỉ cho quái.

Đây là bằng chứng khởi tạo sát đáy rồi giữ nguyên, không phải bằng chứng đã
tái hiện nguyên nhân mọi lần dạt xuống sau hai map trên máy người dùng.
Không kết luận đã tìm được writer ẩn trong Unity chỉ từ Mono. Cần log native
nếu Y vẫn lệch. Chuyển làn không dùng Random và không thay thế việc tìm writer.

## Cách xử lý

HeroController.InitialFootY chọn giữa khoảng chân an toàn khi requested Y
nằm ngoài biên; giữ requested Y hợp lệ. Chỉ áp cho cung/phép, khi hình còn ẩn
trong SpawnHero, trước Prepare/VFX. Không đặt lại Y lúc VFX kết thúc. Melee
giữ nguyên. B2 dùng biên chân trên Y và giữ kiểm tra full frame, khoảng cách,
không chồng thân trên X. Không bắt toàn bộ thân nằm trong sàn; không sửa sprite.

HeroLaneMotion là state riêng mỗi Hero, không lưu JSON, không RNG. Khám phá
chọn lần lượt tâm ± biên độ; giữ mục tiêu cho tới khi đến, rồi chờ đủ thời
gian trước đổi mục tiêu. Cung và phép khởi đầu hai hướng đối nhau. Khi có
mục tiêu chiến đấu: giữ Y hiện tại, không rung theo target; chỉ hồi biên mượt
nếu đang ở ngoài khoảng hợp lệ (ví dụ đổi Melee sang cung lúc chân ở Y=0).
Không teleport khi đổi mode. Spawn/hide/disable/chết/đổi mode xóa trạng thái.

Inspector trên HeroController (B2 kế thừa từ Hero chính):

| Tham số | Mặc định | Ý nghĩa |
|---|---:|---|
| enableRangedLaneMotion | true | Bật chính sách chân/chuyển làn cung và phép |
| rangedFootMargin | 12 | Biên chân tính trong BattleArea, cap 45% chiều cao |
| rangedLaneAmplitude | 10 | Đích tâm ± biên độ, cap vào khoảng an toàn |
| rangedLaneSpeed | 8 | Tốc độ Y, đơn vị BattleArea/giây |
| rangedLaneHoldSeconds | 4 | Thời gian giữ sau khi đến đích, theo thời gian game |

Y dùng budget chuyển động tối đa 1/30 giây đã chốt, không bù phần treo;
pause/zero speed không tiến Y. X vẫn tính trước, theo công thức cũ; counters
exploration/budget/LastMovementSpeed chỉ tính X. Không sửa camera, wave,
damage, range, tốc độ danh nghĩa, SaveManager hoặc JSON. Đặt amplitude=0
để kiểm chứng riêng khởi tạo an toàn, không có hiệu ứng chuyển làn.

Log HeroPosition/HeroRender bổ sung laneInputY, laneExpectedY, laneUnexpectedY,
laneTargetY, laneHold và laneFrame. Input khác expected ở frame trước báo
writer ngoài bước đi; log render cuối frame cho phép so expected với foot
BattleArea sau Canvas. Logging vẫn opt-in; không có I/O save hay transform
write trong probe. Resize/pause có thể gây khác biệt cần đọc cùng parent,
Canvas scale và frame, không mặc định mọi delta là lỗi.

## Kiểm thử Cloud

`bash Tests~/run.sh --ranged-lanes`: 28 ca PASS. Có baseline bật/tắt chuyển
làn, giữ mục tiêu, không dùng RNG chung, chân khớp sprite, 30/60/120 FPS qua
4 lần spawn, 3 Hero/3 map tại 600/800/1150, resize, combat/target/Attack/Hit,
zero-speed, đổi mode, New/Continue/Load/Travel/Retry/Reset. So trace bật/tắt
cùng seed cho X, exploration, Multi-Zone và parallax trùng nhau. Test ghi
Y ngoại lai -3 xác nhận chẩn đoán phát hiện, không che mất nguồn sai lệch.

Các fixture legacy giữ enableRangedLaneMotion=false để bảo toàn mốc toán
học Y cố định/full-frame containment; assertion cũ không bị sửa/xóa. Chúng
không chứng minh chính sách mới: 28 ca trên bật production policy riêng.

Full Mono runner: 587 PASS rồi dừng ở lỗi tồn đọng MissingMemberException
GroundPresentation.fixedRect (TerrainRegression.cs:41); các ca sau chưa chạy.
Nhóm formation/B2 39 PASS và desktop UI 59 PASS. Biên dịch mô phỏng
UNITY_EDITOR: 16 cảnh báo cũ; UNITY_STANDALONE_WIN: 3 cảnh báo cũ.
Không có Unity Editor/Windows Player trong Cloud.

B2SpawnPositionTests thêm 6 TestCase Play Mode (2 mode × 3 projection scale)
dùng native Awake, RectTransform, Canvas và sprite import; không có save I/O.
Kiểm tra ba đời spawn, bước Y/biên chân, pause dt=0, feet sprite, B2 riêng
Image và focus không đổi. Chỉ dùng TestCase/Assert.That và các constraint
NUnit cơ bản; không Assert.Multiple/NonParallelizable. Chưa chạy hoặc biên
dịch trong Unity thật. Bước dt tiêm vào không thay thế nghiệm thu render.

## Checklist nghiệm thu Unity và Windows Player

1. Chạy Presentation Play Mode trong scene trống; xác nhận import/compile,
   các ca cũ và 6 ca mới PASS. Không chạy từ scene có manager của người chơi.
2. 600/800/1150: kiểm tra Home, Settings, popup Xác nhận/Hủy, resize và Return
   Menu; đủ chữ, không đè logo/artwork/Splash, toàn vùng nút click được.
3. Chạy Melee/cung/phép với hai B2 qua ít nhất 3 map. Chân cung/phép ở trong
   biên sàn, thân được nhô lên; khám phá đổi làn chậm, bắn giữ Y ổn định.
   Tạm đặt amplitude=0: vẫn spawn trong sàn, không dạt đáy qua map.
4. Pause/resume, đổi mode, Load/Retry/Reset/Travel, Return Menu/Continue;
   kiểm tra VFX vẫn 80% X, không nhảy ở frame release; HP/damage/Attack/Hit
   và camera Multi-Zone đúng. Dùng dữ liệu thử đã sao lưu, không xóa save thật.
5. Kiểm tra progress chỉ tăng khi đi X, resize khi đang đi/bắn, zero-speed.
   Nếu còn trôi Y, bật traceVisualPositionChanges và tracePositionChanges trên
   Hero chính (B2 kế thừa), thu log trước/sau TickMovement và render cuối
   frame kèm mode/map/parent/Canvas. Không tăng smoothing hoặc random bù Y.

Chỉ sau nghiệm thu native mới kết luận hết lỗi Windows/Unity đã báo.
