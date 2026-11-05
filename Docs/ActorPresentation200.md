# Nhân vật 200% và battleground +20px — 04/11/2026

Thực hiện sau task AI/phát hiện/đánh bất ngờ (`75590ec`), theo yêu cầu kế tiếp.

- CultivationSpriteBridge: mặc định Hero và Monster scale **2**, áp dụng cho
  body child và mọi pose; không scale root logic, HP fill, popup hoặc HUD.
  Thanh HP được đặt trên silhouette đã phóng to, giữ nguyên kích thước và fill;
  phép đặt vị trí tuyệt đối không cộng dồn khi đổi pose hoặc tái dùng pool.
- EnvironmentManager: `battleVerticalOffsetPixels = 20` nâng toàn bộ Stable
  Battle Canvas/Ground/actor/VFX lên **20 pixel màn hình**. Chuyển offset qua
  Canvas scale nên không thay đổi theo cỡ 600/800/1150. Cache projection theo
  cả offset để chỉnh offset không cần resize mới có tác dụng.
- Giữ logicalWidth, tọa độ chân BattleArea-local, camera controller, chỉ số,
  range, tốc độ và schema. Giữ groundDropPixels/artwork cũ; đây là nâng toàn
  frame hiển thị thêm 20px, không chỉnh pixel nguồn hoặc ép vị trí Hero.
- Loot đang rơi/chờ nâng cùng Ground. Loot đã bay sang overlay tiếp tục giữ
  tọa độ pixel riêng, không bị kéo theo lần nữa. Marker `?`/`!` theo thân 2x và
  vẫn giữ 24 pixel cho dễ đọc.

## Kiểm thử

Unity **6000.5.0f1** chạy nhóm ActorPresentationScaleTests (7 ca), awareness,
loot/combat, weapon và B2: **53/53 Play Mode PASS**, không skip. Ca mới đo body
2x ở cả ba mode của Hero/Monster, root/foot không đổi, HP bar không bị phóng to,
projection +20px và phase bay loot độc lập khi offset thay đổi.

Fixture B2 cũ chủ động tắt foot lanes, yêu cầu nhét cả frame alpha cao 100 vào
sàn cao 100; body 2x không thể đáp ứng tiền đề đó. Đã khai báo rõ scale tham
chiếu 1x trong fixture legacy, giữ nguyên mọi assertion chứa frame/non-overlap/
camera/budget. Sáu ca native foot lanes dùng **body production 2x** và tuyển B2
độc lập; không nới guard hoặc bỏ assertion để test qua. Nhóm Mono Field cũng
khai báo reference sprite 1x; nhóm mới chọn `largeActors:true` đo mặc định 2x.

Mono/UnityStub thực chạy:

| Nhóm | PASS |
|---|---:|
| actor-presentation | 5 |
| monster-awareness | 14 |
| loot-visuals (gồm combat loot) | 23 |
| weapon-basic | 31 |
| formation/B2 routing | 39 |
| multi-zone | 39 |
| ranged-lanes | 28 |
| sprites | 29 |
| enemy-sprites | 6 |
| movement-spike | 51 |
| position-hotfix | 13 |

Hai failure có trước trong distance-waves và magic-persistence được ghi riêng
tại Docs/MonsterAwareness.md, đã tái hiện trên bản gốc 31e5e6c. Không sửa chúng.

Đã chụp bản sao MainGame ở Game View 600×338 / 800×450 / 1150×647 sau phóng to,
kiểm marker và hướng nhìn, báo động cả nhóm và tuyển đủ hai đồng đội B2 2x.
Profile có SaveManager inactive trước Awake, cấu hình thư mục GUID/marker riêng;
không truy cập player Saves, không lưu scene thử hoặc harness vào commit.

Giới hạn: chưa chạy Windows Player hoặc kiểm profiling dài hạn. Game View đúng
độ phân giải không chứng minh resize cửa sổ Player/bấm nút native. Lỗi khởi động
UnityEditor.Search và cảnh báo video/TLS được giữ ngoài phạm vi, không coi là lỗi
AI. Không tự merge main và không ghi đè scene đang chỉnh trên máy người dùng.
