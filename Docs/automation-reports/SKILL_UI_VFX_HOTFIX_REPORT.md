# Skill UI & VFX Alignment Hotfix

Hotfix từ HEAD `a4e3b54e9419d4ac7066c576599fe8a6a1cabdbe` trên `development_0.1.5`.

## Căn chỉnh VFX

- Kiếm và quái dùng mốc tích lực hiện có. Cung và Trượng cộng riêng 20 px theo phép chiếu Battle Canvas, bằng hai trường Inspector độc lập.
- Đạn Cung và Trượng dùng pivot sprite và kích thước thân hiển thị. Mốc mặc định của Cung tiến 24% bề rộng thân và cao 48% chiều cao. Trượng tiến 18% và cao 51%. Hướng tiến đối xứng theo hướng bắn. Flash Liên Hoàn Tiễn xuất phát tại đúng mốc của mũi tên đầu.
- Mốc được chụp tại Release. Target endpoint, tốc độ, số mũi, hệ số đường cong, damage và lịch thi triển dùng luồng cũ.

## Võ học

- Bảng trên có năm ô mỗi trang, số thứ tự và nút Lên, Xuống, Bỏ. Chọn ô trống, chọn kỹ năng ở bảng dưới rồi bấm **Thêm vào hàng**. Các nút Trước và Sau xuất hiện khi cần.
- Kho luôn hiện Đâm kiếm, Liên Hoàn Tiễn và Hỏa Cầu Thuật. Kỹ năng thiếu nguồn có icon và nền xám nhưng vẫn xem được. Dòng đỏ phân biệt yêu cầu vũ khí/nguồn với Mana tại lúc thi triển.
- Tên, mô tả, công thức trước biến thiên và phòng ngự, Mana, vận chiêu và hồi chiêu lấy từ registry. Queue tiếp tục dùng Save hiện có. Việc xem kho không học hoặc cấp kỹ năng.

## Windows Player và kiểm thử

- Unity 6000.5.0f1 biên dịch thành công. MainGame Play Mode và Windows x64 Development Player qua các assertion gameplay. Build có 0 lỗi và 57 cảnh báo.
- Kiểm tra bảng Võ học ở 600×338, 800×450, 1150×647 và 1360×680. Đã kiểm tra màu xám, mô tả và cảnh báo, chọn ô trống, thêm/bỏ/sắp xếp, trang thứ hai, ID chưa nhận diện và giữ hàng đợi qua Save/Load. Đã đo tích lực ở 600/800/1150, kiểm tra điểm phát đạn trái/phải và chụp đúng frame tạo projectile. Liên Hoàn Tiễn vẫn có ba mũi. Thiên Quan thắng và giữ Qi cùng thứ tự kỹ năng.
- **StartMenuUI — FIXED.** Bản đối chứng tái hiện `GameManager.OnDisable → StartMenuUI.EnterGameplayImmediately → LockButtonsForEntry → settingsPage.SetActive` khi thoát trong lúc vào màn chơi sau Thiên Quan. Trang cài đặt của menu đã bị hủy trong teardown. `OnApplicationQuit` đánh dấu trước nhánh lưu hiện có để `OnDisable` bỏ qua khôi phục menu khi tiến trình đang thoát. Hai lượt Player mới không còn stack managed này.
- **Thoát native — vẫn tái hiện.** Lượt gọi `UIManager.ConfirmExit` và lượt mở lại AutoSave đều đã ghi PASS gameplay trước khi tiến trình trả `-1073741819`. Windows Event Log ghi `0xc0000005` trong `UnityPlayer.dll`, RVA `0x1dd029`. PDB Unity cài sẵn ánh xạ địa chỉ sang `ExternalGPUProfiler::GetGameViewWindowHandle+0x9`. Lượt đối chiếu render đơn luồng vẫn tái hiện, chưa đủ bằng chứng để chọn bản sửa native.
- AutoSave đọc được trong tiến trình riêng, giữ Trúc Cơ, Qi dự trữ và hàng đợi. Kiểm thử thao tác qua handler trên profile có marker, chưa dùng chuột/phím native. Chỉ chạy smoke theo phạm vi.
- Ảnh và JSON kết quả Windows nằm tại `profiles\idlegame-v10-5f2ec3ba77714106b8a522b3f44af722\evidence` dưới thư mục build. Lượt mở lại nằm tại `profiles\idlegame-v10-e63ec8bf9fc948dc82221628a05d58d7\evidence`. Log đối chứng cũ tại `D:\IdleGame\Builds\CombatVisualHpHotfix\profiles\idlegame-v10-ed7b53b83cfc4a748e493e01bfb71df3\player.log`.

**Windows Development Player**

`D:\IdleGame\Builds\SkillUiVfxHotfix\build\IdleGameV10.exe`

## Nghiệm thu trực tiếp

- [ ] Mở Võ học ở cửa sổ nhỏ, bấm ô trống và kỹ năng xám, sau đó thêm/bỏ/sắp xếp khi trang bị đúng nguồn.
- [ ] Quan sát điểm tụ sáng khi đổi Kiếm, Cung và Trượng. Xem vị trí mũi tên và đốm lửa ở khoảnh khắc vừa xuất phát.
- [ ] Thoát game qua Menu rồi mở lại bản lưu.

## File và Git

- Anchor và projectile trong `CombatManager.cs`, `CombatManager.WeaponSkills.cs`.
- Bảng, icon vẽ trực tiếp và metadata trong `UIManager.WeaponSkills.cs`, `WeaponSkillRegistry.cs`.
- Shutdown trong `GameManager.cs`. Smoke trong `SkillUiVfxHotfixSmoke.cs` và launcher hiện có.

- `c6fb373e3287e884e22fdb2a46b0b2baca250131` — bỏ khôi phục menu khi ứng dụng đang thoát.
- `91e17a4e1fbc00c29f03e1559429d2a9bb39e71b` — anchor tích lực và phát đạn.
- `acfb7b946bba2ba0d8273784fffd58fe6d637652` — bảng Võ học và metadata.
- `881147651f763596180bb143e6bad56b1025b6ca` — smoke và launcher. Đây là SHA dùng build Windows. Commit kế tiếp chỉ thêm báo cáo.
- Sau commit báo cáo, nhánh đi trước `origin/development_0.1.5` 11 commit, gồm 6 commit đã có từ hotfix trước. Working tree chỉ còn hai asset đã sửa sẵn và 15 file `.meta` chưa track. Từng file khớp hash trước hotfix.
