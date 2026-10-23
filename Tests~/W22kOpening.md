# W2.2k A — typography và bố cục nút opening

Snapshot hiện có trong OpeningArtTheme.asset lưu 4 nút 65×11.875, tâm Y cách
14.375 ở mốc 800×450; StartMenuUI trước đây áp font 16 lên các khung đó. TMP
clone còn giữ hình học/margin của template; popup sibling chịu cả layout opening
và LayoutStartMenu/ReadableText của HUD. Đây là các xung đột được xác định từ mã;
không khẳng định đã tái hiện mọi trường hợp mất chữ trong Windows Player.

Người dùng đã cho phép giãn nhóm nút quanh tâm chung. StartMenuUI áp snapshot
trước, sau đó chỉ sửa nhóm quá nhỏ/chồng nhau: nút tối thiểu rộng 152–208,
cao 34–42 theo viewport, khoảng cách 6; giữ thứ tự và tâm ưu tiên của nhóm.
Tâm chỉ được clamp khi nhóm sẽ chạm logo hoặc ra khỏi cửa sổ. Các snapshot đã
đủ chỗ giữ nguyên vị trí. Không ghi theme, không di chuyển logo hoặc artwork.

Nhãn stretch vào button với padding 8 ngang/4 dọc; scale=1, margin TMP=0.
Font theo chiều cao nút, auto-size trong khoảng 14–20; caption không wrap.
Logo fallback co chữ theo khung logo đã lưu. Settings, message và action popup
được StartMenuUI quản lý. UIManager loại cả popup sibling khỏi lượt font/layout
chung; callback mouse/EventSystem/native Windows vẫn giữ đường hiện có.

## Kiểm thử

`PATH=/workspace/.onboarding/bin:$PATH bash Tests~/run.sh --opening-font`

9 ca Mono: snapshot nhỏ thực tế và snapshot đủ chỗ; 600/800/1150; hai thứ tự
khởi tạo UI/opening; relayout nhiều lần, Settings, popup New Game/Cancel;
padding, font bounds, nhóm không chồng/logo/cửa sổ, art/logo và theme không đổi.
UnityStub không có glyph layout, TMP auto-size renderer hoặc native hit testing.
Không coi PASS Mono là bằng chứng textBounds không tràn trong Unity/Windows.

Kết quả A: 9 ca mới PASS, nhóm UI cũ 59 PASS; full runner 559 PASS rồi dừng ở
lỗi cũ MissingMemberException GroundPresentation.fixedRect, các bài sau chưa
chạy. Biên dịch UNITY_EDITOR có 16 cảnh báo cũ.

Runner bổ sung ExplorationProgressHUD (đã có trên origin). UnityStub bổ sung
Transform.Find, Floor/CeilToInt, TMP.margin để biên dịch nguồn mới; không sửa HUD
đã nghiệm thu. Tests~ không được Unity import. Không thêm phụ thuộc runtime.

## Nghiệm thu Unity/Windows

1. 600/800/1150: Home hiển thị đủ dấu tiếng Việt, đủ 4 caption, không cắt/tràn,
   không đè logo. Click được toàn bộ mặt nút; artwork/Splash/CL/CR/bottom/mid
   giữ bố cục đã nghiệm thu.
2. Settings: đủ tên, 3 radio 600/800/1150 và dấu chọn; kéo volume, khôi phục,
   quay lại; resize nhiều lần. Font đồng bộ nút và không nhảy sau một frame.
3. New Game/Continue và Reset trong gameplay: message đầy đủ, không đè action;
   Xác nhận/Hủy nhận cả đường mouse Windows. Dùng dữ liệu thử đã sao lưu.
4. Vào game → Return Menu → Settings → popup; lặp lại ở Windows Player để
   kiểm tra thứ tự font/rebuild và vùng clickable thật. Mở Play Mode layout
   editing: các rect đang kéo không bị snap; asset theme không đổi tự động.

Cloud không có Unity Editor; chưa biên dịch/chạy bằng Unity hoặc Windows Player.
