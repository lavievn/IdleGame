# Hướng dẫn Codex — Bách Luyện Tiên Đồ

## Đọc trước khi thay đổi

Đọc [.agents/skills/idle-game-project-rules/SKILL.md](.agents/skills/idle-game-project-rules/SKILL.md)
trước thay đổi mã hoặc kiểm toán hệ thống. Đọc phần đầu [README.md](README.md)
và [trạng thái 25/10/2026](Docs/PROGRESS_2026-10-25.md) khi cần bối cảnh; nhật ký
lịch sử không phải quy tắc runtime hiện tại. Mã hiện tại và yêu cầu mới của người
dùng phải được đối chiếu trước khi suy diễn. Dự án chưa phải Alpha.

## Định hướng gameplay đang phát triển (không phải đặc tả tính năng)

Khi lập kế hoạch **core loop, progression, reward, balancing, map/wave, nhật ký hành trình
hoặc sự kiện NPC**, đọc [định hướng Core Gameplay Loop ngày 25/10/2026](Docs/CORE_GAMEPLAY_LOOP_DIRECTION_2026-10-25.md).
Đây là **tài liệu làm việc có thể sửa đổi**, chứa cả định hướng đã chốt và đề xuất
chưa chốt. Không tự code/đổi gameplay, checkpoint, party, offline progress hay
sự kiện chỉ vì được mô tả trong tài liệu. Luôn đối chiếu mã hiện tại và xin yêu cầu
cụ thể trước khi triển khai. Tham khảo trạng thái đã có/chưa có trong README,
PROGRESS và skill dự án; tài liệu thiết kế không ghi đè hành vi runtime hiện tại.

## Nạp skill theo tác vụ

| Tác vụ | SKILL.md cần đọc ngoài quy tắc dự án |
|---|---|
| Unity/C#, vòng đời, kiến trúc hoặc kiểm thử mã Unity | [.agents/skills/unity-csharp-scripting/SKILL.md](.agents/skills/unity-csharp-scripting/SKILL.md) |
| Save/Load, autosave, schema, migration | [.agents/skills/save-systems/SKILL.md](.agents/skills/save-systems/SKILL.md) và Unity/C# nếu sửa C# |
| Build, đóng gói hoặc Player | [.agents/skills/unity-build-pipeline/SKILL.md](.agents/skills/unity-build-pipeline/SKILL.md) |
| Asset, sprite, VFX artwork, texture/environment | [.agents/skills/create-game-assets/SKILL.md](.agents/skills/create-game-assets/SKILL.md) và Unity/C# nếu sửa integration |

Chỉ nạp những skill/reference liên quan. Thông báo ngắn skill được dùng lần đầu.
Không cài router hoặc các “Related skills” chưa có chỉ vì được nhắc trong upstream.
Không tải cập nhật upstream tự động; xem [quản lý bộ skill](Docs/AgentSkills/README.md).

## Ưu tiên và an toàn

- Yêu cầu người dùng trong task có ưu tiên hơn các hướng dẫn skill. Quy tắc dự án
  đã đối chiếu mã ưu tiên hơn ví dụ tổng quát của upstream; khi có xung đột, nêu
  bằng chứng/giới hạn và phương án, không tự tái cấu trúc.
- Giữ Unity trong ProjectVersion.txt (snapshot 6000.5.0f1), package/backend theo
  cấu hình repo. Không tự nâng/hạ Unity, cài SDK/dependency hoặc Addressables.
- Phân biệt rõ đã có mã, chờ nghiệm thu và chỉ thảo luận. W2.3 checkpoint và HUD
  W3 không trở thành tính năng đã triển khai chỉ vì có trong roadmap.
- Giữ camera Multi-Zone, tọa độ logic, entry/VFX, B2, công thức combat, map/wave,
  inventory và Save/Load ngoài phạm vi task. Không sửa ảnh nguồn/asset hoặc GUID
  chỉ để dọn một cảnh báo. Giới hạn bước di chuyển Hero không phải clamp toàn game.
- Không đọc/ghi/xóa save người chơi để kiểm thử. Fixture SaveManager có I/O cần
  đường dẫn riêng trước Awake, marker/quyền sở hữu và cleanup giới hạn như bộ
  Play Mode hiện có; không chạy các test save có I/O trong Cloud.
- Khi checker upstream báo lỗi ngoài phạm vi, báo cáo, không tự sửa. Chưa có
  PackageCache thì “missing script” có thể chưa xác nhận; không tạo GUID giả.
- Mono/UnityStub PASS không phải Unity/Windows PASS. Không bỏ/làm yếu assertion
  hoặc sửa dữ liệu kỳ vọng khi chưa xác minh thiết kế/mã. Ghi riêng lỗi cũ và lỗi mới.
- Kiểm tra HEAD/branch/working tree; giữ thay đổi có sẵn. Không merge, xóa lịch sử,
  force-push hoặc đổi nhánh ổn định khi chưa được yêu cầu. Tách commit theo nhóm
  logic độc lập, báo SHA/file/test/rủi ro. Chạy `git diff --check` trước bàn giao.

## Khi không có tự khám phá skill

Codex hỗ trợ project skills tại `.agents/skills/`; phiên mới mở từ repo có thể
liệt kê `/skills` hoặc gọi `$idle-game-project-rules`. Danh sách skill của một
phiên Cloud đang mở có thể chưa refresh. Khi skill không xuất hiện trong UI,
**đọc trực tiếp SKILL.md theo các liên kết ở trên**, rồi dùng reference/script
tương ứng trong thư mục đó. Không cần cài plugin, gọi mạng hoặc tạo bản sao skill.
Không tuyên bố auto-trigger đã được nghiệm thu nếu mới chỉ kiểm tra file.



## Hướng dẫn lập trình tối giản (Karpathy-inspired)

Khi sửa, đánh giá hoặc tái cấu trúc mã, đọc thêm
[karpathy-guidelines](.agents/skills/karpathy-guidelines/SKILL.md).
Đây là skill hướng dẫn hành vi: nêu giả định, chọn thay đổi nhỏ nhất, tránh
refactor ngoài phạm vi và xác minh mục tiêu bằng bằng chứng. Không tự cài
plugin/SDK hoặc nạp skill mới trên toàn máy.

**Ưu tiên khi xung đột:** yêu cầu task > quy tắc dự án đã đối chiếu mã >
hướng dẫn chung của karpathy-guidelines. Không biến guideline thành cớ
trì hoãn những hạng mục đã rõ hoặc tăng test không cần thiết. Trên Cloud,
không tuyên bố Unity/Windows Player PASS nếu chỉ xem nguồn hoặc kiểm tra tĩnh.

## Local Windows storage (07/11/2026)

On hason's workstation, the active worktree is `D:/IdleGame/Project` and development
outputs, temporary data, UPM cache and owned fixtures belong under `D:/IdleGame`.
Use `Tools/Local-IdleGame.ps1` (or `D:/IdleGame/Launch.ps1`) for Editor, compilation,
Mono, native verifiers and Windows builds. Do not create new IdleGame builds/caches
under C. Older C paths are compatibility junctions; invoke the real D paths so
owned-fixture guards do not encounter a junction ancestor. The Unity installation
itself remains in Program Files on C. Do not use personal Saves as fixtures.
