# W2.2m C — Magic tiếp tục bay, damage giữ luật hiện có

## Audit mã nền bab6f97

CombatManager.Launch lưu start/end, arc, FlightDuration, DamageTrace và định
danh life/spawn. RollDamageTrace diễn ra lúc phóng; HP thực sự bị trừ ở
TickProjectiles khi t>=1, sau kiểm tra hình học. Hero Magic là AoE tại end
cố định (heroMagicImpactRadius), enemy Magic nhằm một Hero. Không phải damage
ngay lúc phóng. Đổi mode không đổi attackMode đã lưu trong shot.

Các đường xóa gây mất hình: TickProjectiles bỏ shot nếu Hero/source invalid;
Update xóa hết khi không còn Hero sống; sau hit cuối wave ClearProjectiles(false)
xóa cả volley; StartBattle→ClearBattle(true) cũng xóa hết. End của đạn đã có,
nên vấn đề chính là cleanup/lifetime, không cần đổi camera hay homing.

## Bản sửa

- Projectile còn đủ điều kiện giữ công thức thời gian/quỹ đạo/damage cũ.
  Thêm battleVersion bên cạnh life/spawn để chặn epoch cũ.
- Khi quyền damage hết, Magic chuyển cùng Bolt sang MagicFlight (struct
  chỉ chứa start/end/arc/clock/Bolt, không có Hero, Monster, data hay trace).
  Đến end: một Burst lửa rồi xóa. Phần này không có đường gọi damage/EXP.
- PendingProjectileCount vẫn đếm shot có thể xử lý combat, không tính hình
  đã bị tước damage; DetachedMagicVisualCount cho biết phần hình còn bay.
- Wave mới, phục hồi wave thoát mép, DEVB hủy power cũ và Hero cuối chết dùng
  cleanup combat mềm: physical giữ cách hủy cũ, Magic tiếp tục hình ảnh.
- SetupHeroInfo, ForceClearAllMonsters từ Load/Retry/Travel/New Game/menu,
  disable hoặc mất BattleArea là ranh giới dọn toàn bộ, không mang đạn map cũ
  vào scene/map mới. Chỉ RecoverEscapedWave đổi lời gọi cleanup; không sửa
  lịch sinh wave, quãng đường, checkpoint hay dữ liệu map.
- Bộ đếm Update riêng ngăn flight bị cộng dt hai lần khi death callback chuyển
  volley sang danh sách hình trong cùng lượt. PanEffects pan start/end của
  phần hình và BattleEffects pan Bolt đúng một lần. Pause đóng băng clock.
- Hình orphan kết thúc không muộn hơn 10 giây gameplay từ lúc phóng, kể cả
  duration lỗi/vô hạn. Không clamp duration của shot còn đủ quyền damage;
  thời điểm impact hợp lệ giữ nguyên FlightDuration/attack interval cũ.

Hero Magic vẫn có thể nổ AoE lên các quái còn sống của CÙNG wave quanh end
đã chốt, dù mục tiêu gốc chết: đây là luật cũ, không phải retarget. Khi owner
chết/unregistered/respawn hoặc source enemy chết, chỉ hình tiếp tục; không
tạo damage sau khi điều kiện combat cũ mất hiệu lực. New wave đổi epoch và
retire toàn bộ shot cũ trước khi đăng ký quái mới. Không thêm hit/EXP hai lần.

BattleEffects dùng cùng UI mesh, không tạo GameObject/AudioSource cho tail;
List transfer không giữ dữ liệu actor đã pool/destroy. Sparks giữ cap 256
hiện có; flight rời danh sách và bolt rời mesh khi hết hạn hoặc hard reset.

## Kiểm chứng

17 ca Mono mới: target chết/null (mô hình destroyed-object), owner/source chết,
wave/pool mới, mode đổi vẫn đúng thời điểm hit, hai thứ tự callback volley
(đã/chưa step), TTL vô hạn, epoch cũ, pause/pan/explosion, Load/Travel/Retry/
New/Continue/Menu/Reset hard cleanup và escape soft cleanup. Các đường UI
dùng fixture GUID save tạm sẵn có; manual slot byte-identical, không save thật.

Cần Unity/Windows xác nhận: Destroy thật với Unity fake-null, pool OnEnable/
OnDisable, render mesh/burst đúng điểm, camera/parallax và thứ tự Update/LateUpdate,
game over overlay, frame dài, nhiều caster và Profiler memory qua nhiều wave.
Mono không phải bằng chứng đã nghiệm thu các phần native này.

Kết quả tích hợp Cloud: 638 PASS rồi dừng ở lỗi cũ MissingMemberException
GroundPresentation.fixedRect (TerrainRegression.cs:41); những ca phía sau
chưa chạy. Các nhóm mới Enemy 6, volume 6 (+3 legacy), Magic 17 đều PASS;
sprite cũ 28, opening font 9, Hero SFX 17 và formation/B2 39 PASS. Compile
UNITY_EDITOR: 16 cảnh báo cũ; UNITY_STANDALONE_WIN: 3 cảnh báo cũ, vẫn là
Mono/UnityStub chứ không phải Windows Player build. Không sửa Terrain test
để gọi toàn bộ suite là PASS.
