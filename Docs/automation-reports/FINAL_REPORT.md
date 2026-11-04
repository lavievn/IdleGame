# Báo cáo thực thi tự động — 05/11/2026

## Trạng thái và giới hạn

- Branch: `feature/monster-awareness-ambush`, PR #11; mốc bắt đầu `95a2456`, working tree sạch và đồng bộ remote.
- Đã thực hiện **3/3 nhóm độc lập**, dừng vì đạt giới hạn; không có nhóm triển khai thứ tư.
- Không gặp lỗi Save/Load chưa rõ nguyên nhân, xung đột người dùng hoặc kiểm thử quan trọng thất bại. Mỗi nhóm đạt ngay vòng sửa–kiểm thử đầu; không dùng vòng thứ hai.
- Không merge main, đổi nhánh/base PR, thêm package, thay artwork, đổi schema save hoặc mở rộng gameplay. Checkout G: được giữ nguyên.
- Báo cáo dùng tên thư mục sẵn có `Docs/automation-reports/`; đường `docs/automation-reports/` trỏ cùng nơi trên Windows, tránh tạo hai root khác case trong Git.

## Các commit và kết quả

| Nhóm | Commit | Hoàn thành | Kiểm thử thực chạy |
|---|---|---|---|
| 1 — xác minh đường Load | `9f1a828` | Sửa fixture dùng Bow được trang bị thật thay stance cũ khi unarmed; mở target `--manual-load-ui`, giữ toàn bộ assertion | Mono/UnityStub 8/8 PASS |
| 2 — P1 modal chặn Retry | `a389ee0` | Đóng System Menu/chi tiết Hero-log-damage khi Game Over/entry/title chiếm UI; không đổi Pause | Unity Play Mode 9/9 PASS; Mono desktop/menu/load 59 nhóm PASS |
| 3 — P2 phản hồi B2 khi Pause | `11aee13` | Nút B2 vô hiệu, giải thích cần Resume; khôi phục nhãn/callback; cache phản hồi thay vì ghi text mỗi frame | Unity Play Mode 2/2 PASS; Mono formation/B2 39 nhóm PASS |

Báo cáo từng nhóm: [01](GROUP_01.md), [02](GROUP_02.md), [03](GROUP_03.md).
Kết quả máy đọc: [validation.json](validation.json). Báo cáo cuối được commit riêng dạng docs-only, không phải nhóm công việc thứ tư.

## Phạm vi bằng chứng

- Unity đúng bản ghim **6000.5.0f1**. Hai lượt Play Mode compile mã Unity và chạy riêng ca UI bị ảnh hưởng, không skip.
- Ca native nhóm 2 tái hiện ranh giới trạng thái và routing Retry qua hit-test/handler; không gọi toàn bộ OnHeroDied/Retry có save I/O. Kiểm cả Pause/Resume và snapshot mới.
- Ca native nhóm 3 kiểm callback marker, trạng thái đầy/trống, theme, click-through và dữ liệu không đổi; không coi marker là spawn B2 end-to-end. Guard/callback gameplay B2 gốc không thay đổi.
- Mono Load dùng fixture GUID/thư mục tạm riêng sẵn có; không truy cập Saves người chơi. Native UI không tạo SaveManager.
- 59 nhóm Mono desktop gồm lại 8 ca Load của nhóm 1; không cộng thành số ca duy nhất. Các kết quả thuộc commit của từng nhóm, không dùng kết quả cũ thay validation mã mới.
- FAIL: **0** trong các nhóm đã chạy. `git diff --check` đạt. Metadata **0 lỗi, 3 cảnh báo cũ** Lake/Sea/Steppe. Font/meta do Unity tự sinh được hoàn nguyên/dọn đúng phạm vi; không thay asset nguồn.
- Raw XML/log được giữ tại `C:/Users/hason/Documents/Codex/2026-11-04/truy/outputs/automation-run/` trên máy này; không commit log Editor/credential vào repo.

## Vấn đề còn tồn tại / NOT RUN

- Chưa chạy Windows Player, native transparent-window input/resize desktop hoặc bấm chuột thật trên thay đổi mới.
- Chưa kiểm Native Save/Load end-to-end với Inventory/Forge đang mở; không suy từ các fixture UI rằng luồng đó đã PASS.
- Failure cũ `old battle has an in-flight shot` đã được giải quyết ở **setup fixture** nhóm 1; toàn target desktop nay qua 59 nhóm. Không sửa combat để ép test qua.
- Các failure `--distance-waves` / `--magic-persistence` từng ghi nhận trước automation không thuộc nhóm này, không chạy lại; không kết luận trạng thái hiện tại.
- Lỗi khởi động Editor.Search/video/TLS đã ghi ở phiên trước không được sửa trong các nhóm nhỏ này.

## Đề xuất nhóm tiếp theo

Kiểm end-to-end trên Unity/Windows Player: Save/Load khi Inventory/Forge mở, thay snapshot trang bị và kiểm HP/stats/UI/đạn cũ sau Load. Dùng profile GUID cô lập trước SaveManager.Awake; nếu có bất nhất Save/Load chưa rõ nguyên nhân thì dừng. Không tự triển khai đề xuất trong lần chạy này.
