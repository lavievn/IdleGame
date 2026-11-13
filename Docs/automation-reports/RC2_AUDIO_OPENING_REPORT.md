# RC2 Audio Environment và Opening Quest

- Base `9813b3e6a0b70485fc04e58218e19b8e40846a93` từ `integration/daily-freeze-2026-11-12`.
- Nhánh `feature/rc2-audio-opening-quest`, [Draft PR #27](https://github.com/lavievn/IdleGame/pull/27).
- Opening Quest `dc5a98c44d4175fc1f396751f246802332cf7885`.
- Audio, manifest, kiểm kê và test `b9d71f7ea486dc11cf3324c7f1cf51ee594886b1`.
- Giới hạn hai nguồn Coast cả lúc chuyển tiếp `7aa2a130c725b95da2426d6880177077d6ad753d`.
- Windows build bàn giao dùng `b09f06d`, cập nhật assertion import mode của one-shot Thiên Quan.

## Âm thanh

Bản ZIP thứ hai chứa 191 OGG, đều qua CRC và được ffprobe đọc. SHA-256 của ZIP là `650aa81e8cc3d9d4b6ad668e6338d7001c11a825a3c05d25be1cc2f80037918a`. Bảng [kiểm kê](ENV_MS_INVENTORY.csv) ghi tên gốc, metadata, checksum, giới hạn, cách phát và đường dẫn Unity. 178 mục phát trỏ đến asset đã có cùng SHA-256. Một số tên trong ZIP đã đổi, nên manifest giữ tên gốc để phân loại và tham chiếu asset Unity qua tên hiện có. Giữ nguyên GUID và thiết lập streaming. GateThunder là one-shot riêng của Thiên Quan.

Coast giữ tối đa ba loop nền và một sự kiện beach/wave/wind theo chu kỳ 10–15 giây. `Coast_heavy_rain` và `Coast_medium_rain` phát loop khi Rain. `Coast_wave_storm` phát loop khi Storm. Giới hạn Coast weather mặc định là 2 theo xác nhận, có thể chỉnh trong `WorldWeatherAudioSettings`. Khi cả hai slot đã có clip, Rain ↔ Storm giải phóng một slot rồi crossfade với loop còn lại. Global Rain, Thunder, WindRain và Pebble có giới hạn riêng. Fade mặc định 2 giây. Khi Meadow hoặc Morning xoay tập, tối đa một tập cũ có thể overlap với tập mới trong lúc fade.

Map dùng thời tiết LightRain làm Rain và HeavyRain làm Storm cho audio. Nhóm Meadow phát hai loop nền và một cricket khi khô, xoay hai loop nền sau 300 giây. Trong Rain, Meadow chỉ phát một loop ướt. Trong Storm, để Meadow im trong khi các overlay toàn cục vẫn phát. Morning xoay tập sau 120 giây. Thunder môi trường dùng one-shot với chu kỳ thử 26,25–43,75 giây quanh mốc 35 giây cấu hình được. Nếu clip cũ còn phát, scheduler bỏ lượt đó. Gain mỗi file là 0,50 nhân mức Environment hoặc Weather hiện hành.

[Bảng map](ENV_MS_MAP_FAMILIES.csv) gồm 133 map, tính Hàn Vụ thôn theo profile village của tuyến mở đầu. Có 13 Coast, 19 Meadow, 80 Morning + Primeval và 21 map chưa gán family cục bộ. Overlay toàn cục vẫn được xét trên các map bình thường chưa gán. Lodges đã hỗ trợ tối đa một loop, đang chờ danh sách map.

12 tệp **NEEDS_REVIEW** gồm `Coast_near_water.ogg`, `Meadow_cricket2.ogg`, `Meadow_wind.ogg` và `*_base.ogg` của Lodges, Meadow, Morning, Night, Pebble, Primeval, Rain, Thunder, WindRain. Cần chốt vai trò của các hậu tố này, map Lodges, Meadow rain khi Storm, nhóm cục bộ của các map chưa gán và nhịp Thunder cuối.

## Opening Quest

Hội thoại có chỉ số lượt đọc và nút đóng hoặc để sau. Kết thúc hội thoại thường có nút Xong. Lựa chọn nhận, từ chối và bỏ qua giữ kết quả riêng. Hướng dẫn dùng trạng thái nhiệm vụ hiện có để tô nút tên map, đánh dấu đích có thể tới và chỉ dẫn nhấn nút Travel. Người chơi vẫn có thể chọn map khác. Nhấn chọn polygon giữ nguyên vị trí hiện tại. Tùy chọn ẩn gợi ý áp dụng cả HUD lẫn dấu trên World Map.

## Kiểm thử

| Bài thử | Kết quả |
|---|---|
| CRC, SHA-256, codec, sample rate, channel, thời lượng và trùng asset | PASS, 191 OGG |
| Parser và scheduler C# (.NET 10, thời gian và RNG điều khiển được) | PASS, 35 kiểm tra |
| Unity 6000.5.0f1 compile và MainGame Play Mode | PASS, hội thoại, Travel, Hàn Vụ, Save/Load, Retry và ngăn nhận thưởng trùng |
| AudioSource trong Play Mode | PASS, clip thật, Rain ↔ Storm → Clear, Travel, cập nhật lặp và tái dùng nguồn sau reset |
| HUD 600×338, 800×450, 1150×647 | PASS qua harness Play Mode |
| Windows Development build | PASS, Unity 6000.5.0f1, Mono x64, 0 lỗi build, 58 cảnh báo |
| Windows Hàn Vụ và Coast hai nguồn | PASS cho các bước gameplay và AudioSource qua harness WARP. FAIL khi thoát với `UnityPlayer.dll 0xc0000005`, exit `-1073741819` |
| Windows tích hợp | PASS cho 9 kiểm tra qua harness WARP, gồm Rain Maker, BGM, Travel, Save/Load, EXP và Thiên Quan. FAIL khi thoát với cùng mã lỗi |
| Chuột/phím thật và nghe phối âm trực tiếp | NOT RUN |

Play Mode và Player harness dùng profile sở hữu riêng và gọi handler. Log Editor có cảnh báo shader fallback Rain Maker, video frame đầu, Unity TLS và exception từ UnityEditor.Search khi index. Smoke Hàn Vụ hoàn thành sau các thông báo này.

Windows build nằm ở `D:/IdleGame/Builds/Rc2AudioOpeningHandoff/build/IdleGameV10.exe`. Log kiểm thử domain và Play Mode ở `D:/IdleGame/Logs/Rc2AudioOpening-Domain-Final.log` và `D:/IdleGame/Logs/Rc2AudioOpening-PlayMode-Coast2.log`. Lượt Play Mode chạy bản trước giới hạn nguồn vật lý. Smoke Windows Hàn Vụ đã xác minh giới hạn cuối trong thư mục `D:/IdleGame/Builds/Rc2AudioOpeningFinal/profiles/idlegame-v10-1052e18595c44e5fa558d798620e9e19`.

Kết quả tích hợp và ảnh nằm ở `D:/IdleGame/Builds/Rc2AudioOpeningHandoff/profiles/idlegame-v10-dc549d8c5d3340fb902b5ffc4df34ec0/evidence`. Git diff check sạch. Các commit thay đổi nằm trên nhánh feature.
