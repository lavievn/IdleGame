# Local Integration Baseline & Documentation Sync — 12/11/2026

**BLOCKED — bàn giao từ Linux Cloud, chưa phải chứng nhận baseline Windows.**
N0 chưa hoàn tất; N1 mới chuẩn bị tài liệu, chưa áp vào integration local; N2
đã được chủ dự án duyệt trong task này. Không tuyên bố đạt stop condition N0–N2
hoặc đã có build evidence. Không triển khai Event System/migration hay gameplay.

## 1. Môi trường, HEAD và trạng thái được xác minh

Phiên đang gắn Linux Cloud, không có executor Windows, kết nối máy nhà, ổ D hoặc
artifact local. Environment status hiện hành xác nhận Cloud đang chạy, network
policy enforced, không có outbound identity/secret cấu hình; policy không có VPN.
Remote desktop là desktop của môi trường Cloud, không phải máy Windows của chủ
dự án. Đã thông báo giới hạn truy cập; chưa có Windows endpoint hoặc Integration
HEAD mới được cung cấp. Không suy diễn thành Windows đã truy cập.

| Đối tượng | Giá trị thực biết được |
| --- | --- |
| **Integration branch / HEAD Windows** | **UNKNOWN / BLOCKED** — chủ dự án đã xác nhận merge local ở task trước, chưa có tên/SHA/artifact |
| Windows worktrees / dirty changes / commits chưa push | **UNKNOWN / BLOCKED**; không kiểm kê được qua registry Cloud |
| Nhánh tài liệu hiện tại | `docs/local-integration-baseline-handoff`, worktree riêng `/workspace/IdleGame-local-baseline` |
| Base tài liệu | PR #22 @ `d8e08eb397446d4fdd6da9ed59d25df02025b1cb`; không coi đây là integration runtime |
| PR #21 | OPEN/DRAFT, `0ab8c88033ea9c4d97a7be4ed03705acad69f675`, base development_0.1.7 |
| PR #22 | OPEN/DRAFT, `d8e08eb397446d4fdd6da9ed59d25df02025b1cb`, base nhánh docs #21 |
| Checkout Cloud task EXP | `/workspace/IdleGame` @ `7601c372d82f034e4e7165cb000cc400fabbde84`, sạch, ahead/behind remote0/0 khi kiểm |
| Checkout Cloud task audit | `/workspace/IdleGame-narrative-audit` @ `d8e08eb397446d4fdd6da9ed59d25df02025b1cb`, sạch, ahead/behind remote0/0 khi kiểm |

Đã fetch tất cả head bằng refspec rõ, đối chiếu lại PR #21/#22 và tiếp tục từ
đúng thay đổi tài liệu đang dở sau khi hết hạn mức. Chỉ sửa worktree riêng;
không đụng hai checkout cũ hoặc tác vụ Windows. Không reset, force-push, rebase,
cherry-pick hay merge runtime PR.

Đã đọc nội dung PR #21/#22 và các đặc tả trên đúng SHA không đổi, AGENTS cùng
skill dự án/Unity/Save/Build. Unity của repo vẫn6000.5.0f1; không áp phiên bản6000.3
trong ví dụ skill. Kết quả kiểm tra Cloud nằm ở
[cloud-preflight.json](evidence/local-integration-baseline-2026-11-12/cloud-preflight.json)
và [checks.txt](evidence/local-integration-baseline-2026-11-12/checks.txt).

## 2. Ma trận tích hợp: nguồn công bố và phần còn chờ local

Không chuyển “đã có code trên head PR” thành “đã có trong build local”. Cần cả
ancestry **và** đối chiếu patch: merge/cherry-pick/squash ở local có thể làm
ancestry head trả false trong khi patch tương đương đã có. Không merge lại để
ép ancestry true. Ma trận chi tiết nguồn/overlap đã có trong
[audit #22](NARRATIVE_INTEGRATION_AUDIT_2026-11-12.md).

| PR / head đã công bố | Code cần đối chiếu trên integration Windows | Kết quả tích hợp local |
| --- | --- | --- |
| #12 `5d41014` | SharedHero/103 PNG/CultivationSpriteBridge, import settings, Karpathy | Chưa xác minh patch/head/asset/import |
| #13 `5915829` | Hàn Vụ, originStory v3, optional story/encounters, Save và host integration | Chưa xác minh exact patch level |
| #17 `cc27145` (kế thừa #13) | Chỉ ghi lời kể đã đọc, đóng stale thoại, HUD cho Hero hiện hữu | Chưa biết local đã có các sửa nối tiếp này |
| #16 `c00dae6` | worldClimate, HDR/rain/storm/ambience/BGM, launchers và Save guard | Chưa xác minh cấu hình/asset/audio/teardown local |
| #18 `9349f5f` | EquipmentFoundation catalog/ID/version validation | Chưa xác minh guard mới trong local |
| #19 `2b88eb5` | Codec capture reset burn/receipts và domain tests | Chưa xác minh fix; không áp hai lần qua #20 |
| #20 `7601c37` (kế thừa #19) | experienceJson, EXP bank/release candidate, Mastery contract, probe loop guards | Chưa xác minh Save integration hoặc native behavior |

Head đầy đủ đã ghim trong snapshot của #22. #10/#11 là tổ tiên của nền017 đã
công bố dù PR vẫn mở; đây không chứng minh renderer/build local đã được nghiệm
thu. Không có remote head công bố đồng thời #12/#13/#16 tại lần fetch này.

## 3. Manifest asset và build artifact cần lấy trên Windows

**Chưa có manifest local thực tế hoặc hash build trong task này.** Các vị trí
sau là nguồn cần kiểm theo báo cáo/launcher cũ, không phải xác nhận file đang
tồn tại hoặc được đóng gói trong artifact mới.

| Nhóm | Đầu vào cần kiểm | Bằng chứng phải ghi, hiện đều BLOCKED |
| --- | --- | --- |
| Hero animation | Assets/Resources/HeroAnimation/SharedHero.json và103 PNG từ #12; fallback hiện có | Path, GUID/meta, SHA256, size, profile refs, import result, nguồn upload |
| HDR/rain | Free HDR Skyboxes Pack, RainMaker, Resources/WorldClimateVendor/Assets.asset và shader/adapter | Local AssetDatabase paths/GUID/hash/version, liên kết scene/build và fallback; asset giữ local theo policy |
| BGM | Resources/WorldMapMusic,12 WAV ở bản #16 | Path/hash/import Streaming và tracks thực đi vào build |
| Ambience/weather/SFX | Inspector clips, village-map IDs, master/environment/weather volumes | Slot nào gán clip thật, slot nào null, source ownership; clip fixture im lặng không phải nghe thực |
| Unity/packages/profile | ProjectVersion, manifest/lock, Build Profile, scene list | Unity executable version, enabled scenes, backend/defines/options, Development/Production thực chọn |
| Player | EXE, UnityPlayer.dll, Data/Managed hoặc payload backend tương ứng, launcher/handoff/result/log | Absolute path, source HEAD, file hashes, build timestamp, renderer/device, process exit và asset-manifest digest |

`D:/IdleGame/Project` là đường dẫn được AGENTS ghi trước đây, **chưa kiểm lại trên
Windows**. Không lấy EXE cũ tại Development017/HanVuNarrativePolish/DynamicSkyWeather
làm artifact của integration HEAD mới. Không đọc hoặc sao chép save cá nhân.

## 4. Đồng bộ tài liệu: đã chuẩn bị, chưa áp vào local

Trên nhánh tài liệu an toàn này:

- Ghi phạm vi slice thành **APPROVED_SCOPE**, không đánh dấu implementation PASS.
- Cập nhật baseline12/11 và đầu README để không tiếp tục hỏi duyệt lại N2.
- Bổ sung chỉ dẫn ở Foundation: slice không thêm Save field; receipt/ledger/schema
  trong kiến trúc tổng quát không phải yêu cầu triển khai cho slice đã duyệt.
- Giữ nguyên AGENTS, lịch sử README cũ, các rules/skills và bản AGENTS union mẫu
  của #22; không đưa nguyên blob đó đè lên worktree Windows chưa đọc được.

Khi tiếp tục ở Windows: xác minh clean HEAD và task ownership trước; đối chiếu
README/AGENTS từng hunk với #21/#22 và cập nhật này, giữ Karpathy của #12 cùng
mọi cập nhật local mới hơn. Chỉ commit diff tài liệu đã review. Nếu worktree
dirty/chưa commit hoặc đang có tác vụ khác, giữ nguyên và ghi blocker; không
chuyển branch/reset/đóng Unity hoặc tự commit thay đổi đang thuộc tác vụ ấy.

**N1 vẫn BLOCKED ở integration local.** Việc push bản chuẩn bị Cloud không đồng
nghĩa đã đồng bộ vào nhánh đó.

## 5. Kết quả test theo cấp và loại lỗi

| Cấp / kiểm tra | Kết quả mới trong task này |
| --- | --- |
| Cloud Git/ref/doc preservation/link/scope | PASS những phép đọc/kiểm ghi trong checks.txt; không phải build verification |
| Domain/UnityStub runtime regression | NOT TESTED — chỉ thay tài liệu, không chạy lại suite thay cho baseline local |
| Unity import/compile/Edit Mode | NOT TESTED / BLOCKED thiếu máy Windows và project integration |
| Unity Play Mode: MainGame/Home/animation/Hàn Vụ | NOT TESTED / BLOCKED |
| Weather/HDR/rain/BGM/ambience/SFX nghe thực | NOT TESTED / BLOCKED thiếu asset/config/player local |
| Save/Load/Retry/Travel fixture riêng | NOT TESTED / BLOCKED; không chạy Save I/O trên Cloud |
| Windows Development build | NOT TESTED / BLOCKED; không có BuildReport hoặc artifact mới |
| Player gameplay/render/native input | NOT TESTED / BLOCKED; chưa biết renderer thực |
| Player exit/crash/teardown | NOT TESTED / BLOCKED, chưa có exit code/log mới |

Lỗi cần phân loại độc lập ở lượt Windows: **build** (compile/import/BuildReport),
**gameplay** (đúng hành vi/state/input), **asset/audio** (thiếu ref/clip/render),
**teardown** (ApplicationQuit/native crash/exit). #16 từng báo UnityPlayer.dll
0xc0000005/exit-1073741819 sau khi smoke chạy PASS; đó là bằng chứng lịch sử của
#16, chưa chứng minh local hiện tại còn hoặc đã hết lỗi. Không sửa lỗi ngoài phạm
vi; lưu log/dump/event evidence và ghi blocker nghiêm trọng khi xác nhận.

## 6. N2 đã duyệt và phần có thể tái sử dụng

Theo yêu cầu trực tiếp12/11, giữ tuyến Hàn Vụ, nhận/từ chối thuốc, close trung
lập, từ chối có thể đổi ý; M0→M1 là logic sau sắc thuốc, không tăng tuổi/năm.
Guardian và tương tác Cô Hồn Lâm phản ánh hành động hợp lệ. Không NPC, thoại,
reward, ending hoặc Save field mới; chưa triển khai Event System/migration.

Đối chiếu mã #17, chưa khẳng định exact implementation ở local:

| Hành vi | Tái sử dụng hiện có | Phần còn thiếu/cần chứng minh |
| --- | --- | --- |
| Nhận/từ chối/đổi ý | ChooseHerbs, CanOfferHerbs, HerbalParticipation, declinedHerbalOffer | Integration/native acceptance; không làm lại hệ choice hoặc khóa Declined vĩnh viễn |
| Close trung lập | CloseOriginPresentation không gọi Choose | Input/stale-view sau entry/Retry/Travel/Load trên build tổng hợp |
| M0/M1 | Return → SearchForest trong StoryOrigin.Interact; các stage sau cũng hàm ý đã sắc thuốc | Dùng phép đọc/guard đúng stage, không thêm timeline Save hoặc replay effects khi Load |
| Guardian condition | Identity đã lưu; narrative/journal dựa stage sau sắc thuốc | Không thêm field HP/tuổi hoặc khẳng định khỏi bệnh vĩnh viễn; view/native consistency |
| Forest interaction | CanInteract đòi Accepted + SearchForest ởTV-16; Interact tiếp tục sangSearchElite | View/action đúng trước/sau Load; M1 vẫn đúng ở các stage sau khi action đã tiêu |
| Một lần sắc thuốc | Stage không cònReturn sau Interact; journal/items là projection | Duplicate/stale host command và commit retry cần native/fault verification |
| Persist | originStory v1–v3 và GameManager candidate-before-swap khi Load | CommitOriginChange hiện mutate live rồi SaveGame; fail-write chưa rollback/candidate transaction tổng quát |

Không cần thêm serialized milestone/NPC-condition/receipt/version để mô tả slice
này. **Không thể từ đó tuyên bố có durable exactly-once cho Event System tổng
quát.** Ledger/migration tổng quát trong #22 vẫn là đề xuất ngoài phạm vi.

## 7. N3/N4: phần việc nhỏ và file dự kiến, chưa sửa mã

Chỉ chốt diff cuối sau khi biết integration HEAD. Nếu local đã làm phần tương
đương thì chỉ bổ sung test/bằng chứng, không tạo lại implementation.

| Task sau | File dự kiến kiểm/sửa hẹp | Mục tiêu và điều kiện kết thúc |
| --- | --- | --- |
| N3 — projection/transition slice | Assets/Script/HanVuOrigin.cs; HanVuNarrative.cs nếu thiếu projection | Map M0/M1 từ stage hiện hữu, preserve Accepted/Declined semantics và guardian facts; không event manager, schema hoặc content mới |
| N3 — host/context, nếu chứng minh gap | Assets/Script/GameManager.cs; UIManager.StoryOrigin.cs chỉ nếu thiếu stale intent guard | Giữ một host; owner/map/stage context đúng; duplicate/stale không làm lại transition, close/replay không mutation ngoài facts đã đọc |
| N3 — domain regression | Tests~/StoryOriginRegression.cs; Tests~/HanVuStoryRunner.cs; Tests~/run-hanvu-story.sh nếu cần source list | A/B/neutral/B→A, M1 tồn tại ở stage sau, guardian/forest projection, unrelated wave, duplicate/stale và no age/time grant |
| N4 — commit boundary, nếu gap còn trên local | Assets/Script/GameManager.cs tại TryOriginInteraction/CommitOriginChange; SaveManager.cs **chỉ khi cần sửa boundary có regression** | Candidate dùng schema hiện có, validate trước publish, save failure không để half-state; retry không ghi snapshot cũ đè progression/loot mới |
| N4 — native fixture/acceptance | Assets/Script/HanVuOriginSmoke.cs; MainGameFoundationProbe.cs; WindowsDevelopmentAcceptanceV10.cs; fixture SaveLoad hiện hữu nếu cần | Fixture riêng trước Awake, load/relaunch/fault/Retry/Travel; giữ toàn bộ component tích hợp |
| Launchers chỉ khi có gap do ghép | Tools/Local-IdleGame.ps1; Tools/Run-WindowsAcceptanceV10.ps1 | Giữ hợp nhất mode HanVu và Weather của hai nhánh; không viết launcher test mới nếu đường cũ đủ |

HanVuEncounters và catalog offer hiện có chỉ là phần tham chiếu ở slice này;
chưa cần mở nhiều event/map hay thêm ID mới. EntityDataSO, balance JSON, scene,
audio/weather/combat/camera và Save serialized fields không nằm trong diff dự
kiến của N3/N4 hẹp. Nếu cần thay schema, dừng phần đó và xin task mới, không lén
thêm receipt persist để đạt một tiêu chí tổng quát ngoài slice.

### Rủi ro Save cần kiểm trước nghiệm thu N4

- Clone/candidate phải giữ originStory/worldClimate/experienceJson/cultivationJson/
  tribulationJson/combatSkills/Inventory/Forge/world-map của build tổng hợp.
- Candidate phải tách mutable originStory/lists khỏi canonical trước khi thay
  đổi; shallow copy dùng chung reference sẽ phá điều kiện không half-state.
  UnityStub Instantiate không chứng minh native clone/serialization an toàn.
- Save thất bại hoặc retry khi canonical đã đổi không được publish candidate cũ
  làm mất kill/loot/EXP hoặc tự chạy lựa chọn lần nữa. Không grant trong restore.
- Validity hiện của originStory được kiểm sau overwrite; đường GameManager dùng
  candidate, direct SaveManager.LoadGame lên canonical không được gọi là atomic.
- Legacy thiếuOrigin không tự tạo NPC/quest; v1/v2/v3 giữ identity và lựa chọn.
  Future/invalid/partial phải từ chối trước swap và không overwrite dữ liệu gốc.
- Không đổi schema trong slice: test tính tương thích hiện có, không tự thiết kế
  v4, root version hoặc migration mới. Runtime epoch/context không thay persisted
  history; stage idempotency không phải permission cho mọi event tương lai.

## 8. Checklist thực thi khi chuyển sang Windows

Đây là chỉ dẫn **chưa chạy**, không phải log kết quả. Dùng AGENTS/Tools trên
chính local head, không chạy các lệnh ở worktree đang thuộc tác vụ khác.

1. Kiểm kê read-only: `git worktree list --porcelain`, `git branch -vv`,
   `git status --short --branch`, `git rev-parse HEAD` ở từng worktree;
   `git log --branches --not --remotes --oneline` sau fetch để liệt kê commit
   chưa công bố. Ghi working-tree diff/untracked, không reset/commit thay tác vụ khác.
2. Với từng head #12/#13/#16/#17/#18/#19/#20, ghi ancestry và so diff/patch ID;
   xác định code còn thiếu thay vì merge lại. Ghi exact local SHA và diff status.
3. Kiểm manifest ở mục3 và quyền dùng assets. Không import/copy lại vendor asset
   nếu local đã liên kết; ghi actual refs/hash và những clip còn null.
4. Xử lý tài liệu theo hunk như mục4; tạo nhánh an toàn từ clean integration HEAD,
   review diff rồi commit. Nếu không sạch/đang có task khác, ghi BLOCKED và giữ nguyên.
5. Dùng Tools/Local-IdleGame.ps1 hiện có: modeCheck cho import/compile, MainGame017,
   MainGameHanVu và các mode WorldClimate được local hỗ trợ. Hai launcher của #13
   và #16 có ValidateSet khác nhau: phải xác minh union, không lấy nguyên một file
   thay file kia. Chạy regression safe theo fixture và phân cấp bằng chứng.
6. Build Development qua launcher với OutputRoot mới có ownership dưới
   D:/IdleGame/Builds, không dùng folder đang có lượt chạy. Ghi BuildReport, HEAD,
   backend/options/scenes, EXE/hash và manifest; Unity vẫn6000.5.0f1.
7. Chạy đúng EXE ấy bằng profile riêng. Home là luồng pre-game trong MainGame ở
   nguồn đã kiểm, không tự thêm Home.scene. Kiểm animation/Hàn Vụ/âm thanh thực/
   weather/HDR/rain,600/800/1150, fixture Save/Load/Retry/Travel và input.
8. Ghi profile DevelopmentCultivation015 hay production thực chọn; preset DEV
   Windows MainGame có thể khác production. Ghi renderer/device từ Player log,
   không suy WARP chỉ từ tên launcher. Exit/quitting test riêng, giữ process code,
   Player.log và native crash evidence nếu có; không dừng process đang thuộc task khác.
9. Bổ sung mọi ô UNKNOWN/NOT TESTED bằng kết quả gắn sourceHEAD/artifact, phân lỗi
   build/gameplay/asset/teardown. Blocker nghiêm trọng chỉ báo, không sửa ngoài scope.

## 9. Quyết định còn cần và điều kiện tiếp tục

**Đã duyệt, không hỏi lại:** toàn bộ N2 hẹp nêu ở mục6, gồm M0/M1 logic và không
Save field mới. Không cần chốt tuổi/thọ nguyên/lore/ending/bế quan để thử slice.

Còn thiếu đầu vào thực thi: truy cập máy Windows, xác định clean integration
HEAD/task ownership, asset manifest và artifact mới. Sau N0/N1 cùng native evidence,
chủ dự án giao task N3/N4 hẹp cụ thể; không tự coi duyệt phạm vi là lệnh triển
khai Event System hoặc migration. Nếu hướng triển khai vượt giới hạn no-schema,
cần quyết định/task riêng trước phần vượt đó.

**Điều kiện kết thúc toàn task vẫn chưa đạt:** phải hoàn tất N0/N1 trên Windows,
có bằng chứng build/teardown và N3/N4 được giới hạn theo exact integration HEAD.
Bản Cloud này chỉ hoàn tất ghi nhận N2 và chuẩn bị bàn giao; giữ trạng thái
BLOCKED cho phần local, không mở rộng gameplay.
