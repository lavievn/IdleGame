# W2.2h — luồng bắt đầu map dùng chung

Mã đã được kiểm tra bằng Mono/UnityStub; chưa biên dịch hoặc nghiệm thu Unity
Editor/Windows Player. Các PASS Unity 51 Edit Mode/3 Play Mode trước đây thuộc
phiên bản trước thay đổi này, không phải nghiệm thu bản W2.2h.

| Đường vào | Cinematic zoom | Appear | Wave đầu |
|---|---|---|---|
| Chơi mới, Tiếp tục từ Main Menu | Giữ nguyên | Sau cinematic | Sau VFX |
| LoadAndContinue, Reset trong gameplay | Không | Lớp che ngắn → VFX | Sau VFX |
| Retry, World Map Travel | Không | Lớp che ngắn → VFX | Sau VFX |
| Bật lại coordinator bị disable giữa entry | Không | Bắt đầu lại VFX | Sau VFX |
| ForceManualLoad(deploy=false) | Giữ menu chờ | Chưa phát khi chưa triển khai | Chưa sinh |
| Chuyển wave, sinh lại wave thoát mép | Không thêm cinematic | Không thêm VFX | Giữ luật cũ |

`DeployHero()` là điểm dùng chung. Hủy wave chờ cũ, dọn combat/projectile cũ,
thu hồi B2, đặt lại camera khi hình đang che, spawn Hero tại xMin + 80% width,
chuẩn bị HeroSpawnVfx rồi khóa gameplay. GameObject logic vẫn active; chỉ sprite
và Graphic gắn với Hero bị ẩn. GameManager điều khiển một timeline VFX hiện tại,
không dùng coroutine hoàn tất độc lập. Thay entry mới sẽ xóa pending completion
cũ. `CallNextWave()` từ chối khi entry đang chạy, kể cả lời gọi trực tiếp/stale.

Menu zoom vẫn do StartMenuUI/OpeningCinematicUI hiện có điều khiển. Các đường ngắn
dùng Canvas Overlay **top-level** do GameManager sở hữu, sortingOrder 1000,
Image stretch toàn cửa sổ và GraphicRaycaster chặn click xuyên. Lớp che tan theo
`mapEntryCoverSeconds=0.15` giây unscaled, giới hạn tối đa 1/4 thời lượng appear;
không kéo dài VFX hoặc ảnh hưởng cooldown. Hủy/hoàn tất luôn tắt cover; destroy
GameManager sẽ destroy cover do nó tạo, không đụng Canvas khác.

Hoàn tất ở GameManager.LateUpdate sau Environment.LateUpdate đang pause. Chỉ
sau đó mới hiện UI, bỏ pause, bật camera thống nhất và sinh wave đầu. Thiếu sheet,
sheet sai kích thước, disable/cancel VFX hoặc lỗi API VFX có fallback giải phóng
entry/cover; không bỏ qua lỗi dữ liệu Save/Load. Load bị từ chối vẫn trả về trước
khi dọn gameplay. SaveManager/JSON/Inventory/Forge/công thức combat không sửa.

## Kiểm thử

- `--map-entry`: 20 ca, 6 đường × 3 kích thước và thay entry/hủy/asset lỗi.
- `--multi-zone`: camera mới; xem thông số tại `PositionHotfix.md`.
- `--position-hotfix`: spawn B2 và bounds; ba ca native mới chưa chạy.
- `--spawn`, `--wave-entry`, `--desktop-ui`, `--formation`, `--sprites`: hồi quy.

Các test cũ được cập nhật ở phần **thứ tự chờ entry** để kiểm tra gameplay sau
VFX; giữ các assertion dữ liệu, HP, UI, manual slots và chống wave lặp. Test spawn
ban đầu kiểm tra 80% thay HomeX, đúng thiết kế mới. Fixture Field mới luôn bắt
đầu không pause để không kế thừa trạng thái entry của scene giả lập đã bỏ đi.
Không xóa assertion, bỏ test hoặc tăng tolerance.

Cần nghiệm thu thật: cinematic/short cover và vị trí khi render; Solo/B2 và Hero
chết khi bám camera; VFX/HP bar/charge bar/popup; New/Continue/Load/Travel/Retry/
Reset/Return Menu ở 600/800/1150 và Windows Player. Các kiểm tra Canvas mô phỏng
không thay thế kiểm thử resize cửa sổ thật. Không thử trên save quan trọng.
