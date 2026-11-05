# Integration roadmap — kết nối nền tảng trước mở rộng

Mốc nguồn `0944519c46610e79238c027c83ae9ea023c984c3`, branch `feature/monster-awareness-ambush`. Đầu vào: [audit](GAMEPLAY_SYSTEM_AUDIT.md) và [dependency map](GAMEPLAY_DEPENDENCY_MAP.md). Đây là **kế hoạch để giao việc sau khi xem xét**, không cấp quyền tự triển khai. Tác vụ audit dừng sau ba tài liệu.

## Quyết định đã chốt — 05/11/2026

Theo [cập nhật thiết kế chính thức](../CORE_GAMEPLAY_LOOP_DIRECTION_2026-10-25.md): không class cố định; thiết kế cuối cùng tách MaxHealth khỏi kiểu đánh/vũ khí. Hệ số HP Melee/Bow/Magic hiện tại là tạm; việc thay thế cần nhiệm vụ riêng, không thực hiện trong lần cập nhật tài liệu này. Nguồn HP trang bị phù hợp gồm giáp, trang sức, phù, ngọc và khảm nạm; chưa yêu cầu triển khai ngọc/khảm hoặc bật template mới.

Gear prototype đã phê duyệt đúng cấu hình MainGame hiện tại: Sword/Bow (`sample:iron_sword`, `sample:basic_bow`), level 1, Attack +4, Common, chance .08, Uniform; material .60, `material:refinement`, lượng 1–2. Không sửa/lặp lại cấu hình hoặc công thức. Staff và template phòng thủ tiếp tục ngoài bảng thưởng đến khi có đặc tả chỉ số.

**Giai đoạn 4–5 đang HOÃN:** chưa phát triển công pháp/võ học/kỹ năng kích hoạt/đột phá cảnh giới mới; nội dung dưới đây chỉ lưu tham khảo cho tương lai. Không tự mở lại từ roadmap hoặc yêu cầu người dùng chốt thông số các module này lúc này. Cả các giai đoạn khác cũng chỉ thực hiện khi có nhiệm vụ tiếp theo.

**Cảnh giới chính thức đã được tái xác nhận theo thiết kế 15/10/2026:** Level tổng liên tục; tiến trình nội bộ mỗi đại cảnh giới 30 cấp (Sơ 1–9, Trung 10–19, Hậu 20–29, từ 30 viên mãn/bình cảnh); tiểu cảnh giới tự chuyển, đại cảnh giới chỉ chuyển sau Thiên Kiếp hai pha dự kiến chịu đòn/gây đủ sát thương. Không reset Level tổng khi đột phá; mốc bắt đầu tiến trình mới là Level lúc đột phá. Mapping mỗi 10 level của RankDatabase chỉ là legacy/prototype, không là authority hoặc cách tự chuyển đại cảnh giới chính thức. Batch v0.2 không triển khai Thiên Kiếp hay thay Save; xem [thiết kế cập nhật](../CORE_GAMEPLAY_LOOP_DIRECTION_2026-10-25.md).

## Thứ tự và nguyên tắc

1. Làm rõ baseline DEVB và quyền sở hữu dữ liệu trước khi đo progression.
2. Nối API progression có sẵn vào UI; không tạo hệ class/realm authority mới.
3. Nghiệm thu vòng farm vùng → reward → trang bị/rèn → Save/Load còn thiếu chứng cứ native.
4. **HOÃN:** chốt contract những phần dữ liệu nền chưa có nghiệp vụ, chỉ khi có nhiệm vụ riêng.
5. **HOÃN:** tích hợp module hẹp sau khi được mở lại và duyệt contract; không mặc định thêm truyện/môn phái/boss/kỹ năng.

Giữ một Hero chính, auto combat và tự do chọn vũ khí. Không xóa B2 test hoặc trường profession chỉ để khớp thuật ngữ. Không thay tỷ lệ material .60/gear .08 trong các việc nối hệ; không đổi công thức Combat/Forge, map difficulty hay schema vì tiện implementation.

## Giai đoạn 1 — Baseline production có thể quan sát

- **Ưu tiên:** P0 về độ tin cậy kiểm chứng; chưa phát hiện P0 mất dữ liệu hoặc combat crash trong audit này.
- **Mục tiêu:** người kiểm thử biết rõ Hero đang dùng thông số bình thường hay DEVB, để +4 gear và progression không bị Attack 1000 che lấp.
- **Tái sử dụng:** `CombatBalance.LoadDevProfiles/ResetDevProfiles`, `DevBalanceUI`, menu công cụ, profile marker và MainGameFoundationProbe có sẵn.
- **Cần sửa:** sau khi thống nhất policy build/dev, chỉ rõ override đang hoạt động ở màn hình công cụ hoặc log ngắn; phân biệt profile test với production. Loader hiện được `UIManager.Awake` gọi, phải kiểm tra policy trước khi gate; không âm thầm reset file cá nhân.
- **Cần bổ sung:** chỉ tín hiệu trạng thái/quy tắc bật DEVB, không có stats system mới. Nếu chưa chốt chính sách build thì chỉ báo trạng thái, không tự tắt DEVB cho người dùng.
- **Phụ thuộc:** không có module gameplay mới; lựa chọn product ở USER DECISIONS REQUIRED mục D1.
- **Rủi ro Save/Combat:** thay policy loader có thể đổi derived stats của save đang có; phải giữ dữ liệu gốc, không migrate bằng cách ghi đè chỉ số. Config DEVB là file riêng, không đưa vào Hero schema.
- **Nghiệm thu:** fresh isolated Hero không DEVB có Attack 10; override bật phải thấy thông báo; equip Sword/Bow tăng đúng +4 theo cùng mode; Load không nhân bonus; file DEVB/save người chơi không bị test ghi lại. Chỉ chạy nhóm stats/gear trực tiếp nếu mã thay đổi.

## Giai đoạn 2 — Người chơi sử dụng progression đã có

- **Ưu tiên:** P1; giá trị trực tiếp trước khi thêm combat skill.
- **Mục tiêu:** dùng được statPoints từ level-up và nhìn rõ level/cảnh giới hiện có.
- **Tái sử dụng:** canonical `GameManager.HeroData`; `EntityDataSO.AllocateHealth/AllocateDamage`; `RankDatabase.GetRankName`; panel Nhân vật hiện có, theme/Canvas/TMP/modal; các đường refresh combat stats/dirty/save của GameManager.
- **Cần sửa:** thêm handler quản lý phân điểm trong manager và kết nối button panel hiện có, disable khi hết điểm; refresh UI/combat state cùng đối tượng dữ liệu. Label +1 phải là **Physical Attack**, không giả tăng MagicalAttack. Chỉ hiển thị rank derive từ level với nhãn **legacy/prototype**; không coi nhãn là gate hoặc authority đại cảnh giới chính thức.
- **Cần bổ sung:** hai thao tác UI và presentation rank; không thêm stat, class, currency hoặc bản sao Hero. Caller mới phải bảo vệ null/menu/pending load theo pattern manager hiện tại.
- **Phụ thuộc:** giai đoạn 1 để đánh giá mức tăng; realm presentation chỉ dùng mapping sẵn có. Đột phá độc lập đã hoãn; tái thiết kế mapping hoặc HP cần task riêng, giữ phần allocation độc lập.
- **Rủi ro Save/Combat:** không cần field mới; giữ statPoints/addedHealth/addedDamage và migration cũ. Refresh HP không được heal miễn phí hoặc khởi tạo lại life. Không thay đổi weapon mode chỉ vì phân điểm.
- **Nghiệm thu:** EXP đủ tăng đúng 1 điểm/cấp; bấm một lần tiêu một điểm và +6 HP hoặc +1 Physical; hết điểm không mutation. Equip/unequip, Pause/modal và Load/Retry không làm stale state hoặc nhân modifier. Nhãn legacy/prototype tại level 1/10/11/100/101 khớp mapping cũ, sau Load derive đúng, không có buff/gate mới và không tuyên bố nghiệm thu cảnh giới chính thức. Các hành vi chưa có test mới phải được bổ sung trước khi báo PASS.

## Giai đoạn 3 — Nghiệm thu một vòng farm khu vực ổn định

- **Ưu tiên:** P1; hoàn thiện chứng cứ end-to-end, không triển khai economy mới.
- **Mục tiêu:** một Hero farm node, nhận material/gear production, chọn/trang bị/rèn, clear 5 wave và chủ động Travel hoặc Retry; Save/Load nhất quán.
- **Tái sử dụng:** world-map node/clear/link, exploration distance, awareness/ambush, shared death receipt, Inventory/Equipment/Forge, SaveMenu và verifier hiện có.
- **Cần sửa:** chỉ lỗi tái hiện được ở những cạnh này; không coi bản ghi test cũ của một module khác là bằng chứng. Giữ các nhánh partial reward failure, instance IDs và cleanup VFX. Bổ sung phản hồi cap/không thể nhận nếu thực tế chưa rõ; không tự đặt chính sách inventory mới.
- **Cần bổ sung:** các ca nghiệm thu còn thiếu và bằng chứng native khi môi trường sẵn sàng; tận dụng fixture/UI tooling, không dựng framework verifier mới. Không cần thêm Gold/recipe/drop template.
- **Phụ thuộc:** giai đoạn 1; giai đoạn 2 nếu kiểm agency phân điểm. Native/Player phụ thuộc môi trường cho phép, không tự can thiệp Firewall/cài module. Phần Editor tự động vẫn có thể thực hiện độc lập.
- **Rủi ro Save/Combat:** load snapshot, wave clear/death boundary và stale receipt khi Travel; receipt chỉ runtime, không coi như anti-crash ledger. Không thay checkpoint/schema trong task sửa UI hoặc reward.
- **Nghiệm thu:** config scene giữ .60/.08, controlled A tắt; một death không cấp lại khi callback trùng; một nhánh lỗi không rollback nhánh kia. Verify handler/instance/stat/Forge stock/history rồi Save/Load/combat sau Load. World clear không auto travel; boss còn bị chặn; Retry giữ gear/root/level theo policy hiện có. Phân biệt chance fixture=1 với chơi tỷ lệ thật, handler routing với native click. Chỉ chạy lại test cũ khi có diff ảnh hưởng hoặc nghi ngờ cụ thể; không lặp toàn UI matrix nếu không sửa UI.

## Giai đoạn 4 — HOÃN: hợp đồng phần dài hạn còn rời rạc

- **Ưu tiên:** P1 cho thiết kế tích hợp; không phải viết gameplay ngay.
- **Mục tiêu:** có đặc tả đủ rõ để một task sau nối công pháp/võ học hoặc cảnh giới mà không đổi nghĩa dữ liệu đang lưu.
- **Tái sử dụng:** root/tier/weights, CombatStats condition/source model, WeaponFamily, CombatDamagePart/Resolve, HeroCombatState/attack lifecycle, per-component Save guards. RankDatabase và DTO nghề được giữ đúng phạm vi, không giả là runtime skill catalog.
- **Cần sửa:** tài liệu contract và test plan; làm rõ root multiplier hiện đã nằm trong damage trace để không cộng lại trong adapter tương lai. Phân biệt CastSpeed/CDR/ManaRegen chưa có consumer với stats đã vận hành.
- **Cần bổ sung:** nếu chọn nhánh võ học: definition/loadout/learning-condition/activation contract, stable IDs và resource/cast/cooldown/effect lifecycle; nếu chọn đột phá độc lập: gate/cost/effect/state contract. Không tự chốt các con số hoặc tạo mana chỉ vì enum có ManaRegen.
- **Phụ thuộc:** chỉ mở lại bằng nhiệm vụ riêng sau khi người dùng thay đổi phạm vi hoãn và cung cấp đặc tả cần thiết. Nghề phụ/gold hiện DATA ONLY; không bắt buộc đưa thành feature kế tiếp và không dùng profession làm class.
- **Rủi ro Save/Combat:** learned combat skill/realm state chưa có schema; không reinterpret `LearnedSupportSkill` hoặc renumber enum. Phải có old/current/future compatibility plan trước mọi field/version mới. Đòn đánh cơ bản primary không được rơi lại vào legacy ×2,5/AoE chỉ vì mode RangedMagic.
- **Nghiệm thu:** tài liệu mô tả input/eligibility/one-shot effect/interrupt/cancel/Load semantics; chỉ rõ stat nào có consumer, source ownership và nơi áp root đúng một lần; không khóa class, không thay basic/Forge mặc định. Chưa có quyết định cần thiết thì ghi BLOCKED nhánh đó, không tạo placeholder “hoạt động”.

## Giai đoạn 5 — HOÃN: tích hợp hẹp theo hợp đồng đã duyệt

- **Ưu tiên:** P2 sau khi nền tảng đã nghiệm thu; không tự động thực hiện.
- **Mục tiêu:** nếu người dùng chọn võ học/công pháp, chứng minh một đường học/chọn/hiệu lực trên Hero tự chiến đấu bằng hiệu ứng đã duyệt. Không mặc định xây bộ chiêu/nhiều nội dung mới.
- **Tái sử dụng:** evaluator stats/damage, weapon/root contract, manager/load lifecycle và UI khung Võ học hiện disabled. Giữ SupportProfession/recipe tách biệt, không đổi tên chúng thành combat class.
- **Cần sửa:** thêm điểm gọi nhỏ ở lifecycle đã xác định trong giai đoạn 4; bật UI chỉ khi có chức năng thực. Giữ attack/target/receipt hiện có; tránh scheduler song song cùng cấp damage cho một activation.
- **Cần bổ sung:** chỉ data/runtime tối thiểu cho khả năng cụ thể được duyệt; persistence theo compatibility plan đã phê duyệt. Nếu chưa có đặc tả, dừng ở giai đoạn 4, không viết ability giả hoặc dữ liệu mẫu thành production.
- **Phụ thuộc:** hoàn tất giai đoạn 4 và chấp thuận Save nếu cần; có thể được hoãn để ưu tiên playtest/balance. Không phụ thuộc triển khai môn phái/cốt truyện/boss.
- **Rủi ro Save/Combat:** trùng modifiers khi load/re-equip; snapshot projectiles/cancel; timer/cooldown restore; root hoặc damage áp hai lần. Không dùng tên class để phân eligibility.
- **Nghiệm thu:** cùng Hero đổi vũ khí không mất learned data; điều kiện weapon/root đã duyệt cho kết quả đo được; một activation/hiệu ứng đúng một lần; thiếu điều kiện/resource không mutation; Pause/Resume và cancel khi Load/Travel/Retry đúng contract. Save cũ giữ nguyên, Save mới round-trip, future-version guard vẫn chặn. Nếu chưa xác minh an toàn hoặc cần tái cấu trúc lớn: BLOCKED và báo cáo, không viết lại combat/save.

## USER DECISIONS REQUIRED

Chỉ các quyết định không thể suy ra từ mã hoặc định hướng đã chốt; chúng không chặn việc ghi audit hiện tại.

| ID | Quyết định còn thiếu | Chỉ chặn phần nào |
|---|---|---|
| D1 | DEVB có được phép nạp trong Player bình thường hay chỉ các build/profile dev rõ ràng? Code hiện nạp từ persistent file; chưa có policy phát hành được thể hiện. | Thay đổi loader/build gating giai đoạn 1; hiển thị trạng thái override vẫn có thể làm độc lập. Không tự sửa file cá nhân. |
| D5 | Nếu farm dài chạm material cap hoặc gear list tăng lớn, muốn giữ policy từ chối nhánh hiện tại hay có chính sách sức chứa/xử lý dư khác? | Thay luật inventory/economy dài hạn; không chặn kiểm thử cap và phản hồi chính xác của luật hiện tại. Không tự thêm bán/phân giải/tài nguyên mới. |

D2–D4 cũ đã được gỡ khỏi danh sách cần quyết định hiện tại: đột phá mới/công pháp/võ học/kỹ năng kích hoạt đã hoãn; Staff/template phòng thủ chưa được đưa vào thưởng. Chỉ trở lại đặc tả của các phần này khi người dùng giao nhiệm vụ riêng. Gear Sword/Bow +4 và mục tiêu MaxHealth không phụ thuộc mode đã chốt, không hỏi lại.

Không hỏi lại một Hero, auto combat, không class, tự do chọn vũ khí/công pháp/võ học, tương hợp/tương khắc, môn phái không là class, tiến triển dài hạn hoặc không multiplayer. Các điểm này là ràng buộc của mọi task sau.

## Điều kiện bàn giao từng task sau

Mỗi task chọn một phạm vi, có entry/caller/data authority và tiêu chí kiểm thử cụ thể; giữ công việc có sẵn, diff nhỏ, commit riêng và không push/merge nếu chưa được yêu cầu. Cập nhật PASS/FAIL/NOT RUN/BLOCKED theo cái thực chạy. Báo cáo trước có thể cung cấp ngữ cảnh, không thay thế bằng chứng cho mã mới. Roadmap không là lệnh thực thi tự động.
