# Hotfix seam/overlap background2 và background3

Nền đã fetch: `ace5e2e` trên `feature/w2-combat-presentation`. Giữ nguyên bốn
commit mới của người dùng, gồm artwork seamless, tốc độ/scale mới và zoom map.

## Bằng chứng và giới hạn kết luận

- Mã trước `7d85f50` vẽ background rộng `width + 1` nhưng bước lặp là `width`:
  đây là overlap 1 đơn vị có chủ đích, không phải khe hở.
- `7d85f50` đã bỏ overlap đó ở hai lớp xa. Tuy nhiên ở nền `ace5e2e`, Ground/Sky
  vẫn cộng 1; vị trí và wrap của background vẫn lấy `BattleArea.Width * layerScale`,
  không đo width UI sau `localScale`.
- Đã chạy test mới trên mã GroundPresentation nguyên bản `ace5e2e` (chỉ dùng
  UnityStub bổ sung API để compile code zoom mới). Với W=1000, farLayerScale=1,3,
  tile.localScale.x=1,37, ảnh rộng 1781 nhưng bước lặp 1300: hai tile chồng 481.
  Test thất bại `farTiles ... -500 != -19`. Cùng test đạt sau sửa.
- Nếu tile đều scale=1 trên `ace5e2e`, hai background đã dùng width bằng bước lặp;
  chưa có log Unity mới để khẳng định nguyên nhân vệt người dùng quan sát trong
  trường hợp đó. Có thể đang chạy build trước `7d85f50`, có scale khác 1, hoặc còn
  yếu tố render/artwork khác. Không coi các khả năng này là kết luận.

## Đo và đặt tile

Runtime chỉ sửa `Assets/Script/GroundPresentation.cs`.

Các tile được cài trực tiếp dưới BattleArea, cùng anchor, pivot X=0, Image Simple,
`preserveAspect=false`. Metadata artwork hiện tại là Single Sprite FullRect,
không crop/trim atlas. Không sửa PNG hoặc metadata artwork trong hotfix này.

`DisplayRect()` đọc `Image.GetPixelAdjustedRect()`, tức rect UI đang được hiển thị.
Width trong hệ BattleArea = `displayRect.width * abs(tile.localScale.x)`.
`TileEdges()` lấy hai mép từ localPosition, displayRect.xMin/xMax và scale X;
không dùng độ rộng pixel bitmap làm bước đặt UI. Các layer không có rotation/shear;
Canvas/BattleArea chung có thể scale theo cửa sổ mà không đổi chuẩn đo local.
Canvas gameplay hiện tại không bật pixelPerfect. Nếu đổi các điều kiện này, cần
đánh giá lại mesh/width hiển thị; không suy rộng phép đo trục X cho tile bị xoay.

`JoinTiles()` đặt startX tile sau bằng endX đo được của tile trước. Dùng cùng
phép đo cho layout đầu, period/span, bounds ra khỏi viewport và recycle:

- Cuộn trái: tile có xMax ra khỏi mép trái chuyển tới đúng xMax của tile ngoài cùng
  bên phải. Những tile còn nhìn thấy không bị đổi chỗ theo chỉ số cũ khi phase wrap.
- Cuộn phải: tile ngoài cùng bên phải chuyển tới trước xMin của tile trái nhất,
  sau đó nối lại trái→phải theo mép thực tế, xử lý làm tròn phép trừ float mà không
  cộng/trừ epsilon.
- Pan rất lớn bỏ nguyên vòng lặp các ảnh giống nhau bằng modulo tổng width đã đo,
  rồi recycle phần còn lại; không chạy vòng lặp một lần cho mỗi đoạn đường cực lớn.
- Resize/scale thay đổi: đo lại period/span và chuẩn hóa pha, dựng lại chuỗi từ mép;
  không lấy lẫn sizeDelta chưa scale với khoảng cách đã scale.

Không có `+1`, `-1`, nửa pixel hoặc epsilon để bù spacing trong đường nối/recycle.
Các hệ số pivot Y/chiều cao và crop preview legacy vẫn có ý nghĩa cũ, không phải
spacing fudge. Ground và Sky dùng chung nối mép chính xác, bỏ overlap còn sót lại.
Chiều cao ảnh tiếp tục theo aspect PNG; không cắt alpha để giả seamless.

Giữ các thông số đã có trên nhánh:

- Ground thấp 20 pixel, chỉ phần hình.
- Background2 (farTiles/Ground2) = 0,09 lần pan Ground.
- Background3 (distantTiles/Ground3) = 0,03 lần pan Ground.
- Sky 3× chiều rộng tham chiếu, đứng yên, bám mép trên.
- Camera, Save/Load, combat và World Map zoom/chuột cuộn không sửa.

## Debug tạm trong Editor

`GroundPresentation.traceTileSeams` mặc định bật, chỉ tồn tại trong UNITY_EDITOR.
Có thể tắt ở Inspector. Mỗi tile recycle ghi một dòng, không stack trace; không
log mỗi Update hoặc khi Scroll(0). Windows Player không chứa nhánh log này.

Ví dụ thu được bằng **Mono**, không phải Unity thật:

```
[TerrainSeam] Background2 tile=Ground Mountain Parallax 1 xMin=300 xMax=1600 renderedWidth=1300 neighbor=Ground Mountain Parallax 3 neighborXMin=-1000 neighborXMax=300 join=after
[TerrainSeam] Background3 tile=Ground Sky Parallax 1 xMin=2280 xMax=3970 renderedWidth=1690 neighbor=Ground Sky Parallax 3 neighborXMin=590 neighborXMax=2280 join=after
```

`join=after`: tile.xMin phải bằng neighborXMax. `join=before`: tile.xMax phải bằng
neighborXMin. Tất cả giá trị cùng hệ BattleArea, đã tính scale của tile. Tên lớp
Background2/3 trong log chỉ rõ mapping dù tên GameObject preview cũ được giữ lại.

## Kiểm thử thực thi

- `Tests~/run.sh --terrain-seams`: 37 PASS: ba artwork + fallback × 600/800/1150 ×
  scale X 1,37/1/0,83 (36 ca), thêm một ca log. Kiểm tra nối mép float chính xác
  (tolerance=0 trong mô hình), không overlap/gap, cả hai chiều, pan lớn, nhiều frame,
  resize, đổi terrain, giữ actor/Ground logic, không tạo thêm object/load resource.
- `--terrain-art`: 38 PASS. `--plains`: 11 PASS. Kỳ vọng cũ 30%/10% và Sky 2,5×
  được cập nhật theo commit `7d85f50` thành 9%/3% và 3×; giữ mọi nội dung kiểm tra,
  bỏ kỳ vọng overlap +1 đúng theo yêu cầu hotfix. Inspector override vẫn được test
  bằng một giá trị khác mặc định (4×).
- Các nhóm multi-zone, spawn, map-entry, position-hotfix, ranged-lanes, formation,
  sprites, magic-persistence, audio-volumes, desktop-ui, opening-font,
  movement-spike, render-trace và distance-waves đều PASS.
- Toàn bộ suite: 724 PASS rồi dừng tại MissingMemberException cũ
  GroundPresentation.fixedRect trong Tests~/TerrainRegression.cs:41. Các bài
  phía sau chưa chạy; không gọi toàn bộ suite PASS.
- Mono compile UNITY_EDITOR: 16 cảnh báo có sẵn; UNITY_STANDALONE_WIN: 3 cảnh báo
  có sẵn. UnityStub thêm Mouse.scroll và Mathf.Pow để compile các thay đổi zoom
  mới từ nhánh; không sửa code zoom/UIManager/WorldMapPanelUI.
- Thêm 6 ca `TerrainSeamNativeTests` trong Play Mode assembly hiện có: dùng Canvas,
  Image/RectTransform thật, projection 600/800/1150, scale X 1/1,37, recycle hai chiều
  và resize. Fixture kiểm tra bộ nối hình học qua fallback, không tạo CombatManager
  để chọn terrain; ba artwork được kiểm tra bởi nhóm Mono/import riêng.
  Chỉ tạo object sở hữu, EnvironmentManager luôn inactive trước
  AddComponent và không bao giờ kích hoạt Awake; không có GameManager/SaveManager,
  không scene hook hoặc save I/O. Fixture từ chối scene có manager gameplay.
  Dùng NUnit cơ bản, runtime/UI qua reflection như fixture B2 hiện có.
- Chưa compile/chạy 6 ca native bằng Unity Editor. Native tolerance 0,001 đơn vị
  chỉ dành cho round trip ma trận GetWorldCorners→BattleArea, không được dùng để
  đặt/cộng bù tile. Đây không phải chứng nhận không có seam khi rasterize.

## Nghiệm thu Unity / Windows Player

1. Mở scene test sạch, chạy 6 ca native. Trong gameplay, bật traceTileSeams trên
   GroundPresentation và chờ recycle Background2/3; chụp hai dòng log của điểm lỗi.
2. Tại 600/800/1150, pan hai chiều qua nhiều vòng trên Plains/Forest/Mountain và
   fallback. Các tile phải sát mép, không dải chồng hoặc khe mới; Ground vẫn hạ
   20px, Sky vẫn 3×, hai background vẫn chậm.
3. Kiểm tra New/Continue/Travel/Load/Retry, VFX và B2; mở World Map và thử zoom bằng
   chuột cuộn. Không đổi camera hay tọa độ actor để bù seam.
4. Build Windows thường từ commit mới, lặp bước 2–3. Nếu vệt vẫn còn dù log xác nhận
   hai mép bằng nhau, gửi log cùng ảnh/video và tên layer/terrain để điều tra mesh,
   sampling hoặc artwork; không tăng overlap/smoothing để che lỗi.

Chỉ sau bước native mới có thể nghiệm thu tiêu chí hình ảnh của người dùng.
