# Martial registry → Learned State/Loadout: ghi chú v0.5

06/11/2026. **IMPLEMENTED:** registry/eligibility thuần. **PROPOSED:** learned/loadout persistence và effect bridge; chưa có skill production.

## API đã có và authority

[MartialArtsRegistry](../../Assets/Script/MartialArtsRegistry.cs) nhận definitions explicit qua `TryCreate`, kiểm stable ID/revision/duplicate và mọi enum/weapon/root/unlock policy trước publish registry. Không có catalog mặc định, resource loader, class/profession requirement hoặc startup hook.

- MartialDefinition immutable/copy input: ID/revision, CultivationMethod/MartialArt, Passive/Active, allowed families, root policy/requirements và unlock fact IDs.
- `Evaluate(id, MartialEligibilityContext)` trả eligibility/reason/definition và `IsPassive`. **Không có activation, cost, damage hoặc learned-state mutation.** Eligible chưa có nghĩa learned/selected/ready-to-cast.
- Explicit empty allowed-weapon list nghĩa unrestricted; null policy bị validator từ chối. Context weapon=null nghĩa thiếu thông tin; nhân vật không cầm vũ khí hợp lệ phải được caller biểu diễn là **Unarmed**, không coi Unarmed là Sword hoặc missing context.
- RequiredRoots là AND theo root/min tier 1–5; RequiredUnlocks là AND theo fact IDs. Context chỉ đọc bản sao dữ liệu; root weights, tương khắc và damage chưa tham gia M01. WeightedCounterOnce là metadata cho adapter tương lai, chưa có multiplier mới.

Authority definition thuộc catalog được duyệt; learned/rank/loadout thuộc Hero canonical của GameManager trong tương lai; eligibility derive từ weapon/root/facts lúc hiện hành. Không gán learned records vào registry toàn cục.

## Entry point, input và output dự kiến

1. Catalog bootstrap riêng đọc definitions production đã được duyệt và gọi TryCreate trước mở UI. Revision phải >=1, duplicate ID khiến registry candidate bị từ chối toàn bộ; không chọn definition thắng theo thứ tự.
2. GameManager command Learn/Upgrade/Select kiểm dữ liệu learned/rank/slot/cost đã duyệt trên candidate, rồi mới mutation. Không gọi Evaluate như bằng chứng đã học hoặc đã trả cost.
3. Equip/unequip/Load/selection refresh tạo MartialEligibilityContext từ `WeaponBasicAttack.Family(Equipped(hero))`, root/tier đã validate và facts do authority tương ứng cung cấp. Không normalize root live chỉ để preview.
4. Lựa chọn được giữ khi weapon tạm không tương thích; UI hiển thị reason. Nếu policy sản phẩm muốn bỏ selection/slot phải được duyệt riêng; M01 không thực hiện quyết định này.
5. Passive bridge tương lai chỉ rebuild own source prefixes trong CombatStats; active bridge dùng CombatManager accepted impact/lifetime. IsPassive không được chuyển thành active action. Không áp root hai lần hoặc tạo scheduler cạnh CombatManager.

Input eligibility: definition ID, family nullable/explicit Unarmed, root-tier snapshot, unlock facts. Output: reason ổn định hoặc eligible + definition phân loại. Root/unlock facts không chứng minh realm chính thức; nếu có realm prerequisite phải có adapter authority rõ, không lấy RankDatabase.

## Persistence/migration và nguy cơ

Tái sử dụng EntityDataSO canonical, `GameManager.TryLoadSlot` candidate-load-before-swap, component guards trong SaveManager, CombatStats/sourceId và WeaponBasicAttack. Không đổi nghĩa `LearnedSupportSkill`, profession, recipe hoặc enum WeaponFamily.

Khi được giao M02: component version và `{definitionId, rank}`/loadout IDs riêng; old save thiếu component -> policy empty được duyệt, reset template trước overwrite; unknown future/invalid rank/duplicate slot phải reject trước swap. Definition missing cần policy preserve/quarantine/refuse, không xóa ID rồi save đè. Không lưu cả definition copy làm authority thứ hai.

Rủi ro chính: mất learned records khi đổi weapon/definition; old binary strip field; passive nhân modifier sau Load; source prefix xóa gear; active dùng Physical thay Magical hoặc root multiplier lặp. Chưa có resource/mana/cooldown runtime trong v0.5; không thêm chúng khi triển khai learned state vì “tiện”.

## Test và quyết định còn lại

Đã chạy M01 bằng Mono/UnityStub: bốn family, missing/mismatch weapon, root tier/fact reasons, duplicate/invalid definition/revision, unknown ID/context, Passive classification, input copy và learned fixture không mất khi đổi Sword→Bow→Sword.

Trước phát hành: content registry thực, rank/slot invariants; Learn/Upgrade fail không mutation; old/current/future JsonUtility thật; missing definitions/downgrade; repeated Load/equip passive không double modifier; active cancel/life/projectile khi sau này triển khai. Fixture học hiện tại chỉ là HashSet test, không bằng chứng learned production đã hoạt động.

Chờ người dùng: definitions thật, điều kiện học/nâng/cost, slot capacity, missing-ID/downgrade policy, root/effect/rounding và activation rules. Không biến ba ví dụ proposal v0.4 thành production content. Ghi chú này không mở M02–M05 tự động.
