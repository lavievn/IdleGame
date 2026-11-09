# Bách Luyện Tiên Đồ — Development 0.1.7

## Item & Equipment Foundation

Bắt đầu từ `4419000956660684d70dfd8d0506f0d1099fd98a` trên `development_0.1.5`. Tạo `development_0.1.7` từ đúng HEAD đã có trên GitHub ở nhánh 0.1.6.

### Audit và thay đổi

- Repo đã có template theo ID, instance theo ID riêng, kho sở hữu chung, tám slot (vũ khí, mũ, áo, giày, pháp bảo, dây chuyền và hai nhẫn), dòng phụ ẩn/khóa/hoạt động, Forge và loot tự nhận. Pháp bảo legacy dùng chung slot Artifact và phải được tháo trước khi dùng instance ở slot đó.
- Runtime và compare dùng chung cách phát sinh modifier theo ID instance và trạng thái dòng. Stat dùng nền nhân vật, cấp và điểm phân bổ, sau đó cộng flat và phần trăm tính trên nền theo evaluator hiện có. Giáp/kháng phép trong compare dùng ngữ cảnh đòn đến tương ứng. Preview Mana tối đa, hồi Mana và hồi HP ngoài giao tranh có cùng nền với Combat.
- Common chỉ áp dòng chính. Dòng phụ legacy vẫn nằm trong dữ liệu và có thể xem trong luồng Forge theo quy tắc cũ. Hoàng là Uncommon. Ba vũ khí mẫu lưu SkillID và kiểm tra với template khi cấp kỹ năng. Save cũ thiếu trường đọc từ template. ID lệch template không cấp skill. Dòng modifier hiện có là đường hỗ trợ đặc tính thụ động khi có dữ liệu được duyệt.
- Hòm đồ cho thấy slot, tình trạng đang trang bị, chỉ số chính và kỹ năng. Compare dùng cùng slot để hiện số hiện tại, số dự kiến, delta có dấu và trang kỹ năng được cấp hoặc mất. Các trang dùng bố cục phân trang hiện có.
- Forge giữ các bước cấp vật phẩm, rèn thường, đột phá và đường giảm thử nghiệm hiện có. Modifier được dựng từ instance đã rèn. Vật phẩm Common trong bảng loot production vẫn là Kiếm/Cung cấp 1 Attack +4, tỷ lệ 8%. Nguyên liệu vẫn 60%, lượng 1–2. Loot ghi vào hòm một lần khi quái chết hợp lệ và VFX chỉ báo nhận.
- Không thêm quy tắc bậc Huyền trở lên, roll, chi phí, phá đồ hay kỹ năng. Forge prototype có thể nâng món Common không có dữ liệu đặc tính lên Hoàng theo luật cũ. Trường hợp đó không tự nhận kỹ năng. Hỏa Cầu tiếp tục dùng đường damage legacy của 0.1.6.

### Cách chơi thử

1. Hạ quái, mở **Hòm đồ → So sánh**, chọn món loot và bấm **Trang bị**.
2. Dùng **Đồ mẫu DEV** trong Hòm đồ để thử Áo vải +12 Giáp và Phù Tụ Linh +25 Mana tối đa. Hai món mẫu lấy qua nút DEV và bảng loot production giữ nguyên.
3. Vào **Võ học → Nhận 3 vũ khí mẫu DEV**. Trang bị món Hoàng tương ứng, rồi tự thêm kỹ năng vào hàng. Dùng **Rèn** trên món được chọn để thử các thao tác hiện có.

### Save và kiểm thử

- Giữ version hiện có và cơ chế nạp candidate trước khi thay Hero. Trường SkillID bổ sung là tùy chọn. Load giữ ID, roll, slot, Forge, tài nguyên, hàng kỹ năng và cooldown. Trang bị tăng giới hạn giữ HP/Mana hiện tại, tháo đồ chỉ clamp phần vượt.
- Unity `6000.5.0f1`. Build Windows Development Mono hoàn tất với 0 lỗi, 57 cảnh báo. Kiểm tra cấu trúc project quét 975 tệp với 0 lỗi và 0 cảnh báo.
- MainGame Play Mode và Windows Player WARP đều PASS 5 nhóm smoke. Player thoát với mã 0. Kiểm tra dùng handler trên scene thật, profile riêng và fixture chạy trong bộ nhớ.
  1. Common loot giữ Attack +4, không có dòng phụ hoặc skill hoạt động. Quái chết cấp loot một lần, món vào Hòm đồ và trang bị được. Chỉ ép tỷ lệ trong fixture.
  2. Compare hiển thị chênh lệch. Forge thường đổi +4 thành +5 trên cùng instance, Forge đặc biệt không tự cấp skill. Các ảnh chụp bao gồm Compare, Forge và Compare kỹ năng.
  3. Ba vũ khí Hoàng cấp đúng skill, đổi vũ khí không làm MaxHP 1000 thay đổi. Áo vải giảm sát thương vật lý qua Giáp. Phù tăng MaxMana từ 100 lên 125, giữ Mana hiện tại và chỉ clamp khi tháo.
  4. Save/Load giữ ID instance, roll, cấp, trạng thái Forge, HP 617, Mana 67, queue và cooldown. Fixture tương thích Save cũ nạp được. Retry không cấp trùng loot.
  5. Thiên Quan hoàn tất qua Combat, chuyển sang Trúc Cơ và giữ Qi reserve cùng trang bị.
- Hòm đồ được mở từ preset 600×338 và 800×450 qua handler. Windows Firewall chặn phiên thử thao tác chuột ở bước mở Player. Chưa xác nhận chuột thật, hình ảnh và thao tác cuối để người chơi nghiệm thu. Không chạy full regression.
- Log MainGame: `D:\IdleGame\Logs\Unity-MainGame017-20261110-004927.log`.
- JSON và ảnh Player: `D:\IdleGame\Builds\Development017Equipment\profiles\idlegame-v10-574d3317f36545999fc0e3a41912455d\evidence`.

### Windows Development Player

`D:\IdleGame\Builds\Development017Equipment\build\IdleGame-WARP.cmd`

Tệp game tại `D:\IdleGame\Builds\Development017Equipment\build\IdleGameV10.exe`. Launcher dùng WARP, đường render đã qua smoke và thoát mã 0 trong batch này. Đường GPU mặc định từng gặp lỗi native lúc thoát ở hotfix trước và chưa được kiểm tra lại.

### Git

- `9ad850f29e9e270fc49602c87fe8f3d28b6283af` — metadata grant, nguồn modifier dùng chung và đồ mẫu.
- `2423f2d445599f438912e7eb27e7786b7a1d710e` — stat preview và Inventory Compare.
- `ec128443eef723bfdaeed2fdf7bd83a077b72142` — smoke MainGame và Windows Player. Đây là SHA của bản Player trong báo cáo.
- Báo cáo nằm trong commit tài liệu tiếp theo. Trạng thái Git khi chốt chỉ còn hai asset đã sửa và 15 tệp .meta chưa track từ trước, đối chiếu hash với lúc bắt đầu.
