---
name: idle-game-project-rules
description: >
  Read before changing or auditing lavievn/IdleGame (Bach Luyen Tien Do): Unity C#,
  architecture, combat presentation, camera, movement, UI, assets, save/load,
  testing, builds, or Git delivery. Preserve verified project behavior and
  distinguish implemented code from acceptance pending and future designs.
---

# Quy tắc dự án Bách Luyện Tiên Đồ

Đọc skill này trước khi thay đổi mã. Các đường dẫn dưới đây tính từ gốc repository.
Snapshot đã đối chiếu mã: `adcae91c9f35a510b6f813728cc5703445056dc7`, ngày 25/10/2026.
Khi mã mới khác snapshot, đọc lại hàm liên quan và cập nhật kết luận; không áp một
ghi chép lịch sử lên phiên bản mới. Đọc [kiến trúc và bằng chứng](references/architecture.md)
khi cần truy vết hệ thống. Yêu cầu hiện tại của người dùng có ưu tiên hơn skill.

## Chọn hướng dẫn theo tác vụ

- Unity/C# và vòng đời: đọc skill `unity-csharp-scripting` đã cài cạnh skill này.
- Save/Load, migration, autosave: đọc thêm `save-systems`.
- Build/đóng gói: đọc thêm `unity-build-pipeline`.
- Sprite, environment, VFX/artwork: đọc thêm `create-game-assets`.
- Chỉ đọc reference chuyên biệt khi cần. Không tự cài các “Related skills” chưa có.
- Ví dụ upstream là tham khảo, không phải yêu cầu thay kiến trúc. Nếu xung đột,
  nêu bằng chứng và đề xuất trong phạm vi tác vụ, không tự tái cấu trúc.

## Ba trạng thái phải phân biệt

1. **Đã triển khai, xác minh trong mã:** scene MainGame; combat theo thực thể/B2;
   camera Multi-Zone; entry/VFX; di chuyển giới hạn dt; wave theo quãng đường;
   SaveManager có guard phiên bản và đường dẫn tùy instance; chín bộ environment.
   Có mã không đồng nghĩa đã được nghiệm thu Unity ở mọi tình huống.
2. **Đang phát triển/chờ nghiệm thu:** Inventory/Equipment/Skill và HUD hiện tại
   chưa hoàn thiện; kiểm thử end-to-end mọi map/Player, profiling frame dài sau
   VFX, seam artwork ngoài Plains còn tồn đọng. README và
   `Docs/PROGRESS_2026-10-25.md` là trạng thái mới hơn các nhật ký W1/W2 cũ.
3. **Thiết kế/thảo luận:** checkpoint Save/Load W2.3 chưa chốt/tích hợp đầy đủ;
   HUD W3 ba thẻ Hero, bố cục mới và log ẩn mặc định chưa được triển khai trong
   snapshot. Cảnh giới/Thiên Kiếp trong tài liệu thiết kế không tự trở thành luật
   runtime. Không thêm các tính năng này khi tác vụ không yêu cầu.

Toàn dự án là prototype/development build, **chưa phải Alpha**. Nghiệm thu tạm
một hạng mục không nâng trạng thái phát hành của cả game.

## Kiến trúc và giới hạn thay đổi

- Unity được ghim trong `ProjectSettings/ProjectVersion.txt`: **6000.5.0f1**;
  Unity Test Framework **1.7.0**, URP **17.5.0**, Input System **1.19.0** trong
  `Packages/manifest.json`. Không đổi theo baseline 6000.3 của upstream.
- Global build scene list chỉ bật `Assets/Scenes/MainGame.unity`; SampleScene
  không được bật. Kiểm tra Build Profile riêng nếu tác vụ liên quan build.
- `GameManager` điều phối New/Continue/Load/Retry/Reset/Travel, UI và entry/wave.
  `HeroController`/`MonsterController` quản lý actor; `HeroCombatState` giữ trạng
  thái theo Hero; `CombatManager` định tuyến mục tiêu, timer, đạn và damage.
- `EnvironmentManager` quản lý BattleArea, chuyển đổi tọa độ, tick di chuyển và
  camera; `GroundPresentation` chỉ trình bày environment/depth. `BattleMotion`
  chứa công thức chuyển động; `CombatBalance`/`CombatDamage` giữ công thức combat.
- Thứ tự khai báo: EnvironmentManager **-200**, CombatManager **100**,
  GroundPresentation **200**. `Update` và `LateUpdate` vẫn là hai pha khác nhau;
  không suy rằng toàn bộ callback của một component chạy trước component khác.
- UI hiện tại dùng Canvas/RectTransform, TMP, StartMenuUI, OpeningCinematicUI,
  UIManager và CustomInteractable. Không tự đổi UI framework hoặc bỏ cơ chế input
  cửa sổ Windows. Giữ layout Play Mode đã lưu và kích thước 600/800/1150.
- Không đổi GameManager, camera, SaveManager, inventory hoặc scene chỉ vì một
  ví dụ upstream gợi ý kiến trúc khác. Giới hạn diff theo tác vụ được giao.

## Gameplay, entry và combat presentation

- Idle thiên về quan sát, không ép tương tác liên tục. Trong world-map hiện tại
  (`worldMapVersion >= 1`), nhân vật **không tự chuyển map** khi clear wave;
  WorldMapCatalog quản lý node, quyền travel và vòng farm. Save legacy version 0
  còn nhánh `EntityDataSO.CompleteWave()` tăng map cũ; không xóa/đổi migration
  đó hoặc mô tả hai luồng là giống nhau chỉ để áp quy tắc mới.
- Phân biệt cinematic/entry/VFX đang khóa gameplay, khám phá chờ quãng đường,
  chiến đấu, game over và hủy/chuyển map. Xác minh điều kiện mở khóa và đường hủy.
- Main Menu mới dùng full opening; Load/Travel/Retry trong gameplay dùng đường
  entry ngắn hiện có. Không vô tình bật lại Black Curtain hoặc phát cinematic.
- `CompleteGameplayEntry()` mở khóa sau VFX và gọi wave đầu. Wave đã clear dùng
  `distancePerWave`/quãng X chung; `waveDelay` hiện chỉ cho replay wave bị thoát.
  Không áp lại thiết kế chờ 5 giây cũ cho mọi wave.
- B2 là đồng đội thử nghiệm, không mặc định đã có hệ party save hoàn chỉnh.
  Giữ HP/timer/target/Image độc lập, life identity và an toàn khi object pool tái dùng.
- Sprite bridge: Attack được bảo vệ khỏi Hit thường; popup/damage vẫn chạy;
  Hit không xếp hàng; chết/ẩn/đổi thế/tái dùng phải hủy trạng thái hình ảnh cũ.
- Không sửa damage, range, cooldown, chỉ số hoặc cân bằng nếu không được yêu cầu.

## Camera, chuyển động và artwork

- Phân biệt BattleArea-local, anchoredPosition theo parent, world và screen.
  Dùng `EnvironmentManager.Position()`/`SetPosition()` và so sánh trong cùng hệ.
  Không so trực tiếp anchoredPosition của hai parent khác nhau.
- Không dịch Hero hoặc teleport đội hình để che lỗi camera. Không tự đổi
  Multi-Zone/deadzone/SmoothDamp. Đo logic foot, sprite foot, VFX và pan trước.
- Giữ camera, Ground/parallax và UI đồng bộ. Hero/B2 dùng bước di chuyển tối đa
  **1/30 giây, bỏ thời gian dư** qua `HeroMovementDeltaTime`; không áp clamp đó
  lên cooldown, damage, status, idle progress hoặc toàn bộ Time.deltaTime.
- Hero dùng điểm chân cho biên Y; thân có thể nhô lên. Ranged đổi làn theo mục
  tiêu giữ ổn định, không random mỗi frame; exploration chỉ tính X thực tế.
- Không lật HP bar/damage text cùng sprite; không tắt GameObject logic chỉ để
  ẩn thân Hero trong VFX. Thiếu asset/hủy entry phải khôi phục trạng thái hợp lệ.
- Nguồn art: `Assets/StreamingAssets/ArtSource/Plains/`; runtime:
  `Assets/Resources/Environment/`. Giữ nguồn và GUID, không tự sửa artwork cũ.
  Dùng mapping/cache bốn layer hiện có, không tự thêm preset/render system mới.
- Các tile nối theo mép render thực tế, không thêm +1/epsilon để che seam. Lỗi
  màu/alpha của ảnh khác với lỗi hình học. Chỉ sửa pixel khi tác vụ cho phép.

## Save/Load và an toàn dữ liệu

- Trước thay đổi, truy vết nguồn gọi UI → GameManager → SaveManager và schema
  `EntityDataSO`. Không suy từ thiết kế mong muốn rằng code đã làm như vậy.
- Hiện có AutoSave, ManualSave1, ManualSave2; JSON dùng phiên bản theo thành phần.
  Không tự thêm một root `version`, backup rotation hoặc đổi checkpoint theo
  ví dụ `save-systems`. Mọi thay đổi phải xét migration cũ, rollback, idempotency
  và tránh thưởng/nhận EXP/item trùng sau Load/Retry/crash.
- Giữ guard future-version trước mutation và quét toàn bộ inventory; không
  hạ phiên bản, mất affix/lịch sử rèn hoặc ghi đè file không tương thích.
- `TryLoadSlot()` tạo snapshot riêng trước thay runtimeHeroData. Điều này không
  có nghĩa mọi đường gọi trực tiếp `SaveManager.LoadGame()` đều rollback an toàn.
- New Game/Reset giữ hai slot thủ công theo đường hiện tại. Không tự đổi
  isDirty/AutoSave/Continue hoặc ghi/xóa dữ liệu người chơi để thử.
- Fixture có I/O phải cô lập đường dẫn **trước Awake**, GameObject inactive trước
  AddComponent, cấu hình bằng `ConfigureSaveDirectoryBeforeAwake()`. Mỗi ca có
  GUID/marker, kiểm tra đường dẫn/symlink, chỉ dọn file sở hữu, không xóa đệ quy.
  Không dùng save thật; SaveLoad Play Mode I/O không chạy trong Cloud theo hướng
  dẫn hiện có của bộ test. Đường dẫn mặc định vẫn là persistentDataPath/Saves.

## Kiểm thử và Git

- Đọc `Tests~/run.sh` và README của nhóm test liên quan trước khi chạy. Chọn
  kiểm thử theo diff; không tự chạy gameplay/save I/O cho thay đổi tài liệu.
- Ghi rõ **Mono/UnityStub**, **Unity Edit/Play Mode**, **Windows Player**. Không
  đổi Mono PASS thành Unity PASS, không công bố build PASS khi chưa build/chạy.
- NUnit hiện tại từng không hỗ trợ NonParallelizable và Assert.Multiple; dùng
  API đã được project kiểm chứng, giữ assertion và xác minh hành vi mong đợi.
- Báo riêng lỗi có trước và lỗi mới; full Mono runner đã từng bị chặn tại
  `Tests~/TerrainRegression.cs:41` vì reflection `GroundPresentation.fixedRect`.
  Kỳ vọng terrain cũ cũng có thể lạc hậu sau các bộ ảnh mới: phân tích trước sửa.
- Chạy GUID/.meta checker chỉ đọc khi phù hợp; warning PackageCache thiếu không
  chứng minh script runtime bị mất. Không sửa asset/GUID tự động theo báo cáo.
- Kiểm tra branch/HEAD/working tree; bảo toàn thay đổi đang có. Không merge main
  hoặc nhánh khác, xóa branch/lịch sử, force-push hay rebase nếu chưa được yêu cầu.
  Mỗi nhóm logic độc lập có commit riêng trong phạm vi được giao.
- Báo file thay đổi, commit SHA nếu có, kiểm thử thực chạy và giới hạn/rủi ro;
  kết thúc kiểm tra diff bằng `git diff --check`. Không tự cập nhật bộ skill này.
