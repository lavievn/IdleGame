# CP4 — Regression matrix

Baseline RC2 và PR #28 có cùng danh sách lỗi, lấy từ XML Edit Mode và lượt Play Mode owned-save. Các fixture dưới đây dùng scene thử trống và chỉ tạo actor/UI thuộc test. Không đổi HP, tốc độ di chuyển, tầm nhìn hay damage runtime.

| Test | Baseline Expected → Actual | Nguyên nhân và kiểm tra RC3 |
|---|---|---|
| `AfterDeserialization_DerivedStatsAreRebuiltFromAuthoritativeProgressAndEquipment` | 1554 → 1058 | Fixture Level 7 lưu origin 400, +96 level, +12 phân bổ, +450 growth, +100 bùa. Commit `94cd79c` đã thống nhất HP theo mọi kiểu đánh. So cả ba kiểu với 1058 và giữ assert damage và dữ liệu gốc. |
| `NativeVfxFinish_InjectedLongWalkingStep_IsBoundedWithoutCatchUp` Kiếm/Cung/Trượng | 7,20 / 7,20 / 3,20 → 1,60 ở bước 0,016s | Sau Appear ở vị trí 80%, không có mục tiêu. `ee92f75` khóa walk ở 100 dù có override theo thế. Cả ba test vẫn kiểm vị trí chân, nhiều bước dài và trần 1/30s. |
| `EntryDismissesOldDetailSnapshotWithoutReleasingPause` | “new snapshot” → “Chưa có nhân vật” | Fixture chèn văn bản giả vào panel chỉ số đang lấy dữ liệu live. Dùng panel Nhật ký thật để kiểm hợp đồng snapshot, đóng khi entry, giữ pause và thay văn bản cũ. |
| `LootTargetTracksRectAndUsesRealWindowEdgeWhenHidden` | (320,240) → (656,168) | Fixture trỏ vào card bị tắt mặc định nên nhận đúng fallback mép cửa sổ. Tạo rect Inventory hoạt động và kiểm cả lúc di chuyển lẫn lúc ẩn. |
| `GroupQuestionIsYellowAndReadableThenTurnsRedAfterHalfSecond` 600/800/1150 | ΔX −37 → −27,245 / −27,652 / −27,519 | `c4d266d` cho quái chưa Engaged đi lang thang ở 50. Kiểm độ lớn dịch chuyển local 10 sau khi trừ Ground −37. Giữ kiểm tra marker, delay và cấm đánh khi đang nghi ngờ. |
| `SightRequiresCorrectFacingAndInclusive500UnitGroundDistance` hai hướng | ở 501 kỳ vọng false → true | `10d1bce` đã đặt melee 550, ranged 800. Spawn trong fixture còn roll ngẫu nhiên attack mode. Chọn mode rõ và kiểm điểm ngoài, trong bán kính theo chiều cao và đúng biên cho melee, cung, phép. |

**Dual Progression.** Character UI lấy tốc độ nguồn Qi theo dải hiện tại từ binding. Menu hệ thống hoặc xác nhận thoát giữ đồng hồ đứng, bao gồm lúc timeScale vẫn chạy. Các hành vi EXP, Thiên Kiếp, Mastery và checkpoint Save dùng authority cũ. Không đổi cân bằng Thiên Kiếp.

Log chạy ban đầu `D:\IdleGame\Logs\RC3-unity-EditMode.xml` và `RC3-unity-PlayMode.xml`. Lượt giữa Edit 51 PASS, Play 132 PASS / 2 FAIL. Hai lỗi còn lại do fixture tầm nhìn vẫn random mode. Lượt cuối và Windows nằm trong [báo cáo tích hợp](RC3_INTEGRATION_REPORT.md).
