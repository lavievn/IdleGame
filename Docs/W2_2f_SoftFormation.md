# W2.2f — Tập hợp với leader giảm tốc mềm

Thay cơ chế leader đứng tại anchor. Giữ nguyên chọn mục tiêu, combat, damage,
cooldown, camera, SaveManager và các base speed 450/450/200 hiện có.

HeroFormation.Plan chụp một snapshot cho cả đội mỗi frame, sắp từ trái
(đầu đoàn) sang phải; giữ thứ tự đó tới khi thay roster hoặc qua combat.
Tốc độ ổn định M = min(EffectiveMoveSpeed của Hero sống đã đăng ký combat).

Trong Regrouping, với sai lệch khoảng cách trước/sau so với spacing:

    targetSpeed[i] = clamp(M + gain * (errorAhead - errorBehind),
                          minimumMarchFraction * M, ownEffectiveSpeed)

Mặc định spacing=100, tolerance=5, hysteresis=10 giữ nguyên; gain=2/giây,
acceleration=900 đơn vị/giây², minimumMarchFraction=0,1 (đều chỉnh Inspector).
Leader có errorAhead=0 nên giảm xuống dưới M khi nhóm phía sau còn xa, vẫn
tiến dương; rear chậm không bao giờ vượt ownEffectiveSpeed. Vận tốc chuyển
dần bằng MoveTowards theo acceleration × deltaTime, xuất phát từ bước đi
chủ động thực của frame trước (LastMovementSpeed), không tính camera pan.

Không đặt lại vị trí hoặc kéo ngược. Kẹp va chạm chỉ áp dụng actor phía sau
nếu slot đang quá sát/chồng: không cho vượt actor trước, không dùng làm trạng
thái chờ của leader. Khi sai lệch trong tolerance và vận tốc đủ gần M để
đạt M trong một bước gia tốc, chuyển Marching. Cả đội ổn định cùng M;
hysteresis tránh đổi phase theo sai số nhỏ. Zero speed là phase Halted rõ
ràng; hết debuff hoặc có enemy thì thoát ngay, không tăng tốc member bất động.

Một Hero bypass formation; Hero chết/ẩn rời phép tính. Enemy mới hoặc đang
tấn công bypass regroup, dùng toàn bộ logic tiếp cận/tầm đánh hiện tại.
LastMovementSpeed chỉ là metric runtime, reset ở Spawn/Hide/Die/Disable,
không ghi vào dữ liệu save và không thay damage/attack presentation.

Các assertion cũ buộc leader đứng yên được thay bằng kiểm tra leader tiếp
tục đi có giới hạn. Một ca recruit chờ hội tụ có giới hạn thay vì giả định
300 frame, giữ nguyên sai số khoảng cách 2. Các assertion về targeting,
moveSpeed, không chồng/teleport, gear/passive, B2 và camera được giữ nguyên.
Ca wave 5 giây không còn bắt buộc soft regroup hoàn tất trước lúc sinh quái:
kiểm tra leader tiếp tục đi, sai lệch giảm, own speed giữ đúng và wave vẫn
đúng lịch. Hội tụ hoàn chỉnh được kiểm riêng có giới hạn 20 giây, không nới
sai số khoảng cách. W2.2g sẽ thay đồng hồ wave chính bằng quãng đường.

Cloud: --soft-formation 8/8, gồm 2/3 Hero × 30/60/144 FPS, slow member 200
không boost, leader giảm tốc liên tục xuống dưới 200, hội tụ, không phase
chatter, zero/restored speed, combat interrupt, recruit/death. --formation
39/39 (cả B2) và --camera-entry 38/38. Mono/mcs có 16 cảnh báo cũ. Chưa
nghiệm thu cảm giác di chuyển bằng Unity/Windows Player thật.
