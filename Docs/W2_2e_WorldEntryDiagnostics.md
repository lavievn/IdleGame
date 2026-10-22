# W2.2e — Chẩn đoán frame khi toàn bộ thế giới bị giật

Nền: `a4e683a41e784197ad6bf457008749c7d734ec42`, nhánh
`feature/w2-combat-presentation`.

## Kết luận và điểm dừng

Người dùng xác nhận bản W2.2d vẫn giật trong Unity. Đã có log native cả trước
và sau bước đi/pan đầu tiên, frame 5096. Bước kéo thế giới đầu tiên đã khớp
chính xác với camera hiện tại; chưa có chuỗi frame sau để đánh giá toàn bộ
cú giật được quan sát. Không coi lỗi bàn giao có điều kiện
đã tái hiện bằng Mono ở W2.2d là lời giải thích đầy đủ cho biểu hiện này.
Thay đổi W2.2e chỉ bổ sung chẩn đoán bật/tắt được, theo phương án dự phòng được
phê duyệt. Chưa sửa được hoặc nghiệm thu lỗi còn lại trong Unity.

W2.2f (giảm tốc mềm) và W2.2g (quãng đường sinh wave) chưa triển khai. Dừng
trước hai phần đó theo yêu cầu: “Nếu một phần không đạt, dừng trước phần kế
tiếp và báo cáo nguyên nhân.” Đội hình và đồng hồ 5 giây hiện tại giữ nguyên.

## Bằng chứng mã và giới hạn suy luận

- OpeningCinematicUI.AnimateEntry chỉ zoom các lớp sky/far/near của opening;
  CompleteGameplayTransition ẩn backdrop/curtain rồi gọi
  GameManager.OnOpeningRevealCompleted. Không dịch BattleArea ở đường này.
- OnOpeningRevealCompleted chủ động đặt Hero ở 80% BattleArea khi còn ẩn,
  rồi bắt đầu VFX. HeroSpawnVfx.Advance chỉ điều khiển Graphic và các rect
  con của hiệu ứng, không đặt lại rect logic của Hero.
- GameManager.Update đánh dấu entryCompletionPending; CompleteGameplayEntry
  chạy trong GameManager.LateUpdate sau EnvironmentManager.LateUpdate (-200).
  Do đó Env vẫn pause ở frame bàn giao. Wave đầu được tạo ngay cùng frame;
  chuyển động và camera bắt đầu từ Update/LateUpdate của frame sau.
- CallNextWave và CombatManager.StartBattle không đặt lại tọa độ Hero, không
  gọi SpawnHero. RecoverEscapedWave có SpawnHero về HomeX nhưng cần có quái
  thoát mép và thông báo tương ứng. Chưa có bằng chứng đó là đường gây giật này.
- Camera hiện tại có quyền kéo focus từ vị trí 80% ngoài deadzone về vùng
  theo dõi. Trace Mono trước đây cho pan đầu -12,606567 ở 60 FPS với cấu hình
  MainGame. Log native mới xác nhận bước pan đầu ở deltaTime thực tế, xem
  phân tích bên dưới. Chưa quy mọi cú giật về bước pan này. Không thay deadzone,
  SmoothDamp, focus, scroll/parallax hoặc bù offset để thử che biểu hiện.
- Canvas/layout native, thứ tự script có cùng execution order, deltaTime thật
  và trạng thái cửa sổ khi bỏ pause chưa được Mono tái hiện đầy đủ.

### Log Unity người dùng cung cấp trong lượt này

Frame 5095, từ “VFX complete before gameplay release”, trước/sau StartBattle,
tới “LateUpdate after camera”: life=1, anchor=(-384,0), battle=(576,0),
screen=(1536,0), groundAnchor=(0,0), cameraSpeed/pan/backgroundSpeed=0,
terrain/far/distant phase=0. timeScale đổi 0 → 1, nhưng dt vẫn 0 trong frame
đó. Cửa sổ 1920, BattleArea 1920, canvasScale=1.

Đầu TickMovement frame 5096: cùng vị trí/phase, dt=0,0131111043,
timeScale=1, speed=450. Bằng chứng native này xác nhận **không đổi vị trí
trong CallNextWave/StartBattle và không có pan ở frame bàn giao** trong lần
chạy được ghi. Không còn chỉ dựa vào mô phỏng cho kết luận hẹp đó.

Log bổ sung của cùng frame 5096:

| Mốc | X BattleArea | X màn hình | Bước đi | Pan |
|---|---:|---:|---:|---:|
| Trước TickMovement | 576 | 1536 | 0 | 0 |
| Sau approach | 570,1 | 1530,1 | 5,90002441 | 0 |
| Sau camera | 561,968 | 1521,968 | 5,90002441 | -8,13191 |

Ground anchor vẫn 0, scale vẫn 1, life vẫn 1. Camera và background speed
cùng -620,2308. Terrain phase=1911,868 chính là wrap của -8,13191 trên chiều
rộng 1920; far phase=2493,56055, distant phase=3243,98682 cũng khớp cuộn
30%/10% trên chiều rộng 1,3/1,69. Đây là pan chung đã xác nhận trong native,
không phải SpawnHero/teleport hay thay đổi projection ở frame đó.

Đối chiếu công thức gốc: MainGame có ZoneLeftX=-240,96, ZoneRightX=444,48.
DeadzoneCamera.Pan thu hẹp nửa deadzone xuống 231,36; mép phải tương ứng
khoảng 62,05% BattleArea. Hero ở 80% rồi tự đi 450 × 0,0131111043 ≈ 5,9,
focus=570,1 vẫn ngoài cả biên đỏ phải. Sai lệch tới mép deadzone là -338,74;
smoothTime nhánh ngoài biên là 0,2 × 0,55 = 0,11. Camera tính pan=-8,13191,
khớp log native. Regression mới tái hiện các giá trị này bằng mã gốc.

Vì vậy đã xác định **nguồn của bước kéo đầu tiên**: điều kiện khởi đầu 80%
kích hoạt hiệu chỉnh camera ngay khi gameplay chạy, không phải momentum cũ
hoặc movement/pan lệch frame. Chưa có log 5097–5105 để khẳng định toàn bộ
biểu hiện lớn hơn là cùng cơ chế, hoặc loại trừ frame dài/layout muộn.

Không thể cam kết giữ camera đứng ở 80% với cùng input và cùng quy tắc pan
nêu trên. Phương án cần phê duyệt riêng: hoãn kích hoạt follow riêng ở bàn
giao spawn tới khi Hero tự đi vào deadzone, rồi dùng camera hiện tại. Đây
là thay đổi chính sách kích hoạt camera; chưa triển khai vì giới hạn giữ
nguyên thuật toán camera. Cần xét Hero đứng đánh ngoài deadzone và B2 trước
khi chốt cơ chế bàn giao; không coi phương án này đã được kiểm thử hay phê
duyệt. Không dùng offset, teleport hoặc sửa hệ số để che.

## Công cụ mới ghi gì

`HeroController.tracePositionChanges` mặc định false. Khi bật, log có tiền tố
`[HeroPosition]`; không ghi file hoặc truy cập dữ liệu save. Mỗi log gồm:

- Frame, dt, unscaledDt, timeScale, SpawnVersion, trạng thái Hero, tốc độ và
  bước đi chủ động; anchoredPosition, tọa độ BattleArea, local/world/screen.
- Ground/BattleArea anchor, local/world position, localScale, pivot, chiều rộng;
  Canvas position/localScale/scaleFactor/renderMode, camera chiếu nếu có;
  kích thước cửa sổ, terrain/far/distant phase.
- Focus thô/đã lọc, deadzone velocity, combat offset/velocity và stationary
  velocity, camera target. Chỉ đọc smoother, không gọi Step/Pan để lấy mẫu.
- updateFrame, planFrame, hasPlan, plannedPan/plannedBackgroundPan;
  appliedFrame, actualPan/actualBackgroundPan và panSource:
  `skipped`, `planned`, `recomputed` hoặc `not-run`.
- deployed, entry, revealed, completionPending, entryElapsed,
  openingEntering, menuActive, vfxPlaying, waveMembers và waitingWave.

`pan` cũ nay là alias của pan đã áp dụng thực tế, không còn lấy speed frame
trước nhân dt frame hiện tại. **Đọc appliedFrame/planFrame cùng giá trị pan**:
trước LateUpdate, actualPan có thể thuộc frame trước; plannedPan có thể cũ
khi hasPlan=false. `skipped` ghi actualPan=0 cho frame đó. Các phase nền có
wrap, nên nhảy phase từ 0 sang gần chiều rộng texture có thể là cuộn âm nhỏ,
không phải nền bị teleport một chiều rộng.

Ghi trước/sau projection, bước đi, plan, pan, GameManager.Update/LateUpdate,
FollowHero reset và các mốc Spawn/80%/VFX/StartBattle. Hero.LateUpdate theo sau
Env nhưng không giả định thứ tự với GameManager (cùng order 0); stage và frame
cho phép đọc thứ tự thực tế. Không dùng tên stage Hero.LateUpdate để suy ra
entry đã hoàn tất.

Cửa sổ log mặc định 30 frame, tối đa 120 mỗi mốc. Đếm cả frame pause, tự mở
lại ở Spawn, đặt 80%, lúc yêu cầu hoàn tất VFX và lúc bàn giao. Nếu cửa sổ
trước đã hết, mốc yêu cầu hoàn tất mở lại trước Env.LateUpdate; updateFrame
vẫn ghi nhận Update pause trước đó. Không log toàn bộ opening vô hạn. B2
kế thừa cờ và giới hạn frame; mỗi dòng phân biệt actor và life.

## Thu thập bằng chứng Unity

1. Trong Play Mode, trước khi chọn Chơi mới/Tiếp tục, chọn HeroController
   chính, bật **Trace Position Changes**, giữ **Position Trace Frame Count**
   ở 30. Thay đổi lúc Play để tránh lưu cờ vào scene.
2. Tắt Collapse, lọc Console `[HeroPosition]`. Thực hiện tới lúc giật, dừng
   Play rồi gửi các dòng từ “Opening reveal before intentional 80-percent
   placement”, “GameManager.Update after entry” có completionPending=True,
   “VFX complete before gameplay release”, “CallNextWave after StartBattle”
   tới 10–30 frame sau. Giữ nguyên thứ tự, frame và stage.
3. Ghi kích thước cửa sổ, solo/B2 và FPS/video tương ứng. Log có chi phí và
   có thể làm frame dài; cần so sánh biểu hiện khi tắt log. Không kết luận
   pan lớn là lỗi camera trước khi xét deltaTime và thay đổi Canvas/layout.
4. Cần nghiệm thu native: New/Continue; solo/ba Hero; 600/800/1150; Manual
   Load trong trận; ReturnTitle rồi Continue; camera gameplay thông thường.
   Không tự xóa save để thu log; dùng quy trình dữ liệu thử đã sao lưu.

## Kiểm tra Cloud

Không có Unity Editor/Windows Player. Chỉ có Mono/mcs + UnityStub; phép chiếu
screen trong fixture là mô hình overlay, không phải kết quả render Unity.

`bash Tests~/run.sh --world-trace` bổ sung 10 kịch bản: mặc định không log;
giới hạn frame kể cả pause; cửa sổ hết rồi được mở lại khi VFX hoàn tất;
phân biệt plan/actual khác frame và khác dt; snapshot đầy đủ, đọc không bước
smoother; so sánh bật/tắt log ở 600/800/1150 × solo/ba Hero. So sánh cùng seed
ngẫu nhiên, theo dõi movement, HP/timer, dirty, focus/velocity, terrain/parallax
và số quái; tái hiện frame 5096 từ log Unity bằng camera nguyên trạng.
Fixture có các bước Spawn bổ sung để dựng mô hình Canvas nên life tuyệt đối
khác native; assertion giữ nguyên life trước/sau frame, không ép life=1.
Không đổi assertion cũ.

Đối chiếu tĩnh thêm các thân hàm camera/mapping/pan với HEAD nền. Các hàm
PresentationPan, CombatCenterX, PanWorld, PanEnvironment, SetPosition,
SyncBattleProjection và InstallStableBattleCanvas phải giữ nguyên. Các
chèn ở Update/LateUpdate chỉ ghi biến chẩn đoán và gọi log; số lần tính pan
và thứ tự mutation gameplay không đổi.

Runner Mono đầy đủ đã có blocker độc lập `GroundPresentation.fixedRect` tại
TerrainRegression.cs:41; không gọi toàn bộ suite PASS hoặc sửa blocker đó
trong phần này. Xem báo cáo bàn giao cho kết quả từng nhóm tập trung thực tế.

Kết quả chạy thực tế sau thay đổi:

| Nhóm Mono/UnityStub | PASS | FAIL |
|---|---:|---:|
| `--world-trace` | 10 | 0 |
| `--wave-entry` | 37 | 0 |
| `--spawn` | 16 | 0 |
| `--desktop-ui` | 59 | 0 |
| `--formation` (gồm B2) | 39 | 0 |
| `--sprites` | 28 | 0 |
| CameraFollowTests + CameraAndLaneHotfixTests + NewFeatureTests, gọi riêng | 39 | 0 |

Mỗi lượt biên dịch UNITY_EDITOR có 16 cảnh báo đã tồn tại, không lỗi compiler.
Runner đầy đủ đạt 351 PASS rồi dừng bằng MissingMemberException tại
GroundPresentation.fixedRect; các nhóm phía sau chưa được runner đầy đủ chạy.
Không cộng số ca của các lượt tập trung vào số runner đầy đủ vì có trùng ca.
Đối chiếu tĩnh đạt: 7 hàm camera/mapping/pan nêu trên giữ nguyên từng thân hàm;
Update/LateUpdate/FollowHero giữ nguyên thao tác ngoài chẩn đoán. BattleMotion,
CombatManager, HeroFormation, HeroSpawnVfx, SaveManager, MonsterController,
scene, Resources, Packages và ProjectSettings không đổi.
