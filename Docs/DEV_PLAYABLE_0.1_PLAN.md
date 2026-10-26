# Dev Playable 0.1 — W3.1a readiness audit và kế hoạch

Ngày audit: **26/10/2026** (Asia/Ho_Chi_Minh). Repository: `lavievn/IdleGame`, nhánh `main`.

**Kết luận: chưa đủ điều kiện gọi là Dev Playable 0.1 theo chuỗi yêu cầu.** Combat,
EXP, inventory mẫu, equip và persistence có nhiều phần đã nối; reward item từ
combat và UI compare còn đứt. Đây là audit tĩnh, không phải nghiệm thu Unity/Player.
Không triển khai W3.1b trong commit này.

## 1. Baseline, phạm vi và cách đọc kết quả

- HEAD local khi bắt đầu: `90c65e4d1ab7585eed69fabe6166bbfa58dd9330`, working tree sạch.
- Fetch thấy ba commit tài liệu mới `6711f1c`, `ab50c8f`, `3edacb2` trên remote.
  Đã đối chiếu diff chỉ gồm AGENTS, README và tài liệu core loop, rồi fast-forward
  local. **Snapshot audit: `3edacb2f683df007025604944202ab558f351cd6`**.
- Đã đọc [AGENTS](../AGENTS.md), [project rules](../.agents/skills/idle-game-project-rules/SKILL.md),
  [Unity/C#](../.agents/skills/unity-csharp-scripting/SKILL.md),
  [save-systems](../.agents/skills/save-systems/SKILL.md), [PROGRESS](PROGRESS_2026-10-25.md),
  [core loop](CORE_GAMEPLAY_LOOP_DIRECTION_2026-10-25.md) và phần trạng thái/HUD W3 trong [README](../README.md).
- Chỉ đọc mã, scene YAML, cấu hình và các test hiện có. Chỉ tạo tài liệu này;
  không chạy gameplay, build, Mono suite hoặc test save I/O; không mở save người chơi.
- **Có và hoạt động (tĩnh)**: đường gọi và dữ liệu được nối trong mã; không đồng
  nghĩa đã PASS tương tác/render trong Unity. **Chưa hoàn chỉnh**: chỉ có một phần.
  **Chưa có**: không tìm thấy đường triển khai tương ứng trong phạm vi mã đã truy vết.
  **Cần kiểm thử**: kết luận phụ thuộc vòng đời/render/native input/I/O thực tế.
- Phân loại phát hiện: **C** = chắc chắn từ mã; **R** = rủi ro có cơ sở;
  **U** = cần Unity/Windows kiểm chứng. Khoảng trống tính năng không tự động là bug.
- Định hướng core loop là tài liệu làm việc. Không coi party, Mana, offline hay
  checkpoint W2.3 là tính năng đã được phê duyệt chỉ vì có trong roadmap.

## 2. Phạm vi tối thiểu đề xuất

Một nhân vật chính đi hết chuỗi **New → entry/VFX → khám phá/chiến đấu → EXP/item
→ Hòm đồ → chọn/so sánh → equip/unequip → thấy stat đổi thật → farm/travel → save,
thoát, Continue**. Dùng hai node kề nhau hiện có, không phải map Boss; chọn cặp
thực tế sau khi xem điều kiện và khả năng chơi, không đổi balance để ép thắng.

Ba thẻ HUD có thể phản ánh Hero chính và hai B2 **tạm thời** khi được gọi trong
chế độ dev; không phát sinh party save/equipment riêng. Khi thiếu actor, hiển thị
trạng thái trống rõ ràng. Mana chưa có nguồn runtime đã xác định: cần chốt cách
biểu thị chưa hỗ trợ, không dựng số giả hoặc thêm hệ Mana trong task HUD.
Võ học chưa có luồng người chơi: nút có thể báo chưa khả dụng rõ ràng nếu phạm vi
0.1 được chấp thuận; không coi đổi thế Kiếm/Cung/Phép là quản lý võ học hoàn chỉnh.

Mục tối thiểu này là kế hoạch đề xuất, chưa phải quyền triển khai các task sau.

## 3. Gameplay UI: hiện trạng và đối chiếu W3

| Chức năng | Trạng thái | Bằng chứng / thiếu hụt |
|---|---|---|
| HP Hero chính, cấp, EXP, ATK | Có và hoạt động (tĩnh) | `HeroController.UpdateHealthBar/UpdateStats`, `CombatManager.LateUpdate`; text debug và HP trên actor, chưa có card W3 |
| Mana | Chưa có trong luồng đã audit | Không có trường Mana trong EntityDataSO/HeroCombatState hay binding trong HUD; Magic charge không phải Mana |
| Ba Hero | Chưa hoàn chỉnh | `GameManager.CycleTestCompanions/CreateTestCompanion`, registry `HeroCombatState`; B2 có HP/target/timer/Data riêng, `atkStatusText=null`, không có ba card |
| Map/wave/quãng đường | Có và hoạt động (tĩnh) | `UIManager.ConfigureMapTitle/UpdateMapDebug/UpdateMapTitleAppearance`, các property exploration của GameManager; chưa phải tên map dọc + wave bên trái theo W3 |
| Nhân vật | Chưa hoàn chỉnh | `OpenHeroStats` dùng `FindFirstObjectByType<HeroController>` và text debug kèm log/damage; không chọn rõ actor theo identity |
| Hòm đồ | Có và hoạt động (tĩnh), cần U | `OpenInventory`, InventoryPanelUI, paging/filter/selection/8 slot; nhiều công cụ cấp đồ thử nghiệm |
| Võ học | Chưa có UI người chơi trong đường hiện tại | Chỉ thấy nút đổi thế và dữ liệu learnedSupportSkills/recipes; không có panel học/chọn võ học nối toolbar |
| Bản đồ | Có và hoạt động (tĩnh), cần U | WorldMapPanelUI: zoom, chọn node, route, travel; nút chỉ hiện với world-map mode |
| Menu | Có và hoạt động (tĩnh), cần U | Save1/2, Load, overwrite, size, return title, reset, quit; không cùng toolbar W3 bên phải |
| Log | Chưa hoàn chỉnh so với W3 | `RefreshCombatHUDVisibility` bật eventLog khi HUD visible; chưa có mặc định ẩn và toggle dành cho người chơi |
| Popup xác nhận | Có mã, cần U | StartMenuUI tạo confirmation, pending choice chặn input; overwrite có page riêng. Menu Exit gọi ConfirmExit trực tiếp; `exitConfirmPopup` trong MainGame là null, không được giả định có xác nhận thoát |
| Pause khi mở UI | Không đồng nhất, cần U | Inventory và WorldMap capture/restore trạng thái pause; system menu, stats/log/damage không tự pause. Không mặc định đây là bug khi chưa chốt hành vi mong muốn |
| 600/800/1150 | Có hạ tầng, cần U | `SetWindowScale/ExecuteScale`, TransparentWindow, ManagementWindowLayout; inventory mở rộng bên phải hoặc fallback overlay, không phải chỉ scale Canvas |
| Căn chỉnh Play Mode | Chưa hoàn chỉnh cho Gameplay HUD | OpeningPlayModeLayoutWindow lưu OpeningArtTheme, validate và backup; không có editor HUD tương đương. Inventory có overlay đo/kích thước, không phải bộ lưu vị trí HUD |

### Canvas, sorting và input

- MainGame YAML có ba Canvas Overlay, sorting order 0. Runtime
  `EnvironmentManager.InstallStableBattleCanvas` tạo Stable Battle Canvas, giữ
  logical width/BattleArea; resize chỉ đổi projection qua SyncBattleProjection.
- `UIManager.InstallReadableCanvas`: Readable HUD Overlay order **100**, Compact
  HUD Viewport; reparent menu/log/confirmation/game-over từ scene. Inventory và
  WorldMap nằm trong HUD, dùng sibling order. `GameManager.ShowMapEntryCover` tạo
  Canvas order **1000**. Không thay hệ này để ép actor vào vùng HUD mới.
- `BindClick`/LateUpdate chuyển Button sang CustomInteractable, tắt Button native.
  UIManager.Update xử lý InputSystem trong Editor; TransparentWindow gọi
  HandlePointerClick trong Windows Player. Image/Text thường `raycastTarget=false`
  có chủ ý: hit test dùng RectangleContainsScreenPoint và registry riêng.
- `HitControl` có guard entry, confirmation, Opening, WorldMap, DEVB, inventory,
  forge, damage và system menu. Tuy nhiên không dùng GraphicRaycaster/sorting
  để chọn mọi control chồng nhau: giữa các sibling độc lập có thể ưu tiên thứ tự
  đăng ký. **R**, cần test chồng hitbox, không kết luận mọi modal đã bị click xuyên.
- **R**: `OpenHeroStats` tìm Hero bất kỳ nhưng ghép identity của `gm.HeroData`;
  với B2 có thể hiển thị sai actor hoặc text chưa có vì UpdateStats trả sớm nếu
  atkStatusText null. Card W3 cần bind bằng registry/identity, không tiếp tục suy
  actor chính từ FindFirstObjectByType.
- **C**: chỉnh RectTransform thủ công có thể bị LayoutReadableUI/Inventory.Layout
  ghi lại khi resize/content đổi. Opening layout saver không áp cho HUD.
- **U**: kiểm tra nhiều DPI, overlay/fallback, thứ tự Start, toggle panel khi pause,
  hủy entry và đóng panel; không suy từ scene YAML ra thứ tự hình ảnh cuối render.

## 4. Truy vết reward → inventory → combat

Tất cả đường dẫn C# dưới đây ở `Assets/Script/`, trừ EntityDataSO ở `Assets/DataScript/`.

| Bước | Class/hàm | Trạng thái và dữ liệu |
|---|---|---|
| Quái chết | `CombatManager.HandleMonsterDeathBy` | Remove khỏi active list, MarkDead, EXP vào **attacker.Data**, heal, despawn và `GameManager.OnMonsterDied` |
| Item reward | Không thấy call cấp item trong death/wave loop | **Chưa có** nối combat → bag. Search callsites Add/AddInstance chỉ thấy initialization/test grant/equip trả đồ; không tìm thấy loot producer trong đường chơi |
| Seed/test item | `EquipmentInventory.InitializeNewRun/GrantSamples`, `GameManager.GrantForgeTestSword/GrantForgeMaterials` | New có hai charm mẫu; UI có nút kiếm rèn và +100 nguyên liệu. Không phải loot chiến đấu |
| Lưu vật phẩm | `EquipmentInventory` và `EquipmentRuntime` | Stack ID/quantity legacy; gear instance canonical list với instanceId, isEquipped/equippedSlot; gear không nằm thêm một bản ở bag |
| Hiển thị/chọn | `InventoryPanelUI.Hero/RebuildEntries/SelectBagCell/SelectEquipped/FindSelected` | Luôn dùng GameManager.HeroData chính; 20 ô/trang, filter gear/item; key gear/stack/worn |
| Chi tiết item | `InventoryPanelUI.Refresh/StatLine` | Có rarity, level, refine, main/extra, hidden/locked/active affix; không phải so sánh delta |
| Compare | `EquipmentStatPreview.TryEquip/TryEquipLegacy/TryUnequip/Current` | **Có code nhưng chưa nối UI**. Search Assets chỉ thấy khai báo, không caller; Tests~/EquipmentStatPreviewRegression có gọi nhưng không phải UI runtime |
| Equip/Unequip | `InventoryPanelUI.EquipSelected/EquipRight/RemoveSelected` → `GameManager.EquipGear/UnequipGear/EquipInventoryItem/UnequipInventoryItem` | Nối đầy đủ tới EquipmentRuntime hoặc EquipmentInventory; ring trái/phải và Artifact legacy có luật riêng |
| Recalculate | `EquipmentRuntime.RebuildModifiers`, legacy RebuildModifier → `CombatManager.RefreshRegisteredHeroStats(primary,true)` | Source prefix riêng, không cộng trực tiếp vào base. Giữ tỷ lệ HP, reset attack theo đường hiện có |
| Combat đọc stat | `EntityDataSO.GetCalculatedDamage/GetCalculatedHealth`, CombatStats, `CombatManager.TickRegisteredHeroAttack` | Modifier được combat đọc; UpdateStats phản ánh total. Cần chứng minh hiệu quả trong Unity bằng stat/trace, không ép damage mọi đòn tăng khi crit/giáp/ngẫu nhiên khác |
| Persistence | `SaveManager.SaveGame/LoadGame`, `GameManager.TryLoadSlot` | JSON hero chính có inventory/equipment/modifiers/version; load snapshot riêng rồi normalize/rebind combat |

### Nhân đôi, cộng stat hai lần và mất item

- **Bảo vệ hiện có (C):** AddInstance từ chối ID đã có, nhận bản copy; equip gear
  chỉ đổi assignment trong canonical list; modifier reconciliation thay đúng
  source prefix và dedupe slot/ID. Legacy equip/unequip kiểm tra sức chứa trước
  trả charm cũ, cấm Artifact legacy và gear cùng slot. Không thấy bằng chứng rằng
  thao tác equip thông thường tự nhân đôi item hay cộng bonus hai lần.
- **C:** nút GrantForgeTestSword sinh instance mới mỗi lần, GrantForgeMaterials
  cộng thêm 100 nhiều lần. Đây là công cụ dev có chủ ý nhưng làm vô hiệu đánh giá
  progression nếu nằm cùng luồng chơi thường. GrantSamples và GrantDemoSword có
  flag một lần; nút kiếm rèn thực tế gọi hàm grant lặp khác GrantDemoSword.
- **R:** Normalize giữ item không hợp lệ/unknown và hạ assignment invalid, không
  xóa mọi entry trùng ID. Find trả entry đầu; dữ liệu hỏng có thể gây chọn mơ hồ.
  Không tự xóa item để “sửa” save. Cần fixture synthetic duplicate/future item.
- **R:** reward mới chưa có policy người nhận/ID/idempotency. Không dùng retry,
  pool reuse hay reload để phát thưởng trùng; không tự chọn tỷ lệ/drop table.
- **C:** EXP được cấp cho attacker, gồm B2. B2 bị ClearTestCompanions khi travel,
  load, retry/deploy hoặc return title, Data không serialize; EXP đó mất khi B2
  bị dọn là hành vi test hiện tại. Không mô tả nó thành progression party bền vững.
- **R:** rollback về manual snapshot cũ làm item nhận sau snapshot biến mất theo
  chính sách load snapshot; không được gọi đó là bug mất đồ hoặc tự giữ lại item,
  vì sẽ tạo mixed-era progression/duplication. Cần thông tin UI rõ và test.
- **C:** guard future inventory/equipment/affix/forge trước mutation/overwrite;
  legacy Normalize chạy trước EquipmentRuntime.Normalize, không reroll affix.
  Version theo thành phần, không thêm root version/backup/checkpoint trong audit.

## 5. Đường chơi tối thiểu và persistence

| Luồng | Trạng thái tĩnh | Giới hạn / nghiệm thu bắt buộc |
|---|---|---|
| New Game | Có và hoạt động | `OnNewGameClicked` → confirmation → `OnConfirmResetClicked`, reset AutoSave, InitHeroData, DeployHero; manual slots giữ nguyên |
| Continue | Có và hoạt động | CanContinue lấy slot mới nhất theo thời gian; load vào clone và kiểm tra level. **R:** file mới nhất tồn tại nhưng corrupt/future vẫn được chọn; không fallback tự động sang slot hợp lệ khác |
| Spawn/VFX → wave 1 | Có và hoạt động | DeployHero/OnOpeningRevealCompleted/CompleteGameplayEntry; release ở LateUpdate, CallNextWave sau VFX. **U:** thiếu VFX/hủy entry/resize không kẹt pause hoặc cover |
| Wave theo quãng đường | Có và hoạt động | OnMonsterDied → waitingForExploration → TickExplorationProgress lấy min X thực tế các Hero hợp lệ, bỏ dư khi tới ngưỡng; không dùng pan hay 5 giây cho wave clear |
| EXP/level | Có và hoạt động | AddExp đánh dấu dirty, rebuild stat và growth khi lên cấp; EXP của killer B2 tách Hero chính |
| Item sau combat | Chưa có | Chặn chuỗi mục tiêu; không tính grant mẫu là nghiệm thu loot |
| Farm và mở map | Có và hoạt động | WorldMapCatalog.CompleteWave loop 1..5 tại node, lưu cleared; không auto travel |
| Travel | Có và hoạt động | TravelBlockReason: node kề, map hiện tại đã clear 5 wave, không Boss; TryTravelToWorldMap dọn quái/projectile/companions, entry mới, full HP theo prototype |
| Defeat/Retry | Có và hoạt động | OnHeroDied → RestartRegionAfterDefeat → AutoSave → game-over; Retry DeployHero. World-map giữ node, reset wave, giữ clear history; không đổi checkpoint |
| Manual save/load | Có và hoạt động | UI 2 slot + overwrite page, TryManualSave bool I/O; TryLoadSlot snapshot/rebind, không trộn inventory mới với level cũ |
| AutoSave | Có và hoạt động | AutoSaveRoutine chờ 10 giây **scaled time**, dirty, deployed, không confirmation; pause dài sẽ không tick autosave. Death/travel/new/quit/title có save riêng |
| Return title/quit | Có và hoạt động một phần | ReturnToMainMenu chặn rời nếu save thất bại; ConfirmExit gọi Quit, OnApplicationQuit ghi save nhưng không có UI chặn quit khi ghi thất bại. Crash/kill process không bảo đảm callback |

SaveManager ghi `.tmp` rồi Replace/Move đồng bộ, không queue write cũ. Save thành
công mới clear isDirty. Không có bằng chứng đủ để bảo đảm crash/power-cut durability
trên Windows; không tự thêm backup rotation. Một số auto-save failure chỉ vào log,
trong khi menu mở có thể ẩn log: **R về phản hồi**, cần case lỗi quyền ghi. Trực tiếp
LoadGame vào object sống có thể mutation rồi thất bại; đường GameManager dùng clone
để tránh điều đó. Không sửa API SaveManager trong audit.

## 6. Top 5 chặn Dev Playable và mức độ

1. **P0 / C — Không có combat item reward:** không hoàn tất chơi → loot → build.
   Cần task/spec reward tối thiểu riêng, không invent tỷ lệ hay thay balance.
2. **P0 / C — Compare chưa nối UI:** người chơi không thấy trước tác dụng thay đồ
   dù có helper tính; ưu tiên tái sử dụng helper thay vì viết công thức mới.
3. **P1 / C — HUD/identity thiếu:** chưa có card W3, Mana nguồn dữ liệu chưa có,
   stats chưa chọn actor chắc chắn, log bật mặc định, toolbar thiếu Võ học.
4. **P1 / C — Luồng chơi thường lẫn công cụ cấp đồ/DEVB/B2:** có thể vượt qua toàn
   bộ vòng reward bằng nút test; không thể dùng phiên đó chứng minh progression.
5. **P1 / U+R — Chưa có nghiệm thu xuyên suốt UI/input/persistence trên native
   Player:** 600/800/1150, modal, layout, quit/save failure và Continue từ slot lỗi
   còn cần bằng chứng. Đây là release gate, không phải kết luận tất cả đều lỗi.

Không phát hiện lỗi duplication chắc chắn trong đường equip hợp lệ đã đọc.
Không có số PASS Unity mới trong audit này. Test hiện có không thay bằng chứng
chạy đúng snapshot và đúng môi trường.

## 7. Task nhỏ, phụ thuộc và thứ tự W3.1b → W3.5

Mỗi hàng là một task/commit riêng, có kiểm thử tương ứng; không gom thành một lần
rewrite UI. Ưu tiên kết nối chức năng trước mỹ thuật. Tên phase dưới là đề xuất
lập kế hoạch, không tuyên bố đã bắt đầu triển khai.

| Task | Phạm vi/file dự kiến | Phụ thuộc | Tiêu chí PASS |
|---|---|---|---|
| W3.1b.1 — Nối compare hiện có | InventoryPanelUI.cs; test UI comparison mới cùng .meta; EquipmentStatPreview.cs chỉ đọc trừ khi có bug được chứng minh | Audit này | Chọn charm/gear/ring đúng slot hiện current → preview → delta; tháo có preview; chỉ xem không dirty/mutation; hidden affix không lộ; total sau equip bằng preview cùng mode |
| W3.1b.2 — HUD dữ liệu và điều hướng tối thiểu | UIManager.cs, HeroController.cs hoặc component HUD riêng; test HUD | b.1; chốt Mana/actor placeholder | 3 card bind đúng identity khi có B2, trống khi thiếu; HP/EXP/level thật; map/wave bên trái, nút hệ thống bên phải; log ẩn/toggle; Võ học báo chưa khả dụng nếu chưa có luồng |
| W3.1b.3 — Layout và modal contract | UIManager.cs, layout component/editor riêng nếu cần; test geometry/input | b.2 | 600/800/1150 không đè nút/che combat; pause restore đúng trạng thái cũ; confirmation/forge/map không click xuyên; vị trí HUD chỉnh/lưu/khôi phục Play Mode, giữ Opening layout |
| W3.2.1 — Chốt reward contract tối thiểu | Tài liệu spec riêng trước code | b.1; quyết định người dùng | Chỉ rõ item/template hợp lệ, người nhận, điều kiện cấp, số lượng/tỷ lệ, xử lý replay/rollback, không đổi checkpoint/balance ngầm |
| W3.2.2 — Nối combat reward → bag | CombatManager.cs hoặc reward service nhỏ; GameManager.cs chỉ nếu cần; EquipmentRuntime/Inventory API dùng lại; test reward | 2.1 được duyệt | Một death hợp lệ cấp đúng hợp đồng; callback trùng/pool reuse/stale map không cấp thêm; hòm đồ thấy item; không cần grant mẫu |
| W3.2.3 — Hoàn tất equip feedback và tách debug | InventoryPanelUI.cs, UIManager.cs; test inventory | 2.2 + b.1 | Chọn/equip/unequip nhiều lần không đổi tổng instance/quantity ngoài thao tác hợp lệ; total/HP ratio đúng; lỗi slot/cap có thông báo; grant/DEVB/B2 tách chế độ dev rõ, không sửa tỷ lệ rèn |
| W3.3.1 — Save/Load acceptance có fixture cô lập | Tests SaveLoad hiện có + test mới; chỉ mở task fix riêng nếu phát hiện lỗi | 2.3 | Manual rollback toàn snapshot; item ID/affix/forge/equipped không reroll; normalize lặp không cộng stat; future schema bị từ chối, slot không đổi; New giữ manual |
| W3.3.2 — Phản hồi persistence/travel/retry | UIManager.cs, GameManager.cs khi task riêng cho phép; test failure UX | 3.1 | Save thất bại hiện rõ và không báo thành công; slot corrupt/future có lý do và có đường chọn slot khác; quit cảnh báo phù hợp; không tự đổi chính sách checkpoint |
| W3.4.1 — Chạy core loop hai map | Biên bản test + fixture/harness khi cần | b.3 + 3.2 | Chơi New → reward → compare → equip → farm → unlock/travel → defeat/retry → manual/auto/quit/Continue; không grant, không đổi damage/drop để tạo PASS |
| W3.4.2 — Hoàn thiện khả dụng | Chỉ sửa lỗi đã tái hiện từ 4.1, mỗi nhóm commit riêng | 4.1 | Text/button đọc được, đóng panel được, không kẹt pause, lỗi rõ; không yêu cầu redraw asset hay seamless mọi map |
| W3.5.1 — Development build Windows | Cấu hình/build script chỉ sau task build riêng; biên bản test | 4.2 | Unity 6000.5.0f1, scene MainGame, build report Succeeded; chạy executable native, kiểm input/DPI/cửa sổ/save/quit; lưu SHA và log |
| W3.5.2 — Release gate Dev Playable 0.1 | Checklist/nghiệm thu + known issues | 5.1 | Không còn P0; mọi P1 ảnh hưởng loop được xử lý hoặc cắt phạm vi công khai; clean profile test riêng hoàn tất, không gọi là Alpha |

Đường phụ thuộc chính: **compare → reward contract → reward integration → equip
feedback → save acceptance → hai map → Windows build**. HUD semantic và layout
có thể làm sau compare, nhưng mỹ thuật không được chặn việc nối reward.
Mana thật, party bền vững hoặc Võ học đầy đủ nếu được yêu cầu sẽ là phạm vi mới,
phải chốt trước khi cam kết 0.1.

### Task triển khai đầu tiên đề xuất: W3.1b.1

Tận dụng EquipmentStatPreview, thêm phần current/preview/delta trong chi tiết
InventoryPanelUI, dùng mode của Hero chính và cùng slot thực tế của EquipSelected
(đặc biệt tự chọn ring phải khi ring trái đã có). Không thêm item/drop/đổi stat.
File dự kiến: `Assets/Script/InventoryPanelUI.cs`, một test integration UI mới
trong `Assets/Tests/Presentation/PlayMode/` cùng `.meta`; tham khảo
`Tests~/EquipmentStatPreviewRegression.cs`, không mặc định Mono chứng minh UI.

PASS cụ thể: charm HP, charm ATK, gear và ring phải so sánh đúng; sau equip total
khớp preview trước đó trong cùng ngữ cảnh; tháo trả đúng số lượng; chuyển selection
không làm dirty hoặc đổi assignment; affix ẩn không bị tiết lộ; không chạm camera,
wave, SaveManager hoặc save schema. Trong Unity, đọc được ở ba cỡ và không click
xuyên. Chỉ bắt đầu task này sau bàn giao audit và yêu cầu triển khai tiếp theo.

## 8. Checklist nghiệm thu Unity/Windows cần người dùng thực hiện

1. Môi trường test riêng, không dùng save thật. Với test I/O: đọc README của
   Assets/Tests/SaveLoad, fixture cấu hình đường dẫn trước Awake, inactive GO,
   marker/ownership, cleanup giới hạn. Không chạy SaveLoad I/O trong Cloud.
2. MainGame New/Cancel/Confirm, Continue từ mỗi slot, save corrupt/future và
   lỗi quyền ghi. Xác minh trước/sau bằng snapshot fixture và item IDs, không
   ghi đè dữ liệu cá nhân. Manual snapshot cũ phải phục hồi cả level/EXP/map/item.
3. VFX bình thường/thiếu asset/hủy entry; wave đầu đúng một lần; wave sau theo
   khoảng X chung; pause không tích khoảng đường; escaped wave không clear nhầm.
4. Kill bằng từng Hero, B2 summon/death/recall; đối chiếu EXP đúng Data và giới
   hạn không persistence B2. Không giả định đã có party save.
5. Với loot sau khi task riêng được triển khai: item có mặt không dùng nút mẫu;
   select/compare/equip/unequip/swap ring/Artifact; stats và combat trace khớp,
   inventory không tăng/giảm ngoài luật; mở lại panel/Load không nhân đôi bonus.
6. Clear năm wave, tiếp tục farm cùng node, travel node kề hợp lệ; node chưa
   clear/non-adjacent/Boss có lý do chặn; Retry giữ đúng chính sách hiện tại.
7. Mỗi cỡ 600/800/1150: HUD, text dài, inventory nhiều trang, affix dài, forge,
   map zoom, overlay fallback và expanded/collapse; giữ vị trí cửa sổ, không đổi
   logical battle width/attack range; DPI 100% và ít nhất một DPI cao thực tế.
8. Kiểm modal chồng nhau, press/release, drag cửa sổ, click ngoài panel,
   confirmation/overwrite, pause trước mở và sau đóng; kiểm cả Editor và Windows
   transparent Player, log Console không có exception. Xác minh exit thực tế:
   hiện menu Exit không đi qua popup xác nhận.
9. Chỉnh/lưu/khôi phục Opening Play Mode vẫn đúng; HUD hiện chưa có saver tương
   đương, nghiệm thu sau b.3. Resize không làm sai vị trí logic Hero/VFX/camera.
10. Save → return title → Continue; save → quit executable → relaunch; simulated
    save failure không báo thành công. Ghi version Unity, SHA, mode, bước tái hiện,
    expected/actual, log/screenshot; PASS Unity/Windows ghi riêng với Mono.

## 9. Hoãn sau Dev Playable 0.1

- Party dài hạn, trang bị/lưu riêng cho B2, chia EXP mới; không tự biến ba card
  thành cam kết ba nhân vật persistent.
- Mana/resource hoặc hệ Võ học mới, profession/recipe progression đầy đủ, rèn
  sâu hơn; không mở rộng chỉ vì dữ liệu serialized đã tồn tại.
- W2.3 checkpoint, offline progression, simulation/NPC và nhật ký hành trình;
  cần đặc tả riêng. Không tự đổi chính sách death/travel/full HP.
- Balance mới, redesign camera, tự travel, thay wave clock; thêm currency/drop
  table lớn, mở Boss/endgame và nghiệm thu mọi nội dung 133 node.
- Vẽ lại UI, artwork/seam mọi terrain, tối ưu không có profile. Chỉ khắc phục
  readability/input/performance thực sự chặn loop sau khi tái hiện.

## 10. Xác minh bàn giao tài liệu

Kiểm tra phạm vi diff chỉ có tài liệu này, liên kết tương đối/file tham chiếu,
`python Tools/verify_agent_skills.py` và `git diff --check`. Re-fetch remote trước
commit/push; nếu HEAD remote khác snapshot ngoài dự kiến thì dừng bàn giao Git,
không merge/rebase/force-push tài liệu lên nền chưa audit. SHA commit tài liệu và
trạng thái remote cuối được báo trong bàn giao chat, không chèn SHA tự tham chiếu.
