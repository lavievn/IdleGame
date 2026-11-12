# Hàn Vụ — Vertical slice tham chiếu, 12/11/2026

**APPROVED_SCOPE — chủ dự án duyệt ngày12/11/2026 trong task Local Integration
Baseline.** Đây là phê duyệt phạm vi thử nghiệm, chưa phải implementation hoặc
nghiệm thu. Không bổ sung gameplay, lời thoại, NPC, kết cục hoặc Save field trong
task tài liệu này. Thiết kế nền tại
[baseline](../NARRATIVE_DESIGN_BASELINE_2026-11-12.md), contract tại
[Narrative Foundation](NARRATIVE_EVENT_TIMELINE_FOUNDATION_2026-11-12.md).

## 1. Nội dung được dùng và giới hạn

Dùng **lời nhờ tìm lá bạc/sắc thuốc** đã có ở `hanvu.find_medicine` trong #13/#17
làm biến cố trung tâm. Không thay Hàn Vụ bằng Thanh Khê. Đây là thử nghiệm nhỏ
của foundation, không tuyên bố chốt toàn bộ cốt truyện game.

- Khu vực: Hàn Vụ theo origin tại TV-20 và Cô Hồn Lâm TV-16 hiện có. TV-20 vẫn
  dùng ID catalog cũ; alias Hàn Vụ chỉ trong origin. Không mở bản đồ hoặc tự travel.
- Một NPC thay đổi: người giám hộ đã lưu trong `OriginActor guardian`; tên/giới
  tính giữ nguyên. Chỉ dùng trạng thái **tạm dịu ho sau sắc thuốc** trong nội dung
  hiện có, không định đoạt bệnh nặng/khỏi hẳn/thọ tận.
- Hai lựa chọn: **Nhận lời** và **Từ chối** từ hệ hiện tại. Đóng hoặc không bấm là
  phản hồi trung lập thứ ba, không được ghi thành một trong hai lựa chọn.
- Điểm kết thúc phép đo: sau việc sắc thuốc và khi quan sát được quyền tương tác
  dấu vết tại Cô Hồn Lâm; không mở rộng sang kết cục companion/elite/5 hồi.
- Không thưởng vật phẩm, tiền, stats, công pháp hoặc Mastery mới; giữ năm wave
  thảo dược hiện có sau opt-in. Test có thể dùng fixture tăng tốc đã sở hữu,
  không sửa balance để buộc người chơi farm nhiều giờ.

## 2. Hai đường đi có hậu quả khác nhau

| Lựa chọn / hành động | Hậu quả phải quan sát và giữ được | Không được suy diễn |
| --- | --- | --- |
| A — Nhận lời; hoàn thành điều kiện đang có; chủ động mang về/sắc thuốc | Participation Accepted; herb counter/stage qua Return → SearchForest; guardian được mô tả tạm dịu ho; Cô Hồn Lâm có tương tác **Xem dấu vết** theo StoryOrigin.CanInteract | Không coi accept là tự hoàn tất, auto-travel hoặc khỏi bệnh vĩnh viễn |
| B — Từ chối | Ghi explicit decline; chưa có quest progress từ farm thụ động, guardian giữ trạng thái hiện có, tương tác quest Xem dấu vết chưa được mở bởi việc sắc thuốc | Không gây chết NPC, trừ tài nguyên, đóng map hoặc tự tạo ending. Có thể đổi ý nhận lại theo semantics hiện tại |
| Trung lập — đóng/bỏ qua | Không ghi decline/accept, không đổi mốc hoặc số phận; tiếp tục idle/travel hợp lệ | Không dùng timeout để biến thành B |

Ý nghĩa thử nghiệm của lựa chọn nằm ở **NPC condition và hành động tương tác
quest khả dụng**, không chỉ câu chữ khác nhau. Nhánh B vẫn nghe người đốn củi,
khám phá knot/note và farm theo luật hiện có; không gộp clue thụ động với việc
đã nhận quest. Không gán nội dung hay phần thưởng chưa duyệt cho nhánh B.

Từ chối rồi nhận lại phải giữ lịch sử declinedHerbalOffer và chỉ nhận tiến trình
đang hợp lệ; không lặp effect từ một lần sắc thuốc đã hoàn tất. Không biến lựa
chọn đảo được này thành “event terminal từ chối vĩnh viễn”. Hai nhánh hội tụ sau
khi cùng thực hiện hành động hợp lệ vẫn giữ lịch sử khác nhau.

## 3. Một thay đổi bản đồ/môi trường và một mốc truyện

Thay đổi quan sát được dùng **điểm tương tác đang có ở Cô Hồn Lâm**: trước khi
sắc thuốc, chưa có action quest Xem dấu vết; sau sắc thuốc, action đó khả dụng
khi ở forest và đúng stage. Không đổi HDR/rain/time/audio hoặc tạo marker/asset
mới để thử nhân quả. Đây là thay đổi affordance ở địa điểm trong thế giới, không
phải thêm một biome hoặc block travel. Cần xác nhận bằng view thật và handler,
không chỉ boolean domain.

Mốc truyện thử nghiệm: M0 “trước khi hoàn tất sắc thuốc” → M1 “đã sắc thuốc”.
Đây là **nhãn logic** cho biến cố hiện có, chưa phải ID hay số năm được chốt.
Transition chỉ được xác nhận khi hành động Return → SearchForest hợp lệ commit
thành công. Tên/tuổi thật của mốc và duration chưa chốt; không tăng tuổi giả.

- A có thể đi đến M1 bằng hành động chủ động; B/ignore ở M0 cho đến khi đổi ý và
  đáp ứng điều kiện. Không thêm một mốc để “giải quyết hộ” nhánh không tham gia.
- Environment day có thể đổi trong lúc chờ, active combat/Qi có thể tiến theo
  luật, nhưng M0/M1 và tuổi NPC không đổi vì AFK.
- Với slice hẹp, stage đang lưu có thể làm projection cho M0/M1 và guardian
  condition; không tạo hai field cùng authority. Timeline tổng quát nếu cần lưu
  thêm phải đi qua task persistence/migration riêng đã duyệt.

## 4. Luồng nghiệm thu ngắn

1. Tạo run Hàn Vụ trong fixture sở hữu, ghi snapshot identity/world/Inventory/
   cultivation/EXP. Mở/đóng thoại trước lựa chọn; chứng minh không nhận hoặc từ chối.
2. Từ cùng trạng thái xuất phát, thử B: decline, tới Cô Hồn Lâm bằng đường map
   hợp lệ, farm; quest không tự hoàn thành và không tự thành A. Đọc được manh
   mối tự chọn; idle và world map vẫn dùng được.
3. Thử A trên fixture riêng: nhận lời, năm wave hợp lệ trong rừng, chủ động trở
   lại và sắc thuốc. Không yêu cầu farm/đánh elite hoặc nội dung sau slice.
4. Quan sát NPC condition và action Xem dấu vết; xác nhận M1 chỉ đổi một lần từ
   event hợp lệ. Lặp bấm/replay thoại/accepted callback không apply hậu quả lại.
5. **Save → Load một lần sau resolution** trong fixture riêng. Cùng identity,
   choice history, counters/stage, mốc/projectionNPC/map; không tự chạy reducer để
   “khôi phục reward”. Sau đó lặp Load để kiểm idempotency, không cộng tuổi/EXP/item.
6. Kiểm đổi ý B→A trên fixture thứ ba và Save lỗi/retry; không xóa lịch sử B,
   không double-apply A. Giữ compare/Forge/skills/Qi và map progression ngoài story.

## 5. Acceptance criteria (chưa phải test PASS)

| ID | Điều kiện kết thúc | Cấp bằng chứng cần có |
| --- | --- | --- |
| VS1 | Hai lựa chọn explicit và close trung lập; điều kiện/nhân quả khớp bảng trên | Domain + Play Mode/native input |
| VS2 | Người giám hộ giữ ID/tên/giới tính; condition chỉ đổi từ hành động đúng, không gán death/ending | Domain + viewNPC/journal |
| VS3 | Action tại Cô Hồn Lâm có khác biệt trước/sau A; B không bị khóa travel/farm/clue tùy chọn | Play Mode + Windows view thật |
| VS4 | M0→M1 chỉ một lần sau commit, không do day/night/Qi/EXP/wall-clock/AFK | Domain + Play Mode, pause/unfocused/AFK |
| VS5 | Save/Load giữ cùng hệ quả và identity; lặp Load/Retry/Travel/death không cấp hoặc xóa effect | Native Save fixture + Windows relaunch |
| VS6 | Duplicate/stale command/callback không tăng counters/milestone lần hai; replay chỉ đọc | Domain + actual host routing |
| VS7 | Fault trước/sau replace: toàn bộ snapshot cũ hoặc mới hợp lệ, không half-state/reward | Persistence fixture có quyền sở hữu |
| VS8 | Legacy thiếuOrigin, v1/v2/v3, future/partial/invalid được xử lý đúng; giữ các component khác | Native migration + candidate-before-swap |
| VS9 | Idle không bị modal/timeout mới ép chọn; resize600/800/1150 đọc và bấm được | Play/Windows input + hình ảnh |
| VS10 | Người thử mới mô tả được “tôi nhận và sắc thuốc nên NPC/action thay đổi”; B không bị coi là lỗi chơi | Walkthrough người chơi trên build local xác định |

Không nghiệm thu chỉ vì set flag hoặc in text đúng. Cần cả source snapshot,
before/after NPC/map observations và kết quả Save fixture trên đúng artifact.
Một chu kỳ Save/Load chính là tối thiểu của slice, không thay fault/legacy tests.

## 6. Handoff và các gate

N2 đã được chủ dự án duyệt: giữ Hàn Vụ, nhận/từ chối thuốc, close trung lập,
từ chối có thể đổi ý, M0→M1 logic sau sắc thuốc, projection guardian/Cô Hồn Lâm;
không NPC/thoại/reward/ending/Save field mới. Không cần xin duyệt lại các điểm này.
N0 và đồng bộ tài liệu trên integration local chưa được xác minh từ Cloud;
xem [bàn giao Windows](../automation-reports/LOCAL_INTEGRATION_BASELINE_2026-11-12.md).
N3/N4 cần tác vụ triển khai tiếp theo. Với slice này, ưu tiên stage/flags đã lưu,
không thêm serialized milestone/receipt/NPC condition hoặc migration mới.
Mọi locale/lore mới, NPC outcome khác, phạt nhánhB, reward, tuổi thọ hoặc bế quan
đều cần quyết định riêng; không tự thêm để làm hai lựa chọn “kịch tính hơn”.

Hiện #13/#17 cung cấp phần lớn hành vi tham chiếu; **không làm lại** thoại,
quest counters, journal hay layout đã có. Việc còn lại của foundation là transaction/
dedup/milestone contract và nghiệm thu trên nhánh local đã tích hợp. Chưa chạy
VS1–VS10 như một suite cho build tổng hợp trong task tài liệu này; ghi NOT TESTED.
