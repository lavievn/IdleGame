# Boss coordinator → World-map encounter: ghi chú v0.5

06/11/2026. **IMPLEMENTED:** gate/coordinator thuần. **PROPOSED/BLOCKED:** production encounter, reward persistence và mở boss.

## Mã thực và authority

[BossEncounterGate / BossEncounterCoordinator](../../Assets/Script/BossEncounterCoordinator.cs) không đọc Resources, không nhận Hero và không gọi manager. Gate nhận snapshot WorldMapNode hiện có, copy IDs/flags/links; coordinator chỉ giữ permit, encounter/attempt identity và state.

WorldMapCatalog vẫn là authority 133 node/sáu flag boss; `TravelBlockReason` production vẫn chặn cả sáu. SuggestedLevel120 không được dùng làm unlock policy mới. GameManager vẫn authority normal entry/wave/travel và Hero canonical; B01 chưa có caller ở đó.

Input gate: origin/target ID, current-node-cleared, definition ID/revision/node, permission và readiness từ caller. Gate kiểm known node→boss flag→normal origin→adjacency→clear→definition→permission→readiness và trả reason/permit. Permission/cleared/ready là assertions do adapter tương lai phải lấy từ authority thật, không phải quyền để UI tự set true. Permit là snapshot, không tự cập nhật khi player/definition thay đổi.

## State/API và entry points dự kiến

```text
Idle --TryEnter--> Entry --TryStartFighting--> Fighting --TryResolve--> Resolving
Entry/Fighting/Resolving --TryCancel--> Cancelled
Resolving --TryCompleteResolution(explicit acknowledgement)--> Won hoặc Lost
Lost/Cancelled --TryRetry(explicit authorization + fresh gate)--> Entry (attempt ID mới)
```

Mọi callback mang encounterId/attemptId; ID rỗng, phase sai, stale callback hoặc duplicate terminal bị từ chối. Retry giữ encounter, cần attempt mới chưa dùng và cùng boss definition/revision; cần policy explicit và gate được caller đánh giá lại. Won không tự cho repeat/Retry. `TerminalTransitionCount` là bộ đếm logic in-memory, **không phải số lần kinh tế đã commit**.

1. `GameManager.TryTravelToWorldMap`: adapter tương lai dispatch normal/boss sau validation, re-evaluate gate ngay trước entry. Không chỉ xóa dòng chặn boss rồi gọi normal TryTravel.
2. Entry boss phải cancel normal exploration/next-wave/targets trước spawn; tái sử dụng GameManager entry/VFX và CombatManager life/projectile guards, không đổi camera.
3. Accepted boss terminal: dispatch encounter kind trước normal `HandleMonsterDeathBy` cấp EXP/loot và `OnMonsterDied` tăng wave. Normal reward path hiện chạy trước wave notification, nên gắn thêm reward ở cuối sẽ cấp hai lần.
4. Resolve lập candidate clear+EXP/items+durable ledger. Chỉ sau transaction đã xác minh mới gọi TryCompleteResolution(..., true). Trong tests, true là fixture acknowledgement; không chứng minh Save đã commit.
5. Exit/travel/Retry/Load cần cancel đời callback cũ, rebind Hero sau swap. Không dùng Won để gọi `WorldMapCatalog.CompleteWave` năm lần hoặc auto travel.

## Save/migration và nguy cơ mất dữ liệu

Tái sử dụng WorldMapCatalog IDs/links, GameManager candidate swap/lifecycle, CombatManager accepted damage/death, CombatLootRewards validations và SaveManager writer. Receipt của CombatLootRewards chỉ runtime; usedAttempts của coordinator cũng chỉ trong instance. Cả hai không chống duplicate qua restart/manual Load.

Khi được duyệt, component version/encounter lineage/completion/reward transaction phải nằm cùng snapshot với clear/items/EXP. Pending outcome giữ cố định item IDs/rolls/growth, không reroll khi write lỗi. Load legacy không có ledger không được suy sáu boss đã clear từ metadata. Future-version guard cần riêng; equipment guard không bao phủ boss.

Rủi ro cần xử lý trước release: normal wave scheduler vẫn chạy trong boss; phase death được tính normal reward; crash giữa grant/clear/save; ambiguous disk commit rồi Retry; Load slot cũ rollback timeline; cancellation khi write đã commit nhưng chưa acknowledge. Coordinator v0.5 không thể quyết định các vấn đề kinh tế này. Adapter không được cancel/retry Resolving đã có commit không xác định trước khi reconcile transaction.

## Test và quyết định còn lại

Đã chạy Mono/UnityStub trên catalog thật: đúng sáu boss IDs và normal nodes; missing spec/adjacency/clear/permission/readiness; entry/fight/resolve/ack; duplicate win/loss, stale encounter/attempt, cancel/Retry, snapshot links/flags. Production travel gate vẫn blocked và catalog không đổi.

Trước release cần: một boss definition/content được duyệt, actual spawn/lifecycle, normal scheduler isolation, victory/failure/exit/Retry, candidate transaction fault injection trước/sau Replace/swap, old/current/future/downgrade, manual rollback, SaveLoad và native acceptance. Không chạy Player/Play Mode chỉ để chứng minh B01 pure.

Chờ người dùng: unlock cho từng node, spec/phase/difficulty, failure/HP/cost/exit/Retry, first-clear/repeat/reward/EXP/cap, checkpoint/slot/Load-mid-attempt/downgrade. Không tự mở boss node hoặc tạo prefab từ ghi chú này.
