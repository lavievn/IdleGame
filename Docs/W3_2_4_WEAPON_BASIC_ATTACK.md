# W3.2.4 — Weapon-driven basic attacks

Ngày 28/10/2026. Baseline `main` và `origin/main` sau fetch:
`d239347c558be159ece02e7df6b9962a76afce05`, working tree sạch trước sửa.
Đây là thay đổi gameplay theo yêu cầu W3.2.4; chưa nghiệm thu Unity/Windows.

## Hành vi và nguồn dữ liệu

| Vũ khí | Template ID | Basic attack | Main stat mẫu |
|---|---|---|---|
| Unarmed | Không có Weapon hợp lệ | Melee / Physical | Không bonus vũ khí |
| Sword | `sample:iron_sword` | Melee / Physical | Physical Attack +10 |
| Bow | `sample:basic_bow` | RangedPhysical / Physical | Physical Attack +10 |
| Staff | `sample:basic_staff` | RangedMagic / Magical | Magical Attack +10 |

`EquipmentTemplate.weaponFamily` là dữ liệu cấu trúc, lookup bằng template ID;
không đọc tên hiển thị. `WeaponBasicAttack` resolve món thực sự ở slot Weapon.
Mở rộng catalog cần khai báo family tương ứng, không cần thêm field vào save item.
Factory Common Sword và ControlledLootTest Profile A giữ nguyên kết quả cũ.
Bow +10 dùng cùng giá trị thử với Sword, không phải thiết kế balance mới.

`CombatStatId.Attack` vẫn ID **1**, nay mang nghĩa Physical Attack trong basic
attack của Hero chính. Physical = `baseDamage + addedDamage`, qua evaluator
`CombatStats` hiện có cùng các modifier Attack hợp lệ. Hero cấp 1 bình thường
có Physical 10. MagicalAttack được **append tại ID 15**, raw base **0**, qua cùng
evaluator với modifier MagicalAttack. Staff mẫu cộng 10, không nhận thêm Physical.
Không thêm magical growth, phân bổ điểm, crit, Mana, võ học hoặc skill.

Attack legacy từ charm/nhẫn/affix vẫn là Physical, không convert sang Magical,
không áp hai lần. Điều kiện tag cũ vẫn có hiệu lực: bonus `stance:melee` chỉ có
hiệu lực khi profile thực là Melee; không xóa/sửa tag hoặc giá trị item cũ.
Affix ẩn/khóa vẫn bị loại bởi EquipmentRuntime và EquipmentStatPreview.

## Đồng bộ actor và combat

- Setup/Load/Retry bind Hero chính vào data thật và resolve vũ khí. ChangeAttackMode
  và callback GameManager.SetHeroAttackMode không còn quyền chọn thế cho Hero này.
  Giá trị chỉnh tay trong Inspector được resolve lại khi tick movement/combat.
- Equip/Unequip **Weapon thành công** gọi RefreshWeaponAttack cho đúng controller:
  resolve mode/range, bỏ target và lane intent cũ, đổi sprite role/reset pose,
  hủy timer/SFX chuẩn bị, giấu charge, bắt đầu một interval mới. Không phóng đòn
  từ handler. Thay cùng family cũng phải chờ interval, không tạo hit miễn phí.
- HP tuyệt đối được giữ và chỉ clamp xuống MaxHP mới nếu cần; không tăng HP vì
  đổi vũ khí. Công thức MaxHP theo mode cũ vẫn còn (Melee ×2, Bow ×0.6, Magic ×1
  trên base HP, cộng các phần hợp lệ). Compare phản ánh việc này. Không đổi các
  hệ số đó để cân bằng vũ khí, không teleport/reset life/map hoặc đổi camera.
- Failed equip/unequip không refresh combat. Nhận đồ không equip. Non-Weapon
  tiếp tục đường refresh stat cũ, không resolve thành loại đòn khác.
- Projectile đã phóng giữ attack base, roll, multiplier, offensive boost, roots
  và damage school tại release; phòng ngự mục tiêu được đánh giá lúc impact
  như trước. Đổi vũ khí không clear volley trong encounter còn hợp lệ.

Staff dùng countdown basic interval **1.4 giây / attack speed hiện có**, kể cả
giới hạn speed của CombatBalance. Không có clock cast 2 giây, vận chiêu, charge
bar hay cooldown kỹ năng. Khi eligible và hết interval, phóng một beam sáng
placeholder hiện có, đi thẳng đến endpoint đã chốt. Dùng **Magical** school để
xét kháng phép, multiplier basic Magic **×1**, biến thiên roll/linh căn/defense
vẫn theo kernel cũ. Không có AoE, impact explosion hay detached magic tail.
Nguồn Magical 0 cho damage 0, không tạo minimum-one giả từ Physical.

Beam chỉ kiểm mục tiêu gốc, hit một lần khi tới endpoint và còn trong tolerance
cũ. Mục tiêu di chuyển có thể né như projectile fixed endpoint hiện tại; không
tự homing/retarget. Guard actor life/spawn, monster life/spawn, battle version,
map/run và accepted kill/loot của W3.2.2 vẫn được sử dụng. Đạn hết hiệu lực bị
retire; Staff không đi vào nhánh visual nổ của Magic legacy.

## UI, preview và dev item

Ba control scene **Kiếm / Cung / Phép** bị ẩn và tắt tương tác khi UIManager khởi
tạo; bỏ binding/layout/selection của các control này trong runtime UI. Giữ scene
và UnityEvent cũ an toàn qua callback tương thích, không sửa prefab/scene.
Character stats hiện có hiển thị family, Physical Attack và Magical Attack.

EquipmentStatPreview mô phỏng **assignment hiện tại và sau thao tác**, resolve
mode riêng cho mỗi bên. Tham số mode cũ được giữ cho tương thích API, không ghi
đè weapon. Cả hai nguồn Attack dùng lại WeaponBasicAttack/CombatStats. Compare
hiện dòng Physical/Magical thay đổi, trang profile đổi nếu có, và các stat khác
thực sự đổi (bao gồm MaxHP). Preview không thay data, mode actor, HP, timer hay
isDirty; điều kiện affix và quy tắc ring vẫn đi qua logic cũ. Không đổi scale/layout
chung; vẫn dùng phân trang detail sẵn có tại 600/800/1150.

Trong Unity Editor Play Mode, sau khi vào gameplay:

1. **Sword Common +10:** bật Controlled Loot Test A trên GameManager, hạ một quái
   hợp lệ; Sword vào bag như W3.2.2. Factory độc lập: CreateDemoSword(). Nút cũ
   “Kiếm rèn mẫu” vẫn là đồ thử rèn, không dùng nó để đối chiếu chính xác Common.
2. **Bow/Staff:** menu component GameManager → **DEV — Grant Bow and Staff (Play Mode)**.
   GrantDevWeaponSamples() chỉ có hiệu lực Editor/DEVELOPMENT_BUILD và đang ở
   gameplay; cấp hai món vào bag, không equip. Factory: CreateDemoBow/CreateDemoStaff.
   Không tự grant khi New/Load, không đổi loot progression. Player dev chưa có
   nút grant Bow/Staff riêng trên HUD.
3. Hòm đồ → chọn item → đọc Compare → Trang bị; chọn món đang mặc → Tháo.
   Có thể giữ nhiều mẫu, mỗi lần grant là GUID mới, không bật random loot.

## Save compatibility và B2

- Không sửa SaveManager, save schema/checkpoint hay thứ tự AttackMode/EquipmentSlot.
  Tất cả CombatStatId cũ 0–14 giữ nguyên. New stat được serializer lưu bằng ID15.
- savedAttackMode vẫn tồn tại để không phá JSON/Unity serialized enum; không
  quyết định basic attack sau Load. Capture save có thể cập nhật cache bằng
  mode resolve thực, không lưu lựa chọn tay.
- Quy tắc migration không mutation: save Sword cũ đã có `sample:iron_sword` nên
  lookup family Sword từ catalog mới, giữ instanceId, main/extra stats, slot và
  lịch sử rèn. Save không có Weapon hợp lệ → Unarmed Melee, dù cache là Magic/Bow.
  Không đoán family từ tên hoặc reroll/đổi item. Dữ liệu template không được hỗ
  trợ vẫn theo guard/opaque preservation hiện hữu của EquipmentRuntime.
- RegisterAdditionalHero đánh dấu fallback B2 tạm một cách tường minh: vẫn dùng
  mode/test behavior, charge/Magic legacy của actor đó, stats/timer/life độc lập.
  Không thêm persistent equipment hay party save cho B2. Monster đi đường cũ.

## Kiểm thử đã chạy và giới hạn

**Mono/UnityStub**, không phải Unity native:

| Runner | Kết quả |
|---|---|
| `bash Tests~/run.sh --weapon-basic` | **31 PASS** |
| `--inventory-compare` | **18 PASS** |
| `--combat-loot` (kèm balance/legacy B2/monster attack contracts) | **41 PASS** |
| `--hero-sfx` | **17 PASS** |
| `--enemy-sprites` | **6 PASS** |
| `--archer-animation` | **5 PASS** |

Tổng **118 PASS**, không FAIL trong các nhóm trên. Nhóm mới bao phủ toàn bộ 22
trường hợp của yêu cầu, thêm thay cùng family, zero Magical, affix ẩn/khóa và lethal Staff → EXP/loot A.
Kiểm tra save chỉ là JSON/snapshot **trong bộ nhớ**; serializer UnityStub là
test double, không chứng minh native serialization hoặc SaveManager disk I/O.
Không chạy suite save I/O hay full runner có fixture save; không mở/sửa save thật.

Legacy fixture Field/LootField chọn fallback rõ ràng để giữ nguyên các assertion
Magic AoE/charge cho đường legacy/B2, không dùng chúng làm bằng chứng primary
Staff PASS. WeaponField mới bind primary qua SetupHeroInfo và kiểm tra không
fallback. Hai kỳ vọng preview cũ được đổi theo yêu cầu loại bỏ chọn thế tay;
các assertion ring, hidden/locked, purity và layout vẫn giữ. Fixture B2 native
chuyển primary mode bằng **equip mẫu thực**, giữ các assertion vị trí/VFX cũ.

Thêm WeaponBasicAttackTests.cs + .meta: **6 ca native** kiểm template, nguồn stat,
JsonUtility round trip, preview purity và enum/legacy compatibility trong bộ nhớ.
**Chưa compile/chạy Unity Editor.** C# game + Mono regression compile thành công;
các cảnh báo compile cũ còn tồn tại. GUID checker: **0 ERROR, 62 WARNING** về
tham chiếu package chưa xác nhận khi thiếu PackageCache (như baseline).
Agent Skills verifier và git diff --check PASS; không cập nhật dependency/skill.

## Smoke test Unity/Windows cần nghiệm thu

1. Unity **6000.5.0f1**, profile test riêng, MainGame → New/entry/VFX → combat.
   Không vũ khí: Melee, Physical10/Magical0 ở level1; ba nút chọn thế đã biến mất.
2. Lấy Sword Common bằng A, grant Bow/Staff bằng menu dev. Kiểm Compare/Equip/
   Unequip chuỗi Sword → Staff → Bow → Unarmed ở **600/800/1150**, cả mở rộng và
   fallback overlay: không che Equip/Tháo/Close, text và đổi trang đọc được.
3. Staff: Magical10 ở level1, không charge bar/vận/nổ; chỉ một mục tiêu mất HP,
   có nhịp nghỉ và áp speed. Dùng log để xem source10, ×1, roll/roots/defense;
   damage cuối không nhất thiết là10 do các hệ số hiện hữu.
4. Đổi vũ khí gần lúc release và khi đạn còn bay; không double hit, đạn cũ giữ
   school/power, HP không tăng, actor/camera không nhảy. Thay nhẫn/áo hoặc equip
   thất bại không đổi profile. Pause/close Inventory không để state/preview cũ.
5. Gọi B2, kiểm actor/timer/SFX riêng và Monster Magic legacy; kill cuối wave,
   farm/chuyển map/Retry không có hit/loot từ projectile đời cũ; A vẫn rơi Sword.
6. Chạy 6 ca WeaponBasicAttackTests và B2SpawnPositionTests từ Play Mode Test Runner
   trong scene trống theo README test. Với bộ dữ liệu test riêng do người dùng
   sở hữu, kiểm Save → thoát → Continue/Load, cache mode lệch và save Sword cũ;
   xác nhận ID/slot/affix còn nguyên, profile theo weapon. Cloud chưa kiểm disk I/O.
7. Windows Development Player: xác nhận input/raycast, render tia, SFX/animation,
   tốc độ đánh, lifetime, layout và Save/Continue thực. Không coi PASS Mono/native
   pure-data là PASS hiển thị hay Player. Chưa profiling allocation/frame-time.

Không triển khai W3.2.3, võ học, random loot Profile B, party persistence hoặc
thay công thức progression/map/camera/EXP/rèn để ép smoke test PASS.
