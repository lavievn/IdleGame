# Baseline tích hợp Agent Skills — 25/10/2026

Snapshot project: `adcae91c9f35a510b6f813728cc5703445056dc7`, nhánh nguồn main.
Không thay runtime/Assets/Packages/ProjectSettings/Tests~. Báo cáo này là baseline
chỉ đọc, không xác nhận trò chơi hoặc một bản build đã PASS.

## Công cụ upstream đã rà soát

- `unity-csharp-scripting/scripts/check_unity_project.py`: Python 3.10+, thư viện
  chuẩn, đọc Assets/embedded packages và GUID của package cache/local package.
  Không ghi file, không gọi mạng/subprocess. Gợi ý “fix” chỉ là chuỗi báo cáo,
  không phải sửa tự động. Chạy một lần với `--json --max-issues 0`, exit **0**.
- `create-game-assets/scripts/asset_report.py`: đọc PNG bằng Pillow và in JSON;
  chạy trên Ground2 runtime đã có, không tạo/chỉnh ảnh. Exit **0**, `ok: true`,
  2172×724 RGBA, alpha 0–255, không có vi phạm kích thước/alpha.
- `build_preview_sheet.py`: yêu cầu `--out`, có mkdir/save output; không chạy với
  asset thật trong task này. Test helper chỉ tạo và dọn fixture thư mục tạm.
- Không cài SDK, Unity Editor, pip/npm package hoặc plugin. Không chạy build,
  gameplay, bộ Mono hoặc Save/Load I/O. Không truy cập save người chơi.

## Kết quả checker Unity

Chi tiết nguyên kết quả: [baseline-unity-audit.json](baseline-unity-audit.json).
Đã kiểm tra **727 file**, **0 ERROR, 62 WARNING**, không cắt bớt danh sách.

| Code | Số lượng | Ý nghĩa / xử lý trong task này |
|---|---:|---|
| missing-script | 58 | **Chưa xác nhận**: Library/PackageCache chưa có, công cụ không phân giải được GUID của các package. Không được kết luận 58 component hỏng, không tự tạo GUID giả hoặc đổi scene |
| missing-meta | 3 | Thiếu metadata thư mục runtime Lake, Sea, Steppe; đã có trước task skills, giữ nguyên |
| orphan-meta | 1 | Assets/Resources/OpeningBackground.meta còn nhưng thư mục tương ứng không có; giữ nguyên, cần review riêng nếu muốn xử lý |

Không dùng `--strict` để biến cảnh báo baseline thành lỗi mới. Khi Unity đã có
package cache ở máy người dùng, có thể chạy lại checker để phân loại tham chiếu;
không tự cài package trong Cloud hoặc sửa asset ngoài phạm vi để đưa warning về 0.

## Kiểm tra helper upstream

Từ checkout upstream SHA `184b72be02884f782a0058f2eeae2ce4277453e4`, đã rà soát
fixture trước chạy. Tests dùng TemporaryDirectory, chỉ tạo dữ liệu giả trong
thư mục tạm, không mở scene hoặc SaveManager thật:

- `python3 -B -m unittest discover -s tests -p test_check_unity_project.py`:
  **32 test PASS**, không skip/fail.
- `python3 -B -m unittest discover -s tests -p test_asset_tools.py`:
  **5 test PASS**, không skip/fail, Pillow có sẵn.

Các test chạy trên checkout upstream; hash lock và so sánh byte xác nhận helper
được cài chính là bản đó. Đây là test công cụ Python, **không phải 37 test gameplay
hoặc Unity**. Không chép cả test suite/library upstream vào project.

Verifier cục bộ cũng đã qua **7 ca kiểm soát** trong bản copy thư mục tạm: bản
cài sạch/khôi phục sạch PASS; sửa upstream, mất reference, sai tên skill, link
hỏng và mất route AGENTS đều bị từ chối đúng. Không thay file cài thật để thử lỗi.
Đã dùng thêm PyYAML có sẵn để xác nhận độc lập frontmatter của cả năm SKILL.md là
YAML hợp lệ; verifier khi dùng thường ngày vẫn chỉ cần thư viện chuẩn Python.

## Khả năng khám phá Codex

Xem [codex-discovery.json](codex-discovery.json). Cấu trúc project skill và đường
đọc qua AGENTS.md đã được chuẩn bị; verifier kiểm tra file đọc được, name/description,
resource/link và hash. Catalog của phiên Cloud không refresh từ filesystem.
CLI mới không mở state runtime được do read-only filesystem; chưa thực hiện được
một phiên Codex mới auto-trigger skill. Không khởi chạy model turn hoặc thay quyền.
Fallback là root AGENTS.md trỏ thẳng đến năm SKILL.md và bảng chọn theo tác vụ.

## Các phần chưa được xác minh

- Không biên dịch/chạy Unity Editor hoặc Windows Player. Không đánh giá lại
  gameplay, Save/Load, checkpoint W2.3 hay các cảnh báo thiếu package bằng Unity.
- Verifier chỉ hiểu frontmatter portable đang dùng; không phải validator mọi
  biến thể YAML và không chứng minh auto-trigger native.
- Hash lock giúp phát hiện thay đổi local ngoài review, không xác minh bản quyền
  artwork của game hoặc bảo đảm mọi lời khuyên upstream phù hợp project.
