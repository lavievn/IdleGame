# Camera Framing v0.1 — BLOCKED, chưa đưa vào sản phẩm

Ngày 05/11/2026. Branch `feature/monster-awareness-ambush`; nền mã trước camera `d4c6a83`. **Không có commit triển khai camera**: candidate chưa đạt đã được giữ ngoài repository, các thay đổi camera/scene/selector test của candidate đã khôi phục về nền đã commit. Commit chứa báo cáo này chỉ ghi nhận kết quả.

## Nguyên nhân có bằng chứng từ mã

Không tìm thấy component `CameraFollow` riêng trong runtime local. MainGame dùng `EnvironmentManager.MultiZonePresentationPan` và `BattleMotion.MultiZoneCamera`. Cờ `useMultiZoneCamera` mặc định true; nhánh này không dùng `cameraPreferredX/cameraSmoothTime` của DeadzoneCamera legacy.

Controller hiện điều chỉnh vận tốc theo sai lệch so với tâm viewport. Hero đang chạy trái cần một sai lệch đủ lớn để controller sinh vận tốc bám tương ứng; tăng speed đơn thuần chưa đặt được Hero ở 65%. MainGame còn `cameraUseCombatCenter = true`, nên các quái đang hiển thị có thể kéo focus khỏi chính Hero. Đây là chẩn đoán theo đường tính/call flow, chưa phải một lần đo camera MainGame mới.

## Cấu hình cũ và candidate

| Hạng mục | Sản phẩm hiện tại, được giữ nguyên | Candidate ngoài repo |
|---|---|---|
| Controller | MultiZone, tâm viewport | Cùng MultiZone; thêm preferred ratio + bù vận tốc chuyển động Hero |
| Focus | Combat center bật | Camera subject Hero, combat center tắt |
| Framing | Không có đích theo hướng 0,65 | Trái 0,65; phải 0,35; giữ hướng khi dừng |
| Chuyển hướng | Acceleration/deceleration hiện có | Giữ giới hạn đó; tái dùng CombatFocusSmoother, smooth time 0,35 |
| Zone/speed | Green .20, yellow .45, red .75; min 30/max 450/correction 900 | Không thay các giá trị này |
| Giới hạn chuyển động camera | Acceleration 900, deceleration 1200, max step 48 | Giữ nguyên |
| Hero/quái/spawn/awareness/Combat | Như baseline | Không sửa |

Không đổi zoom/orthographic size, chiều cao, tốc độ Hero/quái, damage, tầm nhìn 500, alert 0,5s hoặc vị trí spawn. Candidate chỉ dịch trình bày chung; dự kiến giữ khoảng cách tương đối Hero–quái, nhưng chưa chạy hết assertion để xác minh.

## Kiểm thử thực chạy và lỗi

1. Lượt compile đầu: Mono stub không có `Mathf.SmoothDamp`. Đổi candidate sang helper SmoothDamp `CombatFocusSmoother` đã có, không thêm implementation giả vào stub.
2. Lượt tiếp theo biên dịch được. Ca đầu mô phỏng 30 FPS/chạy trái ghi normalized viewport sau pan **0,6604665**; nằm trong tolerance di chuyển 0,015 quanh 0,65. Đây là phép đo **Mono controller**, không phải Unity native/MainGame.
3. Trong cùng ca, sau dừng 6 giây, viewport **0,6673297**; assertion ổn định quanh 0,65 với tolerance 0,002 **FAIL**. Runner dừng tại đây, không hoàn tất toàn ca.
4. Có đề xuất quan sát dừng 20 giây vì controller hiện có crawl chậm gần tâm. **Bộ xét duyệt tự động từ chối lệnh copy fixture/chạy lại**, lý do coi đây là nới tiêu chí 6 giây và vượt giới hạn hai vòng. Lệnh không được thực thi; không lấy bản test 20 giây làm PASS và không thử qua công cụ khác.

| Trường hợp | Kết quả |
|---|---|
| Measurement chạy trái 30 FPS | Có số liệu 0,6604665, chưa phải nghiệm thu cả ca |
| Ổn định sau dừng 6 giây | FAIL: 0,6673297 |
| 60/144 FPS, chạy phải, đổi hướng, pause, stall, relative distance | NOT RUN: runner dừng ở ca đầu |
| MultiZone regression | NOT RUN: lệnh ghép dùng `&&`, nhóm trước thất bại |
| MainGame native, awareness timing, first-visible/first-hit, nhiều aspect | NOT RUN |
| Trạng thái feature | BLOCKED; không phát hành candidate |

Không kết luận camera đã sửa hoặc giữ nguyên gameplay từ một assertion chưa được chạy. Các tham số vùng/tốc độ/damping sản phẩm đều giữ baseline sau khi dừng hạng mục.

## Awareness và nguyên nhân ngoài camera

Tầm nhìn hiện tại 500, yêu cầu hướng nhìn, dấu hỏi 0,5 giây rồi engage; ambush khi chưa bị phát hiện có thể chủ động kích hoạt báo động ngay. Những luật này không đổi. Chưa có dữ liệu mới đo thời điểm thấy quái/phát hiện/bị đánh trên candidate, nên chưa xác minh được liên hệ hình ảnh thực tế.

Tách riêng để kiểm sau: nếu attack range (đặc biệt DEVB) lớn hơn phần không gian phía trước đang hiển thị, giữ Hero ở 65% vẫn không tự bảo đảm mục tiêu luôn on-screen. Không thêm culling damage, đổi spawn hoặc sửa AI để che vấn đề này.

## Bằng chứng, bàn giao và điều kiện tiếp tục

`C:\Users\hason\Documents\Codex\2026-11-04\truy\work\camera-framing-v01\` giữ candidate `EnvironmentManager.cs`, `BattleMotion.cs`, `MainGame.unity` và logs `mono-round1.log`, `mono-round2.log`. `CameraFramingRegression.failed-six-second.cs` là bản ca đã chạy thất bại; file `CameraFramingRegression.cs` ở thư mục làm việc chứa đề xuất 20 giây **chưa được chạy**. Không nằm trong Assets/Tests hoặc commit triển khai.

Muốn làm tiếp cần người dùng phê duyệt ngoại lệ kiểm thử riêng; hướng an toàn là giữ tiêu chí hiện tại rồi sửa nguyên nhân dừng nếu có bản sửa nhỏ, không nới assertion để qua. Chưa triển khai tiếp hoặc xin thêm quyền Windows. Các phase progression/Player độc lập của batch tiếp tục trên camera baseline.
