# IdleGame — Khởi động 0.0.54: Inventory / Equipment

**Mốc kế thừa:** 0.0.53 đã được người phát triển nghiệm thu công cụ căn chỉnh Opening UI trong Unity Play Mode ngày 15/10/2026, sau đó nhánh `feature/0.0.53` được fast-forward lên `main` tại commit `aef390677a94a026b77de45e4a137bfddb98e4af` (189 commit so với main cũ, không có xung đột). Nhánh `feature/0.0.54` được tạo từ commit đó. `main` giữ `bundleVersion: 0.0.53`, phiên bản đang phát triển đổi thành `0.0.54`.

## Checkpoint: đã có trong mã trước khi bắt đầu .54

- **Inventory:** cửa sổ kho, ô đồ, bảng chi tiết, so sánh chỉ số trang bị, 8 vị trí trang bị gồm 2 nhẫn độc lập, bảng rèn có modal, trang mặc/tháo và bộ dữ liệu Sprite `InventoryArtTheme`. Kho hiện phân trang, chưa dùng ScrollRect dù có trường texture dành sẵn cho thanh cuộn.
- **Thiết kế UI:** công cụ viền đo kích thước Inventory (màu số đo đỏ) đã có. `UIManager.inventoryUseArtTheme=false` là chế độ sử dụng màu UI mặc định; asset Sprite vẫn lưu để bật lại. Chưa tích hợp công cụ kéo-thả và lưu bố cục trực tiếp trong Play Mode cho Inventory; bộ công cụ đã được nghiệm thu ở Opening là cơ sở tham chiếu.
- **Equipment:** cá thể đồ có mã riêng, phẩm chất Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên, chỉ số chính/phụ, trạng thái dòng `Hidden`/`KnownLocked`/`Active`, nhận diện tiềm năng và trần luyện tự nhiên, Save/Load dữ liệu rèn.
- **Forge thử nghiệm (bộ luật ngày 14/10):** rèn thường +1 lượt mất 1 vật liệu; rèn đặc biệt +1 phẩm mất 10 vật liệu; mỗi lần đủ ngưỡng chỉ tăng một bậc phẩm; ngưỡng thử nghiệm 3/6/12/24/48; dòng phụ chỉ khám phá sau khi tăng phẩm thành công, tụt phẩm chuyển dòng từng biết thành `KnownLocked`. Người phát triển đã kiểm thử một phần và chưa báo lỗi ở các trường hợp được thử.
- **Opening UI 0.0.53:** công cụ Editor căn chỉnh runtime `Tools > Bach Luyen Tien Do > Opening Layout (Play Mode)` cho 13 RectTransform, lưu vào `OpeningArtTheme.asset`, chống reset bố cục và tạo backup trước khi ghi. Người phát triển xác nhận hoạt động tốt và muốn cân nhắc áp dụng phương pháp cho toàn bộ UI.

## Trạng thái UI — đang triển khai, tạm chưa phải đầu việc chính (.54)

- **Inventory / Information / các giao diện cùng nhóm:** bộ asset thiết kế **chưa hoàn thiện**, hiện đang được chuẩn bị. Giữ nguyên layout code, giao diện placeholder và các công cụ đo hiện có; không xem UI art là đã nghiệm thu.
- **Căn chỉnh Play Mode:** phương pháp đã nghiệm thu với Opening ở 0.0.53 và dự kiến mở rộng cho các UI khác, nhưng **hoãn việc chuyển Inventory sang công cụ chỉnh/lưu mới** cho tới khi có bố cục/asset hoàn chỉnh để tránh chỉnh lặp lại.
- **Trạng thái công việc:** ĐANG TRIỂN KHAI / CHỜ ASSET. Không khóa luồng phát triển gameplay, Equipment hay dữ liệu.
- **Mốc quay lại:** khi có khung ảnh, nút và asset thực tế cho Inventory, Information; lúc đó thử trực tiếp trong Play Mode ở ba kích thước cửa sổ trước khi chốt.

## Phần chưa chốt / chưa xây dựng

1. **Inventory UI [đang triển khai / chờ asset]:** hoàn thiện mỹ thuật và bố cục trong cửa sổ 600/800/1150; sau đó xem xét tái sử dụng công cụ Play Mode drag+save theo từng vùng, bảo vệ nút bấm và bố cục responsive. Kho thực tế chưa phải lưới cuộn ScrollRect 50+ ô.
2. **Rèn mạo hiểm:** cơ chế cược có nguy cơ mất vật phẩm vĩnh viễn đã được định hướng, nhưng xác suất thành công, mức mất, nguồn nguyên liệu, cân bằng và giới hạn vượt trần chưa chốt; không được tự thêm RNG hủy đồ vào mã.
3. **Sinh đồ/rơi đồ thực:** bảng loại vật phẩm, ngũ hành, số lượng dòng phụ khi sinh, sức mạnh, tỷ lệ rơi và cách phân rã/chế tác chưa hoàn chỉnh.
4. **Khả năng tương thích:** không được phá schema Save cũ, Modifier/CombatStat, kiểm tra quyền modal của bảng rèn và điều khiển UI của Windows overlay.


## Đầu việc tiếp theo sau khi tạm hoãn UI asset

**Ưu tiên tiếp theo: Equipment và cách cộng chỉ số theo loại sát thương.** Rà soát phân biệt nguồn sát thương vật lý/pháp thuật, giá trị chỉ số gốc và cộng từ trang bị, preview mặc/tháo và Save/Load, rồi thảo luận quy tắc trước khi sửa gameplay. Đây là nền tảng cần ổn định trước khi mở rộng sinh đồ, rơi đồ và võ học.

**Chưa tự triển khai** xác suất thất bại, mất vật phẩm, bảng drop hay random chỉ số mới. Các phần đó còn thiếu quy tắc đã thống nhất; Forge thử nghiệm hiện tại được giữ nguyên.

## 15/10/2026 — Chốt khung chỉ số chung và bốn hướng tu luyện

- Đã thống nhất **phương án B**: đầu tư điểm theo bốn hướng **Công phạt, Hộ thể, Thân pháp, Linh tu**; mỗi điểm cộng nhiều chỉ số trong cùng hướng. Tăng trưởng tự nhiên theo cấp/cảnh giới và điểm tu luyện là hai nguồn riêng.
- UI chính chỉ thể hiện bốn tổng hướng; **hover/chọn vào đúng hướng/chỉ số** mới hiển thị các chỉ số đóng góp và giải thích chi tiết. Thiết kế art của Inventory/Information vẫn đang chờ, **chưa triển khai giao diện mới**.
- **Chính xác thuộc Công phạt**, cơ bản **80%**; khi đánh kẻ cao/thấp cấp hơn thì điều chỉnh **-2/+2 điểm phần trăm mỗi cấp**, giới hạn chênh lệch trong ±5 cấp (tối đa ±10 điểm phần trăm). Tổng Chính xác tối đa **100%**, Né tránh tối đa **45%** (mốc tạm); xác suất trúng = clamp(chính xác hiệu lực − né tránh hiệu lực, 0–100%), chỉ xét một lần. Hệ số Chính xác tăng theo mỗi điểm Công phạt **chưa chốt**.
- Chỉ số hồi phục có giới hạn nhưng **chưa chốt con số**. Tạo một chỉ số chung giảm thời gian **debuff** cho xuất huyết/choáng/làm chậm, chưa chốt mức trần.
- **Kim/Mộc/Thủy/Hỏa/Thổ và linh căn bẩm sinh** giữ nguyên khi sinh ra; không mở nhánh phân điểm hệ, không thêm Chaos. Các hiệu ứng/võ học/trang bị có thể tác động chỉ số hiệu lực, không sửa linh căn gốc.
- Chưa sửa `CombatStats`, `CombatDamage`, `EntityDataSO`, `SaveManager` hoặc sửa gameplay; việc di trú điểm tăng HP/ATK kiểu cũ cần thiết kế trước. **Chi tiết:** [Stat_System_Design_0.0.54.md](Stat_System_Design_0.0.54.md).

## 15/10/2026 — Cảnh giới và Thiên Kiếp: xác nhận phương án B

- **Level và cảnh giới độc lập:** EXP/Level tiếp tục tăng cả khi chưa đột phá. Cảnh giới chỉ lên khi vượt Thiên Kiếp thành công, không còn tự suy ra từ `(Level-1)/10`.
- **Khung 30 cấp nội bộ mỗi đại cảnh giới:** 1–9 Sơ kỳ, 10–19 Trung kỳ, 20–29 Hậu kỳ, 30 Hậu kỳ viên mãn/Bình cảnh. Chuyển Sơ → Trung → Hậu **tự động**, **không có Thiên Kiếp nhỏ**. **Thiên Kiếp chỉ xuất hiện tại bình cảnh để lên đại cảnh giới mới**. Khi thành công, ghi lại Level hiện có làm mốc tiến trình mới; Level farm dư trước đột phá không bỏ qua các kỳ. Không cần hệ 9 tầng độc lập ở bản đầu.
- **Thiên Kiếp ý tưởng ban đầu:** hai chặng 10 giây chống chịu và 10 giây phản công; chưa chốt hệ số, phần thưởng và hình phạt khi thất bại.
- **Trang bị vượt yêu cầu** vẫn có thể mặc nhưng chịu debuff/giảm hiệu suất; **một số võ học** có điều kiện mở khóa cứng (ví dụ Thần thức đòi Hóa Thần).
- Đây mới là chốt **mô hình tiến trình**, chưa sửa mã hoặc luật Save. Xem [Realm_Progression_Design_0.0.54.md](Realm_Progression_Design_0.0.54.md).

## 15/10/2026 — World Map, vũ khí, võ học và điều kiện tiếp cận: khung đã chốt

- **Vũ khí:** mở rộng thành nhiều loại/biến thể; gom theo nhóm cận chiến vật lý, tầm xa vật lý, pháp khí/pháp bảo, hỗn hợp/dị khí. Mỗi item có chỉ số/biến thể/ngũ hành, yêu cầu cảnh giới/chỉ số và debuff thích hợp khi sử dụng vượt khả năng; khoảng roll, cân bằng và drop **chưa chốt**.
- **Võ học/tâm pháp:** dùng chung bộ kiểm tra điều kiện, cho phép phản phệ HP, tốn thêm % MP, kéo dài hồi chiêu khi thi triển vượt yêu cầu; một số thuật đặc biệt có yêu cầu cứng theo cảnh giới.
- **World Map:** theo ảnh phác thảo **5 đại khu màu**, mỗi đại khu thiên về một theme nhưng không loại trừ các địa hình khác; một đại khu có thể chứa nhiều quốc gia. Mỗi quốc gia có nhiều địa hình (ít nhất 8 dạng hệ hiện tại), **không gán độ khó theo địa hình**.
- **Ngẫu nhiên xuất thân:** bất kỳ điểm khởi đầu hợp lệ nào trong nhiều đại khu (thành lớn, thôn, sơn trang, biên giới chiến tranh); **không mặc định bắt đầu ở vùng vàng**. Map Endgame **không nằm cạnh trực tiếp** map khởi đầu, nhưng map nguy hiểm/chiến sự thông thường có thể kề.
- **Đồ thị khám phá:** sương mù; map kề chưa mở chỉ thấy tên/cảnh giới, không thấy ranh giới/số wave; chỉ đi một bước sang node liền kề sau khi đã clear **wave tối thiểu** của node hiện tại ít nhất một lần. Đa số node trung gian có ≥2 đường tới vùng khó hơn và ≥2 đường tới vùng ngang/thấp, **ngoại lệ** hợp lý ở start/biên/endgame. Lưu lịch sử khám phá, đường mở và clear tối thiểu.
- **Quái cố định theo map**, không scale theo Level Hero; nhân vật mạnh có thể quay lại vùng thấp farm/cơ duyên/event.
- **Endgame hoặc chướng ngại đặc biệt:** cần điều kiện cứng (pháp bảo chống chướng khí, công pháp/pháp thuật ngự không, phi kiếm đủ tầng, MP...), **không thể chỉ chịu debuff để lách qua**. Phân biệt rõ map mạnh nhưng vẫn vào được với map có khóa tiếp cận.
- **Trạng thái:** đã chốt định hướng khung, **chưa chốt** tên quốc gia, số node, vị trí, tỷ lệ theme, điều kiện chi tiết, xác suất, hệ số phạt và cân bằng. Chưa sửa gameplay/Save hoặc xây graph. Xem [World_Map_Equipment_Requirements_0.0.54.md](World_Map_Equipment_Requirements_0.0.54.md).

## 15/10/2026 — Danh mục map, địa hình và quái vật (đã lưu bản nháp)

- Đã lưu **133 map ứng viên** thuộc 5 đại khu, mỗi map có tên, mã ID và địa hình; bộ ứng viên được **mở rộng từ 4 lên 7 loài quái không trùng nhau/map, cộng 1 nhóm địch Nhân tộc**. Tổng cộng 931 ô loài quái và 133 ô loại địch Nhân tộc (không phải 1.064 loài khác nhau). Dữ liệu nguồn được tổng hợp từ ảnh bản đồ/danh mục AI; đây là **bản tạm cho người phát triển rà soát, phân bổ và bổ sung**.
- **Tệp đọc:** [World_Map_Monster_Draft_0.0.54.md](World_Map_Monster_Draft_0.0.54.md). **Bảng chỉnh sửa:** [World_Map_Monster_Draft_0.0.54.csv](World_Map_Monster_Draft_0.0.54.csv).
- **15/10/2026 – rà soát lần 1:** đã cập nhật 6 mã `XD-17`, `HT-17`, `HT-25`, `TM-13`, `TM-14`, `TM-21` theo người phát triển; trong đó HT-25 giữ Đồi núi (Cao nguyên cũng được), TM-13 giữ Rừng rậm (Đầm lầy cũng được), TM-14 đặt Thác nước (biến thể địa hình chưa có TerrainType riêng). Các pool quái của 4 map đổi địa hình cũng đã điều chỉnh, chưa là dữ liệu spawn chính thức.
- **15/10/2026 – rà soát lần 2:** người phát triển xác nhận thêm 8 mã: TV-05 Đồi núi, TV-23 Rừng rậm, TL-13 Đồng bằng, TL-23 Đồng bằng, TL-25 Thảo nguyên (biến thể của Đồng bằng), TL-28 Rừng rậm, XD-17 Cao nguyên, TM-14 Đồi núi (Thác nước là cảnh quan). Cập nhật danh sách quái ứng viên tại TL-25, TL-28, TM-14.
- **15/10/2026 – xác nhận mã Xích Diễm:** `XD-17` là Lưu Diễm Tràng, địa hình hỗn hợp **Đồi núi / Cao nguyên**; `XD-19` là Diễm Ảnh Quần Đảo, địa hình **Biển**. Vấn đề trùng mã từ ảnh AI đã giải quyết; CSV không còn cảnh báo từng dòng. Các node thiếu tên từ bản đồ tổng và bản ảnh trùng `TV-10` vẫn ghi riêng trong tài liệu, danh mục 133 map chưa là số lượng cuối cùng.
- **15/10/2026 – mở rộng quái đợt 3:** từng map được bổ sung thêm đúng 3 loài quái từ pool sinh cảnh trong `TuTienEnums.cs`, không lặp lại 4 loài ban đầu và không chọn `Long` làm quái thường; kèm 1 loại đối thủ **Nhân tộc** thay đổi theo map/địa hình (sơn tặc, tà tu, hải tặc, tán tu tranh bảo...). CSV đã tách riêng cột quái gốc, bổ sung, Nhân tộc. Đây là danh mục *ứng viên*; chưa buộc mọi map spawn tất cả, chưa chốt phe phái/tỷ lệ.
- **Ghi chú lịch sử trước đợt tích hợp .54:** danh mục ban đầu chưa áp dụng vào spawn, world graph, gameplay hay Save. Trạng thái triển khai mới nhất nằm ở phần 16/10 bên dưới; cấp quái thử nghiệm và đồ thị thử nghiệm **không phải cân bằng/địa lý cuối cùng**.

## 16/10/2026 — World Map đã tích hợp bước đầu: viền tương tác lục giác

- World Map đọc danh mục **133 map**, các map Boss/endgame đã chốt và **5 điểm sinh** `TV-20`, `TL-16`, `XD-11`, `TM-12`, `HT-17`. New Game dùng World Map nếu tải dữ liệu thành công; Save cũ vẫn giữ chế độ tuần tự .53.
- Sau 5 wave tối thiểu: **đánh dấu clear map**, Hero **đánh tiếp wave 1–5 vòng lặp**, không tự nhảy map. Chỉ thấy các ô liền kề có quyền khám phá sau khi mở bảng Bản đồ; click ô chỉ chọn, bấm nút di chuyển mới đổi node. Boss endgame chưa đủ điều kiện bị khóa.
- Bản đồ UI có ảnh nền tùy nhập dưới `Assets/Resources/WorldMapArtwork.png`, fog phủ toàn bộ; vùng đã khám phá lộ ra qua **đa giác 6 đỉnh thực**; không dùng lỗ tròn ước lượng. Quốc gia (khi có tên/tọa độ) hiển thị trên fog, không dựng biên giới quốc gia bằng code.
- `WorldMapPolygonEditorWindow` cho phép click/kéo 6 đỉnh, **dính góc ô kề** và lưu tọa độ 0–1 theo mã vào `Assets/Resources/WorldMapPolygons.json`. Có test trường hợp đa giác lệch, lõm, cạnh chung.
- **Đã bổ sung sau xác nhận 2 ảnh (16/10):** tọa độ bản nháp **133/133 đa giác có 6 đỉnh, 0/133 được duyệt**, nằm trên nguồn ảnh có mã `imagegen.png` (**1448×1086**). Sáu mã còn là vị trí suy luận do chữ bị thiếu/trùng: `TL-01`, `TL-17`, `TL-19`, `HT-13`, `TM-03`, `TM-13`. Dữ liệu được ghi ở `Assets/Resources/WorldMapPolygons.json` cùng tâm nguồn `WorldMapPolygonSeeds.txt`; các đa giác không chồng lấn nhau, nhưng **chưa căn chính xác đường trắng trên ảnh**. Đồ thị đi lại thử nghiệm nay chọn 4 tâm gần nhất trong đại khu, nối vùng kề theo tâm; chưa là đồ thị cạnh chung cuối cùng.
- **Hai ảnh không tương thích tọa độ:** bản lục giác không đều `irregular_hexes_of_the_fantasy_realms.png` (**1536×1024**) có phân ô khác so với bản có mã. Không được dùng JSON của ảnh có mã để nhấn lên bản irregular. Game kiểm tra kích thước trước khi kích hoạt hitbox. Gói ZIP gồm hai PNG nguồn gốc + tọa độ được lưu tại Library `IdleGame/0.0.54/Drafts/WorldMap_0.0.54_Unity_Import.zip`; hiện chưa commit hai tệp PNG nhị phân vào GitHub trực tiếp.
- **Kiểm thử chưa hoàn tất:** đã bổ sung test dữ liệu và đa giác, kiểm tra bằng công cụ Python offline không trùng vùng; **chưa chạy Unity Play Mode/Windows hoặc `Tests~/run.sh` trong môi trường trợ lý**. Cần người phát triển giải nén ZIP vào project và chỉnh/duyệt viền ở Unity Editor.
- Hướng dẫn sử dụng: [World_Map_Polygon_Interaction_0.0.54.md](World_Map_Polygon_Interaction_0.0.54.md).

## Nguyên tắc phát triển .54

- Chỉ chỉnh mã trên `feature/0.0.54`; `main` là checkpoint 0.0.53.
- Trước khi đổi hành vi Forge/Equipment, thảo luận và chốt chi tiết thiết kế; các tham số 3/6/12/24/48 chỉ là giá trị thử nghiệm.
- Giữ cách căn chỉnh trực quan ở Play Mode là **phương án ưu tiên nghiên cứu** cho UI tiếp theo; không tự sao chép nguyên `OpeningCinematicUI` sang kho vì Inventory thay đổi theo modal/trang và tỉ lệ cửa sổ khác nhau.
- Duy trì test hồi quy tại `Tests~` và Unity Play Mode/Windows trước khi nghiệm thu. Những test chưa được chạy ở môi trường trợ lý không được ghi là đã qua.
- Nếu người phát triển có file `OpeningArtTheme.asset` đã chỉnh trên máy nhưng chưa push được, dữ liệu tọa độ cục bộ đó KHÔNG nằm trong GitHub; cần đồng bộ sau, tránh ghi đè toàn bộ asset mà không đối chiếu diff.

**Tài liệu liên quan:** `Docs/Forge_Test_0.0.52.md`, `Docs/Equipment_Affix_Discovery_0.0.52.md`, `Docs/Equipment_0.0.52_Schema.md`, `Docs/UI_Debug_Workspace_0.0.52.md`, `Docs/Opening_PlayMode_Layout_0.0.53.md`.
