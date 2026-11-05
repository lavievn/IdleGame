# Hợp đồng tiến trình cảnh giới chính thức

## Trạng thái và mốc đối chiếu

**PROPOSED — thiết kế kỹ thuật v0.4, chưa phê duyệt để triển khai.** Ngày 05–06/11/2026; nguồn `ba86368`, đối chiếu lại sau `e1fe9e9` và `0b66288`. Hai commit v0.4 không thêm state cảnh giới hoặc Save. Các tên field/API dưới đây là đề xuất, chưa tồn tại trong production.

Quyết định sản phẩm đã chốt: Level tổng tăng bằng EXP; cảnh giới là tiến trình riêng; chu kỳ nội bộ 30 cấp, Sơ 1–9, Trung 10–19, Hậu 20–29, từ 30 viên mãn/bình cảnh; tiểu cảnh giới tự chuyển; đại cảnh giới chỉ chuyển sau đột phá thành công. Level tổng không reset. Thiên Kiếp dự kiến có pha chịu đòn và pha gây sát thương. Không chốt xác suất, chi phí, buff hoặc thông số hai pha trong hợp đồng này.

## Nền hiện có và trách nhiệm dự kiến

| File/hệ thống | Đã xác minh trong mã | Vai trò khi được duyệt |
|---|---|---|
| [EntityDataSO](../../Assets/DataScript/EntityDataSO.cs), `AddExp`, `RebuildHeroStats` | Level/EXP/points/growth đã hoạt động; giới hạn Level hiện tại 999. Không có state đột phá. | Giữ authority Level; chứa snapshot cảnh giới tối thiểu sau khi schema được duyệt. Không đưa gate vào vòng tăng EXP. |
| [GameManager](../../Assets/Script/GameManager.cs), `InitHeroData`, `TryLoadSlot`, `OnHeroDied`, `OnRetryClicked` | Tạo Hero canonical, load vào snapshot mới rồi thay Hero, dọn entry/combat; defeat world-map reset wave chưa clear, giữ Level/gear. | Điều phối entry/cancel Thiên Kiếp và commit kết quả; không dùng defeat map thường làm policy thất bại đột phá mặc định. |
| [RankDatabase](../../Assets/Script/TuTienEnums.cs), `GetRankName` | Tên legacy mỗi 10 Level tổng; UI đã ghi legacy/prototype. | Giữ đọc save/UI legacy; không suy realm chính thức từ tên này. Cần catalog cảnh giới chính thức riêng được duyệt. |
| [SaveManager](../../Assets/Script/SaveManager.cs), `LoadGame`, `SaveGame` | JsonUtility; reset các field legacy trước overwrite; guard inventory/equipment và phiên bản item lồng nhau. | Thêm guard và migration riêng cho realm trước mutation khi được giao. Guard hiện tại không bảo vệ field realm tương lai. |
| [CombatManager](../../Assets/Script/CombatManager.cs), `HeroCombatState` | LifeVersion, target/projectile cleanup và combat hiện tại. | Tái sử dụng đời actor, pause/cancel và damage thật; chỉ thêm adapter encounter sau khi đặc tả hai pha được duyệt. |

[Audit](GAMEPLAY_SYSTEM_AUDIT.md) và [dependency map](GAMEPLAY_DEPENDENCY_MAP.md) có mốc nguồn cũ: allocation và nhãn realm đã được nối trong v0.2/v0.3. Không lấy mũi tên “chưa có caller” cũ làm hiện trạng UI v0.4.

## State tối thiểu — đề xuất

| State | Authority và invariant |
|---|---|
| `realmProgressVersion` | Version thành phần độc lập, không thay nghĩa version equipment/inventory. Thiếu field nghĩa là legacy, không mặc định bằng giá trị template. |
| `realmId` | Stable ID từ catalog đã duyệt, không dùng index enum hoặc chuỗi hiển thị `RankDatabase`. Chỉ chuyển sang successor hợp lệ sau commit thành công. |
| `realmEntryLevel` | Level tổng tại lúc vào đại cảnh giới; `1 <= entry <= currentLevel`. Tạo mới đề xuất entry=1. |

Không lưu `minorStage`, `internalLevel`, `atBottleneck`: đây là derived data. Không lưu lặp tên cảnh giới. Không thêm buff hoặc hệ số realm vào stats/damage trong phạm vi thiết kế này.

Attempt runtime cần identity riêng (`attemptId`, Hero life/version, phase, trạng thái terminal); đây là state tạm, không tự trở thành field Save. Có lưu attempt giữa chừng hay không là quyết định cần chốt trước triển khai.

## Công thức tiến trình nội bộ

**Quy ước đề xuất để tránh lệch một cấp:** cấp nội bộ bắt đầu ở 1 ngay tại Level vào cảnh giới.

```text
L = currentLevel                 // authority EXP hiện tại
E = realmEntryLevel
rawInternal = 1 + (L - E)        // kiểm invariant trước, không sửa dữ liệu lỗi bằng clamp
displayInternal = min(30, rawInternal)
stage = So(1..9), Trung(10..19), Hau(20..29), VienMan(rawInternal >= 30)
eligibleForTrial = rawInternal >= 30 AND co successor AND cac dieu kien da duyet
```

Không dùng modulo 30: modulo sẽ làm nhân vật tự quay lại Sơ kỳ khi chưa đột phá. `rawInternal` có thể lớn hơn 30; UI hiển thị viên mãn và Level tổng thật. EXP/points/growth vẫn theo `AddExp`; không giữ EXP chờ hay tự chuyển đại cảnh giới.

| E | L | Nội bộ / kết quả |
|---:|---:|---|
| 1 | 1 / 9 / 10 | 1 Sơ / 9 Sơ / 10 Trung |
| 1 | 19 / 20 / 29 | 19 Trung / 20 Hậu / 29 Hậu |
| 1 | 30 / 45 | Đều viên mãn; vẫn cùng realm nếu chưa thành công |
| 45 | 45 / 53 / 54 | Sau thành công ở Level 45: 1 Sơ / 9 Sơ / 10 Trung |
| 45 | 74 | Đủ 30 cấp nội bộ theo quy ước 1-based; được xét vào Thiên Kiếp kế tiếp |

Level 999 vẫn là giới hạn mã hiện tại, không phải quyết định thiết kế “Level vô hạn”. Cảnh giới cuối, successor thiếu và trường hợp vào realm ở Level sát cap cần quyết định trước triển khai; không tự tăng cap hoặc đổi công thức EXP.

## Quy trình đột phá — đề xuất

```text
Normal -> Eligible -> Preparing -> EndurancePhase -> DamagePhase -> Resolving
                     |                |                 |              |
                     +----------------+-----------------+--> Failed/Cancelled
                                                                    Resolving -> Committed
```

1. GameManager xác minh Hero hiện hành, alive, không entry/load/modal đang chờ, đủ tiến trình và điều kiện đã duyệt. Không tiêu gì khi validation thất bại.
2. Tạo một attemptId, khóa entry combat thường theo cùng cơ chế điều phối; chụp identity Hero và dữ liệu cần thiết. Không dùng camera/AI/spawn thường để giả lập Thiên Kiếp đã hoạt động.
3. Pha chịu đòn ghi damage được combat chấp nhận, không dùng animation kết thúc làm kết quả. Tiêu chí sống sót, thời lượng và chuyển pha chờ duyệt.
4. Pha gây sát thương ghi accepted damage trên target/lifetime của attempt. Ngưỡng, deadline, mục tiêu và nguồn damage hợp lệ chờ duyệt. Hai pha hoàn thành mới có thể thành công.
5. Resolving chỉ cho một kết quả terminal. Kiểm tra lại attemptId, owner, realm cũ và life trước commit; callback trùng/stale không đổi realm lần hai.
6. Thành công: successor thay `realmId`, `realmEntryLevel = currentLevel` tại commit; Level/EXP/points/gear không reset. Nếu Level tăng trong attempt, mốc lấy Level lúc commit, không lấy Level lúc vào thử thách. Có khóa EXP trong trial hay không cần chốt; không tự thêm nguồn EXP trial.
7. Khi có persistence, commit realm/cost/reward nếu có trong cùng snapshot đã xác minh. Save lỗi không được hiển thị “đã đột phá thành công”; giữ trạng thái Resolving để xử lý theo policy được duyệt. Không tự dùng SaveGame trên Hero đang mutation như một transaction có rollback.

Pause giữ phase/timer theo game-time; Resume không phát lại damage hoặc effect. Load/New/Menu/Travel/Retry phải cancel đời attempt cũ và projectile trước khi bind Hero mới. Không cho travel hai encounter đồng thời. Không tạo scheduler thứ hai tự cấp damage cạnh CombatManager.

## Thất bại và Retry — chờ người dùng quyết định

| Quyết định | Lựa chọn cần chốt | Phần bị chặn |
|---|---|---|
| R1 — quy ước mốc | Duyệt công thức 1-based và ví dụ Level 45 → nội bộ 1; catalog/successor/cảnh giới cuối. | Pure realm evaluator có thể được chuẩn bị theo contract, production authority chưa mở. |
| R2 — thất bại | Giữ HP nào, quay về đâu, có mất tiến độ/cost nào hay chỉ kết thúc attempt? | Trial defeat và Retry. Không suy từ defeat map thường. |
| R3 — chi phí/xác suất/buff | Có hay không, nguồn/cách hoàn lại nếu cancel, áp ở thời điểm nào. | Commit kinh tế và stat effects; không tự đặt con số. |
| R4 — resume/checkpoint | Load giữa trial là cancel/restart hay resume pha; HP/timer reset thế nào; có trial EXP không. | Persistence attempt và entry recovery. |
| R5 — save cũ/cap | Khởi tạo realm nào cho save Level cao; Level 999 và realm cuối. | Migration/balance dài hạn. |

Những quyết định này không chặn việc bàn giao tài liệu. Không yêu cầu người dùng chốt ngay để tự triển khai tiếp.

## Save compatibility — kế hoạch, chưa thay schema

- Save thiếu realm state: không suy realm từ `floor(level/10)` hoặc `floor(level/30)` vì không biết lịch sử đột phá. Đề xuất thận trọng là initial realm, entry=1, giữ nguyên Level/EXP/items; Level cao sẽ ở bình cảnh. Đây là lựa chọn migration cần duyệt, không tự hạ/ban tặng realm khi load.
- Save mới: đọc version/cấu trúc trước `FromJsonOverwrite`; kiểm realmId, E, successor và dữ liệu hữu hạn/miền hợp lệ vào snapshot mới, rồi mới cho GameManager swap. Version tương lai/ID lạ không được normalize thành initial realm hoặc ghi đè file gốc.
- Reset field realm trước overwrite khi thiếu field để tránh thừa state từ SO template. Migration phải idempotent và không phát event thưởng/buff/roll growth.
- Older executable hiện có thể bỏ qua field lạ rồi làm mất state khi save lại. Không tuyên bố backward-write safe; cần kế hoạch downgrade/feature-version guard trước phát hành, không tăng giả version equipment để chặn thay cho realm.
- Manual slots là snapshot: Load cũ rollback toàn bộ dữ liệu cùng thời điểm. Ledger realm nếu có cũng nằm cùng snapshot; không tự tuyên bố chống rollback giữa slot.
- Test I/O chỉ dùng GUID/marker và `ConfigureSaveDirectoryBeforeAwake`; không đọc/ghi save cá nhân.

## Các ca kiểm thử phải có khi triển khai

| Nhóm | Tiêu chí PASS dự kiến |
|---|---|
| Ranh giới | 1/9/10/19/20/29/30/31; dừng bình cảnh ở Level cao, không modulo/auto realm. |
| Mốc thành công | Thành công Level 30/45, entry đúng Level commit, L không reset; lặp callback không mutation lần hai. |
| Invariant | E>L, E<1, realmId lạ, version tương lai: từ chối trước swap/file write. |
| Hai pha | Không bỏ qua pha; death/cancel đồng frame, pause mỗi pha, stale projectile sau Retry đều có một kết quả terminal. |
| Load | Legacy/current/future, thiếu field sau template đã có realm, roundtrip, save lỗi/ghi thành công rồi crash trước swap. |
| Không đổi nền | EXP/allocation/growth, camera v0.3, basic damage và gear giữ kết quả cũ khi chưa vào trial. |

**NOT RUN:** không có evaluator realm/trial runtime được viết hoặc test ở v0.4. Đây là tiêu chí cho nhiệm vụ sau, không phải kết quả PASS.
