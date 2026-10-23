# W2.2n — Artwork Đồng Bằng

## Nguồn và phạm vi

HEAD artwork: `8cfdc93` (`add art bg`). Bốn ảnh thực tế ở
`Assets/StreamingAssets/ArtSource/Plains/`, tên bầu trời là `sky.png`;
không có `Assets/ArtSource/Plains/sky(1).png` trên nhánh hiện tại.
Giữ nguyên bốn file nguồn; sao chép byte-for-byte vào
`Assets/Resources/Environment/Plains/`. Thêm metadata cho nguồn và bản dùng runtime.
Nguồn nằm trong StreamingAssets nên vẫn được đóng gói dạng file thô, ngoài bản
texture trong Resources; chưa di chuyển nguồn để tránh sửa bố cục do người dùng upload.

Trước sửa, GroundPresentation dùng chung bộ Ground từ scene, hai silhouette
Resources/Parallax (Inspector override → Sprite → preview TextAsset → fallback),
và các prop Grass. Không có chọn artwork theo địa hình.

Sau sửa, chỉ khi `CombatManager.CurrentHeroCombatState.Data.mapTerrain == DongBang`
và đủ cả bốn Sprite thì dùng bộ mới. Đọc dữ liệu Hero chính đã được SetupHeroInfo
thiết lập; không sửa GameManager/CombatManager. New Game, Continue, Load, Travel,
Retry đi qua cùng việc bind dữ liệu này. Các địa hình khác dùng đúng bộ cũ.
Thiếu một ảnh thì dùng trọn bộ cũ, không phối bộ ảnh dở dang.

## Kích thước và lớp vẽ

| Nguồn | Layer | PNG | Bước lặp ngang ở BattleArea rộng 1920 | Tốc độ so Ground |
|---|---|---|---|---|
| Ground.png | Ground | 2172×530 RGBA | 1920 | 100% |
| Ground2.png | Background1 (near/farTiles) | 2172×724 RGBA | 2496 (1,3×) | 30% |
| Ground3.png | Background (distantTiles) | 2172×724 RGBA | 3244,8 (1,69×) | 10% |
| sky.png | Sky (mới) | 2172×274 RGBA, opaque | 4800 (2,5×) | 0% |

Thứ tự: Sky → Background → Background1 → Ground → actor sắp theo chân Y.
Các Image background cũ được tái sử dụng, thay sprite; root Image Ground vẫn tắt.
Các prop Grass thử nghiệm tắt trên Đồng Bằng, hiện lại khi sang địa hình khác.
Đồng Bằng dùng màu trắng để giữ màu ảnh; tint độ khó/màu Ground cũ vẫn được ghi nhớ
và khôi phục trên các địa hình dùng renderer cũ.

Mỗi lớp gồm ba Image, bước lặp như bảng. Chiều rộng hình = bước lặp + 1 đơn vị
(chồng biên tránh khe hình học do số lẻ); chiều cao = chiều rộng × tỷ lệ PNG gốc.
Ví dụ Ground 1921×468,75; Background1 2497×832,33;
Background 3245,8×1081,93; Sky 4801×605,65. Không cắt phần alpha, không kéo méo.
Ground giữ đáy của dải cỏ cũ. Hai lớp xa giữ baseline từ parallaxBaseHeightRatio.
Không sửa size/position/pivot/scale của BattleArea, Hero hay Monster.

Sky đứng yên theo viewport, không đưa amount pan vào vị trí. Lớp này neo trên theo
chiều cao viewport gameplay quy đổi qua tỷ lệ chiều rộng BattleArea; KHÔNG dùng
chiều cao 100 đơn vị của sàn. Khi mở inventory, dùng kích thước gameplay compact,
không lấy phần cửa sổ mở rộng làm chiều cao thế giới.
`skyScale` (mặc định 2,5; tối thiểu 1) và `skyOffset` trên component GroundPresentation
cho phép chỉnh trong Inspector lúc chạy. Component được cài lúc chạy như trước;
giá trị tinh chỉnh trong Play Mode không tự lưu vào scene.
Ở viewport 16:9 rộng logic 1920, Sky cao khoảng 605,65/1080 đơn vị: nó phủ phần
trên, không chủ đích che toàn bộ chiều cao. Background và Ground phủ phía dưới.
Ba tile phủ ngang kể cả khi giảm scale về 1. Offset mặc định 0 giữ sát mép trên;
người chỉnh offset có thể chủ động đưa mép Sky ra khỏi viewport.

Import: single Sprite full rect, mesh FullRect, không tạo physics shape, mipmap tắt,
max 4096 (không hạ ảnh rộng 2172 xuống 2048), không compression/NPOT resize,
filter Bilinear, Clamp; alphaIsTransparency=0 để không tự sửa RGB vùng alpha.
Mỗi texture tải một lần khi Install; không decode/load texture hoặc tạo thêm
GameObject trong Scroll/Update. Chỉ pha lặp và kích thước/vị trí Image được cập nhật.
Đổi địa hình không rút số ngẫu nhiên hay thay vị trí actor.

## Giới hạn đã thấy trong artwork

Kiểm tra trực tiếp PNG: Ground có chi tiết màu bất thường ở phần trên;
Ground2 và Ground3 có nhiều mảng màu/khối/alpha bất thường rất rõ. Đây là dữ liệu
có sẵn trong nguồn, không phải xử lý importer. Giữ nguyên, không sửa alpha/RGB,
không che bằng crop hoặc tái tạo ảnh. Cần người dùng đánh giá artwork trong Unity.

Hai cột mép trái/phải KHÔNG khớp. Sai khác tuyệt đối trung bình RGBA (0–255):
Ground ≈ (45,40; 37,43; 26,18; 79,00), Ground2 ≈ (71,26; 43,30; 35,67; 25,16),
Ground3 ≈ (58,32; 41,25; 34,42; 87,19), Sky ≈ (56,50; 26,70; 3,86; 0).
Tiling chỉ bảo đảm các khung Image phủ liền; KHÔNG chứng minh artwork seamless.
Các phần trong suốt có thể lộ lớp sau; không khẳng định không có lỗ màu/họa tiết.
Không sửa artwork trong task này.

## Kiểm chứng Cloud

- `bash Tests~/run.sh --plains`: 8 PASS. Sprite giả đúng kích thước nguồn;
  kiểm tra mapping, draw order, màu, alpha rect/aspect, coverage hình học, parallax,
  Sky 600/800/1150 và resize chiều cao, vị trí actor, terrain fallback,
  không thêm resource load/GameObject khi cuộn hoặc đổi map.
- UnityStub bổ sung mô hình sibling order (trước đó SetSiblingIndex không làm gì),
  để assertion thứ tự lớp có ý nghĩa. Đây không phải Canvas renderer của Unity.
- Các nhóm multi-zone, spawn, map-entry, position-hotfix, ranged-lanes, formation,
  sprites, magic-persistence, audio-volumes, desktop-ui, opening-font,
  movement-spike, render-trace và distance-waves đều PASS riêng.
- Toàn bộ runner: 646 PASS rồi bị chặn bởi lỗi cũ MissingMemberException
  `GroundPresentation.fixedRect` tại Tests~/TerrainRegression.cs:41;
  các bài phía sau chưa thực thi. Không sửa/bỏ assertion cũ trong task này.
- Compile Mono với UNITY_EDITOR: 16 cảnh báo hiện có. Với UNITY_STANDALONE_WIN:
  3 cảnh báo hiện có. Không tương đương build Windows Player.
- Bổ sung bốn ca NUnit native `PlainsPngImportTests` trong assembly PlayMode hiện có:
  kiểm tra kích thước gốc, single/full rect, 4 vertices, mipmap, unreadable và Clamp.
  Không tạo manager, không đọc/ghi save. Chỉ dùng API NUnit cơ bản tương thích.
- Không có Unity Editor trong Cloud: chưa chạy/biên dịch native NUnit, chưa nghiệm thu
  Play Mode/Windows Player; không kết luận về đường nối/màu/texture sampling native.

## Checklist nghiệm thu Unity/Windows

1. Mở project, chờ import; chạy bốn ca PlainsPngImportTests. Xác nhận không có lỗi
   importer/compile. Địa hình không phải Đồng Bằng phải vẫn hiển thị bộ cũ.
2. Vào Đồng Bằng qua New Game/Continue; xem Hierarchy/Image sprite theo bảng, Sky
   ở dưới cùng, raycast tắt, không còn prop cỏ cũ chồng hình. Kiểm tra các mảng màu
   bất thường của nguồn và quyết định artwork có phù hợp không.
3. Resize 600 → 800 → 1150 và mở/đóng inventory: Sky vẫn sát mép trên vùng game,
   tỷ lệ không đổi, không lộ khoảng trống trên/hai bên. Không chỉnh camera để bù ảnh.
4. Cho camera cuộn lâu theo hai hướng; xem parallax 30%/10%, khung tile không có khe;
   ghi nhận đường nối họa tiết/alpha (nguồn chưa seamless). Sky không cuộn theo Ground.
5. Kiểm tra Hero/Enemy/B2, Attack/Hit và VFX xuất hiện: vị trí chân và hitbox giữ nguyên,
   nhân vật không bị background che. Profiler không thấy load texture lặp mỗi frame.
6. Travel Đồng Bằng ↔ địa hình khác, Load thủ công, Return Menu → Continue,
   Game Over → Retry: bộ nền đổi đúng, không chớp lớp cũ trên ảnh mới, không hồi quy
   camera/VFX. Dùng dữ liệu thử đã sao lưu, không xóa save người chơi để kiểm tra.
