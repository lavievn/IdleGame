# Development Batch v0.5 — bàn giao

06/11/2026, Asia/Saigon. **Đã triển khai mã nguồn và hoàn tất Phase 1–8 trong phạm vi module thuần.** Realm, Martial Arts và Boss chưa được tích hợp production/MainGame.

## Git và phạm vi

- Repository `lavievn/IdleGame`; worktree `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Branch `feature/monster-awareness-ambush`; baseline `cf6cb2ce1200d501b71490aab1754aeeb4214be5`, sạch trước khi sửa. Phiên trước idle/completed, không phát hiện writer Unity/Git/test v0.4 khác trong kiểm tra tiến trình.
- HEAD trước commit báo cáo: `54e9b01`; commit chứa báo cáo này là HEAD bàn giao, xem `git log -1`. Sau tám commit v0.5, ahead upstream local dự kiến 19; không fetch/pull để suy trạng thái remote mới.
- Không push/merge/reset/rebase, không đổi branch, không đọc/ghi Save người chơi, không chạy Player hoặc cài package/dependency.
- Các contract thực tế ở `Docs/architecture/`, không có thư mục `contracts/`; đã đọc file đúng tên. Policy A và 1-based do yêu cầu v0.5 phê duyệt, ưu tiên các dòng “proposal/chờ duyệt R1” lịch sử trong v0.4. Chỉ phần module thuần được mở phạm vi, không mở trial/gameplay/Save production.

| Commit | Kết quả |
|---|---|
| `808c9da` | RealmProgressionEvaluator độc lập + boundary/invalid/catalog tests |
| `d42162c` | Explicit breakthrough transition Policy A, duplicate/stale guards |
| `ffd29b9` | Realm DTO/candidate Save fixture chỉ trong Tests~ |
| `a5a2e3b` | Martial definition registry/validator/eligibility |
| `3c50aa6` | Boss entry gate/coordinator, identities và Retry explicit |
| `174b7b0` | Cross-contract regression trên ba nền tảng và dữ liệu Hero/world hiện hữu |
| `54e9b01` | Ba ghi chú kỹ thuật tích hợp từ code thực tế |
| Commit báo cáo này | Báo cáo, không thêm runtime integration |

## Trạng thái tám phase

| Phase | Trạng thái | Phạm vi đạt và giới hạn |
|---|---|---|
| 1 R01 | **IMPLEMENTED / VERIFIED** | Evaluator nhận catalog ID explicit, raw InternalLevel, minor stage/bottleneck/progress eligibility và reason; không dùng RankDatabase hoặc tự đổi realm. |
| 2 Boundaries/transition | **IMPLEMENTED / VERIFIED** | Đủ 11 hàng boundary đã yêu cầu; invalid/integer/cap, explicit successor/success, duplicate/stale và late transitions giữ Level/EXP. Không có Thiên Kiếp MainGame. |
| 3 R02 preparation | **IMPLEMENTED / VERIFIED fixture; BLOCKED production** | DTO riêng, version/candidate guards, repeated-load/roundtrip bằng Mono/Stub, không inference realm từ Level45; không đổi EntityDataSO/SaveManager. |
| 4 M01 | **IMPLEMENTED / VERIFIED** | Registry/definition/weapon/root/unlock/reason validation; không class, không effect/activation, không learned production. |
| 5 B01 | **IMPLEMENTED / VERIFIED** | Gate/coordinator thuần trên snapshot catalog thật; sáu boss nhận diện đúng, production gate vẫn chặn; state/identity/duplicate/stale/Retry. Không boss prefab/loot/EXP/clear thật. |
| 6 Cross-contract | **VERIFIED** | Suite tổng 21 nhóm PASS; không production caller; không sửa file Assets có sẵn; camera/Combat/Save/UI/world-map giữ baseline. |
| 7 Readiness | **PROPOSED integration** | Ba ghi chú ngắn: authority, API/entry points, migration/risk/tests và quyết định chưa duyệt. |
| 8 Handoff | **IMPLEMENTED tài liệu / VERIFIED checks** | Báo cáo, commit riêng, diff/metadata/scope, checkpoint và bản sao bằng chứng để bàn giao. |

## API thực sự đã triển khai

### Realm

[RealmProgressionEvaluator](../../Assets/Script/RealmProgression.cs): constructor nhận danh sách ID explicit và copy riêng; `Evaluate(realmId, realmEntryLevel, totalLevel)` trả `RealmProgressionResult`. Validate catalog/entry/total/cap trước phép trừ; raw InternalLevel = total−entry+1, không modulo/clamp thành30. Stage Sơ1–9/Trung10–19/Hậu20–29/Viên mãn>=30. ProgressEligible không nói successor tồn tại hoặc trial đã thắng.

Không tìm thấy catalog realm chính thức độc lập trong baseline; RankDatabase chỉ là legacy mapping. Không tự tạo catalog production hoặc successor. Tests dùng IDs namespace `fixture:` riêng, không nội dung mới của MainGame.

[RealmBreakthroughTransition](../../Assets/Script/RealmBreakthroughTransition.cs): immutable `RealmProgressionState`, session Current/Revision, `TryApplySuccess(Guid, expectedRevision, explicitSuccessor, out reason)`. Success callback phải tường minh; catalog chỉ kiểm ID successor tồn tại, không quyết định thứ tự/successor hợp lệ theo sản phẩm. Policy A tại Level45 -> realm mới/entry45/internal1, EXP nguyên; Level46→2, Level74→30. In-memory attempt HashSet và revision chặn callback trùng/stale trong instance, không phải persisted ledger. Snapshot không tự theo Level live tăng trong trial; adapter tương lai phải xử lý mốc success hiện tại như ghi chú tích hợp.

### Martial

[MartialArtsRegistry](../../Assets/Script/MartialArtsRegistry.cs): `TryCreate(definitions, out registry, out validation)` publish registry chỉ khi tất cả definition hợp lệ. `TryGet`/`Evaluate(id, context)` trả reason/classification, không mutate learned state hoặc gọi effects. ID namespace exact, revision>=1, enum/duplicate/family/root-tier/unlock validators.

MartialDefinition phân CultivationMethod/MartialArt độc lập Passive/Active, input lists được copy và read-only. Empty weapon list explicit = unrestricted; null policy bị từ chối. Context null family = thiếu thông tin, Unarmed là family explicit. Root minimum tier và unlock fact conditions là AND. Root policy chỉ metadata/eligibility; không tính thêm tương khắc, damage, mana/cooldown hoặc đổi SynergyMath. Không có property class/profession.

### Boss

[BossEncounterGate / BossEncounterCoordinator](../../Assets/Script/BossEncounterCoordinator.cs): gate copy WorldMapNode IDs/flags/links hiện có; `Evaluate(BossEntryRequest)` trả reason/permit từ origin/target/clear/definition/permission/readiness explicit. Không dùng SuggestedLevel120 làm policy unlock.

Coordinator có `TryEnter`, `TryStartFighting`, `TryResolve`, `TryCompleteResolution`, `TryCancel`, `TryRetry`; Idle→Entry→Fighting→Resolving→Won/Lost, hoặc Cancelled. Encounter/attempt IDs bảo vệ callback, Retry phải authorized với fresh gate và attempt mới. `TryCompleteResolution(..., true)` chỉ acknowledgement do caller cung cấp; **không phải bằng chứng disk commit**. TerminalTransitionCount không cấp EXP/loot hoặc đánh dấu normal wave/clear. Won không tự cho repeat. Guard chỉ in-memory, không chống duplicate qua process/load mới.

## File thêm/sửa

- **Assets/Script, thêm:** `RealmProgression.cs`, `RealmBreakthroughTransition.cs`, `MartialArtsRegistry.cs`, `BossEncounterCoordinator.cs` và bốn `.meta` GUID mới. Không thay file Assets có sẵn.
- **Tests~, thêm:** `RealmProgressionRegression.cs`, `RealmTransitionRegression.cs`, `RealmSaveCompatibilityFixture.cs`, `RealmSaveCompatibilityRegression.cs`, `MartialArtsRegistryRegression.cs`, `BossEncounterCoordinatorRegression.cs`, `FoundationCrossContractRegression.cs`.
- **Tests~, sửa:** `MotionRegression.cs` thêm selectors; `run.sh` thêm bốn source module vào compilation hiện có. Không framework test/dependency mới.
- **Docs/architecture, thêm:** [Realm → Save/Load](../architecture/REALM_SAVE_INTEGRATION_V05.md), [Martial → Learned/Loadout](../architecture/MARTIAL_LOADOUT_INTEGRATION_V05.md), [Boss → world-map](../architecture/BOSS_WORLD_MAP_INTEGRATION_V05.md).
- **Docs/automation-reports, thêm:** báo cáo này. Tổng cuối batch: 21 file, gồm 19 file mới và hai file test runner sửa.

## Kiểm thử thực chạy

Mono của Unity6000.5.0f1, runner hiện có `Tests~/run.sh` và UnityStub. Mỗi phase chạy selector liên quan rồi commit. Phase6 chạy suite tổng sau khi tất cả module cùng tồn tại và thêm cross-contract assertion; không cộng các lượt chạy lại thành số test mới.

| Selector | Nhóm PASS cuối | Bằng chứng chính |
|---|---:|---|
| `--realm-progression` | 3 | 11 hàng boundary; int.Min/Max, entry/total invalid, cap999, unknown/exact/copied catalog |
| `--realm-transition` | 3 | Late Level30/31/45/78/998/999, EXP không đổi; duplicate token/stale revision; early/missing/unknown/same successor |
| `--realm-save-fixture` | 3 | DTO JSON roundtrip/repeated loads, version guard, missing realm/entry, no grant for legacy45, candidate không đổi khi reject |
| `--martial-registry` | 6 | Bốn family/missing/mismatch, root tier/unlock, Passive, duplicate/invalid definitions, unknown/context, copied inputs và learned fixture giữ nguyên |
| `--boss-coordinator` | 5 | Catalog133/sáu boss exact, normal nodes, gate failures, state/terminal ack, duplicate/stale/Cancel/Retry, snapshot isolation |
| `--foundation-contracts` | **21 tổng** | 20 nhóm trên + cross-contract: Hero data/roots/gear/EXP/stat source/world snapshot không đổi |

Tất cả C# lượt kiểm thử trên đạt từ lần chạy đầu, không nới assertion/tolerance, không cần vòng sửa code để ép PASS. Compilation có 16 warning ở file test/production cũ theo đường dẫn log, không có warning từ bốn module mới. Không sửa cảnh báo ngoài phạm vi.

Scope checker lúc đầu false-positive vì tìm `EntityDataSO` trong comment mô tả cap. Sửa checker bỏ comment; lần sửa đầu bị PowerShell quoting nên chưa áp dụng, sau đó sửa bằng patch và chạy lại **PASS**. Không sửa assertion C# hoặc source production vì lỗi công cụ kiểm tra này. Checkpoint đã ghi lần rerun thực sự đạt.

Metadata checker: **772 file, 0 error, 3 warning** folder-meta Lake/Sea/Steppe có sẵn. Bốn script mới có `.meta` riêng; không mở Unity để sinh/import lại artifact. Scope check không có caller production cho module mới; diff Assets chỉ tám file mới gồm source/meta. 15 file được kiểm riêng gồm EntityDataSO, SaveManager, GameManager, CombatManager, TuTienEnums/root math, WeaponBasicAttack, WorldMapCatalog/catalog text, MainGame, camera/motion, damage, production loot, Forge và UIManager đều nguyên baseline. Git diff/staged checks và liên kết tài liệu đạt.

**NOT RUN:** Unity Editor compilation/Edit/Play Mode, JsonUtility native, Windows Player/build/native input, full regression cũ không liên quan. Mono/Stub PASS không chuyển thành Unity/Player PASS. DTO JSON fixture không chứng minh parser malformed JSON hoặc Save production đã sẵn sàng.

## Tích hợp và phần chưa tích hợp

**Đã tích hợp:** bốn POCO source được compiler test runner biên dịch cùng code cũ; selectors mới vào hệ test. Gate test đọc catalog thật bằng Resources stub. Ba nền tảng chưa có MonoBehaviour, startup hook, manager trung tâm hoặc caller MainGame.

**Chưa tích hợp:** realm authority/catalog/Thiên Kiếp production, synchronization Level/EXP với session, field Save realm, learned/rank/loadout persistence, passive/active effects, boss spec/prefab/spawn/reward transaction, mở travel boss. Không thay Combat lifecycle/AI/range/spawn/camera v0.3/UI/Forge/Loot/HP theo mode/Save schema hoặc EXP formula.

## Quyết định còn chờ và tối đa ba task tiếp theo

1. **R02 production — BLOCKED:** duyệt migration save Level cao, downgrade, cảnh giới cuối/cap999 và Save khi đang Thiên Kiếp; đồng thời cung cấp catalog/successor authority chính thức. Sau đó mới nối guards/candidate load/commit, kiểm native JsonUtility và failure recovery. Không mặc định Level45 đã đột phá.
2. **M02 learned/loadout — PROPOSED:** duyệt definitions thật, learning/upgrade/cost, số slot, unknown-ID/downgrade và selection policy; mới thiết kế component/persistence. Không đưa các ví dụ v0.4 thành production skill hoặc thêm mana/CD theo mặc định.
3. **Boss adapter/reward transaction — BLOCKED:** duyệt unlock/spec/failure/Retry/exit, first-clear/repeat/reward/EXP/cap và checkpoint/rollback; sau đó thiết kế durable ledger/candidate transaction trước mở một boss. Logical Won trong fixture không đủ để mở gate.

## Bằng chứng và checkpoint

Workspace: `C:\Users\hason\Documents\Codex\2026-11-05\ti-p-t-c-d-n`.

- `work/v05/CHECKPOINT.md` ghi Phase1–8 và nơi tiếp tục nếu gián đoạn; `work/v05/*.log`, `scope-check.json`, `metadata.json` là bằng chứng gốc.
- `outputs/v05/DEVELOPMENT_V05_REPORT.md`, `outputs/v05/integration/`, `outputs/v05/evidence/`: bản sao để mở từ chat, gồm logs Mono và checks. `handoff.json` ghi HEAD/status thực tế sau commit cuối.
- `work/v05/verify_scope.py` có baseline `cf6cb2c` và whitelist modules; bản sao trong evidence cho phép xem/reproduce kiểm tra nguồn trên đúng baseline.

Đã hoàn thành toàn bộ công việc độc lập an toàn trong batch, ghi báo cáo và dừng. Không tự nối production hoặc mở task tiếp theo.
