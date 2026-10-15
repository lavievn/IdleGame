# 0.0.53 — Căn chỉnh bố cục opening ngay trong Play Mode

## Sửa lỗi nhảy về góc / bảo vệ dữ liệu (15/10/2026)

Bản công cụ ban đầu có lỗi: nút **BẬT CHẾ ĐỘ CĂN CHỈNH** gọi `Layout(true)` ngay khi chuyển chế độ, gây reset các khung cảnh về tọa độ tính bằng code; đồng thời khi `UIManager` chuyển menu sang HUD Canvas, lớp cảnh có thể bị tách khỏi hệ tọa độ menu. Các RectTransform vì vậy có thể nhảy về một góc thay vì giữ đúng vị trí hiện tại.

**Đã sửa trên nhánh `feature/0.0.53`:**

- Bật chế độ căn chỉnh chỉ khóa animation và cho hiện đủ thành phần, **không gọi `Layout(true)`**, không sửa `anchoredPosition`, `sizeDelta`, `pivot` hoặc `anchors` của từng lớp.
- Kiểm tra/dồng bộ để opening backdrop và black curtain dùng cùng `RectTransform.parent` với menu, kể cả khi `UIManager` tạo HUD runtime sau khi menu xuất hiện.
- **Khóa thao tác LƯU BỐ CỤC** nếu hệ tọa độ của hai khung root bị lệch, kích thước không khớp hoặc RectTransform có giá trị không hợp lệ. Không tự lưu hoặc làm mất dữ liệu cũ.
- Mỗi lần lưu thành công, công cụ **sao lưu file asset trước** vào thư mục `<thư mục dự án Unity>/OpeningLayoutBackups/OpeningArtTheme_before_YYYYMMDD_HHMMSS_fff.asset` (ngoài `Assets` để không tự import và không làm rối Resources).
- Hộp thoại xác nhận hiện trước khi ghi. Khi không thể sao lưu, thao tác ghi **bị hủy**.
- **Quan trọng:** asset `Assets/Resources/OpeningArtTheme.asset` trên GitHub chưa có mảng `playModeLayouts` ghi đè trong thời điểm sửa lỗi, nên mã mới không tự thay thế các thiết lập/ảnh/feather hiện tại.

**Kiểm thử sau khi Pull:** bật Play → mở menu công cụ → ghi nhận vị trí mây/núi/splash/logo ở Scene/Game View → bật căn chỉnh → **không có khung nào được nhảy**. Kéo thử một khung, bấm lưu, xem thông báo đường dẫn backup rồi bấm xem lại animation. Thoát Play và vào lại để chắc chắn khung vẫn đúng. Với bố cục mới từ Play Mode, sau khi lưu, kiểm tra `git diff -- Assets/Resources/OpeningArtTheme.asset` trước khi push.

**Nếu bạn đã bấm Lưu bố cục bị lỗi ở máy:** đừng ghi thêm. Mở `OpeningArtTheme.asset` và kiểm tra mục `playModeLayouts` trong YAML/Inspector. Đối chiếu với `git diff` hoặc bản sao lưu trước đó; tránh hoàn tác nguyên asset một cách mù quáng vì có thể mất các Sprite và `edgeFeatherPixels` bạn đã tùy chỉnh.

---

## Vì sao có công cụ này?

Các lớp UI opening sinh ra lúc chạy game (không nằm sẵn trong Scene khi dừng Play). Các lệnh `Layout()` và `Update()` trước đây liên tục trả tọa độ mây/núi/menu về vị trí tính sẵn trong code; sửa `RectTransform` trong Play Mode sẽ bị mất hoặc bị giật. Bản này cho phép dựng cảnh thật rồi chỉnh trực tiếp trong Scene View.

## Quy trình thao tác — chú ý LƯU TRƯỚC KHI DỪNG PLAY

1. Pull `feature/0.0.53`, mở scene game và nhấn **Play** trong Unity.
2. Chọn **Tools > Bach Luyen Tien Do > Opening Layout (Play Mode)**. Có thể dock cửa sổ công cụ này bên cạnh Inspector/Hierarchy/Scene.
3. Bấm **BẬT CHẾ ĐỘ CĂN CHỈNH**. Lúc này lớp đen sẽ ẩn, logo/menu và hai splash sẽ hiện đủ; mây và transition tạm dừng. Game View hiển thị đúng bố cục đang chạy thật.
4. Bấm tên thành phần trong cửa sổ để chọn đúng `RectTransform` trong Hierarchy. Chuyển sang tab **Scene**, nhấn **T (Rect Tool)** hoặc chỉnh `Pos X`, `Pos Y`, `Width`, `Height` trực tiếp ở Inspector. Quan sát kết quả ở Game View.
5. Bấm **LƯU BỐ CỤC VÀO ASSET** trước khi Stop. Tool ghi **13 phần tử** vào `Assets/Resources/OpeningArtTheme.asset` bằng `AssetDatabase.SaveAssets()`. Năm trường kích thước mây/núi cũng được đồng bộ.
6. Bấm **XEM LẠI ANIMATION TỪ ĐẦU** để xem chuyển động với vị trí vừa lưu. Muốn sửa tiếp, bật chế độ căn chỉnh lần nữa; muốn hủy các thay đổi chưa lưu, bấm **HOÀN TÁC THỬ - ĐỌC LẠI BỐ CỤC ĐÃ LƯU**.
7. Sau khi kiểm thử ở 600/800/1150, xem `git diff Assets/Resources/OpeningArtTheme.asset`, rồi tự commit file asset nếu bố cục đã chốt. **Commit code không thể bao gồm các tọa độ bạn chỉnh sau này trong Unity cho đến khi bạn push thay đổi asset.**

## 13 RectTransform được quản lý

1. `sky` — nền trời
2. `farMountains` — núi xa
3. `nearMountains` — núi gần
4. `backClouds` — mây xa
5. `leftClouds` — mây trước trái
6. `rightClouds` — mây trước phải
7. `splashLeft` — khung splash trái (chỉ load tên L*)
8. `splashRight` — khung splash phải (chỉ load tên R*)
9. `gameTitle` — logo game
10. `continueButton`
11. `newGameButton`
12. `settingsButton`
13. `quitButton`

Không cho chỉnh parent `OpeningCanvas` hoặc `Black Curtain` bằng tool này. Giữ nguyên các Anchor/Pivot hiện có; tool chủ yếu quản lý `anchoredPosition` và `sizeDelta`. Các phần Settings và popup xác nhận chưa nằm trong 13 phần tử; chúng tiếp tục theo layout trước đó.

## Bố cục theo kích thước cửa sổ

Bản lưu lưu tọa độ + kích thước dựa trên cửa sổ mẫu **800×450**. Với cửa sổ thực **w×h**, vị trí và kích thước được nhân `(w/800, h/450)` cho từng trục. Do đó, kích thước và tọa độ có thể không phải số tròn khi đổi resolution; cơ chế này giữ tỷ lệ bố cục, không tự bảo đảm mọi trường hợp giữ khoảng cách an toàn giữa logo và nút ở khung nhỏ nhất. Cần kiểm tra trực quan cả 3 kích thước.

## Các hiệu ứng được giữ nguyên

- 0–1,8s fade từ đen; 1,8–4,8s chờ cảnh; 4,8–6,3s logo và menu fade in.
- Splash trái/phải chọn ngẫu nhiên đúng tiền tố `L` / `R` từ thư mục StreamingAssets; từ giây 27, fade 1,4s; nguồn ảnh hoặc MP4 vẫn được crop đúng khung khi thay Width/Height.
- Mây trong thời gian chạy bình thường trôi **quanh tọa độ nền đã lưu**; khi vào gameplay vẫn có transition dạt mây ra hai bên. Không thay tọa độ game, camera hoặc logic combat.

## Giới hạn và lưu ý

- Công cụ chỉ hoạt động trong **Unity Editor khi đang Play**, không phải giao diện kéo-thả trong bản build .exe.
- Điều chỉnh `localScale`, `rotation`, `anchors` hoặc `pivot` không được lưu bởi snapshot hiện tại; thay `Pos X/Y` và `Width/Height`.
- Chỉnh sửa trong Play Mode **không tự lưu**. Cần bấm nút lưu và xác nhận `OpeningArtTheme.asset` đã thay đổi. Nếu reload script / domain hoặc dừng Play khi chưa lưu, thao tác thử sẽ mất.
- Khi công cụ đang ở chế độ chỉnh, không đổi kích thước cửa sổ; kết thúc chế độ chỉnh rồi đổi và xem preview. Nếu đổi trong lúc chỉnh, UIManager bị tạm chặn cập nhật vị trí để tránh mất công căn.
- Chưa chạy Unity Play Mode / bản Windows trong môi trường viết mã; cần xác minh trực tiếp ở Unity.
