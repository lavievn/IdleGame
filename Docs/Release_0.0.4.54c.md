# 0.0.4.54c — chữ, cửa sổ Windows và bảng DEVB

Bản thử tiếp theo của tiến trình 0.0.4, chưa coi là bản cuối. Nền là toàn bộ
0.0.4.54b, commit GitHub `9514345`, bao gồm phép vận công, đạn ngắm cố định,
map/linh căn và chuyển động quái theo ground. Nhánh `feature/0.0.4.54c`.

## Giao diện dễ đọc ở cửa sổ nhỏ

Mã trước đặt nhiều chữ 14–26 đơn vị trên Canvas tham chiếu 1920 × 1080, nên khi
thu cửa sổ chúng bị thu cùng khung game. .54c đưa các bảng đọc/chỉnh thông số
sang Canvas riêng theo điểm ảnh; giữ Canvas chiến trường để không đổi khoảng
cách, ground/camera và tầm đánh gốc.

- Chữ UI dùng kiểu thường, cỡ mục tiêu 14 điểm ảnh trong khung game; bỏ tự
  thu nhỏ để nhét cả đoạn văn dài. Số sát thương nổi có cỡ mục tiêu tối thiểu 12.
- Menu hệ thống co bố cục theo cửa sổ 800/500/250, chữ không thu cùng thế giới.
- Bảng dài chia trang: công thức sát thương, nhật ký đầy đủ và chỉ số hero.
  Log trên màn hình có thể rút gọn bằng dấu “…”. Nhấn log để đọc đầy đủ;
  nút Info tiếp tục xem chi tiết đòn đánh. Nút Chỉ số mở thông số hero.
- Bảng chỉ số thường trực hiện ở cửa sổ đủ cao; khi nhỏ, dùng nút Chỉ số để
  tránh bảng che hết chiến trường. Pause/DEVB/Chỉ số nằm cùng hàng phía trên.

Các kiểm tra hình học và mô hình bố cục đã thử ở 800/500/250. Chưa có ảnh
chụp Unity thực tế để xác nhận nét chữ, glyph tiếng Việt, DPI hoặc clip khi render.

## Kéo cửa sổ và đổi kích thước

Trên bản Windows, nhấn-thả trên cùng một nút mới kích hoạt nút. Giữ chuột và
kéo từ 5 điểm ảnh hệ tọa độ Windows trở lên thì di chuyển cửa sổ; không kích
hoạt nút khi kết thúc kéo, kể cả kéo rồi trả chuột về điểm bắt đầu.

Có thể kéo từ nút, log, bảng thông số, popup và ground. Riêng ô nhập DEVB giữ
thao tác chọn/sửa chữ; có thể kéo bằng nhãn ô, tiêu đề hoặc vùng bảng còn lại.
Giữ vị trí bám chuột lúc bắt đầu, không gọi thao tác kéo caption/Aero Snap.

Mã resize trước chờ cố định 0,2 giây rồi đặt native window trong khi Unity có
thể vẫn đang xử lý thay đổi. Đây là một khả năng gây tranh chấp vị trí, chưa có
log Windows để chứng minh là nguyên nhân duy nhất của lỗi người dùng gặp.

Sửa .54c:

1. Ghi tọa độ góc trái trên và vùng làm việc của màn hình chứa cửa sổ trước khi
   đổi kích thước. Dùng GetMonitorInfo.rcWork để không tính phần taskbar che.
2. Chờ ít nhất hai frame và chờ Screen.width/height khớp kích thước yêu cầu
   (giới hạn 60 frame), sau đó khôi phục kiểu cửa sổ không viền và tọa độ cũ.
3. Chỉ dịch thêm nếu kích thước mới vượt vùng làm việc; hỗ trợ màn hình phụ
   có tọa độ âm. Thu nhỏ ở vị trí hợp lệ không tự neo xuống đáy.
4. Theo dõi vị trí thêm 8 frame để sửa một lần/khung hình nếu Unity đặt lại trễ;
   sau đó ngừng giữ tọa độ, không giằng lại khi người dùng kéo.
5. Tạm ngừng nhận thao tác trong lúc resize để không bắt nhầm gốc kéo; tránh
   thay đổi khung cửa sổ mỗi lần chuyển chế độ xuyên chuột.

Không thể xác nhận hết lỗi snap chỉ bằng mô phỏng. Cần chạy Windows thật,
đặc biệt DPI 125/150%, màn hình phụ, taskbar ở các mép và đổi kích thước liên tục.

Tài liệu đối chiếu:
- Unity 6000.5 Screen.SetResolution: thay đổi được áp dụng ở cuối khung hình.
  https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/screen/setresolution
- Microsoft SetWindowPos và vùng làm việc nhiều màn hình:
  https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos
  https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-monitorinfo

## DEVB: chỉnh cân bằng trực tiếp

Nút DEVB cạnh Tạm dừng. Mở bảng sẽ tạm dừng trận; OK hoặc Hủy trả lại trạng
thái chạy/dừng trước lúc mở. Hai tab Hero/Quái có cấu hình độc lập, mỗi tab:

| Ô nhập | Tác dụng |
| --- | --- |
| ATK cơ bản | ATK gốc ở cấp 1 |
| ATK thêm/cấp | Cộng theo cấp hiện tại−1 |
| Tốc đánh gốc | Hệ số tốc đánh ở cấp 1, không phải số đòn/giây |
| Tốc đánh thêm/cấp | Cộng vào hệ số theo cấp hiện tại−1 |
| Tốc di chuyển | Tốc độ riêng của đối tượng, đơn vị Ground/giây |
| Tầm đánh | Tầm riêng của kiểu đang chọn: cận chiến/cung/phép |
| HP cơ bản | HP gốc ở cấp 1 |
| HP thêm/cấp | Cộng theo cấp hiện tại−1 |

Nút “Tầm: … · đổi” chỉ chọn kiểu tầm cần chỉnh; không đổi kiểu tấn công của
hero/quái đang chơi. Ba tầm được lưu riêng để không biến cung/phép thành tầm
cận chiến. Các số tầm và chuyển động dùng tọa độ Ground, không phải pixel cửa sổ.

Công thức khi có cấu hình DEVB:

- ATK gốc hiện tại = làm tròn lên(ATK cấp 1 + ATK thêm/cấp × (cấp−1)).
- HP gốc hiện tại = làm tròn(HP cấp 1 + HP thêm/cấp × (cấp−1)).
- Hệ số tốc đánh = tốc đánh gốc + tốc đánh thêm/cấp × (cấp−1), chặn 0,1–20.
- Chu kỳ đòn =1,4 giây/hệ số với cận chiến,0,7 giây/hệ số với cung,
  2 giây/hệ số vận công với phép. DEVB cho vượt giới hạn tốc đánh 1,25 cũ;
  chưa bật cấu hình DEVB thì công thức và giới hạn cân bằng cũ vẫn giữ nguyên.
- Giữ điểm đã cộng: addedDamage/addedHealth cộng sau ATK/HP gốc như trước.
  Không đổi linh căn, tên/tộc/giới tính, cấp, EXP, điểm, map hoặc đợt quái.
- Giữ tỷ lệ HP hiện tại, không tự hồi đầy. Hero chết không sống lại khi đổi số.
- Tab Quái cập nhật toàn bộ quái còn trong trận và quái xuất hiện về sau.
  Tab Hero cập nhật hero hiện tại và công thức tăng chỉ số khi lên cấp sau đó.
- Áp dụng xóa đạn đang bay và reset hồi chiêu/vận công cũ để không có đạn mang
  sức mạnh của cấu hình trước. Không xóa trận, không tính thành qua wave.

OK kiểm tra tất cả ô trước khi áp dụng, không cập nhật một phần. Nhập được
số thập phân dùng dấu chấm hoặc phẩy; số không hữu hạn/ngoài giới hạn bị từ chối.
Giới hạn ô: ATK 1–100000; ATK/cấp 0–10000; tốc đánh 0,1–20; tốc đánh/cấp 0–2;
di chuyển 0–20000; tầm 1–20000; HP 1–1000000; HP/cấp 0–100000.
ATK tính theo cấp chặn 1000000; HP tính theo cấp chặn 10000000 để tránh tràn số.

Gốc chỉ điền bản nháp thông số ban đầu của tab, phải bấm OK mới khôi phục.
Khôi phục Gốc xóa cấu hình ghi đè của tab đó, gồm cả giới hạn tốc đánh gốc 1,25.
Hủy bỏ bản nháp, không đổi chỉ số. Ở 250 bảng chỉ hiện một ô mỗi trang; dùng‹/›
để xem tám ô. Cửa sổ rộng hơn hiện nhiều ô cùng lúc, không thu nhỏ chữ.

Cấu hình lưu riêng tại Application.persistentDataPath/dev_balance.jsonl, không
sửa ScriptableObject hay mã nguồn và không dùng slot save nhân vật. Cấu hình
hỏng bị bỏ qua. Chơi mới/load nhân vật vẫn dùng DEVB đã lưu; muốn trở lại cân
bằng thường, chọn Gốc→OK cho cả hai tab. Không có chỉnh riêng cho từng con quái.
Nếu không ghi được tệp, cấu hình vẫn áp dụng trong phiên hiện tại và log báo rõ.

Không thêm ô khắc hệ, hệ số cung/phép, damage map/hard, EXP, hồi máu/kill hoặc
hằng số cộng điểm. Các hằng số đó tiếp tục theo .54b.

## Kiểm chứng và thứ tự thử trên máy người dùng

129 kịch bản hồi quy PASS, gồm 15 kịch bản mới .54c, dùng Unity API mô phỏng.
720 lượt mô phỏng chiến đấu khi chưa bật DEVB, giữ kết quả cân bằng của .54b.
Biên dịch nhánh UNITY_STANDALONE_WIN PASS với API mô phỏng. Mô hình hình học
DEVB được kiểm tra ở 800/500/250; không phải ảnh Unity thực tế.
Chi tiết: Tests~/test-results.txt và Tests~/Patch54cRegression.cs.

Chưa có Unity Editor/Windows native ở môi trường xử lý, nên chưa có executable
Windows và không kết luận lỗi snap đã hết hoàn toàn.

Ưu tiên thử:

1. Đặt cửa sổ giữa màn hình, gần mép dưới/taskbar, rồi đổi 800→500→250→800.
   Kiểm tra vị trí ổn định, UI không nằm dưới taskbar. Thử màn hình phụ và DPI.
2. Kéo từ Pause, DEVB, menu, log, popup: không bấm nhầm khi kéo. Bấm-thả bình
   thường vẫn đúng một lần. Kéo từ phần trên để cứu cửa sổ ở sát mép dưới.
3. Chữ/menu/log/chỉ số ở ba mức. Nhật ký/Info dài phải đọc được qua các trang,
   không ép chữ nhỏ. Thử các màn hình bắt đầu, xác nhận, chết và tải bản lưu.
4. DEVB: nhập bằng bàn phím, chọn/sửa/dán số, đi qua các trang. Thử dấu phẩy,
   số sai, Hủy và OK. Kiểm tra focus bàn phím trong bản desktop không có focus.
5. Thay ATK/HP/tốc đánh/move/tầm hero và quái, đối chiếu chỉ số/hit/log ở cấp
   hiện tại và sau lên cấp/wave mới. HP giữ tỷ lệ, linh căn và điểm không mất.
6. Thử restart với DEVB đã lưu; Gốc→OK từng tab để trở về cân bằng .54b.
   Pause trước khi mở thì đóng vẫn dừng; đang chạy thì đóng lại tiếp tục.
