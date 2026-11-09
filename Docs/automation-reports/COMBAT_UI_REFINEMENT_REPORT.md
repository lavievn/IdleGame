# Combat & UI Refinement Patch

Patch sau Development 0.1.6 trên nhánh `development_0.1.5`, bắt đầu từ `332ab50d3e0eb86a152666fbacd34a9d897f2533`.

## Thay đổi

- Hero và quái dùng cùng bước chọn kỹ năng, vận chiêu, kiểm tra điều kiện trước khi phát và hồi chiêu. Hero chỉ vận trong Combat. Hủy khi mất mục tiêu, nguồn kỹ năng hoặc vòng đời actor. Release mới trừ Mana, ghi cooldown và tiến con trỏ. Thời gian vận chưa xong không lưu vào Save.
- Tia sáng hội tụ theo tiến độ vận chiêu. Đâm kiếm dùng cùng gốc, hướng và vùng 240×84 với damage. Staff đánh thường bằng đốm lửa cong, tăng tốc dần, khóa mục tiêu và chỉ gây damage phép lên mục tiêu đó. Thời gian bay mặc định 0,55 giây. Kích thước, tốc độ, gia tốc và trail chỉnh được trên CombatManager.
- Quái chưa giao chiến đi lang thang ở 50 đơn vị/giây, đổi hướng ở biên rồi dùng tốc độ truy đuổi 100 khi vào Combat. Danh sách skill trên prefab mặc định rỗng. Fixture opt-in dùng Hỏa Cầu và Mana riêng.
- SFX dùng chung profile và fallback, theo âm lượng SFX, có bốn voice dành cho Hero và ba voice cho quái. BGM dùng hai kênh tái sử dụng, chuyển trạng thái trong 1,25 giây, và giữ bài đã chọn khi Pause/Resume ở Thiên Quan.
- HUD có ô Kiếm/Cung/Trượng dưới Mana, nền xanh đậm dâng từ dưới và có màu riêng khi đang vận chiêu. Bấm tên map bên trái để mở World Map. Cuộn để zoom theo con trỏ, giữ chuột trái để kéo, bấm ngắn để chọn. Chỉ vùng chọn sáng và chỉ hiện Travel khi đủ điều kiện.

## Xác minh

- Unity 6000.5.0f1 biên dịch thành công. MainGame Play Mode và Windows x64 Development Player chạy qua cùng bộ smoke trên profile riêng. Build Mono, 0 lỗi, 57 cảnh báo.
- Smoke xác nhận Hỏa Cầu đủ thời gian mới phát, hủy khi mất mục tiêu, Mana/cooldown ghi đúng một lần, quái dùng skill thật và hủy khi chết, SFX qua kênh riêng, tốc độ 50/100 và biên BattleArea. Đã kiểm tra Đâm kiếm hai hướng, Staff một mục tiêu và thanh hồi chiêu từ 0 đến 100% tại 600×338, 800×450, 1150×647, 1360×680.
- World Map qua kiểm tra chọn/glow, Travel có điều kiện, zoom và pan không tự chọn hoặc Travel. BGM về đúng bài với hai AudioSource tái sử dụng. Thiên Quan chạy tới kết quả Trúc Cơ và giữ Qi dự trữ. Kiểm tra tương thích 0.1.6 trên cùng Windows Player qua Bow ba mũi, đổi vũ khí giữ cooldown, Staff Common, Hỏa Cầu AoE, hồi tài nguyên, Save/Load, Retry và Thiên Quan.
- Các kết quả trên là method-driven. Phiên thử chuột native qua computer-use gặp hộp thoại Windows Firewall trước khi thao tác. Chưa xác nhận chuột/phím native hoặc nghe BGM/SFX bằng tai. Không chạy full regression/benchmark. Hệ thống Save cá nhân được giữ nguyên.
- Bằng chứng Editor ở `D:\IdleGame\Project\.utmp\foundation-67f60f8302b740638428c1de2f217109\evidence`. Kết quả Windows lần lượt ở `profiles\idlegame-v10-4b0f927f3ea542e2a996a2a6bb0c1582\evidence` và `profiles\idlegame-v10-74f825a1b512441aa332868f98723ef0\evidence` dưới thư mục build.

## Player và nghiệm thu trực tiếp

`D:\IdleGame\Builds\CombatUiRefinement\build\IdleGameV10.exe`

- [ ] Trong **Võ học**, thêm kỹ năng đang được trang bị cấp vào hàng đợi. Dùng Trượng thử nghiệm để xem vận 1 giây, phóng Hỏa Cầu, trừ 5 Mana và bắt đầu hồi chiêu 5 giây. Tạm dừng ở giữa để kiểm tra tiến độ đứng yên.
- [ ] Đổi Kiếm và thử Đâm kiếm hai hướng. Tháo Trượng hoặc bỏ Hỏa Cầu khỏi queue để xem trạng thái không khả dụng. Dùng Staff đánh thường để quan sát đốm lửa bay cong.
- [ ] Bấm tên map, chọn vùng, cuộn và kéo. Chọn một vùng đủ điều kiện rồi bấm **Tới ô đã chọn**. Nghe chuyển nhạc ở màn hình chính, khi Travel và khi vào/ra Thiên Quan.

## Nguồn và Git

- Combat, VFX, quái và SFX trong `CombatManager.cs`, `CombatManager.WeaponSkills.cs`, `BattleEffects.cs`, `MonsterController.cs` và `HeroCombatSfx.cs`.
- BGM trong `MapBgmController.cs`. HUD và bản đồ trong `HudW3View.cs`, `UIManager.cs`, `WorldMapPanelUI.cs`, `WorldMapPolygonGraphics.cs` và `TransparentWindow.cs`.
- Smoke trong `CombatUiRefinementSmoke.cs`, gọi qua `Tools/Local-IdleGame.ps1`.

- `c4d266d435aa01e6be67f41e899a70b6e902626f` — scheduler chung, VFX, SFX và di chuyển quái.
- `55702513814a0780f8e32833f806b82b7001d2ac` — crossfade BGM.
- `40a23f37ae5bc906f7607662f0f7aec746d1a2d2` — HUD và World Map.
- `2b75a16ec951aebb4e3b29eed29a8910cd900752` — smoke MainGame/Windows.
- `5b017ffa0c2864248b8b5d3e19296aa410488b4a` — nhãn HUD và tia vận chiêu rõ hơn. Đây là SHA của code trong bản build. Commit kế tiếp chỉ thêm báo cáo này.
- Git status sau patch còn hai asset đã sửa sẵn (Windows Build Profile và TMP Fallback) cùng 15 file `.meta` chưa track của Lake, Sea, Steppe. Đã đối chiếu nguyên trạng từng file. Nhánh hiện hành đi trước `origin/development_0.1.5` 10 commit, gồm bốn commit đã có trước patch.
