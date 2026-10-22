# W2.2j — chặn bước di chuyển Hero quá lớn sau VFX

## Bằng chứng và giới hạn kết luận

Người dùng cung cấp log **Unity native**: VFX hoàn tất tại frame 2296, chân
Hero screen (1536,0), moved=0; frame 2297 dt=0.0131396, moved=5.912842;
frame 2298 dt=0.2800149, moved=126.006714, pan=-1.91070127, chân screen
(1402.09216,43.97318). `450 * 0.2800149 = 126.006705`; footDeltaScreen=(0,0).
Đây là chuyển động logic thật do bước thời gian lớn, không phải lệch neo VFX
hoặc thân Image. Log này là bằng chứng do người dùng thu, không phải kết quả
chạy Unity tại Cloud.

`EnvironmentManager.Update()` truyền Time.deltaTime cho Hero.TickMovement.
TickMovement dùng dt trực tiếp cho ForwardApproach, lane Y và formation. Quái
wave đầu xuất hiện bên trái, ngoài tầm đánh: FindClosestMonster chọn mục tiêu,
ForwardApproach tiến tới, RefreshCombatState đặt Approaching. Không có lời gọi
HomeX, ResetPosition hoặc SpawnHero trong nhánh này.

**Chưa xác định tác vụ nào gây stall 280 ms.** Không thể quy CPU/GPU/GC cho một
hàm chỉ từ dt. Cần xem cả frame 2297 và 2298 trong Profiler: dt lớn có thể phản
ánh công việc của frame trước. Các đường cần đo, chưa phải kết luận:

- GameManager.CallNextWave → MonsterSpawner.SpawnMonster/ObjectPool.Get →
  Instantiate/OnEnable → CultivationSpriteBridge.Init/LoadFrame. Capacity=5
  không có nghĩa pool đã tạo sẵn 5 instance; Resources.Load/Sprite.Create và
  lần render/upload đầu tiên có thể có chi phí khác nhau.
- CompleteGameplayEntry → UIManager.ShowMapTitle và MapBgmController.Update
  đổi playlist khi HUD gameplay bật. Rebuild Canvas, render, audio và GC cần
  đo riêng; mã nguồn không cho biết chúng tốn bao nhiêu ms ở máy người dùng.
- HeroRenderProbe tạo snapshot dài tại nhiều stage và ghi Console. Có thể làm
  tăng thời gian frame khi bật chẩn đoán. Bản này bỏ stack trace tự động cho
  riêng `[HeroRender]` bằng Debug.LogFormat/NoStacktrace, giữ dữ liệu/stage.
  Snapshot/string/Console vẫn có chi phí; không tuyên bố đã loại bỏ stall.

## Chính sách đã được người dùng chốt

Chỉ bước đi Hero/B2 dùng `min(inputDt, maxHeroMovementDeltaTime)`, mặc định
**1/30 giây**, cấu hình trên EnvironmentManager. Hero.TickMovement áp trước
mọi nhánh exploration/formation/approach/lane Y. Hàm thuần nằm trong BattleMotion;
không có accumulator, coroutine bù hay vòng substep chạy hết thời gian dư.
Không ghi Time.deltaTime, Time.timeScale hoặc Time.maximumDeltaTime.

| dt đầu vào | Kiếm/cung 450: bước X tối đa | Phép 200: bước X tối đa |
|---|---:|---:|
| 0.016 | 7.2 | 3.2 |
| 0.033 | 14.85 | 6.6 |
| 0.1 | 15 | 6.6667 |
| 0.28 | 15 | 6.6667 |

Ở Time.timeScale=1 và FPS >=30, tốc độ/quãng đường theo thời gian giữ nguyên
450/450/200. Khi stall hoặc FPS <30, Hero đi ít hơn theo thời gian game; **bỏ
phần thời gian treo, không bù về sau** là đánh đổi đã được người dùng phê duyệt.
Nếu mã ngoài đặt timeScale cao khiến scaled dt vượt giới hạn, bước đi vẫn bị
giới hạn; bản này không thêm hoặc thay đổi điều khiển tốc độ game.

Monster, camera Multi-Zone, cooldown, sát thương, projectile/status, VFX và đồng
hồ idle/AutoSave giữ đường thời gian cũ. Formation chỉ nhận bước đi đã giới hạn,
không đổi thuật toán; distance-wave vẫn đếm **quãng đường thật đã đi** theo luật
cũ, không cộng phần thời gian bị bỏ. Không sửa SaveManager/JSON/checkpoint,
VFX anchor, sprite pivot/scale, HP bar, popup hoặc thuật toán camera.

`MovementInputDeltaThisFrame`, `MovementDeltaThisFrame`, `movementFrame` và
`discardedMovementDt` xuất hiện trong log. Bản ghi dt được xóa khi reset/hide/
chết/spawn; không có nợ thời gian sống qua pause, Load, Retry hay chuyển map.

## Kiểm thử

`PATH=/workspace/.onboarding/bin:$PATH bash Tests~/run.sh --movement-spike`

51 ca Mono/UnityStub: input 0.016/0.033/0.1/0.28 ở 3 thế đánh; một giây ở
30/60/120 FPS; nhóm 1/2/3 Hero, approach và regroup; Y lane, pause/resume,
hide/spawn, không bù; kiểm tra Monster/cooldown/hồi HP/pose vẫn dùng thời gian
cũ; New/Continue/Load/Travel/Retry/Reset × 600/800/1150/1920 sau VFX. Screen là
projection Overlay **mô phỏng**, không phải số liệu renderer Unity.

Ví dụ Mono ở projection 1920 (khác số frame/lane/quái với phiên Unity của người
dùng): dt 0.0131396 → moved 5.912842; dt 0.2800149 → movementDt 0.0333333351,
discarded 0.246681571, moved 15, pan -1.910701, screen (1513.09888,6.97094727).
Frame sau dt 0.016 → moved 7.200012. Không dùng số liệu này để tuyên bố native PASS.

Fixture `Field` lịch sử dùng các bước 0.1–nhiều giây cho tham chiếu toán học/AI;
nó cấu hình limit=float.MaxValue tường minh để giữ các assertion cũ. Các test
đó **không kiểm chứng giới hạn render mới**. 51 ca mới bật giới hạn production;
kiểm tra riêng mặc định runtime là 1/30. Không nới tolerance hoặc bỏ assertion.

Kết quả Cloud: 51 ca mới PASS; toàn bộ runner đạt 550 PASS rồi dừng tại lỗi cũ
`MissingMemberException: GroundPresentation.fixedRect` (`TerrainRegression.cs:41`),
các bài phía sau chưa chạy. Mỗi lần biên dịch UNITY_EDITOR có 16 cảnh báo cũ;
Mono với define UNITY_STANDALONE_WIN có 3 cảnh báo cũ. Các nhóm hồi quy spawn,
entry, Multi-Zone, B2, sprite, render trace và formation chạy riêng đều PASS.
Đây không phải kết quả biên dịch Unity hoặc Windows Player build.

Thêm 6 ca NUnit Play Mode trong B2SpawnPositionTests, dùng fixture owned-object
hiện có: 3 thế đánh qua VFX/Canvas thật với dt truyền tường minh, 3 tỷ lệ Canvas
cho B2 ba Hero. Chỉ TestCase/Assert.That; không SaveManager, filesystem hoặc đổi
timeScale. **Chưa biên dịch/chạy các ca này bằng Unity Editor trong Cloud.**
Chúng không thay thế kiểm thử native game loop/render/OS-window stall.

## Nghiệm thu Unity/Windows còn phải thực hiện

1. Xác nhận EnvironmentManager.maxHeroMovementDeltaTime=1/30; thử dữ liệu riêng
   đã sao lưu. Tắt cả hai cờ position/render trace cho lượt kiểm tra hiệu năng.
   Trong Profiler CPU Timeline + GC/Rendering/Audio, ghi chuyển cảnh và ít nhất
   10 frame đầu sau VFX. Lấy capture nếu vẫn có 280 ms; chưa mặc định do log.
2. Chạy 6 ca Play Mode mới trong scene sạch theo README cạnh test. Kết quả cần
   được ghi riêng với các test Save/Load trước đây, không gộp thành nghiệm thu.
3. New/Continue/Load/Travel/Retry/Reset, cả ba thế đánh, solo và B2 khi đang đi;
   resize 600/800/1150 và Windows Player. Giữ spawn 80%, foot liên tục, camera/
   Ground/parallax đồng bộ, không tăng tốc bù; pause/resume không gây bước lớn.
4. Nếu vẫn thấy nhảy, bật Trace Visual Position Changes trong một lượt ngắn,
   giữ log Begin/Finish/release và 10 frame sau. Kiểm tra inputDt/movementDt/
   moved, foot screen, pan và Ground từng frame. Nếu moved đã <=15 (<=6.6667
   cho phép) mà vẫn nhảy, tiếp tục truy transform/camera/render; **không tăng
   thời gian làm mượt hoặc thêm offset để che lỗi**.
