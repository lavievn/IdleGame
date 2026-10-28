# W3.2.2 — Controlled Combat Loot, Profile A

Ngày: **28/10/2026** (Asia/Ho_Chi_Minh). Baseline đã fetch và đối chiếu:
`ba7724c82406b1f7395c64b9d5b939317dccbb46`, main sạch, origin/main cùng SHA.
Triển khai theo D1–D4/D6 của [reward contract](W3_2_1_COMBAT_REWARD_CONTRACT.md)
được người dùng duyệt trong nhiệm vụ này. Không triển khai B hoặc W3.2.3.

## 1. Bật / tắt trong Unity

1. Mở MainGame, vào **Play Mode** với profile chơi thử riêng.
2. Chọn GameObject chứa **GameManager**. Trong Inspector, tìm mục
   **DEV ONLY — Controlled Loot Test A (default Off)**.
3. Bật **Controlled Loot Test** để thử; bỏ chọn để tắt cho những kill kế tiếp.
   Giá trị mặc định là **false**; không sửa scene/prefab để bật sẵn trong commit.
4. Kill hợp lệ khi bật ghi log `[LOOT TEST A] Đang bật · Nhận Thiết Kiếm (Common)
   vào Kho đồ.` Không có cửa sổ bắt xác nhận, không tự mở Inventory/equip/use.

Cờ chỉ có hiệu lực khi compile với **UNITY_EDITOR hoặc DEVELOPMENT_BUILD**.
Player không phải development luôn xem là Off, kể cả scene cục bộ đã bật cờ.
Nếu muốn thử Windows Development Build, người phát triển phải chủ động đưa cờ
vào cấu hình scene thử trước build và hoàn nguyên cấu hình đó sau thử; chưa có
nút điều khiển loot trong HUD và không thêm menu mới. Bật trong Editor Play Mode
không tự biến thành cấu hình Player sau khi thoát Play Mode.

Cờ thuộc component GameManager, không thuộc Hero/save. Load không tự bật loot;
đồ đã nhận vẫn ở snapshot nếu đã save, tắt cờ không tự xóa đồ đã nhận.

## 2. Hành vi và identity thực tế

- Một accepted lethal kill khi A On sinh đúng **một CreateDemoSword()**: template
  `sample:iron_sword`, level 1, Common, refine 0, ATK main +10, tag melee, không
  extra affix, GUID mới, chưa equipped. Không gọi GrantSamples/GrantForgeTestSword.
- Beneficiary là **GameManager.HeroData**, không phải attacker.Data. B2 hạ quái
  vẫn cấp đồ vào Hero chính, kể cả Hero chính đã chết khi B2 còn chiến đấu.
  EXP recipient và melee kill healing giữ nguyên như baseline.
- CombatManager sở hữu service `CombatLootRewards` và truyền HeroData chính vào.
  RunEpoch đổi khi reference snapshot primary thay; encounter GUID mới ở
  StartBattle; mỗi ActiveMonsterInfo giữ lifeId và SpawnVersion đã capture.
  Map version/id/number cũng được capture để kiểm context. Không dùng object
  pool, frame hoặc wave 1..5 đơn lẻ làm KillKey.
- Damage chỉ được chấp nhận với target active, đúng captured spawn và context.
  Death handler còn yêu cầu HP=0 **và** cờ lethal đã được damage pipeline đặt,
  attacker đăng ký/data hợp lệ, membership và TryClaim thành công.
- Claim được tiêu thụ trước EXP/wave notification. Remove active info trước
  phần thưởng; receipt terminal trước factory/Add để chặn re-entry. Profile
  Off cũng ghi NoDrop, không cho callback trùng bật lại thưởng bằng cách đổi cờ.
- `Complete` chạy sau EXP/heal/UI stat update hiện có, trước despawn/destroy và
  trước `GameManager.OnMonsterDied`. Kill cuối wave không mất loot khi progression
  chuyển trạng thái. Magic AoE hợp lệ có mỗi victim một receipt.
- Không gọi Equip, RebuildModifiers hoặc RefreshRegisteredHeroStats lúc nhận
  loot. `EquipmentRuntime.AddInstance` validate/copy/mark dirty; Inventory và
  compare dùng nguyên luồng W3.1b.1, chỉ Equip mới áp modifier.
- Không dùng UnityEngine.Random cho loot. Factory Guid và context Guid không
  tiêu thụ combat RNG; không đổi công thức damage, EXP, heal hoặc spawn.

### Cancel / load / disable / resume

ClearBattle và SetupHeroInfo hủy receipt; Load/Travel/Retry/New/Menu hiện dùng
các đường clear/setup này. Khi primary reference/map thay, key cũ còn bị từ chối
ngay cả trước lần register tiếp theo. Load bị từ chối không publish snapshot nên
không tự đổi epoch đang sống.

Disable CombatManager hoặc GameManager cũng invalidate context và tăng battle
version để projectile cũ không được dùng lại. Khi enable, re-arm chỉ sau khi
owner/reference/map và **tất cả** đời quái còn sống/HP/spawn đều được xác thực;
đăng ký encounter GUID mới. Không phát loot từ enable/cleanup, không phục hồi
projectile cũ. Nếu validation không đạt thì không tự biến object cũ thành life
mới; cần đường clear/new encounter bình thường.

Receipt chỉ giữ các đời của encounter hiện tại, dọn khi invalidate/start mới;
không tăng một ledger RAM vô hạn theo số wave. Quái pooled đăng ký lại ở encounter
mới có thể nhận thưởng bình thường. Kill thực sự mới sau farm/Retry/Load được
phép thưởng theo D3; callback đời cũ, quái thoát và cleanup không được thưởng.

## 3. Failure và persistence

Gear hiện **không có cap tổng số món**. Phân trang 10/20 ô không phải giới hạn
kho. A chỉ sinh gear mẫu, không có stack reward hay profile arbitrary item.

Nếu validate/factory/AddInstance thất bại: receipt đã terminal, không reroll hoặc
thử GUID khác, trả FAILED với run/encounter/life/spawn và lý do; Console ghi error,
event log báo lỗi, service Faulted tắt phát tiếp cho CombatManager hiện tại.
Tắt/bật cờ hoặc sang encounter mới không tự xóa Faulted. Sửa nguyên nhân và khởi
động lại Play Mode. Đây là **FAIL nghiệm thu**, không phải nhánh “mất đồ bình thường”.
Không auto-repair/initialize HeroData hoặc thêm queue pending trong save.

Không sửa SaveManager hoặc EntityDataSO/schema. Item nằm trong equipmentItems
và được save nguyên ID/stat theo cơ chế hiện hành. Receipt không serialized.
Load chỉ restore; không có hook tái tạo loot. Manual rollback cũ bỏ reward sau
snapshot cùng progression; không merge đồ mới vào save cũ. Crash trước lần save
thành công có thể mất loot chưa lưu. Không tuyên bố durable exactly-once xuyên
crash hoặc rollback. Wave escape replay có thể sinh đời quái mới và nhận thêm
loot từ kill mới; loot kill hợp lệ trước escape không bị thu hồi.

## 4. File thay đổi và lý do

- `Assets/Script/CombatLootRewards.cs` + `.meta`: service A, context/receipts,
  validation, insertion, failure quarantine. Factory injection chỉ dùng để kiểm
  thử failure/re-entry; production constructor gọi CreateDemoSword.
- `Assets/Script/CombatManager.cs`: captured keys, accepted-lethal guard,
  claim/complete trước pool cleanup; invalidate/re-arm có validation.
- `Assets/Script/GameManager.cs`: một cờ serialized dev Off mặc định, compile gate,
  hai lời gọi suspend/resume context khi manager disable/enable.
- `Tests~/CombatLootRegression.cs`, `Tests~/MotionRegression.cs`, `Tests~/run.sh`:
  group focused không I/O save, thêm service vào danh sách compile.
- Bốn fixture cũ `BalanceRegression`, `MagicPersistenceRegression`, `MenuRegression`,
  `Patch54bRegression` thay các synthetic death trên target còn sống bằng helper
  lethal damage. Giữ nguyên assertion và kỳ vọng; direct death callback không
  còn là cách chế tạo kill hợp lệ. Các ca có save I/O không chạy trong Cloud.
- `Assets/Tests/Presentation/PlayMode/CombatLootTests.cs` + `.meta`: sáu ca native
  service/JSON/OnEnable, chờ chạy Unity. Tài liệu này ghi phạm vi và nghiệm thu.

Không đổi Inventory comparison/HUD, scene/prefab/asset hiện có, rarity/affix/rèn,
SaveManager, save schema, combat balance, B2 persistence, camera/movement/wave/VFX.

## 5. Kiểm thử thực chạy và giới hạn

```bash
bash Tests~/run.sh --combat-loot
bash Tests~/run.sh --inventory-compare
python .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json --max-issues 0
python Tools/verify_agent_skills.py
git diff --check
```

Compiler Mono được giải nén riêng trong `/tmp` vì môi trường không có mcs/mono;
không thêm dependency vào repository. Phiên này đặt PATH tới compiler tạm; máy
đã có Mono chạy hai lệnh runner như trên.

**Mono/UnityStub:** nhóm loot **41 PASS** (17 ca mới + 6 balance + 18 projectile
baseline), nhóm inventory/compare **18 PASS**. Tổng **59 PASS** ở hai nhóm.
Compiler có 16 warning cũ của runner/Inventory/TransparentWindow, không lỗi.

| Contract | Bằng chứng headless thực chạy |
|---|---|
| T01–T04 | Primary/B2 kill, primary fallen, đúng item/recipient/EXP, positive hoặc fake zero-HP death bị chặn; duplicate/re-entry; pool reuse GUID mới |
| T05–T09 | Projectile cũ, tail không damage, valid AoE 2 victims, clear/escape giữ loot đã cấp; final kill trước wave progress, encounter mới có thể farm; map/ref/disable/resume invalidation |
| T10–T11 | JSON snapshot **trong bộ nhớ** restore ID, normalize không reroll; rollback snapshot cũ; context primary mới, restore không dispatch kill, new life nhận bình thường |
| T12 | Validator từ chối candidate future-version mà không publish/invalid live context; native JSON probe có test riêng nhưng chưa chạy |
| T13–T14 | 25 gear vượt page size; stack 9999 không chặn gear; duplicate item ID, invalid item, null list, unsupported schema, no reroll + quarantine + diagnostic |
| T15 | Dirty qua Add; Off/duplicate không dirty do loot; SaveGame từ chối future version **trước I/O**, giữ dirty và không phát thêm item |
| T16/T18 | Off/On; item properties; paired Unity RNG/EXP/heal/spawn outputs không đổi; baseline damage/accuracy/magic/healing regression vẫn PASS |
| T19 bổ sung | Kill → item → Inventory → Compare → Equip, total thực khớp Preview và chỉ đổi sau Equip |

**Không chạy full suite** vì chứa save I/O và các baseline khác ngoài task.
Không gọi T10–T12/T15 headless là nghiệm thu filesystem/SaveManager end-to-end:
không chạy SaveGame với đường dẫn thực, LoadGame/TryLoadSlot qua file hay lỗi
quyền ghi/disk. Test refusal SaveGame không Awake, dùng future schema nên trả
false trước khi đọc/ghi file; không dùng save người chơi.

**Native tests mới: 6 ca đã viết, chưa compile/chạy Unity Editor hoặc Windows.**
Bao gồm Off/On và RNG, JSON snapshot bằng JsonUtility thật, future JSON probe,
failure terminal, OnEnable thật của monster khi tái kích hoạt. Fixture native
không tạo SaveManager, không I/O, chỉ cleanup object sở hữu; ca pool yêu cầu scene
sạch. Các ca service không thay thế nghiệm thu combat/gameplay manager native.

GUID checker: **0 ERROR, 62 WARNING** (baseline cũ: package refs chưa xác nhận vì
thiếu PackageCache, ba missing-meta folder và một orphan-meta). Không sửa chúng.
Skills verifier và diff check PASS.

## 6. Checklist Unity Play Mode / Windows

Dùng profile test riêng; không xóa/sửa save người chơi để nghiệm thu. Đối với
SaveLoad I/O, tuân thủ fixture [README](../Assets/Tests/SaveLoad/PlayMode/README.md)
và [Edit Mode](../Assets/Tests/SaveLoad/EditMode/README.md): directory trước Awake,
marker/ownership, cleanup giới hạn; chạy trên máy Unity, không Cloud.

1. Import/compile ở Unity **6000.5.0f1**. Chạy `CombatLootTests` trong Play Mode
   Test Runner, scene sạch cho ca pool; ghi SHA, version và kết quả riêng.
2. MainGame, New/Continue profile test; để cờ Off, hạ một quái: không thêm gear.
3. Bật Controlled Loot Test; ghi số gear trước, hạ một quái: tăng đúng một kiếm
   Common level 1/refine 0/+10, chưa equipped, có event `[LOOT TEST A]`.
4. Mở Kho đồ → chọn kiếm mới → So sánh. Chỉ xem không đổi HP/current stats.
   Equip kiếm: ATK cận chiến tăng đúng Preview, số instance không tăng; đổi thế
   kiểm tag melee theo luật cũ. Tháo trả lại hiệu ứng như trước, không mất item.
5. Gọi B2; B2 kết liễu (thử cả primary đã chết nhưng ally còn sống): loot vào
   inventory chính, EXP vẫn về killer. Không tự tạo party save.
6. Kiểm kill cuối wave và Magic AoE nhiều quái; không có reward thứ hai từ tail,
   duplicate impact, quái thoát, hủy wave. Farm cùng map nhiều vòng vẫn có loot
   từ những đời quái mới; replay không thu hồi loot của kill hợp lệ trước escape.
7. Khi projectile đang bay, thử travel, defeat/Retry và manual Load fixture;
   callback đời/map cũ không đổi bag/EXP/wave. Entry/VFX/camera giữ hành vi cũ.
8. Save snapshot có loot, nhận thêm đồ, Load snapshot: đúng item IDs lúc save,
   không nhân bản/reroll/merge bag; Load save trước loot thì đồ mới bị rollback.
   Quit/relaunch từ snapshot saved, không phát reward chỉ vì Continue.
9. Thử bật/tắt cờ giữa hai kill cùng wave; Off không phát, On chỉ phát cho kill
   hợp lệ mới. Disable/re-enable manager qua fixture: validate đời sống, bỏ
   projectile cũ, không nhận thêm đồ chỉ vì enable. Lỗi insertion có thể tiêm
   bằng fixture: phải báo error/quarantine, không thử lại GUID mới.
10. Thử 600/800/1150, expanded/fallback Inventory, chọn/compare/equip/close/pause
    và Windows native pointer. Không cần sửa HUD trong task này.

Build Player để kiểm thử loot phải là **Development Build** với flag được bật
chủ động trong cấu hình thử; build thường không bật được profile A. Ghi native
PASS/FAIL riêng. Hiệu năng khi gear tích lũy lâu, rendering/input và persistence
qua filesystem/crash chưa được nghiệm thu trong Cloud; A không phải loot
progression chính thức hoặc cấu hình farm dài hạn đã cân bằng.
