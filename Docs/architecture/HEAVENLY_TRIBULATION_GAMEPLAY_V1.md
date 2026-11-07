# Phá Thiên Quan — Hợp đồng gameplay v1

07/11/2026 · Development v1.3 · MainGame/Windows owned fixture. **Production OFF**.

## Đường chơi

Nhấn **Độ Kiếp** trong bảng Nhân vật khi Hero thật đang ở bình cảnh theo căn cơ và có successor được catalog cấu hình. Kiểm tra session/config, active life, injury và Burning/pending settlement trước khi ghi checkpoint. Không suy từ Level, không dùng effective combat realm, không tự vào trial khi lên Viên mãn. Profile production hiện tại chỉ có Luyện Khí; UI giải thích lý do khóa.

`SealCombat` là một trận duy nhất theo thiết kế Phá Thiên Quan đã duyệt. Hai pha mô phỏng ở [v1.2](HEAVENLY_TRIBULATION_FOUNDATION_V1.md) tiếp tục có test riêng, không được gọi trong trận này. GameManager giữ Hero/session canonical; khoá wave/exploration, hủy đạn cũ, tạm ngừng đồng hồ tu luyện và tạo đúng một phong ấn không chuyển động theo AI. Placeholder hình học dùng Image hiện có, không thay scene/asset/sprite Hero. Không có B2, monster spawn hoặc kill reward trong trận. Sau settle, quay lại cùng map/progress và tái sinh wave đang dở qua lifecycle cũ; không resume từng con quái/HP/đạn đã bị cô lập.

CombatManager dùng cùng target selection, WeaponBasicAttack, roll damage hiện có, projectile và CombatDamage. Tầm Hero là min(tầm vũ khí, giới hạn encounter); không kéo dài tầm melee. Phong ấn chịu armor/magic resistance và phản kích theo nhịp game-time bằng đường nhận damage có phòng thủ của Hero. Đòn đánh cơ bản vẫn có roll damage như ngoài trial; không có roll xác suất đột phá. Không có EXP, loot, heal-on-kill, EXP thưởng wave hoặc tiến trình map từ phá ấn.

Cuối mỗi frame combat nhận HP đã áp cho hai phía. Hero chết được xét trước phong ấn chết, vì vậy double KO không thắng. Mã encounter/attempt, target, owner/epoch và life chặn callback cũ; receipt có step, event ID và cửa sổ tuổi. Commit thành công kiểm tra lại HP sống, binding epoch, dữ liệu căn cơ nguồn và exact expected active hash. Combat không tự áp thưởng/phạt hoặc tự phục sinh.

## Kết quả

- Thắng: `RealmBreakthroughTransition` trên candidate giữ reserve, energy, injury và các quyền lực ngoài tu luyện; chuyển successor, căn cơ0. Capacity lấy theo actual realm mới, không thêm tiểu cảnh giới. Tick gameplay mới tiếp tục sau khi snapshot đã commit và Hero triển khai lại.
- Thua: penalty versioned chỉ giảm căn cơ đã luyện hóa. Tính loss=min(trị số policy, căn cơ−ngưỡng thấp nhất còn được phép). Luyện Khí/Trúc Cơ tối đa1 tiểu cảnh giới; từ Kim Đan tối đa2. Giới hạn không ép phạt đúng số bậc. Không đổi injury, realm, reserve, Level/EXP, gear/bag/Forge. Hero hồi sinh qua cùng đường load/deploy, không gọi reset tiến trình map do chết thường. Lần thử sau cần trở về bình cảnh; nếu đủ điều kiện, phong ấn tạo mới đầy HP.
- UI hiển thị Độ Kiếp/lý do khóa, tên và HP hai phía, báo kết quả sau commit: cảnh giới/căn cơ0 hoặc loss thật/tiểu cảnh giới, reserve giữ nguyên. HUD tránh phủ thẻ Hero và bảng Character ở600px. Inventory/Forge/WorldMap giữ layout/đường điều hướng; trong trial action ghi/đổi gear/Forge/Travel bị khóa.

## Dữ liệu thử nghiệm và đề xuất production

| Trường | Fixture v1.3 | Production |
| --- | --- | --- |
| Encounter | `fixture:pha-thien-quan-v13` v1, `SealCombat` | Chờ duyệt phiên bản/config |
| HP / Armor / MagicResistance |300 /20 /20|Chưa duyệt|
| Raw phản kích / interval / range cap |12 /1s /200 đơn vị BattleArea|Chưa duyệt|
| Penalty |`fixture:gate-penalty-v1` v1, loss yêu cầu500 rồi chặn ngưỡng |Chưa duyệt trị số|
| Nguồn→đích |Luyện Khí [0,900,2100,3600] → `fixture:truc-co` [0,1200,2800,4800] |ID/thresholds cần duyệt|
| Trúc Cơ base absorb/refine |3/2, kế thừa stage factors ở fixture |Chưa duyệt|
| Qi supply |Mật độ fixture .5 (1 Qi/s); dùng cùng bộ tính capacity |World Qi v2 vẫn giữ nguyên trong config production v1.1|

`HeavenlyGateV13Fixtures` tạo cấu hình dưới `UNITY_EDITOR || DEVELOPMENT_BUILD`. Chỉ launcher marker-owned cài profile `balance:tribulation-v13-fixture` trước Awake; default MainGame không cài. Gate `HeavenlyTribulationEnabled=false`. Cần duyệt số liệu, interrupt/rollback policy và nghiệm thu input thật trước xem xét mở gate. Xem [transaction](TRIBULATION_SAVE_TRANSACTION_V1.md) và [báo cáo](../automation-reports/DEVELOPMENT_V13_REPORT.md).
