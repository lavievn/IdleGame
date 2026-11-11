# Weather Phase 2 — Mưa, giông và âm thanh môi trường

[Draft PR #16](https://github.com/lavievn/IdleGame/pull/16) · `feature/dynamic-sky-weather-cycle` · checkpoint Phase 1 `2c2ce40`. Worktree `D:/IdleGame/Projects/DynamicSkyWeather`. Kết quả đo Phase 1 có trong [DYNAMIC_SKY_WEATHER_REPORT.md](DYNAMIC_SKY_WEATHER_REPORT.md).

## Chỉnh gió và cỡ hạt mưa

- Trong **GameManager → Rain, wind and storm visuals**, **Rain Wind** bật độ nghiêng. **Random Rain Wind** chọn độ lớn và hướng trái/phải theo seed của map mỗi 14 giây trong game, nội suy trong 4 giây. **Wind Direction** chỉnh hướng ưu tiên khi bật ngẫu nhiên và hướng cố định khi tắt. **Wind Strength** mặc định 5, đặt 0 để mưa rơi thẳng. **Gust Strength** tiếp tục chỉnh các đợt gió trong mưa lớn.
- **Drop Size** mặc định 0,30. Chiều dài và chiều rộng vệt mưa đều còn 30% giá trị trước chỉnh. Splash và độ sâu tiếp đất dùng cùng luồng Phase 2.
- Unity Play Mode và Windows Development Player qua smoke ngắn. Vận tốc ngang trung bình trên hạt thật ở hai mẫu mưa nhẹ/mưa lớn là `-2.42` / `3.48` đơn vị mô phỏng mỗi giây. Kiểm tra riêng xác nhận seed cho cả hai hướng, chuyển mượt, chế độ hướng cố định và gió bằng 0. Windows ghi đủ 14/14 kết quả rồi tái hiện lỗi thoát UnityPlayer.dll 0xc0000005.
- Runtime `41c78e9`. [Unity Play Mode](<D:/IdleGame/Logs/Unity-MainGameWorldClimateVendor-20261111-200640.log>) · [Windows log](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-9b48c98d3a1b4d98a6be4806e94f945b/player.log>) · [ảnh mưa nhẹ](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-9b48c98d3a1b4d98a6be4806e94f945b/evidence/phase2-light-splash.png>) · [ảnh mưa lớn](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-9b48c98d3a1b4d98a6be4806e94f945b/evidence/phase2-heavy-splash.png>).

## Asset đã xem

| Asset | Kết quả và lựa chọn |
| --- | --- |
| Rain Maker 2.1.2 | Có trong hai cache Unity và đã import từ ổ D. `RainPrefab2D` gồm RainFall, RainMist, RainExplosion, WindZone và clip mưa/gió. Controller gốc raycast Physics2D cho từng hạt. Dùng RainFall và texture thật trong Canvas, bắt giao điểm với mặt Ground và vẽ splash/ripple bằng mesh UI cố định. Cách này dùng được với BattleArea đang hiển thị bằng Screen Space Overlay và tránh tạo collider cho hạt. |
| Rain Particles | Không thấy gói tải về trong cache ổ D và cache Unity mặc định. **NOT RUN** với prefab. |
| Weather FX Free | Không thấy gói tải về trong cache. Trang nhà phát hành có shader sương 2D cho URP; đánh giá tài liệu ở Phase 1. Phase 2 dùng haze cảnh xa của project. [Unity Asset Store](https://assetstore.unity.com/packages/vfx/shaders/weather-fx-free-rain-snow-sampler-412712). |
| Free Quick Effects Vol. 1 | Không thấy gói tải về trong cache. **NOT RUN** với prefab. |
| Cinematic Weather VFX Bundle | Không thấy gói tải về trong cache. **NOT RUN** với prefab. |

## Thiết lập và cách nghiệm thu

- **Sky** giảm tỷ lệ hiển thị mục tiêu còn 50% bằng góc nhìn rộng hơn của cubemap. Sky PNG fallback dùng 50% `Sky Scale` với giới hạn tối thiểu đủ phủ viewport. Sky dịch theo đúng nửa lượng pan của Ground 2. Bốn pha vẫn dùng cubemap BG Studio đã liên kết.
- **Rain, wind and storm visuals** trong Inspector của GameManager chứa Rain Intensity, Rain Speed, Drop Size, Wind Direction, Wind Strength, Gust Strength, Splash Intensity và Storm Haze. Các cờ Rain Wind, Ground Splash, Water Ripples, Distant Haze, Lightning và Reduced Flashes cho phép thử từng phần. **World Climate Rain 2D** điều khiển renderer mưa.
- Rain Maker phát tối đa 420 hạt và 360 hạt/giây. Mưa lớn rơi nhanh và dùng hạt lớn hơn mưa nhẹ. Gió dùng hàm liên tục theo đồng hồ, gust có chu kỳ 17 giây và hệ số từ cường độ mưa. Haze giảm độ tương phản của lớp cảnh xa bằng alpha.
- Splash xuất hiện tại giao điểm của hạt và mặt đất trong tọa độ BattleArea. Pool tối đa 72 splash, vòng đời 0,35 giây. Mỗi giọt dùng seed riêng để chọn điểm tiếp đất theo chiều sâu toàn BattleArea. Vị trí lưu theo Ground để pan và resize giữ đúng mặt đất. **Surfaces** nhận map ID, khoảng X trong một tile Ground và Y theo BattleArea. Đánh dấu **Water** trên dải thực sự có nước để bật ripple. Map hiện tại chưa có dữ liệu mặt nước đáng tin cậy, nên cấu hình mặc định để trống. Các dải nước/độ cao được kiểm chứng bằng fixture riêng.
- Sét chỉ được lên lịch trong HeavyRain ngoài trời, dựa trên seed và giây trong game. Mặc định mỗi khoảng 42 giây có tối đa một lần thử. Tia chớp trên sky kéo dài 0,23 giây với độ sáng mặc định 0,15, có Reduced Flashes và công tắc tắt. Sấm trễ 0,7–4,2 giây. Load, Retry và đổi map đặt lại mốc quan sát, không phát lại sự kiện cũ.
- **Environmental SFX** trong GameManager chứa các ô clip làng, rừng, núi, nước, chim/côn trùng, đêm, mưa nhẹ/mưa lớn, gió, gust, splash và sấm gần/xa. Điền **Village Map Ids** để chọn ambience làng. Environment Volume và Weather Volume dùng master SFX hiện có. Các loop fade/crossfade và tái dùng tối đa tám source. Một source dùng chung cho âm sự kiện. Các ô mặc định để null.
- **BGM map thường** dùng 12 WAV từ `music.zip`, import Vorbis streaming dưới `Assets/Resources/WorldMapMusic/`. Mỗi map mới chọn một track từ danh sách chung, tránh lặp ngay track trước nếu có lựa chọn. Dùng hai AudioSource và fade 1,25 giây khi đổi map, 10 giây gần cuối track. Nhạc mở đầu và hai track Thiên Quan dùng luồng hiện có. Máy khác có thể đặt các WAV được cấp vào thư mục trên để Unity tự import. WAV và asset nhà cung cấp được giữ cục bộ.

Chu kỳ 780 giây/ngày, seed/Save, Combat, AI, loot, cốt truyện và camera tracking dùng logic hiện có. Unity Player khởi động với profile riêng khi dùng launcher bên dưới.

## Kết quả

| Kiểm tra | Trạng thái |
| --- | --- |
| Unity 6000.5.0f1 compile và MainGame | **PASS**. Unity compile và 13 nhóm MainGame. Kiểm tra GUID 0 lỗi, 0 cảnh báo trong 1.104 file. |
| Windows Development build | **PASS**. Mono2x, 0 lỗi, 57 cảnh báo, 13.0 giây. EXE ở đường dẫn ổ D cố định. |
| Windows shader, sorting và HUD | **PASS**. RTX 2080/D3D11 hiển thị bốn pha, hạt mưa xanh nhạt và tia sét giới hạn trên sky. Ảnh cho thấy Hero và HUD vẫn rõ. Pan HDR bằng đúng một nửa Ground 2, PNG fallback lấp đầy viewport. |
| Mưa nhẹ/nặng, splash, ripple, pan và resize 600×338, 800×450, 1150×647 | **PASS**. 38 splash nhẹ, 70 splash lớn tại mẫu 800×450, cường độ đỉnh 0.42/1.23. Mẫu 256 seed cho độ sâu 0.0..99.0 trong BattleArea 0.0..100.0, splash sống đo được 3.8..97.6. Đổi độ phân giải và pan giữ đúng vị trí. Ripple chỉ xuất hiện trên dải nước fixture. |
| HeavyRain → Clear | **PASS**. Hiệu ứng giảm theo blend 8 giây, emitter, splash và rain loop về khô. |
| Sét và môi trường/SFX | **PASS**. Sét có seed, giảm chớp và tắt trong indoor. Clip null không tạo source. Clip fixture im lặng xác nhận sấm trễ và một lần phát. Hai nguồn loop thời tiết overlap rồi giải phóng nguồn cũ. |
| Travel, Save/Load/Retry và BGM | **PASS**. Travel xóa splash cũ, chọn bài khác và giữ crossfade với đúng hai source BGM. Save/Load/Retry giữ map/seed/giờ, không nhân nguồn SFX hoặc lặp sét. 12 clip có loại Streaming. Kiểm tra chooser Thiên Quan dùng track riêng. |
| Windows soak đủ 30 phút | **NOT RUN đủ 30 phút theo yêu cầu cập nhật.** Lượt Phase 2 ban đầu đã dừng trước khi đo đủ. Bản rải độ sâu được xác minh bằng smoke ngắn. |
| Windows Player khi thoát | **FAIL khi thoát sau khi ghi đủ 13 kết quả**. Event Viewer xác nhận UnityPlayer.dll 0xc0000005, offset 0x1dd029, exit -1073741819. |
| Thao tác chuột/phím native và nghe ngoài loa | **NOT RUN**. Fixture dùng handler và clip sấm im lặng tạo khi test. Ảnh được quan sát từ Player thật. |
| Build Acceptance sau Phase 2 | **NOT RUN**. Bản trên ổ D là Development Player dùng để đo và nghiệm thu. |

Mốc tham chiếu của lượt Phase 2 trước khi rải độ sâu là 300 giây hoạt động, 59,9 FPS trung bình, đỉnh 162 hạt và 72 splash, 5 lần chớp, Unity cấp phát 244,3 MiB và Mono dùng 8,9 MiB. Người dùng bỏ yêu cầu 30 phút và tiến trình fixture được dừng có chủ đích. Các số này thuộc build trước chỉnh độ sâu. [Log mẫu](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-e8450239347e48af88513d0f8f2040d3/player.log>).

### Bằng chứng cục bộ

- [Mở Windows Player bằng profile thử riêng](<D:/IdleGame/Builds/DynamicSkyWeather/build/Play-Dynamic-Sky-Weather-Isolated.cmd>) · [EXE](<D:/IdleGame/Builds/DynamicSkyWeather/build/IdleGameV10.exe>).
- [Unity MainGame](<D:/IdleGame/Logs/Unity-MainGameWorldClimateVendor-20261111-200034.log>) · [Windows build](<D:/IdleGame/Logs/Unity-WorldClimate-Phase2-BattleDepth-Windows-build.log>) · [Windows Player](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-acfec8c0592c442fb83d3f49ca4b61eb/player.log>) · [JSON](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-acfec8c0592c442fb83d3f49ca4b61eb/evidence/player-result.json>).
- [Splash nhẹ](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-acfec8c0592c442fb83d3f49ca4b61eb/evidence/phase2-light-splash.png>) · [splash lớn](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-acfec8c0592c442fb83d3f49ca4b61eb/evidence/phase2-heavy-splash.png>) · [nước 600×338](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-acfec8c0592c442fb83d3f49ca4b61eb/evidence/phase2-water-600.png>) · [nước 1150×647](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-acfec8c0592c442fb83d3f49ca4b61eb/evidence/phase2-water-1150.png>) · [tia sét](<D:/IdleGame/Builds/DynamicSkyWeather/profiles/idlegame-v10-acfec8c0592c442fb83d3f49ca4b61eb/evidence/phase2-lightning.png>).

Chạy lại qua `Tools/Local-IdleGame.ps1 -Mode MainGameWorldClimateVendor` trong worktree đã liên kết asset. Sau khi build, dùng `-Mode Player -PlayerMode WorldClimateVendor -WindowsOutputRoot D:/IdleGame/Builds/DynamicSkyWeather` và giữ cửa sổ Player ở foreground. Fixture dùng hồ sơ riêng ở Lạc Vân Trạch và dọn wave để đo trong khám phá.

## Git

- `51a349c` tỷ lệ và pan sky. `0648b1b` playlist chung cho map thường.
- `6ffac5c` gió, splash, ripple, haze và sét. `cdb6e29` âm thanh môi trường và thời tiết.
- `3871fea` fixture Phase 2 và bổ sung hỗ trợ mặt đất thấp hơn mặt mặc định.
- `dc1e2f6` rải vị trí tiếp đất ngẫu nhiên theo toàn bộ chiều cao BattleArea và chụp tia sét sau khi shader đã cập nhật. Các thay đổi cấu hình Unity và 15 .meta có từ trước vẫn ở worktree. Commit báo cáo xem trong [PR #16](https://github.com/lavievn/IdleGame/pull/16/commits).
