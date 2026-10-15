
## Chế độ căn chỉnh trong Play Mode — 15/10/2026

**Khác với bản chỉnh Inspector chỉ có kích thước:** giờ có cửa sổ công cụ `Tools > Bach Luyen Tien Do > Opening Layout (Play Mode)`. Bấm Play → bật chế độ chỉnh → chọn đối tượng → kéo `RectTransform` trong Scene View → bấm **LƯU BỐ CỤC VÀO ASSET** trước khi Stop. Dữ liệu bền vững trong `Assets/Resources/OpeningArtTheme.asset`, dùng tọa độ reference 800×450; preview bình thường dùng lại vị trí đó và mây trôi quanh nó.

- Công cụ dành riêng cho Unity Editor/Play Mode, không thêm nút debug vào game Windows.
- Bao gồm sky, 5 núi/mây, 2 splash L/R, logo và 4 nút chính; chưa bao gồm phần Settings và xác nhận.
- Lúc chỉnh, cảnh/menu/splash được hiển thị đầy đủ; tạm dừng các vị trí animation. Không dùng để đổi camera/gameplay.
- Xem chi tiết [Opening_PlayMode_Layout_0.0.53.md](Opening_PlayMode_Layout_0.0.53.md).

---

## Chỉnh sửa opening ngày 15/10/2026 — ưu tiên cao nhất

**Thứ tự hiển thị hiện tại đã thay thế thời gian 0,65s trong các mô tả cũ:**

| Thời gian từ lúc mở menu | Trạng thái |
|---|---|
| 0,0s | Màn hình đen hoàn toàn, kể cả vùng menu/HUD |
| 0,0–1,8s | Lớp đen mờ dần, lộ nền video, trời, núi, các lớp mây |
| 1,8–4,8s | Giữ toàn bộ tiên cảnh, **chưa có nút hay logo** |
| 4,8–6,3s | **Logo + bốn nút** fade in trong 1,5s |
| Sau 6,3s | Menu tương tác bình thường |
| 27s | Hai khung splash bên trái/phải bắt đầu fade in, giữ quy tắc cũ |

- `OpeningCinematicUI.cs`: lớp `Opening Black Curtain - Full Window` là sibling của lớp cảnh/menu, phủ tràn Canvas, không nhận raycast. `BlackCurtainAlphaAt()` và `MenuAlphaAt()` dùng nội suy mượt, tính bằng `Time.unscaledDeltaTime`; quay về màn hình chính bằng System Menu sẽ phát lại từ đầu.
- `StartMenuUI.cs`: khóa input native/custom click trong cả giai đoạn chưa có nút; dùng font **màu trắng** cho các nút.
- **Logo `Bách Luyện Tiên Đồ`:** file gốc tại `Assets/Resources/OpeningTitleLogo.png` được **thay bằng nét trắng, giữ nguyên alpha**, để Unity `Image.color` tint đỏ sẫm `#4A1214` (RGB 74/18/20). Kích thước hiển thị tăng khoảng 2 lần nhưng không tràn cửa sổ, menu dịch xuống một chút tránh đè logo. `.meta` và GUID Sprite giữ nguyên.
- **Không sửa:** splash xuất hiện từ giây 27, random PNG/JPG/MP4 hai bên, feather, video nền và các luồng Save/chiến đấu.

**Nghiệm thu cần kiểm tra:** khởi động màn đen thật sự; 1,8s sáng cảnh; nút không hiện và không bấm được trước 4,8s; 6,3s nút hiển thị đủ, chữ trắng; logo đỏ sẫm lớn hơn; mở Cài đặt/Chơi mới/Tiếp tục; quay về title diễn lại trình tự; kiểm tra ở 600/800/1150. Chưa có chạy Play Mode/Windows trong môi trường trợ lý.

---

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

## Quyết định cuối ngày 14/10/2026 — thay thiết kế splash trước đó

Các đoạn mô tả hai splash art trái/phải, trình chiếu ngẫu nhiên và zoom tự động bên trên là **lịch sử đã được thay thế**. Trên nhánh mới nhất chỉ còn **một video splash trong khung đứng yên, fade in một lần ở giây 27**, còn zoom/cắt dựng bên trong tệp video của tác giả. Nền có thể thêm `opening.mp4` dưới tất cả mây/núi. Logo chữ **Bách Luyện Tiên Đồ** đã commit ở `Assets/Resources/OpeningTitleLogo.png` và đưa lên trên giữa. Xem [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md) để lấy đường dẫn/định dạng đúng.
