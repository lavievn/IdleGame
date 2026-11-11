# 0.1.8 — Hero PNG integration trên Codex Cloud (2026-11-10)

## Phạm vi đã triển khai

Tiếp tục từ `development_0.1.8-cloud` @ `7f638c2`, PR #12. Chỉ thay lớp
hiển thị trong `Assets/Script/CultivationSpriteBridge.cs`; giữ một body Image,
không thêm Animator/component phát animation hoặc Spine runtime song song.
Không dùng Windows local. Không merge, force-push, đổi Unity/package hay thêm nội dung game.

Đã đưa **103 PNG nguyên bản** vào `Assets/Resources/HeroAnimation/` cùng
`SharedHero.json`. Các đường Resources và thứ tự `00000…` được khai báo trong
profile; không phụ thuộc thứ tự import hoặc GUID Sprite. Toàn bộ canvas giữ
360×500, RGBA; không crop, rescale, sửa pixel hoặc đưa `archer_run` vào runtime.

| Thư mục nguồn | Trạng thái | Khung | FPS tạm dùng | Phát |
| --- | --- | ---: | ---: | --- |
| sword_idle | Idle | 48 | 24 | Lặp |
| sword_walk | Walk | 17 | 24 | Lặp |
| sword_run | Run | 11 | 24 | Lặp |
| sword_hit | Hit | 27 | 30 | Một lần, 0,9 giây |

FPS là cấu hình thử nghiệm vì ZIP không kèm thông số playback; chỉnh trong JSON.
Hit dài 0,9 giây chỉ là thời gian trình bày đủ 27 khung; Attack vẫn có thể ngắt
Hit và luôn chặn Hit trong cửa sổ Attack. Damage/cooldown không đợi animation.

Một profile chung được chọn độc lập Kiếm/Cung/Trượng/Tay không. Các HeroController
đồng đội dùng cùng bridge cũng nhận profile này, giữ Image/đồng hồ riêng như trước.
Bridge đọc `MovementFrame`, `MovementDistanceThisFrame`, `IsExplorationMovement`,
`IsDeployed` từ controller hiện có: có bước khám phá/formation → Walk; có bước
combat approach → Run; đứng yên/telemetry cũ/không deployed → Idle. Không suy
ngưỡng tốc độ, không đổi công thức di chuyển hoặc lane Y. Khi pause, giữ frame.

Điểm đăng ký cố định **(220,17)** tính từ góc dưới trái canvas, PPU **6**, scale
và flip hiện có; chỉ pivot/size của body child thay đổi. Không neo theo bbox mỗi
khung. Các tư thế chạy có chân nhấc lên vẫn giữ chuyển động tác giả. Root Hero,
HP, anchor VFX và `VisibleBodySize` cũ giữ nguyên. Cần nghiệm thu độ trượt chân
và kích cỡ với renderer thật, đặc biệt lúc chuyển giữa PNG mới và fallback cũ.

## Trạng thái thiếu ảnh và đối chiếu handoff

Yêu cầu mới nhất của người dùng “trạng thái không có ảnh giữ animation hiện tại”
được ưu tiên so với các đề xuất cũ trong handoff. Ở HEAD trước thay đổi,
`Init(false, ...)` gán `normal=LoadFrame(DefaultHeroTexturePath)` và
`attack=hit=null`. Vì vậy Attack hiện tại thực tế giữ **một khung Normal** tại
`Assets/Resources/CultivationSprites/Swordsman_Normal.png` trong **0,16 giây**;
bản này giữ nguyên bitmap/cửa sổ ấy cho mọi vũ khí. Đây không phải animation
đánh nhiều khung. `Swordsman_Attack.png` có trong repo nhưng không phải bitmap
Hero đang dùng tại baseline, nên không tự đổi sang nó theo ghi chú cũ.
Không lấy khung Hit làm Attack. Cast/SkillRelease không được thêm luồng mới;
Death vẫn ẩn Hero ngay theo controller hiện tại.

Thiếu profile → trình bày cũ. Thiếu bất kỳ khung trong một clip → hủy các Sprite
đã tạo dở và dùng bitmap cũ cho riêng clip đó; không phát sequence thiếu khung.
Sprites tạo một lần khi bind, giữ lại khi đổi vũ khí, giải phóng khi destroy.
Không load Resources hoặc cấp phát array mỗi frame.

Không sửa HeroController, CombatManager/projectile, BattleEffects/VFX, HP/skill,
SaveManager/schema/slot, Thiên Quan, scene/prefab, camera, item hoặc balance.
Không chạy test save I/O và không đọc/ghi/xóa save người chơi.

## Import và nguồn asset

Nguồn: `hero.zip` người dùng cung cấp trong phiên chat; không suy diễn giấy phép
hay quyền tác giả ngoài việc dùng asset được yêu cầu cho task này.
ZIP SHA-256: `716ea87bfc174666145a3bb168f305965fba72bb201d249d8e8409db72d3a698`.
103 PNG runtime chính là bản full-resolution gốc, đã so byte với ZIP; ZIP còn
12 ảnh `archer_run` 360×640 không dùng. Không cần giữ thêm bản sao PNG hoặc ZIP
trong build. Một số rìa ảnh có pixel/alpha nhiễu và một số tư thế chạm mép canvas;
không tự chỉnh artwork trong task này.

`Assets/Editor/HeroPngImportSettings.cs` cấu hình lần import Unity: Texture2D
Default, không NPOT scale, không mipmap, Bilinear/Clamp, alpha đầu vào, sRGB,
không CPU readback, không compression, max 512. Bridge tạo full-rect Sprite
thay vì auto-slice. Bộ mới khoảng **70,7 MiB** texture RGBA32 chưa tính overhead;
đây là phép tính 103×360×500×4, không phải số đo Profiler/GPU.

**Import Unity thực tế còn chờ license.** Không tự tạo GUID/.meta giả khi chưa có
Editor import theo luật repo. File mới dùng đường Resources, chưa có scene/prefab
reference cần GUID; Unity sẽ sinh .meta ở lần import đầu. Sau đó commit các .meta
Unity tạo ra trên chính nhánh này. Không thay hoặc di chuyển .meta cũ.

## Kết quả kiểm thử cloud

| Kiểm tra | Kết quả và giới hạn |
| --- | --- |
| `python3 Tools/validate_hero_png.py --source-zip …/hero.zip` | **PASS**: 103 ảnh khớp byte gốc, số/thứ tự khung, đường profile, 360×500 RGBA, PNG CRC, không có archer_run |
| `bash Tests~/HeroPng/run.sh` với Mono/mcs có sẵn trong Unity 6000.5.0f1 | **PASS 9 scenario** bridge boundary; không chạy controller/combat/equipment thực, JSON/Canvas/render/import native |
| Biên dịch riêng `HeroPngImportSettings.cs` bằng mcs, tham chiếu DLL UnityEngine.CoreModule, UnityEditor.CoreModule và netstandard của 6000.5.0f1 | **PASS** API/type check; không có nghĩa callback import đã chạy |
| `Tests~/run.sh --sprites` trước thay đổi | **BLOCKED**: 12 lỗi compile có sẵn; không sửa/làm yếu assertion để vượt qua |
| Unity 6000.5.0f1 batchmode/nographics mở project | **BLOCKED**, exit 198: `No valid Unity Editor license found`, thiếu entitlement `com.unity.editor.headless`; chưa import/compile project |
| 4 TestCase mới `HeroPngImportTests`, Unity Play Mode, MainGame và Player | **NOT RUN** vì license; không có screenshot/video gameplay thật |
| GUID/.meta checker chỉ đọc | **Không PASS**: xem giải thích bên dưới; không tự sửa GUID cũ |
| `git diff --check` | **PASS** |

Mono runner đầy đủ bị chặn bởi test `Patch54fRegression` gọi
`CombatBalance.HeroHealthMultiplier` không còn có, cùng thiếu API trong UnityStub:
TMP name/Top/TextWrappingModes/textWrappingMode, Mathf.Min(3 args)/Cos,
Vector2 division/Scale/up. Lần dùng mcs mặc định C# 7.0 còn bị `readonly struct`;
runner riêng dùng `-langversion:latest`. Không cài thêm dependency; không công bố
combat/Save/Thiên Quan regression PASS từ các kiểm tra boundary.

GUID checker baseline sạch commit cũ: **0 error, 62 warning** (PackageCache chưa
có, missing/orphan .meta cũ). Sau thay đổi: **1 error, 63 warning**. Error
`moved-without-meta` suy đoán folder mới HeroAnimation là folder OpeningBackground
bị đổi tên do `.meta` mồ côi `Assets/Resources/OpeningBackground.meta` có sẵn.
Đây là false positive của heuristic: git cho thấy chỉ thêm folder PNG mới, không
rename/xóa OpeningBackground và không sửa `.meta` nào. Không làm theo gợi ý chuyển
GUID OpeningBackground sang HeroAnimation. Kiểm tra lại sau import có license;
warning missing .meta mới vẫn chưa được giải quyết bằng Editor trong phiên này.

## Nghiệm thu còn cần trên Windows (không chạy local trong phiên Cloud)

1. Dùng Unity **6000.5.0f1** có license, import project, xác nhận không lỗi compile;
   commit .meta mới do Unity sinh. Chạy bốn `HeroPngImportTests`, kiểm tra profile
   bằng JsonUtility thật và thông số texture. Chạy lại GUID checker sau PackageCache.
2. MainGame: Kiếm, Cung, Trượng, Tay không ở cùng trạng thái phải dùng đúng cùng
   bộ Idle/Walk/Run/Hit. Chụp/quay Sword/Bow/Staff; hiện chưa có bằng chứng renderer.
3. Walk khi khám phá/formation, Run khi tiến tới mục tiêu, Idle khi thực sự đứng;
   pause/resume, bị chặn/đạt tầm đánh không giữ motion cũ. Kiểm tra feet/scale/flip,
   jitter/alpha mép ảnh, chuyển fallback Attack và HP ở cửa sổ 600/800/1150.
4. Attack giữ frame cũ đúng 0,16 giây, Hit không ghi đè Attack; Hit one-shot và
   Attack ngắt được. Projectile, cast/release/skill, damage, mana/cooldown, VFX,
   vị trí Hero và Thiên Quan không đổi. B2 vẫn có body/HP/pose độc lập.
5. Spawn/VFX, chết, hide/reuse, đổi vũ khí, Retry/Travel/Load không giữ pose cũ
   hoặc nhân đôi body. Save/Load chỉ nghiệm thu bằng fixture riêng theo luật dự án,
   không dùng save cá nhân. Kiểm tra native input và Windows Player, đo texture
   memory/chi phí load lần đầu bằng Profiler; chưa có kết quả cloud cho các mục này.
