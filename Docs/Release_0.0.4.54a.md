# 0.0.4.54a — sửa linh căn, di chuyển, UI và cụm map

Nền: toàn bộ 0.0.4.54, giữ hệ số khắc theo trọng, cân bằng EXP và mức sát thương
quái tăng tại map20/40/60/80/100. Phiên bản Unity ghi trong dự án: 6000.5.0f1.
Đây là dự án nguồn để mở trong Unity Hub, chưa xuất EXE hoặc nghiệm thu Unity.

## Vô độc lập

- Vô chỉ có thể được random khi đối tượng không thuộc tộc ConLai và có một căn.
  Các tổ hợp hai/ba/bốn căn chỉ chọn ngũ hành, không có Vô. Giữ phân bố số căn
  60/25/10/5 như .54; khi một căn, sáu lựa chọn gồm ngũ hành và Vô ngang nhau.
- Sát thương Vô vẫn không có thưởng/phạt khắc hệ, ở mọi trọng.
- Save .54 tổ hợp có Vô: xóa phần Vô, giữ căn còn lại cùng trọng, chuẩn hóa lại
  tỷ trọng. Ví dụ Hỏa20%/Vô30%/Thủy50% thành Hỏa2/7 + Thủy5/7. Không đổi tên
  hoặc tộc hero. Save ConLai chỉ có Vô (trạng thái không hợp lệ) thay bằng Kim
  Tam trọng100%; đây là cách sửa dữ liệu lỗi, không phải luật sinh con.
- Hàm riêng TryResolveVoidInheritance chốt ngoại lệ di truyền: một cha/mẹ Vô
  thì chỉ lấy căn bên còn lại; cả hai Vô thì con chỉ Vô. Chưa có hệ thống sinh
  con hoạt động nên hàm này chưa nối vào gameplay. F2 trở đi qua nhánh random
  có thể xuất hiện Vô độc lập nếu đối tượng không còn thuộc nhóm ConLai.
  Chưa tự suy diễn công thức kế thừa trọng hoặc tộc của các đời con.

## Di chuyển

Code trước dùng BattleMotion.Approach, cho phép đổi chiều theo dấu tọa độ mục
tiêu; không có biến “điểm gặp nhau” cố định. Nay hero dùng hướng −X, quái +X
trong tọa độ map, chặn bước tiến tại mép tầm đánh riêng của mỗi đối tượng.
Không quay lại đuổi kẻ địch đã ở phía sau và ngoài tầm đánh. Khi tiếp cận,
quét lại mục tiêu hợp lệ gần nhất; khi đã đủ tầm đánh, giữ mục tiêu để không
đổi đích giữa đòn. Cận chiến hero vẫn căn chỉnh trục Y; không thay logic lane.
Quái vừa sinh ngoài mép trái cũng tiến phải về hero, thay vì chỉ trôi với map.
Không có hero triển khai thì quái không tự tiếp cận.

Chuyển động camera vẫn dịch toàn bộ map/nhân vật cùng nhau. Hero có thể trôi
sang phải trên màn hình khi đứng đánh; đây là dịch camera, không phải AI đi lùi.

## Tạm dừng, Info và thông số hero

- Thay BuffIcon màu cam góc trên phải bằng Tạm dừng/Tiếp tục. Dùng cùng đường
  xử lý click của desktop Windows và Editor; vô hiệu uGUI Button cũ nếu có để
  tránh nhấn hai lần. UI/menu vẫn hoạt động khi dừng.
- Time.timeScale=0 dừng di chuyển, camera, combat, đạn, hiệu ứng và coroutine
  có thời gian trong gameplay (gồm chờ wave/autosave). Tiếp tục phục hồi 1.
  Bắt đầu game/retry/load và triển khai hero phục hồi trạng thái chạy. Thoát
  vẫn giữ lưu đồng bộ khi quit như trước. Pause không ghi vào save.
- Info trên log mở bảng lịch sử30 đòn đã gây sát thương, mới nhất trước, có
  Đòn trước/Đòn sau/Đóng. Lấy bản chụp lịch sử khi mở để không tự đổi trang.
  Không tự tạm dừng khi xem. Modal chặn nút phía dưới; popup xác nhận ưu tiên.
- Chi tiết mỗi đòn: đối tượng nguồn/đích và căn/trọng/tỷ trọng lúc tung đòn,
  ATK cơ bản, điểm cộng, tổng ATK, hệ số kiểu đánh, dao động thực tế85–100%, từng cặp căn, tổng hệ số,
  các bước làm tròn, hệ số map và map khó, sát thương cuối tối thiểu 1.
  Đạn không ghi là trúng ngay khi phóng; chỉ ghi lúc va chạm. Đạn bị hủy không
  có lịch sử sát thương. Dữ liệu không đổi nếu hero lên cấp giữa thời gian bay.
  Lịch sử là dữ liệu phiên chơi, xóa khi chơi mới/load và không lưu vào slot.
- Thay text ATK cũ bằng bảng góc trái: HP hiện tại/tối đa, ATK cơ bản + điểm
  cộng = tổng, ATK từng căn (tổng ATK × tỷ trọng), trọng, tốc di chuyển cấu hình,
  tốc đánh thực tế đòn/giây và giây/đòn theo kiểu đánh. Phần ATK linh căn là
  phần nền trước khắc hệ/mode/dao động, không phải sát thương cuối lên mọi quái.
  Đọc lại mỗi LateUpdate, cập nhật HP/cộng điểm/đổi thế/lên cấp.

## Cụm map và môi trường

Một cụm gồm5 map, mỗi map5 đợt, tức25 đợt/cụm. Vị trí trong cụm tính theo lượt vào
map, độc lập chu kỳ5 mapthường/1map khó. Tên cụm + vị trí1/5…5/5 + tên map nhỏ
hiện trên log. Tên map nhỏ vẫn đổi khi hoàn tất5 đợt; retry/load giữ nguyên.

| Chủ đề cụm | Địa hình map nhỏ được random |
|---|---|
| Sơn Lâm | Rừng rậm, đồi núi, hồ nước |
| Bình Nguyên | Đồng bằng, cao nguyên, hồ nước |
| U Trạch | Đầm lầy, rừng rậm, hồ nước |
| Hoang Mạc | Sa mạc, cao nguyên, đồi núi |
| Hải Vực | Biển |
| Cao Sơn | Đồi núi, cao nguyên, rừng rậm |

Sáu chủ đề chọn ngẫu nhiên ngang nhau khi vào cụm mới; có thể gặp lại chủ đề.
Thêm10 tên map cho hồ/biển, tổng40 tên. Lưu chỉ số cụm/chủ đề cùng tiến trình.
Save .54 đã có tên: chọn chủ đề phù hợp địa hình hiện tại để giữ tên/đợt đang
chơi. Không biến5 map nhỏ thành5 đợt và không reset bộ đếm sát thương khi lặp.

- Thêm dữ liệu monsterAnimal/monsterClass: thú, bò sát, chim, thủy sinh, chân
  khớp, lưỡng cư, long, nhân. Đây là phân loại loài/môi trường; chưa phải lớp
  kỹ năng chiến đấu và chưa có mô hình bơi/bay hoặc chỉ số riêng theo loài.
- Rừng có rắn/sói/hổ/gấu/hươu/chim…; không chọn cá và loài thủy sinh.
- Sa mạc có rắn/nhện/bọ cạp…; tổng tỉ lệ hổ/báo1% trong quái có tên loài, chia
  đều0.5% mỗi loài. Đây là mặc định thử cho yêu cầu “cực kỳ hiếm”. Không có cá.
- Biển chỉ chọn Long/Ngư/Kình/Sa/Chương/Giải/Hà/Hải Âu/Nhạn/Ưng; không có ngựa,
  rắn/rết/gấu hoặc Nhân tộc. Các tộc còn lại random ngang nhau ở biển. Nhân tộc
  vẫn giữ tên người và có thể xuất hiện ở các địa hình đất/hồ/đầm còn lại.
- Hồ/đầm có cá, rùa, cá sấu, ếch/cóc và chim nước. Các địa hình khác dùng danh
  sách riêng. Không random toàn bộ51 tên động vật ở mọi map nữa.
- Thêm Long, tổng51 tên loài. Không tự đổi màu map thường theo địa hình; vẫn chỉ
  map khó xanh theo nền trước. Tên quái và dấu tộc lai giữ quy tắc .54.

## Thứ tự kiểm thử trong Unity

1. Chơi mới vài lần: Vô luôn đứng một mình; ConLai không có Vô. Thử save .54
   chứa tổ hợp Vô để kiểm tra căn còn lại/trọng/tỷ trọng vẫn đúng sau chuyển đổi.
2. Hero/quái ba cách đánh: không bước lùi theo tọa độ map; đứng tại tầm đánh,
   không vượt qua nhau ởFPS thấp. Thử nhiều quái và mục tiêu chết/đổi gần nhất.
3. Nhấn Tạm dừng khi đạn đang bay và khi chờ wave: không trúng/ra đòn/sinh đợt
   trong lúc dừng; tiếp tục chạy được, menu/resize vẫn dùng được.
4. Đối chiếu Info với số sát thương log/HP, cả hero và quái, cận chiến/phép/
   đạn vật lý, map thường/khó. Thử xóa trận khi đạn bay: không có đòn ma.
5. Bảng hero: HP sau nhận đòn/hồi kill, ATK phân bổ căn, tốc đánh đổi theo thế,
   tốc di chuyển đúng cấu hình. Không lấy ATK phân căn làm sát thương cuối.
6. Qua5 map: giữ một chủ đề, chuyển chủ đề tại map6; save/load map đang dở giữ
   cụm/tên/đợt. Map6đồngthờilàmap khótheochukỳcũ;map10/11vẫnđổiđúngcụm.
7. Rừng không cá; sa mạc hổ/báo hiếm; biển không thú đất/nhân. Loài, địa hình
   và tên vẫn là dữ liệu/placeholder, chưa có sprite hoặc chuyển động loài riêng.
8. Thử tất cả nút ở cửa sổ800/500/250 trên Windows, đặc biệt Info/Đóng/Pause.

Kiểm tra mã dùng Unity API mô phỏng, không thay thế Play Mode/Windows native.
Kết quả cuối lưu trong Tests~/test-results.txt. Bản đầu game vẫn có chủ ý giảm
sát thương quái50%; không tự thay hồi5–10HP/kill hoặc độ khó ngoài yêu cầu.

Kết quả kiểm chứng: 94 kịch bản hồi quy PASS, 720 lượt mô phỏng chiến đấu,
biên dịch UNITY_STANDALONE_WIN PASS với API mô phỏng. Chưa chạy Unity Editor
hoặc Windows native, chưa xác nhận bố cục thực tế ở cửa sổ 250.

## Bổ sung: tốc độ quái theo ground (09/10/2026)

Bản .54a đầu cộng bước riêng của quái với dịch camera, trong khi ground đang
trôi theo khoảng chạy thực tế của hero. Khi hero300/camera150/quái150, quái
hiển thị300 và ground300: không có bước tiến tương đối trên nền.

Đã bù groundPan−cameraPan vào bước quái đang tiếp cận, dùng khoảng dịch của
cùng frame sau khi hero di chuyển, không dùng CurrentBackgroundSpeed của
frame trước. Camera vẫn dịch toàn bộ đối tượng một lần ở LateUpdate.
Tổng bước nhìn thấy = bước riêng + groundPan (trừ phần bị chặn khi tới tầm
đánh). Ví dụ trên quái450, ground300, nên tiến150 tương đối trên ground.
Không cộng nguyên tốc độ ground vào moveSpeed rồi cộng camera lần nữa.

Khi ground chậm hơn camera, phần bù có thể âm trong tọa độ trước dịch camera;
đây là sửa vận chuyển của nền, không đảo hướng chạy riêng của quái. Sau dịch
camera, quái đang tiếp cận vẫn tiến phải với baseSpeed + groundSpeed. Khi đã
đủ tầm đánh, không thêm bù tiếp cận; quái đứng đánh cùng dịch camera như hero.
Giữ chặn tầm đánh, không vượt mục tiêu với frame dài và tạm dừng không dịch.

97 kiểm tra hồi quy PASS; thử riêng ground75/150/300/600, camera150/1000,
quái50/150/600, FPS30/60/144 và Canvas scale0.2/1/2. 720 lượt mô phỏng vẫn
chạy, nhưng dùng tốc độ mặc định150 nên không thay kiểm tra tốc độ300 thực tế.
Biên dịch nhánh Windows với API mô phỏng PASS; chưa kiểm thử Unity/Windows.
