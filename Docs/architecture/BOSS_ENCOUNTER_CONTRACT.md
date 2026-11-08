# Kiến trúc boss encounter

## Trạng thái và phạm vi

**PROPOSED — thiết kế v0.4, chưa có boss gameplay được tích hợp.** Đối chiếu 05–06/11/2026 tại `ba86368` và các thay đổi hẹp v0.4. Không tạo prefab/nội dung boss, không mở gate, không sửa world-map, reward table hoặc Save schema.

## Sáu node boss đã có metadata

Đếm trực tiếp cột `bossFlag` của [WorldMapCatalog.txt](../../Assets/Resources/WorldMapCatalog.txt): 133 node, sáu flag `B`. Chúng là node có thật trong catalog, chưa là encounter đã chơi được.

| ID | Node | Vùng |
|---|---|---|
| TV-39 | Trầm Thủy Uyên | Tử Vân Cổ Vực |
| TL-02 | Ngân Hà Sơn Mạch | Thương Lam Trung Vực |
| XD-03 | Lôi Nham Tâm Sơn | Xích Diễm Chi Địa |
| HT-01 | Tiền Đồn Phong Nham | Hoàng Thổ Nguyên Dã |
| HT-27 | Lưu Hoang Cốc | Hoàng Thổ Nguyên Dã |
| TM-25 | Hải Trúc Đàm | Thanh Mộc U Cảnh |

[WorldMapCatalog](../../Assets/Script/WorldMapCatalog.cs) tạo boss thành leaf có đường nối tới node thường, metadata SuggestedLevel=120. Đây là dữ liệu prototype, không phải ngưỡng mở boss đã được chốt. `TravelBlockReason` kiểm node hợp lệ/kề/clear node hiện tại rồi luôn chặn `target.IsBoss` với lý do chưa chốt vật phẩm/cảnh giới. Giữ nguyên gate này trong v0.4.

## Các điểm tích hợp hiện có

| Hệ thống | Tái sử dụng được | Khoảng thiếu |
|---|---|---|
| `WorldMapCatalog.TravelBlockReason/TryTravel` | ID, adjacency, visited/cleared, lựa chọn travel thủ công | Boss gate riêng; không lấy clear 5 wave làm hoàn thành boss. |
| `GameManager.TryTravelToWorldMap`, entry, `CallNextWave`, `OnMonsterDied` | Một authority điều phối Hero, clear/cancel, entry/VFX, Save triggers | Chưa có encounter kind hoặc coordinator boss; normal wave scheduler không được chạy đồng thời. |
| `CombatManager.StartBattle`, accepted death, `HeroCombatState` | Actor lifetime, accepted damage, projectile cleanup, pause và một đường death | Chưa có spawn spec/lifecycle boss. Normal death hiện cấp EXP, roll loot rồi gọi OnMonsterDied; không thể gắn thêm reward boss sau đó mà bỏ qua đường này. |
| `CombatLootRewards` | Receipt `(run, encounter, life, spawn)`, terminal claim, hai stream và item instance validation | Receipt không lưu qua Save/crash. Không là boss reward ledger. |
| `SaveManager.SaveGame/LoadGame`, `GameManager.TryLoadSlot` | Ghi slot qua temp/Replace/Move; load candidate riêng trước swap | Chưa có persisted encounter ID, reward outcome hoặc transaction clear+reward. File replacement không tự biến runtime mutations thành transaction. |

## Điều kiện mở boss — đề xuất, chưa mở gate

Một `CanEnterBoss` thuần dữ liệu trả kết quả có reason code: node tồn tại và IsBoss, node kề hợp lệ, current normal node đã clear, không pending confirmation/entry/battle transition, Hero hợp lệ và các điều kiện sản phẩm đã duyệt. Điều kiện Level/realm/item/quest/cost để trống đến khi được duyệt. Không suy threshold 120 từ SuggestedLevel hoặc gate realm mới chưa có runtime.

Gate mở nội dung phải độc lập với catalog flag: chỉ node có EncounterDefinition được phê duyệt và build nội dung đủ mới có thể vào. Định nghĩa thiếu vẫn chặn; không fallback gọi StartBattle với quái thường rồi ghi boss clear. Travel vào boss bị từ chối phải không đổi node, inventory, clear history hay HP.

## State và vòng đời đề xuất

Definition bền dùng `encounterDefinitionId`, revision, `bossNodeId`, các phase/actor role đã duyệt và reward policy ID. Node ID vẫn giữ nguyên, không thêm/sửa topology 133 node. Definition revision không thay đổi nghĩa lịch sử reward/clear cũ.

Runtime coordinator giữ `attemptId`, originNodeId, Hero life/encounter generation, phase, các boss lives hợp lệ và terminal result. Nếu cần persistence, state durable tối thiểu là component version, completed records và reward transaction record theo policy; chưa thêm field nào.

```text
NormalExploration -> EntryValidated -> Entering -> Fighting -> Resolving
                         |                |          |           |
                         +----------------+----------+-> Failed/Cancelled
                                                          Resolving -> Committed -> ExitReady
```

1. EntryValidated kiểm lại gate và canonical owner; xác định origin/definition/attempt. Chưa mutation thì có thể fail an toàn.
2. Entering dùng GameManager khóa scheduler wave thường, cancel target/projectile/entry cũ, dựng boss theo spec được duyệt. Không ghi “clear” khi spawn hoặc khi animation kết thúc.
3. Fighting tái sử dụng damage/defense/awareness/life checks thích hợp. Một boss có nhiều phase phải có identity phase và terminal condition; phase transition không được phát “monster death” thông thường để farm thêm EXP/loot.
4. Accepted terminal kill chuyển Resolving đúng một lần. Vô hiệu incoming callback đời cũ, dừng ordinary wave/exploration; hoàn tất toàn bộ điều kiện encounter trước lập reward/clear transaction.
5. Chỉ sau commit bền thành công mới công bố clear và mở ExitReady/travel sau thắng. Presentation thất bại không cấp lại reward.

Pause dừng game-time; Load/Retry/Travel/New/Menu/owner death đổi generation và dọn mọi reference/projectile. Không để coroutine normal wave đang chờ gọi quái trong boss. Không thay camera v0.3; mọi camera boss khác phải có nhiệm vụ riêng sau.

## Thất bại, Retry và entry/exit

Đề xuất failure kết thúc attempt, không clear hoặc cấp reward chiến thắng; learned/Level/gear không bị wipe. Nhưng HP phục hồi bao nhiêu, ở lại boss hay về origin, có mất cost không, Retry làm lại từ phase nào, có giữ damage/phase progress không đều **chờ duyệt**. Không tự gọi `RestartAfterDefeat` normal rồi coi đó là luật boss.

Exit trước thắng/cancel cần policy riêng, kiểm canonical owner và dọn actor trước rời. Sau thắng, đề xuất người chơi tự chọn quay lại origin node kề; không auto travel, không tự nối tới một node xa vì boss leaf. Boss node không được chuyển thành farm 5 wave bằng `WorldMapCatalog.CompleteWave`. `worldMapCleared` và boss completion chỉ được đồng bộ trong commit đã định nghĩa, không để hai authority mâu thuẫn.

Load giữa attempt đề xuất restart/cancel từ checkpoint trước entry, không phục hồi live projectile/HP/timer. Đây là policy cần duyệt; nếu chọn resume chính xác, phạm vi schema và tests tăng, phải thiết kế lại trước triển khai. Save khi Resolving phải được coordinator quản lý; AutoSave thông thường không ghi một nửa clear/reward.

## Reward và chống cấp trùng — yêu cầu kỹ thuật đề xuất

Trước khi bật boss thật cần chốt: một lần theo node hay có repeat; first-clear/repeat rewards; EXP attribution; material/gear theo bảng nào; thất bại một nhánh xử lý ra sao; có nhận phần thành công giống normal loot hay transaction all-or-nothing. Không thêm item/recipe/currency hoặc thay bảng .60/.08 trong v0.4.

Normal reward path hiện đã cấp EXP trước `CompleteProduction`. Adapter boss phải dispatch theo encounter kind trước mutation thưởng/EXP thường, rồi đi một authority reward được duyệt. Không vừa để normal path cấp thưởng vừa thêm boss reward. Không dùng 5 callback wave hoặc animation receipt làm boss reward identity.

Khóa idempotency đề xuất: `(run/save-lineage ID, bossNodeId, completionOrdinal)` cho completion; `attemptId` phân biệt các lần thử. Với policy one-time, ordinal không tăng mỗi Retry; với repeat phải định nghĩa lúc bắt đầu chu kỳ mới. ID không dựa vào pooled GameObject hoặc tên boss.

Giao dịch đề xuất trên snapshot riêng:

1. Khi terminal result hợp lệ, tạo outcome kế hoạch **một lần**: EXP, material, gear với instance IDs cố định, completion record, reward status. Không roll lại khi disk lỗi/callback trùng. Pending plan giữ cùng transactionId và payload.
2. Validate candidate Hero clone: capacity, item IDs/versions, clear history, ledger và owner trước khi commit. Không mutate Hero live rồi mong Save rollback; không gọi `AddExp` trên live trước transaction. Nếu clone dùng growth RNG, roll chỉ một lần trong pending plan/candidate, không reroll mỗi retry ghi đĩa.
3. Ghi candidate chứa **cùng lúc** EXP/inventory/gear/clear/ledger vào slot được policy duyệt. Write thành công mới swap canonical Hero và publish UI. Đường swap cần rebind combat/UI đúng GameManager, không âm thầm thay data reference đang đánh.
4. Save lỗi rõ ràng: live state chưa nhận thưởng/clear; giữ Resolving và plan, báo lỗi, không retry cấp thưởng. Kết quả I/O không xác định sau Replace phải đọc/xác minh transactionId+payload từ slot thuộc sở hữu trước quyết định tiếp, không tự tạo transaction mới.
5. Crash trước commit bền: Load checkpoint trước entry, không giữ thưởng một nửa. Crash sau file commit nhưng trước swap: Load candidate đã có ledger+items, không gọi grant lần hai. Presentation có thể phát lại thông báo theo policy, nhưng không phát lại kinh tế.

Đây là thiết kế transaction **chưa tồn tại** trong SaveManager/CombatLootRewards. Ghi file atomic hiện tại không đủ để tuyên bố exactly-once boss reward. Nếu nhánh item invalid/cap đầy, phải xử lý theo policy thưởng đã duyệt; không tự clip quantity, sinh ID thay thế, reroll hoặc silently mark paid.

ManualSave1/2/AutoSave là các snapshot khác thời điểm. Anti-duplicate đề xuất bảo vệ một completion trong timeline đã load; Load snapshot trước boss có thể cho đánh và nhận lại trong timeline cũ, nhưng các item từ tương lai cũng bị rollback. Không tuyên bố chống rollback giữa slot hoặc người dùng sao chép file; nếu cần luật toàn tài khoản, cần authority ngoài snapshot và một thiết kế riêng, không nằm trong game offline hiện tại.

## Save compatibility và quyết định còn thiếu

Component `bossEncounterVersion`/ledger tương lai phải được guard trước overwrite; Save cũ không có ledger -> chưa có completion boss. Sáu flag catalog không tự migrate thành cleared. Unknown encounter/definition/revision/future version phải từ chối hoặc preserve theo policy rõ ràng, không xóa record và ghi đè. Giữ worldMapVersion=0 legacy path; không biến legacy map chia hết 6 thành node boss mới.

| ID | Quyết định chờ duyệt | Chặn phần nào |
|---|---|---|
| B1 | Unlock cho từng node, spec boss/phase và difficulty | Gate thật và nội dung combat |
| B2 | Failure/Retry/exit, HP/cost/phase progress | Coordinator failure và checkpoint |
| B3 | One-time/repeat, reward/EXP, cap/failure policy | Idempotency key và outcome plan |
| B4 | Save checkpoint/slot, Load mid-attempt, manual rollback/downgrade | Durable transaction và migration |

Các quyết định không chặn tài liệu và tests thuần mô phỏng sau khi được giao. Không tự bật node để “thử kiến trúc”.

## Ma trận kiểm thử cho nhiệm vụ sau

| Cạnh | PASS dự kiến |
|---|---|
| Gate | Sáu boss vẫn chặn nếu chưa có spec được duyệt; invalid/nonadjacent/not-cleared fail không mutation; legacy không đổi. |
| Lifecycle | Entry cancel, pause, phase chuyển, death cùng frame, duplicate/stale boss callback và pooled life không tạo hai terminal result. |
| Scheduling | Normal wave không chạy trong boss; boss chết không tăng đủ 5 wave thường hoặc auto travel. |
| Reward | Một accepted completion -> đúng outcome/instance IDs; retry write/callback/Load không roll hoặc grant lại; EXP/growth không lặp. |
| Failure | Material cap/invalid gear/unsupported version, disk full/replace failure và ambiguous commit đều giữ invariant theo policy đã duyệt. |
| Crash | Fault injection trước write/sau temp/sau Replace/trước swap/sau UI; recovery khớp clear+ledger+items cùng snapshot. |
| Travel/Save | Sau thắng chỉ travel hợp lệ do người chơi chọn; Load trước/sau completion, manual slot rollback, old/current/future đều rõ kết quả. |

**NOT RUN:** không có boss encounter, persistence ledger hoặc các ca trên được triển khai/chạy ở v0.4. Chỉ kiểm kê catalog/gate và thiết kế; không dùng metadata sáu node làm bằng chứng boss hoạt động.
