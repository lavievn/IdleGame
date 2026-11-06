# Cultivation Gameplay v0.7 — MainGame integration

06/11/2026. **IMPLEMENTED & VERIFIED trong MainGame Play Mode qua profile cô lập:** New Hero → hấp thu Qi → reserve → luyện hóa → tiểu cảnh giới → bình cảnh → reserve tiếp tục tăng → Save/Load → tiếp tục. Level/EXP vẫn tăng độc lập; không tự breakthrough. **Chưa phát hành production balance hoặc migration Save cá nhân.**

## Git và cách chơi thử

- Repo `lavievn/IdleGame`, branch `feature/monster-awareness-ambush`.
- Worktree thực tế `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame` (đường dẫn trong yêu cầu thiếu dấu `\` trước `.codex`).
- Baseline `5cc3f47684f87f1e6753bad17909cbf80fa704a6`, clean, không thấy Unity/Git/Mono/Python writer khác trước sửa.
- Source HEAD MainGame nghiệm thu `9094169`; verifier native cuối `965e427` chỉ thêm test I/O, runtime không đổi. Commit tài liệu bàn giao theo sau Source HEAD; full final SHA/status nằm trong `outputs/v07/handoff.json` ngoài repository. Không push/pull/merge/reset/rebase/force-push.

Trong Unity6000.5.0f1, chọn **Bach Luyen → Playtests → Cultivation v0.7 (isolated)**, sau đó **Chơi mới → xác nhận → Chỉ số/Nhân vật**. Mục này mở bản copy MainGame bị Git ignore, thư mục `.utmp/cultivation-playtest-<GUID>` có marker, cấu hình SaveDirectory trước Awake. Có thể Save/Load/Travel/Retry bằng UI sẵn có. Stop Play như bình thường; muốn quay lại profile cũ phải dùng đúng config/version và thư mục sở hữu, không chuyển slot sang Save cá nhân. Menu khởi tạo profile mới mỗi lần mở để tránh dùng nhầm dữ liệu cũ. Scene copy giữ serialized profileRoot: sau Stop có thể giữ scene copy đó, bấm Play và chọn Continue để dùng lại profile; đường Stop/Play thủ công này chưa được nghiệm thu bằng native input trong batch.

MainGame mặc định vẫn không có playtest profile. JSON cấu hình ở `Assets/Editor/CultivationPlaytestV07.json`; không trở thành asset balance production hoặc dependency Player. Không cần sửa logic để chỉnh dữ liệu thử; **phải tăng configuration version khi đổi định nghĩa tương thích Save**. Snapshot khác ID/version bị reject, không tự migrate/rebase tu vi.

| Commit | Chức năng |
|---|---|
| `60497e9` | Profile có version, stable IDs, realm/threshold/source validation |
| `27aa4e5` | Canonical binding, clock Qi, component Save có guard, Character UI và Mono regression |
| `9094169` | Editor launcher/bootstrap, native write-fault fixtures và MainGame acceptance |
| `965e427` | Ca I/O thật: temp creation/Windows locked Replace, so checksum snapshot đã commit |

## Audit ngắn và các phase

Authority Level/EXP/points/growth là EntityDataSO/AddExp. V0.6 chỉ có domain session/DTO và gate false, chưa có clock Qi. GameManager đã sở hữu New/Load/Retry/Travel/Menu; TryLoadSlot load candidate trước cancel/swap. UI dùng GameManager.HeroData hiện tại. SaveManager đã ghi temp + Replace/Move đồng bộ trên main thread.

Thời gian hiện có: Environment/Combat dùng scaled delta cho movement/combat, GameManager.Update dùng unscaled delta cho entry/VFX, LateUpdate handoff/exploration; AutoSave coroutine và UI có lịch riêng. Không vòng nào trước v0.7 cấp Qi. V0.7 thêm **đúng một caller Qi** trong GameManager.LateUpdate trước entry handoff, không tạo MonoBehaviour scheduler hoặc coroutine Qi thứ hai.

| Phase | Trạng thái | Kết quả |
|---|---|---|
| 1 Baseline/audit | VERIFIED | Mono19 domain +16 related trước sửa; authority/clock/lifecycle traced |
| 2 Configuration | IMPLEMENTED / VERIFIED | Immutable validated playtest profile, explicit successor rỗng, nguồn node/fallback riêng |
| 3 Save/Load | IMPLEMENTED / VERIFIED trong owned playtest | Version/config guard, candidate-before-swap, native roundtrip/faults, legacy zero-state explicit |
| 4 Canonical session | IMPLEMENTED / VERIFIED | GameManager bind/retire, một clock, pause/entry/death/menu/disable không tick cũ |
| 5 Realm/bình cảnh | VERIFIED | Sơ/Trung/Hậu/Viên mãn theo căn cơ, cap giữ reserve; Level Up vẫn hoạt động |
| 6 World Qi | IMPLEMENTED / VERIFIED | Hai node + fallback từ profile, Travel thay density, catalog/topology/rules không đổi |
| 7 UI | IMPLEMENTED / VERIFIED | Level/EXP/realm/stage/bar/reserve/rates/energy, blood/trial khóa; bốn kích thước |
| 8 Gameplay acceptance | VERIFIED MainGame Play Mode | Bốn check groups bao phủ16 tiêu chí trong phạm vi profile; xem fixture limits bên dưới |
| 9 Regression | VERIFIED chọn lọc | Mono5+19+16+4, native Save5, MainGame4; source bảo vệ không đổi |
| 10 Handoff | IMPLEMENTED | Contract cập nhật, báo cáo, commit/checkpoint/evidence ngoài Git |

## Profile và world Qi

`CultivationPlaytestProfile.TryCreate` yêu cầu schemaVersion1, ID `playtest:*`, version≥1, realm/name/starting ID hợp lệ, thresholds tăng nghiêm ngặt, successor explicit nếu có, node known/không trùng, rates hữu hạn/không âm, frame/step bounded. Profile copy dữ liệu thành authority bất biến; không giữ reference tới array mutable từ JSON.

Profile hiện tại `playtest:cultivation-v07`, version1:

| Thông số thử | Giá trị |
|---|---|
| Realm | `realm:luyen-khi`, tên Luyện Khí, tier0, không successor |
| Thresholds | `[0,6,12,20]` cho Sơ/Trung/Hậu/Viên mãn |
| Qi sources | `TV-20=3`, `TV-18=5`, fallback explicit2 cho node khác/legacy |
| Absorption | rate1, efficiency1 |
| Refinement | input rate1/s, efficiency1 |
| Clock | step0.1 game-second, max frame0.5s; >max/âm/NaN/infinity reject nguyên frame |
| Spiritual Energy | Max100, recovery2/s; không có ability tiêu hao mới |
| Blood/injury effects | Blood locked/rate0, không thêm heal/treatment hay combat modifier runtime |

Đây là giá trị playtest để thấy tiến triển nhanh, không phải cân bằng chính thức. Save chỉ ghi configuration identity/version và state, không ghi thresholds/density/rate làm định nghĩa bất biến. Không gán random density cho133 nodes. WorldMapCatalog không chứa metadata Qi trước đây; adapter lấy stable node ID rồi tra bảng profile. Enemy strength/danger/SuggestedLevel/Spawn/EXP/drop không phụ thuộc bảng này và không bị sửa.

## Canonical state, lifecycle và clock

GameManager.Cultivation giữ `CultivationGameplayBinding` cho Hero canonical; binding sở hữu domain CultivationSession, chỉ expose snapshot bất biến. Owner Guid lưu theo snapshot; epoch Guid mới khi bind lại. Mutable session/Burning/breakthrough command không được đưa ra UI/gameplay.

Mỗi fixed step: Tick domain v0.6 → capture component JSON → publish vào `HeroData.cultivationJson` → isDirty. Save có cùng snapshot vừa hoàn tất. Một binding xác minh Hero reference/frame/serialized snapshot; duplicate frame, invalid owner hoặc disposed không cấp tài nguyên. Không có kill subscription, offline tick hoặc callback async cấp Qi. MainGame entry pause không bị unscaled VFX/UI cấp thêm Qi.

New tạo owner/session zero-state, bỏ dữ liệu template. Load chuẩn bị binding candidate trước thay Hero và dispose binding cũ sau accepted candidate. Death/Disable/Menu/Destroy dispose; Retry/Enable phục hồi từ snapshot với epoch mới. Travel giữ session hiện tại và đổi source theo node mới, entry vẫn pause. Save giữ reserve, không consume; Load nhiều lần không cộng lại. Phần thời gian substep chưa thành state (<0.1s) chỉ ở binding, bỏ khi rebind; không có bù thời gian/offline. Đây là policy playtest, cần cân nhắc trước production.

Đạt cap20 vẫn hấp thu, input chưa luyện hóa giữ trong reserve. Domain không đổi actual realm, successor rỗng và không có trial event được nối. Level Up fixture tại cap vẫn tăng Level/points theo AddExp hiện hành, snapshot cultivation không đổi. Không sửa công thức HP/growth/damage hoặc cộng realm stat bonus.

## Schema, migration và transaction safety

Thêm đúng một field Hero: `string cultivationJson`, chuỗi rỗng là component absent. Chuỗi chứa JSON của CultivationComponentDto v1 + `configurationVersion`. Dùng string có chủ đích để tránh JsonUtility inline-class null/default inheritance và alias DTO mutable từ template. Đây là bản materialize để Save của session canonical; không có copy riêng cho UI.

Payload gồm config ID/version, owner, actual realm, foundation, reserve, current energy, blood charges/progress, injury, burning/deferred fields và receipts. Không lưu minor stage, capacity, Max Energy, effective realm, thresholds hoặc EXP trong component. `configurationVersion=0` của fixture v0.6 không được adapter gameplay chấp nhận.

`CultivationSaveJson` đọc cấu trúc/presence/type và reject duplicate keys/malformed/trailing/depth quá32 trước JsonUtility. `CultivationGameplayState.TryRestore` kiểm component version, profile identity/version, catalog, invariants và domain candidate. Profile v0.7 reject mọi Burning/charges/progress/receipts; không cố bỏ nợ để Load. Hero thiếu component trong Save source của playtest bị từ chối, không ghi snapshot giả trống.

`SaveManager.LoadGame` preflight component trước overwrite, reset field template rồi dùng payload đã validate. GameManager chuẩn bị candidate binding trước cancel gameplay/swap Hero. Failed component Load giữ Hero/session/life hiện tại. Direct generic SaveManager Load vẫn là API nạp vào destination candidate; không tái kiến trúc rollback mọi field cũ.

Legacy được phân biệt bằng thiếu field/chuỗi rỗng. **Chỉ khi đã explicit ConfigureCultivationPlaytestBeforeAwake(profile, initializeLegacy:true) trong custom owned directory**, candidate legacy mới nhận Luyện Khí, foundation/reserve/energy/charges/injury0. Không đọc Level/EXP để suy tu vi; giữ Level/EXP/points/roots/gear/world. Load không tự ghi lại file legacy. Save mới sau lựa chọn người thử mới chứa component. New Hero có đường khởi tạo riêng. Production legacy migration vẫn **BLOCKED/OFF**; không đọc/nâng cấp Save cá nhân trong batch.

Writer tái sử dụng temp + File.Replace/File.Move, bổ sung flush writer và FileStream.Flush(true). Guard source invalid và existing future/khác config/component ngăn ghi đè. Context không bật profile không được mở/ghi mất component của profile khác. Dữ liệu root malformed cũng được bảo vệ bảo thủ thay vì tự bỏ phần chưa hiểu. Lỗi BeforeWrite/AfterTempWrite/BeforeReplace giữ file final; kiểm thêm temp path bị chặn bởi thư mục rỗng sở hữu và Windows file đích mở không cho delete sharing, cả hai lỗi I/O thật giữ nguyên byte snapshot đã commit; orphan temp bị Load bỏ qua và write tiếp có thể thay. Không thêm backup rotation/ledger hoặc đổi Manual Save rollback.

**Giới hạn:** fault injection mô phỏng lỗi/gián đoạn trước commit trên máy hiện tại, không chứng minh mất điện hay mọi filesystem. Không test kill process đúng giữa syscall Replace. Không có Burning durable transaction; không nhận active debt trong gameplay adapter. Binary cũ trước guard có thể bỏ field mới khi save lại, không bảo đảm downgrade. Đổi config ID/version cần migration được duyệt hoặc profile khác; không auto chuyển foundation sang bảng mới.

## UI và bằng chứng MainGame

Character dùng HeroData và binding hiện tại của GameManager. Hiển thị `[PLAYTEST v1]`, Total Level/EXP, realm thật, minor stage/bình cảnh, foundation số + bar, reserve, absorption/refinement limit và spiritual current/Max. Blood và Thiên Kiếp là nhãn khóa, không có nút giả. Allocation/pager/close và nền mặc định giữ nguyên. Character polling0.2s bằng unscaled UI time, immediate refresh khi mở/re-layout; không tick tài nguyên bằng UI. Bar cập nhật cùng polling.

MainGameFoundationProbe `-foundationCultivationV07` thực sự chạy scene MainGame copy, manager Awake trong Play Mode và SaveDirectory riêng trước Awake. Không gọi Advance/Tick để tạo tiến triển nghiệm thu; chờ GameManager clock theo thời gian thật. Đã quan sát idle, movement, combat với damage trace tự nhiên, bốn stage, pause, reserve tại cap, real Save/Load/repeated Load, future Load thất bại khi đang combat/Character mở, Travel đổi source, defeat/Retry, Menu/Continue và New sau template bị cố ý làm bẩn. Camera/combat identity được assert sau Retry.

Fixture giới hạn rõ: start node TV-20 được chọn explicit; idle clear quái ban đầu và đặt actor speed0 rồi restore; Travel được cấp clear permission fixture, không chứng minh đã farm tự nhiên đủ5 waves; defeat qua incoming damage fixture; EXP fixture làm một Level Up tại bình cảnh, không phải farm dài hạn. Quan sát zero actor steps dùng MovementFrame mới, không so screen/BattleArea displacement khi camera đang settling. Không sửa camera/movement để đạt PASS.

## Kiểm thử và evidence

| Môi trường | Kết quả |
|---|---|
| Pure domain Mono/UnityStub `--dual-progression` | PASS19 nhóm v0.6, vẫn giữ debt/recovery/breakthrough fixture |
| Integration logic Mono/UnityStub `--cultivation-gameplay` | PASS5 nhóm; config invalid, clock30/60/144fps, candidate/config guard, Level independence,10000ticks hữu hạn |
| Related Mono `--realm-related` | PASS16 nhóm Save/Load/New/rollback/balance hiện có |
| UI Mono `--progression-ui` | PASS4 nhóm, đường mặc định không bật profile giữ gate cũ |
| Unity6000.5.0f1 native Edit Mode `CultivationV07Verifier.RunNative` | PASS5 check groups: current/legacy/template, repeated Load, invalid/future/config protection, injected faults và actual temp/Replace I/O failures |
| Real MainGame Play Mode Source9094169 | PASS4 check groups, phase1–4 PASS; UI600×338/800×450/1150×647/1360×680 |
| Unity compilation | VERIFIED cho source mới và các module hiện có; không suy từ Mono PASS |
| Metadata/scope | 0 errors,3 baseline Lake/Sea/Steppe folder-meta warnings; protected source/diff check PASS |
| Windows Player/native OS input/long natural farm/power-cut | NOT RUN |

Final native profile `.utmp/cultivation-playtest-254b357aeebf4d049533f5f9dba70d94`; MainGame `.utmp/foundation-d0e882009439440e85e5d348603d2c83`. Result JSON, chọn lọc log, bốn ảnh Character và ảnh Sơ kỳ/New nằm ngoài Git tại `outputs/v07/`; không copy/commit raw Save fixture. Profile GUID/marker và path/reparse validation được dùng trước I/O. Không Save cá nhân nào được dùng để kiểm thử.

Các vòng chẩn đoán: Mono harness definite assignment sửa một lần; native Edit harness tách Awake/DontDestroyOnLoad khỏi Edit Mode (Play Mode kiểm Awake thật), current-balance fixture khởi tạo sai thứ tự được sửa giữ kỳ vọng EXP17/points11. Idle assertion đầu so tọa độ chịu camera pan; log xác nhận F0→1/reserve0→2/EXP0/speed0, đổi sang zero fresh actor steps và sample count>0 để kiểm đúng invariant. Có mẫu MovementFrame stale trước pause được loại theo thời điểm bắt đầu phép đo. Không sửa gameplay, giảm assertion tài nguyên hoặc vượt5 vòng cho một vấn đề độc lập. Final safety assertions thêm Level Up tại cap và reject missing component đã chạy lại thành công. Ca I/O thật ban đầu so với checksum trước một lần Save hợp lệ làm isDirty đổi true→false; sửa baseline về file committed ngay trước fault, kèm assertion tất cả field khác giữ nguyên. Lỗi definite assignment của biến checksum trong harness cũng đã được sửa; không sửa writer/giảm assertion để vượt ca này.

## File thay đổi và ranh giới

- Thêm `Assets/Script/CultivationPlaytestProfile.cs`, `CultivationSaveJson.cs`, `CultivationGameplayState.cs`, `GameManager.Cultivation.cs`, `SaveManager.Cultivation.cs`, `UIManager.Cultivation.cs`, `CultivationPlaytestBootstrap.cs`, `MainGameFoundationProbe.CultivationV07.cs` và `.meta`.
- Thêm `Assets/Editor/CultivationPlaytestV07.json`, `CultivationPlaytestLauncher.cs`, `CultivationV07Verifier.cs` và `.meta`.
- Sửa `Assets/DataScript/EntityDataSO.cs`; `Assets/Script/GameManager.cs`, `SaveManager.cs`, `UIManager.cs`, `CultivationPersistence.cs`, `CultivationIntegrationGate.cs`, `MainGameFoundationProbe.cs`.
- Thêm `Tests~/CultivationGameplayRegression.cs`; sửa `MotionRegression.cs`, `run.sh`, `UnityStub.cs` (string escaping/double support trong test double, không thay JsonUtility thật).
- Cập nhật Dual Progression/Blood contract và thêm báo cáo này. Danh sách exact name-status có trong evidence.
- Domain engine/evaluator/breakthrough/blood/recovery v0.6, CombatStats/formula, Camera v0.3/EnvironmentManager, Hero/Monster AI/Spawn, Root synergy, Forge/Loot, WorldMapCatalog/topology và MainGame scene **không đổi**. Font fallback và15 `.meta` import Unity được dọn đúng allowlist/hash, không commit cache/log/save.

## BLOCKED, phạm vi chơi được và ba bước tiếp theo

**Chơi được:** vòng Qi/tu vi/bình cảnh/Save/Load trong MainGame Editor qua owned playtest, dùng UI và lifecycle thật. **Chỉ fixture:** balance hiện tại, permission Travel/defeat/EXP acceptance và breakthrough domain v0.6. **BLOCKED/OFF:** production catalog/rates, migration player saves/config upgrade/downgrade strategy, durable Burning settlement, blood capacity/rates, injury balance, Thiên Kiếp/realm successor thật. Không mở Burning/Thiên Kiếp/boss/active skill trong batch này.

1. Duyệt một production config và migration/upgrade/downgrade policy, gồm substep time loss khi lifecycle rebind; chưa thay133-node balance toàn bộ.
2. Mở persistence/config compatibility cho production sau acceptance fixture và nghiệm thu Editor từ menu playtest bằng người dùng; bổ sung restart/process-interruption kiểm soát nếu yêu cầu phát hành.
3. Nghiệm thu Windows Player/native input và một phiên farm tự nhiên hữu hạn với profile được duyệt; vẫn giữ Burning/Thiên Kiếp tắt cho đến batch persistence/lifecycle riêng.
