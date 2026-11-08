# Production Loot & Forge Economy — 05/11/2026

## 1. Repository

- Branch: `feature/monster-awareness-ambush`; worktree `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- HEAD đầu: `48b2b71a987a5765db04454f2ec4b9c10acf6096`; working tree sạch.
- Commit mã/test: **`8d6b12c994334ccb50a60934ff455d88515372d1`** — cấu hình/đường thưởng production, giữ receipt, khóa dev grants trong Release và kiểm thử. Commit chứa báo cáo này bổ sung tài liệu.
- Không đổi nhánh/reset/merge/push, không chạy Windows Player hoặc can thiệp Firewall. Không đổi Save schema, công thức Combat/Forge, camera, scene hoặc artwork.

## 2. Hợp đồng hiện tại đã xác minh

| Thành phần | Hành vi trước thay đổi / dữ liệu lưu |
|---|---|
| Death | Chỉ nhận lethal damage hợp lệ của actor đã đăng ký; `TryClaim` trước EXP/reward/despawn. EXP theo cấp quái, Hard Mode nhân 3; không sửa. |
| Chống trùng | `CombatLootRewards` giữ receipt theo owner/run/encounter/life/spawn; terminal trước factory/API, hủy context khi đổi vòng đời. Đây là ledger runtime, không phải ledger lưu xuyên crash/checkpoint. |
| Loot thử A | `controlledLootTest` chỉ Editor/Development; kill hợp lệ cấp primary một `sample:iron_sword`, Common, cấp 1, main stat 10, chưa trang bị. Off trước đây luôn NoDrop. |
| Presentation | Award vào kho ngay lúc chết; sprite kiếm chỉ là cosmetic, không cấp đồ lúc kết thúc/hủy. Chụp chân trước despawn; sprite sẵn có chỉ hỗ trợ `sample:iron_sword`. |
| Inventory/Equipment | Stack `itemId/quantity`, giới hạn 9999; gear có `instanceId` duy nhất và slot trong một canonical list. `EquipmentCatalog` có hai hộ phù; `EquipmentFoundation` có sword/bow/staff và các slot cơ bản. |
| Forge | `material:refinement` / “Nguyên liệu rèn”; ItemLevelUp và NormalUp tốn 1, SpecialUp tốn 10. Các thao tác giảm không hoàn nguyên liệu. Có giới hạn/metadata/history; không thêm công thức. Không thu gold. |
| Khởi tạo/dev | New Run đang cấp hai hộ phù mẫu một lần qua `GrantSamples`; giữ hành vi đã có. Nút dev cấp kiếm rèn mẫu và +100 nguyên liệu; trước đây API chưa bị khóa ở Release. |
| Save/Load | `SaveManager` dùng JsonUtility trên EntityDataSO: EXP/level/map, inventory stacks, gear/slot/roll/Forge metadata/history, gold, profession/recipe/support-skill fields và cờ sample. Không lưu animation/receipt hoặc cấu hình GameManager. |
| Loại quái / bảng thưởng | Không tìm thấy rank thường/tinh anh/boss trong death/spawner/data liên quan. Hard Mode là chế độ, không phải rank. Không có bảng production hoặc nguồn material đã định nghĩa. Hai hằng số Thiên/Tiên không đủ quy tắc chọn template/cấp/chỉ số/các phẩm còn lại. |

## 3. Phần đã nối production

- `ProductionLootConfig` là dữ liệu Inspector nằm trong **GameManager → Production rewards → Production Loot**; mặc định `configured=false`, entries trống. Theo trả lời của người dùng: **“Chưa chốt — để chưa cấu hình”**. Không ghi tỷ lệ/thưởng mặc định vào scene.
- Đường bình thường gọi `CompleteProduction` khi profile thử không bật. Nó dùng **cùng receipt/claim**, không tạo ledger thứ hai; production có thể hoạt động trong Release mà không cần cờ test. Profile A là override dev riêng, không cộng cả hai nguồn vào một kill.
- Bảng tối thiểu hiện biểu diễn các kết quả **loại trừ nhau** của một roll: tối đa một entry/kill; tổng xác suất ≤1, phần còn lại NoDrop. Root chưa cấu hình không roll, không cấp thưởng. Đây là hợp đồng cấu hình kỹ thuật chờ chốt sử dụng, không phải bảng cân bằng cuối.
- RNG production dùng System.Random riêng, không dùng combat RNG. Kiểm toàn bảng/ID/phiên bản trước cấp; recheck encounter sau chọn. Invalid/overflow/failure không reroll, không cấp một phần. Lỗi cấp/config đã bật đi theo fail-closed `Faulted` của service; cần sửa dữ liệu và khởi tạo lại CombatManager trước khi tiếp tục thưởng.
- Gear dùng `CreateCommon` với **template/cấp/main stat khai báo rõ**, không gọi factory hay sao chép bảng test A. Material dùng `EquipmentInventory.Add` thật. Reward đã committed vẫn tồn tại nếu UI/VFX chưa sẵn sàng.
- Dev API cấp hộ phù/kiếm/nguyên liệu và các nút InventoryGrant chỉ dùng được ở Editor/Development. Trong Release, API từ chối và control bị vô hiệu; không tắt Forge thật. Không đổi bố cục/hitbox các panel đã đạt kiểm thử.

## 4. Reward/resource được hỗ trợ

- **Đang hoạt động trong phiên bình thường:** EXP và hành vi starter hai hộ phù cũ; không thêm gold hoặc item drop mới mặc định.
- **Đã nối nhưng chưa có cấu hình chơi thật:** gear Common với ID trong catalog hiện có; mỗi instance quantity 1, cấp hợp lệ và main stat dương khai báo rõ. Material chỉ nhận đúng `material:refinement`, lượng 1..9999.
- Production config không tạo rarity/currency/material mới. Gold vẫn được lưu nhưng chưa có nguồn thưởng/chi phí Forge; không tự dùng hai tỷ lệ Thiên/Tiên làm bảng rớt hoàn chỉnh.
- Sword giữ VFX đang có. Material và template không có sprite sword không bị vẽ nhầm thành kiếm; hiện phản hồi qua kho/log, chưa thêm artwork/VFX khác.

## 5. Cấu hình chưa chốt / mức hoàn thành

**Vòng production thực tế: BLOCKED vì chưa cấu hình theo quyết định của người dùng.** Chơi bình thường vẫn NoDrop cho đồ/material khi bảng tắt. Không coi đường code hoặc test có cấu hình fixture là vòng production đã hoàn thành.

Chưa chốt: template nào, tỷ lệ/NoDrop, lượng material, cấp/chỉ số gear; phạm vi theo map/cấp/chế độ; có cần nhiều reward độc lập trong một kill hay dùng bảng tối đa một kết quả như hiện tại. Không thêm rank quái, boss, công thức hay nguồn farm để lấp khoảng trống.

## 6. Kiểm thử thực chạy

| Kiểm tra | Kết quả |
|---|---|
| Ca production mới — Mono/UnityStub, Editor defines | **PASS 17 ca riêng**, tổng hợp các lượt lọc để không chạy lại ca đã đạt: unconfigured, bucket/NoDrop, ID/giá trị sai, overflow, schema tương lai, stale/reentry, callback trùng, kill thật của fixture → VFX/Inventory, gear/equip/Forge và dev guard. |
| Mono không có UNITY_EDITOR/DEVELOPMENT_BUILD | **PASS 3 ca**: hai ca kill nhận gear/material → equip/Forge, một ca dev grants bị từ chối. Không phải Windows Player acceptance. |
| Mono `--loot-visuals` | **PASS 23/23**, profile A/NoDrop, duplicate/cancel/pool và VFX tiếp tục đúng sau đổi callsite. |
| Unity 6000.5.0f1 Play Mode | **PASS 7/7**: 6 ca CombatLootTests tương thích và 1 ca mới `ProductionRewardsForge_SaveLoadSupportsExistingSchema`. |
| Native Save/Load mới | Award gear +14 material bằng config fixture → equip → Forge thường/đặc biệt còn 3 → save/load qua SaveManager thật. ID/slot/roll/history, material, gold 123 và damage đã rèn được giữ; Load không rewrite file. Dùng GUID/marker và SaveManager cấu hình trước Awake, không save thật. |
| Kiểm diff / metadata | **PASS** `git diff --check`; checker 0 lỗi, 3 cảnh báo folder có sẵn Lake/Sea/Steppe. Chỉ dọn font cache/meta do lượt Unity tự sinh. |
| Reward production bằng bảng đã chốt | **BLOCKED**, chưa có bảng/nguồn nguyên liệu được duyệt. |
| Windows Player / native input / UI hình ảnh mới | **NOT RUN**, không mở Player bị chặn hoặc chạy lại ma trận UI. Bản Player cũ được build trước commit này, không dùng làm bằng chứng cho mã mới. |

Các số 1, 7, 14 và xác suất trong test là **fixture**, không phải giá trị production. Kill nhận thưởng và không thưởng được so cùng seed, giữ RNG/EXP; công thức/range/AI không sửa. Một lỗi compile của test dùng `Random.value` không có trong stub được đổi sang API Range đã có; ca so RNG ban đầu dùng hai fixture tạo trước khi seed đã được sửa seed trước dựng fixture. Giữ assertion, chỉ chạy lại ca lỗi và các ca chưa chạy. Không sửa runtime Combat để ép PASS; không có FAIL còn lại ở các ca đã chốt.

Không chạy lại Magic đã đạt hoặc các bộ Save/UI không bị ảnh hưởng. Các file SaveManager, EntityDataSO và EquipmentRefinement không đổi.

Log/result ngoài Git: `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\production-loot`; log chi tiết tại `work/production-loot/` cùng workspace. Lệnh: `Tests~/run.sh --production-rewards [lọc tên]`, `--loot-visuals`; compile guard dùng `IDLEGAME_TEST_DEFINES=RELEASE_VALIDATION`, mặc định runner vẫn UNITY_EDITOR. Native XML: `production-native-round1.xml`.

## 7. Vấn đề còn tồn tại

- Chưa có cân bằng/source production nên không đủ cơ sở kết luận vòng farm chơi thật hoàn thiện. Không có award vô hạn/certain mặc định.
- Bộ receipt không bổ sung ledger save/checkpoint; giữ nguyên luồng Load/replay hiện có, không hứa chống farm lại qua crash bằng schema mới.
- Sprite loot chỉ có kiếm; reward catalog khác/material chưa có VFX riêng. Starter hộ phù và Forge vẫn mang các quy tắc prototype đã có, không tự nâng thành balance chính thức.
- Release dev-control guard đã kiểm compile/API bằng Mono; cần build/nghiệm thu Windows riêng khi điều kiện cho phép. Không dùng công cụ dev làm nguồn progression production.

## 8. Quyết định cần người dùng phê duyệt tiếp

1. Bảng reward cụ thể và NoDrop; chấp nhận tối đa một kết quả/kill hay cần nhiều nguồn độc lập.
2. ID gear, cấp/main stat, phạm vi map/chế độ và lượng/tỷ lệ `material:refinement`; không dùng thông số fixture làm mặc định.
3. Quy tắc phẩm cao/stat generation trước khi nối hai hằng số Thiên/Tiên; có cần gold trong vòng kinh tế hay tiếp tục không dùng.
4. Giữ hai starter hộ phù hiện có hay đổi thiết kế khởi đầu; artwork/phản hồi cho reward không phải kiếm nếu đưa chúng vào bảng.

Đã hoàn thành phần cấu hình/đấu nối và kiểm chứng độc lập có thể làm an toàn. Phần phụ thuộc quyết định sản phẩm được để chưa cấu hình đúng yêu cầu; dừng sau báo cáo, không push/merge.
