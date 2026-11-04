# Hiển thị nhận Thiết Kiếm khi hạ quái

Ngày: 04/11/2026. Baseline `a774218ae613d56e9ab703de78f19eecc6a4d1fe`.
Đã thêm mã và sprite; **chưa compile/chạy Unity hoặc Windows**.

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
theo projectile/camera. Khi bay, hình rời ground sorting/pan, đi tới điểm screen
được chuyển đổi về BattleArea-local bằng Canvas thật; không giả định xMax của
BattleArea chính là mép cửa sổ. Kích thước icon bù scale Canvas khi resize.

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

## Xác minh và giới hạn

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

Chưa có bằng chứng render/SFX/Windows/hiệu năng từ phiên này; cần quan sát trực
tiếp tốc độ, độ sáng, Y-depth và độ rõ ở kích thước thật trước khi nghiệm thu.
