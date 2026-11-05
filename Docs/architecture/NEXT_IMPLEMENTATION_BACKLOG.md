# Backlog triển khai sau v0.4

## Phạm vi và cách giao việc

**PROPOSED — chưa phê duyệt để tự triển khai.** Cập nhật 06/11/2026. Đây là kết quả B4, không mở tiếp gameplay sau khi batch v0.4 kết thúc. Nguồn: [realm](REALM_PROGRESSION_CONTRACT.md), [võ học/công pháp](MARTIAL_ARTS_CONTRACT.md), [boss](BOSS_ENCOUNTER_CONTRACT.md), mã đến `0b66288`.

“Có thể giao độc lập” nghĩa là phạm vi/deliverable/test riêng, không nghĩa bỏ qua dependency. Task ghi “thuần” có thể được giao riêng với fake data và không Save/runtime integration. Mọi task production có quyết định chưa duyệt giữ BLOCKED. Chỉ triển khai khi có nhiệm vụ tiếp theo. Không thay camera v0.3, balance/drop table, HP theo mode hoặc schema ngoài task được giao.

## Nghiệm thu nền hiện hữu

### N01 — Nghiệm thu HUD bằng chuột thật

- **File/hệ thống:** Windows Player, UIManager, InventoryPanelUI, GameManager; không sửa mã mặc định.
- **Tái sử dụng:** Player/profile thử có marker và routing verifier; v0.3/v0.4 đã có bằng chứng Editor.
- **Thay đổi dự kiến:** chỉ biên bản/ảnh/log từ chuột thật. Người dùng mở đúng profile thử, chọn New/Confirm rồi Character → phân một điểm → Inventory → equip/unequip → Forge → Manual Save/Load → Pause/Resume → Travel hợp lệ → defeat/Retry. Kiểm modal không click xuống dưới và resize lúc loot bay nếu có thể quan sát.
- **Phụ thuộc:** công cụ targeting hợp lệ hoặc người dùng trực tiếp thao tác; không đổi Firewall/quyền/window injection.
- **Rủi ro Save/Combat:** dùng profile cô lập xác minh trước Awake/launch; không slot cá nhân, không direct handler để gọi native PASS.
- **PASS:** từng thao tác OS-native có ảnh trước/sau và mutation đúng; không có input thì ghi BLOCKED/NOT RUN từng cạnh.
- **Cần quyết định:** người thực hiện tương tác, thời điểm nghiệm thu; không chặn các task thiết kế độc lập.

## Cảnh giới — triển khai thành các task tách biệt

### R01 — Evaluator cảnh giới thuần

- **File/hệ thống:** file evaluator mới ngoài runtime tích hợp và tests; tham chiếu EntityDataSO/RankDatabase nhưng không sửa chúng.
- **Tái sử dụng:** Level tổng, quy ước stage đã chốt; examples trong realm contract.
- **Thay đổi dự kiến:** hàm `(realmId, entryLevel, totalLevel) -> stage/bottleneck/reason`, không dùng modulo hoặc tự successor.
- **Phụ thuộc:** duyệt quy ước 1-based R1; fixture catalog tối thiểu chỉ trong tests, không production content.
- **Rủi ro Save/Combat:** không I/O/mutation; không nhận RankDatabase legacy làm authority mới.
- **PASS:** ranh giới 1/9/10/19/20/29/30/31, Level45 chưa break, break45→entry45, invalid E/L và cuối catalog có kết quả rõ.
- **Cần quyết định:** convention 1-based, catalog/successor và Level cap. Có thể giao pure tests sau khi duyệt convention, chưa triển khai trial.

### R02 — Realm persistence và migration riêng

- **File/hệ thống:** EntityDataSO, SaveManager component probe, GameManager.TryLoadSlot; tests SaveLoad hiện có.
- **Tái sử dụng:** candidate-load-before-swap, legacy field resets, owned GUID fixtures và file writer.
- **Thay đổi dự kiến:** component version/realmId/entryLevel đã duyệt; validate trước mutation; migration legacy explicit; derive stage từ R01.
- **Phụ thuộc:** R01 và realm R5, downgrade policy. Không dựa vào equipment guard cho field mới.
- **Rủi ro Save/Combat:** mất state từ template/old executable; không tặng/free-break realm từ Level tổng; không nhân growth/modifiers khi Load.
- **PASS:** old/current/future/unknown ID/bad entry, template có field dư, roundtrip và Load thất bại giữ canonical Hero/file cũ nguyên vẹn.
- **Cần quyết định:** migration Level cao, Level999/cảnh giới cuối, hỗ trợ đọc/ghi bởi bản executable cũ.

### R03 — Coordinator Thiên Kiếp hai pha

- **File/hệ thống:** GameManager entry/cancel/death/Retry, CombatManager adapter, HeroCombatState; module coordinator hẹp mới.
- **Tái sử dụng:** actor life/encounter identity, pause, projectile clear, accepted damage; không viết lại combat/camera.
- **Thay đổi dự kiến:** phase machine, attemptId, eligibility, terminal result và commit realm một lần; nối persistence sau R02.
- **Phụ thuộc:** R01/R02; duyệt realm R2/R3/R4 và nội dung thử thách. Có thể kiểm phase machine bằng fake events trước nội dung, không gọi đó gameplay PASS.
- **Rủi ro Save/Combat:** hai scheduler, callback stale, save nửa kết quả, cost/refund hoặc HP sai sau Retry.
- **PASS:** không skip pha; pause/cancel/Load/death cùng frame một kết quả; duplicate success không tăng realm lần hai; L không reset, E lấy Level lúc commit; I/O fail không báo success.
- **Cần quyết định:** threshold/time/cost/probability/buff, failure/Retry/Load policy; giữ BLOCKED production đến khi đủ.

## Công pháp và võ học

### M01 — Definition registry và eligibility thuần

- **File/hệ thống:** catalog/validator mới, WeaponBasicAttack.Family, SynergyMath (chỉ đọc); tests pure.
- **Tái sử dụng:** stable template IDs, weapon family và root profile; không dùng tên class/profession.
- **Thay đổi dự kiến:** definition ID/revision, loại công pháp/võ học, Passive/Active, compatibility/root policy và reason codes.
- **Phụ thuộc:** duyệt vocabulary/validation của martial contract; không cần realm hoặc boss runtime.
- **Rủi ro Save/Combat:** không I/O hoặc damage; tránh registry IDs trùng/tái sử dụng.
- **PASS:** family Sword/Bow/Staff/Unarmed, root eligibility, missing/duplicate definitions; đổi weapon giữ learned fixture, kết quả ineligible rõ.
- **Cần quyết định:** learning/upgrade rules, root policy cụ thể và nội dung; chưa tạo các ví dụ proposal thành production asset.

### M02 — Learned state và loadout persistence

- **File/hệ thống:** EntityDataSO, SaveManager component guards, GameManager command layer; Save tests.
- **Tái sử dụng:** manager canonical data, component versions và safe candidate load; không đổi LearnedSupportSkill/recipe.
- **Thay đổi dự kiến:** learned IDs/rank, loadout IDs và component version; validate-before-mutate; old save empty, future fail closed.
- **Phụ thuộc:** M01, slot capacity/selection policy, unknown definition và downgrade decisions.
- **Rủi ro Save/Combat:** duplicate learned/slot, ID mất, old template thừa state; Load không được phát effect hoặc cấp lại cost/reward.
- **PASS:** old/current/future, bad rank/ID/duplicate slot, failed command không mutation; đổi vũ khí không mất learned/rank; roundtrip đúng selection.
- **Cần quyết định:** số slot, upgrade/cost, unknown-ID preserve/refuse; chưa thêm mana/cooldown.

### M03 — Passive rebuild có ownership

- **File/hệ thống:** CombatStats modifier consumers, GameManager refresh/Load/equipment; module passive bridge mới.
- **Tái sử dụng:** sourceId/requiredTags/blockedTags và refresh HP hiện tại.
- **Thay đổi dự kiến:** derive modifiers từ selected/eligible definitions, chỉ thay own prefixes; UI lý do không đủ điều kiện.
- **Phụ thuộc:** M01/M02 và một passive cụ thể được duyệt thông số.
- **Rủi ro Save/Combat:** double modifiers, xóa equipment modifiers, vô tình hồi HP khi rebuild hoặc buff root hai lần.
- **PASS:** N lần rebuild/equip/Load có đúng một modifier nguồn, giữ nguồn khác; mất compatibility tắt hiệu lực, đổi lại bật đúng một lần; preview/actual khớp.
- **Cần quyết định:** passive nào và stat/magnitude/HP refresh policy; không dùng công pháp mẫu chưa duyệt.

### M04 — Damage adapter và activation thuần, chưa nối gameplay

- **File/hệ thống:** module snapshot/activation pure mới; CombatDamage, SynergyMath, WeaponBasicAttack và fixture tests.
- **Tái sử dụng:** stat evaluator, root pair table, school/element defense, định danh life/target.
- **Thay đổi dự kiến:** Validate→Windup→Release→Impact/Cancel; source stat explicit từng part, offensive snapshot và root đúng một lần; adapter không resolve defense lặp.
- **Phụ thuộc:** M01, duyệt pipeline/rounding/weapon swap/ambush policy. Không cần M02 nếu chỉ fake learned data và không I/O.
- **Rủi ro Save/Combat:** Physical thay Magical, root bình phương, mỗi part phát death, stale target; chưa sửa CombatManager.
- **PASS:** Magical0 không tạo Physical damage; neutral/counter roots; multi-part một accepted HP mutation; duplicate/stale/cancel/pause có kết quả đo được.
- **Cần quyết định:** root profile/element, rounding, effect concurrency và resource/timing nếu có; chưa sinh mana/CD.

### M05 — Tích hợp một active đã duyệt

- **File/hệ thống:** CombatManager accepted impact/projectiles, HeroCombatState, GameManager lifecycle; UI/loadout chỉ phần dùng được.
- **Tái sử dụng:** M04, existing actor life/version/cancel và một death/reward path.
- **Thay đổi dự kiến:** adapter nhỏ cho một effect cụ thể; hủy trên Load/New/Travel/Retry/Menu; không scheduler damage song song.
- **Phụ thuộc:** M02/M04, M03 nếu active phụ thuộc passive, nội dung và policy hoàn chỉnh; regression basic trước/ sau.
- **Rủi ro Save/Combat:** duplicate release/death/loot, đổi weapon bypass interval, projectile dùng stats mới, stale canonical Hero.
- **PASS:** một activation/hit đúng một lần; thực Unity Play Mode lifecycle; SaveLoad không phục hồi reference cũ; basic/camera/loot giữ hành vi nền.
- **Cần quyết định:** active được chọn, trigger, resource/cooldown/cost nếu cần; production BLOCKED khi chưa duyệt.

## Boss và nền giao dịch thưởng

### B01 — Boss gate/coordinator thuần với fake events

- **File/hệ thống:** module validator/coordinator mới; WorldMapCatalog chỉ đọc; tests pure.
- **Tái sử dụng:** sáu node IDs, adjacency, normal clear, phase/lifetime identity pattern.
- **Thay đổi dự kiến:** reason codes và Entry/Fighting/Resolving/terminal với fake definition; giữ gate production đóng.
- **Phụ thuộc:** duyệt hợp đồng kỹ thuật; không cần R03/M05. Realm condition chỉ interface optional nếu sản phẩm chọn, không dependency ngầm.
- **Rủi ro Save/Combat:** không I/O/spawn/reward thật; tránh lẫn boss với normal 5-wave clear.
- **PASS:** sáu node nhận diện đúng, missing spec fail, invalid/nonadjacent/not-cleared không mutation; duplicate terminal/cancel stale không emit reward hai lần.
- **Cần quyết định:** boss unlock/phase/exit/retry; pure fake tests không giải quyết nội dung production.

### B02 — Candidate reward transaction và fault injection

- **File/hệ thống:** SaveManager writer/validation, GameManager candidate swap, reward planning module mới; owned Save fixtures.
- **Tái sử dụng:** AddInstance/cap/version validators, snapshot load/swap, temp/Replace writer. CombatLootRewards receipt giữ vai trò runtime, không giả durable ledger.
- **Thay đổi dự kiến:** transactionId/outcome plan chứa EXP/growth/items/clear/ledger, ghi cùng snapshot rồi publish; retry write dùng cùng IDs/rolls; I/O ambiguous phải verify file.
- **Phụ thuộc:** boss B3/B4 về first-clear/repeat/reward/cap/checkpoint/rollback, schema và downgrade plan; không cần prefab boss.
- **Rủi ro Save/Combat:** mất hoặc nhân thưởng khi crash, disk full, swap fail, re-roll growth/gear; không stage grant trên live Hero trước save.
- **PASS:** fault injection trước/sau temp/Replace/swap; Load khớp ledger+clear+items+EXP, không reroll hoặc cấp hai lần; future schema không ghi đè; manual rollback được mô tả đúng timeline.
- **Cần quyết định:** slot/checkpoint, all-or-nothing hay partial branch, overflow/invalid gear, lineage và repeat identity. BLOCKED production nếu còn thiếu.

### B03 — Một boss encounter hoàn chỉnh sau duyệt nội dung

- **File/hệ thống:** GameManager encounter dispatch/entry/wave/death/travel, CombatManager spawn/accepted terminal, WorldMap gate và boss definition được duyệt.
- **Tái sử dụng:** B01/B02, pooling/lifetime/pause/cancel, hiện trạng world topology; không viết lại CombatManager.
- **Thay đổi dự kiến:** nối một boss trước, khóa normal scheduler, dispatch reward trước đường EXP/loot thường, clear và exit thủ công; chỉ mở node được chỉ định sau validation.
- **Phụ thuộc:** B01/B02, boss B1/B2 nội dung/failure/exit; không phụ thuộc võ học hoặc realm trừ điều kiện được chốt rõ.
- **Rủi ro Save/Combat:** normal loot cộng boss loot hai lần, phase kill bị tính wave, stale projectile, gate mở khi thiếu prefab/spec.
- **PASS:** Unity isolated full entry→fight→win/fail→Retry→SaveLoad→travel, crash recovery reward; node khác vẫn chặn, 133-node topology không đổi và không auto travel.
- **Cần quyết định:** chọn node/spec/prefab/reward và gate; task này được phép tạo nội dung chỉ khi nhiệm vụ mới ghi rõ, v0.4 không tạo.

## Thứ tự khả thi và trạng thái bàn giao

Các nhánh độc lập: `R01→R02→R03`; `M01→M02→M03` và `M01→M04→M05` (M05 cũng cần M02); `B01 + B02→B03`. N01 độc lập với ba nhánh thiết kế. Nếu nhiều nhánh cùng sửa SaveManager/EntityDataSO phải tích hợp tuần tự trên HEAD đã kiểm; không cho nhiều tác vụ đồng thời ghi cùng worktree.

Trước task production đầu tiên: duyệt các quyết định riêng của task, xác minh branch/HEAD/status không có writer khác, chốt diff cho phép và criteria. Task pure không cần chạy Unity. Task đổi runtime chọn regression trực tiếp và nghiệm thu Unity đúng phần; không chạy lại toàn bộ bộ test đã đạt vì số lượng PASS. Bàn giao commit nhỏ, `git diff --check`, nêu IMPLEMENTED/VERIFIED/PROPOSED/BLOCKED/NOT RUN riêng.

**Không có task R/M/B nào trong danh sách này được triển khai tự động ở batch v0.4.**
