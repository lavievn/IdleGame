# Cultivation Balance V1 — Luyện Khí đầu game

06/11/2026 · Development batch v0.8. Profile độc lập, nghiệm thu qua domain và Unity; **production gate đóng** theo chính sách migration. Không phải bản phát hành.

## Mục tiêu và phạm vi

Nhân vật mới, không công pháp/đan dược/gear hỗ trợ tu luyện, đạt Luyện Khí Viên mãn sau xấp xỉ **3600 giây tu luyện có hiệu lực**, tolerance nghiệm thu ban đầu ±5% (3420–3780 giây). Idle, di chuyển và combat cùng dùng clock; không cần giết quái. Pause, menu, entry bị khóa, chết và offline không cấp thời gian. Level/EXP giữ authority riêng của EntityDataSO.

Nguồn New Hero thực: GameManager.InitHeroData → WorldMapCatalog.StartNew. Catalog chọn ngẫu nhiên trong năm stable IDs `TV-20`, `TL-16`, `XD-11`, `TM-12`, `HT-17`; không đổi quyền/chính sách chọn map. V0.7 ép TV-20 trong fixture riêng; điều đó không xác lập một điểm khởi đầu production duy nhất.

## Dữ liệu cấu hình

Asset: `Assets/Editor/CultivationBalanceV1.json`. Configuration `balance:cultivation-early-v1`, version1, definition schema2 / gameplay component schema2. Dữ liệu hiện ở Editor để thử an toàn; không dùng asset v0.7 làm production dependency. Sửa thresholds/rates/initial values/source/clock phải tăng configuration version và tuân thủ policy Load; không chỉ đổi con số version trên Save cũ.

| Biến | Giá trị |
|---|---|
| Realm ID / tên / tier | `realm:luyen-khi` / Luyện Khí / 0 |
| Sơ / Trung / Hậu / Viên mãn | `[0,900,2100,3600]` tu vi căn cơ |
| Successor | Rỗng; không tự đột phá |
| Density năm điểm khởi đầu | Mỗi điểm 1 |
| Fallback các node khác | Explicit 1, chưa cân bằng từng node |
| Absorption rate / efficiency | 2 Qi/s trên mỗi density / 1 |
| Refinement input rate / efficiency | 1 Qi/s / 1 |
| Initial foundation / reserve / spiritual energy | 0 / 0 / 0 |
| Spiritual energy max / recovery | 100 / 2 mỗi giây |
| Fixed step / max valid frame | 0,1s / 0,5s scaled game-time |
| Blood / Burning / Thiên Kiếp | Khóa; không có nguồn cấp Tinh Huyết |

Engine runtime giữ absorption → reserve → refinement. Với density1, mỗi giây hợp lệ hấp thu2 Qi, tối đa luyện hóa1 Qi thành1 tu vi. Phần chưa luyện hóa giữ trong reserve; ban đầu không có tài nguyên miễn phí. Ngưỡng900/2100/3600 thể hiện phân bổ15/20/25 phút mà không cần hệ số tốc độ bí mật hoặc đổi rate theo stage/combat. Tại cap3600, foundation giữ nguyên và absorption vẫn cộng reserve. Spirit recovery vẫn là cơ chế domain hiện có; không tạo ability mới.

## Đo lường

`CultivationBalanceSimulation.Run` chạy **CultivationGameplayBinding.Advance → CultivationSession.Tick** bằng chính profile/catalog/rules của runtime. Chạy đủ3660 giây: toàn bộ đường60 phút cộng60 giây ở bình cảnh; không ngoại suy từ60 giây đầu. Ghi timestamp crossing từ clock đã xử lý, tài nguyên, pending time và accounting. Không chạy60 phút realtime.

Test30/60/144 FPS đi qua cùng fixed scheduler, cùng starting state; test60 FPS bổ sung2263 lần capture/restore native JSON hoặc Mono test double. Các điểm khởi đầu còn lại có full simulation10 FPS (lấy mẫu đúng fixed step). Realm không đổi, Burning không phát sinh, Level/EXP không tăng vì Qi. Tolerance crossing phân bổ là0,15s; acceptance60 phút ±180s. Sai số số học ở ngưỡng có thể khiến Hậu/Viên mãn được quan sát ở2100,1/3600,1s; không cộng tu vi bằng epsilon để che sai số.

Native timestamps và accounting cuối nằm trong [báo cáo v0.8](../automation-reports/CULTIVATION_PRODUCTION_V08_REPORT.md). Kết quả sau checkpoint có thể lệch thời điểm crossing một fixed step vì cách biểu diễn số thực; tổng tài nguyên/time có bounds riêng, không dùng tolerance5% để che drift Save/Load.

## Clock và lifecycle

Chỉ GameManager.LateUpdate gọi clock; UI không cấp Qi. Dùng Time.deltaTime scaled, không dùng timestamp máy tính. Frame âm/NaN/infinity/>0,5s bị reject nguyên frame, không tích offline/catch-up. Đây là policy đã có từ v0.7; frame stall dài không tính là thời gian tu luyện hợp lệ.

Schema2 giữ `clockRemainder`, `clockElapsed`, `clockNodeId` trong component cùng canonical snapshot. Mỗi frame hợp lệ publish checkpoint kể cả chưa đủ0,1s; Save lấy snapshot đồng bộ hiện tại. Load tạo fresh epoch nhưng giữ thời gian lẻ; repeated Load khôi phục snapshot thay vì cộng thời gian. New bỏ dữ liệu template và tạo owner/clock mới. Retry/Disable/Menu phục hồi từ checkpoint đã publish khi bind lại; callback retired không thể cấp tài nguyên.

Trước Travel đã validate, `SettlePending` quyết toán phần thời gian đã trôi ở source cũ. Entry bị pause; khi gameplay tiếp tục, lookup source stable ID mới. Adapter cũng xử lý source change trước Advance để không dùng density mới cho thời gian cũ. Remainder phải hữu hạn, không âm và nhỏ hơn step; elapsed hữu hạn, ít nhất bằng remainder và tối đa1e12 giây. Không lưu epoch/frame index hoặc timestamp wall-clock.

Native JsonUtility được giữ cho cấu trúc/serialization. Sau structural validation, các double authority được đọc lại bằng invariant .NET parsing từ token gốc để tránh lệch1 ULP của native JSON parser; không làm tròn/cộng bù theo kỳ vọng test.

## World Qi và rủi ro fallback

Catalog có133 node; chỉ năm điểm khởi đầu có override density1 được scope trong batch.128 node còn lại dùng fallback1 rõ ràng. Node không có override/legacy lookup cũng dùng fallback1; Save world node không hợp lệ vẫn bị SaveManager từ chối như trước. Chưa cân bằng từng map, long mạch, bí cảnh hoặc bonus môi trường.

Density không liên kết SuggestedLevel, độ nguy hiểm, sức mạnh quái, spawn, drop, EXP, travel restriction hoặc wave clear. Hệ quả cần cân bằng sau: người chơi có thể chọn map an toàn để tu luyện cùng tốc độ với map khó; không giả vờ map khó đã có lợi thế Qi. Không tự tăng sức mạnh quái để bù density trong batch này.

## UI và rollout

Character hiển thị Level/EXP riêng, realm/stage, foundation/bar, reserve, density, absorption/refinement input limit, spiritual energy và blood/trial khóa. Nhãn THỬ NGHIỆM phân biệt profile balance với production. Không thêm ETA: chưa có forecast bao phủ source/rate/lifecycle tương lai; không hiển thị số suy đoán.

Trong Unity chọn **Bach Luyen → Playtests → Cultivation balance v1 - 60 minutes (isolated)**, Chơi mới → xác nhận → Nhân vật. Launcher tạo scene copy ignore và thư mục `.utmp/cultivation-playtest-<GUID>` có marker; Save directory được đặt trước Awake. V0.7 vẫn có menu/profile/schema riêng không đổi asset.

MainGame mặc định tiếp tục tắt tu luyện theo [migration policy](CULTIVATION_SAVE_MIGRATION_POLICY_V1.md). Không phát hành qua một bool true; cần hoàn thiện consent/upgrade commit/rollback và nghiệm thu trước khi thay gate. Rollback tính năng phải giữ field Save, guard version/config và không cho binary cũ ghi mất component.


## v1.1 versioned production extension — historical balance above retained

The v0.8 profile and config1 remain unchanged historical authorities; the old rollout/fallback discussion above describes that baseline. Current default production is `production:cultivation-v11-capacity`/3 with all133 frozen WorldQi2 entries and `capacity:cultivation-v1`/1. Config1/2 Saves keep their original rules unless the player consents to a forward upgrade. See [capacity contract](CULTIVATION_CAPACITY_CONTRACT_V1.md) and [v1.1 report](../automation-reports/DEVELOPMENT_V11_REPORT.md).

Thresholds remain `[0,900,2100,3600]`, initial foundation/reserve/energy0. At TV-20/TL-16/XD-11/TM-12/HT-17 supply1Qi/s limits every stage to1foundation/s from zero reserve, despite rising character capacity. Five native3660s simulations reach Viên mãn at≈3600.1s. No epsilon grant, new EXP curve or offline time is used.

Capacity3 absorbs `min(environmental supply, actual-stage absorption capacity)` and independently refines the reserve available after that absorption up to its refinement capacity. Luyện Khí stage capacities are1.2/1,1.35/1.08,1.5/1.16,1.65/1.24. Hotspot5 does not grant5absorbed/s: native runtime reaches Viên mãn≈3304.3s and reserve≈1506.675 after3660s. At.6/.8, final foundations≈2196/2928 with no reserve. Existing old-config reserves are preserved through upgrade; there is no stockpile wipe, decay or new realm unlock. Future major-realm entries remain fixtures, not authored balance.

The eight-rate comparison,30/60/144FPS result and injury/regression/old-clock attribution are actual Unity native session/binding simulations, separately classified from real MainGame short farm, owned stage-boundary setup and Player UI-handler tests. Supply, capacity, actual absorption and actual refinement are shown separately. Blood effective combat realm never grants capacity; Burning/Trial stay OFF in gameplay.
