# Opening cinematic 0.0.53 — prototype bằng khối màu / asset thay thế

**Trạng thái:** đã bổ sung mã và tài nguyên mẫu trên `feature/0.0.53`; **chưa biên dịch hoặc nghiệm thu Play Mode / bản Windows trong môi trường trợ lý**. Chưa gộp `main`. Không dùng ảnh ghép có watermark hoặc phông xanh do người phát triển gửi làm asset chính thức.

## 1. Kịch bản hình ảnh

- Sau logo Unity, **lớp trời, núi và mây che kín viewport gameplay**; những khối màu có tên biểu thị nơi cần gắn ảnh.
- Menu hiện mềm sau khoảng **0,65 giây**, chuyển độ rõ trong **1,5 giây**: **Tiếp tục → Chơi mới → Cài đặt → Thoát** (trên xuống). Chưa có tên game, chừa ô `gameTitle` tạm thời ẩn khi chưa gán Sprite.
- Mây trước/sau trôi chậm bằng sin/parallax không tạo mới texture mỗi frame, núi xa dao động rất nhỏ.
- Sau **27 giây kể từ khi opening được hiển thị**, splash nhân vật hai bên xuất hiện dần và trượt vào vị trí trong **2,2 giây**, sau đó tiếp tục treo cảnh. Cue là thời gian dự kiến phù hợp phần nhạc mạnh khoảng 25–30 giây, **chưa đồng bộ waveform/audio DSP**.
- Chơi mới/Tiếp tục vẫn phải qua xác nhận Save như phiên bản trước. **Chỉ khi xác nhận thành công** mới phát chuyển cảnh 2,7 giây. Menu mờ trước, mây/splash dạt ra 2 bên, các lớp núi/trời giãn tỉ lệ tạo ảo giác `hyper zoom`, lớp phủ mờ và lộ gameplay thật.
- Gameplay đã được khởi tạo bên dưới nhưng **tạm dừng logic bằng thời gian game** khi chuyển; animation sử dụng `Time.unscaledDeltaTime`. Kết thúc thì đóng overlay và resume. **Không scale Canvas logic, hero, quái, hay camera combat**, tránh lệch tọa độ khi resize.
- Nhạc `opening.ogg` vẫn do `MapBgmController` quản lý; khi overlay tắt, chuyển sang BGM theo chủ đề đã có, crossfade 10 giây.

## 2. Gán ảnh trong Unity — không chỉnh code

1. Pull nhánh `feature/0.0.53`, mở `Assets/Resources/OpeningArtTheme.asset` trong cửa sổ Project rồi chọn tài nguyên đó. Inspector có **9 ô Sprite**:
   - `sky` — bầu trời/phông sáng phủ hết màn hình.
   - `farMountains` — lớp núi xa.
   - `nearMountains` — lớp núi gần, có thể là một mảng PNG trong suốt.
   - `backClouds` — mây/sương lưng chừng.
   - `leftClouds` — mây tiền cảnh trái.
   - `rightClouds` — mây tiền cảnh phải.
   - `splashLeft` — nhân vật/splash bên trái.
   - `splashRight` — nhân vật/splash bên phải.
   - `gameTitle` — **để trống** cho đến khi đặt tên game.
2. Nhập PNG ở chế độ **Sprite (2D and UI)**; dùng PNG có alpha cho mây, núi và splash. Kéo Sprite từ Project vào từng ô của asset. Chỉ cần lưu asset; runtime khi khởi động sẽ tự đọc.
3. Nếu slot trống, lớp đó được hiển thị bằng **khối màu kèm tên chức năng**. Nếu slot có Sprite, ảnh được dùng thay khối màu và nhãn placeholder của lớp đó ẩn. Không phải chỉnh trong Hierarchy đang Play Mode.
4. Các đối tượng động được đặt tên `Opening Cinematic - Placeholder Layers`, bên trong `01 SKY`, `02 FAR MOUNTAINS`, `03 NEAR MOUNTAINS`, `04 BACK CLOUDS`, `05 FRONT CLOUDS LEFT`, `06 FRONT CLOUDS RIGHT`, `07 SPLASH LEFT`, `08 SPLASH RIGHT`, `09 GAME TITLE`. Có thể kiểm tra vị trí RectTransform trong Play Mode; để thay lâu dài, **gán bằng OpeningArtTheme.asset**, vì các đối tượng runtime tự tạo sẽ không lưu các thay đổi Inspector sau khi thoát Play.
5. Sprite hiện vẽ dưới dạng `Image` thông thường, có thể bị kéo theo kích thước RectTransform của khối. Khi bạn gửi ảnh thật, cần chỉnh tỉ lệ, `preserveAspect`, hình dạng cloud và vùng an toàn splash theo asset thực để tránh méo hoặc cắt ảnh.

## 3. Mốc chuyển động chỉnh được trong mã

| Mốc | Giá trị mẫu | Địa điểm |
| --- | --- | --- |
| Bắt đầu hiện 4 nút | 0,65s | `OpeningCinematicUI.Update` |
| Fade-in 4 nút | 1,5s | `MenuRevealSeconds` |
| Splash xuất hiện | 27s | `ShowcaseCueSeconds` |
| Fade-in splash | 2,2s | `UpdateShowcase` |
| Zoom/reveal vào gameplay | 2,7s | `EntrySeconds` |
| Chuyển từ opening sang BGM map | 10s | `MapBgmController` |

Các số là **prototype** để xem nhịp và sẽ điều chỉnh sau khi thay artwork. Chưa có timeline âm thanh trong asset hoặc editor chuyên dụng.

## 4. Nút bấm và luồng Save

- **Tiếp tục**: dùng logic `GameManager.OnContinueClicked()`; không có bản lưu thì vô hiệu hóa nút. Popup **Có/Không** vẫn tồn tại và quyết định có thật sự tải dữ liệu không.
- **Chơi mới**: dùng `OnNewGameClicked()`; xác nhận đặt lại **AutoSave**, giữ hai ô Manual Save. Nếu xác nhận thất bại thì không chuyển cảnh.
- **Cài đặt**: trang cài đặt ngay trong opening, hiện hỗ trợ âm lượng nhạc giảm/tăng 10%, bật/tắt, Quay lại. Không xóa dữ liệu người chơi. Các cài đặt đồ họa chuyên sâu sẽ bổ sung khi cần.
- **Thoát**: gọi `Application.Quit()` (không thoát trong Unity Editor).
- Khi Load Save trong lúc đang gameplay, không ép diễn lại cinematic. Nếu trở lại màn hình chờ, overlay được khôi phục.

## 5. Những thứ chưa làm

- Chưa có artwork cuối, không có tên game, hiệu ứng chữ/particle, camera 3D thật, nút setting nâng cao.
- Chưa có chạy Play Mode, kiểm tra Windows native transparency, độ mượt trên máy sử dụng CMP 40HX, hay GPU/memory benchmark.
- Không dùng video đã render sẵn, không nhập ảnh minh họa trong cuộc hội thoại làm asset dự án.

## 6. Checklist nghiệm thu 0.0.53

1. Mở Play Mode tại scene `Assets/Scenes/MainGame.unity`; kiểm tra Console không lỗi compile/null.
2. Từ logo Unity đến menu: thấy sky/núi/mây toàn màn hình **không lộ gameplay**, bốn nút hiện đúng thứ tự. Nút Tiếp tục mờ/không click nếu chưa có Save.
3. Đợi ít nhất 30s: mây vẫn trôi, hai splash xuất hiện và không che hết 4 nút. Nghe nhạc opening nếu AudioCatalog có sẵn.
4. Bấm **Cài đặt**, chỉnh âm lượng, quay lại; thử Thoát trong bản Windows, tránh nhầm với nút Menu hệ thống trong gameplay.
5. Bấm **Chơi mới** rồi **Không**: quay về menu, không xóa AutoSave; bấm **Có**: game xuất hiện qua chuyển mây 2,7s. Không mất/nhân đôi wave, không sinh quái trước lúc vào game.
6. Bấm **Tiếp tục** với Save có thật: đúng cấp/EXP/map/đồ, không bị tiếp tục ở dữ liệu cũ; nhạc crossfade sau khi transition xong.
7. Kiểm tra kích thước cửa sổ **600 / 800 / 1150** và bản Windows overlay: mây phủ đúng vùng chơi, Menu/các nút nhận click, không xuyên click và không làm sai camera/combat tọa độ.
8. Gán thử **một ảnh sạch** vào slot `sky`, rồi vào lại Play Mode: slot chuyển từ khối màu sang sprite. Để trống các slot khác vẫn hoạt động.
9. Mở kho/rèn khi đã vào gameplay: âm lượng, HUD, camera, combat và save vẫn hoạt động theo 0.0.53 trước thay đổi.

## 7. Tệp thêm/sửa

- `Assets/Script/OpeningCinematicUI.cs`: layer màu + hiệu ứng theo thời gian thực và reveal.
- `Assets/Script/OpeningArtTheme.cs` + `Assets/Resources/OpeningArtTheme.asset`: cấu hình tham chiếu ảnh có thể thay thế.
- `Assets/Script/StartMenuUI.cs`: 4 nút chính, trang Cài đặt và hộp xác nhận.
- `Assets/Script/GameManager.cs`: chỉ khi bắt đầu từ opening mới chờ transition rồi tiếp tục gameplay.
- `Assets/Script/UIManager.cs`: menu toàn màn hình, ưu tiên click, giữ bố cục ở các kích thước.
- `Tests~/OpeningCinematicRegression.cs`: bài thử cấu trúc trên Unity stub (không thay thế Play Mode).

## 8. Splash art thay đổi ngẫu nhiên (bổ sung)

Thay vì chỉ có hai ảnh cố định, opening đã có module `OpeningSplashShowcase.cs`. Người phát triển thêm PNG/JPG hoặc MP4 vào `Assets/Resources/OpeningSplashArt/`, Unity lập danh mục và lựa chọn hai bên sau mốc 27 giây, zoom in/out có nhịp nhanh chậm và crossfade. Hai ô `splashLeft`/`splashRight` trong `OpeningArtTheme.asset` trở thành ảnh dự phòng nếu kho trống. **Xem [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md)** để có quy trình nhập tài nguyên, giới hạn video, checklist Windows. Các mô tả hai ảnh cố định ở các phần lịch sử trên dành cho phiên bản trước khi thêm kho.
