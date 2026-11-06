# Dual Progression — Contract V1

06/11/2026 · Development v0.6 REVISED. Thay toàn bộ quy tắc dùng Total Level/entry Level để suy ra tu vi hoặc tiểu cảnh giới. Đây là hợp đồng của mã domain đã triển khai; **chưa phải cấu hình cân bằng hoặc tính năng tu luyện đã bật trong MainGame**. Xem [báo cáo](../automation-reports/DUAL_PROGRESSION_V06_REPORT.md) và [Tinh Huyết](BLOOD_ESSENCE_CONTRACT_V1.md).

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

Các component/snapshot bất biến, session là nơi duy nhất publish snapshot mới sau validation. Constructor dữ liệu không thay validation session. Không có manager mới, `MonoBehaviour`, startup hook hoặc scheduler tu luyện. Session thuần chưa được gắn vào Hero canonical. Level/EXP không xuất hiện trong chữ ký API tu luyện.

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

**IMPLEMENTED:** `CultivationComponentDto`, codec và `CultivationCandidateSlot` thuần. **PROPOSED:** schema đó cho production. Không thêm field mới vào `EntityDataSO`, không ghi component dở dang vào slot người chơi.

DTO version1 dự kiến chứa configurationId, ownerId, actual realm, foundation, reserve, current energy, injury, blood charges/progress, transaction reference/ratio/uses/time/status/end reason/owner/actual realm và settled IDs. Không lưu Level, minor stage, capacity, Max Energy, effective realm hoặc modifier. Burn actual realm/reference là snapshot giao dịch, không phải dữ liệu dẫn xuất hiện tại.

Candidate load kiểm version/configuration/owner/fresh epoch, catalog và mọi invariant trước swap. Unknown/future/invalid/hidden debt bị reject; state hợp lệ đang dùng không bị mutate. Missing/version0 trả `LegacyMigrationRequired`: không đổi EXP hoặc v1 realmEntryLevel thành căn cơ/charges. Chính sách migration mới chưa duyệt.

Recovery fixture chuyển Active burn thành SettlementPending với lý do Load/CrashRecovery, giữ nợ để thanh toán explicit; không tick offline/heal/grant. Đây chưa phải quyết định production hoặc bảo đảm disk durability. Chưa có writer mới/atomic commit/recovery transaction tích hợp SaveManager. Manual Load có thể quay toàn bộ timeline; không có ledger ngoài slot.

`CultivationIntegrationGate.ProductionEnabled=false`. Thay đổi MainGame thực: bỏ stage/internal Level suy từ EXP khỏi Character, hiển thị `Tu vi chưa cấu hình` và `Tu luyện mới chưa kích hoạt`; giữ Level/EXP/points và đường Save v1. Không có tu luyện, skill cưỡng ép, Thiên Kiếp hoặc buff Tinh Huyết hoạt động trong game.

## Gate trước phát hành

Phải duyệt catalog ngưỡng/successor/transfer, Qi densities/rates, Max Energy/recovery, blood unlock/capacity/rates/overflow, injury policy và migration Save. Sau đó nối SaveManager candidate-before-swap và durable deferred settlement vào lifecycle GameManager, rồi mới thêm một scheduler game-time và adapter combat/UI. Test yêu cầu: native JsonUtility old/current/future/partial/template contamination, failed write trước commit, crash recovery/pending debt, stale Hero callbacks, pause, Load/Retry/Travel/Menu/New, combat stat identity và nghiệm thu tự nhiên. Không mở flag chỉ vì domain PASS.
