# Dual Progression — Contract V1

> **Cập nhật v0.7 — 06/11/2026:** Qi/tu vi đã được tích hợp và nghiệm thu trong MainGame qua profile Editor cô lập. Production balance/migration và Burning vẫn tắt. [Báo cáo v0.7](../automation-reports/CULTIVATION_GAMEPLAY_V07_REPORT.md) là mốc tích hợp mới; kết quả v0.6 dưới đây vẫn áp dụng cho domain.

06/11/2026 · Development v0.6 REVISED. Thay toàn bộ quy tắc dùng Total Level/entry Level để suy ra tu vi hoặc tiểu cảnh giới. Đây là hợp đồng của mã domain đã triển khai; **không phải production balance; v0.7 chỉ bật MainGame trong owned playtest**. Xem [báo cáo](../automation-reports/DUAL_PROGRESSION_V06_REPORT.md) và [Tinh Huyết](BLOOD_ESSENCE_CONTRACT_V1.md).

## Authority và API

| Dữ liệu | Authority | Giá trị tính lại |
|---|---|---|
| Level, EXP, points, growth | `EntityDataSO`, pipeline `AddExp` hiện hành | Không phụ thuộc tu luyện |
| ActualRealmId, tu vi căn cơ | `CultivationFoundation` trong `CultivationSession.Current` | MinorStage, AtBottleneck, CombatTier từ catalog |
| Linh khí chưa luyện hóa | `UnrefinedQiReserve.Amount` | Không phải EXP hoặc linh lực |
| Linh lực khả dụng | `SpiritualEnergy.Current` | Max từ rules và injury |
| Charges, tiến độ cô đọng | `BloodEssenceState` | Capacity từ actual realm/minor stage |
| Thương tổn | `FoundationInjuryState.Severity` | Refinement factor, Max Energy, trial gate |
| Khoản nợ Tinh Huyết | `BurningTransaction` và settlement receipts | TotalDebt từ reference × ratio × charges used |

Các component/snapshot bất biến, session là nơi duy nhất publish snapshot mới sau validation. Constructor dữ liệu không thay validation session. Không có manager/scheduler MonoBehaviour mới. V0.7 GameManager sở hữu một binding/session cho Hero canonical trong profile thử; Editor bootstrap chỉ cấu hình trước Awake, không tick tài nguyên. Level/EXP không xuất hiện trong chữ ký API tu luyện.

`CultivationSession(ownerId, epoch, catalog, rules, initial)` yêu cầu owner/epoch khác rỗng. Mọi command phải dùng owner/epoch hiện tại, Guid command/session hợp lệ. Candidate Load tạo epoch mới. Receipt callback cũ không được áp vào Hero sau Load; host tương lai phải giữ đúng một session và một clock cho mỗi Hero canonical.

## Catalog, ngưỡng và cấu hình

`CultivationRealmEvaluator.TryCreate(definitions, out evaluator, out reason)` copy các định nghĩa bất biến vào dictionary ID ordinal. Reject catalog rỗng, ID rỗng/trùng, tier trùng/âm/overflow, thiếu ngưỡng, ngưỡng âm/NaN/infinity/trùng/sai thứ tự. Mỗi realm có bốn ngưỡng `[0, Trung kỳ, Hậu kỳ, Viên mãn]`, tăng nghiêm ngặt. Catalog không khai báo successor tự động.

`Evaluate(actualRealmId, foundation)` chỉ nhận tu vi hữu hạn trong `[0, ngưỡng Viên mãn]`. Stage tăng hoặc giảm theo amount; bằng ngưỡng cuối là bình cảnh. Không modulo, không đổi ActualRealmId. Quá cap là dữ liệu invalid, không âm thầm sửa khi Load.

`CultivationRules` bắt buộc truyền max delta, absorption rate/efficiency, refinement rate/efficiency, Max Energy, recovery source rate/modifier, blood rate, debt ratio, absolute stat factor, injury penalties/trial limit và capacity policy. Validate hữu hạn/không âm, efficiency và debt ratio ≤1, factor ≥1. Không có default production. Các ID `fixture:*`, cap1000, tỷ lệ10%, factor2, rate, ngưỡng injury trong test chỉ là fixture.

## Qi pipeline và clock

`Tick(owner, epoch, step, dt, paused, worldQiDensity)`:

1. Hấp thu = density × absorption rate × efficiency × game-time delta.
2. Cộng vào reserve.
3. Lấy input tối đa refinement rate × delta; yield = refinement efficiency × injury factor.
4. Gain căn cơ giới hạn bởi khoảng còn thiếu tới bình cảnh; trừ reserve đúng input `gain / yield`. Yield0 không tiêu reserve.
5. Hồi linh lực và cô đọng Tinh Huyết theo cấu hình riêng; cập nhật thời gian Burning nếu có.

Không cần kill count hoặc trạng thái di chuyển/chiến đấu. Host tương lai có thể gọi ở cả idle/travel/combat, nhưng chỉ một lần mỗi simulation step. Step tăng đơn điệu; callback cùng/cũ step bị từ chối. Pause không cấp tài nguyên, không trôi Burning hoặc treatment. Delta âm/NaN/infinity/vượt MaximumStepSeconds bị từ chối nguyên bước, không clamp rồi âm thầm cấp phần thời gian khác. Không xử lý offline. Overflow không publish một phần.

Ở bình cảnh vẫn hấp thu; reserve giữ nguyên phần chưa luyện hóa, căn cơ không vượt cap. Injury có thể giảm hiệu suất luyện hóa; phần input đã luyện được chuyển đổi theo efficiency, không nhân đôi. Không có density production cho 133 nodes, không sửa Level/Enemy/Spawn/SuggestedLevel từ density.

## Các nguồn phục hồi tách riêng

- `SpendSpiritualEnergy`, `RestoreSpiritualEnergy(..., source)`, `ClampSpiritualEnergy`: chỉ linh lực; overspend/nguồn invalid bị từ chối, restore giới hạn bởi Max sau injury. Không tụt realm vì cạn linh lực. Reconcile Max không nhân modifier sau mỗi refresh.
- Tick luyện hóa: phục hồi căn cơ đã mất, vẫn không chữa injury.
- `HealInjury(..., treatmentSource)`: chỉ giảm injury; không thêm Qi/căn cơ/linh lực.
- `InjuryTreatmentSession`: PassiveNurturing hoặc ExplicitExternalTreatment, rate/budget/max-delta tường minh; Active → Completed/Cancelled. Tick owner/epoch và treatmentId/step chống trùng kể cả khi tái dựng treatment trong cùng session. Cancel không chữa thêm. Không có item/recipe production.

Injury factor = `max(0, 1 - injury × configuredPenalty)`; Max Energy tương tự. Trial chỉ được bắt đầu khi injury ≤ configured limit. Đây là policy cấu hình dùng fixture, chưa duyệt tỷ lệ production.

## Đột phá

`RealmBreakthroughTransition.ApplySuccess(owner, epoch, successEvent, expectedRealmRevision, expectedActualRealm, explicitSuccessor, foundationAfterSuccess)` dùng tu vi thực. Phải đang bình cảnh, injury đạt điều kiện, không Burning/pending debt. Event phải explicit; API không chứng minh đã có Thiên Kiếp thật.

Caller cung cấp successor đã biết có tier cao hơn và giá trị căn cơ sau thành công hợp lệ theo target. Domain không chọn successor, không suy từ tier hoặc thứ tự RankDatabase. `foundationAfterSuccess` là input transfer policy chưa duyệt production, fixture dùng0. Reserve giữ nguyên để tiếp tục luyện hóa; đạt bình cảnh tiếp theo vẫn cần success event mới. Revision, expected actual realm, event Guid và epoch chống callback duplicate/stale. Không giảm/reset Level/EXP hoặc dùng EffectiveCombatRealm để đột phá.

## Thay thế và giữ lại mã cũ

- `CultivationRealmEvaluator` thay authority cũ dựa trên Level. Hai type v0.5 được đổi thành `LegacyLevelRealmProgressionEvaluator`, `LegacyLevelRealmProgressionState`.
- Transition hiện tại ở `CultivationBreakthrough.cs`; bản v0.5 trong `RealmBreakthroughTransition.cs` thành `LegacyLevelRealmBreakthroughTransition`.
- `RealmMinorStage` được tái sử dụng; MartialArtsRegistry, BossEncounterCoordinator, WorldMapCatalog, WeaponFamily, CombatStats giữ nguyên.
- `RealmProgressionAuthority.cs` giữ tên file để giữ GUID; class đổi thành `LegacyRealmSnapshotCompatibility`. Ba field realm v1 trong Hero được giữ để đọc/ghi snapshot cũ, **không phải authority tu luyện mới**. Validation primitive giữ semantics v1; không gọi evaluator để derive stage trong MainGame.
- `TryEvaluateLegacyLevelSnapshot` và test cũ chỉ phục vụ lịch sử/compatibility. CLI Play verifier `-foundationRealmV06` bị chặn với thông báo retired. Không dùng làm bằng chứng gameplay mới.

## Save và integration gate

**IMPLEMENTED:** DTO/codec/candidate thuần và adapter SaveManager v0.7 trong owned playtest. EntityDataSO có `string cultivationJson` materialize state đã validate; chuỗi rỗng là absence, tránh inline-class template defaults/alias. **BLOCKED:** production config/migration. Không dùng slot cá nhân để nghiệm thu.

DTO version1 chứa configurationId, configurationVersion (adapter v0.7 yêu cầu≥1 và khớp profile), ownerId, actual realm, foundation, reserve, current energy, injury, blood charges/progress, transaction reference/ratio/uses/time/status/end reason/owner/actual realm và settled IDs. Không lưu Level, minor stage, capacity, Max Energy, effective realm hoặc modifier. Burn actual realm/reference là snapshot giao dịch, không phải dữ liệu dẫn xuất hiện tại.

Candidate load kiểm version/configuration/owner/fresh epoch, catalog và mọi invariant trước swap. Unknown/future/invalid/hidden debt bị reject; state hợp lệ đang dùng không bị mutate. Domain missing/version0 trả `LegacyMigrationRequired`. Gameplay adapter nhận diện absent/empty rõ ràng và chỉ cho owned playtest đã explicit bật initializeLegacy khởi tạo Luyện Khí/zero resources; không lấy EXP hoặc v1 realmEntryLevel làm căn cơ/charges. Production migration chưa bật.

Recovery fixture chuyển Active burn thành SettlementPending với lý do Load/CrashRecovery, giữ nợ để thanh toán explicit; không tick offline/heal/grant. Đây chưa phải quyết định production hoặc bảo đảm disk durability. V0.7 dùng writer temp + flush + Replace/Move hiện có cho component không Burning; chưa có durable recovery transaction của Burning tích hợp SaveManager. Manual Load có thể quay toàn bộ timeline; không có ledger ngoài slot.

`CultivationIntegrationGate.ProductionEnabled=false`. MainGame mặc định giữ nhãn chưa cấu hình; v0.7 explicit Editor launcher mới cấp profile/session và hiển thị PLAYTEST. Vòng Qi/tu vi/Save/Load hoạt động trong profile đó; Level/EXP/points và v1 metadata không suy ra tu vi. Không có skill cưỡng ép, Thiên Kiếp hoặc buff Tinh Huyết hoạt động trong game.

## Gate trước phát hành

Phải duyệt catalog ngưỡng/successor/transfer, Qi densities/rates, Max Energy/recovery, blood unlock/capacity/rates/overflow, injury policy và migration Save. Sau đó nối SaveManager candidate-before-swap và durable deferred settlement vào lifecycle GameManager, rồi mới thêm một scheduler game-time và adapter combat/UI. Test yêu cầu: native JsonUtility old/current/future/partial/template contamination, failed write trước commit, crash recovery/pending debt, stale Hero callbacks, pause, Load/Retry/Travel/Menu/New, combat stat identity và nghiệm thu tự nhiên. Không mở flag chỉ vì domain PASS.


## Integration delta v0.7

- `CultivationPlaytestProfile.TryCreate` tạo config bất biến từ JSON schema1, identity/version, realm name/thresholds/successor explicit, node Qi overrides + fallback, absorption/refinement/clock/energy. JSON ở Assets/Editor, không là production default. Một realm Luyện Khí, successor rỗng; bảng thử [0,6,12,20] chỉ phục vụ nghiệm thu.
- `GameManager.Cultivation` expose binding snapshot hiện tại; mutable domain session không ra UI. New zero-state; Load prepares candidate trước swap; Death/Menu/Disable dispose; Retry/Enable restore với epoch mới; Travel giữ session và đổi source. Sole tick là GameManager.LateUpdate trước entry handoff. Không offline/catch-up hoặc kill dependency.
- `CultivationGameplayBinding.Advance` xác minh canonical Hero/frame/not disposed, dùng step0.1 game-second/max frame0.5s theo profile; duplicate/stale/pause không cấp Qi. Không persist substep remainder; rebind bỏ <step thời gian chưa credit, không cấp bù. Snapshot JSON được cập nhật sau mỗi step và đánh dirty cho Save hiện có.
- `CultivationGameplayState` và `CultivationSaveJson`: field presence/type/duplicate/depth guard, configurationVersion và complete DTO validation trước template overwrite/candidate publication. Profile source thiếu component bị reject khi Save. Existing future/khác config được bảo vệ, không xóa component để load.
- SaveManager giữ kiến trúc temp/Replace, thêm flush và owned fault probes. Native current/legacy/template/future/config/invalid/repeated Load và BeforeWrite/AfterTempWrite/BeforeReplace và actual temp-creation/Windows locked-Replace failures PASS. Không tuyên bố power-cut-safe hoặc Burning durable. Save legacy source không tự rewrite khi Load.
- UIManager dùng Hero/binding canonical, foundation bar và text polling0.2s; Level/EXP, realm/stage/bottleneck, reserve/rates/energy, blood/trial khóa. Bốn kích thước đã nghiệm thu MainGame, không có nhãn stage dựa trên Level.
- Chơi thử bằng Unity menu **Bach Luyen → Playtests → Cultivation v0.7 (isolated)**, chọn Chơi mới. Launcher dùng MainGame copy bị ignore và SaveDirectory GUID/marker riêng trước Awake. [Báo cáo v0.7](../automation-reports/CULTIVATION_GAMEPLAY_V07_REPORT.md) ghi bằng chứng và fixture limits.

Các gate phát hành phía trên vẫn áp dụng cho production. V0.7 hoàn thành adapter/session/UI trong profile cô lập, chưa duyệt cân bằng, migration người chơi/config upgrades/downgrade hoặc deferred debt persistence.
