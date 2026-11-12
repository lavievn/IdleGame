# Hàn Vụ N3 — Domain & interaction hardening, 12/11/2026

<!-- daily-freeze-rc2 -->
> Lượt Daily Freeze xác nhận lại N3 30/30 và bổ sung smoke Hàn Vụ native. Các ca stale callback chi tiết vẫn là bằng chứng domain. [Báo cáo Daily Freeze](DAILY_FREEZE_2026-11-12_FINAL_REPORT.md).


## Nền, HEAD và phạm vi

- Branch/worktree riêng: `feature/cloud-hanvu-n3-domain`, `/workspace/IdleGame-hanvu-n3`.
- Base #17 đã fetch/API xác minh:
  `cc271451faccd4c2ba7d9ef679a93c7627bc5f5c`.
- Dependency #13: `5915829aa2510a739bde64addf0a41b705c1b8eb`, là tổ tiên của #17.
  Hai head không đổi trong preflight. PR mới target nhánh #17 để diff chỉ có N3.
- HEAD runtime đã kiểm: `3c06f8bea1470f35e2d135c5a28a6dc004ba217e`.
  Commit tài liệu/harness bàn giao sau đó không sửa runtime; HEAD cuối nằm trong
  PR metadata và được đối chiếu remote sau push.
- #21 `0ab8c88`, #22 `d8e08eb`, #23 `382f389` đều OPEN/DRAFT khi đọc. Đã đọc
  AGENTS/README/project skills và baseline, Foundation, slice ở #23. Không merge
  các nhánh tài liệu ấy vào nền gameplay. Không coi branch này là integration
  Windows, không làm N0/N1, N4 Save transaction hoặc N5/N6.

Scope slice đã duyệt giữ nguyên: nhận/từ chối thuốc, close/ignore trung lập,
từ chối có thể đổi ý; M0/M1 suy từ việc sắc thuốc, không tuổi/năm/NPC/reward/Save
field mới. Chỉ có ba file production thay đổi, đều thuộc logic tương tác truyện:
HanVuNarrative, GameManager và callback wiring ở UIManager.StoryOrigin. Không đổi
HudW3/layout/visual; mọi string literal trong cả ba file giữ nguyên so với base.

## Lỗi đã tái hiện, nguyên nhân và bản sửa

### 1. Ngữ cảnh cũ được dùng trước UI tick

Trên runtime #17, `BeginOriginConversation` lưu dialogue/conversation, còn callback
choice lại lấy `GameForUI().HeroData` lúc chạy. Tick đóng thoại khi đổi Hero/map
nhưng callback có thể chạy trước tick ấy. Retry/travel round trip còn có thể giữ
cùng Hero/map nếu view bỏ lỡ frame ẩn. Không có token spawn tại điểm tương tác.

**FAIL trước sửa trên nguồn production thật**, sáu ca:

1. Opening cũ nhận quest cho Hero thay thế, trước khi view tick.
2. Opening cũ vẫn chọn được sau cùng Hero được respawn.
3. Opening cũ còn dùng được sau đi/rời rồi trở về cùng làng nhưng spawn đã đổi.
4. Callback “sắc thuốc” cũ gọi API interaction chung trên Hero mới ở SearchForest,
   làm chuyển thẳng SearchElite dù đó không phải intent đang hiển thị.
5. Callback encounter cũ chọn cho pending encounter của Hero thay thế.
6. View không quan sát frame ẩn sau respawn, rồi hiển thị/ghi lời kể cũ ở đời mới.

Checkpoint test `41d3765`: **20 PASS / 6 FAIL** trong26 nhóm. Các lỗi này tái hiện
với production dialogue callback và origin host methods; lifecycle replacement/
spawn là đầu vào mô phỏng, **chưa chứng minh tần suất xảy ra trong Unity Player**.

Sửa: thêm context **tạm thời, không serialize** gắn với Hero reference,
OriginStoryState reference, map, actor reference và SpawnVersion hiện có.
GameManager capture/check context theo readiness/trial liability. Callback và
tick kiểm context trước khi gọi domain/ghi lời kể. Close retire context; một
cuộc trò chuyện mới nhận context mới. Không tạo field NPC, milestone, event ID
hoặc ledger Save. Domain chọn/tiến stage và commit boundary cũ giữ nguyên.

### 2. Delegate được giữ lại còn có thể tiêu intent mới

Kiểm tiếp sau guard đầu phát hiện hai ca nữa: giữ delegate Accept cũ, sau đó mở
cuộc thoại mới của Hero thay thế hoặc đóng/mở lại ở cùng đời, rồi gọi delegate
cũ. Nếu lambda đọc field context đang thay đổi, nó dùng context **mới** và nhận
quest ngoài intent cũ. **28 PASS / 2 FAIL** trên runtime checkpoint `1df0d46`.

Sửa bổ sung: mỗi lần mở conversation/encounter dùng context object riêng và bind
Accept/Decline bằng closure chụp đúng object đó qua `BindClick` hiện có. Handler
so identity intent trước khi kiểm host. Delegate của view đã đóng/thay thế bị
từ chối mà không đóng hoặc tiêu view mới. Không tạo scheduler/manager hoặc
protocol event tổng quát. BindClick/registered-interactable/Windows-clickable
lists hiện có đã chống thêm trùng; không sửa các helper ấy.

Kết quả cuối: **30 PASS / 0 FAIL**, gồm11 nhóm #17 nguyên vẹn và19 nhóm mới.
Trong đó tám ca từng FAIL đều PASS; current choice/medicine/forest interaction
vẫn hoạt động. Mọi sửa lỗi đều ở interaction context, không đổi luật Hàn Vụ.

## Những gì đã đúng và không viết lại

| Yêu cầu | Bằng chứng sẵn trên #17 | Phần bổ sung N3 / kết luận |
| --- | --- | --- |
| Accept/Decline/Ignore | Optional participation, encounter Pending/terminal, không auto-quest khi farm | Giữ nguyên domain; test thêm actual dialogue controls và idle callbacks |
| Decline → Accept | CanOfferHerbs/ChooseHerbs giữ declinedHerbalOffer và cho đổi ý | Giữ nguyên; test duplicate decline và return theo world-map clear thật |
| Close trung lập | CloseOriginPresentation không gọi Choose | Test đóng rồi gọi lại handler không tạo choice |
| Replay không thưởng/tiến trình mới | Lines/Journal/ItemCount là projection | Test toàn bộ stage + nút replay thật; không thêm API timeline |
| Lời kể chưa đọc | TellsOfTrip/TellsOfWoodcutter, record đúng line; #17 đã sửa câu hỏi trước câu trả lời | Test đường render/update logic thật trước/sau line; chỉ bổ sung stale-context guard |
| Return → SearchForest | Interact trong làng khi stageReturn; repeat talk không consume lại herbs | Thêm test wrong map/repeat, không sửa HanVuOrigin |
| M1 ở các stage sau | Journal dùng stage>=SearchForest cho guardian tạm dịu ho | Test SearchForest/SearchElite/Complete, không thêm milestone/condition field |
| Forest action | Accepted + SearchForest + đúngForest, sau đó SearchElite | Test đủ stage/participation và fresh intent sau travel context |
| Duplicate/stale | Domain stage/choice guards có, view lifecycle guard còn thiếu | Tám reproductions và context/intent fix ở trên |
| AFK/EXP/Qi | Choice cần explicit action; cultivation/EXP authority riêng | Test time/UI idle, AddExp thật, CultivationGameplayBinding.Advance thật; không thay công thức |
| Weather | #16 chưa có trên base này; không có liên kết clock trong origin domain | Chỉ xác minh source boundary; weather integration **NOT TESTED**, không gộp code #16 vào test |

M1 trong test được nhận biết từ facts/journal hiện có, không thêm helper production
để “có thay đổi”. OriginStoryState/SaveVersion3/UpgradeSave và toàn bộ HanVuOrigin
giữ nguyên byte; không chỉnh migration. Trạng thái tạm dịu ho không được đổi
thành khỏi bệnh vĩnh viễn hoặc death/ending.

## Cấu trúc test và giới hạn chứng cứ

`Tests~/HanVuN3/run.sh` biên dịch production HanVuOrigin/HanVuNarrative/
HanVuEncounters, UIManager.StoryOrigin và các lớp dữ liệu thật. Các origin host
methods được **trích nguyên văn lúc chạy từ GameManager.cs**, không viết lại
implementation trong fixture; extractor dừng nếu anchor đổi. Cách này tránh
kéo SaveManager/I/O và toàn bộ suite stub đang bị chặn vào test N3.

Boundary chỉ thay dịch vụ bên ngoài: readiness/lifecycle inputs, presentation
helpers và sink đếm lần yêu cầu commit. SpawnVersion do fixture thay đổi; không
chạy thực `OnRetryClicked`, `TryTravel` của GameManager hoặc native Load. Phép
travel domain dùng WorldMapCatalog thật; các host-context tests không được nâng
thành native travel/respawn PASS. Commit count không phải số lần Save thành công.

Test equality dùng nested data encoder có sẵn, không coi đó là native JsonUtility.
Qi test dùng profile fixture explicit và binding/session/equations production,
không mock công thức. Không đọc/ghi/xóa save người chơi hoặc chạy Save I/O.

Runner original #17 không sửa. Không làm yếu assertion để xanh suite. Khi bổ
sung bài travel, fixture phải hoàn thành map waves và assert return thành công
theo world rules; không bỏ travel gate hoặc biến map mutation thành quest credit.

### Tái chạy bằng nguồn đã ghim

```bash
bash Tests~/run-hanvu-story.sh
bash Tests~/HanVuN3/run.sh
bash Tests~/HanVuN3/baseline.sh original
bash Tests~/HanVuN3/baseline.sh retained
```

Cần Python3, C#7.2+ Mono; CSC/MONO có thể trỏ wrapper đã có. Hai lệnh baseline
**expected exit1**: original20/6, retained28/2. Chúng git archive vào thư mục tạm,
không switch/merge branch. Archive gồm các world-map fixture resources; không
sửa source/assertions để bỏ lỗi. README runner giải thích giới hạn chi tiết.

## Validation thực chạy

| Kiểm tra | Kết quả |
| --- | --- |
| Original #17 data runner trước và sau | **PASS11/11**; không chỉnh runner/assertion gốc |
| N3 trên #17 trước fix | **20 PASS / 6 FAIL**; nguồn Assets chưa thay đổi ở checkpoint test |
| First guard và retained delegates | First guard28/0; thêm hai retained-delegate tests → **28 PASS / 2 FAIL** |
| N3 cuối | **PASS30/30**,19 nhóm mới; đã bao gồm11 nhóm gốc, không cộng thành41 |
| Standalone toàn runtime Roslyn + Unity6000.5.0f1 DLLs | **PASS0 lỗi/93 cảnh báo**; baseline cũng0/93. Không phải Editor import hoặc Player build |
| Full historical Mono runner, **compile-only** | **BLOCKED12 lỗi cũ**: Vector2/Mathf/TMP APIs trong stub và HeroHealthMultiplier legacy. Không chạy test executable hoặc Save I/O |
| Metadata/GUID checker | **PASS0 lỗi/62 cảnh báo**, PackageCache/metadata cũ; không asset mới, không sửa/tạo GUID |
| Shell syntax, git diff check, literal/layout/scope checks | **PASS**; evidence validation.txt |
| Unity Edit/Play/native input/Windows | **NOT TESTED** |
| Native GameManager lifecycle/SaveManager Save/Load/durable commit | **NOT TESTED**; dữ liệu/callback PASS không phải native Save/Load PASS |

Bằng chứng:
[before](evidence/hanvu-n3/before.txt),
[retained-before](evidence/hanvu-n3/retained-before.txt),
[after](evidence/hanvu-n3/after.txt),
[validation](evidence/hanvu-n3/validation.txt),
[full-runner compile blockers](evidence/hanvu-n3/full-compile-blocked.txt).

## File thay đổi và checkpoint

| File/nhóm | Thay đổi |
| --- | --- |
| Assets/Script/HanVuNarrative.cs | Context object tạm thời; phần enum/Lines/flags/journal cũ giữ nguyên |
| Assets/Script/GameManager.cs | Hai helper capture/check origin context; không sửa CommitOriginChange/Save paths |
| Assets/Script/UIManager.StoryOrigin.cs | Capture/check/retire intent, bind callback; không đổi thoại, tọa độ, style hoặc layout |
| Tests~/HanVuN3/* | Runner, production host extractor, boundary, regression và pinned baseline reproduction |
| Docs/automation-reports/* N3 | Báo cáo và evidence của task |

- `41d3765`: regression trước sửa,20/6.
- `1df0d46`: Hero/map/spawn context, Qi/context tests,28/0 trước retained cases.
- `3c06f8b`: capture riêng từng intent/delegate,30/0; đã push branch riêng.
- Commit bàn giao sau cùng: sửa gói resource của baseline reproduction, bổ sung
  báo cáo/evidence; không đổi implementation runtime đã kiểm tại3c06f8b.

Không đổi EntityDataSO, SaveManager, Save schema/version/migration, EXP/Qi/Combat/
Blood/Inventory/Forge/Loot/Weather/Audio/Sky/Camera/HUD layout, scene hoặc assets.
Private context của UI không có SerializeField/Serializable; không vào player Save.

## Host/Save risks còn lại và N4 chính xác

1. **CommitOriginChange vẫn mutate live rồi SaveGame**, thất bại chỉ thông báo.
   Guard N3 không bảo đảm rollback hoặc durable exactly-once. N4 phải chứng minh
   candidate tách mutable state trước publish và dữ liệu cũ/mới nhất quán khi write lỗi.
2. Retry commit không được đem candidate cũ ghi đè kill/loot/EXP mới. Recheck
   canonical Hero/context/source state khi publish, giữ mọi component tích hợp.
3. Native Clone/JsonUtility/Load và fail-closed của originStory cần fixture đúng
   boundary. GameManager Load dùng candidate; direct SaveManager.LoadGame lên
   canonical không được coi là atomic. Legacy thiếuOrigin/v1/v2/v3 phải giữ
   identity, choices và stage, không tự chạy hậu quả khi restore.
4. Context N3 chỉ bảo vệ đường interaction presentation hiện có. Internal host
   methods vẫn là trusted current-state API, không phải protocol command nhận
   intent từ mọi nguồn. Native ordering, real BindClick/input/entry/respawn cần
   nghiệm thu riêng; không thêm general event manager để che khoảng trống ấy.
5. Giới hạn nhiều offer trên cùng map chưa ảnh hưởng slice và không được mở rộng.
   Weather #16/build tổng hợp local không phải nền test này, không suy native PASS.

N4 dự kiến kiểm/sửa hẹp GameManager.TryOriginInteraction/CommitOriginChange và
SaveManager boundary **chỉ khi có bằng chứng cần thiết**, cộng fixture native
HanVuOriginSmoke/SaveLoad hiện có. Không thêm field/receipt/schema mới cho slice;
nếu cần thay contract persisted, phải có task riêng thay vì làm lén trong N3.
Không triển khai N4 ở đây. Stop condition N3 đạt ở regression và context fixes
có reproductions; không tiếp tục các phần persistence hoặc narrative tổng quát.

## Draft PR và bàn giao

[Draft PR #24](https://github.com/lavievn/IdleGame/pull/24) target
`feature/cloud-hanvu-story-polish` (#17), không lặp lại diff #13/#17.
Commit `6d2058591f209774424955ad6b63447de83a2d6e` chốt report/evidence;
commit liên kết bàn giao sau đó chỉ sửa tài liệu này. HEAD cuối và kết quả
local/remote equality được cập nhật trong PR. Giữ Draft chờ nghiệm thu native;
không merge PR nào trong task.
