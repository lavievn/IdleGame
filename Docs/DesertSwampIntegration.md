# Tích hợp Sa mạc và Đầm lầy

Nền `caa4372` trên `feature/w2-combat-presentation` đã có đủ tám PNG nguồn trong
`Assets/StreamingAssets/ArtSource/Plains/`. Tên thư mục nguồn không quyết định terrain.
Không sửa, đổi tên hoặc ghi lại ảnh nguồn; tám PNG runtime khớp byte-for-byte với
nguồn. Giữ nguyên toàn bộ canvas 2172×724, RGB của Sky và RGBA của các lớp Ground.

## Mapping runtime

| Bộ Resources/Environment | Ground | Ground2 / Background2 gần | Ground3 / Background3 xa | Sky |
|---|---|---|---|---|
| Desert | ground_d.png | ground_d2.png | ground_d3.png | sky_d.png |
| Swamp | ground_s.png | **Ground_s2.png** | ground_s3.png | sky_s.png |

Chữ hoa của `Ground_s2` được giữ cả trong file lẫn đường dẫn Resources.Load.
Hậu tố 2/3 và nội dung hình xác nhận thứ tự cảnh gần/xa; không suy từ thứ tự file.
`GroundPresentation.Install()` cache thêm hai bộ, `RefreshTerrain()` chọn Desert
cho `TerrainType.SaMac`, Swamp cho `TerrainType.DamLay`. Không đổi enum gameplay.
Đủ bốn Sprite mới thay cả bộ; thiếu một lớp thì dùng trọn bộ fallback cũ.
Sky của bộ mới cũng khởi tạo được renderer khi các bộ cũ không có asset.

Không có renderer, preset hoặc chính sách cuộn riêng. Giữ ba tile mỗi lớp và
thứ tự Sky → Ground3 → Ground2 → Ground → actor. Dùng nguyên cơ chế đo mép render,
recycle chính xác, Ground hạ 20 pixel màn hình, tốc độ 100% / 9% / 3%, Sky đứng
yên và scale mặc định 3.0. Không đổi camera, vị trí sàn/actor, Save/Load hay combat.
Đồng bằng, Rừng, Núi/Đồi và Cao nguyên vẫn dùng bộ ảnh cũ; Hồ nước, Biển và enum
chưa biết tiếp tục fallback cũ. Không sửa PNG hoặc metadata của ba bộ cũ.

## Binding các map thật

`WorldMapCatalog.BaseTerrain()` đã nhận đúng hai nhãn; `ApplyMap()`/`Normalize()`
gán `HeroData.mapTerrain`, và `CombatManager.SetupHeroInfo()` bind dữ liệu Hero
chính mà renderer đọc. Không cần sửa catalog, EnvironmentManager hoặc loader khác.

- Sa mạc (12): XD-03, XD-05, XD-09, XD-11, XD-13, XD-15, HT-08, HT-14,
  HT-16, HT-18, HT-21, HT-26.
- Đầm lầy (4): TV-20, TM-06, TM-16, TM-18.
- XD-11 và TV-20 đã là điểm khởi đầu; không thêm map hoặc đổi quy tắc di chuyển.

## Import và kiểm chứng

Metadata mới theo bộ Mountain đang dùng: Sprite Single FullRect, không trim,
không resize NPOT, max 4096, không nén/mipmap, Bilinear, Clamp, alpha transparency,
không CPU-readable hoặc physics shape. Mười GUID mới (8 ảnh + 2 thư mục) hợp lệ,
không trùng các metadata khác. Không thay GUID nguồn.

Kiểm thử Mono/UnityStub thực tế sau thay đổi:

| Nhóm | PASS | FAIL |
|---|---:|---:|
| `--terrain-art` | 54 | 0 |
| `--terrain-seams` | 55 | 0 |
| `--plains` | 11 | 0 |
| `--multi-zone` | 39 | 0 |
| `--map-entry` | 20 | 0 |
| Tổng | 179 | 0 |

Compile thành công với 16 cảnh báo cũ, khớp baseline trước thay đổi. Kiểm tra đủ
133 node qua Normalize, đúng 12/4 map mới, sprite/order, swap, thiếu từng lớp,
cache, resize 600/800/1150, parallax, mép tile và scale ngang. So sánh byte PNG,
kích thước, cấu hình import và GUID được kiểm tra riêng bằng Python.
Thêm 8 TestCase vào `TerrainPngImportTests`, không đổi assertion/API NUnit.
Các ca Unity native chưa biên dịch hoặc chạy trong Cloud; không chạy toàn bộ
suite Mono (còn lỗi cũ `TerrainRegression.cs:41`, `GroundPresentation.fixedRect`).

## Giới hạn và nghiệm thu còn lại

- `sky_s.png` nguồn có vùng gần trắng, opaque ở khoảng 210 dòng đầu (RGB 253–255
  ở các dòng đã lấy mẫu); đây là pixel của nguồn, không phải mất alpha khi copy.
  Giữ nguyên theo yêu cầu, cần xem mức độ hiển thị trong Unity.
- Hai mép ảnh mới chưa seamless: sai khác alpha trung bình của Ground2 Desert/
  Swamp lần lượt khoảng 11.62/14.44 trên thang 0–255. Mép hình học khớp không
  bảo đảm màu/họa tiết nối liền. Không blend/crop hoặc tự sửa artwork lần này.
- Cache theo cấu trúc hiện tại thêm khoảng 48 MiB nếu cả tám texture là RGBA32;
  tổng năm bộ khoảng 114.6 MiB, chưa tính driver/fallback. Nguồn trong StreamingAssets
  vẫn kèm build như trước. Chưa đo bộ nhớ bằng Unity Profiler.

Trong Unity/Windows: chạy 20 ca import (4 Plains + 16 Terrain); xem XD-11 và TV-20
rồi chuyển qua ba bộ cũ; kiểm tra đủ bốn lớp/sky, resize 600/800/1150, cuộn hai
chiều nhiều vòng, vùng trắng và seam nguồn. Xem Console, texture memory, opening,
VFX và B2. Kiểm thử Mono không chứng minh rasterization, alpha sampling, màu hoặc
render chuyển cảnh thật. Không tuyên bố Unity/Windows Player PASS.
