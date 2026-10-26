# W3.2.1 — Combat Loot Reward Contract

Ngày: **26/10/2026**, Asia/Ho_Chi_Minh. Repo `lavievn/IdleGame`, nhánh `main`.
Baseline đã fetch/đối chiếu: **896dd72896985d53a6d806b205019f95c5f3f8c9**;
local = origin/main, working tree sạch trước khi viết.

**Trạng thái: ĐỀ XUẤT CHỜ DUYỆT. Chỉ tài liệu, chưa có code loot.** Khuyến nghị
phương án **A, gear-only, profile test tách biệt** để nghiệm thu Dev Playable 0.1.
B chưa đủ dữ liệu để coi là cấu hình progression sẵn triển khai. Không bắt đầu
W3.2.2 chỉ vì tài liệu này đã được commit.

## 1. Phạm vi và nguồn đối chiếu

Đã đọc [AGENTS](../AGENTS.md), [project rules](../.agents/skills/idle-game-project-rules/SKILL.md),
[Unity/C#](../.agents/skills/unity-csharp-scripting/SKILL.md),
[save-systems](../.agents/skills/save-systems/SKILL.md),
[kế hoạch Dev Playable](DEV_PLAYABLE_0.1_PLAN.md),
[định hướng core loop](CORE_GAMEPLAY_LOOP_DIRECTION_2026-10-25.md),
[schema trang bị](Equipment_0.0.52_Schema.md),
[affix discovery](Equipment_Affix_Discovery_0.0.52.md) và mã hiện tại.

W3.1a là snapshot lịch sử: compare đã được nối ở baseline này, xem
[W3.1b.1](W3_1b_1_InventoryComparison.md). Nội dung sau là thiết kế reward, không
đổi EXP, damage, rèn, checkpoint, camera, movement, wave hay party B2.

## 2. Những gì thực sự đã có và có thể tái sử dụng

Các file dưới thuộc `Assets/Script/`, trừ EntityDataSO thuộc `Assets/DataScript/`.

| API/dữ liệu | Hành vi hiện tại / cách dùng lại |
|---|---|
| `CombatManager.DealDamageFromHero` | Kiểm active target, controller sống, attacker đã đăng ký; HP về 0 thì gọi death handler |
| `CombatManager.HandleMonsterDeathBy` | Remove ActiveMonsterInfo khỏi combat list trước EXP/heal; MarkDead, despawn, destroy Data, rồi báo GameManager. Chưa cấp item |
| `ActiveMonsterInfo.lifeId`, `monsterLifeSequence` | Mỗi lần StartBattle tạo một đời combat mới; không dùng GameObject pool làm kill identity |
| `MonsterController.SpawnVersion` | Tăng trong OnEnable; MarkDead và OnDisable không phải bằng chứng kill do damage |
| `battleVersion`, projectile hero/monster life + spawn | Projectile bị loại khi battle/life/spawn không còn phù hợp. MagicFlight tách ra chỉ có hình, không target/data để gây damage |
| `GameManager.OnMonsterDied` | Remove khỏi danh sách wave, CompleteWave và bật khám phá khi wave hết; không biết đầy đủ lý do chết/life identity để làm loot trigger độc lập |
| `GameManager.HeroData` | Beneficiary nhân vật chính của run; dùng trực tiếp từ GameManager sở hữu combat, không FindFirst Hero hoặc dùng attacker.Data |
| `EquipmentRuntime.AddInstance/Find` | Validate item, yêu cầu list có sẵn, từ chối equipped và ID trùng, lưu bản copy, isDirty=true; không có cap tổng số gear |
| `EquipmentInventory.Add/Count` | Stack theo itemId, lượng tối đa **9999/itemId**, Add trả false khi vượt; không tự giới hạn ID vào catalog được phép |
| `EquipmentFoundation.FindTemplate/CreateCommon/IsValid` | 7 template, tạo GUID, Common, refine 0, main stat truyền vào; không tính baseStat từ level/map; Weapon mặc định có tag stance:melee |
| `CreateDemoSword` | `sample:iron_sword`, level 1, Common, ATK main +10, không affix thêm; phù hợp mẫu test A |
| `EquipmentRefinement.CreateForgeDemoSword` | Kiếm level 1/Common, Kim, tiềm năng Tiên, 4 affix random ẩn; dùng System.Random từ GUID, không phải generator rarity/drop tổng quát |
| `EquipmentStatPreview`, InventoryPanelUI | Đồ nhận vào bag có thể chọn/compare/equip theo W3.1b.1; nhận đồ không cần tự Equip hoặc RebuildModifiers |
| `EntityDataSO.inventory/equipmentItems` | Item/stack/affix/forge và assignment nằm trong snapshot Hero chính; không có kill receipt, encounter ID, pending loot hay RNG state reward |
| `SaveManager.SaveGame/LoadGame` | JSON snapshot, validate version; normalize không reroll item; SaveGame thành công mới clear dirty; `.tmp` → Replace/Move, không ledger reward |
| `WorldMapCatalog.CompleteWave/RestartAfterDefeat` | Loop 1..5 tại cùng node, clear history riêng; death reset wave theo luật hiện hành. Không dùng node + wave 1..5 làm identity duy nhất |

### Catalog và randomness đã đối chiếu

- Template hiện có: `sample:iron_sword` (Weapon/Attack), `base:head` (Head/Armor),
  `base:chest` (Chest/Armor), `base:boots` (Boots/MovementSpeed), `base:artifact`
  (Artifact/MaxHealth), `base:necklace` (Necklace/MagicResistance), `base:ring`
  (RingLeft hoặc RingRight/Attack).
- Stack catalog: `sample:health_charm`, `sample:attack_charm`; nguyên liệu rèn
  dùng `EquipmentRefinement.MaterialId`. API nhận arbitrary ID không phải quyền
  cho loot tự bịa ID, recipe, potion, currency hay template chưa có.
- Rarity: **Common → Hoàng → Huyền → Địa → Thiên → Tiên**. Code có
  `ThienDropProbability=0.00005` (**0,005%**) và `TienDropProbability=0.00001`
  (**0,001%**). Đây là mốc lưu sẵn, chưa có selector sử dụng và chưa rõ mẫu số
  khi tích hợp: trên kill hay trên lần đã rơi gear. Không đổi đơn vị thành 0,005
  hay 0,001 trong xác suất dạng 0..1.
- Chưa có tỷ lệ Hoàng/Huyền/Địa, drop chance toàn cục, trọng số template, bảng
  baseStat/level/rarity, số affix theo phẩm chất hoặc distribution tiềm năng.
  `MaximumExtraLines(level)` và `MaximumAffixLines=6` là giới hạn cấu trúc/tạm,
  không phải xác suất rơi hay đặc tả đầy đủ cho generator mới.
- `CreateForgeDemoSword` chỉ random loại/giá trị affix trong pool có sẵn, vẫn sinh
  **Common** với potential Tiên. Không coi potential Tiên là vừa rơi đồ Tiên;
  không nhân hệ số rarity hoặc tự reveal affix để tạo sức mạnh mong muốn.

## 3. Contract chung đề xuất

### 3.1 Trigger và kill hợp lệ

Reward chỉ được tạo tại **điểm lethal damage đã được chấp nhận** trong
`HandleMonsterDeathBy`, sau validate/consume quyền xử lý đời quái nhưng trước
pool release/destroy Data và trước `GameManager.OnMonsterDied` đổi trạng thái wave.
Capture dữ liệu immutable trước cleanup. Không hook loot ở MarkDead, OnDisable,
DespawnMonster, OnMonsterDied hoặc CompleteWave.

Các điều kiện phải đồng thời đúng:

1. Đây là lời gọi từ damage pipeline với HP kết quả bằng 0; target thuộc combat
   list và thuộc encounter đang mở. Không lấy riêng `IsAlive=false` làm kill.
2. ActiveMonsterInfo, lifeId và SpawnVersion khớp snapshot lúc đăng ký spawn;
   attacker/lifetime tuân thủ guard combat đang có. Không nới guard projectile.
3. GameManager/run epoch, map context, encounter token còn hiện hành; wave chưa
   bị hủy/travel/load/defeat. HeroData chính vẫn là beneficiary đã bind với run.
4. KillKey chưa có outcome xử lý. Duplicate gọi trực tiếp death handler cũng
   phải bị chặn trước reward/EXP, không chỉ trông vào caller kiểm HP.

Khi B2 kết liễu: **loot vẫn vào HeroData chính**, kể cả Hero chính đã chết nhưng
B2 còn chiến đấu hợp lệ. EXP/heal giữ nguyên cấp cho attacker.Data như hiện tại.
Không dùng điều kiện “Hero chính phải IsDeployed” hoặc yêu cầu killer=primary để
vô tình bỏ loot. Không sao chép loot vào B2 hoặc tự chia EXP lại.

### 3.2 Identity và chống callback trùng trong runtime

Đề xuất context **chỉ trong RAM**, không thêm field serialized vào EntityDataSO:

| Thành phần | Ý nghĩa |
|---|---|
| RunEpoch GUID | Thế hệ runtime do owner giữ; thay khi New/Load/Continue thành công thay snapshot, không lấy tên Hero làm khóa |
| EncounterToken GUID | Một lần spawn nhóm quái/attempt; đổi mỗi StartBattle; invalidate ngay khi clear/cancel/disable/đổi run |
| Monster lifeId + captured SpawnVersion | Đời combat và đời activation của đúng controller; capture lúc đăng ký, không đọc pool object muộn để dựng lại khóa |
| Map snapshot | worldMapId hoặc legacy mapNumber + chế độ map; dùng validate/log, không tự migrate legacy sang world-map |
| KillKey | `(RunEpoch, EncounterToken, lifeId, captured SpawnVersion)`; không dùng wave ordinal, thời gian frame hoặc GameObject.GetInstanceID đơn lẻ |

State cho mỗi đời: **Eligible → Resolving → Awarded / NoDrop / Failed**;
**Cancelled** là terminal khi encounter bị hủy trước lethal commit.
Claim trước khi roll/gọi API để chống re-entry; NoDrop cũng được ghi nhận để
callback trùng không được roll lại. Đời bị cancel không được đổi lại Eligible.
Không clear receipt chỉ vì mở Inventory, đổi thế hoặc ApplyDevBalance; balance
refresh không phải một kill hay encounter mới. Tách khỏi vòng đời projectile.

Receipt của encounter đã đóng có thể dọn: mọi callback mang token cũ bị từ chối
trước lookup, không cần giữ HashSet mọi kill suốt phiên farm. New pooled life
được token/life mới; callback cũ không được suy identity từ object vừa tái dùng.
Nếu disable/enable component, chỉ re-arm qua context mới được xác thực, không
ngầm coi active list cũ là quyền phát thưởng còn hiệu lực.

**Không sửa hành vi Magic AoE hiện tại:** projectile còn hợp lệ trong cùng battle
có thể hạ nhiều target hợp lệ; mỗi target có KillKey riêng. Magic tail đã tách chỉ
là hình, không loot. Projectile battle/map/life cũ bị loại; không chuyển tail thành
nguồn kill hoặc tự cấm AoE hợp lệ chỉ để chống duplication.

### 3.3 Roll một lần, cấp vào bag một lần

- Allowlist template/profile trước khi bật combat reward. Một outcome có tối đa
  **một gear**, hoặc NoDrop. Không dùng GrantSamples/GrantForgeTestSword để cấp
  loot vì các hàm đó là lối dev với flag/thông báo riêng.
- A dùng factory có sẵn; B cần factory/table được duyệt. Kiểm IsValid và supported
  item/save versions, isEquipped=false, ID mới trước khi AddInstance.
- `EquipmentRuntime.AddInstance(primary, item)` là mutation duy nhất cho gear.
  Nếu profile sau này cho stack: `EquipmentInventory.Add(primary, allowedId, qty)`
  với qty đã validate. Không chèn list trực tiếp và không thay stats/modifiers.
- Chỉ ghi “đã nhận” sau API trả true; ghi NoDrop mà không popup. Hiển thị tên đồ
  qua cơ chế log hiện có, không yêu cầu bấm nhận hoặc tự equip/use/sell/convert.
- Xử lý đồng bộ trên main thread, không yield giữa claim và kết quả Add; không
  gọi Save ở giữa. Nhận đồ không reset attack, không hồi HP hoặc recalculate
  combat. Equip sau đó tiếp tục dùng đường đã kiểm tra trong W3.1b.1.
- RNG loot độc lập với UnityEngine.Random dùng damage/heal/spawn. A không roll
  combat RNG; B có RNG inject được để test biên. Lưu item đã sinh một lần, không
  gọi generator lại khi refresh UI, save/load hay gặp callback trùng.
- Outcome gồm KillKey, profile/version, beneficiary epoch, accepted/no-drop/
  failure reason và item ID phục vụ test/debug. Đây chưa phải schema save mới.

### 3.4 “Đầy kho” và lỗi insertion

**Hiện tại gear không có cap tổng số món.** 10/20 ô trên UI là page size, không
phải capacity. Phương án A và B mặc định đề xuất **gear-only, giữ mô hình không
cap hiện có**; không thêm giới hạn kho ngầm hoặc cơ chế auto bán/xóa item.

| Tình huống | Hành vi contract đề xuất |
|---|---|
| Gear list nhiều trang | Vẫn AddInstance bình thường; không báo đầy vì UI hết ô |
| Stack đã 9999 | Không hỗ trợ stack reward trong cấu hình tối thiểu A/B; profile chứa stack bị từ chối trước combat reward, không roll món rồi bỏ đi |
| Yêu cầu sau này cho rơi stack | Phải chốt overflow contract riêng trước bật: không clamp lượng, không tạo stack ID thứ hai để vượt cap, không fallback random sang món khác |
| AddInstance false vì ID trùng | Chỉ coi AlreadyAwarded khi receipt đã commit đúng cùng item/beneficiary; ID trùng không có receipt là lỗi, không phát một GUID mới để thử lại |
| Item invalid/list null/unsupported version | Lỗi cấu hình/runtime, không báo nhận thành công, không tự Normalize/initialize run để chữa; ghi lỗi rõ kèm context và chặn reward profile tiếp tục phát |
| Lỗi bất ngờ sau lethal kill | Outcome Failed, không roll lại hoặc lấy callback trùng làm retry. Báo lỗi trong UI/log và đánh dấu nghiệm thu FAIL; không coi việc bỏ item là hành vi release được chấp thuận |

Giới hạn cần chốt: không thể hứa “giữ mọi reward đang pending qua quit/load” bằng
queue RAM. Nếu muốn cap gear hoặc stack overflow có hoàn trả bền vững, cần thiết
kế nơi chứa pending/receipt và persistence riêng; **không nằm trong W3.2.2 tối
thiểu và không được lén thêm schema**. Trước release, insertion failure với profile
gear hợp lệ là lỗi chặn nghiệm thu, không một nhánh mất đồ im lặng được cho phép.

## 4. Save/Load/Retry: định nghĩa chính xác giới hạn bảo đảm

### Bảo đảm đề xuất trong schema hiện có

- **Cùng KillKey, cùng run/encounter:** tối đa một lần Add. Kill không rơi đồ cũng
  không được reroll; cancel/escape/cleanup không là kill.
- **Save sau reward:** snapshot chứa gear/ID/affix hoặc stack đã thêm; Load chỉ
  restore snapshot, không dispatch lại kill/reward và không regenerate gear.
- **Load/Continue thành công:** invalidate context cũ trước publish HeroData mới
  hoặc nhận callback mới. Load bị từ chối không được đổi inventory/epoch đang
  sống. Không merge đồ đang có vào snapshot manual cũ.
- **Retry/escaped-wave replay:** kết thúc context cũ; quái spawn lại là đời mới.
  Callback từ đời cũ không được thưởng, nhưng **một kill mới thực sự trong attempt
  mới có thể được thưởng**. Map/wave 1..5 lặp không phải một kill cũ.
- **Farm:** clear history/node không khóa loot mọi lần sau; không dùng
  worldMapCleared làm ledger kill. Không cấp reward cho CompleteWave thay cho kill.
- **isDirty:** Add thành công đánh dấu primary dirty ngay cả khi B2 nhận EXP.
  NoDrop/duplicate/rejected không tự dirty primary do loot. Giữ AutoSave 10 giây
  scaled time và các điểm save hiện có, không save mỗi kill.

### Điều chưa thể cam kết nếu cấm thay persistence

Save hiện không ghi encounter/kill/RNG receipts. Item GUID chỉ chống thêm lại
**cùng item ID**, không chống một lần reroll ra GUID mới cho cùng kết quả cũ.
HashSet RAM, battleVersion hoặc seed RNG đều không thay được ledger bền vững.

| Trường hợp | Kết quả theo snapshot hiện hành |
|---|---|
| Save trước kill → kill nhận item → Load save cũ | Reward sau snapshot bị rollback cùng progression; không giữ lại ở bag. Giết quái spawn mới sau Load là kết quả mới, không restore-event phát thưởng |
| Save sau kill → Load nhiều lần | Mỗi Load trả đúng snapshot/ID; không cộng thêm bản sao vào run đang sống |
| Crash trước lần save thành công kế tiếp | Có thể mất reward chưa save theo chính sách hiện tại; không hứa durable exactly-once hoặc phục hồi pending |
| Save thất bại | Dirty và đồ trong RAM vẫn còn; không báo đã persist, không phát item thứ hai khi thử save lại; không bảo đảm sống qua crash |
| Muốn cấm nhận lại “cùng enemy logic” qua mọi Retry/Load cũ | Chưa có stable encounter identity xuyên save; cần đặc tả/receipt persistence và thay phạm vi được duyệt trước |

Vì vậy cần duyệt **D3** bên dưới trước code. Nếu yêu cầu là đúng một lần xuyên
crash/rollback hoặc cấm farm lại một enemy logic dù đời mới, phương án không đổi
schema **chưa đáp ứng yêu cầu đó**. Dừng thiết kế persistence để thảo luận; không
đổi checkpoint W2.3 hay giả vờ receipt RAM đã bảo đảm việc này.

Một rủi ro riêng: wave đang dở bị escape có thể replay toàn wave trong khi EXP
và loot của các kill hợp lệ trước escape đã giữ. Theo lựa chọn per-life, giết
lại quái mới có thể tạo lợi ích farm attempt. Không xóa reward kill đã hợp lệ để
che vấn đề; người dùng phải chốt có chấp nhận cho 0.1 hay cần logical-spawn ledger
riêng. EXP cũng không được thay trong task loot để làm kết quả trông cân bằng.

## 5. Hai cấu hình thử nghiệm để lựa chọn

| Tiêu chí | A — ControlledLootTest, khuyến nghị | B — RarityLootExperiment |
|---|---|---|
| Mục đích | Nghiệm thu combat → item → compare → equip nhanh | Đánh giá distribution phẩm chất và lựa chọn gear |
| Trigger/số lượng | Đề xuất **100% kill hợp lệ → 1 CreateDemoSword()**, chỉ trong profile test | Tách P(drop/kill), template weights, current rarity và potential distribution; tối đa 1 gear/kill |
| Item | Kiếm mẫu level 1/Common/ATK +10 có sẵn, GUID mới; không cho thêm affix/rèn | Chỉ 7 template đã có, các enum/stat/element hợp lệ; bảng level/baseStat/affix/gate phải được duyệt |
| Random | Nội dung xác định, GUID vẫn riêng; không random rarity | RNG inject được, roll một lần; không gọi Unity combat RNG; chưa có generator tổng quát để bật ngay |
| EXP/B2 | Không đổi EXP; loot luôn primary | Tương tự A |
| Full inventory | Gear không cap hiện tại; không stack drop | Giữ gear-only không cap cho thử nghiệm đầu; stack/cap cần contract khác |
| Tách khỏi progression | Mode riêng, mặc định Off; chọn duy nhất một mode, log `[LOOT TEST A]`, không tự bật từ save | Mode riêng, mặc định Off, profile/version rõ; chưa coi là progression chính thức |
| Ưu | Ít biến số, test chính xác một kill/một item; dùng factory thật; dễ tìm lỗi duplicate | Gần lựa chọn build dài hạn, dùng schema hiện có, kiểm được rare/affix và giá trị kinh tế |
| Nhược | Nhiều kiếm giống nhau, tăng bag nhanh; không chứng minh cân bằng/độ thú vị hay nâng cấp liên tiếp | Chưa có bảng đủ; dễ sai mẫu số xác suất, power và affix; rare không thể nghiệm thu bằng chờ thủ công |
| Mã W3.2.2 dự kiến | Khoảng 2 file runtime sửa + 1 service/profile nhỏ mới; 2 nhóm test, `.meta`; estimate cấu trúc, không cam kết LOC | Cùng hạ tầng A + profile/table và generator riêng, validation/seed tests; nhiều điểm cần spec hơn, không chỉ đổi một % |
| Rủi ro | Bật nhầm test profile, spam gear, nhầm per-life với durable identity | Thêm rủi ro bias, reroll/exploit rollback, rare budget và inflation; chưa có bảng power đã chốt |

**Không dùng hai hằng Thiên/Tiên làm bảng B hoàn chỉnh.** Trước B phải chọn rõ
chúng áp trên kill hay trên gear drop. Nếu áp trên gear drop thì xác suất trên
kill còn nhân P(drop/kill); không tự suy từ thứ bậc rarity ra các tỷ lệ còn lại.
Các mốc 0,005% và 0,001% chỉ được đề nghị giữ làm anchor sau khi người dùng xác
nhận mẫu số và phạm vi; phần Common/Hoàng/Huyền/Địa chưa đề xuất con số tùy ý.

Factory B phải bảo toàn số affix cố định/hidden/known-locked/active và forge
metadata hợp lệ. Có thể tái sử dụng CreateCommon, validator, schema và kiến thức
pool affix từ kiếm test; không gọi CreateForgeDemoSword rồi đổi mỗi rarity để
coi là generator đủ mọi template. Không sửa tỷ lệ rèn hoặc Normalize để sinh lại
item. Nếu muốn tận dụng nguyên factory kiếm test, kết quả vẫn là Common với
potential Tiên; đó chỉ là biến thể test A, chưa phải B “rơi theo rarity”.

A tạo đồ dùng để nghiệm thu, không thêm permanent reward flags vào save. Đồ đã
cấp vẫn là gear thật trong snapshot nếu người dùng save; profile test tắt đi
không tự xóa đồ đã nhận. Dùng profile chơi thử riêng khi đánh giá progression.

## 6. Quyết định cần người dùng chốt trước W3.2.2

| ID | Cần duyệt | Khuyến nghị tối thiểu |
|---|---|---|
| D1 | A hay B; có chấp thuận profile test tách biệt? | **A**, mặc định Off, bật tường minh trong môi trường dev |
| D2 | Tần suất, item, lượng và đối tượng map | A: một kiếm mẫu/kill hợp lệ, mọi map đang hỗ trợ gồm legacy/world-map; không thêm Boss loot; không scaling theo level/map |
| D3 | “Cùng kết quả chiến đấu” có nghĩa gì khi Retry/Load/escape? | Per-life KillKey, restore không phát lại, đời mới có thể rơi; chấp nhận rollback/crash window hiện tại. Nếu yêu cầu nghiêm hơn thì phải đổi phạm vi trước |
| D4 | Kho đầy và stack | Gear-only, không thêm cap, không stack drops; reject profile stack chưa có overflow contract. Không queue RAM giả làm persistence |
| D5 | B nếu chọn | Chốt P(drop), mẫu số Thiên/Tiên, weights phần còn lại, template, item level, stat budget, potential/affix/forge và RNG test contract trước code |
| D6 | Phản hồi/lỗi | Log nhận đồ không modal; lỗi insertion hiển thị rõ và là release blocker; không tự bán/đổi món/reroll hoặc mở Inventory để nhận |

D1–D4/D6 tạo một gói A đề xuất cụ thể. Người dùng có thể duyệt hoặc chỉnh từng
điểm; commit tài liệu **không phải** approval. D5 chỉ cần khi chọn B. Không xin
quyền triển khai qua tài liệu này hoặc tự bắt đầu W3.2.2 trong cùng nhiệm vụ.

## 7. File và thứ tự triển khai dự kiến ở W3.2.2

1. `Assets/Script/CombatLootRewards.cs` (tên đề xuất, mới + `.meta`): context/
   KillKey/outcome, profile Off/A/B, eligibility và generator adapter. Không phụ
   thuộc UI để chấp nhận kill; RNG inject được cho B/test. Không MonoBehaviour
   polling tìm quái chết hay serialized receipt.
2. `Assets/Script/CombatManager.cs`: capture spawn/context ở StartBattle; validate
   lethal/claim tại death handler, gọi owner cấp reward trước despawn; invalidate
   tại ClearBattle/OnDisable. Giữ nguyên các công thức, thứ tự EXP/heal và guard
   projectile; bổ sung reward không làm một death được xử lý hai lần.
3. `Assets/Script/GameManager.cs`: cấp vào HeroData chính qua API, quản lý epoch
   khi snapshot thay/entry mới/cancel, log kết quả; không dùng OnMonsterDied như
   trigger thứ hai, không sửa timer/distance hay chính sách Retry/checkpoint.
4. Test runtime/headless `Tests~/CombatLootRegression.cs`, runner có nhóm riêng;
   test Unity trong `Assets/Tests/Presentation/PlayMode/` + `.meta`, no-save-I/O.
   Tên và số file có thể thu gọn sau review; mỗi nhóm commit có kiểm chứng riêng.
5. B có thêm table/profile/generator được duyệt cùng validator/test, có thể cần
   file dữ liệu mới; không tự thay scene/prefab/asset đang có chỉ để bật mode.

EquipmentRuntime/EquipmentInventory, EntityDataSO, SaveManager, WorldMapCatalog,
InventoryPanelUI và EquipmentStatPreview dự kiến **dùng nguyên API**, không sửa.
Chỉ mở scope nếu test chỉ ra API không biểu diễn được contract đã duyệt và nêu
bằng chứng trước thay đổi; không thêm root save version hoặc checkpoint schema.

## 8. Test cases bắt buộc cho implementation tương lai

| ID | Ca | PASS cụ thể |
|---|---|---|
| T01 | Kill bằng primary/B2, primary đã chết nhưng B2 còn hợp lệ | Loot vào đúng HeroData chính, một item; EXP/heal vẫn đúng attacker như baseline |
| T02 | Callback chết cùng info gọi 2+ lần/re-entry | Tối đa một roll/Add, EXP/wave progress không lặp; NoDrop không reroll |
| T03 | Gọi death trực tiếp khi HP >0, target không active/data thiếu | Không loot, không commit fake kill; có lý do từ chối |
| T04 | Cùng pool object respawn | Life mới có thể nhận; callback giữ token/spawn cũ không nhận, không phá outcome mới |
| T05 | Projectile cũ sau clear/load/travel/new map | Không damage/reward vào đời mới; Magic tail vẫn visual-only |
| T06 | Magic AoE hợp lệ hạ nhiều target | Mỗi target một KillKey, không chỉ một loot cho cả projectile; duplicate impact không thêm |
| T07 | Escape, MarkDead/Despawn/OnDisable/ClearBattle không do lethal | Không reward, không dùng OnMonsterDied giả làm kill; wave clear/cancel không bonus item |
| T08 | Kill cuối wave, nhiều lần farm cùng node/wave ordinal | Reward resolve trước context chuyển, không bỏ kill cuối; attempt mới không bị HashSet cũ khóa nhầm |
| T09 | Kill một quái rồi wave khác escape/defeat/Retry | Giữ item đã cấp theo snapshot; cancel đời còn lại; callback cũ bị chặn; kill mới theo D3, không nhầm vào outcome cũ |
| T10 | Save sau kill → Load lặp | Item ID/stat/affix/forge phục hồi đúng một bản, không gọi generator trên Load, không merge bag cũ |
| T11 | Save trước kill → nhận đồ → rollback/Continue/relaunch | Bag về snapshot; không restore-event reward; encounter mới theo D3; không công bố durable exactly-once |
| T12 | Load fail/future schema | Live Hero/epoch/inventory không đổi; không tự repair hoặc award vào snapshot bị từ chối |
| T13 | Gear nhiều hơn page size; stack cap 9999 | Gear vào trang sau; không false-full. Profile stack mặc định bị từ chối; nếu mở scope stack thì test 9998+1, 9999+1 và overflow contract được duyệt |
| T14 | Add fail, duplicate ID không receipt, null list, invalid template/schema | Không đổi sang item mới/reroll, không thông báo nhận thành công; lỗi rõ, không tiếp tục phát bằng profile lỗi |
| T15 | isDirty/save failure | Primary dirty sau Add kể cả B2 kill; NoDrop/duplicate không dirty do loot; save failure không clear dirty hay phát lại item |
| T16 | A profile Off/On, mode loại trừ nhau | Off không loot; A đúng một Common kiếm mẫu không equipped, đúng tag; B không chạy đồng thời |
| T17 | B nếu duyệt | Fake RNG ở biên 0/1, từng khoảng, normalized weights và đơn vị xác suất; đúng allowlist/level/stat/affix gates; một roll outcome giữ nguyên |
| T18 | RNG và combat regression | Bật loot không tiêu thụ Unity combat RNG, không đổi damage/EXP/heal/spawn do loot; nhận đồ chưa đổi stat, sau equip mới có hiệu lực |
| T19 | UI reward → Inventory → Compare → Equip | Item mới hiện đúng bag/page, không popup bắt nhận; HP/modifier không đổi chỉ vì nhận/chọn; equip dùng đường W3.1b.1 |
| T20 | Context lifetime/disable/re-enable/same-node travel | Token cũ bị loại trước lookup, context mới cần register hợp lệ, receipt memory dọn có giới hạn |

T01–T09/T13–T20 ưu tiên pure/service + Mono/UnityStub, sau đó Unity Play Mode cho
pool, Awake/OnEnable, event ordering, UI và lifetime thật. T10–T12/T15 persistence
I/O chỉ chạy fixture cô lập theo README SaveLoad: đường dẫn trước Awake, marker,
cleanup sở hữu, không save thật, **không chạy SaveLoad I/O trong Cloud**. Mono PASS
không thay Unity/Windows PASS; test statistical hiếm không thay deterministic
boundary tests hoặc kiểm chứng bảng dữ liệu được duyệt.

## 9. Rủi ro và kiểm tra bàn giao nhiệm vụ này

- Duplication: key theo GameObject/map/wave đơn lẻ; roll lại khi NoDrop/Add false;
  grant tại cả death và wave callback; giữ bag mới khi Load snapshot cũ; nhầm
  new attempt với kết quả cũ. Contract trên phải được test trước integration.
- Persistence: có dirty không đồng nghĩa đã ghi đĩa; synchronous Add + receipt RAM
  không transaction với disk; không durable ledger, không queue pending qua save.
- Cân bằng: A 100% có thể làm bag tăng rất nhanh và kiếm lặp; B thiếu bảng, hai
  anchor rare chưa đủ, potential/current rarity khác nhau. Không lấy test PASS
  để suy ra progression hấp dẫn hoặc đổi tỷ lệ rèn/EXP nhằm bù loot.
- Mã đã được truy vết tĩnh; chưa có implementation mới nên không chạy gameplay,
  Mono suite, Unity hoặc Windows, không đọc/ghi/xóa save người chơi trong task.
- Bàn giao chỉ tài liệu này; kiểm link/file tham chiếu, skills verifier và
  `git diff --check`. Re-fetch trước commit/push, không force-push nếu remote
  thay đổi ngoài dự kiến. SHA commit và xác nhận remote báo riêng trong chat.
