# Thử nghiệm ngày–đêm và thời tiết

Bản Player hiện tại và các thay đổi sky, BGM, mưa/giông nằm trong [báo cáo Phase 2](WEATHER_PHASE2_STORM_AUDIO_REPORT.md). Số liệu dưới đây thuộc lượt đo Phase 1.

[PR #16](https://github.com/lavievn/IdleGame/pull/16) · `feature/dynamic-sky-weather-cycle` · baseline `f695df96547c57296562cbd6f1b061d359933814`. Worktree thử nghiệm ở `D:/IdleGame/Projects/DynamicSkyWeather`.

## Tích hợp asset trên Windows

Worktree `D:/IdleGame/Projects/DynamicSkyWeather`, nhánh `feature/dynamic-sky-weather-cycle`. HEAD đầu đợt tích hợp là `b282d55390a993df4c3c4b535bd3fe18b4d7e210`.

| Asset | Kết quả import và cấu hình |
| --- | --- |
| BG Studio Free HDR Skyboxes Pack 1.0 | Import texture và material từ cache `D:/IdleGame/Caches/Asset Store-5.x`. Unity 6000.5.0f1 nhận cả năm cubemap BC6H ở 2048×2048 mỗi mặt và material `Skybox/Cubemap`. Bình minh dùng Sky-1, ban ngày dùng Sky-2, nhiều mây dùng Sky-3, hoàng hôn dùng Sky-4. Pha đêm dùng Sky-3 với độ phơi sáng thấp và sắc xanh vì bộ ảnh thiếu panorama đêm riêng. Sky-5 được kiểm tra khi import. |
| Digital Ruby Rain Maker 2.1.2 | Import prefab 2D và các phụ thuộc của prefab. Dùng emitter `RainPrefab2D/RainFallParticleSystem` và texture hạt của gói. Shader gốc `Legacy Shaders/Particles/Alpha Blended` và controller gắn `Camera.main` được bỏ qua trong lúc chạy. Mesh UI phủ viewport dùng shader UI của Unity, không nhận ánh sáng Renderer2D, không nhận raycast và nằm dưới Canvas HUD. |

WorldClimateAssetAdapter chiếu cubemap HDR bằng shader của project sang RenderTexture tối đa 640×480, tái sử dụng giữa các frame và cập nhật tối đa 18 lần/giây khi pha hoặc thời tiết thay đổi. Texture được ghép ở đúng lớp sky cũ trong Battle Canvas. Núi, cây, nền đất và camera tracking dùng luồng cũ. Tắt HDR hoặc thiếu tài nguyên sẽ khôi phục PNG gốc. Adapter đặt emitter theo kích thước viewport, nội suy mật độ từ WorldClimate, giới hạn tốc độ sinh ở 360 hạt/giây và tổng hạt ở 420. Khi kết thúc nội suy về khô, emitter dừng, xóa hạt và ẩn lớp rain.

### Mở lại trên máy có quyền dùng asset

1. Mở worktree bằng Unity **6000.5.0f1**. Từ **Window → Package Manager → My Assets**, chọn **Free HDR Skyboxes Pack 1.0** và **Rain Maker 2.1.2**.
2. Chọn import các thư mục texture, material và ReadMe của gói BG Studio, cùng `RainMaker/Prefab` và Readme của gói Digital Ruby. Giữ metadata gốc của các file được chọn.
3. Chạy **Bach Luyen → Environment → Link local weather assets**. Unity tạo `Assets/Resources/WorldClimateVendor/Assets.asset` từ tài nguyên đã import. Mở `Assets/Scenes/MainGame.unity`; bật **World Climate Lighting**, **World Climate Hdr Sky**, **World Climate Rain 2D** trong GameManager. Có thể thử nhanh từng pha bằng **Preview World Climate** trong Editor.

Asset nhà cung cấp, metadata đi kèm và cấu hình liên kết nằm trong các thư mục được Git ignore. Repo có adapter, shader projection và lệnh liên kết do project viết. [BG Studio](https://assetstore.unity.com/packages/2d/textures-materials/sky/free-hdr-skyboxes-pack-175525) · [Digital Ruby](https://assetstore.unity.com/packages/vfx/particles/environment/rain-maker-2d-and-3d-rain-particle-system-for-unity-34938)

### Kiểm thử asset thật

| Môi trường | Kết quả |
| --- | --- |
| Unity 6000.5.0f1, import và MainGame Play Mode | Hai lượt import và linker exit 0. Kiểm tra GUID 1.067 file, 0 lỗi và 0 cảnh báo. MainGame **7/7 nhóm** sau khi khai báo CanvasRenderer cho lớp mưa. Ở 800×450 có 50 vệt mưa nhẹ, 159 vệt mưa lớn. Khô dừng và xóa hết hạt. Tắt HDR phục hồi PNG. |
| Windows Development build | Mono2x, D3D11, **0 lỗi, 57 cảnh báo**, 14,8 giây. Build tại đường dẫn cũ, bằng `2e88beb4ae872f56c855ba1522184917da6a5916`. Bản Acceptance sau tích hợp asset **NOT RUN**. |
| Windows Player, Direct3D 11 trên NVIDIA RTX 2080 | **7/7 nhóm hình ảnh**. Chụp đủ các giờ 06:30, 12:00, 18:30 và 22:00. Luma đo trong RT tương ứng 0,243 / 0,527 / 0,506 / 0,262. Ở 800×450 có 52 vệt mưa nhẹ, 171 vệt mưa lớn và 326 hạt native ở mẫu mưa lớn. Kiểm tra pan, độ phủ và clipping ở 600×338, 800×450, 1150×647. Ảnh chụp hiển thị vệt xanh nhạt và chữ HUD rõ ở cả ba kích thước. |
| Windows Player, 30 phút với asset thật | **PASS phần chạy**. 1.805,0 giây hoạt động / 1.822,3 giây thực, 109.060 frame, trung bình 59,85 FPS với giới hạn 60, frame dài nhất 1.297,3 ms. Đỉnh 353 hạt, 48.364 frame có hạt và 60.696 frame không hạt. Unity cấp phát 324,4–404,5 MiB, Mono dùng 8,2–8,7 MiB trong các mẫu. |
| Windows Player khi thoát | **FAIL khi thoát sau khi ghi kết quả**. Exit `-1073741819`, Event Viewer ghi `UnityPlayer.dll 0xc0000005`, offset `0x1dd029`. Đây là cùng chữ ký đã thấy trước khi import hai gói. |
| Hồi quy MainGame của climate và Save | **PASS 7/7 nhóm**. Đồng hồ, biome, Save cũ/tương lai, Save/Load, Travel, Retry và fallback qua adapter tổng hợp. Fixture map ướt chạy lại **7/7 nhóm**. [Log hồi quy](<D:/IdleGame/Logs/Unity-MainGameWorldClimate-20261111-193716.log>) · [log fixture](<D:/IdleGame/Logs/Unity-MainGameWorldClimateVendor-20261111-193644.log>). |
| Chuột/phím native trong lượt tích hợp | **NOT RUN**. Fixture dùng handler và cấu hình preview trong bảy kiểm tra hình ảnh, sau đó tắt preview để chạy theo đồng hồ thật. Các hồ sơ kiểm thử tách riêng. |

Mẫu từ bản Development ở Lạc Vân Trạch, 800×450, seed 3. Fixture dọn wave để giữ Hero khám phá. Số liệu lấy từ Unity Profiler, không bao gồm phép đo toàn bộ bộ nhớ tiến trình hoặc VRAM. Mức cấp phát Unity tăng qua hai lần đổi ngày trong khi Mono quanh 8–9 MiB.

| Giây hoạt động / thực | Ngày | Thời tiết | FPS trung bình tích lũy | Hạt hiện tại / đỉnh | Unity cấp phát (MiB) | Mono đang dùng (MiB) |
| --- | --- | --- | --- | --- | --- | --- |
| 300 / 300 | 0 | HeavyRain | 59.6 | 333 / 353 | 324.6 | 8.3 |
| 600 / 600 | 0 | Fog | 59.8 | 0 / 353 | 324.4 | 8.7 |
| 900 / 900 | 1 | LightRain | 59.8 | 118 / 353 | 365.4 | 8.2 |
| 1200 / 1217 | 1 | LightRain | 59.8 | 116 / 353 | 365.6 | 8.4 |
| 1500 / 1517 | 2 | Fog | 59.8 | 0 / 353 | 404.5 | 8.7 |
| 1800 / 1817 | 2 | Fog | 59.8 | 0 / 353 | 404.4 | 8.4 |

Các ảnh bằng chứng chứa sky và texture Rain Maker nên được lưu cục bộ ở ổ D. Bấm mở trên máy đã chạy thử.

- [Bình minh](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-hdr-06.5.png>) · [ban ngày](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-hdr-12.0.png>) · [hoàng hôn](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-hdr-18.5.png>) · [ban đêm](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-hdr-22.0.png>).
- [Mưa nhẹ](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-light-rain-800.png>) · [mưa lớn 600×338](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-heavy-rain-600.png>) · [mưa lớn 800×450](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-heavy-rain-800.png>) · [mưa lớn 1150×647](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-heavy-rain-1150.png>).
- [Import HDR](<D:/IdleGame/Logs/Unity-WorldClimate-Imported-Sky.log>) · [import Rain Maker](<D:/IdleGame/Logs/Unity-WorldClimate-Imported-Rain.log>) · [liên kết asset](<D:/IdleGame/Logs/Unity-WorldClimate-VendorSetup.log>) · [Play Mode](<D:/IdleGame/Logs/Unity-MainGameWorldClimateVendor-20261111-190417.log>) · [Windows build](<D:/IdleGame/Logs/Unity-WorldClimate-Phase1-Windows-build.log>) · [Windows Player](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/player.log>).
- [Ảnh sau 30 phút](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/vendor-soak-30min.png>) · [kết quả Windows JSON](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-cc39f492a4864f98b1a0bfcad212755a/evidence/player-result.json>).
- [Mở Windows Player bằng profile thử riêng](<D:/IdleGame/Builds/DynamicSkyWeather/build/Play-Dynamic-Sky-Weather-Isolated.cmd>) · [EXE](<D:/IdleGame/Builds/DynamicSkyWeather/build/IdleGameV10.exe>).

Chạy lại bằng `Tools/Local-IdleGame.ps1 -Mode MainGameWorldClimateVendor` trong worktree đã liên kết asset. Sau khi build, dùng `-Mode Player -PlayerMode WorldClimateVendorSoak -WindowsOutputRoot D:/IdleGame/Builds/DynamicSkyWeather`. Để cửa sổ Player có focus và giữ game ở foreground trong 30 phút vì đồng hồ tự dừng khi mất focus.

### Sương mù

Weather FX Free 1.0.6 có shader sương 2D và được nhà phát hành liệt kê hỗ trợ URP trên Unity 6000.3.25. Có thể thử riêng shader sương ở lớp cảnh xa và dùng cường độ từ WorldClimate. Tích hợp và tương thích Renderer2D/Overlay 6000.5.0f1 **NOT RUN**. [Unity Asset Store](https://assetstore.unity.com/packages/vfx/shaders/weather-fx-free-rain-snow-sampler-412712) · [Mô tả của tác giả](https://heyheythere.itch.io/weather-fx-free).

### Commit tích hợp

- `ee52b54943c12167c86a8d895bf7471e73248d70` thêm adapter HDR và Rain Maker cùng quy trình liên kết asset cục bộ.
- `753f5027f5eb146d5edccd68ba3dbc4db644ea6c` thêm kiểm thử MainGame và Windows với asset thật.
- `2e88beb4ae872f56c855ba1522184917da6a5916` khai báo CanvasRenderer cho UI mưa.
- Commit báo cáo có trong [lịch sử PR #16](https://github.com/lavievn/IdleGame/pull/16/commits). Git status vẫn gồm bảy file thiết lập Unity đã thay đổi và 15 file .meta chưa theo dõi từ trước đợt tích hợp. Các thay đổi đó được giữ ngoài commit.

## Đồng hồ và thời tiết


- Một ngày dài **780 giây hoạt động**, mỗi giờ trong game dài 32,5 giây. Game mới bắt đầu ngày 0 lúc 05:00. Các pha là bình minh 05:00–08:00, ban ngày 08:00–17:00, hoàng hôn 17:00–20:00 và ban đêm 20:00–05:00. Màu chuyển mượt trên các lớp nền. HUD, chữ và sprite nhân vật giữ màu hiển thị riêng.
- Đồng hồ dùng delta không scale khi gameplay đang chạy. Pause, màn hình tiêu đề, entry, màn hình thua, mất focus và Thiên Quan giữ nguyên thời gian. Editor batch được tính như phiên đang hoạt động để chạy fixture. Load tiếp tục từ snapshot, thời gian đóng game không được cộng.
- Một lượt thời tiết kéo dài 4 giờ trong game, tương đương 130 giây theo cấu hình mặc định. Seed, ngày, map và lượt xác định thời tiết. Trọng số khác nhau cho đồng bằng, rừng, cao nguyên/đồi núi, đầm lầy, sa mạc, vùng nước, vùng tuyết và thảo nguyên. Trời quang, nhiều mây, sương, mưa nhẹ và mưa lớn có các mức riêng, nội suy trong 8 giây. Các map trong danh sách indoor và Thiên Quan không dùng mưa. Catalog hiện tại chưa có cờ indoor, nên danh sách có thể cấu hình riêng trong Inspector.
- Save có một thành phần tùy chọn `worldClimate` v1, gồm seed, ngày và phần ngày. Trạng thái thời tiết và mức blend được tính lại từ các giá trị đó cùng map đang lưu. Save cũ dùng ngày 0 lúc 05:00 và seed tương thích cố định 780. Save có phiên bản khí hậu tương lai được từ chối trước khi áp vào nhân vật hoặc ghi đè qua luồng lưu hiện có.

## Kiểm thử bản PNG fallback trước khi import asset


| Môi trường | Kết quả |
| --- | --- |
| Unity 6000.5.0f1 compile và kiểm tra GUID | **PASS**. Unity thoát 0. Kiểm tra 985 tệp, 0 lỗi và 0 cảnh báo GUID. |
| MainGame Play Mode, Direct3D 11 trên RTX 2080 | **PASS 7/7 nhóm**. Mốc 0/13/26 phút mô phỏng, pause và thời gian unscaled thực, pha và blend, seed theo biome, map khô, Save cũ/tương lai, Travel/Retry, fallback và cỡ cửa sổ 600×338, 800×450, 1150×647. Phép thử adapter dùng texture tổng hợp và kiểm tra viewport/sorting, không dùng asset thương mại. |
| Windows Development build, Mono2x | **PASS**. 0 lỗi, 57 cảnh báo, 54,7 giây. Runtime xác nhận UniversalRenderPipelineAsset / Renderer2D và Battle Canvas Overlay trên cả WARP lẫn RTX 2080. |
| Windows Player, WARP và RTX 2080 | **7/7 assertion cho mỗi lượt**. Hai lượt đều ghi ảnh, JSON và không báo lỗi kích thước cửa sổ. Replay từ process mới **1/1** xác nhận checkpoint 07:59. Hồi quy Development 0.1.7 **5/5** xác nhận Combat, loot, Võ học, Forge, Save/Retry và Thiên Quan. |
| Windows Player teardown | **FAIL sau khi ghi kết quả** ở cả bốn lượt. Event Viewer ghi `UnityPlayer.dll`, `0xc0000005`, offset `0x1dd029`, trùng chữ ký từng ghi ở [PR #13](https://github.com/lavievn/IdleGame/pull/13). |
| MainGame, soak khám phá bằng fallback | **PASS**. 1.805,0 giây thực, đồng hồ tiến 1.805,0 giây qua hai lần đổi ngày, 54.119 frame. Trung bình 29,98 FPS với giới hạn 30 FPS, frame dài nhất 400,6 ms. Các mẫu managed heap 5 phút một lần nằm trong 764,0–809,5 MiB. Fixture dùng MainGame ở 800×450 với đợt quái được dọn để Hero tiếp tục khám phá. |
| MainGame Camera V03 | **PASS 6/6 nhóm**. 600×338, 800×450, 1150×647 và 1360×680 giữ Hero quanh x=0,65 khi đi, sai số viewport lớn nhất 0,00088. Khi ngừng đánh, Hero về x=0,65. Có log nhận diện và giao tranh. |
| Thao tác chuột/phím native | **NOT RUN**. Các thao tác Save, Travel và test gameplay gọi handler trong fixture. |

### Ảnh Windows Player dùng sky PNG fallback

![Cùng map lúc 12 giờ](evidence/dynamic-sky-weather/legacy-day.png)

![Cùng map lúc 22 giờ](evidence/dynamic-sky-weather/legacy-night.png)

- Log [Unity compile](<D:/IdleGame/Logs/Unity-Check-20261111-144023.log>), [MainGame](<D:/IdleGame/Logs/Unity-MainGameWorldClimate-20261111-144641.log>), [build](<D:/IdleGame/Logs/WorldClimate-Baseline-build.log>) và [soak](<D:/IdleGame/Logs/Unity-MainGameWorldClimateSoak-20261111-145435.log>).
- [Số liệu soak dạng JSON](<D:/IdleGame/Projects/DynamicSkyWeather/.utmp/foundation-c3c0a3ff6b784090ba25d593a3dd7989/evidence/result.json>) và [ảnh sau 30 phút](<D:/IdleGame/Projects/DynamicSkyWeather/.utmp/foundation-c3c0a3ff6b784090ba25d593a3dd7989/evidence/climate-soak-30min.png>).
- Kết quả [MainGame](<D:/IdleGame/Projects/DynamicSkyWeather/.utmp/foundation-e9f464495a1841dcb8916a84a466e668/evidence/result.json>), [WARP](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-71ea06e919f54c6ba11b660e6e290c9b/evidence/player-result.json>), [RTX 2080](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-71f8bf70aeb941b9987a0a24fe8c5212/evidence/player-result.json>), [replay](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-9819ecc8d12d419094990be18ff3b6e5/evidence/player-result.json>) và [hồi quy 0.1.7](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-bea1a5272c2048abae864ea2d8dfe18e/evidence/player-result.json>).
- [Log camera](<D:/IdleGame/Logs/Unity-DynamicSkyWeather-Camera.log>) và [CSV camera/awareness](<D:/IdleGame/Projects/DynamicSkyWeather/.utmp/foundation-0559923be1e24d328962018073f60283/evidence/camera-awareness.csv>).
- Ảnh bổ sung từ Player RTX 2080 [bình minh](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-71f8bf70aeb941b9987a0a24fe8c5212/evidence/climate-legacy-06.png>), [hoàng hôn](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-71f8bf70aeb941b9987a0a24fe8c5212/evidence/climate-legacy-19.png>) và [fallback 1150×647](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-71f8bf70aeb941b9987a0a24fe8c5212/evidence/climate-fallback-1150.png>).

Chạy lại từ worktree bằng `Tools/Local-IdleGame.ps1` với `-Mode MainGameWorldClimate`, `-Mode MainGameWorldClimateSoak`, hoặc `-Mode Player -PlayerMode WorldClimate -WindowsOutputRoot D:/IdleGame/Builds/DynamicSkyWeather`. Các lệnh thử dùng profile có marker trên ổ D.

Fixture chạy lại ở Lạc Vân Trạch được cố định trong commit e66b18e. Lượt Windows 30 phút cũng dùng Lạc Vân Trạch.
