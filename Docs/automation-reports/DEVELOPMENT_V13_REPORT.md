# Development v1.3 — Bách Luyện Tiên Đồ: Phá Thiên Quan

07/11/2026 · **MainGame và Windows Development Player đã chạy trận thật trong profile cô lập. Production vẫn OFF.**

## 1. Repo, baseline và phạm vi

`lavievn/IdleGame`, `feature/monster-awareness-ambush`, worktree **D:/IdleGame/Project**. Baseline sạch `565875c997b7e36ade23730e3a0b4f72e0df2e44` (Development v1.2). Unity **6000.5.0f1**, backend Mono2x, scene MainGame duy nhất. Không có Unity/Player/Git writer của phiên trước tại baseline. Không push, merge, reset, rebase, đổi nhánh, chuyển project/cache/build về C hoặc tạo nhiều writer vào worktree.

Đã đối chiếu v1.2 report/foundation, v1.1 report, Capacity, Dual Progression, Save Migration, Blood Essence và nguồn Combat, Hero AI, SaveManager, GameManager, WorldMap, UI. Baseline xác nhận v1.2 chỉ có domain/fixture hai pha; World Qi v2/Capacity production hoạt động; trial gate OFF, chưa có encounter, penalty production hoặc persistent trial. Chạy trước thay đổi: **188 ca liên quan PASS**, nhóm v1.2 77 assertion PASS. Unity/Save/Build skill của repo được áp dụng, giữ profile GUID/marker và đường dẫn riêng trước Awake. Không đọc/ghi Save cá nhân để test.

## 2. Điều tra đòn đơn thể hạ nhiều quái

**Chưa tái hiện lỗi basic trong phạm vi đã thử.** 16 ca cho từng lớp Mono, MainGame và Windows Player: Sword/Bow/Staff/Unarmed × 2/3 quái × đứng chồng/tách. Fixture đặt quái 1 HP, cho đúng một phát đòn qua logic thật. Bản native/Player ghi số release, accepted hit và kill theo cùng release ID; mỗi hàng là **1/1/1**, quái còn lại sống. Không thay số liệu damage để chữa một lỗi chưa xác nhận. Fixture không phải farm tự nhiên.

Combat source: melee gọi đúng target; Bow/Staff basic giữ original target và lifetimes. **Legacy Magic** có vòng nổ theo bán kính; control native/Player dùng nhánh legacy tường minh, 1 release/3 hit/3 kill. Không đồng nhất phép AoE legacy với Staff basic. Nhiều phát/actor hoặc cleanup wave có thể nhìn giống một nhịp đánh, nhưng không gán các khả năng đó là nguyên nhân chắc chắn của quan sát người dùng. Một release basic được xác nhận có nhiều accepted target sẽ là lỗi. Evidence: `single-target.csv` trong hai hồ sơ runtime, `final-mono/single-target-v13.log`. Vẫn cần tình huống thực tế/trace nếu hiện tượng tái diễn ngoài mẫu.

## 3. Gameplay và gate

Trong owned fixture, **Độ Kiếp** mở encounter Phá Thiên Quan ngay trong MainGame. Vật thể phong ấn có hình học placeholder, HP, phòng thủ, target lifetime và phản kích định kỳ. Không dùng sprite quái/AI/chuyển động của quái thường, không tạo scene hoặc actor Hero thứ hai. Cắt projectile/wave cũ, dừng exploration/tu luyện trong trận, dùng Hero/camera hiện tại; khi xong triển khai lại đời Hero hợp lệ trên map/progress cũ và tái sinh wave đang dở. Không khôi phục từng HP quái/đạn giữa chừng.

Chỉ khi actual foundation tới bình cảnh, successor được profile khai báo, actor đang sống và triển khai, session/config đúng, không injury cấm, Burning/pending settlement, chuyển đời hoặc active attempt khác. Không dùng Total Level hay EffectiveCombatRealm. Gate source **`HeavenlyTribulationEnabled=false`**, profile thường không có encounter config. Profile riêng `balance:tribulation-v13-fixture` cài trước Awake trong verifier owned. Trúc Cơ chưa mở production; chưa duyệt target data, số phạt, encounter balance và chính sách resume/cancel/crash thực tế. Xem [hợp đồng gameplay và đề xuất config](../architecture/HEAVENLY_TRIBULATION_GAMEPLAY_V1.md).

## 4. Combat, bảo vệ Hero, thắng/thua

Sword/Unarmed melee; Bow physical và Staff magical có projectile thật, dùng target life và battle version. Cùng sprite Hero. Tầm vũ khí giữ nguyên, gặp cap encounter chỉ có thể ngắn hơn. Phong ấn nhận damage qua phòng thủ, trả đòn qua pipeline damage Hero có Armor/Resistance. Cùng damage rolls chiến đấu đã có, không có RNG thắng/thua đột phá. Không cấp EXP, loot, heal-on-kill, wave completion hoặc reward thường. Domain xét HP hai phía cuối frame và **double KO thất bại**; HP sống, actor life, owner/epoch, source realm/clock kiểm lại tại commit. Tests cover duplicate/expired/stale receipt, wrong epoch/life/target/revision, injury và Burning/pending.

Thắng chuyển `fixture:truc-co` Sơ kỳ, căn cơ **0** qua `RealmBreakthroughTransition`, capacity **3/2** theo fixture, không grant EXP/Level/Blood. Tu luyện tiếp ở tick sau commit. Thua tự hồi sinh qua candidate/deploy bình thường, giữ đại cảnh giới, injury, Level/EXP, map wave, Inventory, Equipment, gold và Forge progression. Penalty V1 chỉ trừ căn cơ đã luyện hóa: Luyện Khí/Trúc Cơ tối đa1 tiểu cảnh giới, Kim Đan trở lên tối đa2; loss có thể nhỏ hơn trần. Fixture yêu cầu500, từ3600→3100 Hậu kỳ, phải trở về bình cảnh để thử lại; không có mức phạt production. Reserve **1234→1234** cho cả sáu trận mỗi môi trường. Kiểm cả phần lẻ clock .037s chính xác qua swap; Travel sau settle xử lý fraction một lần, không sửa manual slot.

## 5. Save, crash và restart

Checkpoint nguồn đầy đủ phải commit trước bắt đầu. Receipt Active/terminal nằm trong cùng Hero snapshot; phiên bản, ID/life/epoch/config/realm/phase, outcome/appliedId, penalty, fingerprint và nguồn SHA được validate. Không thay version/config của Save tu luyện 1/2/3. Backup SHA bất biến; stage/flush/replace với expected Active hash giữ từ lúc vào, xác minh bytes rồi swap. Save lỗi chưa commit giữ Hero hiện tại và hiện chờ ghi; retry một lần có hiệu lực, không tạo reward/penalty lần nữa. Lỗi sau replace chỉ được nhận là hoàn tất nếu exact result và backup hợp lệ. Không chấp nhận snapshot cạnh tranh mới làm expected hash. Config upgrade/ordinary writes không bỏ Active liability.

Native kiểm lỗi trước temp/replace, Windows file lock thật, đối thủ ghi snapshot mới, sau replace, backup và repeated restore. MainGame và Windows lỗi ghi tại trận Sword không báo thắng hoặc đổi canonical cho tới retry thành công. Player process mới đọc chính xác cả Save success và failure; Active checkpoint từ chối load/reset, không tự resume, thưởng hoặc phạt; hash **vẫn không đổi sau khi Player thoát**. Save, Load, Menu, New/Retry/Travel trong active/awaiting commit bị khoá trong fixture. Không duyệt luật phạt khi thoát; Active bị gián đoạn cần review, chưa resume HP/timer. Không tuyên bố bảo đảm mọi power-loss/process-kill hoặc filesystem CAS đa writer. Xem [hợp đồng Save](../architecture/TRIBULATION_SAVE_TRANSACTION_V1.md).

## 6. Fixture balance và thời gian quan sát

Phong ấn300 HP, Armor/Magic Resistance20, raw strike12 mỗi1s, cap range200; explicit config v1, không phải cân bằng chính thức. Hero Level1, EXP0, profile cố định căn cơ/reserve/energy; fixture tính modifier delta từ **chỉ số thực sau ApplyHeroBalance** để đạt HP/Attack dưới đây; Sword/Bow/Staff dùng mẫu gear. Attack là nguồn damage phù hợp vũ khí, trước roll/defense. Bộ trang bị/bag/Forge và gold777 đối chiếu nguyên snapshot. Hero weak và same-hp-armor cùng24 HP/78 Attack, chỉ đổi Armor900 ở đối chứng; cùng nguồn map/identity trong mỗi lần chạy. Các số thời gian game-time không gồm UI pause/load. Khác biệt giữa Editor/Player không phải benchmark cân bằng chính thức.

| Fixture | HP / Attack / Armor thêm | MainGame | Player | Sát thương phản kích đầu ở Player | HP trước hồi sinh/đổi đời | Căn cơ trừ |
| --- | --- | --- | --- | ---: | ---: | ---: |
|weak / Unarmed|24 / 78 / 0|Failure 4.001 s|Failure 4.011 s|6|0|500|
|sword / Sword|600 / 185 / 0|Success 4.202 s|Success 4.277 s|6|576|0|
|bow / Bow|600 / 185 / 0|Success 2.983 s|Success 3.042 s|6|582|0|
|staff / Staff|600 / 185 / 0|Success 4.384 s|Success 4.435 s|6|576|0|
|unarmed / Unarmed|600 / 185 / 0|Success 4.203 s|Success 4.262 s|6|576|0|
|same-hp-armor / Unarmed|24 / 78 / 900|Success 8.405 s|Success 8.542 s|1|16|0|

## 7. Kết quả phân lớp

| Lớp | Trạng thái và bằng chứng |
| --- | --- |
| Mono / UnityStub |188 related PASS trước/sau; 16 single-target mới PASS; v1.2 domain77 và v1.3 domain37 PASS. Không cộng số group executions thành assertion độc nhất.|
| Native Unity batch |54 kiểm tra v1.3 (37domain +17 checkpoint/IO) PASS. V09 migration/clock/133 nodes, V11 compatibility và capacity mô phỏng3660s/5starts/30–60–144FPS PASS.|
| MainGame Play Mode |16 basic + legacy AoE control,6 trận thật, actual HP/projectile, target cố định, one Hero, camera, respawn, Save fail/retry/Load/Travel và UI4sizes PASS. Result ghi4/4 phase; tương tác qua methods/hit handler.|
| Windows Development build |Succeeded, 0 errors / 0 warnings trong **lượt incremental cuối**, 674221582 bytes. Lượt đầu lỗi TMPDIR/Burst với1 error/57 warnings; giữ log, launcher sửa chỉ phạm vi tiến trình (native D path cho Unity, dạng Bash chỉ cho Mono).|
| Windows Player fixture |TrialV13 PASS,6 encounter thật,37 domain assertions,16 basic + legacy control, weapon/sprite/Save/respawn, `uiIssues=[]`.|
| Windows process mới |TrialReplay PASS: exact terminal success/failure, Active fail closed và không đổi bytes sau quit.|
| Windows Player mặc định |Matrix PASS,11 records, `uiIssues=[]`; natural farm, New/Continue, Inventory/Forge/Map/Character4sizes, combat4weapon, invalid/repeated Load, consent/backup, Retry/Travel/camera. Capacity PASS, V2 giữ rules tới consent rồi V3, old .037 fraction, Save/Load/UI4sizes.|
| Native chuột/bàn phím |**NOT RUN**. Gọi `HandleMouseClick` trên đúng hit rect là method-driven, không giả làm input OS.|

Farm mặc định chạy **30.017s** không override trong khoảng đo, 941 frames, mean 31.931ms, p95 32.678ms, 3 disk writes; raw result ghi Hero sống, 30 damage events, Level1/EXP14. v1.2 là một run tham chiếu khoảng30s/mean6.950ms, không phải A/B kiểm soát cùng map/RNG. Không tuyên bố tăng hiệu năng. `windows/matrix/player-performance.json` và `player-result.json` ghi số thật.

UI evidence: `main-game/` và `windows/v13trial/` có Character/Trial HUD tại **600×338, 800×450, 1150×647, 1360×680**, ảnh kết quả thắng/thua. Inspect ảnh600px: phong ấn hiện dạng vật thể; HUD không phủ thẻ Hero/Character. Matrix giữ các panel Inventory/Forge/WorldMap. Cảnh báo VideoPlayer/ComputeBuffer shutdown và log build ban đầu được giữ; không nới assertion để PASS.

## 8. Lỗi cũ, phần chưa nghiệm thu và v1.4

`OpeningFontRegression` lỗi `HUD never overwrites popup font: 18 != 16` đã tái hiện trên baseline trong v1.2. Không sửa, không gọi full-suite PASS; 188 là các nhóm mục tiêu. Các vòng sửa v1.3 được ghi log riêng: giới hạn assembly Editor, BOM checkpoint, fixture HP/Attack prototype, enum Forge, UnityStub clock, visual/HUD và temp path Burst. Không thay production formula/cân bằng vì lỗi fixture. Native input, retail/IL2CPP, đa writer Save và arbitrary power-loss **NOT RUN**. Production trial/Trúc Cơ **BLOCKED** bởi authored balance/penalty/interrupt policy; Active recovery hiện là bảo toàn, không tự tiếp tục.

Ưu tiên v1.4: chốt encounter HP/defense/strike, realm thresholds/capacity và trị số penalty; chốt thoát/cancel/resume với trial liability (bao gồm chính sách manual timeline rollback); nghiệm thu native input và receipt fault tại production boundary. Với quan sát multi-kill, thu release/target trace đúng weapon mode và life nếu tái diễn trước khi sửa Combat.

## 9. Commit, Git cuối và handoff

| SHA | Nội dung |
| --- | --- |
|`106caa4`|feat(combat): route a fixed Heavenly Gate through existing attacks|
|`f81b821`|feat(save): settle trial realm and penalty in a verified full snapshot|
|`691947d`|feat(gameplay): integrate gated tribulation and result UI in MainGame|
|`3f678df`|test: verify v1.3 combat checkpoint lifecycle and Windows replay|
|Commit docs cuối|Báo cáo này + gameplay/config proposal + transaction; SHA đầy đủ trong handoff ngoài repo.|

Runtime/test source sau commit là `3f678df382dc00f8abaf2ae96ffc01676e248b47`. Build/Player ghi `sourceHead` là baseline vì chạy **trước commit**; `tested-source-manifest.json` băm source workingtree tại build và khớp từng byte trước/sau khi commit. SHA256 EXE `e3c3a181dcb8bab365f7dae4788c8f8164dd1dfa8a5e51d5a717c04b1fc3f7e5`; Assembly-CSharp `fea0563f929cfbc2557a0eac183ed678511e5597e7bcda37bcce88507a0b9449`. Không commit Player/log/Save/.utmp/cache/ảnh. Scene, art, ProjectSettings/Packages, World Qi/capacity production, damage formulas và v1.1 migration writer được kiểm tra không đổi. Dọn đúng artifact import sinh từ baseline sạch (15 meta không tracked và TMP fallback); metadata checker **0 lỗi / 3 cảnh báo** thư mục Lake/Sea/Steppe có sẵn.

Bằng chứng: `D:/IdleGame/Automation/v13/evidence`, build/owned profiles `D:/IdleGame/Builds/DevelopmentV13Final`. Handoff ngoài repo: `D:/IdleGame/Automation/v13/evidence/handoff.json`; ghi HEAD cuối và clean status sau commit report. Tái chạy qua `D:/IdleGame/Launch.ps1 -Mode MainGameTrial` hoặc `-Mode Native -Method DevelopmentV13Verifier.RunNative`, `-Mode Mono -Group single-target-v13`/`gate-v13`; Player `-Mode Player -PlayerMode TrialV13` và `TrialReplay` với `-WindowsOutputRoot`/`-ReplayProfile` ở D. Repo bàn giao sạch, không push/merge/reset/rebase, không còn process test của phiên. **Không thực hiện Sleep theo chỉ đạo mới nhất.**
