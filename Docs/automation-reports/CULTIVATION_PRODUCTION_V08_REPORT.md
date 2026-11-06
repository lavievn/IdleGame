# Cultivation Production v0.8 — 60-minute balance and readiness

06/11/2026 · **IMPLEMENTED & VERIFIED trong owned playtest; PRODUCTION BLOCKED, chưa RELEASED.** Hoàn thành profile cân bằng60 phút, clock checkpoint schema2, migration policy/guards, UI và nghiệm thu Unity. MainGame mặc định chưa bật tu luyện vì chưa có consent/upgrade commit/rollback flow cho Save cá nhân. Burning và Thiên Kiếp tiếp tục OFF.

## Repository, baseline và commit

- Repository `lavievn/IdleGame`; branch `feature/monster-awareness-ambush`.
- Worktree thực: `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Baseline `661e5ecc7fffd23695a15f719a46a0aa93cfc94e`, sạch, ahead2 so với remote tracking local lúc bắt đầu. Không pull/fetch nên không khẳng định trạng thái mới nhất của remote.
- Phiên “Kiểm tra trạng thái Git IdleGame” dừng do usage limit trong audit v0.8, chưa có thay đổi v0.8. Không thấy Unity/Mono/Python/bash writer khác khi tiếp quản. V0.7 và hai commit sau đó về sight/base speed được giữ nguyên.

| Commit thực tế | Nội dung |
|---|---|
| `e36714a680b27ba30c7bcc58a6a8f79cc988674b` | Balance v1, initial-state validation, clock schema2/checkpoint/Travel settlement, precision parsing, full scheduler simulation và Mono regression |
| `7aba9d1fc72322ecc893429d96b0b2991bd2c0f4` | Editor launcher, density UI, Unity native verifier, MainGame v0.8 và separate-process replay acceptance |
| Commit chứa báo cáo này | Ba tài liệu bàn giao; SHA cuối cùng và status được lưu trong `outputs/v08/handoff.json` ngoài repo |

HEAD mã/test khi bàn giao tài liệu: `7aba9d1`. Unity evidence ghi baseline HEAD661e5ec vì nghiệm thu trên diff chưa commit; hai commit trên chứa đúng bytes đã kiểm và được đối chiếu staged checksum trước commit. Không coi dòng sourceHead trong evidence là một checkout sạch khác. Sau commit tài liệu, final handoff ghi HEAD cuối và status. Không push/merge/reset/rebase.

## Baseline và starting map

Đọc v0.7 report, Dual Progression/Blood contracts, profile và domain engine, lifecycle Save/UI/clock, WorldMapCatalog và native regressions. Baseline Mono5+19+16+4 PASS trước sửa.

GameManager.InitHeroData gọi WorldMapCatalog.StartNew, chọn ngẫu nhiên một trong `TV-20`, `TL-16`, `XD-11`, `TM-12`, `HT-17`. Không tự mặc định TV-20 là điểm khởi đầu duy nhất. MainGame v0.8 acceptance để nguyên lựa chọn New Hero thực và nhận được TM-12. Không sửa catalog/start selection/topology/travel restriction/waves.

V0.7 dùng profile `playtest:cultivation-v07`, `[0,6,12,20]`, source overrides TV-20/TV-18 và bỏ remainder<0,1s khi rebind. Asset v0.7 giữ nguyên; verifier native v0.7 chạy lại PASS sau v0.8.

## Balance và kết quả60 phút

Profile `balance:cultivation-early-v1`, version1, definition/gameplay schema2; asset `Assets/Editor/CultivationBalanceV1.json`. Chi tiết ở [Cultivation Balance V1](../architecture/CULTIVATION_BALANCE_V1.md).

- Realm Luyện Khí, thresholds `[0,900,2100,3600]`; successor rỗng.
- Density1 tại cả năm điểm khởi đầu; absorption2 Qi/s, efficiency1; refinement input1 Qi/s, efficiency1.
- Initial foundation/reserve/spiritual energy0; spiritual maximum100/recovery2/s.
- Fixed step0,1s, max valid frame0,5s scaled game-time. Idle/movement/combat dùng cùng clock; pause/menu/entry/death/offline không tick.
- Domain giữ absorption → reserve → refinement. Không đổi EXP/drop/monster/growth để ép mục tiêu. Tại bình cảnh cap foundation3600 nhưng reserve tiếp tục tăng, không tự breakthrough.

Native simulator chạy đủ3660 giây bằng **CultivationGameplayBinding.Advance → CultivationSession.Tick** với JSON profile thật. Không dùng công thức xấp xỉ thay runtime, không nhân kết quả phiên ngắn để chứng minh60 phút. Native60 FPS có2263 lần capture/restore xen giữa; chạy lặp cùng input/checkpoint schedule cho output measurement giống hệt. Mono cũng kiểm chạy lặp cùng frame sequence.

| Native scheduler | Sơ→Trung (giây) | Hậu (giây tích lũy) | Viên mãn (giây tích lũy) | Qi accounting error ở3660s |
|---|---:|---:|---:|---:|
| 30 FPS | 900,000000 | 2100,100000 | 3600,100000 | 3,07e-9 |
| 60 FPS +2263 checkpoints | 900,000000 | 2100,100000 | 3600,100000 | 4,03e-8 |
| 144 FPS | 900,000000 | 2100,100000 | 3600,100000 | 7,12e-8 |

Mục tiêu3600s ±5% PASS: đo được **60 phút0,1 giây**, lệch+0,00278%. Phân bổ xấp xỉ15/20/25 phút. Sai số double ở ngưỡng dẫn tới một fixed step bổ sung ở Hậu/Viên mãn; không nới cap hoặc cộng epsilon vào foundation. Sau3660s: foundation3600, reserve≈3720, không có Blood/Burning, realm và Level/EXP không thay đổi vì Qi.

Bốn starting nodes còn lại chạy full3660s Mono10 FPS, crossing900/2100,1/3600,1s, cùng accounting bounds. Các phép đo30/60/144 dùng TV-20 với cùng baseline density1; không tuyên bố đã nghiệm thu native input trên cả năm vùng.

## Substep, Save và compatibility

Schema2 lưu remainder/elapsed/source cùng component canonical mỗi frame hợp lệ. Load giữ remainder, fresh epoch và không cấp lại thời gian; repeated Load/Manual rollback thay timeline theo slot. New bỏ field template. Travel sau validation quyết toán remainder ở source cũ trước đổi map; adapter cũng giữ attribution nếu source thay giữa các Advance. Không tạo scheduler thứ hai, callback retired không thể cấp Qi.

Mono1000 chu kỳ Advance0,037s → capture/restore giữ37s tích lũy; settlement trả foundation37/reserve37. Native SaveManager giữ checkpoint0,037s qua actual file/JsonUtility; cộng0,063s cho đúng0,1s. Native simulation60 FPS có2263 checkpoints; elapsed3660,000000019s, không drift bất thường. Remainder144 FPS cuối≈9,14e-13s. Bounds chống NaN/infinity/âm/frame quá lớn/remainder≥step/elapsed>1e12; không có timestamp wall-clock.

Lỗi phát hiện và sửa: native JsonUtility parse reserve3719,9999999977244 thành3719,9999999977249 (1 ULP). Adapter tái dùng token double đã structural validate và invariant .NET parsing để giữ binary value; giữ assertion exact roundtrip. Không làm tròn liên tục hoặc sửa dữ liệu kỳ vọng để PASS.

[Migration policy V1](../architecture/CULTIVATION_SAVE_MIGRATION_POLICY_V1.md) phân loại New/legacy/current/old config/different config/future/malformed/template. Config cũ/khác/future và component khác schema bị từ chối trước canonical swap/ghi đè; không suy cultivation từ Level, không retag version. Owned fixture legacy initialization zero-state giữ Level45/EXP17/points/gear/world, không ghi file nguồn khi chỉ Load. Đây không phải migration player.

Writer temp/flush/Replace/Move và slot semantics giữ nguyên. Native kiểm BeforeWrite/AfterTempWrite/BeforeReplace, actual temp-creation blocker và Windows destination lock; committed bytes giữ nguyên, orphan temp không được Load, write kế tiếp phục hồi. Không có ledger ngoài slot/backup rotation mới, không đổi Manual rollback. Separate Unity process nạp snapshot cũ giữ component byte-identical, paused checkpoint ổn định, resumed clock chỉ tăng game-time hợp lệ.

## World Qi, UI và MainGame thực tế

Năm start overrides density1;128 node còn lại trong catalog133 dùng fallback1, **chưa cân bằng từng node**. Nguy cơ đã ghi nhận: farm map an toàn cũng có tốc độ Qi như map khó. Không gán giả thông số riêng cho133 map, không liên kết Qi với danger/monster strength, không tự tạo long-mạch/bí-cảnh bonus. Travel MainGame thay stable node và tra fallback1; do các density hiện giống nhau, nghiệm thu không giả vờ có đổi numerical density. Mono custom-profile case density1→3 kiểm attribution0,04s ở source cũ và0,06s ở source mới.

Character giữ layout/theme hiện tại, thêm mật độ trên dòng reserve và nhãn thử nghiệm cho balance v1. Level/EXP, realm/stage, foundation/bar, reserve, absorption/refinement limit, spiritual energy, blood/trial khóa đều có. Native TMP không tràn tại600×338,800×450,1150×647,1360×680; đã xem cả bốn ảnh capture. Không thêm ETA do chưa có forecast đáng tin qua rate/source/lifecycle; không đoán số.

MainGame Play Mode chạy scene copy và manager Awake thật, SaveDirectory riêng đặt trước Awake:

- Natural farm **30,00116 giây realtime**, **30,0069787268294 giây active**, đúng bằng tổng scaled frame time hợp lệ đã lấy mẫu; TM-12,23 damage events, Level1/EXP14, Hero sống. Trong khoảng này không sửa HP/speed/gear/drop/enemy/EXP/actor position. Đây là farm ngắn thực tế, không là bằng chứng duy nhất cho60 phút.
- Sau đó tách stationary/movement fixture: clear encounter, speed0 tạm thời cho idle rồi restore; Qi tăng không cần kills/EXP. Không mô tả khoảng fixture này là natural farm.
- Pause, UI4 sizes, real Save/repeated Load và complete snapshot comparison, invalid future candidate giữ Hero/session/combat life.
- Travel dùng permission fixture (clear origin explicit), không tuyên bố đã farm đủ5 waves; pending settled trước đổi map. Save/Load sau Travel PASS.
- Defeat dùng incoming damage fixture → Retry, Menu/Continue, New với template cố ý làm bẩn; checkpoint/owner/epoch đúng, manual snapshot giữ nguyên, camera/combat identity PASS.
- Separate-process replay dùng bản copy slot chỉ từ owned profile trước đó, giữ cultivation component chính xác, không offline payout; phase1 riêng PASS, không gán PASS cho ba phase không chạy trong replay.

Các thao tác lifecycle gọi method/UI binding trong editor harness; không phải bằng chứng native Windows mouse input.

## Evidence theo môi trường

| Lớp bằng chứng | Kết quả thực tế |
|---|---|
| PURE DOMAIN / Mono UnityStub | **67 nhóm PASS**: balance3, historical gameplay5, dual progression19, related Save/balance16, progression UI4, Inventory/Equipment/Forge/rollback20 |
| UNITY NATIVE | Compile6000.5.0f1 PASS; v0.8 verifier7 check groups PASS, full simulation/native JSON/faults/legacy/fractional; v0.7 compatibility5 groups PASS |
| MAINGAME PLAY MODE | V0.8 phase1–4 PASS; natural farm, clock, UI4 sizes, Save/Load/Travel/Retry/Menu/New; separate-process reload phase1 PASS |
| WINDOWS PLAYER | **NOT RUN**: không build/triển khai profile Editor thành Player và chưa có owned Player input acceptance flow cho balance v1; không lấy editor method calls làm native mouse proof |
| Metadata / scope | 0 errors;3 warning baseline folder-meta Lake/Sea/Steppe sau dọn import artifacts; protected-source comparison và diff check PASS |
| Power cut / kill ngay trong Replace syscall / manual native-input launcher | NOT RUN |

Evidence ngoài Git ở workspace phiên tiếp quản `C:\Users\hason\Documents\Codex\2026-11-06\ki-m-tra-phi-n-ki\outputs\v08`: native/play/restart/v07 results, simulations30/60/144, ảnh4 sizes, natural farm, counts/scope/commits/handoff. Raw Save fixtures giữ trong `.utmp`, không copy/commit hoặc dùng Save cá nhân.

Profile cuối: nativev0.8 `cultivation-playtest-dd21ea413b4a46b2917ca3a6067fbd0b`; MainGame `foundation-0e74d355ca104f94b797e5c1f1714312`; restart `foundation-7d454efa0ed747febdc9999f4f27e50b`; v0.7 compatibility `cultivation-playtest-0cb2e4617ed440298033d0d6058c3f42`.

## Chẩn đoán hữu hạn và scope

Mỗi lỗi độc lập dưới5 vòng: simulator lấy mẫu2 FPS khiến quan sát crossing quá thô cho assertion0,15s, đổi phép đo các start khác sang10 FPS đúng fixed step, không giảm assertion; native exact double roundtrip chẩn đoán1 ULP rồi sửa parse authority (ba lần chạy gồm diagnostic và PASS); bổ sung chạy lặp xác định sau đó PASS. Test double Mono được bổ sung đọc số mũ, không giả mạo native JsonUtility.

Giữ nguyên Camera v0.3/EnvironmentManager, Hero/Monster AI, CombatDamage/CombatStats/CombatBalance, movement100 và monster sight đã duyệt, spawn/loot/forge/weapon behavior, world topology/catalog, boss/martial arts, EXP curve, engine domain/blood/breakthrough/recovery, SaveManager atomic writer và EntityDataSO schema boundary. GameManager chỉ thêm một cultivation settlement guard ở Travel.15 `.meta` tự sinh và font fallback được dọn đúng allowlist/hash, không commit cache/log/raw saves/artwork.

## Gate, rủi ro và ba nhiệm vụ tiếp

**MainGame mặc định:** vẫn chưa bật tu luyện; Save legacy vẫn dùng đường hiện tại. **Owned balance playtest:** chạy được tu luyện60 phút, persistence schema2 và UI thật. **Domain fixture:** Blood/Burning/breakthrough contracts v0.6 vẫn có regression nhưng chưa nối production.

**BLOCKED production:** chưa có consent UI cho legacy player, backup/upgrade commit/rollback transaction và nghiệm thu đường đó. Đã có policy từ chối config/schema không tương thích, chưa có converter các profile. Không lấy im lặng làm consent, không ép New Game, không nhận batch production-ready bằng bool true. Blood Essence Burning/Thiên Kiếp/offline vẫn OFF. Windows Player chưa nghiệm thu.

Rủi ro còn lại: fallback128 map chưa world-balance; per-frame JSON checkpoint cần profiling allocation trước rollout rộng; frame>0,5s bị bỏ theo policy; downgrade sang binary trước guard không an toàn; I/O fault tests không chứng minh power-loss durability. UI nghiệm thu state khoảng32 tu vi, cap/stages60 phút được chứng minh trong simulator, không chạy realtime một giờ để chụp cap3600.

1. Triển khai và nghiệm thu consent/candidate/backup/upgrade commit/rollback cho Save legacy bằng fixture rồi đường người chơi, giữ nguyên các field không liên quan.
2. Chuẩn bị owned Windows Development Player và native input acceptance cho profile v1; đo allocation/frame stalls của checkpoint, sau đó đánh giá mở production gate qua các tiêu chí đã duyệt.
3. Cân bằng world Qi theo dữ liệu được duyệt, tách density/bonus/danger/monster strength; giữ Burning/Thiên Kiếp khóa cho đến batch persistence/gameplay riêng.
