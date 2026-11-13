# Daily Freeze 12/11/2026 · Tái tạo Windows RC2

Nhánh `integration/daily-freeze-2026-11-12` có code ngày/đêm, thời tiết, sky projection, âm thanh môi trường và BGM. Bản Windows RC2 được build ở `82f5fc026ea22dee634a18798a3f5103d69eee91`. Một checkout mới thiếu các tài nguyên local trong `.gitignore`. Game vẫn mở bằng sky PNG dự phòng và có thể thiếu mưa vendor, ambience cùng playlist BGM mới.

## Chơi trên máy khác

Chuyển riêng tư `IdleGame-DailyFreeze-RC2-Windows.zip` từ máy đã có RC2. Đối chiếu SHA-256 với `sha256.json`, giải nén cả gói vào thư mục có quyền ghi, rồi mở `build/Play-RC2-Isolated.cmd`. Launcher dùng profile thử riêng trong `build/PlaytestProfiles`. Mở cùng launcher lần sau để tiếp tục profile đó. Build dùng Unity `6000.5.0f1` và chạy trực tiếp trên Windows, không cần Unity Editor.

## Build lại đúng asset RC2

1. Pull nhánh `integration/daily-freeze-2026-11-12` và đóng Unity của checkout này.
2. Lấy `IdleGame-DailyFreeze-RC2-LocalAssets.zip` bằng kênh riêng tư từ máy có tài nguyên và quyền sử dụng. Đối chiếu SHA-256, giải nén ra thư mục riêng. Thư mục nguồn phải chứa `Assets/`. Có thể dùng trực tiếp một bản sao project đầy đủ đã được cấp quyền.
3. Từ thư mục gốc Unity checkout, chạy lệnh sau, đổi đường dẫn cho đúng máy.

   ```powershell
   powershell.exe -NoProfile -File .\Tools\Restore-DailyFreezeAssets.ps1 -SourceRoot 'D:\RC2-LocalAssets'
   ```

   Script kiểm tra SHA-256 và kích thước của 478 tệp, gồm metadata Unity. Nếu đích đã có tệp khác bản RC2, script dừng và liệt kê tệp đó. Dùng checkout riêng để giữ mọi thay đổi hiện có. Có thể bỏ `-SourceRoot` để chỉ xác minh. Kết quả đúng là `Verified 478 RC2 asset files`.
4. Mở Unity `6000.5.0f1`, chờ import và mở `Assets/Scenes/MainGame.unity`. Có thể dùng `Tools/Local-IdleGame.ps1 -Mode Build` hoặc Build Profiles để build Windows. Chọn thư mục output riêng theo cấu hình của máy. Khi kiểm thử, đổi sang map ngoài trời. Một map an toàn hay indoor có thể không hiện đủ hiệu ứng thời tiết.

## Nguồn từng nhóm asset

| Nhóm | Vị trí trong project | Cách khôi phục |
|---|---|---|
| Free HDR Skyboxes Pack 1.0 | `Assets/Free HDR Skyboxes Pack/` | Import bằng Unity ID có quyền dùng gói. [Unity Asset Store](https://assetstore.unity.com/packages/2d/textures-materials/sky/free-hdr-skyboxes-pack-175525). |
| Rain Maker 2.1.2 | `Assets/RainMaker/` | Import bằng Unity ID có quyền dùng gói. [Unity Asset Store](https://assetstore.unity.com/packages/vfx/particles/environment/rain-maker-2d-and-3d-rain-particle-system-for-unity-34938). |
| Cấu hình kết nối hai gói | `Assets/Resources/WorldClimateVendor/Assets.asset` | Có trong gói local. Nếu import từ My Assets, chọn **Bach Luyen → Environment → Link local weather assets** trong Unity để tạo cấu hình với GUID của gói đã import. |
| Playlist map thường | `Assets/Resources/WorldMapMusic/` | 12 WAV đã cấp cho dự án cùng `.meta` import. |
| Âm thanh môi trường và sấm Thiên Quan | `Assets/Resources/IntegratedAmbience/` | Bộ OGG đã cấp, `GateThunder.ogg` và `.meta`. Dùng bộ asset local đã xác minh để giữ đúng các lớp theo nhóm tên. |

GitHub công khai chứa code, manifest và công cụ xác minh. Gói nguồn Asset Store và audio có quyền phát hành công khai chưa được xác nhận được giữ ngoài repo. Kiểm thử trên một máy khác cần bản build chuyển riêng tư hoặc asset do tài khoản có quyền cung cấp. [Unity Asset Store EULA](https://unity.com/legal/as-terms).

Trên máy build gốc, hai ZIP và `sha256.json` nằm tại `D:\IdleGame\Builds\DailyFreeze_2026-11-12\CrossMachineHandoff`. Bản Windows được đóng gói từ chính RC2 đã nghiệm thu. Gói chỉ chứa các tệp chạy và launcher, không kèm hồ sơ thử, Save hoặc log.

## Kiểm tra gói bàn giao

- Phục hồi đủ 478 tệp vào thư mục thử trống. Lượt xác minh tiếp theo khớp SHA-256 của toàn bộ tệp.
- Giải nén ZIP Windows vào đường dẫn mới có dấu cách. Launcher mở tới trạng thái sẵn sàng chơi và chuyển Save sang hồ sơ thử trong thư mục giải nén.
- Smoke từ bản đã giải nén ghi 14 kiểm tra đạt, gồm bốn pha HDR, Rain Maker 2D, chuyển map và BGM. Lượt chạy thoát với `UnityPlayer.dll` `0xc0000005` sau khi ghi kết quả. Thao tác chuột/phím thật và nghe ambience qua loa chưa được thử trong lượt đóng gói này.

Các giới hạn nghiệm thu RC2 nằm trong [báo cáo Daily Freeze](DAILY_FREEZE_2026-11-12_FINAL_REPORT.md).
