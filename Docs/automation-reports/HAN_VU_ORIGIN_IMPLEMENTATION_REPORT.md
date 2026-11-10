# Hàn Vụ thôn — Story Origin & Gameplay Vertical Slice

## Phạm vi và baseline

- Nhánh `feature/story-origin-thanhkhe-foundation`, baseline `fcfbc85affbf166d9e27e9209c2e22920066982c`. Worktree trên `D:\IdleGame\Projects\HanVuOrigin`. Checkout Development 0.1.7 tại `D:\IdleGame\Project` vẫn giữ các thay đổi sẵn có.
- Đã đối chiếu hợp đồng Hàn Vụ, các skill Unity/C#, Save và build cùng luồng GameManager → World Map → Combat → SaveManager. New Game cũ chọn ngẫu nhiên một trong năm map sinh và roll race. World Map đã hỗ trợ travel thủ công, lặp năm wave và cấp phần thưởng qua CombatManager. SaveManager nạp vào candidate trước khi thay Hero đang chạy.

## Phần chơi được

1. Chọn **Chơi mới**. Hero là Nhân tộc, người nuôi dưỡng là Nhân tộc và bạn thuở nhỏ khác giới Hero. Cả ba dùng NameDatabase 2–3 từ. Root giữ xác suất 1/2/3/4 linh căn **60/25/10/5**. Hàn Vụ thôn dùng ô **TV-20**, với lời dẫn tự hiện và nút trò chuyện.
2. Bấm **tên map** bên trái để mở Bản đồ. Chọn **Cô Hồn Lâm** (TV-16), rồi bấm **Tới ô đã chọn**. Qua năm wave, mỗi wave hoàn tất ghi một thảo dược vào túi nhiệm vụ trong Nhật ký, độc lập với loot thường.
3. Khi đủ năm cây, tên map được tô sáng. Tự trở lại Hàn Vụ và bấm **Sắc thuốc**. Người nuôi dưỡng dịu cơn ho và kể bạn thuở nhỏ đã vào rừng tìm vị thuốc khác.
4. Trở lại Cô Hồn Lâm, bấm **Xem dấu vết**. Mảnh giấy ở ngã rẽ dẫn tới **Ma Tùng Sâm** (TV-11). Tự di chuyển, qua năm wave và hạ sói đầu đàn ở wave cuối. Nhật ký ghi vật chứng dây vải buộc nút đôi và suy đoán của Hero thành hai mục riêng.

### Dữ liệu và tương thích

- `OriginDefinition`, `OriginStoryState` và `StoryOrigin` chứa định nghĩa tuyến, checkpoint và quy tắc sự kiện. Origin `hanvu`, quest `hanvu.find_medicine`, NPC `hanvu.guardian` và `hanvu.companion`. Túi nhiệm vụ có các ID `hanvu.herb`, `hanvu.trail`, `hanvu.elite_clue`, tách khỏi hòm đồ và phần thưởng ngẫu nhiên.
- Alias Hàn Vụ và cấm wave chỉ áp dụng cho Hero có origin này. Hai map chiến đấu dùng sprite Wolf và melee basic. Tạm dùng cấp 1 ở rừng thuốc, cấp 2 ở rừng tìm kiếm và cấp 3 với +50% HP ở sói đầu đàn. Wave cuối chỉ có một Elite, và chỉ trong giai đoạn tìm kiếm. EXP, sát thương, loot và chống callback trùng đi qua pipeline hiện có.
- Snapshot lưu danh tính, giai đoạn và số wave đã xong. Load/Retry giữa wave mở đầu sẽ chơi lại wave dở, giữ các wave đã ghi nhận. Travel khỏi tuyến bị giới hạn trong lúc làm origin. Sau khi hoàn tất, các quy tắc bản đồ thông thường trở lại và đường rời làng vẫn qua Cô Hồn Lâm.
- Save thiếu origin giữ nguyên tên, race, map và tiến trình. Nội dung truyện không được sinh cho nhân vật cũ. Quest được ghi qua AutoSave hiện có ở mốc truyện. Load một bản thủ công cũ khôi phục toàn snapshot của bản đó.
- HUD placeholder dùng Canvas/TMP và cơ chế input hiện có. Gợi ý, đối thoại và Nhật ký Hàn Vụ không pause. Bản đồ và Kho đồ vẫn có hành vi mở panel như trước. Không có sprite, animation, camera, VFX hay âm thanh mới.

## Kiểm thử

| Ca | Kết quả |
|---|---|
| Unity compile và MainGame Play Mode 6000.5.0f1 | **PASS 5/5**, exit 0. Kiểm tra 256 lượt sinh nhân vật, pool họ/tên/đệm, odds linh căn, map an toàn, lựa chọn polygon và Travel, tất cả mốc, reload wave 5, Retry, callback lặp và Save thiếu origin. |
| MainGame smoke Development 0.1.7 | **PASS 5/5**, exit 0. Loot thật, compare, trang bị, Forge, skill queue, Save/Load, Retry và Thiên Quan. Fixture cũ được dẫn thủ công từ làng tới rừng. |
| Build Windows Development Mono | **Succeeded**, 0 lỗi, 58 cảnh báo. Bản build dùng code/test HEAD `319c5b8224b296bc9fbbf2ad1cb586ad83b56f30`. |
| Windows Player WARP | **Gameplay PASS 5/5 ở cả hai lượt**. Tệp kết quả đã được ghi, không lỗi assertion. **Teardown FAIL** hai lượt, exit `-1073741819` / `0xc0000005`, `UnityPlayer.dll`, offset `0x1dd029`. Khớp chữ ký sự cố đã ghi ở [Skill UI & VFX Hotfix](SKILL_UI_VFX_HOTFIX_REPORT.md). WARP vẫn chạy được gameplay nhưng không tránh lỗi thoát ở bản này. |
| Mono/UnityStub | **Không chạy được assertion**. mcs mặc định C# 7.0 từ chối `readonly struct` sẵn có. Thử C# 7.2 bộc lộ lỗi API stub và ca legacy `HeroHealthMultiplier` đã bị gỡ trước batch. Đã kiểm tra quy tắc tên và roots trong Unity thật. |
| Chuột/phím native và chơi đủ tuyến với HP/timing mặc định | **NOT RUN**. Smoke gọi handler và vùng hit-test thật, rút ngắn các wave bằng HP fixture trong khi vẫn dùng đòn Hero, pipeline kill/reward và save thật. |

- Lệnh `Tools\Local-IdleGame.ps1 -Mode MainGameHanVu`, `-Mode MainGame017`, `-Mode Build -WindowsOutputRoot D:\IdleGame\Builds\HanVuOrigin`, và `-Mode Player -PlayerMode HanVuOrigin -WindowsOutputRoot D:\IdleGame\Builds\HanVuOrigin`.
- Log Play Mode `D:\IdleGame\Logs\Unity-MainGameHanVu-20261110-172601.log` và `Unity-MainGame017-20261110-172658.log`. Cảnh báo ngoài gameplay có UnityEditor Search và Pause trên VideoPlayer đã tắt.
- Hai hồ sơ Player dưới `D:\IdleGame\Builds\HanVuOrigin\profiles`, `idlegame-v10-47349e18ccac42b487fa94b3f6717468` và `idlegame-v10-27510e71072a4eefb2942f9b36de29c7`. Mỗi hồ sơ có `player.log`, ảnh và `evidence\player-result.json`.
- Không dùng Save cá nhân. Các tệp Unity tự sinh khi import/build được tách khỏi commit. Metadata tự sinh được giữ tại `D:\IdleGame\Automation\HanVuOrigin-319c5b8-import-metadata`.

## Checklist nghiệm thu Windows

- Dùng launcher [Play-HanVu-Isolated.cmd](D:/IdleGame/Builds/HanVuOrigin/build/Play-HanVu-Isolated.cmd) để giữ hồ sơ thử riêng, rồi chọn **Chơi mới**. Mở lại cùng launcher để thử Continue. Bản build ở `D:\IdleGame\Builds\HanVuOrigin\build\IdleGameV10.exe`.
- Quan sát lời dẫn, tên làng và **Wave — / An toàn**. Thử Trò chuyện hai lần, ẩn gợi ý rồi mở Nhật ký.
- Mở Bản đồ bằng tên map và thử chọn rừng mà chưa bấm Travel. Sau khi tới rừng, chơi đủ năm wave ở HP và timing bình thường, thử Save/Load hoặc Retry ở wave 5 và kiểm tra số cây không tăng trùng.
- Quay về Sắc thuốc, sau đó tìm mảnh giấy ở Cô Hồn Lâm. Thử mở Nhật ký khi sói đang hoạt động. Đi Ma Tùng Sâm, hạ Elite, mở lại nhật ký và kiểm tra vật chứng, lời kể, suy đoán.
- Mở Kho đồ, trang bị loot nếu có, thử Forge và Save/Load. Kiểm tra gợi ý, nút và dòng chữ trên kích thước cửa sổ bạn thường dùng.

## Commit và phần còn lại

- `e514ef17312e17d99369e32e3fd4f715259a5c6c` — nguồn origin, save, travel, wave và HUD placeholder.
- `319c5b8224b296bc9fbbf2ad1cb586ad83b56f30` — fixture Unity/Windows, regression và launcher mode.
- Báo cáo được ghi trong commit tài liệu tiếp theo. Worktree nhánh Hàn Vụ sạch khi chốt. Checkout 0.1.7 giữ nguyên trạng thái có sẵn.
- Còn nghiệm thu nhịp chơi ở thông số thật, rà thoại và danh xưng, thay placeholder bằng UI/nhân vật do người dùng phát triển, và chốt mạch truyện tiếp theo sau manh mối. Phạm vi dừng ở đây. PR animation #12 tách riêng.
