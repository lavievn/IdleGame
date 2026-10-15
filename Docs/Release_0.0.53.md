# 0.0.53 — Điều chỉnh âm lượng nhạc nền (14/10/2026)

**Nhánh:** `feature/0.0.53` tách từ `main` tại commit `9caaf09ce5176ced487b6db6f47289fd2d737c97`. **Chưa gộp main**; cần thử trên Unity Editor và bản Windows.

## Tài nguyên nhạc nền đã xác nhận trên main

- `Assets/BGM`: 19 bài BGM trong năm nhóm + `opening.ogg` = **20 bản âm thanh**, có đầy đủ `.meta`.
- `Assets/Resources/MapBgmCatalog.asset` đã được commit, tham chiếu `opening` và nhóm 4 / 4 / 4 / 3 / 4 bài.
- Nhóm BGM4 hiện có **3 bài**; vẫn chờ bài thứ tư. Không thay đổi ánh xạ chủ đề hoặc mã chuyển âm 10 giây.
- Không có bằng chứng đã kiểm thử nghe được trong Windows build của thay đổi 0.0.53; sự tồn tại của clip và catalog không tự xác nhận phát đúng.

## Chức năng mới

- Thêm nút **Âm lượng** cùng kiểu với nút **Kho đồ** (62 × 24) trên hàng công cụ phía trên bên phải, **ngay bên phải Kho đồ**.
- Để không đè nút Chỉ số, DEVB, Tạm dừng: Kho đồ dịch sang trái một ô; Âm lượng lấy vị trí Kho đồ cũ. Các nút khác không đổi.
- Nhấp nút Âm lượng mở/đóng bảng nhỏ có **giá trị %**, **−10%**, **+10%**, và **Tắt nhạc / Bật nhạc**. Mức 0–100%, mặc định 60%. Nút âm lượng không nằm trong System Menu; có thể chuyển sau.
- Nút và bảng ẩn khi mở kho, các cửa sổ modal, màn hình bắt đầu hoặc Game Over. Bảng không bắt gameplay phải dừng.
- Dùng `PlayerPrefs` khóa `IdleGame.BgmVolume` lưu thiết lập máy, **không** gắn vào file Save và không bị New Game xóa. Mở lại bảng có % thực tế; tắt/bật không mất mức vừa sử dụng trong phiên.
- Âm lượng tác động **toàn bộ nhạc BGM kể cả opening**, áp dụng ngay với bài đang phát **và trong pha chuyển âm 10 giây**; không restart nhạc, không thay đổi thể loại bản đồ.
- **SFX, voice over chưa có**. Bảng hiện chỉ điều khiển âm lượng nhạc, không phải master volume của mọi âm thanh tương lai.

## Tệp thay đổi

- `Assets/Script/MusicVolumePanelUI.cs` và `.meta`: UI riêng dùng `CustomInteractable`, dễ chuyển sang System Menu.
- `Assets/Script/UIManager.cs`: nút 62 × 24, mở bảng, phối hợp hiển thị, bố cục khi đổi kích thước.
- `Assets/Script/MapBgmController.cs`: API đọc/đặt âm lượng, lưu `PlayerPrefs`, áp dụng ngay cho 2 kênh crossfade.
- `ProjectSettings/ProjectSettings.asset`: `bundleVersion: 0.0.53`.
- README/tài liệu tiến trình: cập nhật trạng thái nhạc đã push và mốc mới.

## Kiểm thử Unity/Windows cần người phát triển thực hiện

1. Pull `feature/0.0.53`, đợi Unity nhập rồi mở Play Mode, kiểm tra Console không có lỗi biên dịch.
2. Tại màn hình Chơi mới/Tiếp tục, opening phát như bản trước. Vào game kiểm tra 4 nút Kho đồ → Âm lượng → Chỉ số → DEVB không đè nhau, vẫn nhìn được Tạm dừng (thử chiều rộng 600/800/1150).
3. Bấm **Âm lượng** mở/đóng bảng; **−/+/Tắt nhạc/Bật nhạc** đều nhận click trong Editor và Windows overlay, ngay cả sau đổi kích thước cửa sổ.
4. Khi nhạc đang phát, bấm xuống 30%, 0%, rồi lên 60%: thay đổi ngay, không cần đợi chuyển bài; không ngắt hoặc phát lại từ đầu.
5. Khi gần kết thúc bài hoặc chuyển sang chủ đề map khác, chỉnh âm lượng **trong lúc chuyển âm 10 giây**: cả hai kênh đổi mức hợp lý, không bật nhạc bất ngờ sau khi tắt.
6. Đóng/mở game: mức volume giữ nguyên. Tạo nhân vật mới, Load save cũ: thiết lập âm lượng **không reset**.
7. Mở Kho đồ/System Menu/Game Over: bảng và nút âm lượng ẩn hoặc bảng tự đóng; không gây chồng click với UI kho/rèn.
8. Không làm thay đổi logic trang bị, lưu nhân vật, chiến đấu, BGM1–5 hoặc opening.

**Giới hạn xác minh:** Không có Unity Editor/Windows Play Mode trong môi trường trợ lý; kiểm tra tại đây mới là mã/tài nguyên trên GitHub và rà soát tĩnh.

## Bổ sung cuối bản 0.0.53 — cảnh mở đầu (đang kiểm thử)

- **Không dùng video AE:** `OpeningCinematicUI` dựng runtime 9 lớp hình làm placeholder. Mỗi lớp có vị trí, màu và nhãn; người phát triển gán Sprite qua `Assets/Resources/OpeningArtTheme.asset` mà không cần chỉnh code.
- Menu có 4 nút **Tiếp tục / Chơi mới / Cài đặt / Thoát**. Trang Cài đặt trước mắt cho đổi âm lượng nhạc và quay lại. Popup Có/Không vẫn được giữ để không phá luồng Save/Continue.
- Mây bay liên tục; sau **27 giây** thì hai placeholder splash art hiện. Khi xác nhận vào game thành công: menu biến mất, mây/splash dạt ra và lớp núi giãn trong **2,7 giây**, gameplay bên dưới lộ ra rồi mới tiếp tục tick combat. Nhạc opening → BGM vẫn chuyển âm qua bộ quản lý âm thanh hiện tại.
- Không scale gameplay, camera combat, Ground, hero hay đạn trong hiệu ứng này. Không nhập ảnh có watermark/chroma từ bản mockup.
- Đã cập nhật stub và hồi quy menu 4 nút, thêm kiểm thử cấu trúc opening; **chưa chạy thành công trong Unity Editor/Windows**.
- Quy trình gán Sprite và checklist: [Docs/Opening_0.0.53.md](Opening_0.0.53.md).

## Bổ sung màn Cài đặt hai cột và quay lại tiêu đề (14/10/2026)

- **Opening → Cài đặt:** bố cục hai hàng căn giữa; cột trái là `Kích thước màn hình` / `Âm thanh`, bên phải có 3 lựa chọn **600 / 800 / 1150** (chọn độc quyền) và **thanh kéo 0–100%**, kèm phần trăm. Hai nút dưới cùng là **Khôi phục mặc định** / **Quay lại**.
- **Khôi phục mặc định:** đặt cửa sổ về **800×450** và nhạc về **60%** (mốc thử nghiệm), không đặt lại Save/nhân vật. Trang Cài đặt vẫn mở sau khi đổi độ phân giải.
- **Thanh âm lượng:** dùng một `RectTransform` riêng để nhận kéo; Editor xử lý chuột trong `UIManager`, bản Windows nhận kéo qua `TransparentWindow` mà không kích hoạt kéo cửa sổ. Dùng cùng `MapBgmController.SetVolume` đã lưu bằng `PlayerPrefs`; ảnh thanh, phần đầy và tay kéo hiện đều là khối màu.
- **System Menu trong gameplay:** thêm nút **Quay lại màn hình chính**, gọi `GameManager.ReturnToMainMenu()`. Nếu còn đang chơi, cố lưu tiến trình về **AutoSave trước khi đóng trận**; nếu không thể ghi file thì không thoát trận. Sau lưu thành công, hủy wave đang chờ, xóa quái và Hero trong map, dọn nhân vật thử, mở lại menu cùng hoạt ảnh mây và `opening.ogg` (không tải lại Scene). **Tiếp tục** dùng AutoSave mới nhất.
- **Hệ thống cũ:** các nút đổi kích thước trong System Menu vẫn dùng hàm resize chung. Không thay đổi dữ liệu trang bị, võ học hoặc phép tính sát thương.
- **Kiểm thử bắt buộc trong Unity:** thử kéo slider tại 0/50/100%, kéo ra ngoài thanh rồi thả chuột, thử Windows overlay xem có kéo nhầm cả cửa sổ không; đổi 3 kích thước, kiểm tra hai cột không chồng; nhấn mặc định/Quay lại; ở trận đấu mở System Menu → Quay lại màn hình chính → Tiếp tục, đối chiếu map/EXP/kho đồ/wave, kiểm tra không sinh quái dư và nhạc opening quay lại.
- **Giới hạn:** chưa chạy được Unity Editor/Windows Build trong môi trường thực thi này; việc commit và rà mã không chứng minh thao tác kéo hoặc ghi Save đã hoạt động đúng ở máy người phát triển.

## Bổ sung kho splash art (đang kiểm thử)

- Thêm `Assets/Resources/OpeningSplashArt/` và `OpeningSplashCatalog.asset`, cùng công cụ Editor `Tools → IdleGame → Rebuild Opening Splash Catalog`. Ảnh PNG/JPG và video MP4 hợp lệ sẽ được phát hiện tự động sau khi Unity import.
- Kể từ mốc 27 giây, hai vị trí splash dùng slideshow ngẫu nhiên (thường khoảng 4,8–7,4 giây/lượt), zoom in/out với gia tốc, kéo nhẹ ngang và chuyển cảnh mờ chồng 0,7s; tránh lặp tức thì khi có nhiều ảnh.
- Video hiển thị qua một VideoPlayer duy nhất vào RenderTexture 512×896, tắt tiếng, không chạy giải mã trong gameplay. Kho trống giữ hai ô màu cũ. Nếu chỉ có video, hai bên luân phiên để giới hạn tài nguyên; không dùng video tham chiếu trong build.
- Tài liệu/cảnh báo dung lượng/codec, hướng dẫn gán assets và checklist: [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md).
- **Chưa xác minh với Unity Play Mode / Windows / CMP 40HX**; chưa gộp vào `main`.

## Cập nhật cuối 0.0.53 — Logo Bách Luyện Tiên Đồ và hai video riêng biệt

- **Tài nguyên chính thức đã commit:** `Assets/Resources/OpeningTitleLogo.png` (tách nền từ ảnh người phát triển gửi, không kèm tệp font). Logo căn trên giữa, thay dòng chữ giới thiệu cũ, luôn độc lập với trạng thái Chơi mới/Tiếp tục.
- **Video nền** `OpeningBackgroundVideo.cs`: một tệp tùy chọn `Assets/StreamingAssets/OpeningBackground/opening.mp4` đặt sau toàn bộ mây/núi và menu, phủ viewport, tắt tiếng, lặp; không có video thì sky màu tiếp tục làm dự phòng.
- **Video splash** `OpeningHeroSplashVideo.cs`: một khung cố định, không pan/zoom/crossfade/slideshow. Đọc `Assets/StreamingAssets/OpeningSplash/splash.mp4`; ở giây **27** chỉ fade in **một lần trong 1,4 giây**, sau đó video chạy/lặp tại vị trí đó. Không có video thì dùng khối màu nhãn. Phần dựng hiệu ứng nhân vật do video gốc đảm nhiệm.
- **Đã bỏ mã cũ:** `OpeningSplashShowcase`, `OpeningSplashCatalog`, importer và test random slideshow không còn sử dụng. Hai MP4 **chưa được thêm** — bạn tự chép vào StreamingAssets để xem thử.
- Nhạc `opening.ogg`, 4 nút, trang Cài đặt, lưu Save, mây trôi và cloud reveal lúc vào gameplay giữ nguyên.
- Xem [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md). **Chưa biên dịch hoặc nghe/xem trên Unity Editor/Windows** trong môi trường trợ lý.

## Cập nhật mới nhất — Splash hai bên, ngẫu nhiên, không tự pan/zoom

- **Thay cho một khung cố định ở giữa:** Hai khung `07 SPLASH LEFT - OVERSCAN` và `08 SPLASH RIGHT - OVERSCAN`, mỗi khung rộng 62% và cao 76% vùng opening, căn sát đáy, tâm lệch ±56% bề ngang. Chỉ tràn ra ngoài mép trái/phải, không vượt đỉnh hoặc đáy cửa sổ. Đứng yên, chỉ fade in một lần tại giây 27 (1,4s).
- **Ngẫu nhiên khi mở màn hình chờ:** Quét `Assets/StreamingAssets/OpeningSplashArt` (PNG/JPG/JPEG/MP4); mỗi bên chọn một file, tránh trùng nhau nếu có ≥2 file. Không đổi cảnh liên tục. Nếu thư mục mới rỗng thì dùng video cũ `OpeningSplash/splash.mp4` hoặc hai khối màu.
- **Viền mềm mây/núi:** shader `OpeningSoftEdge.shader` và `OpeningEdgeFeather.cs`, mặc định **48 đơn vị UI**, có thể điều chỉnh `OpeningArtTheme.asset → Edge Feather Pixels`. Chỉ tác động năm layer núi/mây, không đổi hình nguồn.
- **Đã thêm test cấu trúc:** hai khung riêng, đúng chiều, không tự dịch chuyển, alpha chỉ tăng từ giây 27; chạy trên Unity doubles, **chưa thực thi thực tế trong Editor/Windows**.
- Đường dẫn và lưu ý nặng GPU khi phát cả 3 video: [Opening_SplashArt_0.0.53.md](Opening_SplashArt_0.0.53.md).

## 15/10/2026 — Màn đen mở đầu, menu xuất hiện trễ, logo đỏ sẫm

- Lớp curtain `Opening Black Curtain - Full Window` phủ đen toàn bộ Canvas khi vào title. Fade đen → cảnh nền mây núi/video hoàn chỉnh trong **1,8s**.
- Giữ cảnh không có menu thêm **3,0s**, từ mốc **4,8s** logo và các nút mới bắt đầu fade in **1,5s**, đầy đủ và click được sau **6,3s**.
- Logo ảnh gốc đã được thay thực sự bằng PNG **nét trắng/alpha nguyên bản** (không dùng lại chữ đen), Unity tô `#4A1214`. Điều chỉnh logo lớn gần **2×** và hạ hàng nút để không chồng chữ.
- Text nút màu **trắng** và nút chưa hiện **bị khóa tương tác** với cả native Windows overlay lẫn uGUI. Quay lại title sẽ reset mốc thời gian.
- Hai khung splash vẫn bắt đầu fade ở giây **27**, chỉ tràn ngang hai mép và không thay nội dung trong một lượt opening.
- Đã thêm test pure logic cho mốc fade và kiểm tra trạng thái curtain/input. **Chưa chạy Unity Play Mode/Windows Build**.

## 15/10/2026 — Trực quan hóa bố cục trong Play Mode

- Mở **Tools → Bach Luyen Tien Do → Opening Layout (Play Mode)** khi đang Play. Bật chế độ chỉnh để xem cảnh + splash + menu; tạm ngừng cloud drift, fade, và không ghi đè các RectTransform.
- Có thể chọn trực tiếp **13** đối tượng (sky, 5 mây/núi, 2 splash, logo, 4 nút). Dùng Rect Tool ở Scene View/Inspector để kéo và đổi kích thước. Bấm **LƯU BỐ CỤC VÀO ASSET**; tool ghi tỉ lệ tọa độ và kích thước theo mẫu 800×450 vào `Resources/OpeningArtTheme.asset` bằng `AssetDatabase.SaveAssets`.
- Bấm **XEM LẠI ANIMATION TỪ ĐẦU** để quay lại opening và kiểm tra bố cục mới; hiệu ứng mây trôi giờ cộng offset từ tọa độ đã lưu, không khởi động lại từ tọa độ hardcode.
- Phần editor-only nằm trong `Assets/Editor/OpeningPlayModeLayoutWindow.cs`; không biên dịch vào build game. Không chuyển opening sang Prefab, vẫn dùng runtime UI hiện tại.
- Chưa chạy Unity Play Mode trong môi trường trợ lý. Hướng dẫn: `Docs/Opening_PlayMode_Layout_0.0.53.md`.
