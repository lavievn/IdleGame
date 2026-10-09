# 0.0.4.54b — cung, vận công và phép nổ vùng

Nền: toàn bộ .54a, bao gồm sửa quái tiến theo tốc độ riêng + tốc độ ground.
Nhánh: `feature/0.0.4.54b`. Chưa nghiệm thu Play Mode/Windows native.
Các ghi chép .54a/.54/.52 là lịch sử; quy tắc tấn công dưới đây có ưu tiên.

## Sát thương và chu kỳ tấn công

| Kiểu đánh | Hệ số ATK | Chu kỳ tại tốc đánh 1 | Đòn đầu |
| --- | --- | --- | --- |
| Cận chiến | 100% | 1,4 giây | Ngay khi đủ tầm, không chờ khớp Y |
| Cung | Ngẫu nhiên 55–80% mỗi đòn | 0,7 giây | Bắn ngay khi đủ tầm |
| Phép | 250% | 2 giây vận công | Vận công đủ rồi bắn một quả cầu |

Cả hero và quái dùng cùng quy tắc theo kiểu đánh. Cung giảm 20–45% trước khắc
hệ; vẫn giữ dao động chung 85–100% của phiên bản trước. Vì vậy ở hệ trung tính,
sát thương cung trước làm tròn là 46,75–80% ATK, trung bình khoảng 62,44%.
Đây là hai hệ số riêng, đều được ghi trong Info; chưa có item chính xác hoặc
một lần roll trượt độc lập. Đòn hụt vì di chuyển được ghi sát thương 0.

Giữ công thức linh căn nhất–ngũ trọng, hệ Vô, map/hard damage của .54a.
Sát thương nền = max(1, round(ATK tổng × hệ số kiểu đánh × dao động)).
Sát thương linh căn = max(1, round(sát thương nền × tổng trọng số tương/khắc)).
Quái gây lên hero còn nhân hệ số map và map khó như trước.

Cận chiến/cung hồi chiêu sau khi ra đòn. Không chờ đối thủ đứng lại hoặc
CurrentTarget của đối thủ; đủ khoảng X là có thể đánh. Cận chiến không kiểm
tra điểm rơi và không hụt do projectile. Hero vẫn tiến trái, quái tiến phải,
không quay lại cơ chế điểm gặp chung; không đổi logic camera/ground .54a.

## Vận công và thanh xanh

Phép vận công trước MỖI lần bắn; một chu kỳ bằng 2/baseAttackSpeed giây,
baseAttackSpeed vẫn bị giới hạn như bản trước. Hero vận công khi đã triển khai,
kể cả khi đang tiếp cận hoặc đang chờ wave. Quái vận công từ lúc có trong trận.
Đầy mà chưa có mục tiêu trong tầm thì giữ ở 100%; có mục tiêu hợp lệ thì bắn
ngay, reset về 0 và bắt đầu vận công đòn kế tiếp, song song với đạn đang bay.
Không reset chỉ vì đổi mục tiêu hoặc chuyển wave thường. Đổi kiểu đánh,
load/retry/reset và chết hủy chu kỳ cũ. Tạm dừng đóng băng chu kỳ và đạn.

Thanh xanh nằm phía trên thanh HP, chỉ hiện cho nhân vật đang dùng phép,
fill trái sang phải từ 0–100%. Thanh được tạo tự động và tái sử dụng theo actor,
không cần gán reference mới vào scene/prefab, không nhận raycast/cản nút UI.
Ẩn và xóa progress khi chết/ẩn/pool/reset hoặc chuyển sang cung/cận chiến.

## Điểm ngắm, hụt và vùng nổ

- Ranged chụp vị trí thân mục tiêu tại lúc bắn, không đuổi theo đối thủ.
- Điểm xuất phát và điểm rơi cùng dịch theo camera; background riêng không
  kéo đạn. Camera trôi không tự biến một mục tiêu đứng yên thành mục tiêu hụt.
- Cung đi thẳng, chỉ gây sát thương lên đối thủ đã chọn nếu tâm thân còn nằm
  trong dung sai 12 đơn vị quanh điểm rơi tại lúc đạn đến. Quái chết/pool không
  chuyển đạn cũ sang đối thủ khác. Chưa có va chạm chặn đạn giữa đường.
- Phép chỉ chọn một mục tiêu gần nhất trong tầm lúc vận công đầy. Bắn một
  quả cầu theo đường cong, kích thước 4 lần cũ: hiểu “tăng thêm 300%” là ×4.
- Phép không homing. Khi đến điểm rơi, nổ theo vòng tròn BÁN KÍNH100 đơn vị
  trong tọa độ Ground, tính cả X/Y; mọi đối thủ đang sống trong vùng đều trúng,
  kể cả đối thủ ngoài tầm bắn của người thi triển. Không gây sát thương đồng đội.
  Quái chỉ có một hero đối thủ trong hệ chiến đấu hiện tại.
- Mỗi lần bắn chụp ATK, hệ số, dao động và linh căn bên tấn công. Vùng nổ dùng
  cùng sát thương nền, tính khắc hệ riêng với từng nạn nhân lúc trúng. Lên cấp
  trong khi đạn đang bay không tăng sức mạnh đạn cũ.
- Hero phép vẫn nổ nếu mục tiêu ngắm ban đầu chết nhưng trận còn đối thủ.
  Đạn quái bị hủy khi chính quái bắn chết; reset/load/chết hero xóa toàn bộ đạn.
- Thời gian bay mặc định: cung 0,18 giây, phép 1,2 giây, luôn giới hạn không quá
  85% chu kỳ đòn để không dài hơn khoảng giữa hai lần đánh.

Thông số có thể tinh chỉnh trong CombatManager: physicalHitRadius=12,
magicImpactRadius=100, magicProjectileScale=4. Chưa có ngắm đón chuyển động,
accuracy item, tăng/giảm vận công theo trang bị, interrupt hoặc skill tree.

## Kiểm chứng và điểm cần thử trong Unity

114 kịch bản hồi quy PASS với Unity API mô phỏng, bao gồm 17 kịch bản mới .54b.
720 lượt mô phỏng 120 giây tối đa, FPS30, cấp1/10/20/50/100/999, ba thế đánh,
mapVisit1 và100, mỗi trường hợp20seed. Chi tiết: `Tests~/test-results.txt`.
Biên dịch nhánh `UNITY_STANDALONE_WIN` với API mô phỏng PASS. Đây không phải
bản build Windows có thể chơi; chưa có Unity Editor/Windows native để kiểm tra.

Quan sát cân bằng: ở mapVisit 1/cấp 1, chết trong120 giây là cận chiến11/20,
cung11/20, phép1/20; cấp 20 là20/20,20/20,17/20. Phép mạnh rõ hơn giai đoạn
đầu khi có nhiều mục tiêu sát nhau. Đây là đo trên fixture, không phải tỉ lệ chết
thực tế trong game; fixture không chuyển map và cho hero cộng toàn bộ điểm ATK.
Giữ các hệ số người dùng yêu cầu, chưa tự cân lại EXP/HP/heal hoặc tốc đánh.

Ưu tiên test:

1. Cho hero/quái dùng phép ở 800/500/250: thanh xanh phía trên HP hiện đúng,
   chạy đúng 2/baseAttackSpeed, bắn một đòn khi đầy rồi reset. Cung/cận chiến
   không có thanh; không nhân bản thanh sau pool/load/retry.
2. Cung bắn ngay khi quái đang tiến phải vào tầm; đạn đến vị trí cũ, quái chạy
   khỏi dung sai 12 thì hụt. Thử quái bắn cung khi hero đang tiến trái. Đứng yên
   hoặc chỉ camera trôi thì vẫn trúng. Cận chiến khác lane vẫn đánh ngay.
3. Phép: vận công lúc tiếp cận, đầy mà chưa thấy quái giữ 100%; khi quái vào tầm
   bắn một quả cầu lớn. Tụ 3 quái trong 100 đơn vị quanh điểm rơi thì cả 3 trúng,
   tách khỏi vùng thì không trúng. Đổi linh căn để đối chiếu Info từng nạn nhân.
4. Tạm dừng lúc vận công và đạn đang bay; load/reset/chết quái/ngắm mục tiêu
   đã chết; không có đòn ma. Thử đánh nhiều wave để charge không reset vô cớ.
5. Theo dõi cân bằng cung/phép ở cấp 1/20/50, map thường/khó, quái thưa/tụm.
   Tinh chỉnh bán kính/scale nếu 100 hoặc ×4 chưa phù hợp cảm giác hình ảnh.
