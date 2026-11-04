# Phát hiện Hero và đánh bất ngờ — 04/11/2026

Tiếp nối bản loot `31e5e6c` đã được người phát triển nghiệm thu. Không đổi save
schema, thưởng, EXP, damage formula hoặc camera; chỉ thêm trạng thái AI và hệ số
đòn bất ngờ theo yêu cầu mới.

## Hành vi

- Mỗi vòng đời pooled bắt đầu chưa phát hiện, nhìn trái/phải ngẫu nhiên. Sau
  mỗi khoảng ngẫu nhiên 1,5–5 giây gameplay thì đổi sang hướng đối diện; dùng
  System.Random riêng, không rút thêm Unity combat RNG.
- Quái chưa phát hiện không tự bước tới Hero. Nó chỉ nhận cùng displacement
  với Ground qua bù ground/camera hiện có, không đứng cố định trên màn hình.
- Tầm nhìn là nửa mặt phẳng trước hướng nhìn và khoảng cách 2D BattleArea-local
  **≤500** (có xét Y), tới Hero đang deployed, gồm cả đồng đội B2.
- Một quái nhìn thấy Hero sẽ báo động mọi quái còn sống thuộc encounter hiện
  tại. Tất cả hiện `?` vàng 24 pixel, đứng trên Ground và không được tấn công
  trong **0,5 giây**. Sau đó cùng đổi sang `!` đỏ, quay về Hero và bắt đầu đuổi/
  đánh theo range, tốc độ, cooldown và lựa chọn mục tiêu hiện có.
- Đánh trúng quái **chưa phát hiện**: nhân đôi sát thương đã tính phòng thủ của
  lần impact hợp lệ đầu tiên, giới hạn int.MaxValue; sau đó **cả nhóm đánh trả
  ngay**, bỏ qua pha chờ `?` theo xác nhận của người dùng. Quái đang hiện `?`
  đã phát hiện nên không chịu ×2, nhưng bị đánh sẽ kết thúc chờ cho cả nhóm.
- Trạng thái xét tại impact, không chụp lúc bắn. Đạn hụt, stale projectile,
  callback trùng hoặc target từ vòng đời cũ không được kích hoạt báo động.
  Một impact đã làm cả nhóm phát hiện; các impact tiếp theo (kể cả các target
  tiếp theo của AoE hiện có) không còn được ×2 vì nhóm đã cảnh giác.
- Kể cả đòn bất ngờ kết liễu mục tiêu, đồng đội còn sống vẫn được báo động trước
  despawn. Loot/EXP vẫn theo guard nhận thưởng hiện có.
- Đã phát hiện thì không tự quên do Hero quay ra sau/đi xa; không bổ sung leash
  hay cơ chế mất dấu chưa được yêu cầu. Khi không còn mục tiêu sống, quái đứng
  với Ground. Pool/new encounter/clear/death không giữ trạng thái từ đời cũ.

## Integration

EnvironmentManager gọi CombatManager.TickMonsterAwareness sau Hero movement và
trước monster movement. Tất cả timer tiến trước khi broadcast phát hiện mới để
không lệch một frame do thứ tự danh sách. Thời gian dừng cùng Pause; Encounter/
spawn identity được kiểm trước khi cập nhật hoặc báo động.

MonsterController quản lý trạng thái, timer nhìn và một TMP marker tái sử dụng.
Marker theo chân/thân quái và bù Canvas scale để giữ 24 pixel khi resize. Hướng
nhìn chỉ lật body child trong CultivationSpriteBridge, không lật HP/text/root.
CanAttack cùng vòng combat đều chặn quái chưa Engaged.

## Kiểm thử trên máy

- Unity 6000.5.0f1: nhóm MonsterAwarenessTests + LootDropPresentationTests +
  CombatLootTests + WeaponBasicAttackTests, **31/31 PASS**, không skip.
- Mono `--monster-awareness`: **14 nhóm PASS**, gồm 3 attack mode đứng theo
  Ground, hai hướng và biên 500/Y, interval/RNG, đồng bộ cả nhóm 0,5s, Pause,
  ×2 một lần, hit trong `?`, pool/stale life, lethal/loot, đạn hụt và phát hiện
  trong khi đạn đang bay.
- Các nhóm Mono loot visuals, weapon, formation/B2, multi-zone camera, ranged
  lanes, sprite và enemy sprites đều PASS. Fixture combat cũ được khai báo rõ
  là đã Engaged; các assertion sát thương/movement gốc được giữ nguyên. Ca mới
  chọn production unaware spawn tường minh.
- `--distance-waves` dừng tại assertion `only an actually incomplete threshold
  may wait another frame`; `--magic-persistence` qua 9 nhóm rồi dừng tại `valid
  magic launch`. Đã chạy lại trên bản gốc `31e5e6c` và gặp đúng hai lỗi này;
  không sửa/giảm assertion ngoài phạm vi AI.
- Chụp MainGame ở Game View 600×338 / 800×450 / 1150×647: quái nhìn ra sau khi
  idle, ba marker vàng `?`, marker đỏ `!`, và cả nhóm đánh trả ngay khi bị đánh
  bất ngờ. Có kiểm Pause giữa hai trạng thái.

Profile render là bản sao scene, SaveManager owner inactive, đường dẫn GUID và
marker riêng được kiểm tra trước ConfigureSaveDirectoryBeforeAwake/activation.
Không truy cập player Saves. Harness, ảnh và slot giả không đưa vào runtime repo.
Chưa nghiệm thu Windows Player hoặc profiling dài hạn; không coi Mono/Editor là
Player PASS. Không tự merge main hay sửa scene local đang được người dùng chơi.
