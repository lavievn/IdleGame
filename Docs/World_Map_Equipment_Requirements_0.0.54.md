# Bách Luyện Tiên Đồ — World Map, vũ khí, võ học và điều kiện cảnh giới (0.0.54)

**Ngày:** 15/10/2026. **Trạng thái:** chốt hướng thiết kế sau trao đổi, chưa phải triển khai code/asset/gameplay. Không thay `main`; nội dung thế giới, tỷ lệ phân bố và cân bằng debuff tiếp tục thảo luận sau khi chốt khung graph.

## I. Quy tắc chung — điều kiện sử dụng và đi lại

- Dùng **một hệ đánh giá điều kiện** có thể tái sử dụng: cảnh giới tối thiểu (và kỳ Sơ/Trung/Hậu nếu cần), Level, chỉ số độc lập, năng lực/ kỹ năng đã sở hữu, trang bị/pháp bảo phù hợp, điều kiện đặc biệt của địa điểm.
- **Cùng bộ điều kiện, nhưng kết quả không giống nhau:** trang bị thông thường có thể dùng khi thiếu điều kiện nhưng chịu debuff/giảm hiệu suất; võ học/tâm pháp có thể chịu phản phệ/chi phí/hồi chiêu; một số võ học đặc biệt không thể thi triển trước cảnh giới yêu cầu; **map Endgame hoặc có chướng ngại đặc biệt có thể khóa lối vào hoàn toàn**.
- Tách **yêu cầu để đi tới map** (đã clear tối thiểu wave nơi đang đứng; có cạnh nối hợp lệ) với **yêu cầu để vượt chướng ngại** (pháp bảo chống độc, năng lực ngự kiếm, đủ MP...), **yêu cầu để sống sót** (không bị debuff quá nặng) và **yêu cầu để dùng trang bị/kỹ năng đủ hiệu suất**.
- Không cho chỉ số nhận được từ chính vật phẩm đang xét tự đáp ứng yêu cầu của vật phẩm đó; kiểm tra điều kiện trên chỉ số nguồn hợp lệ trước khi nhận hiệu ứng, tránh vòng lặp trang bị và exploit. Quy tắc chi tiết cho tổ hợp nhiều item còn thiết kế tiếp.
- Mọi yêu cầu và hình phạt phải có lý do đúng đặc tính, thông tin rõ ràng trong tooltip; không áp một phép trừ ngẫu nhiên giống nhau cho tất cả.

## II. Vũ khí: nhiều biến thể — ít nhóm cơ chế

**Đã chốt:** mở rộng số lượng loại vũ khí hiển thị, nhưng gom theo **nhóm vận hành** để tái sử dụng chỉ số chính và nhóm hình phạt; mỗi item cá thể có chỉ số, yêu cầu, độ hiếm, ngũ hành và debuff được xác định/lăn ngẫu nhiên **trong khoảng thiết kế của nhóm và biến thể**, nên hai item cùng loại không bắt buộc giống nhau. Tỷ lệ rơi, khoảng giá trị và trần phạt chưa chốt.

| Nhóm vận hành | Loại/biến thể ví dụ | Chỉ số có thể nổi trội | Debuff phù hợp nếu không đủ yêu cầu |
| --- | --- | --- | --- |
| Cận chiến vật lý | Kiếm, đao, thương, kích, phủ, chùy, song kiếm/đao, trảo, quyền sáo | Công vật lý, tốc độ tấn công, chí mạng, chính xác | Giảm tốc độ đánh/di chuyển, giảm chính xác, chậm vận chiêu |
| Tầm xa vật lý | Cung, nỏ, liên nỏ, phi đao, ám khí, cơ quan khí | Công vật lý, chính xác, tốc độ bắn, chí mạng | Giảm chính xác/tốc độ bắn, tăng thời gian giữa đòn, giảm tầm hữu hiệu |
| Pháp khí/pháp bảo | Trượng, phất trần, phù, ấn, kính, hồ lô, cầm, châu, ngọc, phi kiếm thần thức, luân | Công pháp thuật, linh lực, hồi linh lực, tốc độ vận chiêu, sát thương hệ | Hao MP, hồi chiêu dài, chậm vận chiêu, phản phệ HP |
| Dị khí/hỗn hợp | Roi, xích, kiếm trận khí, khôi lỗi, hồn phiên, cờ trận, trận bàn, pháp luân | Tùy cơ chế vật lý/phép/hỗn hợp, hiệu ứng đặc biệt | Tùy biến thể; không buộc dùng một tập debuff giống nhau |

- **Phẩm chất** Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên **không đồng nhất** với **cảnh giới sử dụng** hoặc **cấp item**.
- Loại vũ khí **không khóa loại sát thương**: các biến thể có thể thiên vật lý, pháp thuật, đa phần, mang thuộc tính ngũ hành hoặc Vô. Nhóm là cấu hình mặc định, từng template/item có thể ghi đè theo thiết kế.
- **Đã dùng được** không có nghĩa **đạt 100% hiệu suất**; thiếu cảnh giới/chỉ số có thể khiến món tốt bị yếu đi hoặc làm yếu cả nhân vật.
- Chỉ số vật lý/pháp thuật và yêu cầu cảnh giới phải phù hợp schema Save/Equip/Forge hiện tại. Không tự đổi enum hoặc reroll affix của item đang lưu.

## III. Võ học, tâm pháp, bí thuật

- Cùng hệ điều kiện chuẩn như trang bị, nhưng phân ba nhóm: **võ học** (đòn/kỹ thuật), **tâm pháp** (nội lực, vận hành, hộ thể, hồi phục), **bí thuật/cấm thuật** (điều kiện đặc biệt cao).
- **Thiếu điều kiện nhưng được thi triển:** có thể tự chịu sát thương khi ra chiêu, hao **thêm % MP**, nhân **hệ số thời gian hồi chiêu**, giảm hiệu suất hoặc có phản phệ theo cấu hình. Không chốt giá trị/xác suất/giới hạn trước khi cân bằng.
- **Điều kiện cứng:** một số kỹ thuật cần cơ chế cảnh giới cụ thể (ví dụ thần thức từ Hóa Thần); sở hữu bí tịch hoặc học chưa đồng nghĩa được thi triển khi chưa đạt điều kiện.
- Yêu cầu phải cho biết điểm nào thiếu và hậu quả khi sử dụng, để người chơi quyết định chủ động.

## IV. World Map — cấu trúc từ ảnh phác thảo 5 vùng màu

**Đã chốt:**
1. Thế giới rộng lớn chia thành **5 đại khu theo 5 vùng màu của ảnh phác thảo**. Mỗi đại khu mang **theme thiên nhiên/khí hậu chiếm ưu thế**, **KHÔNG** giới hạn địa hình, KHÔNG mặc định tương đương đúng một quốc gia. Một đại khu có thể chứa nhiều quốc gia, thế lực, chiến trường, sơn trang và thôn làng.
2. Mỗi quốc gia có nhiều loại địa hình, mục tiêu hỗ trợ **ít nhất 8 loại như hệ hiện tại**; tỷ trọng theme chủ đạo cao hơn nhưng không loại trừ đầm lầy trong quốc gia đồi núi, hang động trên đảo, v.v. **Không ràng buộc độ khó và Level quái theo tên địa hình**. Một kiểu địa hình có thể xuất hiện ở nhiều cảnh giới khác nhau.
3. **Xuất thân ngẫu nhiên:** khi tạo nhân vật, chọn một trong các **điểm khởi đầu hợp lệ trên toàn thế giới**, có thể là kinh thành, làng nhỏ, nhà tranh, sơn trang, vùng biên chiến sự… **Không mặc định khu màu vàng hoặc khu trung tâm**. Mỗi đại khu phải có ít nhất một khởi điểm đủ phù hợp để khởi chơi với quái yếu gần đó, nhưng không nhất thiết yên bình.
4. **Không có cạnh nối trực tiếp từ một map khởi đầu tới map Endgame**. Ngược lại, điểm khởi đầu **được phép nằm cạnh** vùng chiến tranh, vùng tranh chấp hoặc map nguy hiểm chưa thuộc loại Endgame. Nếu cạnh liền kề là map nguy hiểm, phải có cách hiển thị mức nguy hiểm trước khi người chơi di chuyển.
5. Map là **đồ thị các node có cạnh nối đi lại thực tế**, không chỉ màu nền/địa hình. Lối đi đặc biệt có thể yêu cầu khả năng vượt địa hình, và vị trí kề nhau trên ảnh chưa chắc có tuyến đường khả thi.
6. Mỗi node có **dải Level/cảnh giới quái cố định theo map**, số wave tối thiểu và tổng số wave **riêng**, độc lập Level nhân vật. Người chơi mạnh quay về vùng yếu để tìm cơ duyên/event/farm bình thường.
7. **Quy tắc mật độ kết nối:** các node **trung gian thông thường** nên có tối thiểu **2 node mạnh hơn** và **2 node ngang bằng hoặc yếu hơn** (dựa trên cấp/độ khó map), khi topology cho phép. **Ngoại lệ** với điểm khởi đầu, vùng biên/bờ biển, điểm cụt, Endgame/bí cảnh và nơi có chướng ngại đặc biệt. Quy tắc không thể áp cứng cho node yếu nhất hoặc mạnh nhất. Cần kiểm tra graph để không tạo nút cô lập/vùng bất khả thi; không tự kết nối Endgame với điểm xuất thân để đủ bốn cạnh.

**Những gì CHƯA chốt:** tên 5 đại khu/quốc gia, tọa độ, số node, theme gán từng màu, tỷ lệ spawn địa hình, cấp cụ thể của node, phân bố quái, tỷ lệ event và mẫu hình world map hoàn chỉnh. Các địa danh ở các bản trao đổi trước chỉ là *ví dụ*, không khóa vùng vàng thành khởi đầu.

## V. Khám phá và di chuyển — fog of war

- **Mở được World Map ngay từ đầu**, nhưng chỉ có ánh sáng/thông tin đầy đủ tại khu đang đứng và khu đã mở khóa/khám phá trước đó; các vùng xa còn sương mù.
- Khi biết đường sang một node liền kề nhưng **chưa clear tối thiểu node đang đứng**, chỉ cho biết **tên và cảnh giới/cấp khuyến nghị** của node đó. Chưa tiết lộ ranh giới chính xác, số lượng wave phải hoàn thành, mức chi tiết/đường nhánh.
- **Chỉ di chuyển 1 cạnh tới node liền kề**, sau khi đã hoàn thành **ít nhất một lần toàn bộ wave tối thiểu** của map hiện tại. Điều kiện này được lưu vĩnh viễn theo node để về sau quay lại không phải clear lại chỉ để rời đi.
- Khi đã tới/khám phá/map được clear, mở dần giới hạn, ranh giới, thông tin wave, môi trường và đường tiếp theo. Tổng wave, nội dung cơ duyên và tỷ lệ sự kiện vẫn có thể giữ bí mật cho đến khi đạt mốc khám phá phù hợp.
- **Tách trạng thái:** `undiscovered`, `knownNeighbor`, `visited`, `minWavesCleared`, `fullyExplored`; không dùng một bool `unlocked` cho mọi ý nghĩa. Điều kiện vào đặc biệt là một đánh giá độc lập, không làm mất thông tin khám phá đã có.
- Không tự động di chuyển khỏi map và không tự mở toàn bộ láng giềng chỉ vì Level/cảnh giới tăng.

## VI. Vùng nguy hiểm so với map Endgame có khóa cứng

| Loại map | Có được vào khi nhân vật yếu? | Áp lực |
| --- | --- | --- |
| Thông thường | Có, sau khi đã mở đường | Mức quái/wave cố định |
| Nguy hiểm | Có thể liều vào sau khi mở đường | Debuff môi trường: ví dụ chướng khí chậm di chuyển, tăng sát thương phép; băng tuyết giảm HP tối đa, tăng sát thương Thủy nhận vào |
| Endgame hoặc khu có chướng ngại bắt buộc | **Không** nếu chưa đủ điều kiện đặc biệt | Cần pháp bảo chống chướng khí, công pháp/cảnh giới thích hợp, phi kiếm ngự không đủ tầng và MP, hoặc năng lực khác |

- **Không đánh đồng "cảnh giới quái cao" với "Endgame"**: map quái mạnh bình thường vẫn có thể cho phép nhân vật dưới cấp liều vào; map có chướng ngại cứng mới khóa.
- **Map Endgame tuyệt đối không là node liền kề trực tiếp với map khởi đầu**; muốn đến phải trải qua nhiều node, mở đường và vượt điều kiện riêng.
- **Debuff môi trường liên tục** do đứng trong địa điểm không bị vô hiệu chỉ bằng `DebuffDurationReduction` (chỉ số đó có thể giảm thời lượng những debuff có thời gian do đòn đánh gây ra). Cần phân biệt nguồn `environment:persistent` và `combat:timed`.
- Không đặt các hệ số debuff cụ thể khi chưa bàn về hiệu lực, khả năng chống chịu, công pháp bảo hộ và mức phạt theo chênh cảnh giới.

## VII. Kiến trúc & thứ tự triển khai

**Hiện có:** `TuTienEnums.cs` định nghĩa 8 terrain (`DongBang,CaoNguyen,DamLay,RungRam,SaMac,DoiNui,HoNuoc,Bien`) và 6 `RegionTheme` (chủ đề nhóm 5 map), `WorldNames.AssignMap` chọn địa hình/region theo thứ tự map; `CombatManager` sinh cấp quái dựa quanh Level Hero; `EquipmentInstance` có item stats/affix; `CombatStatModifier` có lọc tag. **Chưa có World Map node graph, dữ liệu quốc gia, spawn khởi đầu theo vùng, yêu cầu cảnh giới chung, điều kiện vào Endgame.**

**Khi xây mã:**
1. Định nghĩa **ID node, ID đại khu/quốc gia, theme chủ đạo, địa hình map, nối cạnh và yêu cầu đi lại**, dữ liệu tác giả riêng với trạng thái khám phá được Save. Seed khởi đầu ngẫu nhiên chỉ khi New Game; Save/Load phải giữ đúng điểm xuất thân/đã khám phá.
2. Xây và kiểm thử **graph/map route validator**: có tối thiểu một điểm khởi đầu hợp lệ ở mỗi đại khu; không nối trực tiếp start ↔ Endgame; kiểm tra cạnh 2 mạnh hơn + 2 bằng/thấp hơn cho node nội bộ hợp lệ và báo ngoại lệ; đường đi giữa khu khả thi, không ép mọi node 4 cạnh nếu bản đồ không hỗ trợ.
3. Chọn một lát cắt khu vực thử nghiệm, kết nối wave hiện có, thay rule quái scale với nhân vật bằng **dải Level cố định cho map**, giữ lại các map cũ khi Load.
4. Thêm điều kiện chung, debuff môi trường, vũ khí/võ học/tâm pháp trên schema ổn định, chỉ bổ sung ngẫu nhiên item/Forge khi có thiết kế chỉ số, tỷ lệ và cách Save.
5. Mở rộng môi trường như **núi lửa, hang động, băng tuyết** sau, có thể ban đầu là **biến thể/hiệu ứng của địa hình** để không vội phá enum và asset hiện tại.
6. Hoàn thiện UI/World Map khi asset xong. **Giai đoạn này chỉ là chốt khung**, chưa sinh tự động 90–200 node hay thiết kế tĩnh toàn bộ thế giới.

**Liên quan:** [Realm_Progression_Design_0.0.54.md](Realm_Progression_Design_0.0.54.md), [Stat_System_Design_0.0.54.md](Stat_System_Design_0.0.54.md), [Development_0.0.54.md](Development_0.0.54.md).
