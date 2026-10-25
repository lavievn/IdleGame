# Agent Skills — Bách Luyện Tiên Đồ

Bộ hướng dẫn dùng cho Codex của `lavievn/IdleGame`, tích hợp ngày **25/10/2026**.
Không thay runtime, scene, prefab, artwork, package hoặc quy tắc Save/Load.

## Nguồn, nhánh và giấy phép

- Upstream: <https://github.com/gamedev-skills/awesome-gamedev-agent-skills>.
- SHA ghim: **184b72be02884f782a0058f2eeae2ce4277453e4**.
- Đã đọc README, `docs/INSTALLATION.md`, `docs/COMPATIBILITY.md`,
  `docs/SKILL-FORMAT.md` và router tại SHA đó trước khi chọn cách copy thủ công.
  Các tài liệu gốc truy cập qua URL GitHub ghim SHA, ví dụ
  [hướng dẫn cài](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/184b72be02884f782a0058f2eeae2ce4277453e4/docs/INSTALLATION.md).
- Giấy phép upstream **Apache-2.0**; giữ nguyên [LICENSE](upstream/LICENSE) và
  [NOTICE](upstream/NOTICE), kể cả copyright Abhishek Barali và contributors.
  Không áp giấy phép này lên toàn bộ game hoặc khẳng định quyền đối với artwork.
- [upstream-lock.json](upstream-lock.json) lưu đường dẫn gốc/cài đặt và SHA-256 của
  **toàn bộ 18 tệp upstream** cùng hai tệp pháp lý. Không viết lại/rút gọn nội dung
  upstream; không thêm file hoặc sửa trực tiếp trong bốn thư mục đó.
- Nhánh nguồn thực tế: **main, adcae91c9f35a510b6f813728cc5703445056dc7**.
  Khi kiểm tra, W2 còn ở `292baa18254545f1ec5023c4faf11780a630d514`, là tổ tiên
  của main. Ba commit mới của main chỉ cập nhật README và xác định main là nhánh
  phát triển chính. Vì điều kiện dùng W2 làm nguồn không còn đúng, tạo nhánh
  **tools/codex-gamedev-skills** từ main; không merge vào bất kỳ nhánh nào.

## Bộ được cài

| Skill | Mục đích | Nguồn upstream |
|---|---|---|
| [unity-csharp-scripting](../../.agents/skills/unity-csharp-scripting/SKILL.md) | Lifecycle C#, component, serialization; checker GUID/.meta chỉ đọc | `skills/unity/unity-csharp-scripting` |
| [save-systems](../../.agents/skills/save-systems/SKILL.md) | Dữ liệu chuẩn, save slot, migration, crash safety | `skills/disciplines/save-systems` |
| [unity-build-pipeline](../../.agents/skills/unity-build-pipeline/SKILL.md) | Scene/build, backend, stripping và kiểm chứng Player | `skills/unity/unity-build-pipeline` |
| [create-game-assets](../../.agents/skills/create-game-assets/SKILL.md) | Art direction, sprite/tile/texture, alpha/provenance, QA | `skills/disciplines/create-game-assets` |
| [idle-game-project-rules](../../.agents/skills/idle-game-project-rules/SKILL.md) | Quy tắc dự án, kiến trúc đã đọc, trạng thái thực tế và phạm vi | Viết riêng; snapshot mã tại SHA nguồn trên |

Mỗi thư mục có `SKILL.md`; mọi `references/`, `scripts/`, `assets/` và
`agents/openai.yaml` được cung cấp bởi từng skill đều được giữ đủ. Không cài cả
thư viện bằng npx hoặc plugin. Không cài router upstream: router là dispatcher
76 skill/đa engine, có reference tới bộ không được chọn. Bốn skill không phụ thuộc
runtime vào router; bảng chọn gọn trong [AGENTS.md](../../AGENTS.md) thay nhiệm vụ
định tuyến. Các tên ở “Related skills” chỉ là gợi ý, không phải dependency còn thiếu
và không được tải tự động.

`create-game-assets/assets/asset-manifest.json` là template upstream: đường dẫn
`assets/player/player-idle.png` trong đó là ví dụ để thay khi lập manifest thật,
không phải một asset hoặc dependency runtime cần tải cho dự án này.

## Cách Codex nạp ở phiên mới

1. Checkout nhánh có commit tools này và mở Codex với thư mục làm việc là gốc repo
   hoặc thư mục con. Hướng dẫn root `AGENTS.md` yêu cầu đọc project-rules trước sửa mã.
2. `.agents/skills/<name>/SKILL.md` là cấu trúc project skill được upstream chỉ rõ
   cho Codex. Trên client hỗ trợ native discovery, `/skills` liệt kê tên;
   `$idle-game-project-rules` hoặc `$save-systems` gọi tường minh. Description dùng
   cho lựa chọn ngầm, body/reference chỉ cần nạp theo tác vụ.
3. Khi danh sách Cloud chưa refresh hoặc không có native discovery, **đọc file
   trực tiếp theo AGENTS.md**: project-rules trước, rồi chỉ skill tương ứng. Đây là
   phương án tương thích đã kiểm tra đường dẫn/nội dung, không cần user gửi lại
   URL hoặc prompt cài đặt và không cần mạng sau checkout.
4. Không cần sao chép sang `.codex/skills`, sửa cấu hình global, tạo symlink hoặc
   cài thêm plugin. Sau một lần cài/cập nhật nên mở phiên mới để kiểm tra catalog.

**Giới hạn đã đo:** Codex CLI 0.159.2 có mặt nhưng các thử nghiệm `skills/list`
và `debug prompt-input` bị chặn bởi state runtime/file system chỉ đọc của Cloud;
catalog executor của phiên này trả 0 skill. Không khởi chạy model turn, không
đổi quyền hệ thống hoặc cấu hình global để vượt giới hạn. Do đó **chưa xác nhận
auto-discovery/auto-trigger của một phiên Codex mới**, không coi kiểm tra file là
kiểm thử tự động kích hoạt. Xem [bằng chứng khám phá](codex-discovery.json).

## Kiểm tra bộ cài còn hoạt động

Từ gốc repo, dùng Python **3.10+**, không cần dependency cho verifier và GUID checker:

```bash
python3 Tools/verify_agent_skills.py
python3 Tools/verify_agent_skills.py --json
python3 .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json --max-issues 0
```

Verifier chỉ đọc: kiểm tra đủ năm skill, frontmatter portable name/description,
UI metadata tùy chọn, hash upstream/LICENSE/NOTICE, resource/link cục bộ và cú pháp
Python/JSON. Không phải parser mọi biến thể YAML hoặc test native Codex. Nó không
gọi mạng, auto-update, thực thi các helper khác hoặc sửa file. Bản ghi kiểm tra:
[verification.json](verification.json).

Tool artwork có thêm dependency Pillow **>=10,<14**, đã có Pillow 12.3.0 trong
Cloud lúc tích hợp; không cài mới. Nếu thiếu thì báo, chỉ cài khi task cho phép:

```bash
python3 .agents/skills/create-game-assets/scripts/asset_report.py Assets/Resources/Environment/Plains/Ground2.png --expect-size 2172x724 --require-alpha --json
```

`asset_report.py` chỉ đọc và in kết quả. `build_preview_sheet.py` **có ghi ảnh tại
--out và tạo thư mục output**; chỉ chạy khi tác vụ cho phép, dùng output riêng,
không trỏ vào asset/source đang có. Không biến lệnh pip trong ví dụ upstream thành
bước cài tự động. Không cần chạy Unity hay bộ gameplay để kiểm tra cài skill.

## Baseline và xung đột đã xử lý bằng hướng dẫn

Xem [BASELINE.md](BASELINE.md), [audit Unity JSON](baseline-unity-audit.json) và
[audit ảnh mẫu](baseline-asset-report.json). Không sửa asset/.meta theo baseline.

- Upstream Unity nhắm **6000.3**, repo ghim **6000.5.0f1**, UTF 1.7.0. Giữ phiên
  bản repo; không tự đổi Packages, backend, stripping, scene hoặc cài SDK.
- Ví dụ Rigidbody/Transform không thay tọa độ Canvas/BattleArea. Ví dụ deltaTime
  không bỏ clamp di chuyển Hero 1/30s hoặc áp clamp lên toàn bộ game.
- Ví dụ save có root version/backup/checkpoint chỉ là mẫu. Repo dùng version
  theo thành phần và guard future schema; giữ migration cũ. `isDirty`, AutoSave,
  Continue và Load cần truy vết trước sửa, không coi generic pattern là tính năng
  đã triển khai. W2.3 còn thảo luận; world-map không tự đổi node, legacy version 0
  còn đường tăng map cũ và phải được phân biệt.
- Quy tắc source art/định vị/VFX và mapping hiện tại ưu tiên hơn ví dụ crop/resize
  chung. Không tự sửa artwork hoặc giả định asset đã được chấp thuận cho phát hành.
- Không đổi Mono PASS thành Unity/Windows PASS; dự án chưa Alpha. Hướng dẫn xử lý
  lỗi checker không cấp quyền sửa file ngoài task; warning PackageCache không đủ
  để kết luận Missing Script thật.

## Cập nhật upstream có kiểm soát

1. Chỉ bắt đầu khi có nhiệm vụ cập nhật. Xác minh branch/HEAD/working tree và tạo
   nhánh tools riêng; không chạy `npx skills update` hoặc pull trong phiên thường.
2. Clone/fetch upstream vào thư mục tạm riêng; chọn SHA cụ thể, đọc diff của bốn
   thư mục, LICENSE và NOTICE so với SHA khóa. Rà soát script I/O/network, reference,
   yêu cầu phiên bản/dependency và xung đột project-rules trước khi chạy mã mới.
3. Sau review, thay **đúng bốn thư mục đầy đủ** từ SHA mới trong nhánh tools;
   không chép đè project-rules/AGENTS hoặc tự bổ sung toàn bộ router/library.
   Giữ thông báo bản quyền; nếu sửa nội dung upstream phải ghi rõ local patch,
   không gọi đó là bản copy nguyên gốc.
4. Cập nhật `upstream-lock.json` từ nội dung đã so sánh trực tiếp với checkout
   upstream; ghi SHA/ngày/hash từng file và giấy phép. Không đơn thuần lấy hash
   của một bản local đã sửa để che sai khác.
5. Chạy verifier, xem link/resource, helper tests/baseline phù hợp trong chế độ
   chỉ đọc; ghi thay đổi cảnh báo thay vì tự sửa project. Kiểm tra catalog/đọc
   file trên Codex mới; ghi riêng khả năng auto-trigger còn chưa được xác nhận.
6. `git diff --check`, kiểm tra chỉ hướng dẫn/tools thay đổi, commit/push nhánh
   tools theo nhiệm vụ; không tự merge. Nếu lỗi, quay lại bản ghim qua một commit
   được review, không xóa lịch sử hoặc sửa gameplay để hợp thức hóa skill.
