# Hàn Vụ — Cloud story polish, 12/11/2026

Nhánh bàn giao: `feature/cloud-hanvu-story-polish`. Nền:
`feature/story-origin-thanhkhe-foundation` tại
`5915829aa2510a739bde64addf0a41b705c1b8eb`, đầu Draft PR #13 khi bắt đầu.
PR mới nhắm vào nhánh nền này để chỉ hiển thị phần sửa bổ sung. Không merge,
rebase hoặc sửa PR đang mở.

## Kiểm tra trước khi sửa

- Working tree ban đầu sạch, HEAD `work` tại `a774218` của `main`. Đã fetch toàn
  bộ nhánh remote và đọc `AGENTS.md`, skill dự án, Unity/C#, Save, định hướng
  core loop và cả hai task trong `Docs/story/`.
- `development_0.1.7` tại `fafc02d` là nền phát triển của các PR hiện hành.
  `main` chưa có hệ Hàn Vụ. Nhánh PR #13 có đủ vertical slice, optional story
  (#14) và narrative/UI polish (#15), gồm các commit `e6af529`, `e7e678f`,
  `685a1b2` và báo cáo `5915829`; đây là nền đầy đủ nhất cho nhiệm vụ.
- Các PR đang mở đã rà: #11 monster awareness, #12 Hero PNG animation,
  #13 Hàn Vụ và #16 Weather/Sky/Audio. Cả bốn đều Draft. Nhánh #11 là tổ tiên
  của nền truyện (0 commit riêng còn thiếu; nền truyện đi trước 82 commit).
- Đã đối chiếu mã thay vì coi hai task handoff là phần còn phải làm mới.
  Các kết quả Unity/Windows trong báo cáo cũ thuộc lần chạy trước trên máy khác,
  không phải bằng chứng nghiệm thu của nhánh Cloud mới.

## Những phần đã có trên nền, giữ và kiểm tra lại

| Yêu cầu | Mã đã triển khai trên nền |
| --- | --- |
| Tự do nhận, từ chối hoặc bỏ qua | `HerbalParticipation`, lựa chọn `Pending` của ba kỳ ngộ; travel theo adjacency/clear/Boss, không theo stage truyện. Farm chưa nhận không cộng lá bạc. |
| Mở đầu tự nhiên, tên và giới tính ổn định | Sáu lượt lời trong `HanVuNarrative`; người nuôi ho nhưng quan tâm Hero, Hero đề nghị tìm thuốc, bạn thuở nhỏ được giới thiệu trước khi giao thuốc. Danh tính lưu trong origin v3. |
| Tiều phu tùy chọn | Nhãn tương tác ở Cô Hồn Lâm, có thể đi qua hoặc quay lại nói chuyện; không khóa combat/travel. |
| Manh mối theo tiến trình | Nút dây sau 40% quãng khám phá, mảnh giấy sau wave hợp lệ thứ hai; tách quan sát/lời kể/suy đoán. Vật chứng cuối đòi đúng Elite, không suy diễn bắt cóc. |
| HUD và nhật ký gọn | Chỉ hiện state `CanFight`; thẻ xếp sát từ phải sang trái. Nhật ký/Ẩn gợi ý ở dưới Hero, thoại và nhật ký từng mục trong panel 300 px, có replay/phân trang. |

## Phần sửa bổ sung

1. **Không ghi lời kể chưa đọc.** Trước đây bấm tiều phu ghi luôn cả vị trí ở
   câu sau. Nay Hero hỏi trước; chỉ khi câu trả lời được hiển thị mới ghi
   `heardWoodcutter` và autosave qua đường hiện có. Đóng ngay câu hỏi, rời map
   hoặc Load chưa đọc câu trả lời đều không được tự tạo lời kể. Câu trả lời
   chứa đủ chi tiết đã ghi trong nhật ký: mang giỏ, thong thả, lối rẽ phía bắc.
2. **Phân biệt từ chối và đóng.** Nút lưu lựa chọn từ chối đổi từ “Để sau” sang
   “Từ chối”. Nút “Đóng” vẫn trung lập; có thể quay lại nhận thuốc sau khi từ chối.
3. **Đóng thoại cũ ở ranh giới gameplay.** Entry, Game Over, Retry hoặc chuyển
   phiên không giữ một cuộc thoại có thể tự ghi thêm cờ khi HUD mở lại, kể cả
   dùng lại Hero/map. Menu tạm che HUD vẫn giữ câu đang đọc.
4. **Không dành chỗ cho Hero vắng mặt.** `VisibleCardHeight` trả 0 khi không còn
   thẻ Hero hoạt động; vẫn giữ chiều cao đúng khi chỉ còn đồng đội hoặc khi
   Hero chính trở lại. Không thay đổi actor, HP hoặc logic combat.
5. **Kiểm thử có thể chạy không cần Editor.** Thêm runner thuần dữ liệu, dùng
   nguyên các lớp production liên quan và UnityStub hiện có. Mở rộng test về
   bốn tổ hợp giới tính, từ chối/bỏ qua, wave không liên quan, giấu gợi ý và
   migration v1/v2. Runner không tạo manager hoặc truy cập save.
6. Cập nhật fixture HUD vốn còn kỳ vọng placeholder/bố cục cũ theo đúng đặc tả
   hiện tại; thêm trường hợp mất Hero cuối, hồi sinh và ranh giới thoại. Smoke
   MainGame thêm đóng trước câu trả lời của tiều phu rồi Save/Load và nói lại.

Không đổi schema origin v3 hoặc migration production. Các save cũ đã có
`heardWoodcutter=true` tiếp tục giữ lời kể đã ghi, không bị xóa lại.

## Kiểm thử thực hiện trong phiên Cloud

| Kiểm tra | Kết quả và giới hạn |
| --- | --- |
| `Tests~/run-hanvu-story.sh` | **PASS 11/11 nhóm Mono/UnityStub**: bốn nhóm catalog/travel/world JSON, hai nhóm truyện có sẵn, năm nhóm mới về thoại, lời kể, kỳ ngộ, manh mối và migration. Không phải Unity serialization hoặc SaveManager round trip. |
| Standalone Roslyn compile runtime | **PASS**, 0 lỗi / 93 cảnh báo với DLL Unity **6000.5.0f1**, UI/TMP/InputSystem đi kèm bản cài Cloud. Biên dịch cùng cách ở nền `5915829`: cũng 0 lỗi / 93 cảnh báo. Không phải Editor import/build. |
| Standalone Roslyn compile `HudW3Tests.cs` | **PASS**, 0 lỗi / 2 cảnh báo API obsolete với NUnit/DLL Unity. Chỉ biên dịch; không chạy Play Mode. |
| `Tests~/run.sh --han-vu-origin` | **BLOCKED trước thay đổi**, 12 lỗi có sẵn: 10 lỗi API UnityStub thiếu ở runtime và 2 lỗi test HP legacy tham chiếu `HeroHealthMultiplier`. Không sửa combat hoặc làm yếu assertion để thông qua runner này. |
| GUID/.meta checker | **PASS** 0 lỗi, 62 cảnh báo; thiếu PackageCache nên các tham chiếu package chưa xác nhận. Không sửa asset/GUID. |
| `git diff --check` | **PASS**. |
| Unity Play Mode/MainGame, Development017 regression | **NOT TESTED** trong phiên này. Smoke có Save I/O không chạy trên Cloud theo `AGENTS.md`. |
| Save/Load qua SaveManager, Retry gameplay thực | **NOT TESTED**. Chỉ xác minh migration/state thuần dữ liệu; đã bổ sung smoke để chạy sau trong fixture cô lập. |
| Render chữ/vùng bấm 600×338, 800×450, 1150×647; chuột thật; chơi đủ tuyến ở nhịp mặc định | **NOT TESTED**. Không có screenshot/video gameplay mới hoặc lượt nghiệm thu Editor/Player trong phiên này. |
| Windows Player/build, lỗi teardown UnityPlayer.dll đã ghi trước đây | **NOT TESTED**; không kết luận đã sửa hoặc tái hiện lỗi thoát. |

Lệnh chạy test thuần dữ liệu, với compiler C# 7.2+ và Mono trong PATH:

```bash
bash Tests~/run-hanvu-story.sh
# Hoặc dùng wrapper/compiler và Mono đã có, không cài thêm dependency:
CSC=/path/to/csharp-compiler-wrapper MONO=/path/to/mono bash Tests~/run-hanvu-story.sh
python3 .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
git diff --check
```

Phiên này dùng Roslyn `csc.exe` và Mono đã có dưới
`.onboarding/unity/6000.5.0f1/Editor/Data/MonoBleedingEdge` của môi trường Cloud,
không mở Editor, không dùng cache asset hoặc save từ máy nhà. Runner có một
adapter môi trường chỉ ném lỗi nếu test vô tình gọi API trình bày ngoài phạm vi.

## Tệp và commit

- `414a267` — `GameManager.cs`, `HanVuNarrative.cs`, `UIManager.StoryOrigin.cs`,
  `HanVuOriginSmoke.cs`: thời điểm ghi lời kể, hội thoại, nhãn lựa chọn, đóng
  thoại ở ranh giới gameplay và smoke đóng sớm/Load.
- `8259c45` — `HudW3View.cs`, `Assets/Tests/Presentation/PlayMode/HudW3Tests.cs`
  và README cùng thư mục: chiều cao HUD rỗng và các ca kiểm thử native.
- `557f975` — `Tests~/StoryOriginRegression.cs`, `HanVuStoryRunner.cs`,
  `run-hanvu-story.sh`: runner và regression thuần dữ liệu.
- Commit sau cùng chỉ thêm báo cáo này. Không thêm asset/script mới dưới
  `Assets`, không tạo `.meta` hoặc GUID thủ công.

## Tích hợp và nghiệm thu còn lại

- PR này phụ thuộc nội dung PR #13 vì lấy nền trực tiếp từ nhánh truyện.
  Chưa kiểm tra việc tích hợp vào `main` hoặc hợp nhất mọi nhánh đang mở.
- Với PR #16, **cùng sửa `Assets/Script/GameManager.cs`** ở các phần khác nhau.
  Có nguy cơ cần xử lý khi tích hợp; chưa khẳng định có conflict hunk.
  Weather/Time, Audio Phase 2, Sky/Rain không nằm trong diff mới.
- Với PR #12, không có file implementation trùng với diff mới. Vẫn cần nghiệm
  thu HUD/thoại cùng animation khi hai nhánh được tích hợp ở nhiệm vụ riêng.
- Không sửa camera tracking, CombatManager, công thức combat, loot, world-map
  authority, scene, package hoặc cân bằng.
- Nghiệm thu tiếp: đọc/đóng/replay tiều phu trước và sau câu trả lời; Save/Load
  bằng fixture cô lập ở cả hai thời điểm; chết/Retry giữa thoại; thử 0/1/2/3
  Hero, gục/hồi sinh/thu hồi; kiểm tra chữ, vùng bấm và nhật ký ở ba viewport.
  Chạy lại MainGame/Development017 và Player trước khi chuyển PR khỏi Draft.
