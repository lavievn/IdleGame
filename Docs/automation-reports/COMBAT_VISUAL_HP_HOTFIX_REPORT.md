# Combat Visual & HP Hotfix

Hotfix sau Combat & UI Refinement trên nhánh `development_0.1.5`, từ HEAD `ea6dd1a0c315f812e5fb722b1bfd46ed85c1e0d8`.

## HP và hồi phục

- MaxHP cũ thay đổi theo tư thế đòn đánh. Cận chiến nhân base HP ×2, Cung ×0,6 và Trượng ×1. Phần HP đã tích lũy theo cấp chỉ được cộng ở cận chiến. Vũ khí mẫu dùng dòng tấn công, nên bỏ hệ số tư thế ở công thức thay vì lọc bỏ HP hợp lệ của trang bị.
- Hero mới dùng base 1.000 HP. Các loại vũ khí dùng chung phần tăng theo cấp, điểm HP đã phân bổ và modifier MaxHP hợp lệ. Preview trang bị dùng cùng công thức. Tính lại MaxHP giữ lượng HP hiện tại và hạ xuống nếu vượt cap.
- Save cũ giữ base lúc tạo và các trường đã lưu. Nếu HP lưu vượt cap mới, runtime hạ HP về cap và ghi lý do vào Nhật ký. Tải không ghi đè slot nguồn. Retry tiếp tục hồi sinh bằng MaxHP theo cơ chế hiện có.
- Hồi thụ động 10 HP/giây trong khám phá, nhận diện và tiếp cận. Giao chiến bắt đầu khi Hero thực hiện đòn hoặc quái bắt đầu đòn nhắm vào Hero. Hồi phục khóa khi đang đánh, đang có đạn hoạt động, hoặc đối thủ đã giao chiến còn trong tầm đánh. Khoảng đệm 0,5 giây tránh bật/tắt khi tạm ra khỏi tầm. Wave kết thúc xóa trạng thái giao chiến. Pause, chết, các khóa lifecycle và Thiên Quan chặn hồi thụ động.

## VFX

- Tích lực của Hero và quái dịch trái 4 px và lên 10 px theo scale của Stable Battle Canvas.
- Đâm kiếm chuyển từ chân lên thân. Visual và kiểm tra damage cùng dùng mốc thân hai phía, cùng hướng và vùng 240×84 BattleArea. Khoảng cách tương đối giữa các làn, multiplier và cooldown giữ theo dữ liệu hiện có.
- Liên Hoàn Tiễn lóe trắng xanh về phía bắn một lần khi Release, giữ nguyên ba mũi tên và đường đạn.
- Mẫu impact của Cung dùng cho các hit được chấp nhận, cao hơn 20 px so với mốc cũ trên mục tiêu. AoE phát một lần cho mỗi mục tiêu trúng. Đạn hụt hoặc bị hủy kết thúc đường bay mà không phát hit.

## Kiểm thử

- Unity 6000.5.0f1 biên dịch thành công. MainGame Play Mode qua 6 nhóm kiểm tra. Windows x64 Development Player build thành công với 0 lỗi, 57 cảnh báo. Các assertion gameplay của hotfix và smoke tương thích 0.1.6 đều qua trên Player.
- Đã xác nhận MaxHP 1.000 mặc định, swap bốn loại, level và đồ HP, giữ HP hiện tại, Save/Load/Retry, hồi HP khi tiếp cận, dừng khi bắt đầu tấn công và không hồi trong Thiên Quan. Thiên Quan kết thúc ở Trúc Cơ, giữ Qi dự trữ.
- Đo offset tại 600×338 và 1150×647, xác nhận Đâm kiếm hai hướng, flash Cung một lần, ba mũi, impact dùng chung theo từng hit và không phát khi hụt/hủy.
- Chạy method-driven và xem ảnh chụp thật từ game. Chưa kiểm thử bằng chuột/phím native. Chỉ dùng profile Save thử nghiệm. Không chạy benchmark hoặc full regression.
- Lượt Windows hotfix cuối và lượt tương thích đều ghi ngoại lệ ở `StartMenuUI.LockButtonsForEntry` trong `GameManager.OnDisable` sau khi các assertion đã qua. Lượt tương thích trả mã thoát `-1073741819`. Lỗi đóng ứng dụng nằm ngoài phạm vi hotfix.
- Evidence Editor tại `D:\IdleGame\Project\.utmp\foundation-3b30f41ee6724227b1f06b0eaef42d60\evidence`. Evidence Windows nằm dưới thư mục build tại `profiles\idlegame-v10-02b5b62b063245249cbdd97d608a97ca\evidence` và `profiles\idlegame-v10-4d7eee9b538a401187ff2b5427fd0d48\evidence`.

**Windows Development Player**

`D:\IdleGame\Builds\CombatVisualHpHotfix\build\IdleGameV10.exe`

## Nghiệm thu trực tiếp

- [ ] Tạo Hero mới, đổi Kiếm → Cung → Trượng → Tay không, rồi thử tháo/lắp trang bị có HP khi đang mất máu.
- [ ] Quan sát HP khi Hero đi tới quái ở xa, khi vận chiêu, khi quái nhắm chiêu và trong Thiên Quan.
- [ ] Nhìn tia tích lực, độ cao Đâm kiếm hai hướng, flash đầu chuỗi Cung và điểm lóe khi đòn thực sự trúng ở kích thước cửa sổ thường dùng.

## File và Git

- Stat và gear preview trong `EntityDataSO.cs`, `Hero_Main.asset`, `CombatBalance.cs`, `EquipmentStatPreview.cs`, `GameManager.cs`.
- Combat/VFX trong `HeroCombatState.cs`, `CombatManager.cs`, `CombatManager.WeaponSkills.cs`, `BattleEffects.cs`, `EnvironmentManager.cs`.
- Smoke trong `CombatVisualHpHotfixSmoke.cs`, gọi qua `Tools/Local-IdleGame.ps1`.

- `94cd79ccb9c580b3afdc65a239e50261ae6bf4cc` — MaxHP độc lập với vũ khí, nguồn base và gear preview.
- `36e5881cb9e827105e865249646efb6daac056e3` — hồi HP theo giao chiến và VFX.
- `3923a132758daa9a97a74f23da0e1a18783bb995` — smoke test cô lập.
- `b977bc2c8b1c8890a9bd60207b44cae5b3669611` — thông báo clamp HP khi nạp Save cũ.
- `50668cd1e6b623ae1c3abb20fc0926750771d24e` — fixture tương thích Đâm kiếm ổn định. Đây là HEAD dùng build Windows. Commit kế tiếp chỉ thêm báo cáo.
- Sau commit báo cáo, nhánh đi trước `origin/development_0.1.5` 6 commit. Git status chỉ còn hai asset đã sửa sẵn và 15 file `.meta` chưa track của Lake, Sea, Steppe. Hash các file này khớp baseline.
