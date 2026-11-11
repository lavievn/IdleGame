# Hàn Vụ thôn — Thoại, manh mối và HUD

Đợt triển khai [Issue #15](https://github.com/lavievn/IdleGame/issues/15) tiếp tục trên `feature/story-origin-thanhkhe-foundation` từ `4c86b766fd23bb865f36db01525e288552713777`. Quy tắc khám phá tự do từ [Issue #14](https://github.com/lavievn/IdleGame/issues/14) vẫn là đường đi của bản này.

## Phần chơi được

- Lần đầu vào Hàn Vụ, sáu câu thoại luân phiên giữa người nuôi dưỡng và Hero. Cơn ho được thể hiện bằng lời thoại. Người nuôi vẫn lo Hero lạnh, Hero chủ động đề nghị tìm lá bạc, và bạn thuở nhỏ được nhắc bằng tên trước khi đi rừng. Tên, vai xưng hô và lời nhắc dùng danh tính đã lưu.
- Có thể đọc từng câu, lùi một câu, đóng hoặc rời làng. **Nhận lời** và **Để sau** xuất hiện ở cuối cuộc thoại mở đầu. Trong làng, **Nhật ký → Xem lại mở đầu** phát lại từ câu đầu. Khi mang thuốc về, cuộc thoại nhắc chuyến lên núi của người bạn trước nút **Sắc thuốc**. Đoạn sau khi sắc thuốc ghi sự cải thiện tạm thời và lời hẹn hỏi thăm lúc thuận đường.
- Cô Hồn Lâm có nút **Người đốn củi** ở phía dưới bên trái khung cảnh ngay từ lần tới đầu. Đó là điểm tương tác dạng nhãn tạm. Chỉ bấm trò chuyện mới ghi lời kể. Đi đủ 40% quãng khám phá giữa các wave cho thấy nút dây trên cành thông. Qua wave hợp lệ thứ hai có thể thấy mảnh giấy dưới đá. Cả hai dấu đều độc lập với nhận nhiệm vụ và loot thường.
- Khi người chơi đang tìm dấu vết, **Xem dấu vết** ở Cô Hồn Lâm ghi vật chứng mảnh giấy để tiếp tục tuyến. Dải vải gần chỗ sói đầu đàn ở Ma Tùng Sâm được ghi sau đợt Elite hợp lệ. Nhật ký tách điều quan sát, lời kể và suy đoán. Dấu nút đôi có thể thuộc người đi rừng khác.
- **Nhật ký** và **Ẩn gợi ý** nằm dưới thẻ Hero, phía trên cụm menu hệ thống. Bảng thoại hiện từng lượt lời hoặc từng mục nhật ký. Thẻ chỉ hiện cho Hero đang sống và triển khai, xếp sát từ phải qua trái. Khi số Hero thay đổi, HUD dùng lại vị trí trống.

## Save và phạm vi

Origin Save v3 lưu các mốc đã nghe chuyến đi, đã gặp người đốn củi và hai dấu đã quan sát. Save v1/v2 giữ danh tính, vị trí, nhiệm vụ, lựa chọn kỳ ngộ và gợi ý đã lưu. Các mốc mới bắt đầu ở trạng thái chưa quan sát. Khi load giữa cuộc thoại, bảng mở từ đầu nếu người chơi chọn xem lại. Những câu đã ghi thành mục nhật ký vẫn giữ và chỉ xuất hiện một lần.

Thoại, nhật ký và khám phá dùng HUD và nhịp chạy hiện có. Mọi yêu cầu travel vẫn do bản đồ quyết định. Dấu vết chỉ đọc tiến độ di chuyển hoặc wave đã chấp nhận và không thay đổi bảng loot, đội hình hay AI.

## Kiểm thử

| Môi trường | Kết quả |
| --- | --- |
| Unity 6000.5.0f1, MainGame Play Mode | PASS 7/7 nhóm Hàn Vụ. Sáu lượt lời đúng thứ tự và đúng danh tính, bỏ qua hoặc xem lại, load giữa thoại, di chuyển trước khi nhận, người đốn củi tự nguyện, dấu vết theo di chuyển và wave, Retry, không phát lại vật chứng Elite. Save v1/v2 và save thiếu Origin đều tải được. |
| Unity MainGame, Development 0.1.7 | PASS 5/5 nhóm hồi quy tập trung, gồm loot, so sánh và trang bị, Forge, Võ học, Save/Load, Retry và Thiên Quan. |
| Windows Development Player, Mono2x | Build thành công. 0 lỗi, 58 cảnh báo. Bản build từ commit runtime và test cuối cùng ở dưới. |
| Windows Development Player, Direct3D 11 WARP | PASS 7/7 nhóm, exit 0. Có kiểm tra hình học vùng bấm và gọi handler ở 600×338, 800×450, 1150×647. 1/2/3 Hero, Hero gục, hồi sinh và rút khỏi đội hình đều qua. Lượt này không tái hiện lỗi thoát UnityPlayer.dll từng được ghi nhận. |
| Chuột Windows native và chơi toàn tuyến ở nhịp mặc định | NOT RUN. Khi mở cửa sổ Player để thử chuột, hộp thoại Windows Firewall che giao diện. Profile kiểm thử được đóng riêng. Các lượt Play Mode/Player tự động dùng handler và fixture tăng tốc giao tranh. |

Bằng chứng trên máy thử nghiệm:

- [Kết quả Windows Player dạng JSON](<D:/IdleGame/Builds/HanVuNarrativePolish/profiles/idlegame-v10-5cf39a8651734976ba4c4fcb339b1b3e/evidence/player-result.json>)
- Ảnh Windows Player [600×338](<D:/IdleGame/Builds/HanVuNarrativePolish/profiles/idlegame-v10-5cf39a8651734976ba4c4fcb339b1b3e/evidence/narrative-hud-600.png>), [800×450](<D:/IdleGame/Builds/HanVuNarrativePolish/profiles/idlegame-v10-5cf39a8651734976ba4c4fcb339b1b3e/evidence/narrative-hud-800.png>), [1150×647](<D:/IdleGame/Builds/HanVuNarrativePolish/profiles/idlegame-v10-5cf39a8651734976ba4c4fcb339b1b3e/evidence/narrative-hud-1150.png>), [thoại mở đầu](<D:/IdleGame/Builds/HanVuNarrativePolish/profiles/idlegame-v10-5cf39a8651734976ba4c4fcb339b1b3e/evidence/narrative-opening-600.png>), [người đốn củi](<D:/IdleGame/Builds/HanVuNarrativePolish/profiles/idlegame-v10-5cf39a8651734976ba4c4fcb339b1b3e/evidence/narrative-woodcutter.png>) và [đội hình ba Hero](<D:/IdleGame/Builds/HanVuNarrativePolish/profiles/idlegame-v10-5cf39a8651734976ba4c4fcb339b1b3e/evidence/narrative-hud-three-heroes.png>).
- Log [MainGame Hàn Vụ](<D:/IdleGame/Logs/Unity-MainGameHanVu-20261111-142201.log>), [MainGame Development 0.1.7](<D:/IdleGame/Logs/Unity-MainGame017-20261111-142330.log>) và [Windows build](<D:/IdleGame/Builds/HanVuNarrativePolish/build.log>).

### Nghiệm thu trực tiếp

Mở [Play-HanVu-Narrative-Isolated.cmd](<D:/IdleGame/Builds/HanVuNarrativePolish/build/Play-HanVu-Narrative-Isolated.cmd>). Launcher chạy Windows Development Player qua WARP với thư mục save riêng. Player nằm tại `D:\IdleGame\Builds\HanVuNarrativePolish\build\IdleGameV10.exe`.

1. Bấm **New Game**. Đọc, lùi và đóng thoại mở đầu. Mở bản đồ rồi đi Cô Hồn Lâm trước khi nhận nhiệm vụ.
2. Kiểm tra **Nhật ký** và **Ẩn gợi ý** dưới thẻ Hero tại 600×338, 800×450 và 1150×647. Bấm **Người đốn củi**, đọc hết lượt lời, rồi kiểm tra mục lời kể và manh mối sau khi đi và qua hai wave.
3. Trở về Hàn Vụ, mở **Nhật ký → Xem lại mở đầu** và nhận lời. Chơi năm wave hợp lệ, mang thuốc về, đọc thoại trước và sau khi sắc thuốc. Tiếp tục dấu vết và kiểm tra Nhật ký sau khi gặp sói đầu đàn.
4. Lưu giữa thoại, mở lại và xem lại từ đầu. So sánh danh tính, tiến độ nhiệm vụ và các mục Nhật ký đã ghi.

## Tệp và commit

- Nội dung và state gồm `Assets/Script/HanVuNarrative.cs`, `HanVuNarrative.cs.meta`, `HanVuOrigin.cs`, `GameManager.cs`.
- HUD gồm `Assets/Script/HudW3View.cs`, `UIManager.HudW3.cs` và `UIManager.StoryOrigin.cs`.
- Kiểm thử gồm `Assets/Script/HanVuOriginSmoke.cs`, `MainGameFoundationProbe.cs`, `WindowsDevelopmentAcceptanceV10.cs` và `Tests~/run.sh`.
- Báo cáo này nằm tại `Docs/automation-reports/HAN_VU_NARRATIVE_UI_POLISH_REPORT.md`.

- `e7e678fc886cfbea1bd74ac38fa9e0e1b8d338c2` — thoại, manh mối, trạng thái lưu và HUD.
- `ae39bd1fc0fb5cf299a67e86e1363ad04f980a5e` — kiểm thử thoại, khám phá, tương thích save và HUD.
- `685a1b2e8e1098c6fbc81f867fd04265eae6c6b8` — sửa tràn chữ, thời điểm ghi lời kể và căn các nút theo chiều cao thẻ hiện hành.
- Commit cuối của đợt này là commit chỉ chứa báo cáo. Có thể lấy SHA bằng `git log -1 --format=%H -- Docs/automation-reports/HAN_VU_NARRATIVE_UI_POLISH_REPORT.md` hoặc xem [PR #13](https://github.com/lavievn/IdleGame/pull/13/commits).

Git status trước khi bàn giao còn hai thay đổi cục bộ tại font fallback TMP và ProjectSettings, cùng 15 tệp .meta ở Lake, Sea và Steppe đã có từ trước. Các tệp cục bộ này nằm ngoài các commit Issue #15.
