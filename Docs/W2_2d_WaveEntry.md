# W2.2d — Bàn giao spawn và khoảng nghỉ giữa wave

Nền: `5ea5745176f36a353230b6b5a64acf785edc0376` trên
`feature/w2-combat-presentation`. Chưa nghiệm thu thay đổi này bằng Unity/Player.

## Phát hiện và bản sửa

Người dùng đính chính: toàn bộ thế giới, gồm Hero/camera/map, cùng giật sau VFX.
Không có thông báo quái thoát mép phải. Vì vậy không sửa RecoverEscapedWave.

CallNextWave và CombatManager.StartBattle không gọi SpawnHero hay đặt lại tọa độ
Hero. Đường reset HomeX có tồn tại trong RecoverEscapedWave, nhưng không đủ bằng
chứng để quy biểu hiện này cho nó. Trace dùng cấu hình camera MainGame hiện tại
xác nhận việc tạo wave giữ nguyên vị trí xuất hiện và SpawnVersion.

Lỗi thứ tự frame được tái hiện có điều kiện: EnvironmentManager.Update (-200)
thấy pause và bỏ qua bước đi; GameManager.Update (0) kết thúc VFX, gọi
CompleteGameplayEntry → CancelGameplayEntry → UIManager.SetPaused(false);
EnvironmentManager.LateUpdate (-200) sau đó thấy hết pause và tự tính pan vì
không có planned pan từ Update. Khi deltaTime dương tại bàn giao, camera được
chạy một bước dù Hero chưa đi bước nào. Toàn bộ actor/nền cùng dịch.

Bản sửa: Update chỉ đánh dấu entryCompletionPending. GameManager.LateUpdate
mới hoàn tất entry, SAU LateUpdate đang pause của EnvironmentManager. Wave đầu
vẫn tạo trong frame kết thúc VFX, không cộng 5 giây; frame tiếp theo mới chạy
đủ movement/combat/camera. Hủy entry xóa cả cờ chờ để không có wave muộn.
Missing VFX/watchdog cũng dùng cùng bàn giao; Load thủ công vẫn vào trực tiếp.
Không đặt lại Hero về HomeX, không bù offset/teleport hoặc sửa công thức camera.

### Trace đối chứng Mono, không phải số đo trong Unity

BattleArea rộng 1920, cửa sổ 800, scale chiếu 800/1920; X xuất hiện 576 = 80%,
Hero speed 450, HomeX 0. Dùng deltaTime dương 0,1 giây để kiểm tra bàn giao.
Canvas/GPU và Time.timeScale scheduling thật chưa được mô phỏng đầy đủ.

| Giá trị | Trước kết thúc VFX | Mã cũ sau Env.LateUpdate | Mã mới sau bàn giao |
|---|---:|---:|---:|
| Hero anchoredPosition X | -384 | -561,6835 | -384 |
| Hero X trong BattleArea | 576 | 398,3165 | 576 |
| HomeX | 0 | 0 | 0 |
| Focus / filtered focus X | 0 / 0 | 576 / 576 | 0 / 0 |
| Bước đi chủ động | 0 | 0 | 0 |
| Camera pan | 0 | -177,6834 | 0 |
| Ground root anchoredPosition X | 0 | 0 | 0 |
| Terrain loop phase | 0 | 1742,3165 (pan âm wrap 1920) | 0 |
| Far / distant phase | 0 / 0 | 2442,695 / 3227,0317 | 0 / 0 |
| Screen X, overlay được mô hình hóa | 640 | 565,9652 | 640 |
| SpawnVersion | 3 | 3 | 3 |

Frame di chuyển đầu tiên sau sửa, tại 60 FPS: Hero đi trái 7,5 đơn vị;
camera pan gốc -12,606567; X cuối 555,893433; screen X mô hình 631,622253.
Tọa độ thay đổi do bước đi + pan chung, không phải một lần spawn/reset thứ hai.
Camera vẫn có quyền SmoothDamp từ vị trí 80% ngoài deadzone về vùng theo dõi
theo thiết kế cũ; bản sửa không khóa camera ở 80% sau khi gameplay bắt đầu.

Đây là lỗi bàn giao đã có chứng cứ bằng mã và kiểm thử đối chứng. Chưa thể
khẳng định nó giải thích toàn bộ biểu hiện thực tế nếu chưa ghi frame trong
Unity (đặc biệt deltaTime ở frame bỏ pause). Không dùng số mô phỏng như log Unity.

## Đồng hồ wave

- Chỉ một tham số runtime: GameManager.waveDelay, mặc định 5 giây gameplay.
  MainGame.unity đổi giá trị serialize của cùng tham số từ 1,5 thành 5; nếu chỉ
  sửa initializer C# thì scene cũ vẫn dùng 1,5. Không đổi các thông số scene khác.
- Đếm từ callback quái cuối cùng chết. Cộng deltaTime đã scale, pause không
  cộng thời gian. Tốc độ 0/75/200/450/900, đổi thế hoặc tập hợp không đổi đồng hồ.
  timeScale 2 tương ứng khoảng 2,5 giây thực, timeScale 0,5 khoảng 10 giây thực.
- Dùng double cộng thời gian để tránh trễ thêm frame do sai số cộng float;
  việc sinh wave thực tế được lượng tử hóa theo frame, không sớm hơn 5 giây.
- Không dùng BattleMotion.WaveDelay nghịch đảo tốc độ cho scheduler nữa. Giữ
  helper cũ và kiểm tra số học tương thích của nó, không thay mã camera chung file.
- CancelNextWave tăng version chờ, đồng thời StopCoroutine. Iterator cũ không
  thể sinh wave hoặc xóa handle của phiên mới kể cả bị gọi lại sau Load/Retry.
- Lịch replay quái thoát mép cũng dùng cùng tham số. Không sửa quy tắc phục hồi
  quái thoát, New Game, save, checkpoint hay tiến trình bản đồ.

## Chẩn đoán trong Unity nếu vẫn còn giật

Trên HeroController chính, bật `Trace Position Changes` trước Play rồi thử
Chơi mới → Đồng ý. Lọc Console theo `[HeroPosition]`, tắt Collapse để đọc thứ tự.
Log ghi Awake/SpawnHero (kèm stack), đặt 80%, kết thúc VFX, trước/sau StartBattle,
trước/sau TickMovement và sau camera trong 30 frame không pause đầu tiên.
Gồm frame/deltaTime/timeScale, life, tốc độ/bước đi, anchor/BattleArea/HomeX,
focus/pan, ground anchor/phase, parallax, kích thước/scale chiếu và screen point.
WorldToScreenPoint dùng camera của Canvas nếu cần; overlay dùng camera null.
B2 kế thừa cờ chẩn đoán khi được gọi. Cờ mặc định false, không ghi file/save,
không đổi tọa độ/trạng thái; bật log có thể ảnh hưởng hiệu năng nên chỉ dùng
để thu thập bằng chứng. Nếu vẫn giật, cần log này và FPS/video để phân biệt pan,
frame dài, thay đổi phép chiếu hay một nguồn reset khác trước khi sửa tiếp.

## Kiểm thử

`bash Tests~/run.sh --wave-entry`: 37 kịch bản Mono/UnityStub, gồm 12 tổ hợp
New/Continue × 600/800/1150 × solo/B2, bàn giao với deltaTime dương, không spawn
lặp, giới hạn bước đi, 5 giây ở 60 FPS, speed 0/75/200/450/900, timeScale
0,5/1/2, pause, tập hợp thật, Load/Reset/ReturnTitle/DeathRetry/Travel/Disable,
hủy hoàn tất VFX đang chờ, delay tùy chỉnh 2,5 hoặc 0, pause, bảo toàn handle/đồng
hồ phiên mới trước iterator cũ, và tính chỉ đọc của chẩn đoán.

Đối chứng biên dịch lại GameManager từ commit nền với test mới (trong /tmp)
thất bại đúng assertion giữ pause tới sau camera; cùng ca đạt với bản sửa.
Fixture spawn và manual-load UI bổ sung LateUpdate theo vòng đời mới, không
thay assertion cũ. EffectsRegression thay kỳ vọng scheduler phụ thuộc tốc độ
bằng yêu cầu 5 giây mới; không sửa công thức/expected của sát thương/đạn.

Chỉ dùng save tạm của fixture, không gọi Awake SaveManager trên đường mặc định.
Không chạy Unity Editor/Windows Player trong Cloud. Cần nghiệm thu native:
Chơi mới/Continue và frame kết thúc VFX ở 600/800/1150; solo/3 B2; pause giữa
khoảng nghỉ, tử trận/Retry, Load giữa trận, đổi map; Attack/Hit, HP/damage text,
pooling và Console. Không coi toàn suite Mono PASS khi còn blocker terrain cũ.

Kết quả Cloud cuối: 37/37 ca mới, spawn 16/16, đội hình/B2 39/39, UI 59/59,
sprite 28/28; nhóm riêng gồm effects/camera/lane/B1/rollback-save/thế đánh
51/51. Các con số là từng lượt chạy có thể trùng ca, không cộng thành tổng riêng.
Toàn runner đạt 341 PASS rồi dừng ở MissingMemberException
`GroundPresentation.fixedRect` trong TerrainRegression.cs; các nhóm phía sau
chưa được runner đầy đủ thực thi. Mono UNITY_EDITOR có 16 cảnh báo cũ;
Mono UNITY_STANDALONE_WIN có 3 cảnh báo cũ. Cấu hình Windows này chỉ kiểm tra
nhánh tiền xử lý C# với UnityStub, không phải Unity compile hay Windows build.
