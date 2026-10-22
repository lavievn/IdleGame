# W2.2 — Vào map và hiệu ứng xuất hiện Hero

Nền: `f8855f8af599ce09ee0b98a027821b53ca62a2ab`.

## Nguyên nhân và phạm vi

Trước thay đổi, `GameManager.DeployHero()` gọi `SpawnHero()` và `CallNextWave()`
trước `BeginGameplayTransition()`. `SpawnHero()` bật hình và đặt HomeX ngay.
`OpeningCinematicUI.AnimateEntry()` phóng sky/far/near và đẩy mây trong 2,7 giây;
không có camera Unity zoom hay thay tọa độ Ground trong phương thức này.
`CompleteGameplayTransition()` mở pause ngay, nên Hero đang hiện sẵn bắt đầu
movement/camera follow khi lớp phong cảnh vừa kết thúc. Đây là đường hiển thị
sớm có bằng chứng từ mã. Không đủ dữ liệu để kết luận mọi cú giật quan sát được
trên máy người dùng là một lệnh teleport; cần nghiệm thu chuyển động thực tế.

Luồng sau W2.2b: spawn logic ở HomeX trong khi toàn bộ hình/UI Hero bị ẩn →
giữ pause và hoãn wave → giữ nguyên chuyển cảnh → khi cinematic kết thúc,
đặt Hero tại `BattleArea.rect.xMin + Width * 0.8` trong khi vẫn đang ẩn →
phát VFX/fade thân → khôi phục UI gắn Hero và bắt đầu wave.
GameObject logic vẫn hoạt động; không đổi phép tính camera, AI hay cooldown.
HP, damage text, charge bar và các Graphic con được tạm vô hiệu hóa việc vẽ,
giữ nguyên alpha/layout và khôi phục đúng enabled ban đầu khi xong hoặc hủy.
Graphic tạo muộn cũng bị ẩn. VFX chỉ bật sau cinematic, neo chân Hero.
80% chỉ là vị trí xuất hiện, không thay HomeX/camera hay buộc vị trí khi chơi.
UI không thể bỏ pause giữa entry; timeline dùng unscaledDeltaTime.

Chỉ New Game/Continue thực sự từ title dùng toàn bộ entry. Reset khi đang chơi,
Load thủ công và Retry giữ đường vào trực tiếp. Capture nguồn trước InitHeroData
vì hàm này trước đây tự gọi SetupPreGameUI, khiến Reset cũng mở lại opening.
Load/ReturnTitle/hide/đổi thế đánh hủy fade cũ. Asset hỏng/thiếu hoặc cinematic
không tick có fallback/watchdog; không để Hero trong suốt hay giữ màn hình đen.

## Asset và căn chỉnh

`Assets/Resources/Spawn/Appear.png`: sheet RGBA 4000×2500, 8 cột × 5 hàng,
38 frame 500×500 ở 30 FPS, thứ tự trái sang phải/trên xuống dưới; hai ô cuối rỗng.
Nguồn: appear.mov qtrle/argb, 1,266667 giây; SHA-256:
`f263f34aa73e18a5f104911a9cb7722fb1d70fc94e4e7c817c208983401ba581`.
Giải mã bằng ffmpeg ra PNG RGBA rồi ghép không thay pixel/alpha từng frame.
Không đưa MOV vào runtime, không thêm codec Windows. Import texture giữ kích
thước (NPOT=None, max=4096), không mipmap/compression; Sprite.Create FullRect.

Vòng chân ở source khoảng (250,455), pivot hiển thị (0,5;0,09). Chiều cao canvas
VFX = chiều cao thân × `HeroController.spawnVfxScale` / 0,91; mặc định 1,4.
Tính theo thân sprite khoảng 430×480 source pixels, không dùng 500 như đơn vị game.
Cột sáng trong MOV hẹp nên thêm ánh sáng mềm cục bộ bằng Unity, phủ silhouette
trong lúc fade. Đây là lớp bổ trợ, không thay thế asset gốc. VFX và thân cùng
parent nên tự nhận tỷ lệ chiếu của Battle Canvas khi resize 600/800/1150.
Tăng/giảm `spawnVfxScale` trong Inspector HeroController để nghiệm thu thẩm mỹ.

## Xác minh

`bash Tests~/run.sh --spawn`: kiểm tra logic với UnityStub, asset Texture2D giả
có đúng kích thước để kiểm tra state/rect/alpha, không phải render/importer thật.
PNG đầu ra được đối chiếu riêng với đủ 38 PNG giải mã từ MOV, bao gồm alpha.
Chạy thêm `--desktop-ui`, `--sprites` và nhóm B2. Toàn suite vẫn có blocker cũ
`GroundPresentation.fixedRect`; không tuyên bố toàn bộ suite PASS.

Chưa có Unity Editor/Windows Player trong Cloud. Cần kiểm tra trực quan New Game,
Continue, ReturnTitle→Continue, Load giữa entry, Reset và Retry; quay video tại
600/800/1150 để xác nhận không nhảy vị trí, ánh sáng che đủ thân, UI chỉ hiện sau VFX,
chỉ bắt đầu đánh sau VFX. Kiểm tra Console, thiếu sheet, DPI/resize, đồng đội B2
được clone sau entry, Windows Player build. Không thay SaveManager/định dạng save.
