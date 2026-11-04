# HUD W3 — 05/11/2026

Ba thẻ góc trên phải đọc từng `HeroCombatState`: tên/cấp/HP độc lập,
ô B2 chưa tuyển ghi “Chưa có Hero”, đồng đội gục vẫn giữ thẻ. Mana hiện
“—” vì chưa có nguồn runtime. Không tạo party hoặc Mana mới.

Tên map xếp các từ theo chiều dọc bên trái, wave đọc `worldMapId` và
`worldMapLoopWave`; save legacy mới dùng tên/wave legacy. Quãng đường thực
vẫn hiện riêng khi nhật ký tắt. Mép phải có Nhân vật, Hòm đồ, Võ học (vô hiệu),
Bản đồ và Menu. Hai góc dưới dành trống; không đổi battle/camera/actor.

Pause và nút nhật ký ở trên trái. Menu → Công cụ & hiển thị giữ Pause,
âm lượng, toàn bộ nhật ký, chi tiết sát thương, B2 thử nghiệm và DEVB.
Nhân vật chỉ hiện chỉ số; nhật ký ngắn tắt mặc định và không xóa lịch sử.
Hòm đồ tái sử dụng nút/InventoryPanelUI và mở rộng/thu gọn cửa sổ hiện có.
Loot giữ RectTransform của nút này; khi ẩn/mất dùng mép phải cửa sổ thật.
Animation không cấp thưởng.

## Chỉnh trong Inspector

- `UIManager → HUD W3 → Hud Layout`: margin, khoảng cách, thẻ, thanh nút,
  khung map, font và chiều cao nhật ký, tính bằng pixel trên Readable HUD.
  Thay trực tiếp trong Play Mode; context menu `HUD W3 - Ap dung layout va theme`
  áp lại khi cần. Không lưu các object runtime để chỉnh bố cục.
- Dùng `Inventory Art Theme` đang có, bật `Inventory Use Art Theme` khi muốn
  dùng ảnh. Mục `06. HUD W3 / CAC KHUNG DUNG CHUNG` nhận nền khung, header,
  thẻ, avatar, portrait và các lớp thanh chỉ số. Mục 01/02 giữ khung/nút/tab
  Inventory. Không có ảnh vẫn có nền màu và viền.
- Header background, text, X và các control tách nhau. Thay ảnh không đổi
  callback/hitbox. Skin không đăng ký input; modal gốc và control quản lý hit-test. Avatar giữ tỷ lệ; chỉ Sliced khi bật tùy chọn **và** sprite
  có Border khác 0. Context menu cũ áp ảnh Inventory vẫn hoạt động.
- `Inventory Layout Debug` là overlay đo kích thước có sẵn; tắt khi nghiệm thu
  hình ảnh, bật lại hoặc dùng context menu khi chuẩn bị artwork.

`UIManager.HudW3.cs` quản lý view/theme; `HudW3View.cs` chỉ đọc dữ liệu;
`HudW3Layout.cs` chứa các thông số chung. Không quét scene, dựng lại hierarchy
hoặc tạo lại toàn HUD mỗi frame. Binding scene thực hiện một lần sau Start;
layout chỉ chạy khi resize/thông số đổi. Nội dung panel giữ logic cũ, nhãn nút
co giãn theo hitbox; font Inventory theo chiều rộng panel thay vì toàn cửa sổ.

## Bằng chứng và giới hạn

Unity đúng bản ghim 6000.5.0f1. `HudW3Tests` kiểm kích thước 600/800/1150,
HP/ô trống/đồng đội gục/rebind dữ liệu, map khác legacy, fallback/9-slice,
caption khi resize, nền trang trí không chặn nút ngang hàng và loot đổi đích. Có kiểm mesh chữ HP/Mana thực sự hiện.
Hồi quy loot, CombatLoot, InventoryComparison, awareness và actor presentation
được chạy theo diff: tổng hợp 50 ca Unity khác nhau đạt. Vòng UI cuối qua 17/18;
fixture caption được sửa chọn overload UnityEvent, giữ assertion và chạy lại 1/1 đạt.
Mono loot 23 và Inventory/theme/window 18 nhóm đạt.

Ảnh chụp từ MainGame trong Unity Game View: 600×338, 800×450, 1150×647 và
Inventory mở rộng tương ứng. Profile GUID/marker riêng cấu hình trước Awake;
không dùng player Saves. Các bước B2/HP chết/tên dài dùng dữ liệu thử trong
profile đó. Load/Retry/Travel, Pause/Resume, log, modal và loot resize được
gọi qua handler; Menu/Hòm đồ có thêm bấm chuột Windows thật trong Game View.
Không coi các kiểm tra này là Windows Player PASS.

Mono `--desktop-ui` dừng sau 31 ca đạt ở `old battle has an in-flight shot`.
Đã tái hiện cùng assertion trên source gốc `88e140f`, không sửa combat hoặc
giảm assertion. Fixture menu được cập nhật đúng trigger W3 và vẫn kiểm toàn
bộ hành động cũ, thêm Tools; chỉ loại container chặn nền khỏi danh sách action.
Lỗi UnityEditor.Search/video/TLS khi khởi động nằm ngoài diff.

Còn nghiệm thu Windows Player với native transparent-window input, resize
desktop thật và artwork do người phát triển gán sau này. Không đổi save/schema,
AI, damage, loot RNG, cờ thử nghiệm, sprite 200% hoặc battleground +20px.
