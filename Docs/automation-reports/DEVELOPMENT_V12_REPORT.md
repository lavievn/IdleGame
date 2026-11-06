# Development v1.2 — Bách Luyện Tiên Đồ

07/11/2026 · **Đã triển khai và xác minh nền tảng; Thiên Kiếp production vẫn khóa. Không phát hành.**

## Kết quả và phạm vi

Thực hiện tuần tự A → B trên repo `lavievn/IdleGame`, nhánh `feature/monster-awareness-ambush`, worktree thực `D:/IdleGame/Project`. Baseline sạch `56f3f49654e4e7adab6e5a1b0b327e38b1b63d10`. Không fetch/pull/push/merge/reset/rebase hoặc đổi nhánh; không dùng subagent hay writer song song. Đã đọc báo cáo v1.1, quy tắc/skill repo Unity–Save–Build, Dual Progression, Capacity, Save Migration, Blood Essence và phần lịch sử Realm; dùng authority căn cơ hiện tại thay cho thiết kế Level cũ.

**A:** đã đo riêng thời gian JSON checkpoint, `Cultivation.Tick`, UI nhân vật; thêm control cấp phát global và kiểm chứng Save. Không tối ưu lịch publication, cache, schema, persistence hay clock vì phép đo hiện tại chưa chứng minh chúng gây vấn đề frame cần sửa. Việc không tối ưu là quyết định dựa trên số đo, không phải tuyên bố allocation bằng0.

**B:** có domain hai pha, receipt chống trùng/stale, transfer thành công explicit và bridge fixture đọc combat state/lifetime. Không có Thiên Kiếp trong gameplay thật. `HeavenlyTribulationEnabled=false`; production factory luôn trả `Locked`. Không tự đặt xác suất thắng, mức phạt, chi phí, buff, cân bằng cảnh giới hay luật Save giữa trial.

Unity giữ nguyên **6000.5.0f1**, backend Mono2x, MainGame là build scene duy nhất. Không truy cập nội dung Save hoặc cấu hình DEVB cá nhân. Mọi Save có I/O dùng profile GUID/marker riêng, cấu hình trước Awake, không có ancestor junction. Build/cache/log/fixture mới ở ổ D theo quy định lưu trữ repo.

## A — Phương pháp và số đo

`CultivationPerformance` có ba scope tách biệt: `Cultivation.CheckpointJson` bao gồm capture DTO và JsonUtility; `Cultivation.Tick` bao gồm bước domain và validation; `Cultivation.CharacterUI` bao gồm toàn bộ `RefreshCharacterProgression`. Scope chỉ đọc đồng hồ/profiler khi bật explicit; mặc định tắt. Scope struct không boxing, buffer counter cố định, báo cáo tạo sau khi dừng. Không đổi kết quả/đường đi các method gốc. Thời gian đo có overhead instrumentation, không phải CPU hardware tuyệt đối hay khẳng định0 overhead khi tắt.

Microbenchmark chạy code thật:256 lần làm nóng,10000 lần/round,3 round, lần lượt Tick-only / JSON-only / binding tại144FPS giả lập. Binding vẫn xuất checkpoint mỗi frame; restore exact clock/owner/resources với epoch mới, pause không cấp thời gian. Không so native batch headless với Player có đồ họa như cùng một môi trường.

| Phép đo Player cuối | Thời gian trung bình mỗi lần, µs (min–max qua3round) |
| --- | ---: |
| Tick-only | 2.72–2.84 |
| JSON-only | 4.83–4.98 |
| JSON trong binding | 5.43–5.57 |
| Tick trong binding | 3.12–3.57 |

Native batch ban đầu: Tick0,90–0,96µs; JSON5,35–5,38µs. Raw rows nằm trong `native-measurements.json`, Player rows trong `player-v12performance/isolated-measurements.json`. Mỗi binding round có10000 JSON calls và695 Tick calls được đo; không diễn giải số Tick ít hơn như mất thời gian.

Bốn cửa sổ UI trên cùng Hero canonical **đang pause**,800×450, làm nóng1giây rồi đo8giây; đóng/mở/đóng/mở, không có combat/progression trong cửa sổ. UI polling giữ0,2giây. Tên file có chỉ số cửa sổ để không ghi đè khi frame count bằng nhau. Bản chạy A đầu đã có collision tên frame-count; số UI dùng nghiệm thu là bốn file riêng trong bản cuối.

| Cửa sổ | Refresh calls | UI µs/call | Global bytes/frame |
| --- | ---: | ---: | ---: |
|character-closed-paused #0|39|2.05|7236.7|
|character-open-paused #1|40|68.18|7364.5|
|character-closed-paused #2|40|1.95|7236.7|
|character-open-paused #3|40|68.92|7361.2|

Đây là cost của method cập nhật UI; toàn bộ TMP layout/render nằm ở nhiều pha khác và không được gán hết vào scope này. Delta global giữa UI đóng/mở là so sánh bối cảnh, không phải allocation chính xác của từng UI call.

`GC.GetAllocatedBytesForCurrentThread` trả0 cả khi cấp phát control64KB; **không hỗ trợ**, mọi giá trị per-call trực tiếp ghi−1. Bổ sung `ProfilerRecorder` global `GC Allocated In Frame`: control rỗng và các batch256 thao tác/frame,4 frame làm nóng +64 steady samples. Kết quả tăng thêm so với control: **Tick≈480byte/call; JSON≈1730byte/call**. Đây là ước lượng differential trong fixture, có noise/cost của host và không thay thế per-thread attribution. Không cộng chồng nó với global frame allocation hoặc gọi nó là leak.

Farm tự nhiên30giây của Player cuối: **4318frames**, mean **6.950ms**, p95 **6.938ms**, main-thread mean **6.941ms**, global **12308byte/frame**, Gen0 **44**, Advance **33.32µs**, **3disk snapshot writes**. Ba writes không phải file write mỗi frame; JSON publication vẫn mỗi accepted frame. Heap 9715712→9863168byte là hai ảnh chụp ngắn, không chứng minh leak-free. Farm ghi nhận30damage events, Level1/EXP14, Hero sống; không override gear/HP/EXP/monster trong khoảng đo. Không coi chênh lệch với v1.1 ở lần chạy khác là cải thiện có kiểm soát.

JSON chiếm nhiều hơn Tick trong các phép đo riêng, nhưng cost thời gian hiện nhỏ so với frame. Giữ nguyên checkpoint giúp tránh đổi contract trước Save/Load/Travel/migration. Đo allocation đã có control hỗ trợ; attribution chính xác theo call stack và nghiên cứu leak dài vẫn chưa thực hiện. Không tuyên bố đã giải thích toàn bộ31,8% allocation tăng trong báo cáo v1.1.

## B — Domain Thiên Kiếp đã thực hiện

`HeavenlyTribulation.cs`: factory fixture yêu cầu actual bottleneck, injury hợp lệ, không Burning/pending debt, explicit known ascending successor và transfer hợp lệ. Hai pha Endurance→Damage→Resolving→Committed; Failed/Cancelled là terminal. Clock owner/epoch/attempt/life và step tăng đơn điệu; receipt thêm target life/event/phase; stale/duplicate/invalid/paused bị từ chối trước mutation. Incoming không lấp outgoing threshold. Revalidate HP/pause/realm revision/eligibility tại commit; dùng `RealmBreakthroughTransition` hiện có, không suy tu vi từ Level hay effective combat realm.

`TribulationFixtureCombatBridge` chỉ Editor/Development: chụp đúng `HeroCombatState`, data reference, LifeVersion; actor được rebind/respawn khiến callback cũ stale. Đọc HP thật và damage đã được combat chấp nhận, không tự tính damage hoặc ghi HP. Fixture dùng API Attack/Armor và `CombatDamage.ResolveBasicSnapshot` hiện có để chứng minh sức mạnh/phòng thủ tác động vào evidence. Đây chưa là event subscriber hay encounter thật trong MainGame.

Không có default production policy hoặc Resources asset trial. Các số1giây/2giây/10damage/transfer0 chỉ có trong fixture có ID `fixture:`. Không có RNG, xác suất, chi phí, reward, penalty, retry tự động, nguồn EXP/loot, thiên lôi, target production, scheduler thứ hai, artwork hoặc animation mới. Production catalog vẫn chỉ Luyện Khí; Trúc Cơ chưa mở.

Success fixture chỉ commit trong bộ nhớ sau hai pha. Resolving không cấp tiến độ. Failed/Cancelled không áp penalty chưa duyệt; đây không phê duyệt thất bại miễn phí cho production. Cancel causes Load/New/Retry/Travel/Menu/Disabled/Explicit chỉ được kiểm thử ở domain. Không nối trial vào lifecycle production khi Save/crash/resume/failure policy còn thiếu.

## Save và tính đúng

Không đổi schema/config ID/version, resolver, serializer semantics, candidate-before-swap, migration receipt/backup/rollback, AutoSave owner authorization hoặc publication cadence. `EntityDataSO.cultivationJson` tiếp tục materialize đầy đủ clock/remainder/provenance trên mỗi accepted frame. Không có dữ liệu attempt được lưu hoặc âm thầm resume; production không có attempt để lọt qua gate.

Native V09/V11 kiểm tra byte/token preservation, legacy/config1/2/3 compatibility, consent cancel/accept, immutable backup/history, eight fault points, backup/temp blockers, mismatched backup, Windows locked Replace, source/metadata races, orphan temp, newer-snapshot rollback refusal, modeled interrupted metadata recovery và future/malformed/unknown/nonzero Blood/Burning rejection. Clock.037s cũ qua upgrade vẫn được settlement đúng source một lần. Không dùng Save cá nhân làm fixture; không xóa nợ để Load.

Native Capacity chạy đủ3660 game-seconds ở8Qi tiers,5starts và30/60/144FPS có capture/restore; error conservation<1e−6, mốc60phút và capacity boundary giữ nguyên. MainGame thật xác minh Update, owner canonical, Save/Load/fresh epoch, Travel old-source fraction, Retry/camera/combat identity và New. Process Unity mới khôi phục checkpoint V3 chính xác, không offline award.

## Kiểm thử phân lớp

| Lớp | Kết quả | Giới hạn |
| --- | --- | --- |
| Mono / UnityStub |188 ca liên quan PASS; thêm1test group chạy77assertion Thiên Kiếp PASS |Các nhóm có overlap; không gọi tổng này là189 assertion độc nhất hoặc Unity PASS |
| Unity native batch |77assertion Thiên Kiếp; V09 migration/133nodes; V11 compatibility; V11 capacity/simulations PASS |Native APIs/GameObject/JsonUtility; không gọi đây là toàn bộ Unity Test Runner/NUnit suite |
| Unity MainGame Play Mode |4/4phases PASS; process restart PASS; Character4sizes native TMP PASS |Methods/owned fixtures, không native clicks. Khoảng farm5s realtime quan sát≈4,185 active seconds; không sửa clock để ép5s |
| Windows Development build |Succeeded,0errors,56warnings,674152510bytes,Mono2x |Không retail/IL2CPP/release |
| Windows Player performance |Ba rounds component timing,4UI windows,allocation controls,30s farm,exact Save/Load PASS |Đo ngắn, per-thread allocation unsupported |
| Windows Player domain+capacity |77assertion Thiên Kiếp fixture + real canonical capacity/consent/backup/.037clock/UI4sizes PASS |Không phải Thiên Kiếp gameplay thật |
| Windows Player matrix |PASS,uiIssues=[] |New,legacy consent/cancel/accept,invalid/repeated Load,Inventory/Forge/Map/Character4sizes,weapons,Travel,SaveLoad,Retry,MenuContinue,New; method-driven |
| Windows Player fresh process |Replay PASS,exact checkpoint/backup metadata,no offline award |Owned fixture Save |
| Native mouse/keyboard |**NOT RUN** |Không gán screenshot/hit-handler thành thao tác input thật; phiên này không thử CUA để khẳng định block cũ còn tồn tại |

Các nhóm Mono: cultivation-balance3; cultivation-gameplay5; dual-progression19; realm-related16; progression-ui4; cultivation-related20; weapon-basic32; sprites28; archer-animation5; enemy-sprites6; basic-movement11; multi-zone39. Tổng188 executions; nhóm mới tribulation1 chứa77 assertion có tên trong evidence.

**Lỗi tồn tại sẵn:** lần gọi nhầm `--balance` không phải nhóm được runner hỗ trợ đã rơi vào suite rộng, dừng ở `OpeningFontRegression`: `HUD never overwrites popup font: 18 != 16`. Đã xuất source baseline `56f3f49` sang thư mục fixture riêng ở D, bổ sung dữ liệu Resources văn bản và chạy đúng `--opening-font`, tái hiện cùng lỗi. Không sửa font/assertion/UI ngoài phạm vi để ép PASS. Log lần gọi rộng và log baseline được giữ. Full suite không PASS và không được tuyên bố PASS; các nhóm liên quan đúng đã chạy lại thành công.

Shutdown warning ComputeBuffer/TMP/import và build warnings được giữ trong log, không báo0warnings. Không gặp assertion/NullReference/MissingReference làm thất bại các lần nghiệm thu cuối. Chưa chạy arbitrary kill/power-loss, long leak study, mọi độ phân giải, native input hoặc retail/IL2CPP.

## Source, commits và artifact

| Commit cục bộ | Nội dung |
| --- | --- |
|`c40d64a`|Instrumentation và fixture đo checkpoint/Tick/UI, Save-equivalence|
|`d681e13`|Domain Thiên Kiếp, gate khóa, bridge/77assertion, Player controls và launcher MainGame|
|Commit tài liệu cuối|Hợp đồng này, báo cáo và evidence metadata; SHA cuối trong `handoff.json`|

Runtime Player cuối build từ **`d681e1325ea34842c5c601b9d0c368ff09524575`**, source commit được Player tự ghi. Các lần A/MainGame trước đó ghi HEAD baseline/c40d64a trong khi đang có scoped work-in-progress; nguồn runtime được kiểm tra lại trong Player cuối. SHA256 exe `e3c3a181dcb8bab365f7dae4788c8f8164dd1dfa8a5e51d5a717c04b1fc3f7e5`; Assembly-CSharp `a9f95daf2be83282568c958c8d08639e669ac2e359603dbf0d4ce2062a8c0c1c`. Giữ tên executable lịch sử IdleGameV10.exe không có nghĩa đây là binary v1.0.

Git scope comparison giữ nguyên SaveManager/GameManager/EntityDataSO, Hero/Monster/combat formulas/stats/AI, camera/world/artwork, scene, packages/settings và frozen config/Qi data. Chỉ thêm scope wrapper vào Capture/Tick/UI và flag trial false; production behavior cũ giữ nguyên. Dọn đúng15meta import-generated,1meta do helper sinh ngoài Assets và trả TMP fallback asset về blob baseline đã sạch, có path/hash allowlist; không reset/recursive-delete. Feature script meta được giữ. Metadata/GUID checker và final diff/status được ghi ở `metadata.json`/`handoff.json`.

Repo report: `Docs/automation-reports/DEVELOPMENT_V12_REPORT.md`; contract: `Docs/architecture/HEAVENLY_TRIBULATION_FOUNDATION_V1.md`. Evidence gốc `D:/IdleGame/Automation/v12/evidence`; final build/profiles `D:/IdleGame/Builds/DevelopmentV12Final`. Bản giao gồm báo cáo này và **Development_v1.2_evidence.zip** (kết quả JSON/log/77checks/screenshots/source diff/manifests; không đóng gói Save cá nhân hoặc binary674MB).

Lặp kiểm thử: `D:/IdleGame/Launch.ps1 -Mode Native -Method DevelopmentV12Verifier.Measure` hoặc `.Tribulation`; các verifier V09/V11 RunNative như cũ. `-Mode MainGame` chạy lifecycle capacity owned, `-ReplayProfile <owned foundation path>` replay ManualSave2. Player dùng `Tools/Run-WindowsAcceptanceV10.ps1` với `-ProjectRoot D:/IdleGame/Project -OutputRoot <new marker-owned D path>` và các mode Build→Performance→Tribulation→Run→Replay. Không chạy qua junction C. Mono dùng `Launch.ps1 -Mode Mono -Group <tên nhóm hỗ trợ>`; runner cũ chưa reject unknown group.

## Những quyết định còn thiếu trước mở Thiên Kiếp

1. Successor realm catalog, capacity/thresholds mới và foundation/reserve transfer.
2. Định nghĩa hai pha: nguồn/nhịp damage, thời lượng, survival, target, deadline/threshold và thứ tự xử lý đồng thời chết/hết giờ/thành công.
3. Có hay không RNG/xác suất thắng; entry cost/refund/reward; failure injury/HP/location/retry và cancel policy.
4. Save giữa trial, quit/crash/load resume hay cancel, durable success transaction và lỗi ghi đĩa; không báo thành công trước persistence.
5. Blood/Burning, injury/skill/modifier interaction và canonical combat event provenance.
6. Nghiệm thu production adapter/UI/lifecycle trong Unity/Player/native input trước khi mở gate.

Không cần quyết định các mục này để dùng phần domain/fixture hiện tại. Chúng chưa được duyệt; production gate tiếp tục khóa.
