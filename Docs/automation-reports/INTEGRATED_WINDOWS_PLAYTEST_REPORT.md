# Bản Windows Development tích hợp

Nhánh local `integration/hero-hanvu-weather-audio` ở `D:/IdleGame/Projects/IntegratedPlaytest`.

## Cập nhật bản thử mix theo tiền tố — 12/11/2026

Bản Windows Development hiện tại dùng mã `350049e`. [Mở launcher với hồ sơ riêng](<D:/IdleGame/Builds/IntegratedPrefixMix/build/Play-Integrated-Isolated.cmd>) · [EXE](<D:/IdleGame/Builds/IntegratedPrefixMix/build/IdleGameV10.exe>).

- Build có đủ 190 OGG nguồn. Mỗi map chọn một tiền tố và chạy đồng thời đủ 20 clip thuộc nhóm đó, mỗi clip có gain cơ sở `0,50`. Làng, đồng bằng, thảo nguyên, cao nguyên, tuyết và sa mạc dùng `Meadow`. Rừng dùng `Primeval`. Hồ và đầm lầy dùng `Morning`. Ven biển dùng `Pebble`. Các nhóm tiền tố còn lại có trong bản thử và chưa được gán vào map. Giới hạn voice thực tăng lên 64 để hai nhóm 20 clip có thể fade chồng khi Travel.
- **Âm thanh môi trường** là thanh riêng trong Cài đặt ở menu chính và bảng Âm lượng trong game. Mặc định 100%, lưu riêng. Điều khiển ambience, âm thanh thời tiết và sấm Thiên Quan. Ở mức 100%, mỗi clip trong nhóm phát với gain `0,50`. [Ảnh menu 600×338](<D:/IdleGame/Builds/IntegratedPrefixMix/profiles/idlegame-v10-c482be7ed8ff48b4a7251f432367020e/evidence/opening-environment-volume.png>) · [Ảnh trong game 800×450](<D:/IdleGame/Builds/IntegratedPrefixMix/profiles/idlegame-v10-c482be7ed8ff48b4a7251f432367020e/evidence/gameplay-environment-volume.png>).
- Kích thước hạt mưa tăng từ `0,30` lên `0,60` theo cấu hình Rain Maker. Hero có tốc độ mặc định và Run 200 đơn vị/giây khi tiếp cận địch. Walk khám phá là 100 đơn vị/giây, độc lập với trang bị và buff. Map an toàn chạy Idle và Hero đứng yên. Thiên Quan tiếp tục dùng luồng tiếp cận của thử thách.

| Kiểm thử bản cập nhật | Kết quả |
| --- | --- |
| MainGame, Unity 6000.5.0f1 | **PASS 8/8**. Ba thanh âm lượng, 133 map và biến thể làng, Rain Maker, Travel, Idle/Walk/Run, Save/Load, Thiên Quan và Retry. [Log](<D:/IdleGame/Logs/Unity-MainGameIntegrated-20261112-003925.log>) |
| Windows Development build | **PASS**. Mono2x, 0 lỗi, 58 cảnh báo. [Log](<D:/IdleGame/Builds/IntegratedPrefixMix/build.log>) |
| Windows tích hợp, RTX 2080 | **PASS 8/8**. Hai nhóm 20 clip đều phát thực khi chuyển map. Hạt mưa và tốc độ Hero được kiểm tra trên Player. [JSON](<D:/IdleGame/Builds/IntegratedPrefixMix/profiles/idlegame-v10-c482be7ed8ff48b4a7251f432367020e/evidence/player-result.json>) · [Bảng map](<D:/IdleGame/Builds/IntegratedPrefixMix/profiles/idlegame-v10-c482be7ed8ff48b4a7251f432367020e/evidence/map-ambience.tsv>) |
| Windows Hàn Vụ, WARP | **PASS 7/7**. Hội thoại, đi lại tự do, kỳ ngộ, thảo dược, Elite và tương thích Save. [JSON](<D:/IdleGame/Builds/IntegratedPrefixMix/profiles/idlegame-v10-8f9b17c8e34a4d649b3cd2cd0f3b465c/evidence/player-result.json>) |
| Thoát Player tự động | **PASS** ở hai lượt smoke. Nhật ký sự kiện Windows không có Application Error cho đường dẫn build trong khoảng kiểm thử. |

Thao tác chuột/phím vật lý và mức âm nghe qua loa **NOT RUN**. Log Player vẫn có cảnh báo video hiện hữu. Các commit code local gồm `cb13338`, `ee92f75` và `350049e`. Worktree còn thay đổi thiết lập và metadata do Unity sinh từ đợt tích hợp.

## Bản thử tích hợp trước cập nhật

| Phần | Checkpoint nguồn | Merge local |
| --- | --- | --- |
| Nền Development 0.1.7 | `fafc02d` | — |
| PR #12 Hero PNG | `5d41014` | `0801a66` |
| Hàn Vụ | `5915829` | `66cb8c1` |
| PR #16 Dynamic Sky Weather | `c00dae6` | `53183ea` |

Giải sáu xung đột Weather/Hàn Vụ ở snapshot và bộ chạy kiểm thử. Worktree tích hợp nhận cubemap HDR, Rain Maker, cấu hình tham chiếu vendor và 12 WAV BGM từ DynamicSkyWeather, giữ nguyên metadata. Tệp được đối chiếu byte và Unity quét **1.404 tệp, 0 lỗi, 0 cảnh báo GUID/tham chiếu**. Unity tự sinh .meta cho các PNG Hero và một số art terrain trong worktree mới.

## Âm thanh môi trường

Đã xác minh 190 OGG, 175.024.064 byte, từ thư mục Drive người dùng cung cấp. Bộ cache local giữ nguyên các nguồn. Phân tích theo tiền tố, thời lượng, RMS, độ biến thiên và năng lượng theo dải tần. Nhiều hậu tố có thời lượng/phổ gần nhau, chẳng hạn Coast_02/13, nhưng không ghép A/B khi chưa xác nhận quan hệ biên vòng lặp. [Mô tả hệ thanh trượt của myNoise](https://mynoise.net/NoiseMachines/help.php).

| Kết quả phân loại | Số clip |
| --- | ---: |
| Đã gán vào mix hoặc trích sấm | 29 |
| Vai trò/hậu tố chưa xác định đủ rõ | 130 |
| Gần im lặng theo mức RMS phân tích | 13 |
| Nhóm nhạc nền có cao độ, để ngoài lớp ambience | 18 |

Xem [bảng từng OGG và vai trò](<D:/IdleGame/Caches/IntegratedAmbience/assignment.tsv>) và [số liệu âm thanh](<D:/IdleGame/Caches/IntegratedAmbience/analysis.tsv>). Chỉ clip đã gán được đưa vào build. Snow/Desert dùng các lớp gió/nền phù hợp từ bộ hiện có.

Mười profile môi trường phủ **133 map**, có cấu hình riêng khi TV-20 là Hàn Vụ thôn. Mỗi profile dùng tối đa bảy lớp môi trường, cộng lớp mưa nhẹ, mưa lớn và gió. Giờ trong game và thời tiết điều khiển gain. Loop tiếp tục ở cùng vị trí khi chỉ đổi gain. Travel dùng pool hai nguồn mỗi slot để chồng fade, tối đa 10 slot đang chọn. BGM của map thường và Thiên Quan dùng controller và playlist riêng.

Tiếng sấm Thiên Quan là đoạn 5,70 giây từ `Thunder_05` khoảng 33,35–39,05 giây. Sự kiện impact đưa đúng một yêu cầu vào hàng đợi theo ID encounter và số thứ tự tia sét. **Trial Thunder Delay** mặc định 0,12 giây và **Trial Thunder Volume** 0,67. Kết thúc encounter dừng nguồn và hủy hàng đợi. Lịch sét, damage và VFX dùng luồng combat cũ.

## Kiểm thử và bản chơi

| Nhóm | Kết quả |
| --- | --- |
| Unity 6000.5.0f1 compile và import | **PASS**. [Log](<D:/IdleGame/Logs/Unity-Check-20261111-234858.log>) |
| MainGame tích hợp | **PASS 6/6**. Hero animation thật, 133 map và 10 slot, mưa/ngày đêm, Travel và BGM, snapshot, sấm và hủy khi Load/Retry. [Log](<D:/IdleGame/Logs/Unity-MainGameIntegrated-20261112-000014.log>) |
| MainGame Hàn Vụ | **PASS 7/7**. Thoại, lựa chọn, năm wave, Elite, migrate Save. [Log](<D:/IdleGame/Logs/Unity-MainGameHanVu-20261111-235615.log>) |
| Windows Development build | **PASS**, Mono2x, 0 lỗi, 58 cảnh báo, 16.2 giây. |
| Windows tích hợp RTX 2080 | **PASS 6/6**. Animation Hero, 10 lớp, crossfade và playhead, BGM, Save/Load/Retry, sấm impact. |
| Windows tuyến Hàn Vụ | **PASS 7/7**. Mở đầu, tự do khám phá, năm wave, Elite và Save cũ trên cùng EXE. |
| Windows Weather/HDR | **PASS 14/14**. HDR bốn pha, 600/800/1150, Rain Maker, gió và splash, blend và audio. |
| Windows teardown | **PASS 3/3**. Ba lượt trên đúng EXE cuối đều thoát mã 0, không có Application Error cho đường dẫn build trong khoảng kiểm thử. Không tái hiện UnityPlayer.dll 0xc0000005. |

- [RTX tích hợp log](<D:/IdleGame/Builds/IntegratedPlaytest/profiles/idlegame-v10-54bd16a94ee2403eb45aa307abc8e0da/player.log>) · [RTX tích hợp JSON](<D:/IdleGame/Builds/IntegratedPlaytest/profiles/idlegame-v10-54bd16a94ee2403eb45aa307abc8e0da/evidence/player-result.json>) · [Hàn Vụ log](<D:/IdleGame/Builds/IntegratedPlaytest/profiles/idlegame-v10-ade131a948a3489e92ca3f06155d7762/player.log>) · [Hàn Vụ JSON](<D:/IdleGame/Builds/IntegratedPlaytest/profiles/idlegame-v10-ade131a948a3489e92ca3f06155d7762/evidence/player-result.json>) · [Weather log](<D:/IdleGame/Builds/IntegratedPlaytest/profiles/idlegame-v10-6791a39e67e945b7b54d4c26b3edbaa9/player.log>) · [Weather JSON](<D:/IdleGame/Builds/IntegratedPlaytest/profiles/idlegame-v10-6791a39e67e945b7b54d4c26b3edbaa9/evidence/player-result.json>) · [Rà 133 map và biến thể làng](<D:/IdleGame/Builds/IntegratedPlaytest/profiles/idlegame-v10-54bd16a94ee2403eb45aa307abc8e0da/evidence/map-ambience.tsv>) · [Ảnh Unity Hero/Hàn Vụ](<D:/IdleGame/Projects/IntegratedPlaytest/.utmp/foundation-a513075546df475bac03b5839c4a89be/evidence/integrated-hero-village.png>) · [Ảnh Unity Mưa đêm](<D:/IdleGame/Projects/IntegratedPlaytest/.utmp/foundation-a513075546df475bac03b5839c4a89be/evidence/integrated-night-rain.png>) · [Ảnh Unity Rừng mưa lớn](<D:/IdleGame/Projects/IntegratedPlaytest/.utmp/foundation-a513075546df475bac03b5839c4a89be/evidence/integrated-forest-heavy-rain.png>) · [Ảnh Unity Thiên Quan](<D:/IdleGame/Projects/IntegratedPlaytest/.utmp/foundation-a513075546df475bac03b5839c4a89be/evidence/integrated-gate-thunder.png>).

- [Launcher hồ sơ riêng](<D:/IdleGame/Builds/IntegratedPlaytest/build/Play-Integrated-Isolated.cmd>) · [IdleGameV10.exe](<D:/IdleGame/Builds/IntegratedPlaytest/build/IdleGameV10.exe>)
- [Log Windows build](<D:/IdleGame/Builds/IntegratedPlaytest/build.log>)

Các smoke gọi handler trong hồ sơ có marker riêng. Ngưỡng kiểm tra fade dùng master SFX hiện tại. Launcher mở menu với hồ sơ riêng và bật Thiên Quan DEV. Mức âm nghe qua loa và thao tác chuột/phím thủ công **NOT RUN**. Asset vendor và âm thanh nguồn nằm cục bộ. Nhánh tích hợp không có remote tracking.

Commit code của bản build `a747735`. Báo cáo nằm ở commit kế tiếp trên cùng nhánh local.
