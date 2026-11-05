# Gear Drop v0.1 — prototype production

Ngày: 2026-11-05. Kết quả: đã bật nguồn gear trong MainGame, kiểm tra vòng progression tự động; chưa nghiệm thu Windows Player/native input hoặc balance cuối cùng.

## 1. Repository

- Branch/worktree giữ nguyên: `feature/monster-awareness-ambush`, `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- HEAD ban đầu: `879df33edf82c74a41f2ba619844187285f76a74`; working tree sạch.
- Commit mã/cấu hình đã kiểm thử: `ae050a7` — `feat: enable level-one Sword and Bow prototype gear drops`.
- Commit tài liệu chứa báo cáo này là HEAD bàn giao; SHA đầy đủ cuối cùng nằm tại `outputs/gear-v01/summary.json` ngoài repository.
- Không push/merge/đổi branch/reset repository, không sửa Firewall hoặc chạy Windows Player.

## 2. Catalog đã kiểm tra

Catalog nằm trong `Assets/Script/EquipmentFoundation.cs`, không có file EquipmentCatalog.cs riêng. `EquipmentCatalog` trong `EquipmentInventory.cs` là hai hộ phù legacy.

| Template ID hiện có | Slot / main stat | Prototype |
|---|---|---|
| `sample:iron_sword` | Weapon / Attack; Sword → Melee | Có |
| `sample:basic_bow` | Weapon / Attack; Bow → RangedPhysical | Có |
| `sample:basic_staff` | Weapon / MagicalAttack; Staff → RangedMagic | Chưa chọn |
| `base:head` | Head / Armor | Chưa chọn |
| `base:chest` | Chest / Armor | Chưa chọn |
| `base:boots` | Boots / MovementSpeed | Chưa chọn |
| `base:artifact` | Artifact / MaxHealth | Chưa chọn |
| `base:necklace` | Necklace / MagicResistance | Chưa chọn |
| `base:ring` | RingLeft hoặc RingRight / Attack | Chưa chọn |

Factory `CreateCommon` tạo GUID duy nhất, một main stat, Common, refine 0 và không có extra stat; vũ khí gắn điều kiện stance đúng family. Contract reward chấp nhận item level 1–999, main stat hữu hạn và >0; không tự đặt thêm trần balance. Không thêm template, slot, rarity hoặc affix.

## 3. Công thức liên quan

- Nhân vật mới không DEVB: `baseDamage = 10 + level - 1`, `addedDamage = 0`. Hộ phù Công Kích khởi đầu là +20 Attack ở Artifact, hộ phù Sinh Mệnh +100 HP; không bị thay thế/xóa bởi vũ khí.
- Physical = làm tròn `(baseDamage + addedDamage + flat modifiers) × (1 + percent modifiers)` theo bộ CombatStats. Magical dùng nền 0 và modifier MagicalAttack riêng. Điều kiện stance vẫn được áp dụng.
- `WeaponBasicAttack` lấy Physical cho Sword/Bow, Magical cho Staff. Đánh thường dùng `SynergyMath.EvaluateBasicDamage`, sau đó `CombatDamage.ResolveBasicSnapshot` áp boost/kháng. Hệ số Sword 1, Bow 0,7; roll 0,85–1, có làm tròn. Interval nền Sword 1,4 giây, Bow 1,4/1,5 giây. Vì thế một hit Cung không thể so ngang trực tiếp với hit Kiếm để kết luận giảm sức mạnh.
- Forge giữ nguyên `max(0,25; 1 + 0,025×Δlevel + 0,02×Δrefine + 0,14×Δrarity)`. Baseline của item giữ nguyên, không roll lại main stat.
- NormalUp tiêu 1 material, SpecialUp tiêu 10. Một Normal rồi một Special: multiplier 1 → 1,02 → 1,16, hiệu lực main stat 4 → 4,08 → 4,64. Chỉ số integer tăng +4 → +5; lần Normal đầu có thể chưa đổi damage integer do làm tròn.

## 4. Main stat prototype đã chọn

| Template | Level | Main stat | Lý do |
|---|---|---|---|
| `sample:iron_sword` | 1 | +4 Attack | Tăng 40% từ nền tay không 10; khi có charm +20, chỉ tăng 30 → 34 (~13,3%). Không thay charm, không vượt số demo +10 chỉ vì demo tồn tại. |
| `sample:basic_bow` | 1 | +4 Attack | Cùng nguồn Physical với Sword; giữ hệ số/tốc độ/ranged mode hiện có. So cùng mode cho thấy nguồn và damage tăng. |

Đây là lựa chọn prototype từ nền Attack 10 và charm +20, **không sao chép fixture 7/11**. Với nguồn không charm: 10 → 14 sau equip → 15 sau Normal+Special. Fixture xác nhận thay thế/tháo vũ khí chỉ giữ bonus món đang đeo, không stack.

Staff chưa đưa vào bảng: Magical nền 0 và charm khởi đầu tăng Physical, chưa có mức main stat phép cấp 1 để đối chiếu mà không thêm giả định cân bằng giữa hai đường. Các slot phòng thủ/HP/di chuyển và Ring chưa có mục tiêu độ lớn cho prototype này; Artifact còn dùng chung slot với charm legacy. Việc loại các template này giới hạn phạm vi, không phải lỗi catalog/factory. Contract Staff vẫn được kiểm tra và không thay đổi.

## 5. Cấu hình thực tế trong MainGame

Inspector: GameManager → Production Loot v0.1 → Equipment / Templates.

- `configured = true`, `controlledLootTest = false` (giữ nguyên).
- Material: Enabled, chance **0,60**, `material:refinement`, lượng **1–2** (giữ nguyên).
- Equipment: Enabled, chance **0,08**, policy **Uniform**, hai template ở mục 4; mỗi lần thành công đúng 1 Common, không dòng phụ ngẫu nhiên.
- Không phân loại thường/tinh anh/boss hoặc nhân thưởng Hard Mode.
- Dùng nguyên hai RNG, shared receipt và inventory/factory có sẵn; không thay `CombatLootRewards` hoặc `ProductionLootConfig`.
- Kiếm Common có thể hiển thị tên “Mộc Kiếm” theo DisplayName hiện có dù ID là `sample:iron_sword`. Cung giữ tên catalog “Cung mẫu”; không đổi artwork/tên/asset. VFX hình kiếm hiện chỉ dành cho template kiếm; Cung có phản hồi nhận đồ bằng log/Inventory hiện có.

## 6. Progression thực chạy

### Mono/UnityStub — từng template

`Tests~/run.sh --gear-drop` đọc block cấu hình thật từ MainGame. Dùng death/receipt, inventory, GameManager equip/unequip/replace/Forge handler và damage evaluator thật trong fixture; không phải gameplay Unity native.

| Phép đo, Hero cấp 1 không charm, đích trung tính | Sword | Bow |
|---|---|---|
| DamageSource: trước / equip / Forge | 10 / 14 / 15 | 10 / 14 / 15 |
| Damage roll 1: trước tay không / equip / Forge | 10 / 14 / 15 | 10 / 10 / 10 |
| Damage roll 0,85: equip / Forge | 12 / 13 | 8 / 9 |

Cung roll 1 vẫn ra 10 sau Forge vì làm tròn; roll 0,85 chứng minh Forge thay đổi damage thực. Không sửa công thức để làm đẹp số. Material Forge đến từ reward service, không nút phát nguyên liệu. Kiểm tra callback trùng, GUID khác nhau khi thay đồ, khoản tiêu 11, một history entry và charm +20 vẫn cộng đủ. Staff fixture chỉ kiểm tra tách Magical/Physical và chuyển mode, không được thêm vào production.

### Unity 6000.5.0f1 Editor Play Mode — MainGame thật

Mở bản sao scene bằng verifier sẵn có, profile GUID có marker, cấu hình Save directory trước Awake; không truy cập save người chơi. Flag mới: `-foundationValidationRun -foundationGearDropV01 -executeMethod MainGameFoundationProbe.RunIsolated`.

Verifier xác nhận asset .60/.08 rồi ép **hai chance thành 1 chỉ trong bộ nhớ** để có chuỗi xác định. Không ghi override vào scene hoặc Save schema. Không dùng controlled loot A, dev gear/material grant hoặc sửa damage quái để tạo kill.

Lượt cuối, profile `.utmp/foundation-e06856961b3c44a6ba23efd7f39fcc1c`:

1. Nhân vật mới không gear/material; Combat.Update hạ quái thật, nhận Cung + material. Callback death lặp không đổi số lượng.
2. Inventory mở/chọn/equip/unequip/re-equip qua hit-test/click routing và handler thực; Attack 10 → 14. Không phải input chuột native.
3. Đòn đánh tự động tiếp theo ghi trace dùng nguồn 14, đúng RangedPhysical.
4. Tiếp tục combat tự động đến đủ material; stock 12 → 11 → 1 sau Normal/Special. Nhân vật đã lên cấp trong lúc farm nên source tại Forge 15 → 16; multiplier và history khớp công thức.
5. Manual Save/Load qua GameManager, snapshot canonical khớp inventory/instance IDs/slots/main stat/Forge history/resource/chỉ số và combat state; file save không bị Load ghi lại.
6. Combat tự động sau Load dùng source đã khôi phục 16. Cả bốn trạng thái pha của nhánh verifier này PASS, failure rỗng.

Lượt Unity đầu cũng hoàn tất chuỗi nhưng đọc DEVB cá nhân đang để Attack 1000. Để đánh giá +4 ở nền thường, lượt cuối reset **static DEVB trong process thử trước tạo nhân vật**, không sửa/xóa `dev_balance.jsonl`. Cấu hình DEVB cá nhân trên máy vẫn giữ nguyên.

## 7. Mô phỏng nhẹ

Roll trực tiếp cấu hình MainGame, không tạo 1.000 GameObject/instance gear. Seed material `1105`, equipment `20261105`; từng stream có RNG riêng. Chạy một chuỗi 1.000, lấy mốc tích lũy; chạy song song cùng seed để kiểm tra tái lập.

| Kills | Lần nhận material | Tổng material | Gear | Sword | Bow | Cả hai | Không gì |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 60 | 85 | 6 | 3 | 3 | 3 | 37 |
| 500 | 290 | 429 | 23 | 11 | 12 | 8 | 195 |
| 1.000 | 600 | 901 | 70 | 35 | 35 | 39 | 369 |

Có ca thành công/không thưởng ở từng stream, đúng biên chance và endpoints chọn template. Kết quả này phát hiện lỗi xác suất rõ rệt và kiểm đếm; **không chứng minh balance hoặc trải nghiệm 1.000 kill thực tế**. Không đổi tỷ lệ theo mô phỏng.

## 8. Kiểm thử và bằng chứng

| Kiểm tra | Trạng thái |
|---|---|
| Mono `--gear-drop`, có UNITY_EDITOR | PASS 5/5 |
| Mono `IDLEGAME_TEST_DEFINES=RELEASE_VALIDATION`, `--gear-drop` | PASS 5/5; không phải Windows Player |
| Mono `--weapon-basic` | PASS 31/31, gồm Sword/Bow/Staff và lifecycle/chuyển mode |
| Unity Editor Play Mode verifier mới trên MainGame, nền thường | PASS bốn pha trong nhánh GearDropV01 |
| Metadata checker | PASS: 0 lỗi, 3 cảnh báo có sẵn |
| `git diff --check` / staged check | PASS |
| Windows Player, native mouse, ma trận UI/hình ảnh | NOT RUN; không khởi chạy Player hoặc đụng Firewall |
| Source gear prototype | Không BLOCKED: Sword/Bow hoạt động; Staff/slot khác chủ động chưa đưa vào |

Không có test FAIL còn tồn tại trong nhóm đã chạy. Không chạy lại Magic, UI matrix hoặc toàn bộ reward/SaveLoad suite đã PASS ở lượt trước khi các module đó không thay đổi. Snapshot Save/Load trên mã/cấu hình mới đã được verifier thực chạy, không lấy PASS cũ thay thế.

Log Unity có exception từ `UnityEditor.Search.SearchDatabase` lúc khởi tạo Editor; stack thuộc Editor search, không phải gameplay. Probe đã có bộ lọc lỗi Search từ trước tác vụ này; không thêm ignore để ép PASS, không tuyên bố toàn Console không có lỗi. 17 cảnh báo compiler Mono cũ không chặn biên dịch.

Bằng chứng ngoài Git: `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\gear-v01\` chứa `mono-round2.log`, `release-round2.log`, `weapon-regression.log`, `unity-cycle-summary.log`, `unity-cycle-result.json`, `expected-snapshot.json`, `actual-snapshot.json`, `metadata.json`, `summary.json`. Log đầy đủ còn ở `work/gear-v01/unity-cycle-round1.log` và `unity-cycle-round2.log` trong cùng workspace Codex. Không commit log/save/cache.

File thay đổi: `Assets/Scenes/MainGame.unity`; `Assets/Script/MainGameFoundationProbe.cs` (Editor-only, opt-in); `Tests~/GearDropRegression.cs`; `Tests~/MotionRegression.cs`; báo cáo này. Không sửa Combat/AI/camera/EXP/Forge formula, catalog/factory, inventory runtime hoặc Save schema. Artifact font cache và 15 metadata do các lượt Editor sinh ra đã được dọn về trạng thái ban đầu sạch.

## 9. Rủi ro và giới hạn

- Nguồn production .08 hoạt động qua cấu hình thật; chuỗi Unity xác định dùng override chance trong process thử, không phải đo thời gian chờ gear ở 8%.
- Unity native cycle quan sát Cung; Sword đã kiểm bằng fixture Mono và hồi quy weapon. Chưa bấm UI bằng input native hoặc nghiệm thu hình ảnh/Player mới.
- Profile DEVB cá nhân đang che mức tăng nhỏ của gear (+4 trên Attack 1000); khi chơi thử balance cần tắt DEVB bằng công cụ hiện có. Không tự thay file cá nhân.
- Không cam kết balance cuối cùng, mọi template đều rơi hoặc một lần rèn luôn tăng damage integer ở mọi roll. Staff chưa có nguồn drop trong prototype này.

## 10. Đề xuất tiếp theo

1. Nghiệm thu chơi thử/native UI và Windows Player khi môi trường cho phép, với DEVB tắt.
2. Thu phản hồi thời gian nhận gear/đủ nguyên liệu ở tỷ lệ thật .08/.60 trước khi cân bằng tiếp.
3. Chốt mức MagicalAttack cấp 1 cho Staff và mục tiêu main stat các slot còn lại nếu muốn mở rộng bảng template.

Dừng sau bàn giao; không tự mở rộng balance/gameplay.
