# Realm → Save/Load: ghi chú tích hợp v0.5

06/11/2026. **IMPLEMENTED:** evaluator/transition thuần và DTO fixture. **PROPOSED/BLOCKED:** tích hợp Save production, chưa được cấp quyền thay schema.

## Mã hiện có và authority

- [RealmProgressionEvaluator](../../Assets/Script/RealmProgression.cs): `Evaluate(realmId, realmEntryLevel, totalLevel)` trả raw InternalLevel, MinorStage, AtBottleneck, ProgressEligibleForBreakthrough và reason. Catalog ID được copy từ caller; không có catalog realm chính thức/built-in mới, không đọc RankDatabase. Cap validation 999 đối chiếu `EntityDataSO.AddExp` hiện tại.
- [RealmBreakthroughTransition](../../Assets/Script/RealmBreakthroughTransition.cs): `Current`, `Revision`, `TryApplySuccess(attemptId, expectedRevision, explicitSuccessor, out reason)`. Chỉ sau success explicit mới đổi ID và entry; không tự tìm successor. Guard duplicate/stale chỉ có hiệu lực trong instance này, không qua Save/Load hoặc process mới.
- [EntityDataSO](../../Assets/DataScript/EntityDataSO.cs) tiếp tục authority Level/EXP/points/growth. RealmProgressionState là snapshot riêng, chưa đồng bộ tự động với Hero. Khi tích hợp phải lấy Level/EXP **tại thời điểm accepted success**, không dùng snapshot cũ từ entry trial. API v0.5 không có lệnh theo dõi EXP đang tăng giữa trial; không gắn thẳng session snapshot cũ vào Hero.
- [SaveManager](../../Assets/Script/SaveManager.cs) reset field của template trước `FromJsonOverwrite`, kiểm inventory/equipment/nested item versions. Những guard đó chưa bao phủ realm. [GameManager.TryLoadSlot](../../Assets/Script/GameManager.cs) load candidate trước CancelGameplayEntry/clear/swap Hero; cần giữ thứ tự này.

## Entry point và input/output dự kiến

1. New: sau lựa chọn tạo Hero, khởi tạo realm từ catalog chính thức được duyệt. Không dùng tên legacy mỗi 10 Level để gán ID.
2. Load: component probe đọc version/presence và realm fields vào candidate mới; validator dùng catalog explicit và Level của candidate. Mọi invalid/unknown/future state trả failure trước hủy gameplay hoặc thay Hero.
3. Accepted breakthrough success: adapter xác minh owner/life/attempt, snapshot Level/EXP hiện tại, cung cấp successor từ policy đã duyệt; áp Policy A. Thành công Level45 → entry45/nội bộ1, Level46→2, Level74→30. Không chuyển 15 Level dư, không reset Level/EXP.
4. Persistence commit thành công mới publish state/UI; failure không thông báo đột phá đã lưu. Identity/receipt bền phải được thiết kế riêng; không serialize HashSet guard runtime hoặc mặc định chỉ lưu last ID là đủ.

Input: component presence/version, stable realmId, entry, total Level/EXP, catalog, success/attempt/successor khi chuyển. Output: accepted candidate + derived view hoặc reason; không damage/stat/loot side effects. ProgressEligible chỉ nói đủ 30 cấp, không cam kết successor, quyền vào trial hoặc trial đã thắng.

## Migration và nguy cơ mất dữ liệu

- **BLOCKED:** migration save cũ Level cao. Fixture trả `LegacyMigrationRequired`; không gán realm miễn phí từ Level45, cũng không tự hạ về realm đầu. Mục initial-realm trong contract v0.4 vẫn là proposal chưa duyệt.
- **BLOCKED:** downgrade. Binary cũ có thể bỏ field realm rồi save lại làm mất state; cần policy chống ghi bằng bản cũ trước phát hành. Không tăng giả equipmentVersion để thay feature guard.
- **BLOCKED:** cảnh giới cuối/cap999. Evaluator vẫn trả progress eligible nếu nội bộ>=30; thiếu successor phải do caller thể hiện, transition trả MissingSuccessor. Không suy final realm từ vị trí trong danh sách hoặc tự nâng cap.
- **BLOCKED:** lưu khi đang Thiên Kiếp, thất bại/Retry/cost và checkpoint/slot. Chưa có trial runtime để resume; không lưu target/projectile reference.
- Khi schema được duyệt: thêm component state tối thiểu, reset missing defaults trước overwrite, guard trước mutation, migrate idempotent, không roll growth/phát reward khi Load. Không migrate bằng cách sửa trực tiếp file người chơi.

## Bằng chứng và test cần trước phát hành

[DTO fixture](../../Tests~/RealmSaveCompatibilityFixture.cs) nằm hoàn toàn trong `Tests~`: version=1 là **fixture version**, không phiên bản Save production. Candidate swap sau guard; Load lặp giữ realm/entry/Level/EXP, không cấp breakthrough. JSON roundtrip chạy bằng **Mono/UnityStub**, không là nghiệm thu JsonUtility thật hoặc malformed JSON parser.

Trước phát hành cần: old/current/future và unknown ID, missing field/template bẩn; Level/entry integer bounds/cap; late-success tại Level thay đổi trong trial; duplicate callbacks qua save/relaunch; failure trước/sau file Replace và trước swap; downgrade/manual-slot rollback; test Unity JsonUtility thật. I/O theo GUID/marker, cấu hình SaveDirectory trước Awake, không dùng save cá nhân.

Policy A và quy ước 1-based đã được người dùng chốt ở batch v0.5; không hỏi lại. Catalog/successor production, migration/downgrade/final realm/trial Save vẫn chờ quyết định. Không tự mở R02 production từ ghi chú này.
