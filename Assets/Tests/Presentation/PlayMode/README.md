# Kiểm thử vị trí B2 bằng Unity Play Mode

Chưa chạy hoặc biên dịch bằng Unity Editor trong Cloud. Ba ca NUnit dùng
`TestCase`/`Assert.That`, không dùng `Assert.Multiple` hay `NonParallelizable`.
Assembly chỉ dành cho test; truy cập Assembly-CSharp và Image qua reflection.

Chạy trong scene không có manager/actor gameplay. Fixture từ chối scene đang có
SaveManager, GameManager, CombatManager, EnvironmentManager, HeroController hoặc
MonsterController, kể cả inactive. Không có prebuild hook, chuyển scene hay I/O.
Fixture không tạo SaveManager và không gọi New Game/Load/Reset.

Các ca đồng bộ dùng Instantiate, Awake trì hoãn và Canvas thật: gọi hai đồng đội,
kiểm tra biên toàn bộ Image (kể cả phần alpha), parent/anchor/pivot, Image độc lập,
không chồng nhau và không đổi Hero/camera target/focus lúc gọi. GameManager và
CombatManager bị disable trước khi kích hoạt để không tự chạy Start/Update;
chỉ khởi tạo CombatManager theo lời gọi tường minh. Dọn đúng các object đã tạo.

Ba tỷ lệ Canvas tương ứng 600/800/1150 được kiểm tra bằng Canvas thật, không phải
resize cửa sổ Windows. Chưa bao phủ render, chuyển động nhiều frame, combat,
Retry/Load hay camera handoff thật. Cần kiểm thử gameplay theo checklist trong
`Tests~/PositionHotfix.md`; PASS của fixture không thay thế nghiệm thu này.

## W2.2j — giới hạn bước di chuyển

Thêm 6 ca NUnit đồng bộ dùng cùng fixture cô lập, giữ các assertion B2 trước
đây: 3 thế đánh qua VFX thật rồi truyền dt 0.016/0.033/0.1/0.28/0.016; 3 tỷ lệ
Canvas cho đội B2 ba Hero. Kiểm tra giới hạn runtime mặc định 1/30 giây, quãng
dịch chuyển logic/screen và không có bước tăng tốc bù. Không tạo SaveManager,
không truy cập filesystem, không thay đổi Time.timeScale.

**Các ca mới chưa được biên dịch/chạy bằng Unity Editor trong Cloud.** Chúng
truyền dt tường minh vào TickMovement; không tạo stall CPU/GPU 280 ms thật,
không chạy vòng entry đầy đủ hoặc camera nhiều frame. Vẫn cần nghiệm thu native
New/Continue/Load/Travel/Retry/Reset, camera sau VFX và Windows Player; xem
`Tests~/HeroMovementSpike.md`. Chỉ dùng TestCase/Assert.That đã có trong fixture,
không dùng các API NUnit từng không tương thích ở W1.6.

## W2.2n/o — import artwork môi trường

`PlainsPngImportTests` (4 ca) và `TerrainPngImportTests` (8 ca Forest/Mountain)
chỉ đọc Resources; không tạo manager, hook, truy cập save hoặc đổi scene.
Kiểm tra single/full-rect sprite, kích thước nguồn, mesh, mipmap, Clamp và CPU
readability. Dùng TestCase/Assert.That cơ bản. Chưa compile/chạy bằng Unity Editor
trong Cloud. Không kiểm chứng alpha sampling, đường nối tile hay chuyển cảnh;
xem `Docs/W22oMultiTerrain.md` để nghiệm thu trong Unity/Windows Player.

## Hotfix seam background2/background3

`TerrainSeamNativeTests` thêm 6 ca Canvas/RectTransform native, không truy cập save.
Scene phải không có manager/actor gameplay; fixture chỉ tạo Canvas/ảnh của mình,
EnvironmentManager inactive trước AddComponent và không bao giờ chạy Awake.
Kiểm tra mép render, scale ngang, pan/recycle hai chiều và resize; không tạo Hero,
GameManager hoặc SaveManager. Ba projection mô phỏng bằng Canvas scale, không đổi
kích thước cửa sổ Windows. Chưa chạy/compile bằng Unity thật trong Cloud; các ca
hình học không thay nghiệm thu rasterization/alpha trong Player.
Xem `Docs/TerrainSeamHotfix.md` để bật log Editor và nghiệm thu hình ảnh.
