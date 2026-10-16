# 0.0.54: Modal / Video opening / chân Hero — 16-10-2026

## Các hiện tượng được báo
- **Máy A, bản Windows build:** lv2 → chuyển map → có 3 Hero → Save 1 → thoát hẳn game → mở lại → Chơi mới → Load Save 1 → về tiêu đề: popup Có/Không không nhận click; lần mở game tiếp theo không phát/tải được video opening.
- **Máy B, Unity Editor:** quy trình tương tự với Save 2 không bị lỗi nút xác nhận, nhưng nút Quay lại màn hình chính bỏ qua xác nhận và về ngay. Đôi khi Hero cận chiến lao sát đáy màn hình rồi thụt xuống dưới khung nhìn.
- Người dùng xác nhận các cơ chế khác tạm hoạt động; chỉ sửa phần nhỏ, không đổi định dạng Save, 131 polygon, Fog, camera X.

## Phát hiện trong mã và sửa

1. `UIManager.ReturnToTitle()` trước đây gọi trực tiếp `gm.ReturnToMainMenu()`: **không có câu hỏi xác nhận**. Đã thêm trang `SystemReturnTitleConfirm` với `ReturnTitleYes` / `ReturnTitleNo`. Chỉ Yes mới thực hiện tự lưu và trở về; No quay về menu hệ thống.
2. `StartMenuUI.MakeButton()` trước đây khóa **tất cả** nút khi `openingMenuReady=false` hoặc `entryLocked=true`, kể cả Có/Không trên popup độc lập. Đã thêm `confirmationAction` không chịu khóa fade; vẫn bắt buộc `HasPendingConfirmation` và popup hiển thị.
3. `OpeningCinematicUI.EnsureOpeningSiblingsParent()` trước đây reparent nền/video và màn đen theo `preGameUI` nhưng **không reparent popup xác nhận**. Đã sửa cùng Canvas và đưa popup đang hiển thị lên đầu khi cần. `ShowConfirmation()` đồng bộ trước khi mở.
4. `OpeningBackgroundVideo` có khả năng gọi `Prepare()` trùng trong lúc decode chưa xong nếu quay lại menu; callback sẵn sàng muộn có thể phát video khi gameplay đã bắt đầu. Đã thêm `preparing`, `wantPlayback`, chặn prepare trùng và callback khi stop, reset an toàn khi quay về menu; log rõ lỗi decoder. `OpeningHeroSplashVideo` cũng log file ngẫu nhiên đã chọn và file bị decoder từ chối.
5. `HeroController.TickMovement()` nhận y trực tiếp từ quái mà không có clamp đáy. `EnvironmentManager.ClampHeroFeetY()` giới hạn tại `battleArea.rect.yMin + 12f`, áp cho Hero chính và các Hero B2 khi spawn/đuổi quái/chuyển map. **Không thay đổi Y của monster**, phạm vi đánh, camera X hay thứ tự render.

## Video: điều chưa thể kết luận

- Video nền là `Assets/StreamingAssets/OpeningBackground/opening.mp4`. Video splash trái/phải lấy **chỉ** tệp trong `Assets/StreamingAssets/OpeningSplashArt/` có tên bắt đầu **L** hoặc **R** và định dạng PNG/JPG/MP4.
- Tại thời điểm kiểm tra nhánh, thư mục `OpeningSplashArt` trên GitHub không có tên bắt đầu L/R. Nếu máy build không có tệp riêng được chép thêm, hai splash sẽ hiển thị placeholder. Đó là vấn đề nguồn file khác với lỗi video nền trên lần khởi động thứ hai.
- Trước đây Console còn báo lỗi đọc video `Assets/Resources/OpeningSplash/Dragon Tattoo Samurai Live Wallpaper.mp4`. Đây là file import của Unity **khác** với hai nguồn playback StreamingAssets kể trên; không đủ chứng cứ để khẳng định cùng nguyên nhân.
- Không thể xác nhận codec hay trạng thái file trên máy A từ source code; nếu tái hiện, gửi log `[Opening] Video background unavailable` hoặc `Splash video decoder failed`, cùng đường dẫn file được log. Không tự thay, xóa hoặc mã hóa lại MP4 khi chưa xác minh.

## Hồi quy đã bổ sung (chưa chạy Unity)

- Popup title vẫn nhận No khi menu vừa trở lại từ direct load và đang fade; popup có cùng Canvas sau reparent.
- Background VideoPlayer không Prepare hai lần khi lượt trước đang pending; khi quay về title sẽ chạy lại (stub).
- Menu hệ thống ReturnTitle: No giữ gameplay, Yes lưu AutoSave rồi về tiêu đề.
- Hero chính/B2 không tụt chân dưới mép vùng chiến đấu khi quái có y âm lớn.

## Kiểm tra trên máy

1. Unity Editor: mở hệ thống → Quay về màn hình chính → chọn **Không**, đảm bảo không save hoặc thoát. Lặp lại chọn **Có**, kiểm tra Autosave có ghi và menu mở.
2. Windows build: dùng đúng Save 1 / Save 2 và quy trình người dùng báo; xác nhận popup Có/Không click được sau load. Kiểm tra kích thước cửa sổ 600/800/1150 và sau resize.
3. Thoát hẳn app → mở 2 lần. Quan sát cả `opening.mp4` nền lẫn hai splash L/R; ghi lại `Player.log` / các log `[Opening]` nếu lần hai mất video.
4. Tạo quái ở làn rất thấp; thử Hero cận chiến kéo xuống đáy và Hero phụ xuất hiện/đổi map. Chân nhân vật phải còn nằm trong màn hình; nếu ảnh sprite có padding trong suốt lớn, cần kiểm tra visual sau clamp.

**Giới hạn:** Các commit này được rà soát mã nguồn và thêm kiểm thử; chưa được chạy trên Unity Play Mode hoặc Windows build trong môi trường hiện tại.
