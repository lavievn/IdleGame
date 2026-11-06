# Development v0.6 REVISED — Dual Progression

06/11/2026. Đã triển khai nền tảng C# thực tế cho tu vi độc lập, Qi, linh lực, Tinh Huyết, khoản nợ, injury, recovery, breakthrough và candidate Save fixture. **Tu luyện/Burning chưa hoạt động trong MainGame.** Gate production giữ đóng vì thiếu catalog/balance và giao dịch lưu khoản nợ an toàn. Không dùng kết quả module để tuyên bố hoàn thành gameplay.

## Repository và checkpoint

- Repository `lavievn/IdleGame`, branch `feature/monster-awareness-ambush`.
- Worktree `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Baseline thực tế `d07d7700aeb5511eba815c78ffe60a815cf6b35f` (old v0.6), không phải mốc v0.5. Working tree sạch khi bắt đầu revised; không có Unity/Git/Mono/Python writer khác được tìm thấy. Không reset công việc v0.6 cũ.
- HEAD mã nghiệm thu `f8d9af8`. Commit tài liệu chứa báo cáo này đi sau HEAD đó; SHA bàn giao đầy đủ được ghi trong `outputs/v06-revised/handoff.json` ngoài repository để tránh self-reference hash.
- Chỉ dùng Unity6000.5.0f1 đã cấu hình và Mono của Unity; không cài dependency. Không push/merge/reset/rebase. Không thao tác sleep/shutdown trong batch revised.

| Commit | Nhóm hoàn thành |
|---|---|
| `411d8b6` | Foundation/Qi/energy, evaluator và rules cấu hình |
| `b3d2629` | Burning/deferred settlement, source modifiers, injury/recovery |
| `5127560` | Breakthrough tu vi và đổi API Level cũ thành LegacyLevel* |
| `2532f00` | DTO/version guards/candidate load/khôi phục khoản nợ |
| `dfa126c` | Tiêu tiến độ Tinh Huyết đúng một lần, chống stale modifier epoch |
| `5bcafd0` | Loại Level-derived authority khỏi MainGame, giữ snapshot v1 compatibility |
| `c79fcaa` | Unity native verifier và MainGame gate verifier |
| `f8d9af8` | Pause/catalog assertions, chỉ đánh PASS cho phase verifier thực sự chạy |

Checkpoint từng nhóm được ghi ngoài repo tại `work/v06r/CHECKPOINT.md`. Code đã commit, test state không phải nguồn authority để tiếp tục phát triển.

## Kết quả Phase 1–12

| Phase | Trạng thái | Kết quả và giới hạn |
|---|---|---|
| 1 Dual progression | IMPLEMENTED / VERIFIED | Năm component bất biến; EXP/Level/points/growth độc lập hoàn toàn với session tu luyện |
| 2 Evaluator v2 | IMPLEMENTED / VERIFIED | Threshold catalog validated; stage tiến/lùi theo căn cơ; actual realm không tự tăng/giảm |
| 3 Qi | IMPLEMENTED / VERIFIED | Density/absorption/refinement riêng; reserve giữ ở bình cảnh; pause/invalid dt/overflow/duplicate step guard. Chưa có map density hoặc scheduler production |
| 4 Spiritual Energy | IMPLEMENTED / VERIFIED | Spend/Restore/Clamp và injury-derived Max; không tiêu căn cơ; chưa có pool/skill mới trong game |
| 5 Accumulation | IMPLEMENTED / VERIFIED | Capacity catalog, progress/rate/acceleration, hold-excess fixture; thiếu balance/capacity policy production |
| 6 Burning | IMPLEMENTED / VERIFIED domain | HP<30%, heal đầu, top-up30s không heal/restack, effective realm combat-only, modifier allowlist. BLOCKED live activation |
| 7 Deferred cost | IMPLEMENTED / VERIFIED domain | Reference/rate khóa, linear debt, explicit pending settlement, receipt/epoch chống trùng/stale; không có durable production transaction |
| 8 Injury/recovery | IMPLEMENTED / VERIFIED | Thiếu căn cơ mới sinh injury; regain/heal/energy tách biệt; nurturing/treatment fixture. Chưa tạo tài nguyên chữa trị thật |
| 9 Breakthrough | IMPLEMENTED / VERIFIED | Success explicit và successor/transfer input; reserve nuôi ba lần liên tiếp; không có Thiên Kiếp hoặc auto success |
| 10 Save/Unity/gate | PARTIAL | DTO/candidate + Unity compilation/native JSON VERIFIED. Schema mới PROPOSED, migration/durable transaction BLOCKED. MainGame chỉ nối gate và bỏ suy luận cũ |
| 11 Matrix | VERIFIED trong phạm vi bên dưới | Domain/selected Mono/native JSON/gated MainGame đã chạy; live cultivation lifecycle/Windows Player NOT RUN |
| 12 Handoff | IMPLEMENTED | Hai contract, báo cáo, commit, evidence, checkpoint; còn các quyết định production liệt kê bên dưới |

## API và authority thực tế

Namespace `TuTienCore`:

- `CultivationFoundation`, `UnrefinedQiReserve`, `SpiritualEnergy`, `BloodEssenceState`, `FoundationInjuryState`, `CultivationSnapshot`: state domain, chưa là field Hero production.
- `CultivationRealmEvaluator.TryCreate/Evaluate`: stable actual realm + foundation + ngưỡng explicit → stage/bình cảnh/capacity/tier/reason. Không nhận Total Level, không dùng RankDatabase.
- `CultivationRules`: explicit bounded game-time/rates/efficiencies/Max Energy/debt/factor/injury/excess-charge policy; không có default MainGame balance.
- `CultivationSession.Tick`: một simulation step theo owner/epoch; hấp thu, reserve, refinement, recovery và passive accumulation; không xử lý offline. Host phải có duy nhất một clock/session canonical khi tích hợp.
- `SpendSpiritualEnergy`, `RestoreSpiritualEnergy`, `ClampSpiritualEnergy`, `HealInjury`, `InjuryTreatmentSession`: ba loại phục hồi riêng; không có Restore chung.
- `ActivateBlood`, `ExtendBlood`, `EndBlood`, `SettleBlood`, `RealmTierFor`; `BloodEssenceModifiers.Reconcile`: transaction state, output heal lần đầu, chi phí hoãn, source ownership và eligibility combat có opt-in.
- `RealmBreakthroughTransition.ApplySuccess`: owner/epoch/event/revision/actual realm + successor/căn cơ sau thành công explicit; không chọn successor hoặc quyết định Thiên Kiếp thành công.
- `CultivationComponentCodec.Capture`, `CultivationCandidateSlot.TryLoad`: DTO version/configuration, validation trước swap; không phải writer Save production.

MinorStage/Capacity/Max Energy/EffectiveCombatRealm không được lưu như authority. Không cộng bonus realm. Modifier thử nghiệm chỉ allow Attack/MagicalAttack/Armor/MagicResistance/HealthRegeneration/KillHealBonus; không nhận MaxHealth/speeds/percent stats. CombatStats/damage formula giữ nguyên.

## Thay thế v0.5 và bảo toàn v1 Save

Evaluator/state/transition cũ được đổi tên `LegacyLevelRealmProgressionEvaluator`, `LegacyLevelRealmProgressionState`, `LegacyLevelRealmBreakthroughTransition`. `RealmProgressionAuthority` đổi thành `LegacyRealmSnapshotCompatibility` trong file/GUID cũ. API đánh giá Level cũ có tên `TryEvaluateLegacyLevelSnapshot`, chỉ dùng fixture lịch sử và code verifier đã retired; MainGame không gọi nó.

Giữ ba field v1 `realmComponentVersion`, `realmId`, `realmEntryLevel` cùng giá trị/tên/format để không phá snapshot đã có từ phiên cũ. GameManager New và SaveManager chỉ dùng compatibility helper. Guard v1 vẫn kiểm version/ID/entry; không tính tiểu cảnh giới khi validate. Không suy realm/tu vi/charges từ Level cao hoặc EXP cũ. Không đổi version Inventory/Equipment.

`RealmMinorStage`, MartialArtsRegistry, BossEncounterCoordinator, WorldMapCatalog, WeaponFamily, CombatStats vẫn tái sử dụng được và không đổi nghĩa. Root evaluator, Camera v0.3, AI/Spawn, Forge/Loot, Combat formula, world topology và scene MainGame không bị sửa so với `d07d770`.

## MainGame trước/sau

Trước revised, Character lấy nội bộ/stage từ `currentLevel - realmEntryLevel + 1`. Sau revised, `CultivationIntegrationGate.ProductionEnabled=false`; Character hiển thị Level riêng, Luyện Khí từ metadata v1 và `Tu vi chưa cấu hình` / `Tu luyện mới chưa kích hoạt`. Không còn dòng nội bộ/stage/bình cảnh suy từ Level. Level45 không chứng minh đã luyện tu vi hoặc đạt Viên mãn.

Thay đổi runtime chỉ gồm presentation gate và đổi helper compatibility. New Hero/EXP/points/growth/Save v1 tiếp tục theo logic đã có. **Không có** Qi density production, cultivation scheduler, linh lực skill, passive blood, Burning combat, forced ability, trial hoặc breakthrough MainGame. Chưa đưa cấu hình tạm vào133 map nodes.

## Save risks và policy

DTO fixture dự kiến version1: config/owner/actual realm, foundation/reserve/current energy/injury, charges/progress, burning identity/reference/rate/uses/remaining/status/end reason/actual realm và settled receipt IDs. Không thêm field này vào EntityDataSO hoặc JSON Hero production.

Candidate Load kiểm version/configuration/owner/fresh epoch/canonical invariants trước publish. Future/unknown/negative/nonfinite/hidden debt/receipt sai đều reject, giữ session cũ. Missing/version0 → `LegacyMigrationRequired`; chưa duyệt migration, không tặng căn cơ từ EXP. Không mặc định template thành dữ liệu hợp lệ mới.

Recovery fixture đưa Active thành Pending với lý do Load/CrashRecovery, không xóa nợ, tick offline, heal hoặc tự settlement. Reference/cost/receipt tồn tại qua native JsonUtility roundtrip; epoch mới từ chối callback cũ. Đây **không phải** bảo đảm durable/rollback-safe ở production: chưa có disk writer và host transaction liên kết HP/charge/debt. Chưa test failed-write/crash giữa các bước của writer mới vì writer chưa được phép tích hợp. Receipt list/command IDs cần chính sách retention khi phát hành.

Manual Save rollback vẫn phục hồi cả timeline cũ; không có ledger ngoài slot. Executable cũ có thể bỏ field mới khi save lại: không tuyên bố downgrade bảo toàn trạng thái. Chính sách migration legacy, Save khi Active, crash/load settlement, transfer căn cơ sau success, excess-charge và durable lifecycle còn BLOCKED.

## Kết quả kiểm thử

| Môi trường | Lệnh/verifier | Kết quả |
|---|---|---|
| Mono/UnityStub | `Tests~/run.sh --dual-progression` | PASS19 nhóm:6 core,7 blood/recovery,3 breakthrough,3 persistence |
| Mono/UnityStub | `--progression-ui` | PASS4 nhóm, gate không suy stage từ Level, canonical UI/points |
| Mono/UnityStub | `--realm-related` | PASS16 nhóm hồi quy Save/Load/rollback/New hiện có |
| Mono/UnityStub | `--legacy-realm-integration` | PASS2 nhóm snapshot v1/cap999; lịch sử, không là chứng cứ model mới |
| Unity6000.5.0f1 native | `DualProgressionV06Verifier.RunNative` | PASS3 check groups: compile Assembly-CSharp; owned JSON roundtrip debt/receipt; invalid candidate reject |
| Unity6000.5.0f1 Play Mode | `MainGameFoundationProbe.RunIsolated -foundationValidationRun -foundationDualProgressionV06` | PASS2 check groups: New/AddExp/UI4 sizes; Save/Load v1/canonical replacement và không có schema mới |
| Metadata/scope | read-only project checker và protected-file diff | 0 errors,3 baseline folder-meta warnings Lake/Sea/Steppe; protected source unchanged; diff check PASS |
| Windows Player/natural long farm/live Burning | Không chạy | NOT RUN; không cần Player để nghiệm thu module thuần |

Domain matrix gồm: invalid/duplicate thresholds/IDs/tiers/capacities; stage regression; Qi accounting và reserve/bottleneck; invalid/max delta/overflow/duplicate steps/pause; spiritual separation; blood HP/owner/charge; top-up/time/source; locked linear debt dù growth; injury deficit; mọi terminal reason và stale receipt; pause Burning/treatment; recovery không xóa nhầm tài nguyên;3 breakthrough riêng từ reserve; future/unknown/invalid/hidden DTO và candidate-before-swap. Một clock canonical và actual death/Retry/Travel/Menu hooks cho tu luyện mới chưa được tích hợp nên không được coi đã nghiệm thu runtime.

Native fixture cuối: `.utmp/dual-v06-ea502704535947218b7785425ee0c732`; MainGame cuối: `.utmp/foundation-49e94f89b60a40dca4c89f7be4b12114`. Tất cả có GUID/marker, Save directory riêng trước Awake; không đọc/ghi/nâng cấp Save cá nhân. Kết quả cuối MainGame chỉ phase1/2 PASS, phase3/4 NOT RUN. Các label cũ của shared runner không được diễn giải thành hệ gameplay mới.

UI đã kiểm kích thước thực600×338,800×450,1150×647,1360×680, không overflow progression text hoặc mất nút allocation/close. Screenshot từ cùng fixture; quan sát visual không thay nghiệm thu native input. EXP cấp xác định qua fixture AddExp, không phải bằng chứng farm dài hạn.

## Vòng sửa–kiểm thử và artifact

- Core round1 phát hiện energy restore tái tạo wrapper foundation dù không thay dữ liệu. Giữ assertion identity, sửa Copy giữ component bất biến không đổi; round2 PASS.
- Final review phát hiện fractional progress bị giữ sau khi tạo charge tới cap và external acceleration dư bị mất. Regression FAIL trước sửa; sửa tiêu phần lẻ đúng một lần/reject overflow, matrix PASS. Không giảm assertion.
- Bổ sung target owner/epoch guard cho modifier và test stale Hero sau candidate Load; PASS.
- Sửa nhãn verifier không đánh PASS cho phase không chạy; chạy lại MainGame, hai check PASS và hai phase NOT RUN. Không vấn đề nào vượt5 vòng.
- Dọn đúng15 `.meta` import và font fallback do Unity sinh; hash/path allowlist, không xóa dữ liệu ngoài profile. Không commit output/cache/log/test saves. Evidence/log/count/scope/manifest được lưu ngoài repo trong `outputs/v06-revised/`.

## File thay đổi

- Thêm runtime domain: `Assets/Script/CultivationModel.cs`, `CultivationEngine.cs`, `BloodEssenceTransactions.cs`, `FoundationRecovery.cs`, `CultivationBreakthrough.cs`, `CultivationPersistence.cs`, `CultivationIntegrationGate.cs` và `.meta` tương ứng.
- Thêm test: `Tests~/CultivationCoreRegression.cs`, `BloodEssenceRegression.cs`, `CultivationBreakthroughRegression.cs`, `CultivationPersistenceRegression.cs`; thêm `Assets/Editor/DualProgressionV06Verifier.cs/.meta`.
- Sửa compatibility/gate: `Assets/DataScript/EntityDataSO.cs`, `Assets/Script/GameManager.cs`, `SaveManager.cs`, `UIManager.cs`, `RealmProgressionAuthority.cs`, `RealmProgression.cs`, `RealmBreakthroughTransition.cs`.
- Sửa harness/archived API callers: `Assets/Script/MainGameFoundationProbe.cs`, `Assets/Editor/RealmIntegrationV06Verifier.cs`; `Tests~/MotionRegression.cs`, `run.sh`, `ProgressionUiRegression.cs`, `RealmIntegrationRegression.cs`, `RealmProgressionRegression.cs`, `RealmTransitionRegression.cs`, `RealmSaveCompatibilityFixture.cs`, `RealmSaveCompatibilityRegression.cs`, `FoundationCrossContractRegression.cs`.
- Thêm hai contract V1 và báo cáo này; thêm banner superseded vào realm contract/save note/backlog lịch sử. Danh sách name-status chính xác có trong evidence ngoài repo.

## Ba task tiếp theo

1. Duyệt catalog actual realm/ngưỡng/successor/transfer cùng Qi/energy/blood/injury balance và policy excess charges; tạo config version có validation.
2. Tích hợp component Save mới vào candidate load và durable debt settlement với migration/active Save/crash policy được duyệt; kiểm native old/current/future/template/failed-write/restart bằng owned fixtures.
3. Nối một session/clock vào Hero canonical và lifecycle GameManager, rồi adapter combat/UI dưới gate; nghiệm thu New → Qi → bình cảnh → Save/Load → recovery trước khi bật production. Thiên Kiếp/boss/active skill vẫn là batch riêng.
