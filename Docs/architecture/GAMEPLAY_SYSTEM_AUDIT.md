# Gameplay System Audit — Bách Luyện Tiên Đồ

## Mốc kiểm kê và phạm vi

2026-11-05; branch `feature/monster-awareness-ambush`; HEAD nguồn `0944519c46610e79238c027c83ae9ea023c984c3`; working tree đầu tác vụ sạch. Giữ worktree đang hoạt động. Đây là kiểm kê tĩnh theo đường gọi MainGame, chỉ bổ sung tài liệu; không sửa mã/scene/config/save hoặc chạy Editor, test suite, Player.

Đã đối chiếu các báo cáo thực sự tồn tại: [Foundation](../automation-reports/FINAL_REPORT.md), [Production Loot](../automation-reports/PRODUCTION_LOOT_V01_REPORT.md), [Gear Drop](../automation-reports/GEAR_DROP_V01_REPORT.md). Kết luận NoDrop/chưa có nguồn material trong Foundation là lịch sử, đã được thay bằng hai stream và Gear Drop trong hai báo cáo mới. PASS lịch sử được ghi là bằng chứng có phạm vi, không coi là nghiệm thu mới của audit.

Định hướng chính thức: một nhân vật chính, chiến đấu tự động, không class cố định; tự do chọn vũ khí/công pháp/võ học; tương hợp/tương khắc; môn phái thuộc thế giới/cốt truyện, không là class; tiến triển dài hạn; cốt truyện có thể phân nhánh và kết thúc truyện không kết thúc gameplay; không multiplayer. Đây không phải yêu cầu tự triển khai các hệ chưa nối.

Phương pháp: tìm symbol trong `Assets/Script`, `Assets/DataScript`, kiểm tra các thư mục C# khác dưới Assets để tránh bỏ sót module; đọc đúng các hàm entry/caller, kiểm tra serialized callback khi cần và tên/test source liên quan. Không đọc save cá nhân, không quét artwork hoặc lịch sử Git. Các kết luận MISSING giới hạn ở runtime local/entry point đã tìm, không phủ định thiết kế, nội dung tài liệu hoặc nhánh khác.

## Quy ước trạng thái

- **IMPLEMENTED & USED**: có triển khai và đường MainGame gọi; không đồng nghĩa nghiệm thu mọi tình huống.
- **IMPLEMENTED BUT DISCONNECTED**: có hàm xử lý nhưng chưa thấy caller gameplay.
- **PARTIAL**: một phần chuỗi đã hoạt động, phần còn lại có giới hạn rõ.
- **DATA ONLY**: enum/DTO/asset contract hoặc lưu trữ, chưa có nghiệp vụ tương ứng.
- **MISSING**: tìm có mục tiêu nhưng không thấy triển khai/đường gọi cần thiết.
- **UNKNOWN**: chưa đủ bằng chứng để kết luận.

## A1. Nhân vật và progression

| Hệ thống | Trạng thái | Bằng chứng file / class / method | Giới hạn và rủi ro |
|---|---|---|---|
| Nhân vật chính và tạo mới | IMPLEMENTED & USED | [GameManager.InitHeroData](../../Assets/Script/GameManager.cs#L445): clone `heroDataSO`, reset progression/inventory/equipment, chọn world-map birth, giới tính/tên/race/roots, `ApplyHeroBalance`; `DeployHero` → `CombatManager.SetupHeroInfo`. [EntityDataSO](../../Assets/DataScript/EntityDataSO.cs#L6) là dữ liệu canonical. | Một Hero được lưu; không tạo bản sao dữ liệu riêng cho UI. |
| Tên nam/nữ | IMPLEMENTED & USED | [NameDatabase.GetRandomName](../../Assets/Script/TuTienEnums.cs#L15), được `InitHeroData` và `StartBattle` gọi. Hai bộ nguồn riêng, ghép 1–3 từ, không chọn hai từ từ cùng một tên nguồn. | Tên ngẫu nhiên không phải hệ thân thế/cốt truyện. |
| EXP, level, growth | IMPLEMENTED & USED | [EntityDataSO.AddExp](../../Assets/DataScript/EntityDataSO.cs#L282) được `CombatManager.HandleMonsterDeathBy` gọi; `CombatBalance.RequiredExp/HeroAttack/HeroHealth`; `RebuildHeroStats`. | Level 1–999; EXP yêu cầu `ceil(100 × 1,15^(level−1))`, có cap lưu trữ. Mỗi cấp +1 stat point và roll HP pool 50–100 một lần. Nền Attack `10 + level−1` khi không DEVB. |
| Phân điểm thuộc tính | IMPLEMENTED BUT DISCONNECTED | [AllocateHealth/AllocateDamage](../../Assets/DataScript/EntityDataSO.cs#L303): tiêu 1 điểm, +6 HP hoặc +1 addedDamage. Có caller trong `Tests~/BalanceRegression.cs`, `SaveRollbackRegression.cs`; không có caller C# production hoặc callback scene/prefab/asset tìm được. | Điểm được sinh/lưu, chưa có đường người chơi chi trong MainGame. AddedDamage hiện là Physical, không phải MagicalAttack. |
| Nền cảnh giới | IMPLEMENTED BUT DISCONNECTED | [RankDatabase.GetRankName](../../Assets/Script/TuTienEnums.cs#L35): 10 tên Luyện Khí → Chân Tiên, mỗi 10 level một bậc, trên 100 là Tiên Đế. Không tìm thấy caller runtime/UI hoặc test trực tiếp của hàm. | **Có nền tảng cảnh giới**, không kết luận hệ chưa tồn tại. Hiện là hàm suy tên theo level, chưa tham gia chỉ số hoặc hiển thị MainGame đã kiểm tra. |
| Đột phá cảnh giới độc lập | MISSING | `EntityDataSO` không có state/điều kiện đột phá; `AddExp` tăng cấp liên tiếp; không có caller RankDatabase trong combat/growth. | Không có bằng chứng gate đột phá, tiêu tài nguyên, xác suất hoặc buff riêng theo realm. Không được lấy “trọng” của linh căn hoặc Special Forge làm đột phá nhân vật. |
| HP/stat/mode | IMPLEMENTED & USED | [GetCalculatedHealth(mode)](../../Assets/DataScript/EntityDataSO.cs#L307), `CombatBalance.HeroHealthMultiplier`, `CombatManager.RefreshWeaponAttack/RefreshRegisteredHeroStats`. | HP nền nhân theo Melee 2 / Bow 0,6 / Magic 1; Melee cộng thêm HP growth pool đã lưu. Đây là coupling theo mode, không phải class khóa nhân vật. |
| B2 | PARTIAL | `GameManager.CycleTestCompanions/CreateTestCompanion`, [HeroCombatState](../../Assets/Script/HeroCombatState.cs#L6), `CombatManager.RegisterAdditionalHero`. | Hai đồng đội thử nghiệm runtime, không được SaveManager lưu; không coi là party production. Định hướng một nhân vật chính không yêu cầu xóa fixture B2. |

## A2. Linh căn, tương khắc và chỉ số

| Hệ thống | Trạng thái | Bằng chứng | Giới hạn |
|---|---|---|---|
| Root profile | IMPLEMENTED & USED | [SynergyMath.GenerateRootProfile](../../Assets/Script/TuTienEnums.cs#L107), `EntityDataSO.NormalizeRoots`; gọi khi tạo Hero/quái. | Enum Kim/Mộc/Thủy/Hỏa/Thổ/Độc/Băng/Vô. Sinh mới chọn ngũ hành và có thể Vô đơn căn; Độc/Băng có định nghĩa, chưa được bộ sinh thường chọn. Tier 1–5, weights chuẩn hóa tổng 1; ConLai sinh hai căn. |
| Bảng khắc | IMPLEMENTED & USED | `SynergyMath.Counters/GetElementalMultiplier/EvaluateCore/EvaluateSnapshot`; `CombatManager.TickRegisteredHeroAttack` và damage quái. | Kim→Mộc→Thổ→Thủy→Hỏa→Kim; chiều ngược giảm damage; same/unrelated/Vô/Độc/Băng trả 1 trong bảng hiện tại. Không tìm thấy bảng “tương sinh” buff riêng; không tự suy thêm. |
| Weighted damage | IMPLEMENTED & USED | [EvaluateBasicDamage](../../Assets/Script/TuTienEnums.cs#L139) → `EvaluateCore` → `EvaluateSnapshot` → `CombatDamage.ResolveBasicSnapshot`. | Hệ số `Σ weightA × weightD × pairMultiplier`; áp cả basic Sword/Bow/Staff. Không chỉ áp chiêu phép; không cộng base attack lặp cho từng căn. |
| Di truyền Vô | IMPLEMENTED BUT DISCONNECTED | `SynergyMath.TryResolveVoidInheritance`; test `Patch54aRegression.cs`. | Helper cho trường hợp Vô, chưa có hệ sinh con/đường MainGame. Không phải progression đang chơi. |
| Stat/conditions chung | IMPLEMENTED & USED | [CombatStats.GetWithModifiers](../../Assets/Script/CombatStats.cs#L58), `characterTags`, `CombatStatModifier`, runtime callers damage/HP/movement/timer. | Tags lấy từ characterTags + context caller; không tự sinh điều kiện root/terrain/race. `TagSO` không phải đầu vào tự động của bộ này. |
| Công pháp/võ học ↔ linh căn | MISSING | Có root evaluator và modifier conditions nhưng không thấy combat-art/skill catalog, học/trang bị/activation caller. | Không suy `LearnedSupportSkill` thành võ học chiến đấu. Cần hợp đồng tích hợp trước khi dùng root làm điều kiện học hoặc vận chiêu. |

### Contract tính toán cần giữ

Với gap `g = tierAttacker − tierDefender`: chiều khắc dùng `1 + 0,30 × (g >= 0 ? 1 + 0,10g : max(0,25; 1 + 0,25g))`; bị khắc dùng `1 − 0,30 × (1 − 0,10g)`. Tier được clamp 1–5. Không có hệ số cảnh giới được chèn thêm vào chuỗi này.

**Phần trăm chỉ số áp trên nền, không nhân cả bonus flat:** ordinary stat = `base + Σflat + base × Σpercent`; ratio stat = `base + Σflat + Σpercent`. Ví dụ base 10, flat 4, percent 0,1 → 15, không phải 15,4. Nguồn canonical là `CombatStats.GetWithModifiers`; đây cũng làm rõ cách diễn đạt rút gọn trong báo cáo Gear Drop trước. Integer dùng rounding của CombatStats; damage còn có các bước rounding khác.

| Chỉ số đã khai báo | Consumer thực tế | Kết luận |
|---|---|---|
| MaxHealth, Attack, MagicalAttack | EntityDataSO/WeaponBasicAttack → HeroCombatState/damage | IMPLEMENTED & USED; Magical nền 0, không lấy baseDamage/addedDamage Physical thay thế. |
| Armor, MagicResistance, ElementResistance | `CombatDamage.Resolve`, incoming school/element tags | IMPLEMENTED & USED trong evaluator; phần lớn basic hiện đi nhánh element Vô. |
| PhysicalDamagePercent, MagicalDamagePercent, ElementDamagePercent | `CaptureBasicBoost`, `Resolve`, phần damage theo school/element | IMPLEMENTED & USED; bonus riêng Kim/Hỏa không tự chuyển basic Vô thành đòn có hệ đó. |
| AttackSpeed | `TickRegisteredHeroAttack`, `WeaponBasicAttack.Interval`, timer quái | IMPLEMENTED & USED; interval clamp speed tối đa 1,25 khi bình thường, khác cap DEVB. Tăng số trên UI không luôn tăng DPS khi chạm cap. |
| MovementSpeed | `HeroController.EffectiveMoveSpeed`, movement runtime | IMPLEMENTED & USED. |
| HealthRegeneration, KillHealBonus | `TickPassiveRegeneration`, `HandleMonsterDeathBy` | IMPLEMENTED & USED; phần heal nền theo kill chỉ Melee, bonus được đọc riêng. |
| CastSpeed, CooldownReductionPercent, ManaRegeneration | enum, generic evaluator/preview và nhãn Inventory | DATA ONLY về tác dụng gameplay: không tìm thấy consumer timer/cast/mana runtime. |
| Mana pool/max/current, crit/chí mạng | Không có trong EntityDataSO/HeroCombatState/CombatStatId hoặc runtime damage đã truy | MISSING; HUD hiện “Mana —” vô hiệu, không phải mana resource hoạt động. |

## A3. Vũ khí, võ học, kỹ năng và class

| Hệ thống | Trạng thái | Bằng chứng | Giới hạn |
|---|---|---|---|
| Weapon family → basic attack | IMPLEMENTED & USED | [WeaponBasicAttack](../../Assets/Script/WeaponBasicAttack.cs#L7), `CombatManager.SetupHeroInfo`, `HeroController.BindBasicAttackData/SyncWeaponMode`, `GameManager.EquipGear`. Sword/Unarmed→Melee; Bow→RangedPhysical; Staff→RangedMagic. | Không kiểm class/profession hoặc level Hero để trang bị. Catalog validity + slot compatibility + xung đột Artifact/charm là điều kiện thật. |
| Weapon level / level advantage helper | IMPLEMENTED BUT DISCONNECTED | `SynergyMath.GetWeaponLevelPenalty/GetLevelAdvantageMultiplier` chỉ có định nghĩa trong runtime đã tìm. | Item level ảnh hưởng Forge/power, nhưng hai helper không được tự xem là điều kiện trang bị/damage đang áp. Không nối chúng tùy tiện vì sẽ đổi balance. |
| `WeaponType {Kiem, Dao, Quyen}` | DATA ONLY | `TuTienEnums.cs:11`, không có consumer tìm được. | Không phải authority của slot vũ khí hiện tại; authority là `EquipmentTemplate.weaponFamily`. |
| Magic vận chiêu legacy | PARTIAL | `HeroController.ChangeAttackMode` chỉ cho unbound/B2; `CombatManager.AdvanceCycle/TickRegisteredHeroAttack/Launch/TickProjectiles`; `HeroCombatState.AttackTimer/WindupMode`. | Legacy magic có charge/AoE; primary Staff là basic single-target, không cast clock và không ×2,5 của legacy magic. Không gọi cả hai là hệ Skill hoàn chỉnh. |
| Combat skill / công pháp catalog và kích hoạt/học/nâng cấp | MISSING | Không tìm thấy skill definition/catalog/loadout, activation scheduler, learned-combat-skill API hoặc điều kiện học trong runtime local. [HUD Võ học](../../Assets/Script/UIManager.HudW3.cs#L137) bị disabled. | Có automatic attack/timer/projectile, chưa có learned skills. Chưa có điều kiện weapon/root cho từng chiêu, mana cost hoặc cooldown từng skill. |
| Dữ liệu nghề phụ/skill hỗ trợ/recipe | DATA ONLY | [EquipmentFoundation](../../Assets/Script/EquipmentFoundation.cs#L118): `ProfessionProgress`, `LearnedProfessionRecipe`, `LearnedSupportSkill`, `CraftingRecipeData`, `RefinementRuleData`; lưu trong EntityDataSO và round-trip Save tests. | Không có consumer học/nghề/craft recipe. `SupportProfession` = Forging/Alchemy/Talisman/Enchantment, không là class Hero. Forge hiện không kiểm requiredProfessionLevel của DTO recipe. |
| `TagSO` / SkillType | DATA ONLY | [TagSO](../../Assets/DataScript/TagSO.cs#L4): ID/category/skin trigger, equality theo ID; không có runtime consumer ngoài chính type. | Không phải skill damage/cooldown/activation. `CultivationSpriteBridge` là presentation sprite, không phải hệ công pháp. |

**Phụ thuộc class:** không tìm thấy class/profession cố định khóa vũ khí ở primary. Phụ thuộc đáng chú ý là mode → HP/movement/heal/interval và fallback B2 legacy, không phải một trường class cần xóa. `savedAttackMode` được giữ tương thích; `RestoreSelectedStance` bind lại dữ liệu và resolve từ vũ khí, không lấy cache này làm authority. Không đề xuất xóa profession/enum hoặc sửa schema để “bỏ class”.

## A4. Quái, map và wave

| Hệ thống | Trạng thái | Bằng chứng | Giới hạn |
|---|---|---|---|
| Spawn/pool và dữ liệu quái | IMPLEMENTED & USED | [MonsterSpawner.SpawnMonster](../../Assets/Script/MonsterSpawner.cs#L6); `GameManager.CallNextWave`; [CombatManager.StartBattle](../../Assets/Script/CombatManager.cs#L216) clone baseMonsterData; `Assets/Prefabs/Monster.prefab` có attackMode 0. | Pool chung defaultCapacity 5/maxSize 20 (giới hạn cache, không phải trần active toàn game); wave 1–3 quái. Mode lấy từ controller/prefab, không phải một catalog AI/stat riêng cho từng loài. |
| Môi trường, tên/chủng loại | IMPLEMENTED & USED | `WorldMapCatalog.EnsureLoaded/WorldMapNode`, `StartBattle`, `WorldNames.ClassifyAnimal/MonsterName/RandomMonsterName`. | World-map chọn một trong 7 species của node hoặc vai người thù địch; tên gắn root/race/gender. Legacy dùng habitat theo terrain. `MonsterClass` là taxonomy, không phải class Hero. |
| AI phát hiện/ambush | IMPLEMENTED & USED | `EnvironmentManager.Update` → `TickMonsterAwareness/TickMovement`; `MonsterController.TickAwareness/ReceiveAlert`, `CombatManager.DealDamageFromHero`. | Tầm nhìn 500/hướng nhìn, đổi hướng 1,5–5 giây; cảnh giác 0,5s; báo động nhóm; hit bất ngờ ×2 sau phòng thủ và báo động ngay. Không sửa trong audit. |
| World-map/độ khó vùng | IMPLEMENTED & USED | [WorldMapCatalog](../../Assets/Script/WorldMapCatalog.cs#L28): 133 node, 5 birth points; `SuggestedLevel` theo khoảng cách graph tới birth, thường `1 + 3×depth`; `StartBattle` lấy band ±1. | Level map không chạy theo level Hero; fallback legacy có level spread theo Hero. Links/level là cấu hình prototype, không suy tên từ mapNumber. |
| Wave/clear/travel | IMPLEMENTED & USED | `OnMonsterDied` → `EntityDataSO.CompleteWave` → `WorldMapCatalog.CompleteWave`; `TickExplorationProgress`; `TravelBlockReason/TryTravelToWorldMap`. | Clear 5 wave lần đầu, loop 1–5 tại node; đi tiếp sau quãng đường, không auto chuyển map. Travel chỉ node kề sau clear; hiện deploy đầy HP, có AutoSave. |
| Boss/elite | PARTIAL | `WorldMapNode.IsBoss`, label UI, level boss metadata 120 và `TravelBlockReason` chặn boss. Đếm file `Assets/Resources/WorldMapCatalog.txt`: 133 ID duy nhất, 5 start, 6 boss flag. Không có boss spawn/AI/reward hoặc elite contract tìm được. | Metadata/gate đã có; 6 cờ không có nghĩa 6 boss encounter. Elite MISSING trong luồng đã tìm. Không thêm boss để lấp khoảng trống. |
| Defeat/Retry/Hard | IMPLEMENTED & USED | `OnHeroDied` → `RestartRegionAfterDefeat/RestartAfterDefeat`, `OnRetryClicked` → `DeployHero`; `IsHardMode`, incoming damage và death EXP. | World defeat giữ level/gear/clear history, reset tiến độ chưa clear, wave 1. Legacy khác: trở đầu vùng; Hard legacy map chia hết 6, damage quái ×2/EXP ×3. World ApplyMap đặt difficulty 0. Reward không nhân Hard. |

## A5. Economy và persistence

| Hệ thống | Trạng thái | Bằng chứng | Giới hạn |
|---|---|---|---|
| Production loot / gear | IMPLEMENTED & USED | [MainGame productionLoot](../../Assets/Scenes/MainGame.unity#L3818), `ProductionLootConfig.RollMaterial/RollEquipment`, `HandleMonsterDeathBy` → `TryClaim/CompleteProduction`. | Material .60, `material:refinement`, 1–2; gear .08, Uniform Sword/Bow level1 Attack4, Common/qty1. controlled A tắt. Hai RNG riêng, shared receipt; không sửa config. |
| Receipt/feedback | IMPLEMENTED & USED | [CombatLootRewards](../../Assets/Script/CombatLootRewards.cs#L6), `LootDropPresentation`, `CombatManager.ShowAwardedLoot` và log nhánh material/gear. | Receipt runtime có epoch/encounter/life/spawn; nhánh lỗi không reroll/rollback thưởng đã commit. Không phải ledger bền qua Save/crash. Kiếm có VFX, Cung log/Inventory, material không giả VFX kiếm. |
| Inventory/equipment | IMPLEMENTED & USED | `EquipmentInventory.Add/Equip/Unequip`, [EquipmentRuntime.Equip/RebuildModifiers](../../Assets/Script/EquipmentRuntime.cs#L51), `InventoryPanelUI.EquipSelected/RemoveSelected` → GameManager handlers. | Stack tối đa 9999; gear là instance list, không thấy capacity cap. Artifact legacy và instance không đồng thời chiếm cùng slot. |
| Forge runtime | IMPLEMENTED & USED | [EquipmentRefinement.TryApply](../../Assets/Script/EquipmentRefinement.cs#L167), `InventoryPanelUI.ChangeForge` → `GameManager.ApplyForgeTest` → refresh stats. | Prototype thật, dù handler còn tên Test: Normal/level-up tốn 1, Special 10; update rarity/refine/history/power; không phải toàn bộ recipe/crafting/profession system. Risky destroy rule chỉ là schema/helper an toàn, chưa là quy trình rèn nguy hiểm. |
| Gold | DATA ONLY về economy | `EntityDataSO.gold`, khởi tạo/normalize, SaveManager, Inventory hiển thị. Tìm mutation runtime chỉ thấy reset 0/normalize số âm. | Không có nguồn thưởng, shop hoặc sink Gold đã kết nối; Forge hiện không thu gold. Không tự thêm drop/sink. |
| Save/Load chính | IMPLEMENTED & USED | [SaveManager.SaveGame/LoadGame](../../Assets/Script/SaveManager.cs#L92), version probe; GameManager `TryManualSave/TryLoadSlot/AutoSaveRoutine/ReturnToMainMenu`; atomic temp + replace/move. | Lưu Hero snapshot: identity/root/growth/maps/inventory/equipment/forge/gold/DTO nghề. Normalize/rebuild derived modifiers; future equipment versions bị từ chối. Không lưu B2, live HP/timers/projectiles/receipts hoặc một realm/skill runtime chưa tồn tại. |
| Môn phái/cốt truyện phân nhánh | UNKNOWN về nội dung tổng thể | Không thấy entry point progression/story/sect trong runtime local qua tìm symbol và MainGame; WorldMapNation/nhãn địa danh là dữ liệu thế giới, không chứng minh membership/class/quest state. | Chưa audit tài liệu nội dung/nhánh khác; không lấy tên địa danh/vai địch làm chứng cứ đã có hệ truyện. Không đưa vào việc phải làm tiếp ngay. |

## D. Rủi ro phù hợp định hướng và sửa nhỏ nhất được đề xuất

| Rủi ro có bằng chứng | Đánh giá | Thay đổi nhỏ nhất cho tác vụ sau |
|---|---|---|
| DEVB nạp từ persistent settings | `UIManager.Awake` gọi `CombatBalance.LoadDevProfiles` không có guard Editor ở đường đã đọc. Báo cáo Gear Drop đã quan sát Attack 1000; phiên cuối reset static trong fixture, không sửa file cá nhân. Gameplay có fallback bình thường, **không bắt buộc DEVB**, nhưng cấu hình máy có thể che balance. | Hiển thị rõ override đang bật và xác định chính sách build/profile dev; tận dụng loader/UI hiện có, không âm thầm sửa file hoặc Save. |
| Điểm thuộc tính chưa có thao tác production | Cấp độ hoạt động nhưng agency phát triển còn thiếu; Physical có allocation, Magical chưa có đối ứng được thiết kế. | Nối hai API đã có vào panel, ghi đúng nhãn Physical; refresh state/dirty/save qua manager. Không tự tạo allocation Magic. |
| Realm chỉ là hàm tên chưa nối | Không có hai hệ progression đang cùng buff để gây double scaling; nguy cơ đến khi thêm realm authority thứ hai. | Trước tiên hiển thị derive từ level, không thêm field hoặc multiplier; độc lập breakthrough phải có quyết định sản phẩm. |
| Mode giống “soft class” | Vũ khí tự do nhưng đổi mode đổi HP cap/movement/kill-heal; `RefreshWeaponAttack` clamp HP và reset interval, không heal miễn phí. | Giữ rule hiện tại và test việc đổi vũ khí; tách affinity/move/HP khỏi mode chỉ khi được yêu cầu balance riêng. Không tái cấu trúc chỉ để đổi tên class. |
| Affix/stat có mặt nhưng không có consumer | CastSpeed/CDR/ManaRegen chưa vận hành; bonus element cụ thể không tự áp vào basic Vô. Production Sword/Bow chỉ có main stat nên không bị ba chỉ số “chết” này. | UI/tooltip nói rõ hỗ trợ/điều kiện; chỉ nối khi có activation contract được duyệt. Không tự thêm mana/cooldown. |
| Nhiều nguồn modifier | Gear rebuild sở hữu prefix `equipment:instance:`; charm dùng `equipment:primary-trinket`. `AddCombatModifier` append, không ép unique source. | Giữ rebuild idempotent theo chủ nguồn; passive tương lai cần ownership riêng. Đây là rủi ro tích hợp, chưa phải bằng chứng lỗi cộng trùng production hiện tại. |
| Save và derived data | Snapshot chứa modifier + item; Load tái dựng prefix hiện có. Version probe hiện tập trung inventory/equipment/affix/forge, không phải guard mọi dữ liệu tương lai; `WorldMapCatalog.Enabled` coi version >=1 là world mode. Thêm realm gate/learned combat skill state sẽ cần default/version/compatibility riêng. | Không rename enum/field profession hoặc dùng lại `LearnedSupportSkill` làm class/skill combat. Không suy guard equipment thành bảo vệ toàn schema; lập migration và old/current/future tests trước mở rộng. Chưa thử future world/skill schema trong audit. |
| Khu vực và dài hạn | Clear/Travel hiện phù hợp farm theo node; boss bị chặn vì luật unlock chưa chốt. Gear list không cap, material cap 9999 sẽ có nhánh award fail khi đầy. | Nghiệm thu farm dài và phản hồi overflow trước; không tự thêm sell/salvage/boss hoặc thay drop rate. |
| Root hiện có hai lớp tác động | Weighted root multiplier áp mọi trace; boost/resistance theo element của CombatDamagePart là lớp khác, basic đi Vô. | Ghi rõ contract trước tích hợp võ học; không cộng lại hệ số root trong skill adapter hoặc tự đổi basic thành element vũ khí. |

## Bằng chứng kiểm thử và giới hạn xác minh

- **Không chạy kiểm thử trong audit này.** Số PASS dưới đây là kết quả báo cáo cũ trên code module liên quan, không phải lần chạy mới.
- Gear Drop report: Mono gear 5/5, release-guard variant 5/5, weapon 31/31; verifier Unity MainGame thực chạy combat → Cung → UI routing equip → Forge bằng material reward → Save/Load → combat. Chance bị ép 1 trong runtime fixture; scene thật vẫn .60/.08. Native mouse/Player còn NOT RUN.
- Tests tồn tại, đã đọc tên/assertion liên quan: `BalanceRegression.cs` (EXP/allocation), `ElementWorldRegression.cs` (25 tier matchups, weighting, names/save), `Version0051StatsRegression.cs` (tags/resistance/regen), `WorldMapRegression.cs` (133 node/loop/travel/boss gate), `MonsterAwarenessRegression.cs`, `WeaponBasicAttackRegression.cs`, `GearDropRegression.cs`; `Assets/Tests/SaveLoad/EditMode/{JsonUtilitySaveLoadTests,EquipmentVersionGuardTests}.cs`, PlayMode SaveLoad. Sự tồn tại của test không chứng minh chức năng production đã nối.
- Chưa có test thực thi realm gate, learned combat skill, mana hoặc nghề/craft recipe vì chưa tìm thấy runtime tương ứng. Tên skill/recipe trong fixture JSON chỉ chứng minh round-trip DTO.

Xem [bản đồ phụ thuộc](GAMEPLAY_DEPENDENCY_MAP.md), [roadmap và USER DECISIONS REQUIRED](INTEGRATION_ROADMAP.md). Không có thay đổi runtime từ báo cáo này.
