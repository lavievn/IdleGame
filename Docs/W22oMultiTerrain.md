# W2.2o — Đồng Bằng, Rừng và Núi/Đồi

Ghi nhận lịch sử W2.2o. Thông số tốc độ/Sky mới và cơ chế nối mép hiện tại xem
[TerrainSeamHotfix.md](TerrainSeamHotfix.md); không còn overlap bù 1 đơn vị.

## Nguồn và mapping đủ 12 PNG

Nhánh `feature/w2-combat-presentation`, nền đầu lượt `1873dcb` (`update map j_m`).
Nguồn thực tế: `Assets/StreamingAssets/ArtSource/Plains/`. Tên thư mục nguồn
không quyết định địa hình. Không sửa/đổi tên PNG hoặc metadata nguồn.
Mỗi bản trong Resources khớp byte-for-byte với PNG nguồn tương ứng.

| Nhóm | Ground | Background1 (gần) | Background (xa) | Sky |
|---|---|---|---|---|
| Plains | Ground.png | Ground2.png | Ground3.png | sky.png |
| Forest | ground_j.png | ground_j1.png | **ground_j2.png.png** | sky_j.png |
| Mountain/Hills | ground_m.png | ground_m1.png | ground_m2.png | sky_m.png |

File có hai đuôi `.png` đã được báo trước khi xử lý; chỉ bản dùng runtime được đặt
lại tên `ground_j2.png` ở `Assets/Resources/Environment/Forest/`. Không gán ảnh theo
thứ tự Explorer. Các file nhóm m giữ nguyên tên trong Resources/Environment/Mountain.
Bốn bản Plains vẫn ở Resources/Environment/Plains.

Tất cả ảnh rộng 2172 pixel. Ground Plains cao 530, Sky Plains cao 274; 10 ảnh
còn lại cao 724. Giữ toàn bộ canvas và alpha, không crop/mirror/resize bitmap.

## Audit và chọn terrain

- `WorldMapCatalog.BaseTerrain()` dùng cột địa hình của catalog để tạo enum;
  `ApplyMap()` và `Normalize()` gán enum đó vào `HeroData.mapTerrain`.
- `CombatManager.SetupHeroInfo()` bind dữ liệu Hero chính vào CurrentHeroCombatState.
  Các đường DeployHero/New/Continue/Load/Retry/Reset/Travel đều sử dụng dữ liệu này.
- `GroundPresentation.RefreshTerrain()` theo dõi thay đổi reference dữ liệu hoặc
  enum terrain. Chỉ khi có thay đổi mới chọn/bind lại artwork. Không dựa vào tên map,
  quốc gia, ID, race hay Hero B2; không thêm event bus hoặc renderer mới.

| Enum hiện có | Nhãn catalog | Số map | Bộ ảnh |
|---|---|---:|---|
| DongBang | Đồng bằng (17), Thảo nguyên (1) | 18 | Plains |
| RungRam | Rừng rậm | 27 | Forest |
| DoiNui | Đồi núi (50), Đồi núi / Cao nguyên (1) | 51 | Mountain |
| CaoNguyen | Cao nguyên | 5 | Mountain |
| DamLay / SaMac / HoNuoc / Bien | Đầm lầy / Sa mạc / Hồ nước / Biển | 32 | Fallback cũ |

Tổng 133 map; không sửa catalog hoặc quy tắc phân loại cũ. Enum chưa nhận diện
cũng dùng fallback cũ. Không chuyển tất cả terrain sang một trong ba nhóm mới.

## Renderer, cache và vòng đời

Chỉ runtime `GroundPresentation.cs` thay đổi. Các phần EnvironmentManager,
Multi-Zone Camera, GameManager, SaveManager, combat, inventory, animation, audio,
opening, scene, Packages và ProjectSettings không sửa trong W2.2o.

Install tải cache 12 Sprite một lần mỗi GroundPresentation. Khi dữ liệu map đổi,
chọn cache sẵn; đủ cả bốn Sprite mới thay cả nhóm, thiếu bất kỳ layer nào thì
khôi phục trọn bộ fallback cũ, không giữ nhầm ảnh/Sky của nhóm trước. Các Image
được tái sử dụng. Bind cả bốn lớp và layout đồng bộ trong một lượt, không yield
hoặc xóa nền trước khi đổi. Ground.LateUpdate vẫn kiểm tra được lúc pause/VFX.
Chưa xác nhận hiện tượng chớp một frame bằng Unity native.

Back-to-front: Sky → Background → Background1 → Ground → actor/VFX theo cơ chế cũ.
Sky không nhận pan; hai lớp xa giữ 10%/30%, Ground giữ 100% và pha wrap cũ.
Giữ ba tile mỗi lớp, overlap 1 đơn vị chống khe hình học do làm tròn. Chiều rộng
Ground/Background1/Background/Sky lần lượt là W/1,3W/1,69W/2,5W (cộng 1 cho overlap),
chiều cao tính từ tỷ lệ PNG đầy đủ. Không thay tốc độ, deadzone, SmoothDamp hay
vị trí/scale logic của nhân vật.

Sky giữ scale mặc định 250%, neo theo mép trên viewport gameplay compact, không
neo theo chiều cao 100 của sàn; vẫn dùng skyScale/skyOffset đã có. Sky Forest và
Mountain cao hơn Plains vì tỷ lệ PNG khác nhau; không ép về cùng chiều cao.
Các prop Grass thử nghiệm chỉ hiện trên fallback, không chồng lên artwork mới.

Cache có chủ đích giữ cả ba bộ để không load khi chuyển map. Không tạo Sprite/
Texture/GameObject trong Scroll/Update. Không Destroy Sprite do Resources sở hữu;
texture được Unity quản lý theo vòng đời tài nguyên. Không tuyên bố không có leak
native chỉ từ kiểm thử mô phỏng. Nếu cả 12 texture dùng RGBA32 không mipmap, ngân
sách pixel khoảng 66,6 MiB, chưa tính renderer/driver và bộ preview fallback.
Profiler Unity cần xác minh bộ nhớ thật và unload qua scene. Các PNG nguồn trong
StreamingAssets cũng vẫn nằm trong bản build dạng file thô, ngoài bản texture.

## Ground hạ 20px

Xem [W22oGroundDrop.md](W22oGroundDrop.md). Chỉ ba tile Ground của ba nhóm artwork
bị hạ; hai lớp background, Sky, prop fallback, sàn logic, Hero/Enemy và chân logic
không dịch. `groundDropPixels` chung mặc định 20 pixel màn hình Unity Screen,
đổi sang đơn vị BattleArea bằng tỷ lệ canvas gameplay hiện tại. Không dùng trực
tiếp 20 world unit. Ở 600/800/1150 pixel, với W=1920: 64 / 48 / 33,3913 đơn vị.
Khi mở inventory dùng chiều rộng gameplay compact; resize tính lại offset.
Thông số Inspector hiện trên GroundPresentation được cài lúc chạy; thay đổi trong
Play Mode không tự lưu scene. Biên alpha/đường mặt cỏ khác nhau giữa PNG, cần xem
thực tế vị trí bề mặt dưới chân Hero, không tự crop padding để ép ảnh trùng nhau.

## Alpha và giới hạn artwork

Metadata 12 bản runtime: Single Sprite FullRect, full source canvas, mipmap tắt,
max 4096, NPOT không resize, không compression, filter Bilinear, Clamp, không
physics shape, không readable CPU. Giữ GUID Plains cũ; GUID mới không trùng nhau.
Source StreamingAssets giữ metadata đã có, không import nó thay cho Resources.

W2.2o bật `alphaIsTransparency=1` ở runtime, kể cả bốn ảnh Plains (trước đó 0).
Unity có thể dàn RGB sang texel trong suốt khi import để giảm màu rác ở biên khi
bilinear lấy mẫu. PNG nguồn và bản sao vẫn nguyên byte, alpha không bị cắt/xóa;
RGB alpha=0 không có đóng góp trực tiếp qua shader UI alpha chuẩn. Không thêm
shader hoặc sửa pixel bán trong suốt để che vấn đề artwork. Chưa kiểm tra native
sampling, atlas/import hoặc halo trong Windows Player; không cam kết hết mọi viền
màu chỉ từ metadata. Các artifact có alpha > 0 phải đánh giá/sửa ở nguồn riêng.

Hai mép không seamless. So sánh cột trái/phải sau premultiply alpha (bỏ ảnh hưởng
RGB không nhìn thấy ở alpha=0) vẫn cho sai khác trung bình RGB/A, thang 0–255:

| Ảnh | Sai khác RGB / alpha |
|---|---|
| Ground.png | 68,97 / 74,71 / 69,73 / 79,00 |
| Ground2.png | 35,99 / 33,62 / 34,43 / 25,16 |
| Ground3.png | 74,34 / 78,03 / 83,74 / 87,19 |
| sky.png | 56,50 / 26,70 / 3,86 / 0 |
| ground_j.png | 12,34 / 14,21 / 8,40 / 11,53 |
| ground_j1.png | 56,56 / 52,57 / 35,88 / 38,06 |
| ground_j2.png.png | 34,00 / 28,40 / 18,07 / 4,56 |
| sky_j.png | 25,78 / 13,01 / 3,40 / 0 |
| ground_m.png | 13,52 / 11,49 / 10,10 / 4,23 |
| ground_m1.png | 42,38 / 35,80 / 27,51 / 7,42 |
| ground_m2.png | 49,03 / 44,01 / 31,54 / 13,42 |
| sky_m.png | 38,11 / 23,15 / 7,27 / 0 |

Tile phủ đủ khung không có nghĩa họa tiết/alpha nối liền. Không sửa, blend hoặc
mirror artwork để che seam. Các mảng màu/alpha bất thường của Plains đã ghi nhận
ở W2.2n cũng được giữ nguyên. Ảnh mới có một số viền màu ở rìa silhouette khi xem
nguồn; cần xác minh mức còn lại sau import bằng Unity trước khi kết luận.

## Kiểm thử

- `Tests~/run.sh --plains`: 11 PASS (8 ca cũ giữ nguyên, thêm 3 ca Ground drop).
- `Tests~/run.sh --terrain-art`: 38 PASS, bao gồm 8 enum; 133 node catalog;
  swap nhiều lượt; 3 nhóm × 3 kích thước; thiếu từng layer của 3 bộ (12 ca);
  cache/đổi snapshot; New, Continue, Load, Retry, Reset, Travel (6 ca).
- Các nhóm multi-zone, spawn, map-entry, position-hotfix, ranged-lanes, formation,
  sprites, magic-persistence, audio-volumes, desktop-ui, opening-font,
  movement-spike, render-trace và distance-waves đều PASS riêng.
- Toàn bộ runner: 687 PASS, rồi dừng ở MissingMemberException cũ
  `GroundPresentation.fixedRect` tại Tests~/TerrainRegression.cs:41. Các ca phía
  sau chưa chạy. Không sửa/bỏ assertion cũ để báo toàn bộ suite PASS.
- Compile Mono UNITY_EDITOR: 16 cảnh báo cũ; UNITY_STANDALONE_WIN: 3 cảnh báo cũ.
  Đây không phải Unity Editor compile hoặc Windows Player build.
- Thêm 8 ca import native trong `TerrainPngImportTests`, dùng TestCase/Assert.That
  cơ bản; giữ 4 ca PlainsPngImportTests. Không dùng Assert.Multiple,
  NonParallelizable, UnityEditor, hook hoặc manager/save I/O trong các ca import.
- Cloud chưa chạy/biên dịch các ca native bằng Unity thật. Mono dùng Sprite giả
  đúng kích thước, kiểm tra tham chiếu/layout/toán học, không chứng minh màu/alpha,
  sampling, GPU upload, memory leak native hoặc chuyển cảnh render thực tế.

## Checklist nghiệm thu Unity / Windows Player

1. Import project, chạy 12 ca PNG import; kiểm tra TextureImporter alpha, full rect,
   độ phân giải, Console. Không có Sprite missing hoặc compiler error mới.
2. Đi qua Plains ↔ Forest ↔ Mountain/Highland và một map fallback. Kiểm tra đủ bốn
   ảnh, Sky đúng nhóm, thứ tự vẽ và không chớp nền/prop cũ khi đổi map.
3. Resize 600/800/1150, mở/đóng inventory: chỉ Ground hạ 20 pixel; Sky bám trên,
   tỷ lệ ảnh nguyên vẹn; chân nhân vật không đổi; mặt cỏ hợp lý dưới chân.
4. New Game, Continue, Load hai ô, Travel, Retry, Reset: VFX, B2, camera Multi-Zone,
   Attack/Hit và projectile không hồi quy. Dùng save thử đã sao lưu, không xóa save
   thật để kiểm tra artwork.
5. Cuộn camera lâu, xem seam và halo trên nền tương phản. Profiler kiểm tra tải
   texture chỉ lúc cài cache, bộ nhớ ổn định qua nhiều map/scene; build Windows
   thường và xem lại các bước 2–4. Ghi riêng lỗi artwork với lỗi renderer.
