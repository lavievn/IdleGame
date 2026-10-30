# Checklist nghiệm thu Unity native — W3.2.4 Weapon-driven Basic Attack

Ngày lập: 31/10/2026. Đối chiếu mã tại HEAD `a774218ae613d56e9ab703de78f19eecc6a4d1fe`.
**Trạng thái: CHƯA CHẠY các ca nghiệm thu dưới đây.** Đây là tài liệu kiểm thử,
không xác nhận Unity/Windows PASS và không nâng dự án lên Alpha.

## Phạm vi và nguồn đối chiếu

Chỉ nghiệm thu basic attack theo Weapon của Hero chính và hồi quy trực tiếp.
Không yêu cầu triển khai W3.2.3, skill/Mana/crit, loot Profile B, party persistence,
checkpoint mới hoặc đổi balance/camera/map để đạt checklist.

- [Đặc tả triển khai và giới hạn W3.2.4](W3_2_4_WEAPON_BASIC_ATTACK.md),
  [README hiện tại](../README.md), [trạng thái lịch sử 25/10](PROGRESS_2026-10-25.md).
- [WeaponBasicAttack.cs](../Assets/Script/WeaponBasicAttack.cs): family theo template,
  slot Weapon thực, hai nguồn Attack và interval.
- [HeroController.cs](../Assets/Script/HeroController.cs): bind primary/fallback,
  mode/range, lane/target, sprite và charge bar.
- [CombatManager.cs](../Assets/Script/CombatManager.cs): `RefreshWeaponAttack`,
  `TickRegisteredHeroAttack`, `Launch`, `TickProjectiles`, damage/lifetime guards.
- [GameManager.cs](../Assets/Script/GameManager.cs): Equip/Unequip, callback chọn thế
  tương thích, grant dev và capture cache mode.
- [EquipmentStatPreview.cs](../Assets/Script/EquipmentStatPreview.cs),
  [UIManager.cs](../Assets/Script/UIManager.cs): preview và UI runtime.
- [Native test và hướng dẫn fixture](../Assets/Tests/Presentation/PlayMode/README.md),
  [WeaponBasicAttackTests.cs](../Assets/Tests/Presentation/PlayMode/WeaponBasicAttackTests.cs),
  [regression Mono tham khảo](../Tests~/WeaponBasicAttackRegression.cs).

## Chuẩn bị và cách ghi kết quả

- Dùng Unity **6000.5.0f1** theo ProjectVersion; giữ package/backend hiện có.
  Ghi HEAD, Editor/OS, cấu hình balance/dev override, scene và kích thước cửa sổ.
- Test Runner dùng scene trống theo README fixture. Gameplay dùng `MainGame`.
  Không chạy fixture B2 trong scene có manager/actor, kể cả inactive.
- Chỉ dùng môi trường/profile thử riêng. Không đọc, ghi hay xóa save người chơi.
  New/Continue/Load/Retry có thể đi qua persistence: phải cô lập trước khi chạy.
  Ca disk I/O cần đường dẫn fixture riêng **trước Awake**, marker/quyền sở hữu
  và cleanup giới hạn như bộ SaveLoad hiện có; không chạy save I/O trong Cloud.
- Ca cần trạng thái khó dựng qua UI (stat 0, affix khóa, cache sai, reuse lifetime,
  failed handler) dùng fixture/debugger native cô lập; không sửa C#/asset production
  để ép kết quả. Chưa có fixture phù hợp thì ghi BLOCKED kèm điều kiện thiếu.
- Mỗi ID bên dưới ghi `NOT RUN / PASS / FAIL / BLOCKED`, bước tái hiện, actual,
  log/video/screenshot và môi trường. Với timer/HP/projectile ghi giá trị trước/sau.
  Tách lỗi có sẵn với lỗi mới; không đánh PASS chỉ vì nhìn thấy animation.

## Dữ liệu chuẩn

Hero cấp 1 sau balance mặc định, không trang bị/modifier khác và không dev override:

| Weapon thực | Template | Mode | Physical Attack | Magical Attack |
|---|---|---|---:|---:|
| Unarmed | Không có Weapon hợp lệ | Melee | 10 | 0 |
| Sword Common | `sample:iron_sword` | Melee | 20 | 0 |
| Bow mẫu | `sample:basic_bow` | RangedPhysical | 20 | 0 |
| Staff mẫu | `sample:basic_staff` | RangedMagic | 10 | 10 |

Sword Common: bật Controlled Loot Test A trên GameManager, hạ quái hợp lệ.
Không dùng nút “Kiếm rèn mẫu” để đối chiếu Common +10. Bow/Staff: trong Editor
Play Mode, menu component GameManager → **DEV — Grant Bow and Staff (Play Mode)**.
Grant chỉ thêm bag, không equip; mỗi lần cấp có instanceId riêng. Không tự grant
ở New/Load. Player Development chưa có nút grant Bow/Staff riêng trên HUD.

## A. Native Test Runner

| ID | Thao tác | Kỳ vọng / giới hạn |
|---|---|---|
| N01 | Chạy `WeaponBasicAttackTests` trong Play Mode Test Runner. | 6 ca được discover và PASS: 3 TestCase family/stat, 1 JsonUtility round trip, 1 Compare purity, 1 legacy Sword/enum. Ghi compile error riêng nếu có. |
| N02 | Chạy `B2SpawnPositionTests` theo README trong scene trống. | Assertion vị trí/VFX/movement hiện có vẫn đạt; primary đổi mode qua equip Bow/Staff thực. Không coi Canvas scale mô phỏng là resize cửa sổ Windows thật. |

N01 dùng JsonUtility thật nhưng chỉ dữ liệu trong bộ nhớ; không chứng minh
SaveManager disk I/O, combat nhiều frame, render, SFX hay Player. Các con số
Mono/UnityStub PASS trong tài liệu triển khai là kết quả lịch sử, không phải kết
quả native của checklist này. Các ca dưới đây không mặc nhiên đã có automated test.

## B. Weapon authority, thao tác trang bị và trạng thái actor

| ID | Thao tác | Kỳ vọng |
|---|---|---|
| W01 | New trong profile thử; qua entry/VFX, chưa equip. | Unarmed Melee, stat theo bảng; gameplay chỉ mở sau entry hiện có, không đánh xuyên khóa. |
| W02 | Nhận Sword A, grant Bow/Staff; mở bag trước equip. | Item đúng template/stat; profile/timer không đổi chỉ vì nhận đồ. A vẫn chỉ cấp Sword Common +10. |
| W03 | Equip chuỗi Sword → Staff → Bow → tháo về Unarmed. | Mỗi bước đúng bảng; mode/range và sprite role theo Weapon thực, target/lane intent cũ được bỏ; không cần nút chọn thế. |
| W04 | Đổi tên hiển thị trong dữ liệu fixture; thử Weapon không hợp lệ/không hỗ trợ qua guard hiện hữu. | Tên không quyết định family; không có Weapon hợp lệ resolve Unarmed. Dữ liệu không hỗ trợ được bảo toàn theo guard hiện hữu, không tự đoán family/reroll. |
| W05 | Với Staff đang mặc, gọi callback cũ `SetHeroAttackMode`/`ChangeAttackMode`; chỉnh attackMode Inspector rồi tick. | Primary quay về mode của Weapon; callback cũ an toàn, không chiếm quyền chọn thế. |
| W06 | Đổi vũ khí khi gần hết interval, cả Sword→Sword và Staff→Staff khác instance. | Hủy chuẩn bị/SFX cũ, giấu charge; bắt đầu interval mới. Handler không phóng đòn, không hit miễn phí/double release. |
| W07 | Ghi HP tuyệt đối rồi đổi qua các family khi HP dưới và trên MaxHP mới; thử HP=0 trong fixture. | HP mới = min(HP cũ, MaxHP mới), HP=0 vẫn 0; không hồi sinh/heal. MaxHP giữ công thức mode cũ (Melee ×2, Bow ×0.6, Magic ×1 và phần cộng hợp lệ). |
| W08 | Equip sai slot, ID không tồn tại, món đã mặc; tháo slot trống. | Handler thất bại không refresh weapon, không reset timer/target/HP hay phát đòn. |
| W09 | Equip/tháo áo, nhẫn, charm/Artifact hợp lệ khi đang Staff. | Giữ profile Staff; stat/timer đi đường refresh non-Weapon hiện có, không đòi timer giữ nguyên như ca thất bại. Attack legacy vẫn chỉ là Physical. |
| W10 | Quan sát tọa độ/camera khi đổi trong combat và exploration. | Không teleport/reset life/map hoặc đổi camera; lane intent mới theo mode, không kế thừa target cũ. |

## C. Nhịp đánh, damage và projectile

| ID | Thao tác | Kỳ vọng |
|---|---|---|
| C01 | Cho Staff đánh một mục tiêu trong tầm qua nhiều chu kỳ. | Countdown basic 1.4 giây / speed theo CombatBalance và giới hạn hiện có; không cast 2 giây, không charge bar/skill cooldown. Có nghỉ giữa các đòn. |
| C02 | Thay attack speed bằng dữ liệu fixture, kiểm cả giới hạn speed. | Interval theo `WeaponBasicAttack.Interval`; không dùng clamp movement 1/30s cho timer/damage. Đo khi eligible, không tính thời gian pause/ngoài tầm là lỗi nhịp. |
| C03 | Staff bắn vào cụm nhiều quái, xem trace và HP từng quái. | Beam sáng placeholder thẳng, chỉ target gốc hit một lần; Magical school, basic ×1; không AoE/nổ/detached magic tail. Source mẫu 10, damage cuối còn phụ thuộc roll/roots/boost/defense, không buộc bằng 10. |
| C04 | So sánh Armor/MagicResistance trong fixture, giữ các đầu vào khác cố định. | Staff dùng phòng ngự Magical; Sword/Bow dùng Physical. Kernel roll/linh căn/defense hiện có được giữ. |
| C05 | Staff có MagicalAttack=0; thêm Attack legacy lớn, affix Magical ẩn/khóa. | Damage nguồn Magical vẫn 0, không minimum-one từ Physical. Affix ẩn/khóa không cộng ở runtime lẫn preview. Bonus `stance:melee` chỉ hoạt động ở Melee, không cộng hai lần. |
| C06 | Di chuyển target khỏi endpoint đã chốt, đưa quái khác vào endpoint. | Beam không homing/retarget; target gốc ngoài tolerance có thể né, quái thay thế không nhận hit. |
| C07 | Phóng Staff rồi đổi Sword/Bow; phóng Bow rồi đổi Staff trước impact. | Đạn cũ hợp lệ tiếp tục; giữ attack base/roll/multiplier/offensive boost/roots/school tại release. Defense target lấy lúc impact; không clear volley chỉ vì đổi Weapon. |
| C08 | Khi đạn bay: target chết, pool tái dùng, Hero chết/redeploy; kết thúc battle, Travel/Retry sang encounter mới. | Guard life/spawn/battle/map/run chặn hit đời cũ; projectile hết hiệu lực retire. Staff không vào nhánh nổ Magic legacy, không hit/loot lên object tái dùng. |
| C09 | Staff gây lethal, kể cả quái cuối wave; quan sát callback trùng trong fixture. | Chỉ target hợp lệ chết, EXP và Profile A nhận đúng một lần; không nhân đôi reward khi impact/callback/đạn cũ lặp. Wave/farm/travel giữ luồng hiện có. |
| C10 | Kiểm Sword, Bow, Staff có/không target, đổi gần release và lúc bị hit. | Sprite/SFX đúng loại và một sự kiện ở release; không SFX từ preview/impact, không charge Staff. Attack/Hit/death/reset pose không kẹt animation. |

## D. UI và Compare

Lặp U01–U04 ở cửa sổ **600/800/1150**, cả detail mở rộng và fallback overlay.

| ID | Thao tác | Kỳ vọng |
|---|---|---|
| U01 | Khởi tạo UI, mở Character/Inventory. | Ba control Kiếm/Cung/Phép ẩn và không tương tác/raycast; family, Physical Attack và Magical Attack đọc được. Không yêu cầu xóa UnityEvent/scene cũ. |
| U02 | Compare Sword→Staff→Bow và preview tháo Weapon. | Current/future resolve riêng theo assignment; số Physical/Magical, profile và MaxHP khớp sau thao tác thật. Unarmed là preview sau tháo. |
| U03 | Chỉ chọn/đổi trang/đóng Compare, không equip; ghi snapshot trước/sau trong fixture. | Không đổi data/modifier/isDirty/mode/HP/timer do preview. Khi gameplay vẫn chạy, tách biến động combat tự nhiên khỏi mutation do preview. Affix/ring giữ quy tắc cũ. |
| U04 | Mở/đóng Inventory, pause/resume, đổi item liên tục và dùng Equip/Tháo/Close. | Không preview cũ sau thao tác; text/phân trang đọc được, nút không bị che, input/raycast đúng. Không đòi layout HUD mới chưa triển khai. |

## E. Persistence, B2 và Windows

| ID | Thao tác | Kỳ vọng |
|---|---|---|
| P01 | Native in-memory: round trip từng Weapon với cached savedAttackMode cố ý sai; thêm ca không Weapon nhưng cache Bow/Magic. | Profile theo assignment; không Weapon → Melee. ID, slot, main/extra stats, affix, lịch sử rèn giữ nguyên; không đoán từ tên/reroll. |
| P02 | Native fixture với Sword legacy và enum serialized. | `sample:iron_sword` resolve Sword không mutation; CombatStatId 0–14 giữ nguyên, Attack=1, MagicalAttack=15; AttackMode/EquipmentSlot không đổi thứ tự. |
| P03 | Chỉ trong môi trường save thử cô lập: Save → thoát → Continue/Load; Retry và load fixture Sword cũ/cache sai. | Bind primary vào data thật; profile/stat theo Weapon, dữ liệu item bảo toàn, không tự grant. Capture cache có thể ghi mode resolve; không đổi schema/checkpoint. Ghi rõ đường cô lập, không dùng save thật. |
| B01 | Gọi B2, cho đồng đội Magic đánh rồi đổi Weapon primary. | B2 fallback legacy vẫn có charge/Magic legacy; HP/timer/target/Image/life/SFX độc lập. Không chuyển đồng đội sang equipment primary hoặc thêm party save. |
| B02 | Quan sát Monster Magic và B2 qua combat, chết/reuse, Retry/Travel. | Đường legacy giữ behavior riêng; primary Staff đơn mục tiêu không bị trộn với Magic AoE/charge legacy. Camera/entry/B2 không hồi quy. |
| WIN01 | Chạy Windows Development Player từ cấu hình repo hiện có; lặp W03/W06, C01/C03/C07/C08, U01–U04 và persistence cô lập nếu đủ điều kiện. | Input/raycast, tia/SFX/sprite, lifetime, nhịp đánh và resize thật đạt. Chuẩn bị Bow/Staff bằng dữ liệu test riêng hợp lệ; ContextMenu Editor không phải nút Player. Thiếu đường cấp mẫu thì ghi BLOCKED, không thêm HUD mới. |

## Biên bản nghiệm thu

| ID / môi trường | Trạng thái | Actual / bằng chứng | Lỗi cũ hay mới / ghi chú |
|---|---|---|---|
| Chưa thực hiện | NOT RUN | Chưa có bằng chứng Unity native hoặc Windows trong tác vụ tài liệu này | Không suy diễn từ Mono PASS |

Chỉ chốt W3.2.4 sau khi người nghiệm thu điền kết quả và các ca FAIL/BLOCKED được
xử lý hoặc ghi nhận rõ. Native pure-data, gameplay Editor và Windows Player là
ba phạm vi bằng chứng riêng; không thay thế lẫn nhau. Profiling allocation/frame-time
chưa được chứng minh bởi checklist chức năng này.
