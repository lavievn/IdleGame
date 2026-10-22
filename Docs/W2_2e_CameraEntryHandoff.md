# W2.2e — Bàn giao camera sau spawn

Nền `ace1c64d15ee403e44cafb4c26e2796322a5b3b9`. Đây là bản sửa pha vào
game đã được phê duyệt sau commit chẩn đoán. Không sửa SaveManager hoặc JSON.

Log Unity xác nhận frame 5095 tạo wave nhưng không đổi vị trí; frame 5096
Hero đi 5,9000 đúng speed × dt, sau đó camera pan -8,13191. Spawn X=576 (80%)
nằm ngoài mép phải deadzone X=231,36 (62,05%). Đây là hiệu chỉnh của camera
gốc, không phải reset HomeX hoặc teleport. Regression giữ nguyên mẫu native
để kiểm tra camera bình thường; chỉ fixture của mẫu đó tắt handoff mới.

GameManager.CompleteGameplayEntry chỉ bắt đầu handoff SAU VFX và sau
Env.LateUpdate đang pause. Không đổi tọa độ Hero, cinematic, thời điểm wave
đầu hoặc công thức combat. Trong opening/VFX, Time.timeScale=0 nên không có
camera pan. Load thủ công, Retry và chuyển map không tự bật handoff.

EnvironmentManager.PlanPresentationPan bọc PresentationPan gốc. Ngoài handoff
nó gọi và trả đúng kết quả cũ; thân PresentationPan, deadzone, SmoothDamp,
combat focus và các thông số gameplay không đổi. Trong handoff:

- Mặc định duration tối thiểu 0,8 giây gameplay; maxSpeed 450 đơn vị/giây;
  acceleration 900 đơn vị/giây²; maxStep 12 đơn vị/frame, đều chỉnh Inspector.
- Từ yêu cầu pan gốc, giới hạn vận tốc rồi MoveTowards theo gia tốc. Delta
  dùng riêng cho camera entry là min(deltaTime, maxStep/maxSpeed), tránh
  một frame dài kéo cả thế giới. Không sửa Time.deltaTime của actor/combat.
- Pan được giới hạn dùng chung trong Update (bù chuyển động quái) và LateUpdate
  (Hero/quái/đạn/Ground/parallax), không tính hai lần trong một frame bình thường.
- Khi pan gốc bị giới hạn, xóa xung lò xo chưa thực sự áp dụng, không giữ
  vận tốc camera giả để bật mạnh ở frame sau. Begin cũng bỏ momentum pha cũ.
- Sau duration, chỉ thoát khi pan gốc và pan thực tế khớp trong ít nhất 0,1
  giây liên tiếp. Giữ trạng thái smoother khi thoát vì nó đã áp dụng đúng các
  plan gốc, không cưỡng bức snap hoặc nhả limiter giữa lúc còn bị giới hạn.
  Duration là thời gian tối thiểu, không phải deadline bỏ giới hạn đột ngột.
- Pause dừng clock và pan. Resize chỉ đổi projection; không khởi động lại
  handoff hoặc dùng tọa độ màn hình làm input. Hero chết/FollowHero, Load,
  Menu, Reset/New Game, Retry và disable manager hủy trạng thái cũ. Inspector
  NaN/infinity/không dương dùng default an toàn, không đóng băng camera.

Log `[HeroPosition]` bổ sung cameraEntry, entryCameraElapsed,
entryCameraVelocity và entryRequestedPan (yêu cầu gốc trước limiter), bên cạnh
planFrame/appliedFrame/plannedPan/actualPan.
Giữ công cụ trace mặc định tắt; xem W2_2e_WorldEntryDiagnostics.md để thu log.

Kiểm thử `--camera-entry` dùng Mono/UnityStub: 600/800/1150 × 1/2/3 Hero ×
30/60/144 FPS; kiểm pan/step/acceleration, own movement + shared pan, Ground
phase; thoát hữu hạn; speed 0/200/450/900; pause, long frame, resize, hủy vòng
đời; momentum và cấu hình lỗi. Kiểm tra camera gốc bằng nhóm CameraFollow,
CameraAndLane và Effects, cùng đối chiếu thân hàm với commit nền.

Chưa có Unity Editor/Windows Player trong Cloud. Cần nghiệm thu trực tiếp
độ mượt ở ba kích thước, New/Continue và B2, chuyển từ handoff sang giao
chiến, Load/ReturnTitle/Retry, frame drop/resize/pause và Console. Mẫu log
native ban đầu là dữ liệu người dùng cung cấp, không phải lần chạy Unity
do Cloud thực hiện. Không gọi toàn bộ runner Mono PASS khi còn blocker cũ
GroundPresentation.fixedRect.

Kết quả Cloud: camera-entry 38/38; world-trace 10/10; wave-entry 37/37;
spawn 16/16; desktop-ui 59/59; formation/B2 39/39; camera/lane/effects riêng
39/39. Mono/mcs UNITY_EDITOR: 16 cảnh báo cũ. Trace theo frame được xuất khi
chạy camera-entry (tiền tố MONO FRAME TRACE): request đầu -8,13191, actual
khoảng -0,1547 với delta 0,0131111043. Không phải trace Unity sau bản sửa.
Đối chiếu tĩnh giữ nguyên thân PresentationPan/CombatCenterX/PanWorld/
PanEnvironment/SyncBattleProjection/SetPosition và toàn bộ BattleMotion.
