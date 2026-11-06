# Realm Integration v0.6 — bàn giao

06/11/2026 (Asia/Saigon). **IMPLEMENTED + VERIFIED:** Realm canonical đã được nối vào MainGame/New Hero/Character UI và Save/Load có guard. Vòng New Hero → AddExp/Level → tiểu cảnh giới/bình cảnh → Save → Load → tiếp tục lifecycle đã chạy bằng Unity thật trên profile cô lập. Chưa triển khai Thiên Kiếp hoặc đại cảnh giới tiếp theo.

## 1. Git và commit

- Repository `lavievn/IdleGame`, worktree `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Branch `feature/monster-awareness-ambush`; baseline sạch `5d444cd7a4a295358675a64590d62d4c9914df9c`, đã đồng bộ origin trước phiên. Kiểm tra tiến trình không thấy Unity/Git/test writer khác đang dùng worktree. Không reset/rebase/merge/force-push, không đổi branch.
- HEAD trước commit báo cáo: `ac8636d`; commit chứa báo cáo này là HEAD bàn giao, xem `git log -1`. Sau bảy commit v0.6, ahead upstream local dự kiến 7. **Không push** trong v0.6.

| Commit | Nội dung |
|---|---|
| `9b3eef7` | Unity baseline: load đủ bốn type v0.5 từ Assembly-CSharp bằng Editor thật |
| `9b7cea2` | Canonical Realm component, authority/catalog khởi đầu, guarded legacy/current/future Save; Native fixture và Mono |
| `54d5c8b` | New Hero reset Realm rõ ràng; Character hiển thị canonical thay nhãn legacy |
| `17851f4` | MainGame progression/UI/persistence/recovery verifier và selector hồi quy liên quan |
| `eb393d2` | Xác nhận ba field thực sự có trong JSON đã ghi; explicit sentinel/null không bị coi là thiếu component |
| `ac8636d` | Native roundtrip với Sword equipped/modifier list không rỗng, không double stats sau Load lặp |
| Commit báo cáo này | Bàn giao kết quả/giới hạn; không thêm gameplay |

## 2. File thêm/sửa

**Thêm:** `Assets/Script/RealmProgressionAuthority.cs` và `.meta`; `Assets/Editor/RealmIntegrationV06Verifier.cs` và `.meta`; `Tests~/RealmIntegrationRegression.cs`; báo cáo này.

**Sửa:** `Assets/DataScript/EntityDataSO.cs`, `Assets/Script/SaveManager.cs`, `GameManager.cs`, `UIManager.cs`, `MainGameFoundationProbe.cs`; `Tests~/MotionRegression.cs`, `ProgressionUiRegression.cs`, `run.sh`. Tổng 14 file cuối phiên, gồm sáu file mới/tám file sửa.

Không đổi scene MainGame, camera v0.3, root synergy, Combat damage/lifecycle/AI/range/spawn, MaxHealth theo mode, EXP formula, Forge/Loot configs, world catalog hoặc module martial/boss. Không commit output/cache/log/save thử. Unity sinh 15 import metadata và dynamic font cache: chỉ dọn đúng whitelist artifacts từ baseline sạch, kiểm hash font bằng HEAD trước refresh index. Metadata checker cuối: 776 file, **0 lỗi/3 warning folder-meta Lake/Sea/Steppe có sẵn**.

## 3. Realm authority và catalog thực tế

Ba field canonical trong EntityDataSO:

```text
realmComponentVersion = 1
realmId = "realm:luyen-khi"
realmEntryLevel = 1
```

Không lưu InternalLevel, MinorStage, Bottleneck hoặc tên hiển thị. RealmProgressionAuthority tái sử dụng RealmProgressionEvaluator v0.5 và kiểm version/ID/entry/total trước tính. Catalog v0.6 chỉ có **Luyện Khí**, tên được xác minh từ dữ liệu RankDatabase hiện có và yêu cầu người dùng; stable ID là định danh kỹ thuật mới cho đúng cảnh giới này. Không dùng thứ tự RankDatabase làm authority hoặc tạo successor/cảnh giới mới.

Các lớp v0.5 vẫn ở namespace TuTienCore và được Unity load từ Assembly-CSharp. Trạng thái pure/transition v0.5 không được serialize vào Save; component mới chỉ int/string được JsonUtility thật xác minh. RealmBreakthroughTransition vẫn chưa có caller MainGame, không có trial/success callback production.

## 4. New Hero và progression

GameManager.InitHeroData gọi `RealmProgressionAuthority.Initialize(runtimeHeroData)` ngay sau reset Level/EXP. Clone template có Realm sai cũng bị reset về version1/LuyệnKhí/entry1. Không thay identity/name/gender/root/race/birth/random/stat/inventory pipeline.

AddExp giữ nguyên code cũ, Level/EXP/points/growth vẫn theo logic hiện hành; Realm ID/entry không đổi trong Level Up. Mỗi lần UI đọc đều derive:

```text
internalLevel = totalLevel - realmEntryLevel + 1
1..9 = Sơ kỳ; 10..19 = Trung kỳ; 20..29 = Hậu kỳ; >=30 = Viên mãn/bình cảnh
```

Các mốc thực chạy: 1/9/10/19/20/29/30/45 với entry1. Level45 vẫn Luyện Khí Viên mãn, raw internal45; không modulo, không chuyển realm, không reset EXP/points, không buff/loot mới hoặc trial button. Cap999 và invalid ID/entry/version được kiểm tra Mono. Policy A vẫn được giữ trong module thuần: nếu tương lai có success commit ở Level45 thì entry45/internal1; v0.6 không mở đường success đó.

## 5. Save/Load và migration giới hạn

### Validation trước publication

SaveManager đọc một RealmVersionProbe riêng bằng JsonUtility, phân biệt **thiếu toàn bộ field** với component hiện diện nhưng partial/null/sentinel/không hợp lệ. Presence được kiểm theo key trước khi chấp nhận migration; giá trị do native JsonUtility đọc và authority validate. Future version, unknown ID, entry0/entry>Level, unsupported sentinel hoặc partial component bị từ chối trước khi mutation target bởi Realm load.

Trước FromJsonOverwrite, reset ba field Realm trên candidate để template không rò vào Save legacy. Với current component, kiểm lại field sau overwrite đúng probe; validate lại theo Level đã restore. GameManager.TryLoadSlot vẫn dùng candidate-load-before-swap hiện có; không dựng transaction/rollback architecture mới. Failed Realm Load trong combat thật không thay canonical Hero, life, combat binding hoặc encounter.

### Legacy migration được duyệt có giới hạn

Save không có Realm component → Luyện Khí, entry1, version1; giữ Level tổng và các field khác qua đường normalize/migration cũ. Không suy Level/10 hoặc Level/30, không grant realm/stat/reward. Level45 legacy ở Viên mãn. Migration đặt dirty để lần Save hợp lệ có thể ghi component mới; **Load trực tiếp không ghi đè file legacy**. Repeated Load/Save idempotent, không tăng realm/entry.

Native fixture so sánh toàn bộ JSON các field không thuộc Realm trước/sau, chỉ loại `isDirty` vì migration hợp lệ thay cờ dirty. Snapshot chuẩn bị có Level45, EXP34200 sau balance migration sẵn có, points/root/material/gold/world/equipped gear/modifiers; Realm migration không thay chúng. Existing balance/equipment/world migration vẫn theo code cũ, không bị thay công thức hoặc version để phục vụ Realm.

### Save/write và bảo vệ snapshot

Typed SaveGame từ chối Realm invalid trước serialize/ghi. Raw JSON writer cũng kiểm component. Trước overwrite, snapshot hiện có chứa Realm unknown/future/invalid đã parse được bị bảo vệ độc lập với equipment versions. Không tăng giả Equipment/Inventory version.

Ghi vẫn dùng .tmp→File.Replace/File.Move hiện có. Inject lỗi bằng thư mục `.tmp` trong profile riêng: Save trả false, file `.json` hợp lệ trước đó byte-for-byte không đổi. Chưa kiểm crash/power-cut giữa/sau OS file replacement; không tuyên bố durability tuyệt đối.

### Downgrade

**Executable v0.5 hoặc cũ hơn chưa biết ba field mới, có thể bỏ chúng khi save lại. Không bảo đảm downgrade-write preservation.** Guard v0.6 bảo vệ Realm version tương lai mà v0.6 đọc được, không làm binary cũ hiểu schema mới. Khi cần nhiều realm/successor hoặc trial persistence phải có nhiệm vụ thiết kế/migration riêng. Không đọc/nâng cấp/ghi Save cá nhân để kiểm thử.

## 6. UI trước/sau

Trước (ví dụ suy từ mapping legacy ở code cũ): Level45 có thể ghi `Cấp 45 · Cảnh giới legacy/prototype: Hóa Thần`. Đó là nhãn mỗi 10 Level, chưa là authority chính thức.

Sau, ảnh MainGame thật ghi:

```text
Level 45 · Luyện Khí · Viên mãn
Nội bộ 45 · Bình cảnh · Điểm thuộc tính: 44
HP ... · Physical Attack ... · EXP ...
```

Summary đọc GameManager.HeroData và HeroCombatState hiện hành, không giữ Realm snapshot của Hero trước Load. Không có nhãn “đột phá ngay”, nút trial hoạt động hoặc artwork/theme/framework mới. Readable paging/stat allocation cũ giữ nguyên.

Đã đo/quan sát cả bốn ảnh **600×338, 800×450, 1150×647, 1360×680**: ForceMeshUpdate không overflow summary, nút HP/Physical/Close trong bounds. Đây là Unity Game View thật và UI routing; không phải OS-native mouse/Windows Player acceptance.

## 7. Kết quả theo môi trường

| Môi trường / nhóm | Kết quả thực chạy |
|---|---|
| Unity6000.5.0f1 baseline compilation | **VERIFIED:** RealmProgressionEvaluator, RealmBreakthroughTransition, MartialArtsRegistry, BossEncounterCoordinator load đúng Assembly-CSharp; không sửa module vì warning cũ |
| Mono/UnityStub realm evaluator | **3 nhóm PASS:** boundaries/invalid/integer/cap/catalog |
| Mono canonical Realm | **2 nhóm PASS:** AddExp stage tới45, points/growth/stat sources, invalid/version/cap/no silent clamp |
| Mono progression UI | **4 nhóm PASS:** source DEVB, allocation/HP/stat identity, New binding, canonical Realm boundary presentation |
| Mono selected related regression | **16 nhóm PASS:** Save rollback, manual Load/New/waiting entry và EXP/balance/stat identity liên quan; không chạy full Combat/Magic/Forge |
| Unity Native JsonUtility + SaveManager CLI | **VERIFIED:** legacy/current/future/unknown/invalid/partial/sentinels, repeated Load, contaminated template, actual persisted component fields, write failure and unrelated fields; equipped modifier roundtrip không double |
| Unity MainGame Play Mode CLI | **4 phase PASS:** New/progression/UI4size; Save/Load/Character/Allocation/equipment/Forge; running-combat failedLoad/Travel/Retry; legacy Load/UI rebind/New from contaminated template |
| Metadata/Git | **VERIFIED:** metadata0errors/3baselinewarnings, diff checks, protected-source comparisons và clean worktree sau bàn giao |

Tổng **25 nhóm Mono PASS**, không cộng các lượt chạy lại thành test mới. Native CLI là verifier chạy trong Editor bằng JsonUtility/SaveManager thật, không là Mono serializer stub và không tuyên bố đã chạy NUnit runner. Không dùng PASS lịch sử v0.5/v0.3 làm PASS mới.

## 8. Ma trận Native và lifecycle chi tiết

| Ca bắt buộc | Bằng chứng mới |
|---|---|
| Legacy không Realm / template có Realm khác | Native Load từ JSON strip đúng ba field; template version99/ID khác/entry999 không rò vào result; conservatively version1/LuyệnKhí/entry1 |
| New/current Realm | JSON ghi thật có đủ ba field, Load khôi phục, field khác so sánh đầy đủ |
| Future version / unknown ID / invalid entry | Reject trước mutation; existing invalid/future slot không bị Save hợp lệ ghi đè |
| Explicit sentinels/null / partial fields | Không bị coi là legacy thiếu component; reject |
| Load lặp | Realm/entry/EXP/Level không tăng, equipped Sword Attack+4 và modifier count không nhân sau ba lần Load |
| Save lỗi trước commit | Invalid typed Realm và .tmp-directory fault không đổi byte snapshot `.json` trước đó |
| Load ngay sau Level Up, Character đang mở | Load candidate mới, allocation đúng một lần trên Hero mới, dữ liệu cũ không đổi |
| Load thất bại khi combat chạy | Future Realm slot bị reject khi có live monsters/actor; canonical object/life/state còn nguyên |
| Travel→Save/Load, defeat→Retry | Realm1/entry1/Level45 và camera/combat binding giữ đúng. Travel permission và defeat được fixture cung cấp explicit, không coi là farm tự nhiên |
| Legacy Load→UI→New | Entry live fixture45 đổi về legacy entry1; UI đổi từ Sơ kỳ1 sang Viên mãn; New dùng template clone chứa Realm sai vẫn trở về Level1/EXP0/points0/entry1 |

## 9. Lỗi phát hiện và vòng sửa–kiểm thử

1. Native fixture ban đầu kỳ vọng EXP73, trong khi bước ApplyHeroBalance chuẩn bị dữ liệu đã chuyển EXP thành34200 trước serialize. Chẩn đoán lưu source/loaded JSON chứng minh Load giữ34200 và Realm đúng. Sửa assertion so với snapshot source, không đổi input/production Load/EXP formula. Một vòng sửa fixture; native PASS rồi bổ sung hai test chứng minh persisted fields/sentinel và equipped modifiers theo yêu cầu safety, đều PASS.
2. Lượt compile verifier Play đầu dùng tên `ForgeOperation` không tồn tại. Sửa đúng `EquipmentRefinement.Operation.NormalUp`, không đổi Forge runtime. Lượt Play2 PASS; không giảm assertion hoặc size matrix.

Không vấn đề nào vượt năm vòng sửa. Expected error do fault injection ghi file được ghi lại trong log, không bị coi là gameplay error hoặc che để PASS. Các cảnh báo compiler obsolete/licensing/TLS sẵn có không được sửa ngoài phạm vi.

## 10. Giới hạn / BLOCKED / NOT RUN

- **Không còn BLOCKED cho Realm persistence v0.6 trong phạm vi đã kiểm:** production component và MainGame đã nối sau Native migration PASS, không dùng UI giả khi Save chưa an toàn.
- **PARTIAL:** bằng chứng theo fixture/case nêu trên; EXP được fixture cấp tại mốc, không là long-run balance/natural farm. Catalog chỉ Luyện Khí, không có successor/Thiên Kiếp thực tế.
- **NOT RUN:** Windows Player build/OS-native mouse, full unrelated regression, crash/power-cut giữa file Replace, downgrade bằng executable cũ. Không coi đây là chứng nhận mọi Save corrupt hoặc mọi schema tương lai.
- Chưa triển khai võ học active/boss production hoặc đổi HP theo mode. Quy tắc failure/checkpoint/trial hai pha nằm ngoài v0.6.

## 11. Công việc tiếp theo

1. Nghiệm thu Windows Player/native UI trong profile thử được sở hữu nếu được giao; không dùng handler routing làm native PASS.
2. Trước mở successor/Thiên Kiếp: duyệt catalog/đột phá/failure/cost/trial Save và durable identity; giữ Policy A, không tự mở từ progress eligibility.
3. Chốt policy downgrade/phát hành nếu hỗ trợ chạy lại binary cũ; không giả đổi version equipment để chặn.

## 12. Bằng chứng / checkpoint

Workspace phiên: `C:\Users\hason\Documents\Codex\2026-11-05\ti-p-t-c-d-n`.

- `work/v06/unity-types.log`: baseline four types/PASS; `native-round1.log`, `native-diagnostic.log`, `native-round2.log`, `native-final.log`, `native-equipped.log`: dữ liệu chuẩn bị/FAIL và native PASS thật; `play-round1.log` compile verifier lỗi, `play-round2.log` bốn phase PASS.
- Native profile cuối: `.utmp/realm-v06-784eefd1055d4534b2738abf3982a933`, marker `realm-v06-native`. Scene-free inactive SaveManager cấu hình SaveDirectory trước Awake, không Singleton/player Save I/O. Profile trước đó cùng prefix cũng được sở hữu.
- MainGame: `.utmp/foundation-a7a447d69f0745ce9b873ee60224d9db`, marker/framework verifier cũ, manager owner inactive/configured trước Awake, ignored scene copy. Reset DEVB chỉ trong process thử, không đổi file cá nhân.
- `work/v06/CHECKPOINT.md`, `scope.json`, `mono-counts.json`, `metadata.json`, `generated-imports.json`: checkpoint và checks. `outputs/v06/` có báo cáo, bốn PNG, JSON kết quả và logs tóm tắt; không xuất Save thử vào Git hoặc deliverable.

Đã hoàn thành Phase1–8 trong phạm vi được duyệt và dừng. Không push/merge hoặc thực thi task tiếp theo.
