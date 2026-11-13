# Daily Freeze 12/11/2026 · Windows RC2

Nhánh `integration/daily-freeze-2026-11-12` có code ngày/đêm, thời tiết, sky projection, âm thanh môi trường và BGM. Bản Windows RC2 được build ở `82f5fc026ea22dee634a18798a3f5103d69eee91` với Unity `6000.5.0f1`. Nhánh freeze lưu 203 clip và metadata của chúng dưới dạng tệp Git thông thường, cùng cấu hình tham chiếu HDR/Rain Maker của dự án.

## Pull và build trên máy khác

1. Trong GitHub Desktop chọn nhánh `integration/daily-freeze-2026-11-12`, rồi **Fetch origin → Pull origin**. Đóng Editor của checkout này trước khi pull. Audio và metadata đi theo nhánh.
2. Mở project bằng Unity `6000.5.0f1`. Nếu checkout chưa có hai gói vendor, dùng **Window → Package Manager → My Assets** để import **Free HDR Skyboxes Pack 1.0** và **Rain Maker 2.1.2** bằng Unity ID đã có gói. Giữ đường dẫn mặc định và `.meta` khi import. Các gói này dùng Unity Asset Store EULA, nên source package được lấy từ My Assets. [Free HDR Skyboxes Pack](https://assetstore.unity.com/packages/2d/textures-materials/sky/free-hdr-skyboxes-pack-175525) · [Rain Maker](https://assetstore.unity.com/packages/vfx/particles/environment/rain-maker-2d-and-3d-rain-particle-system-for-unity-34938).
3. Unity liên kết cấu hình đã pull với GUID của gói. Nếu thiếu tham chiếu sau import, chạy **Bach Luyen → Environment → Link local weather assets** để liên kết lại. Sau đó mở `Assets/Scenes/MainGame.unity` và thử map ngoài trời. Map an toàn hoặc indoor có thể hạn chế hiệu ứng thời tiết.
4. Build bằng **Build Profiles → Windows** hoặc chạy `./Tools/Local-IdleGame.ps1 -Mode Build` trong **PowerShell 7** với thư mục output của máy. Dùng profile thử riêng khi nghiệm thu.

| Tài nguyên | Vị trí | Nguồn |
|---|---|---|
| Playlist map thường | `Assets/Resources/WorldMapMusic/` | 12 WAV trong Git |
| Âm thanh môi trường và sấm Thiên Quan | `Assets/Resources/IntegratedAmbience/` | 191 OGG trong Git, gồm `GateThunder.ogg` |
| Cấu hình tham chiếu HDR và mưa | `Assets/Resources/WorldClimateVendor/Assets.asset` | Cấu hình dự án trong Git |
| Cubemap HDR | `Assets/Free HDR Skyboxes Pack/` | Unity My Assets |
| Prefab và texture mưa 2D | `Assets/RainMaker/` | Unity My Assets |

Để kiểm tra khớp từng byte với bộ asset local đã dùng build RC2, chạy `Tools/Restore-DailyFreezeAssets.ps1` từ checkout. Script kiểm tra SHA-256 và kích thước của 478 tệp. `-SourceRoot` có thể trỏ tới project hoặc gói local có quyền sử dụng để chép các tệp còn thiếu. Nếu tệp đích khác, script báo đường dẫn để giữ thay đổi hiện có.

## Chơi trực tiếp bản Windows RC2

Tải `IdleGame-DailyFreeze-RC2-Windows.zip` từ [Release Draft nội bộ](https://github.com/lavievn/IdleGame/releases/tag/untagged-3da698e049d55b02199d) bằng tài khoản có quyền push. Đối chiếu SHA-256 với `RC2-Internal-SHA256.json`, giải nén cả gói rồi mở `build/Play-RC2-Isolated.cmd`. Launcher sử dụng profile riêng trong `build/PlaytestProfiles`. Bản Windows đã chứa HDR sky, mưa, BGM và audio môi trường.

Hai ZIP gốc và `sha256.json` nằm ở `D:\IdleGame\Builds\DailyFreeze_2026-11-12\CrossMachineHandoff`. Gói Windows chỉ chứa Player và launcher, không kèm hồ sơ thử, Save hay log.

## Kiểm tra gói bàn giao

- Đã clone nhánh freeze từ GitHub tại `c9f4ddf` và đối chiếu SHA-256 của 411 tệp tài nguyên có trong Git. Bổ sung 67 tệp vendor vào clone để khôi phục đủ 478 tệp, rồi build Windows với Unity `6000.5.0f1`. Build thành công, 0 lỗi và 58 cảnh báo.
- Smoke trên build mới xác nhận 190 OGG môi trường có trong Player, 133 map và Hàn Vụ lấy được nhóm âm, 20 lớp phát đúng mức và crossfade khi đổi map. Rain Maker, BGM và sấm Thiên Quan chạy theo kiểm tra runtime. Player ghi PASS nội dung rồi thoát với `-1073741819` (`0xc0000005`).
- Đã phục hồi đủ 478 tệp vào thư mục thử trống và xác minh SHA-256.
- Đã giải nén Windows RC2 vào đường dẫn mới có dấu cách. Launcher mở tới trạng thái sẵn sàng chơi với hồ sơ thử riêng.
- Smoke từ ZIP ghi 14 kiểm tra đạt, gồm bốn pha HDR, Rain Maker 2D, chuyển map và BGM. UnityPlayer.dll báo `0xc0000005` lúc thoát sau khi ghi kết quả. Thao tác chuột/phím thật và nghe ambience qua loa chưa được thử trong lượt đóng gói.

Các giới hạn còn lại nằm trong [báo cáo Daily Freeze](DAILY_FREEZE_2026-11-12_FINAL_REPORT.md).
