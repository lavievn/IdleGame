# Hiển thị nhận Thiết Kiếm khi hạ quái

Ngày: 04/11/2026. Baseline `a774218ae613d56e9ab703de78f19eecc6a4d1fe`.
Đã tiếp tục từ PR #10, commit `e465f22133afade69ccadcfe2f5a556ef287c7aa`.
Đã compile và kiểm thử bằng Unity **6000.5.0f1** trên Windows ngày 04/11/2026;
chưa build/chạy Windows Player độc lập. Kết quả cụ thể ở phần xác minh bên dưới.

## Hành vi

Chỉ khi `CombatLootRewards.Complete()` trả `Awarded`: chụp tọa độ chân quái
trước despawn và callback kết thúc wave; lóe sáng, kiếm bật lên rồi rơi xuống
trong 0,6 giây, nằm đất **0,5 giây sau khi chạm đất**, sau đó thu nhỏ/bay cong
về mép phải cửa sổ trong 0,55 giây và biến mất. Bay mặc định về cao độ 35%
màn hình, kết thúc ngoài mép phải 16 pixel. Đây là điểm đến tạm khi chưa có nút
Hòm đồ; không tạo nút HUD mới.

Đồ đã cộng vào kho tại lúc quái chết theo luồng cũ. Animation hoàn tất/hủy
không gọi cấp đồ, equip hoặc save lần nữa. Không thay đổi cờ test, drop rate,
EXP/damage/combat RNG, checkpoint hay schema save. Profile A vẫn chỉ bật cho
Editor/Development theo cấu hình cũ, không tự bật loot production.

`LootDropPresentation` tự cài trên object CombatManager khi có phần thưởng đầu.
Không cần gắn prefab hay sửa scene. Muốn chỉnh trước Play, có thể thêm component
này vào cùng object CombatManager: Sword Sprite, Icon Size (40 pixel màn hình),
Jump Height (42 đơn vị logic), Drop Duration, Collect Delay, Collect Duration.
Để Inventory Target trống hiện tại; sau này có thể gán RectTransform nút Hòm đồ.
Điểm đến sẽ lấy tâm nút đó; thiếu/ẩn nút thì dùng mép màn hình.

Tọa độ gốc theo BattleArea-local, cùng quy ước điểm chân của quái. Pha rơi/chờ
được Y-sort cùng actor và pan bằng dịch chuyển môi trường, không pan hai lần
theo projectile/camera. Khi bay, chụp tâm sprite theo pixel màn hình rồi chuyển
sang `Loot Collection Canvas` Screen Space Overlay, scale 1, sorting order tối
thiểu 101 (HUD hiện tại là 100). Pha bay không còn kế thừa scale/origin/mask của
BattleArea. Đích đọc lại mỗi frame từ Screen.width/height hoặc từ Canvas của
Inventory Target; không giả định xMax của BattleArea là mép cửa sổ. Giữ icon 40
pixel ở cả hai hệ tọa độ. Canvas bay dùng chung, tối đa một instance cho presenter,
được dọn cùng các root ở Clear/disable/destroy.

Tạm dừng theo Time.timeScale qua CombatManager.Update. Đổi wave không cắt
animation cuối đợt. Load/Retry/Travel/New/Menu/hard clear/disable dọn hình ngay;
Game Over theo nhánh giữ Magic có thể để animation hiện có hoàn tất. Tối đa 48
hình cùng lúc; vượt giới hạn dọn hình cũ nhất, không ảnh hưởng đồ trong kho.
Thiếu sprite hoặc lỗi phần hiển thị không chặn despawn/wave/phần thưởng đã cấp.

## Tài nguyên

- `Assets/Resources/Loot/iron_sword.png`: sprite đã tạo và được người dùng chấp
  nhận ở cuộc trò chuyện ngày 04/11/2026 bằng `image_gen`, nền trong suốt.
- Giữ nguyên PNG nguồn, RGBA 1254×1254, alpha 0–255; SHA-256:
  `fbbc5bbae7318dfb0320eeb6276c2140f1fb0a390d8d402a3b4f455fee1c1cb4`.
- Import Sprite Single/Full Rect, pivot giữa, bilinear, Clamp, không mipmap/
  compression/read-write/physics shape, max texture 256. Không sửa ảnh cũ/GUID cũ.
- Flash dùng mesh `BattleEffects.Burst` có sẵn, không thêm shader/particle package.

## Xác minh ngày 04/11/2026 trên máy phát triển

- Checkout gốc ở `main`, `a774218`, có thay đổi Unity chưa commit. Giữ nguyên
  toàn bộ; nhánh `fix/loot-vfx-unity-validation` nằm trong worktree riêng, bắt
  đầu chính xác từ commit PR #10. Không merge main, không đổi package/backend.
- Unity Editor **6000.5.0f1 (88b47c5e7076)** đã cài trên máy; tái sử dụng
  PackageCache của checkout gốc, không cài/nâng Editor, SDK hay dependency.
- Trước sửa: compile Unity thành công; `LootDropPresentationTests` (5 ca) và
  `CombatLootTests` (6 ca) **11/11 PASS**. Mono `--loot-visuals` **21 nhóm PASS**.
- Thêm hồi quy resize: ba tỷ lệ 600/800/1150 đều **FAIL trên bản PR**, kiếm bay
  bị kéo lệch lần lượt 110,66 / 147,54 / 212,10 pixel khi battle projection đổi
  trong lúc không tăng thời gian. Nguyên nhân: FlightStart và root còn thuộc
  BattleArea. Sửa bằng Canvas/pixel màn hình riêng cho pha bay.
- Sau sửa: Unity Play Mode **16/16 PASS**, không skip: 10 ca presentation và
  6 ca combat loot. Bao gồm hồi quy resize, Pause/Resume thật qua
  CombatManager.Update ở cả rơi/chờ/bay, RectTransform đích di chuyển, 48 hình
  tối đa, Destroy qua frame thật và dọn Canvas khi disable.
- Mono/UnityStub `bash Tests~/run.sh --loot-visuals`: **23 nhóm PASS** gồm 6
  nhóm presentation và 17 nhóm combat loot. Dùng mono/mcs đi kèm Unity qua
  wrapper PATH tạm, không cài công cụ. Cảnh báo compiler không thuộc diff.
  Dòng `[LOOT TEST A] FAILED ...` trong ca cố tình làm hỏng inventory là lỗi
  được tiêm có chủ đích; assertion xác nhận không visual/không chèn đồ/không
  thử lại. EXP/wave vẫn đánh dấu dirty như hành vi hạ quái hiện có.
- `git diff --check`: đạt. GUID/.meta checker cuối: **0 lỗi, 3 cảnh báo** thiếu
  metadata thư mục Lake/Sea/Steppe đã có từ baseline; không sửa asset ngoài task.
  Metadata do Editor tự sinh trong worktree thử không đưa vào commit.
- MainGame bản sao đã chạy trong Editor với save riêng có GUID/marker dưới
  `.utmp/loot-preview-*`. GameObject chứa SaveManager được lưu inactive;
  bootstrap kiểm tra đường dẫn/reparse point rồi gọi
  ConfigureSaveDirectoryBeforeAwake trước khi kích hoạt. Không truy cập player
  Saves. Test A và trạng thái mở map chỉ cấu hình trên instance thử, không sửa
  scene, cờ hoặc dữ liệu gốc.
- Đã chụp 34 frame mỗi cỡ **600×338 / 800×450 / 1150×647**: flash, bật/rơi,
  nằm đất, thu nhỏ/bay cong/mất hình; camera cuộn, quái cuối đợt và 3 quái chết
  cùng lúc. Giữ 40 pixel vì đã nhìn rõ trên ảnh native. Đã Pause/Resume khi
  đang rơi; resize Game View 800→1150 khi Pause ở pha bay, xác nhận vị trí pixel
  không đổi và Resume kết thúc ở mép phải mới.
- Các handler thật Load/Retry/Travel/New/Menu đã chạy trên save thử: có loot
  sống trước mỗi hành động, Count về 0 ngay sau hành động; không thưởng thêm.
  Load khôi phục snapshot đã lưu, New làm trống kho theo luồng hiện có; các
  hành động khác giữ số món đã nhận. Travel dùng fixture đánh dấu map hiện tại
  đã clear để đi tới hàng xóm hợp lệ. Manual Load về menu đúng thiết kế, harness
  Deploy lại trước ca độc lập tiếp theo. Harness/scene thử không đưa vào commit.

Giới hạn: các độ phân giải trên là **Game View thật của Editor** (Screen.width
đã kiểm), không phải resize cửa sổ Windows Player. Công cụ chuột native gặp lỗi
SendInput nên thao tác nghiệm thu chạy qua harness gọi handler; chưa chứng minh
click nút và cửa sổ trong Player. Console có lỗi sẵn từ UnityEditor.Search khi
khởi động và cảnh báo video/TLS; không có
exception từ LootDropPresentation. Không sửa engine/camera/terrain ngoài phạm vi.

### Kiểm tra trước đây của PR #10 trong Cloud (lịch sử)

- PNG đọc được, có alpha thật; đối chiếu import setting và checksum.
- Metadata checker trên **snapshot nguồn cục bộ**: 0 lỗi; 5 cảnh báo folder meta
  do snapshot chỉ lấy mã, không lấy toàn bộ Assets/scene/package. Không phải
  kết quả kiểm tra toàn bộ dự án Unity.
- `bash -n Tests~/run.sh` và `git diff --check`: đạt.
- `bash Tests~/run.sh --combat-loot`: bị chặn vì `mcs: command not found`.
  Không có Mono/.NET compiler hoặc Unity trong phiên này; không cài SDK hay
  dependency, không tuyên bố Mono/native/Player PASS.
- Thêm 4 nhóm hồi quy `--loot-visuals` (chạy kèm 20 nhóm loot hiện có): awarded/
  duplicate/pool/final-wave, Off/nonlethal, pan/wave/hard clear, missing art.
- Thêm **5 ca Play Mode** `LootDropPresentationTests`: 3 tỷ lệ projection,
  ngân sách/dọn hình qua frame thật, template/ID/RNG. Test dùng object riêng,
  từ chối scene có manager/actor, không SaveManager/save I/O. Chưa chạy.

### Nghiệm thu trong Unity trên máy phát triển

1. Dùng profile chơi thử riêng; chạy MainGame và bật Controlled Loot Test A như
   trước. Hạ một quái rồi quái cuối đợt: kho tăng đúng một món/kill, thấy chuỗi
   flash → rơi → nằm 0,5s → thu nhỏ/bay sang phải → mất hình.
2. Kiểm camera cuộn và quái pooled: kiếm không bám xác hoặc màn hình khi nằm đất;
   khi bay không bị kéo theo mặt đất. Nhiều quái chết tạo các hình độc lập.
3. Kiểm cửa sổ 600/800/1150, resize cả lúc nằm và đang bay: icon đọc được,
   đích vẫn là mép phải cửa sổ thật. Kiểm không bị panel/mask UI che mất đường bay.
4. Pause lúc rơi/chờ/bay rồi Resume; đổi wave, Retry/Travel/Load/Menu: không sót
   hình cũ, không nhận thêm đồ do animation kết thúc/hủy. Không test trên save thật.
5. Test Runner → Play Mode, scene trống: chạy `LootDropPresentationTests`, rồi
   `CombatLootTests`. Có Mono thì chạy `bash Tests~/run.sh --loot-visuals`.

Còn nghiệm thu Windows Player: dùng profile save cô lập tương tự, hạ quái cuối
đợt rồi resize cửa sổ thật 600/800/1150 khi đang bay; bấm Pause/Resume và các nút
Load/Retry/Travel/New/Menu. Kiểm cả Inventory Target được gán sau này. Kết quả
Editor/Mono ở trên không thay thế PASS của Windows Player hoặc profiling dài hạn.
