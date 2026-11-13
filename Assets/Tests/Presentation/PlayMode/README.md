# Kiểm thử vị trí B2 bằng Unity Play Mode

## Hàn Vụ Cloud polish — 12/11/2026

`HudW3Tests` dùng actor triển khai tổng hợp để kiểm tra đúng hợp đồng HUD hiện
hành: chỉ hiện 1/2/3 Hero đang sống, xếp liền từ phải sang trái, ẩn slot gục/thu
hồi, hồi sinh và rebind dữ liệu không giữ tên/HP cũ, chiều cao bằng 0 khi không
còn actor. Các assertion placeholder `Chưa có Hero`, `Ô trống`, đường dẫn
`Mana Disabled` và bố cục trái sang phải thuộc HUD cũ đã được thay theo đặc tả
Hàn Vụ và mã runtime hiện tại. Fixture truyền đủ tham số `BeginLife` và vẫn
không tạo SaveManager hoặc chạy save I/O.

Thêm ca hội thoại giữ nguyên khi menu tạm che HUD, nhưng đóng ở ranh giới entry/
Retry, không ghi lời kể hoặc tự nhận nhiệm vụ. `HanVuOriginSmoke` có thêm bước
đóng trước câu trả lời của tiều phu rồi Load, sau đó nói chuyện lại. Smoke này
**không chạy trên Cloud** vì có save I/O; chỉ chạy ở fixture cô lập đã có.

Roslyn đã biên dịch runtime và file test HUD với DLL Unity 6000.5.0f1. Unity Play
Mode, render chữ, chuột thật và Windows Player của thay đổi mới **NOT TESTED**.
Kiểm thử thuần dữ liệu không cần Editor: `bash Tests~/run-hanvu-story.sh`, dùng
compiler C# 7.2+ và Mono, có thể cấu hình `CSC`/`MONO`. Xem báo cáo
`Docs/automation-reports/CLOUD_HAN_VU_STORY_POLISH_REPORT.md` để phân biệt kết quả.

## Hiệu ứng nhận đồ — 04/11/2026

`LootDropPresentationTests` có 5 ca native mới: 3 tỷ lệ Canvas cho sprite/rơi/
chờ 0,5s/thu nhỏ và bay về mép màn hình, 1 ca coroutine giới hạn/dọn graphics,
1 ca guard template/ID/RNG. Dùng scene trống; không tạo manager/SaveManager,
không save I/O. **Chưa compile/chạy trong Unity ở Cloud.** Xem hướng dẫn và
checklist gameplay tại `Docs/LootDropPresentation.md` ở gốc repo.

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

`PlainsPngImportTests` (4 ca) và `TerrainPngImportTests` (16 ca Forest/Mountain/Desert/Swamp)
chỉ đọc Resources; không tạo manager, hook, truy cập save hoặc đổi scene.
Kiểm tra single/full-rect sprite, kích thước nguồn, mesh, mipmap, Clamp và CPU
readability. Dùng TestCase/Assert.That cơ bản. Chưa compile/chạy bằng Unity Editor
trong Cloud. Không kiểm chứng alpha sampling, đường nối tile hay chuyển cảnh;
xem `Docs/W22oMultiTerrain.md` để nghiệm thu trong Unity/Windows Player.

Desert/Swamp dùng đúng tên file nguồn, kể cả `Swamp/Ground_s2` viết hoa chữ G.
Tám ca import mới giữ nguyên assertion của Forest/Mountain, chưa chạy bằng Unity.
Mapping, map thật và giới hạn artwork xem `Docs/DesertSwampIntegration.md`.

## Hotfix seam background2/background3

`TerrainSeamNativeTests` thêm 6 ca Canvas/RectTransform native, không truy cập save.
Scene phải không có manager/actor gameplay; fixture chỉ tạo Canvas/ảnh của mình,
EnvironmentManager inactive trước AddComponent và không bao giờ chạy Awake.
Kiểm tra mép render, scale ngang, pan/recycle hai chiều và resize; không tạo Hero,
GameManager hoặc SaveManager. Ba projection mô phỏng bằng Canvas scale, không đổi
kích thước cửa sổ Windows. Chưa chạy/compile bằng Unity thật trong Cloud; các ca
hình học không thay nghiệm thu rasterization/alpha trong Player.
Xem `Docs/TerrainSeamHotfix.md` để bật log Editor và nghiệm thu hình ảnh.


## W3.2.4 — weapon basic attack

`WeaponBasicAttackTests` thêm **6 ca** pure-data với JsonUtility thật của Unity:
Sword/Bow/Staff và hai nguồn Attack, JSON round trip trong bộ nhớ, Compare không
mutation và ID enum/legacy Sword. Không tạo SaveManager, không save I/O, không
chứng minh render hoặc combat nhiều frame. B2SpawnPositionTests chuyển primary
mode bằng equip Bow/Staff thực, giữ các assertion vị trí/VFX đã có.
**Chưa compile/chạy Unity Editor trong Cloud.** Xem checklist gameplay và phạm vi
Mono tại `Docs/W3_2_4_WEAPON_BASIC_ATTACK.md` ở root repository.
