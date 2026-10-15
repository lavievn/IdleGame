# World Map 0.0.54 — tích hợp thử nghiệm (15/10/2026)

> **Trạng thái:** Đã đưa mã và danh mục vào nhánh `feature/0.0.54`; **CHƯA chạy/duyệt Unity Play Mode hoặc Windows build**. Không merge vào `main` trước khi thử thực tế.

## Dữ liệu đã chốt

- **133 map có tên** từ `Docs/World_Map_Monster_Draft_0.0.54.csv`, import thành `Assets/Resources/WorldMapCatalog.txt`. Mỗi map giữ ID, đại khu, tên, địa hình, 7 loài quái đề xuất và 1 nhóm địch Nhân tộc.
- **5 điểm khởi đầu ngẫu nhiên:** TV-20, TL-16, **XD-11** (không phải SM-11), TM-12, HT-17.
- **6 map Boss đặc biệt endgame:** HT-27, TV-39, TM-25, XD-03, TL-02, HT-01. Đánh dấu Boss trong catalog. **Tạm chặn đi vào** cho tới khi chốt điều kiện cảnh giới/pháp bảo/ngự kiếm; không tự thêm điều kiện tu luyện tưởng tượng.
- **XD-17:** Đồi núi / Cao nguyên. **XD-19:** Biển. Thảo nguyên dùng nền Đồng bằng; các biến thể môi trường không tự thêm enum.
- **Đất nước:** chưa có danh sách *quốc gia* riêng/biên giới/tọa độ quốc gia. 5 **đại khu** không được coi là 5 quốc gia.

## Hành vi trong game

1. **Chơi mới:** ngẫu nhiên 1/5 điểm xuất thân, `worldMapVersion = 1`. Save cũ `worldMapVersion = 0` giữ `mapNumber` và tiến trình tuần tự trước đó. **Không có chuyển đổi Save cũ tự động**.
2. **Quái:** `CombatManager.StartBattle` lấy 1 trong 7 loài hoặc 1 vai trò địch Nhân tộc của map; cấp quái lấy theo map, **không theo Level Hero**. Hiện số cấp là giá trị **THỬ NGHIỆM** dựa trên khoảng cách đến điểm xuất thân (`1 + 3 × depth`, Boss = 120); chưa phải phân bố/balancing cuối.
3. **Wave:** lần đầu kết thúc wave tối thiểu 5/5, đánh dấu `worldMapCleared`; **luôn tiếp tục farm cùng node**, wave quay **1 → 2 → 3 → 4 → 5 → 1** vô hạn. Việc clear không tự gọi chuyển map.
4. **Đường đi:** trước khi clear map hiện tại, không hiển thị map kề trong cửa sổ World Map và không thể đi. Sau khi clear, khi **mở Bản đồ** mới thấy tên/level map kề, người chơi bấm chọn mới chuyển; chỉ được đi trực tiếp 1 cạnh. Quay lại map đã mở được. Boss bị khóa cứng.
5. **Save:** `worldMapId`, `worldMapWaveProgress`, `worldMapLoopWave`, `worldMapVisited`, `worldMapCleared`. Trạng thái clear lưu lâu dài, kể cả khi chết/tiếp tục farm.
6. **Thay map:** hủy coroutine wave và projectile/quái cũ trước khi bắt đầu chiến đấu ở map mới; tạm hồi đầy HP khi sang map (chưa chốt chi phí di chuyển/HP).
7. **Thất bại:** ở lại node hiện tại, bắt đầu lại wave 1, giữ các map đã clear và Level/đồ.

## Giao diện bản đồ và mây mù

- Nút **Bản đồ** trong HUD gameplay mở `WorldMapPanelUI`. Giao diện tạm dừng chiến đấu khi mở và trả lại trạng thái pause khi đóng.
- Nền: ảnh **đã chốt**. Dùng menu Unity **Tools → Bach Luyen Tien Do → Import World Map Artwork** và chọn ảnh `WorldMapArtwork.png`; công cụ lưu ảnh dưới `Assets/Resources/WorldMapArtwork.png` với import type Sprite. **Ảnh chưa được push trực tiếp bằng GitHub connector**, cần thực hiện thao tác import này một lần trên máy có Unity, rồi commit asset PNG nếu muốn máy khác sync.
- Mây mù che toàn bộ bản đồ trừ vùng đang đứng / các node đã khám phá. **Bản hiện tại dùng vị trí đánh dấu ước lệ và vùng sáng hình tròn mờ**, chưa phải polygon hex chính xác, vì ảnh raster không chứa tọa độ đỉnh đa giác/quan hệ kề máy đọc được. Đây **không phải phân bố chính xác các map về địa lý**.
- Không hiển thị ranh giới **quốc gia** hay tự sinh ranh giới mới. **Chỉ tên quốc gia đã được cung cấp** trong `Assets/Resources/WorldMapCountries.txt` mới xuất hiện phía trên mây; file này tạm để trống, tránh tự đặt tên hoặc coi tên đại khu là quốc gia. Định dạng từng dòng: `nationId<TAB>tên quốc gia<TAB>u<TAB>v`; u,v từ 0 đến 1, gốc dưới-trái của ảnh.
- Các map chưa mở và chưa kề không lộ tên/wave/ranh giới. **Chỉ sau clear**, danh sách map kề mới hiện trong bảng chọn, dù khu vực chưa ghé vẫn bị mây che.

## Lưu ý cần thiết kế tiếp

- **Đồ thị cạnh tạm thời:** chuỗi node theo ID + cạnh xuyên đại khu để kiểm thử tính năng. **KHÔNG lấy nó làm quyết định biên giới/lục giác chính thức**, bởi không có tọa độ polygon đọc được từ ảnh; chưa kiểm tra quy tắc 2 đường khó hơn + 2 đường ngang/dễ. Boss chỉ có 1 cạnh, không nối trực tiếp điểm khởi đầu.
- **Quốc gia / polygon:** người phát triển cung cấp danh sách quốc gia và phân ranh, liên kết node–quốc gia, tọa độ các lục giác, cạnh tiếp giáp thật. Khi đó thay `BuildTemporaryLinks` bằng graph chính thức và tạo fog theo **hình đa giác**, không dùng các lỗ tròn tạm.
- **Độ khó:** chưa chốt dải cấp map, quái tinh anh/boss, môi trường (độc/băng/lava/biển), hạn chế di chuyển và xác suất spawn. Boss hiện chưa sinh như encounter riêng, chỉ đánh dấu + khóa lối vào.
- **Nhân tộc:** hiện sử dụng hệ controller quái sẵn có và tên theo vai trò, **chưa có AI/công pháp/trang bị/phái riêng**.
- **BGM:** tiếp tục dùng `RegionTheme` tương ứng địa hình để không phá playlist hiện tại; sau này sẽ map đại khu/địa hình/theme chi tiết hơn.

## Những file chính

- `Assets/Resources/WorldMapCatalog.txt` — 133 dòng runtime; `Assets/Resources/WorldMapCountries.txt` — vị trí nhãn quốc gia (chưa gán).
- `Assets/Script/WorldMapCatalog.cs` — node, temporary graph, progression, Save fields, di chuyển.
- `Assets/Script/WorldMapPanelUI.cs` — HUD modal, sương mù, danh sách node liền kề.
- `Assets/Editor/WorldMapArtworkImporter.cs` — import ảnh vào Unity.
- `Assets/Script/GameManager.cs`, `CombatManager.cs`, `SaveManager.cs`, `UIManager.cs`, `Assets/DataScript/EntityDataSO.cs` — đường tích hợp.
- `Tests~/WorldMapRegression.cs` và `Tests~/run.sh` — các bài kiểm thử Unity-stub. **Chưa thực thi ở lượt này**, không coi là pass.

## Checklist kiểm thử trong Unity (chưa nghiệm thu)

1. Cập nhật `feature/0.0.54`, import ảnh PNG từ công cụ Editor, Play Mode; kiểm tra Console không có compiler error.
2. Chơi mới nhiều lần: từng điểm xuất thân nằm trong 5 ID hợp lệ (thứ tự random).
3. Mở Bản đồ trước wave 5: toàn bộ map lạ bị fog, không có danh sách node kề, chỉ vị trí đang đứng rõ.
4. Hoàn thành wave 5: **vẫn đứng cùng map**, wave quay về 1; sau đó mở Bản đồ để thấy map kề.
5. Chọn map kề: chuyển thủ công, no boss; quái mới đúng pool và cấp độc lập với Level Hero.
6. AutoSave / Save1 / Save2 / Load / Tiếp tục / chết / quay lại menu: current node, lịch sử visited/cleared, tiến trình wave không bị rớt; save cũ vẫn hoạt động.
7. Kiểm tra resize 600/800/1150 và click qua native transparent window, mây mù không chặn nút, không sinh event khi chỉ mở bản đồ.
8. Không có tự chuyển map ở bất cứ vòng farm nào; chưa test Boss encounter bởi Boss chưa mở khóa.
