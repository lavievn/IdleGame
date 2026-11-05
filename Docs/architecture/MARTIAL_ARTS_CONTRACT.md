# Hợp đồng công pháp và võ học

## Trạng thái và ràng buộc

**PROPOSED — v0.4 chỉ thiết kế, chưa tích hợp production.** Ngày 05–06/11/2026; đối chiếu nguồn `ba86368`, cập nhật nhãn trace tại `e1fe9e9`. Không class cố định: một Hero tự do đổi vũ khí. Weapon family quyết định tương thích võ học, không quyết định nhân vật thuộc class nào. Không triển khai mana, resource, cooldown hoặc runtime skill trong batch này.

“Công pháp” là phương pháp tu luyện/hiệu ứng duy trì; “võ học” là kỹ thuật chiến đấu có điều kiện vũ khí và lifecycle. Một công pháp có thể cung cấp passive khi được chọn; một võ học có thể passive hoặc active theo definition được duyệt. Không dùng hai tên này thay cho profession/recipe hoặc đổi nghĩa `LearnedSupportSkill` đã lưu.

## Đối chiếu nền mã

| File / symbol | Có thể tái sử dụng | Giới hạn phải giữ |
|---|---|---|
| [WeaponBasicAttack](../../Assets/Script/WeaponBasicAttack.cs), `Family`, `Resolve`, `Value` | Family lấy từ template ID; Unarmed/Sword/Bow/Staff; nguồn Physical/Magical riêng. | `WeaponType` cũ không phải authority mới. Staff basic là single target, không phải võ học/cast tự động. Staff chưa nằm trong bảng thưởng production. |
| [CombatStats](../../Assets/Script/CombatStats.cs), `CombatStatModifier`, `GetWithModifiers` | sourceId, required/blocked tags, flat/percent và element. | Công thức ordinary = base+flat+base×percent; ratio = base+flat+percent. Tags root/skill chưa tự sinh. CastSpeed/CDR/ManaRegen có enum không chứng minh có scheduler/resource. |
| [SynergyMath](../../Assets/Script/TuTienEnums.cs), `GetElementalMultiplier`, `EvaluateSnapshot` | Bảng root/tier/weights hiện tại, thông tin cặp khắc và trace. | Basic đã áp root weighting. Không nhân lại root ở adapter sau cùng; không đổi bảng hoặc biến itemElement thành căn của Hero. |
| [CombatDamage](../../Assets/Script/CombatDamage.cs), `Resolve`, `ResolveBasicSnapshot` | School, damage parts, boost và defense/resistance. | `Resolve` dùng `attacker.GetCalculatedDamage(mode)` cho attackMultiplier, không tự chọn MagicalAttack chỉ vì part.school=Magical. `ResolveBasicSnapshot` là basic/Vô, không nhận arbitrary skill element. |
| [HeroCombatState](../../Assets/Script/HeroCombatState.cs) | Data canonical, LifeVersion, HP, weapon identity, attack timer. | Chưa có learned-art state hoặc activation/loadout. Không dùng AttackTimer làm mọi skill cooldown. |
| [CombatManager](../../Assets/Script/CombatManager.cs), projectile lifetime/clear | Kiểm owner/life/spawn, valid target, pause, cleanup và accepted damage/death. | Không tạo nhánh skill cấp damage/EXP/loot song song qua handler chết. Cần cổng accepted impact dùng chung khi triển khai. |

## Definition và stable ID — đề xuất

Definition bất biến trong catalog, tách khỏi dữ liệu học của Hero:

| Trường đề xuất | Ý nghĩa |
|---|---|
| `id`, `definitionRevision` | ID namespace bền, ví dụ `proposal:art:sword_form_a`; revision của definition khác schema Save. Không tái sử dụng ID đã xóa cho nội dung khác. |
| `kind` | CultivationMethod hoặc MartialArt; độc lập với `effectMode` Passive/Active. |
| `allowedWeaponFamilies` | Danh sách explicit; Unarmed không mặc định khớp tất cả. Công pháp có thể unrestricted nếu definition ghi rõ. |
| `learningPrerequisites`, `rankRules` | Điều kiện học/nâng đã duyệt, max rank và bảng hiệu ứng theo rank. Không có giá trị tự mặc định thành một cost mới. |
| `rootAffinityPolicy` | None, EligibilityOnly hoặc WeightedCounterOnce; quy định input root và đúng một nơi tính multiplier. Không tự thêm “cùng căn +X%”. |
| `effects` | Passive modifier descriptors hoặc active parts: school, element, source stat, coefficient/fixed amount, target policy. Tham số chờ balance duyệt. |
| `activationPolicy`, `interruptPolicy` | Trigger tự động/thủ công, concurrency, cancel/interrupt và xử lý target mất; chưa mở nút/AI chọn skill production. |
| `presentationId` | Tham chiếu tài nguyên sau này; thiếu artwork không cho phép tự thay bằng skill có cùng damage. |

ID phải được phân giải qua registry và kiểm trùng trước mutation. Tên hiển thị/translation/prefab không là khóa Save. Thiếu definition khi Load: từ chối kích hoạt, giữ record để người dùng sửa nội dung hoặc theo policy đã duyệt; không xóa record rồi save đè một cách im lặng.

## Học, nâng cấp, lựa chọn và loadout

```text
Unlearned --validated Learn--> Learned(rank>=1)
Learned --validated Upgrade--> Learned(rank+1)
Learned --Select--> Selected --eligibility satisfied--> Effective/Ready
Selected --weapon/root condition lost--> SelectedButIneligible
Selected --Deselect--> Learned
```

Học không đồng nghĩa được chọn; được chọn không đồng nghĩa có hiệu lực. Chuyển vũ khí không xóa kỹ năng đã học hoặc tự reset rank. Slot capacity, số công pháp/active được chọn và có cho selected-but-ineligible giữ slot hay không cần duyệt; đề xuất giữ lựa chọn, tắt hiệu lực và hiển thị lý do để đổi lại vũ khí khôi phục nhất quán.

Loadout đề xuất lưu stable IDs theo nhóm công pháp, passive võ học, active võ học; validation kiểm học trước, trùng ID/slot, rank và compatibility. Không thêm bao nhiêu slot vào gameplay khi chưa duyệt. Thao tác học/nâng/chọn là manager command trên Hero canonical; thất bại không tiêu cost, không sửa learned/loadout/modifier list.

Passive có owner prefix riêng như `martial:passive:<id>` hoặc `cultivation:<id>`. Rebuild chỉ thay prefix thuộc mình; không xóa `equipment:instance:`/`equipment:primary-trinket` hoặc modifier nguồn khác. Rebuild nhiều lần sau Load/equip không được cộng dồn. Save chỉ một authority cho passive: đề xuất lưu learned+loadout, derive modifiers; nếu modifier list chung vẫn được serialize, load phải gỡ đúng prefix và rebuild một lần trước combat.

## Root affinity và damage — một lần duy nhất

Phân biệt hai câu hỏi: **có được kích hoạt không** và **damage có hệ số nào**. EligibilityOnly đọc root để quyết định boolean, không nhân damage. WeightedCounterOnce dùng đúng root profile/tier/weight của Hero và defender với bảng SynergyMath; không phát minh thêm hệ số tương hợp ngoài bảng đã có. Definition chọn root nào/đơn căn hay profile đầy đủ phải explicit và được duyệt. Bảng khắc hiện tại không phải bảng “tương sinh” buff.

Pipeline active đề xuất:

1. Validate learned/selected/weapon/root/target/lifetime và điều kiện đã duyệt, không mutation khi fail.
2. Tại release chụp source stat theo từng part: Physical Attack dùng `CombatStatId.Attack`, MagicalAttack dùng `CombatStatId.MagicalAttack` với cùng evaluator và context tags. Chụp coefficient, roll nếu được duyệt, offensive boost, root profile và definition revision. Không lấy Physical thay Magical vì Magical=0.
3. Dữ liệu root phải được validate/normalize trên bản sao profile trước tính toán; không gọi normalize gây dirty trên Hero live khi chỉ preview. Tại impact xác minh target/lifetime, tính root multiplier đúng một lần bằng `SynergyMath.GetElementalMultiplier` trên profile nguồn đã snapshot và defender hiện tại. Nếu tái sử dụng `EvaluateSnapshot` cho effect phù hợp, không nhân root thêm ngoài kết quả ấy. Hai cách là lựa chọn thay thế, không xâu chuỗi cả hai.
4. Đưa damage đã có source/root vào `CombatDamage.Resolve` bằng part có `fixedDamage`, `attackMultiplier=0`; offensive boosts đã snapshot thì truyền attacker null và cộng boost đúng một lần trước defense. Không gọi lại `ResolveBasicSnapshot` trên kết quả skill hoặc đưa damage đã Resolve qua `ResolveLegacy` lần nữa.
5. Apply kết quả qua một cổng accepted impact dùng chung có owner/activation/life validation; cổng này không resolve stats/root/defense thêm lần nữa. Nếu part element khác Vô, defense dùng element đó. Root-counter multiplier khác lớp element resistance; mỗi lớp chỉ áp một lần.

Đây là yêu cầu cho adapter tương lai, chưa là API có sẵn. Phải giữ rounding/clamp hiện có tại điểm tái sử dụng và chốt rounding skill riêng trước khi viết; không suy basic rounding/minimum thành luật mọi skill. Multi-part cần transaction impact rõ: tổng damage một lần trên HP, không mỗi part phát lại death/receipt. Surprise/ambush hiện ×2 sau defense trên accepted hit; skill có hưởng không và một activation nhiều hit tiêu surprise thế nào là quyết định sản phẩm, không tự mặc định.

Basic attack hiện tại tiếp tục đường cũ, không biến thành skill sample hoặc sửa hệ số root/basic để phù hợp đề xuất này.

## Active lifecycle và cancel

```text
Ready -> Validating -> Windup -> Released -> InFlight/Impact -> Completed
                        |            |             |
                        +------------+-------------+-> Cancelled/Interrupted
```

Mỗi activation có ID, owner HeroData reference, Hero LifeVersion/SpawnVersion, encounter generation, definition revision, weapon identity tại release và target identity. Mỗi hit có hit ordinal; khóa `(activationId, hitOrdinal, targetLife)` chống callback trùng. Chưa release thì cancel không impact; release rồi target stale thì retire projectile, không tìm target mới để cấp thưởng ngoài policy.

Đổi vũ khí/mất compatibility trước release: đề xuất cancel windup. Sau release: đề xuất giữ snapshot cho hit đã phát, chỉ tiếp tục nếu owner/target/encounter còn hợp lệ. Đây là lựa chọn cần duyệt; không để tên family đổi làm projectile cũ dùng stat vũ khí mới.

Pause không tiến timer game-time hoặc nhận effect mới; Resume tiếp tục cùng activation, không release lặp. Death, Load, Retry, Travel, New và Menu hủy mọi activation cũ trước bind/life mới. Re-equip cùng item không được bypass nhịp attack. Không resume target/projectile references từ Save. Scheduler active sau này phải phối hợp CombatManager, tránh hai đồng hồ cùng release một action; mana/cooldown/cost chỉ được thêm theo nhiệm vụ riêng.

## Persistence / versioning — chưa thêm field

Đề xuất thành phần `martialArtsVersion`, danh sách `{id, rank}`, loadout IDs và component công pháp nếu cần tách authority. Không lưu definition copy, live target, coroutine, activation hoặc cooldown nếu chưa có policy resume. Old save không có component -> empty learned/loadout; reset template defaults trước overwrite. Không reinterpret support profession/recipe DTO.

Kiểm version, IDs, rank, duplicates và slot policy trước mutation trên candidate mới. Unknown future version phải fail closed và không ghi đè. Unknown definition cần policy preserve/quarantine/refuse rõ ràng; không grant skill thay thế. Bản cũ của executable chưa biết version mới có thể bỏ field khi save lại: cần giải pháp downgrade riêng trước phát hành. Không coi guard equipment hiện tại là bảo vệ combat skills.

## Ba ví dụ minh họa — toàn bộ là đề xuất

| ID minh họa | Loại / compatibility | Hiệu lực minh họa, chưa có thông số production |
|---|---|---|
| `proposal:art:sword_form_a` — Kiếm Thức Mộc | MartialArt Active; Sword; root policy WeightedCounterOnce nếu duyệt profile/element | Một accepted target, Physical source; chuyển Bow giữ learned, mất eligibility. Hệ số/cost/timing/element chưa chốt. |
| `proposal:art:bow_form_a` — Tiễn Pháp Thủy | MartialArt Active; Bow; root policy EligibilityOnly nếu duyệt điều kiện | Snapshot Physical, một projectile; root eligibility không tự cộng damage. Tiêu chí root và effect element chờ duyệt. |
| `proposal:cultivation:breathing_a` — Điều Tức Pháp | CultivationMethod Passive; unrestricted; root policy None | Một modifier từ danh sách stat đã có, owner prefix riêng; stat/magnitude chờ duyệt. Chưa thêm ManaRegen hoặc tài nguyên chỉ vì tên công pháp. |

Không tạo asset, grant, học, UI activation hoặc mở Staff drop từ các ví dụ này.

## Kiểm thử bắt buộc cho nhiệm vụ sau

- Learn/upgrade/select fail không mutation; đổi Sword→Bow→Sword giữ rank/loadout và bật/tắt đúng eligibility, không class gate.
- Hai nguồn Attack khác nhau, Magical=0, mixed parts, root tier/weight/defender tại impact; root neutral=1 và pair khắc theo bảng hiện có, không square multiplier.
- Passive rebuild/equip/Load nhiều lần giữ đúng một modifier mỗi source, không xóa gear modifiers.
- Cancel mỗi pha, target chết/despawn, owner life mới, pause/resume, weapon swap trước/sau release; duplicate hit/death chỉ một HP mutation/receipt.
- Legacy/current/future Save, missing IDs/duplicate loadout/invalid rank, missing field từ template bẩn, save failure và downgrade policy.

**NOT RUN:** không có runtime võ học/công pháp hoặc các test hợp đồng trên được chạy trong v0.4. Các ca weapon-basic đã chạy chỉ xác minh đường basic/nhãn trace hiện hữu.
