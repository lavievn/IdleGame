# W2.2g — Wave theo quãng đường hành quân

Nền: commit B `f421f24`, nhánh `feature/w2-combat-presentation`.
Không sửa SaveManager/JSON, damage, cooldown, AI chọn mục tiêu, world map,
inventory, camera gameplay, sprite hoặc VFX. Không thêm checkpoint save.

## Quy tắc và phép đo

GameManager.distancePerWave mặc định 1000 đơn vị Ground-local, chỉ khai báo
default một lần. Ngưỡng không dương/NaN/infinity dùng default an toàn.
Wave đầu vẫn CallNextWave ngay sau VFX hoặc deploy trực tiếp như trước.

OnMonsterDied chỉ arm chờ khám phá sau callback quái cuối. Xóa quãng đường
cũ và bỏ qua frame chết, vì bước đi frame đó có thể đã tính trong combat.
GameManager.LateUpdate lấy mẫu sau toàn bộ Update của Hero/combat:

    step[i] = bước đi chủ động ngoài combat của Hero i trong đúng frame này
    commonStep = min(step[i] của các Hero sống, đã deploy và đăng ký combat)
    explorationDistance += commonStep

Một Hero chính là bước đi của chính mình. Với nhiều Hero, không cộng tổng
hoặc lấy người nhanh nhất; thành viên chưa tick/spawn mới đóng góp 0. Danh
sách được khử trùng lặp, phép min không phụ thuộc thứ tự. Loại Hero chết/ẩn
khỏi tập mẫu, không trừ cumulative distance nên đổi roster không gây âm.
Regroup đang tiến vẫn cộng theo tiến độ chậm nhất thực tế; Halted không cộng.

HeroController.MovementFrame/IsExplorationMovement phân biệt bước đi hiện
tại với combat hoặc dữ liệu frame cũ. MovementBudgetThisFrame được chụp cùng
bước đi: clamp sai số float và chặn overspeed mà không tính lại buff sau khi
movement đã diễn ra. Không dùng Hero screen position, centroid, Ground pan,
parallax, spawn position, teleport hay resize delta làm phép đo.

Ngưỡng đạt thì sinh đúng một wave, bỏ phần dư. CallNextWave chặn khi wave
cũ còn đối tượng chưa xử lý. Nếu tạm thiếu tham chiếu spawner, giữ chờ tới
khi nó có lại thay vì đánh mất lịch. Không có queue encounter nhiều lần.
Mỗi frame chỉ lấy mẫu một lần; callback chết lặp không arm lại lịch.

Với chuyển động ổn định: 200 → khoảng 5 giây gameplay; 450 → khoảng 2,22
giây, làm tròn lên theo frame. Tọa độ float có thể khiến tổng thực còn rất
nhỏ dưới 1000 tại mốc lý thuyết, nên chờ thêm một frame thay vì giảm ngưỡng.
Tốc độ 0 không có encounter mới; timeScale đổi thời gian thực, pause dừng
cả movement và tích lũy. Phép cộng dùng double và reset sau mỗi encounter.

## Vòng đời và đồng hồ cũ

CancelNextWave xóa flag/distance/frame cùng coroutine recovery hiện có:
Load, Reset/New Game, ReturnTitle, Game Over/Retry, đổi map và disable không
giữ tiến độ phiên cũ. Load/Retry/Travel vẫn bắt đầu wave đầu trực tiếp theo
luồng cũ; không lưu explorationDistance vào file hoặc thay quy tắc checkpoint.

waveDelay=5 và WaitAndCallNextWave chỉ còn được gọi bởi RecoverEscapedWave
để phát lại wave đang dở khi quái thoát mép. Giữ hành vi recovery, version
chống iterator cũ, wave size/HP/EXP như trước. Đây không phải cơ chế chờ
wave sau một lần tiêu diệt hoàn chỉnh. Tooltip và test name phân biệt rõ.

## Kiểm thử và thay đổi kỳ vọng

--distance-waves: 34 ca Mono/UnityStub: solo 0/75/200/450/900; 30/60/144 FPS;
2/3 Hero đi đều hoặc tập hợp, đảo thứ tự, minimum không sum; pause/timeScale;
quái cuối chết, callback/LateUpdate lặp; camera/parallax/teleport/resize;
combat không bank distance; zero speed, buff giữa frame, roster/death,
B2 recruit/removal thật; threshold, overflow, chống wave đôi; cấu hình lỗi;
spawner tạm thiếu; Game Over và hủy lịch qua Load/Reset/Menu/Retry/Travel/Disable.

19 ca của WaveEntryRegression mô tả đồng hồ chính 5 giây được thay bằng
các kiểm tra quãng đường ở trên. Giữ các assertion 80%, atomic handoff,
own movement/camera, hủy completion pending và read-only diagnostics. Ca
stale iterator được chạy qua RecoverEscapedWave thật thay vì giả định quái
cuối chết luôn tạo coroutine. WaveEntry còn 18 ca. Kiểm tra scheduler của
FormationRegression và MotionRegression đổi sang distance flag/threshold;
giữ targeting, pooling, uniqueness, cancel và Hero hidden khi Load chờ deploy.
EffectsRegression chỉ đổi tên ca helper 5 giây để nói rõ escape recovery,
không đổi assertion đạn/sát thương hoặc thời gian bay.

Kết quả: distance 34/34; wave-entry 18/18; formation/B2 39/39;
soft-formation 8/8; world-trace 10/10; spawn 16/16; desktop-ui 59/59;
sprites 28/28; camera-entry 38/38. Camera/lane/effects và escaped-wave/legacy
map/death được gọi riêng 49/49. Không cộng các lượt này với runner đầy đủ.

Runner đầy đủ: 412 PASS, rồi MissingMemberException của ca terrain cũ tại
GroundPresentation.fixedRect (TerrainRegression.cs:41); các ca sau chưa
chạy qua runner đầy đủ. Không gọi toàn suite PASS và không sửa blocker đó.
Mono/mcs UNITY_EDITOR có 16 cảnh báo cũ; kết quả không phải Unity compile.
Nhánh tiền xử lý UNITY_STANDALONE_WIN cũng biên dịch bằng Mono/UnityStub,
3 cảnh báo cũ; không chạy executable Windows hoặc coi đây là Player build.

## Nghiệm thu còn cần Unity/Windows Player

- New/Continue → VFX 80% → entry handoff → wave đầu: ghi log pan theo frame,
  kiểm độ mượt/camera release ở 600/800/1150 và 1/2/3 Hero.
- Sau combat, leader giảm tốc nhưng tiếp tục tiến, Mage 200 không boost,
  không giẫm slot/teleport; enemy mới ngắt tập hợp, Attack/Hit/popup vẫn đúng.
- 200/450 khám phá tới threshold; pause/timeScale; B2 gọi/xóa giữa hành quân;
  không tính pan/parallax. Thử Load, Menu→Continue, Retry, Reset và đổi map.
- Console không lỗi; mở/build Player thường. Cloud không có Unity Editor
  hoặc Windows Player, chưa nghiệm thu render/Canvas/lifecycle/Win32 thật.

## Danh sách file theo commit

A: Assets/Script/{EnvironmentManager,GameManager}.cs;
Tests~/{CameraEntryHandoffRegression,MotionRegression,WorldEntryTraceRegression}.cs;
Docs/W2_2e_CameraEntryHandoff.md.

B: Assets/Script/{HeroController,HeroFormation}.cs;
Tests~/{FormationRegression,MotionRegression,SoftFormationRegression,WaveEntryRegression}.cs;
Docs/W2_2f_SoftFormation.md.

C: Assets/Script/{GameManager,HeroController}.cs;
Tests~/{DistanceWaveRegression,EffectsRegression,FormationRegression,MotionRegression,WaveEntryRegression}.cs;
Docs/W2_2g_DistanceWaves.md.
