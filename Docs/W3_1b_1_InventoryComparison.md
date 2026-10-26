# W3.1b.1 — Inventory Equipment Comparison UI

Baseline: `0982078f471910e14d01603e31673da7a59a9cea`, main, 26/10/2026.
Đã fetch và xác nhận remote/local cùng SHA trước triển khai; working tree sạch.

## Phạm vi và cách dùng

Chọn đồ trong Kho đồ, vùng chi tiết mặc định hiển thị **So sánh**. Mỗi trang
hiển thị một chỉ số thay đổi với Current, Preview, Delta; xanh khi tăng, đỏ khi
giảm, trung tính nếu không đổi trong thế hiện tại. Nút **Chi tiết** giữ thông tin
item/affix hiện có, có phân trang để không che nút thao tác. Hai nút mũi tên cạnh
số trang chuyển nội dung. Không thay scale chung, scene, prefab hoặc theme asset.

- Nguồn số duy nhất: `EquipmentStatPreview`. Không sửa helper, combat hay schema.
- Chỉ hiện chỉ số đổi: HP tối đa, ATK, giáp/kháng, bonus sát thương vật lý/phép/
  nguyên tố, tốc đánh, bonus di chuyển/thi triển, giảm hồi chiêu, hồi phục và hồi
  HP khi hạ quái nếu thực sự bị ảnh hưởng. Ratio được hiển thị bằng phần trăm;
  attack speed là chỉ số, không tự tính thành đòn/giây bằng công thức khác.
- Đồ trong túi xem trước Equip; đồ đang mặc xem trước Unequip. Legacy charm
  kiểm tra sức chứa khi trả charm cũ, không giả lập thay thế qua Pháp bảo modern.
- Equip mặc định và Preview dùng chung hàm chọn slot nhẫn. Hai nhẫn đã mặc thì
  mặc định thay trái; khi trái có đồ và phải trống thì tự chọn phải. Nếu nút
  **Nhẫn phải** có đích khác, các trang cuối ghi rõ kết quả riêng cho nút đó.
- Slot cũ lưu trên nhẫn đã tháo không còn ép Preview/Equip mặc định vào phải;
  slot được chọn theo template và tình trạng hai ô hiện tại.
- Affix hidden/locked không tham gia Preview theo evaluator hiện có; không
  gọi Normalize, gắn modifier hay thay HP khi xem. Tab Chi tiết giữ cách hiển
  thị `???` cho hidden và `[Khóa]` cho affix đã biết như Inventory trước đây.
- Selection/filter/page/reopen cập nhật hoặc xóa kết quả cũ. Thay đổi stat/gear
  bên ngoài được kiểm tra mỗi 0,2 giây unscaled khi Inventory mở, kể cả pause.
- Dưới chiều cao panel 540px, túi dùng 10 ô/trang thay 20 để giữ vùng đọc và
  các hàng navigation/Forge/Equip tách biệt. Đồ không bị xóa, chỉ đổi phân trang.

## Kiểm thử thực chạy trong Cloud

```bash
bash Tests~/run.sh --inventory-compare
python .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json --max-issues 0
python Tools/verify_agent_skills.py
git diff --check
```

Compiler/runtime Mono được giải nén trong `/tmp` do môi trường không có Mono,
không cài dependency hoặc cấu hình vào repository. Đặt PATH tới compiler đó khi
chạy runner trong phiên Cloud này; máy đã có mcs/mono dùng lệnh trên bình thường.

**18 ca Mono/UnityStub PASS**: 5 regression helper hiện có, 7 regression UI/action
mới, 4 geometry/window contract và 2 inventory theme baseline. Bộ mới kiểm tra:

1. Charm HP và ATK, thay charm và tháo; Current/Preview/Delta và màu.
2. Gear hợp lệ, thay gear, chọn đồ đang mặc, tháo; tổng combat bằng Preview.
3. Nhẫn trái, tự chọn phải, hai nhẫn đang mặc, nút phải riêng.
4. Artifact/charm xung đột hai chiều; kho đầy loại charm trả về; sai slot, item
   không hợp lệ, material không thể equip: có lý do, không có số giả.
5. Hidden/locked không lộ số qua so sánh; thế không hưởng bonus là Delta 0.
6. Xem Preview không đổi HP hiện tại, modifier list, assignment hay isDirty;
   external stat change, item biến mất, filter, page và reopen không giữ kết quả cũ.
7. Vùng text/navigation/Forge/Equip tách nhau ở 600/800/1150, compact và expanded.

Unity GUID checker: **0 ERROR, 62 WARNING**, cùng số baseline cũ (58 missing-script
chưa xác nhận vì thiếu PackageCache, 3 missing-meta thư mục, 1 orphan-meta).
Không sửa asset/GUID theo các warning đó. Skills verifier và diff check PASS.

## Unity/Windows chưa nghiệm thu

Thêm **9 ca NUnit Play Mode** trong
`Assets/Tests/Presentation/PlayMode/InventoryComparisonTests.cs` cùng `.meta`:
ScriptableObject thật, purity snapshot JSON, charm totals, ring destination,
invalid/blocked, hidden/locked, và RectTransform 600/800/1150. Test dùng reflection
vì assembly test không tham chiếu Assembly-CSharp; fixture UI inactive, không chạy
GameManager.Start hoặc tạo SaveManager. Chạy ca layout trong scene sạch không có
GameManager/UIManager/SaveManager, chỉ cleanup object do fixture tạo.

**Chưa compile/chạy bằng Unity Editor và chưa chạy Windows Player trong Cloud.**
Mono/UnityStub geometry không chứng minh font wrapping, rendering, DPI, native
input hoặc khả năng click. Không chạy full suite Mono/save I/O trong task này.

Nghiệm thu thủ công:

1. Mở MainGame, New/Continue bằng profile test riêng, mở Kho đồ; chọn hộ phù HP
   rồi ATK, so Current/Preview/Delta và màu; trang bị, chọn ô đang mặc rồi tháo.
2. Dùng đồ có sẵn hoặc nút đồ mẫu dev để kiểm gear và chi tiết; kiểm đổi thế không
   tự áp bonus sai. Không thay loot, damage hoặc tỷ lệ rèn để tạo kết quả PASS.
3. Với fixture nhẫn: trái trống → trái; trái có/phải trống → phải; cả hai có → mặc
   định trái và Preview riêng cho nút phải. So tổng thực tế sau bấm đúng nút.
4. Kiểm Artifact/charm bị chặn, material, affix hidden/locked, phân trang túi và
   phân trang chi tiết; đóng/mở, external stat/equipment change khi panel mở.
5. Thử 600/800/1150, màn hình đủ chỗ mở rộng và fallback overlay; text đọc được,
   không che Equip/Unequip/Close/Forge; nút mũi tên và hai tab nhận click đúng.
6. Kiểm Windows transparent Player, DPI thực tế, pause và đóng Inventory trở lại
   cửa sổ compact. Ghi SHA, kích thước, ảnh/log nếu overflow hoặc input không đúng.

Không triển khai W3.1b.2, reward, camera/movement/wave, EXP, affix/rarity/rèn,
SaveManager/save schema hoặc B2 trong thay đổi này.
