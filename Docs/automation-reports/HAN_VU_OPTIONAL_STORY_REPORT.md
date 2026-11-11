# Hàn Vụ thôn — Cốt truyện tự nguyện và kỳ ngộ

Issue [#14](https://github.com/lavievn/IdleGame/issues/14). Nhánh `feature/story-origin-thanhkhe-foundation`, baseline `a93c2161d39bdc6c22fd805a488ad3ee6d9645b3`. Báo cáo này ghi kết quả đợt triển khai tùy chọn. Kết quả vertical slice trước nằm trong [báo cáo origin](HAN_VU_ORIGIN_IMPLEMENTATION_REPORT.md).

## Hành vi

- Hero có thể rời làng ngay sang ô kề. Các map chiến đấu áp điều kiện clear bình thường khi rời map, kể cả đường quay về làng. Adjacency và khóa Boss vẫn áp dụng. Bấm ô kề đang khóa để đọc lý do. Nhấn Travel để thực hiện chuyển map.
- **Trò chuyện** mở lời nhờ tìm lá bạc. **Nhận lời** bắt đầu đếm. **Để sau** ghi lựa chọn từ chối và vẫn cho nhận lại khi quay về. **Đóng** giữ trạng thái hiện tại. Farm trước khi nhận không tạo thảo dược nhiệm vụ. Mỗi wave được CombatManager xác nhận hoàn tất ở Cô Hồn Lâm sau khi nhận cấp một lá, tối đa năm. Map wave có thể quay về 1 theo vòng đời travel/Retry bình thường trong khi bộ đếm truyện vẫn giữ.
- Nhật ký cho phép tắt/bật gợi ý riêng của Hero. Nút kỳ ngộ và trò chuyện vẫn dùng được. Gợi ý bản đồ dùng màu nhẹ khi đang làm truyện và có việc cần đi tới map khác. Dialogue, Journal và lựa chọn kỳ ngộ hoạt động cùng idle combat.
- Sói đầu đàn là đợt thứ năm **của chặng tìm kiếm** ở Ma Tùng Sâm. Kết quả lấy từ đánh dấu trên quái Elite khi spawn và được chuyển qua callback kill đã được chấp nhận. Thứ tự wave bản đồ sau Retry hoặc một con sói thường không thể cấp vật chứng. Lời kể, vật chứng và suy đoán về người bạn vẫn là những dòng riêng.

### Kỳ ngộ

| Kỳ ngộ | Điều kiện | Tham gia | Từ chối hoặc đóng |
|---|---|---|---|
| Gánh hàng ở đầu thôn | Hàn Vụ thôn | Hỏi thăm và ghi lời kể của người bán hàng | Xin phép đi ghi lựa chọn. Đóng giữ đang chờ. |
| Giỏ lá bên đường | Cô Hồn Lâm sau một wave thế giới hoặc đã clear | Xem giỏ và ghi những gì thấy | Để nguyên ghi lựa chọn. Đóng giữ đang chờ. |
| Khách qua đường | Bát Quỷ Lâm (TV-24) | Giúp buộc lại cổ tay và ghi kết quả | Từ chối ghi lựa chọn. Đóng giữ đang chờ. |

Chỉ mở kỳ ngộ mới ghi nhận đã thấy. Khi đã chọn, kỳ ngộ kết thúc và có thể đọc lại trong Nhật ký. Mỗi event dùng một ID ổn định và chỉ phát nội dung thông tin. Save ghi trạng thái đang chờ/đã tham gia/đã từ chối, không dựa vào thời gian thực hoặc roll khi tải.

## Save và tương thích

- Origin Save v2 lưu lựa chọn thảo dược, lịch sử từ chối, tùy chọn gợi ý và trạng thái kỳ ngộ. Dữ liệu dùng chung giao dịch lưu hiện tại.
- Save v1 giữ tên và ID. Save đã có lá bạc hoặc đã qua bước Gather tiếp tục nhiệm vụ đang làm. Save Gather chưa có lá bạc được đưa về lời mời. Save thiếu origin tiếp tục như cũ.
- Tiến độ story được ghi cùng snapshot Hero ở mốc quan sát, lựa chọn và wave hợp lệ. Việc load một slot cũ khôi phục toàn bộ snapshot của slot đó. Callback lặp trong một timeline bị chặn ở đường kill hiện có.

## Kiểm thử đợt này

| Ca | Kết quả |
|---|---|
| Unity 6000.5.0f1 compile và MainGame Hàn Vụ | **PASS 5/5**, exit 0. Đi và farm trước khi nhận, lý do khóa ở Bản đồ, tu luyện, ba lựa chọn kỳ ngộ, Save/Load, Retry, năm lá bạc, một vật chứng Elite và hai dạng Save cũ. |
| MainGame Development017 | **PASS 5/5**, exit 0. Loot, trang bị, Forge, skill, Save/Load, Retry và Thiên Quan. |
| Windows Development build | **Succeeded**, 0 lỗi, 58 cảnh báo. Mono2x, MainGame, SHA đã test `1fc0dde580a35276f35b0a03f470918960e4e738`. |
| Windows Player WARP | **PASS 5/5**, exit 0. Kết quả không có lỗi assertion. Lỗi teardown UnityPlayer đã ghi ở đợt trước không tái hiện trong lượt này. |
| Mono/UnityStub | **Bị chặn trước khi chạy assertion**. mcs mặc định C# 7.0 từ chối `readonly struct` có sẵn trong `WeaponSkillRegistry.cs`. |
| Chơi tay ở nhịp mặc định và chuột/phím native | **NOT RUN**. Smoke dùng handler và hit-test UI ở 800×450, rút ngắn wave bằng HP fixture và giữ pipeline kill/reward thật. Các kích thước cửa sổ khác chưa nghiệm thu. |

- Lệnh dùng `Tools\Local-IdleGame.ps1 -Mode MainGameHanVu`, `-Mode MainGame017`, `-Mode Build -WindowsOutputRoot D:\IdleGame\Builds\HanVuOptionalStory` và `-Mode Player -PlayerMode HanVuOrigin -WindowsOutputRoot D:\IdleGame\Builds\HanVuOptionalStory`.
- Log Unity tại `D:\IdleGame\Logs\Unity-MainGameHanVu-20261111-125045.log` và `Unity-MainGame017-20261111-125206.log`. Thư mục `.utmp` của checkout kiểm thử `D:\IdleGame\Projects\HanVuOptionalValidation` chứa hồ sơ `foundation-7e7cc5ec8bf6431e94f2a9068c497c60` và `foundation-a0344442274c47dab2b09c899a958824`.
- Hồ sơ Windows tại `D:\IdleGame\Builds\HanVuOptionalStory\profiles\idlegame-v10-0707b9c45ed343d192c001965470637a` có `player.log`, ảnh HUD và `evidence\player-result.json`.
- Bản thử ở `D:\IdleGame\Builds\HanVuOptionalStory\build\IdleGameV10.exe`. [Launcher WARP với hồ sơ Save thử riêng](D:/IdleGame/Builds/HanVuOptionalStory/build/Play-HanVu-Optional-Isolated.cmd).

## Tệp thay đổi

- Mã runtime gồm `Assets/Script/HanVuOrigin.cs`, `HanVuEncounters.cs` và `HanVuEncounters.cs.meta`, `GameManager.cs`, `CombatManager.cs`, `SaveManager.cs`, `UIManager.StoryOrigin.cs`, `WorldMapCatalog.cs`, `WorldMapPanelUI.cs`.
- Kiểm thử gồm `Assets/Script/HanVuOriginSmoke.cs`, `Tests~/StoryOriginRegression.cs`, `Tests~/run.sh`.
- Tài liệu gồm `Docs/automation-reports/HAN_VU_OPTIONAL_STORY_REPORT.md`.

- `e6af5291866443d2513f865bc0887a68bad36064` — travel, nhiệm vụ tùy chọn và ba kỳ ngộ.
- `8063fe49cfba7183f4d72c5493b47a980a0a303c` — fixture tùy chọn và tương thích Save.
- `1fc0dde580a35276f35b0a03f470918960e4e738` — lý do khóa khi chọn ô kề và kiểm tra Bản đồ.
- Báo cáo nằm trong commit tài liệu tiếp theo. Nhánh gốc tại `D:\IdleGame\Projects\HanVuOrigin` giữ các thay đổi local có sẵn. Build và smoke chạy từ checkout kiểm thử riêng và dùng Save fixture.

## Nghiệm thu và bước tiếp

Dùng launcher với hồ sơ thử riêng. Chọn **Chơi mới**, đi một map kề trước khi trò chuyện, farm và quay lại. Mở một kỳ ngộ rồi đóng để xem trong Nhật ký, sau đó chọn dứt khoát ở lần mở lại. Thử nhận lá bạc, tắt gợi ý, chết/Retry và load để đối chiếu bộ đếm nhiệm vụ. Thử mở lại game sau khi lưu.

Đợt sau có thể bổ sung điều kiện phát hiện theo trạng thái mô phỏng và chính sách hết hạn rõ ràng cho từng origin. Cần nghiệm thu thoại, kích thước HUD và nhịp chiến đấu thật trước khi mở rộng kho sự kiện.
