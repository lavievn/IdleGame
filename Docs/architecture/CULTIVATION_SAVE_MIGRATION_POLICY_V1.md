# Cultivation Save Migration Policy V1

06/11/2026 · v0.8 · **Production migration BLOCKED/OFF**. Đây là policy và guard được kiểm bằng fixture, không phải quyền nâng cấp Save cá nhân. Không bắt người dùng New Game để tiếp tục sử dụng Save cũ.

## Authority và classification

`EntityDataSO.cultivationJson` chứa component độc lập. Level/EXP/points, roots, inventory, equipment, forge và world giữ authority cũ; không suy tu vi từ Level. Load v0.8 không thay thresholds/config của Save rồi chỉ cập nhật version để vượt validation.

| Dữ liệu nguồn | Hành vi đã triển khai |
|---|---|
| New Game không có state | Bỏ component tồn dư ở template; profile đã explicit chọn khởi tạo đúng initial values và owner mới |
| Legacy Save không có field hoặc chuỗi rỗng | Context bình thường vẫn dùng Save legacy không có tu luyện; chỉ owned playtest đã explicit `initializeLegacy=true` tạo candidate zero-state, không tự ghi file |
| Component hợp lệ, cùng schema/config ID/version | Validate trước candidate swap; restore resources và clock; fresh epoch, không thưởng thời gian |
| Configuration version cũ | Từ chối Load với lý do không tương thích; giữ snapshot nguồn/state hiện tại, chưa có converter production |
| Configuration ID khác | Từ chối; dùng đúng profile/context gốc hoặc migration được version hóa về sau |
| Future configuration/component version | Từ chối đọc và ghi đè; không downgrade |
| Malformed/partial/null/duplicate/unknown fields hoặc type sai | Reject trước overwrite; giữ valid canonical Hero/session |
| Template/Character có component tồn dư | New không kế thừa; Load reset field trước overwrite và dùng component đã validate từ file, không dùng dữ liệu template |

V0.7: definition schema1, config `playtest:cultivation-v07` version1, gameplay component1. V0.8: definition schema2, config `balance:cultivation-early-v1` version1, gameplay component2. Domain payload v1 cho blood/debt vẫn giữ contract cũ; adapter v2 validate clock rồi truyền payload domain v1 vào candidate validator. Không coi component1 là future chỉ vì profile mới; báo cần migration explicit. Profile v0.7 đọc đúng snapshot cũ, không dùng balance mới để reinterpret số tu vi tuyệt đối.

## Clock schema2

Ba field thêm: `clockRemainder`, `clockElapsed`, `clockNodeId`. Remainder bounded `[0,step)`, elapsed hữu hạn `[remainder,1e12]`; source string giới hạn128 ký tự, remainder>0 bắt buộc có source. Tất cả field authority bắt buộc hiện diện, exact scalar type; unknown field cần schema explicit. Không có wall-clock timestamp/offline bonus. Schema1 giữ policy lịch sử bỏ remainder khi rebind; không tự thêm field clock vào Save v0.7.

Mỗi frame hợp lệ publish checkpoint cùng state canonical. Capture không flush thời gian chưa xử lý vào foundation; Load khôi phục remainder và dùng lại một lần khi đủ step. Travel quyết toán remainder ở source cũ trước mutate map. Repeated Load/rollback đi về timeline trong slot, không cộng thêm thời gian từ session bị bỏ. Native doubles được đọc từ token invariant đã validate để giữ chính xác binary value của số đã serialize.

## Transaction boundaries đã có

SaveManager giữ temp write → flush writer/FileStream.Flush(true) → Windows File.Replace hoặc File.Move. Validate source và existing slot trước commit; future/unknown/invalid không bị ghi đè. File temp mồ côi không là canonical Load. Không thêm root version, backup rotation hoặc ledger ngoài slot. ManualSave1/2 vẫn rollback theo snapshot, New/Reset giữ hai manual slots như trước.

GameManager.Load tạo EntityDataSO candidate, SaveManager validate component trước overwrite; chuẩn bị binding candidate trước hủy/swap canonical Hero. Candidate thất bại giữ live state/session/life. Save write failure giữ committed file và session đang chạy. API SaveManager.LoadGame trực tiếp vẫn là API cho destination candidate; batch không tái cấu trúc rollback toàn bộ các field legacy bên ngoài cultivation.

Native fixture kiểm before-write/after-temp/before-replace, actual temp path bị chặn và Windows destination lock không cho Replace; so byte snapshot ngay trước fault, Load giữ final, write kế tiếp phục hồi. Đây không phải chứng minh mất điện hoặc kill-process đúng giữa syscall. Không đọc/ghi/xóa Save cá nhân để thử.

## Điều kiện nâng cấp production còn thiếu

Legacy player migration cần lựa chọn explicit của người chơi trong flow thật. Trước khi canonical commit phải:

1. Đọc/validate source không mutate; hiển thị khởi tạo Luyện Khí theo balance đã chọn, không đổi EXP/gear/world và không cấp Tinh Huyết.
2. Chuẩn bị candidate version hóa và backup/rollback source byte-identical theo chính sách được duyệt; không thêm backup rotation chung bằng suy diễn.
3. Chỉ commit sau đồng ý explicit; failed write/load giữ source và live session; restore rollback không tạo duplicate rewards/clock.
4. Nghiệm thu upgrade/cancel/restart/fault/rollback bằng fixture, rồi xác nhận đường người chơi và binary compatibility trước mở production.

Batch hiện **không có consent UI hoặc production upgrade transaction**; do đó gate đóng đúng yêu cầu. `initializeLegacy=true` trong launcher/editor verifier là đồng ý cho fixture sở hữu, không phải consent của người chơi và không mở MainGame mặc định. Không lấy im lặng làm chấp nhận.

Configuration change chọn một trong ba đường được duyệt về sau: giữ profile cũ; converter có schema/config version và rollback; hoặc từ chối Load với thông báo hợp lệ. Không âm thầm chuyển absolute foundation sang bảng thresholds mới hoặc tính lại theo Level.

## Rollback tính năng

Giữ `cultivationJson` và compatibility guards ngay cả khi tắt scheduler/UI; không xóa field để giả lập rollback. Hiện context không bật profile từ chối component khác cấu hình để tránh Save làm mất state. Binary trước guard có thể không hiểu field; không bảo đảm downgrade qua các binary đó. Không copy owned playtest slots vào thư mục player Saves.

Burning/active debt/charges/receipts vẫn bị gameplay profile từ chối; không xóa nợ để Load. Burning durable settlement, Thiên Kiếp và offline progression chưa được phép mở theo batch này.
