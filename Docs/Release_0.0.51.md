# 0.0.51 — Nền thuộc tính, đặc tính và phòng ngự

**Nền:** main 0.0.50 (commit `4ed0020`). **Nhánh kiểm thử:** `feature/0.0.51-combat-stats`. Chưa gộp main hoặc phát hành chính thức.

## Đã triển khai

- `CombatStatId` và `CombatStatModifier`: lưu nguồn (`sourceId`), thuộc tính, cộng thẳng (`flat`), cộng theo tỷ lệ (`percent`), nguyên tố, điều kiện thẻ `requiredTags` / `blockedTags`.
- `EntityDataSO.characterTags`, `combatStatModifiers`, `combatStatsVersion`: có lưu Unity JSON cùng Hero chính; đọc save 0.0.50 sẽ khởi tạo các danh sách mới rỗng, không sinh ngẫu nhiên lại khi tải. API `AddCharacterTag`, `RemoveCharacterTag`, `AddCombatModifier`, `RemoveModifiersBySource` đánh dấu dữ liệu cần lưu.
- HP và ATK đọc qua cùng bộ thuộc tính; không sửa ngược `baseHealth`/`baseDamage` hoặc điểm thuộc tính. HP theo thế Cận/Cung/Phép vẫn giữ công thức cũ; cộng đồ áp dụng sau hệ số của HP nền. `CombatManager.RefreshRegisteredHeroStats(controller)` làm mới HP theo cùng tỷ lệ, không hồi sinh nhân vật đã chết.
- Tốc chạy, tốc đánh và hồi HP tự động của từng Hero dùng bộ thuộc tính; hồi HP tích lũy phần thập phân, có thể kiểm chứng ở nhịp khung hình thấp. Hồi máu sau khi hạ quái hỗ trợ bonus có điều kiện.
- `CombatDamage`: một chiêu có nhiều `CombatDamagePart`, mỗi phần riêng `DamageSchool.Physical/Magical` và `ElementType`. Giáp chỉ tác động sát thương vật lý, kháng phép chỉ tác động phép, kháng hệ chỉ tác động phần mang đúng hệ. Tổng cộng sau khi kháng riêng từng phần rồi làm tròn một lần.
- Giáp/kháng phép sử dụng hiệu quả giảm dần, và giới hạn chống miễn nhiễm; giá trị âm gây yếu kháng thực sự nhưng có giới hạn. `CombatManager` dùng công thức mới cho các đòn cận và đạn bay hai chiều.
- **Tương thích 0.0.50:** đòn thường vẫn đi qua `SynergyMath` tính các hệ số linh căn cũ, và `CombatDamage.ResolveLegacy` chỉ thêm tầng phòng ngự; không có modifier thì không thêm tính toán/đối tượng tạm ở mỗi hit. Đòn thường hiện tại phân loại Cận/Cung = vật lý Vô, Phép = phép Vô **ở tầng giáp/kháng mới**. Hiệu ứng linh căn 0.0.50 vẫn được giữ nguyên ở tầng trước. Chưa thay toàn bộ đòn thường thành skill mang nguyên tố riêng.
- Hiển thị Giáp, Kháng phép, Hồi HP và ATK/tốc chạy tính lại ở bảng thông số Hero.

## Chỉnh trong Unity Inspector

**Công thức kháng:** `Hierarchy → GameController → Component CombatManager → 0.0.51 - GIAP / KHANG PHEP / KHANG HE → Resistance Settings`:

| Trường | Mặc định | Ý nghĩa |
|---|---:|---|
| `armorHalfReduction` | 100 | 100 giáp cho 50% giảm vật lý nếu chưa đụng giới hạn |
| `magicResistanceHalfReduction` | 100 | 100 kháng phép cho 50% giảm phép |
| `maximumSchoolReduction` | 0.85 | Giảm tối đa 85% cho từng loại |
| `maximumSchoolVulnerability` | 0.50 | Kháng âm có thể làm nhận thêm tối đa 50% |
| `maximumElementReduction` | 0.75 | Kháng từng nguyên tố tối đa 75% |
| `minimumElementReduction` | -0.50 | Kháng hệ âm làm tăng sát thương nhận tối đa 50% |

**Đặc tính nhân vật:** `Hierarchy → GameController → Component GameManager → Hero Data SO (nhấp vào asset được gán) → 0.0.51 - THUOC TINH / DAC TINH`. Chỉnh `Character Tags` và `Combat Stat Modifiers`. Đây là thiết lập trên asset mẫu; hãy kiểm thử trên **New Game** để game tạo bản dữ liệu runtime và không ghi đè save cũ.

Ví dụ modifier mẫu để thử:

- `stat=Armor; flat=100`: giảm 50% phần vật lý.
- `stat=MagicResistance; flat=-100`: nhận thêm 50% phần phép (do giới hạn yếu kháng).
- `stat=ElementResistance; element=Hoa; flat=.25`: giảm 25% phần mang Hỏa.
- `stat=ElementDamagePercent; element=Hoa; percent=.20`: tăng 20% phần sát thương Hỏa.
- `stat=MovementSpeed; flat=50; requiredTags=["terrain:forest"]` + `characterTags=["terrain:forest"]`: tăng tốc chạy khi đủ thẻ điều kiện.

Trường `percent` trên các thuộc tính chỉ số thường = tỷ lệ nhân với **giá trị gốc** (ví dụ `MovementSpeed percent=.1` tăng 10% tốc gốc). Trên các thuộc tính đã có chữ `Percent` và `ElementResistance`, `flat` / `percent` đều biểu diễn **giá trị tỷ lệ trực tiếp** (`.20` là 20%). `Armor` và `MagicResistance` là điểm phòng ngự, không phải tỷ lệ giảm cố định.

**Dữ liệu đang chơi:** nếu thay danh sách modifier bằng mã trong lúc Hero đang sống, hãy gọi `CombatManager.RefreshRegisteredHeroStats(hero)` để cập nhật Max HP mà không đổi sai tỷ lệ. UI kho đồ sau này sẽ tự gọi hàm này. Chỉnh Inspector trên asset trong Play Mode chưa có cơ chế tự động chuyển vào bản sao Hero đang chơi.

## Đã khai báo nhưng chưa đưa vào gameplay

- `CastSpeed`, `CooldownReductionPercent`, `ManaRegeneration`: cấu trúc thuộc tính có sẵn; chưa có kỹ năng thực hay thanh linh lực để áp dụng.
- `ElementDamagePercent` dùng được trong `CombatDamage.Resolve` cho chiêu nhiều phần, nhưng **chưa có hệ tạo/thi triển kỹ năng mới**. Hệ linh căn hiện tại không thay thế bằng mô hình kỹ năng mới.
- Tag địa hình/vũ khí chưa có hệ thống tự cấp/tháo từ trang bị hoặc map. Có thể thử bằng cách cấp `characterTags` hoặc context API.
- Chưa có kho đồ, vũ khí ngẫu nhiên, xuất thân ngẫu nhiên, kỳ ngộ, hồi phục theo thời gian offline, lưu đồng đội B2.

## Kiểm thử hồi quy và giới hạn

- `Tests~/Version0051StatsRegression.cs` kiểm tra nhiều phần sát thương, yếu kháng và giới hạn, lọc tag, tỷ lệ Max HP khi thêm/tháo đặc tính, hồi HP theo thời gian, tốc chạy có điều kiện và chiến đấu thực tế với quái.
- `Tests~/run.sh` đã bao gồm `CombatStats.cs` và `CombatDamage.cs`.
- **Chưa chạy Unity Editor, Windows build hay Mono test trên môi trường cập nhật GitHub**. Thư viện mô phỏng `Tests~/UnityStub.cs` không xác minh được quá trình Unity JSON ghi danh sách modifier phức tạp; cần test save/load trên Unity thật trước khi gộp main.

## Checklist kiểm thử Unity

1. Console không có lỗi sau khi Pull và mở Scene.
2. New Game không có modifier: sát thương và chuyển động vẫn giống 0.0.50.
3. Chọn asset dữ liệu Hero rồi thử Armor/MagicResistance/Hoa Resistance; quái cận/Phép tấn công và ghi log đúng.
4. Thử modifier MovementSpeed +50, Attack +5, MaxHealth +100, HealthRegeneration +3 rồi xác nhận UI và chiến đấu.
5. Thử cả hai Hero B2 cùng đánh, Hero tử vong ẩn ngay và camera vẫn hoạt động.
6. Sao lưu save 0.0.50; Load thử để chắc chắn cấp, linh căn, map không đổi; sau đó lưu và nạp lại các modifier trên Unity.

## Bổ sung menu lưu thủ công (bản sửa sau kiểm thử)

**Nguyên nhân:** `SaveMenuUI` cũ vẫn chứa nút Save1/Save2 nhưng đã bị ẩn khi `UIManager.ConfigureSystemMenu` dựng trang menu mới. `GameManager.ForceManualSave` vẫn tồn tại, nhưng không có đường bấm từ menu đang hiển thị.

**Đã sửa trên cùng nhánh:**

- `Menu → Lưu thủ công → Lưu vào ô 1 / Lưu vào ô 2`, kèm `Quay lại`.
- Nếu ô đang có dữ liệu, xuất hiện trang **Xác nhận ghi đè / Hủy**. Chỉ ghi sau khi đồng ý.
- Lưu thành công: đóng Menu, ghi vào nhật ký sự kiện. Lỗi ghi tệp: giữ menu và hiện `LƯU THẤT BẠI`.
- Lưu tay dùng trực tiếp `GameManager.TryManualSave` → `SaveManager.SaveGame`; chỉ cho phép hai ô lưu tay khi Hero đang chạy, không ghi nhầm ô autosave.
- Menu chính có **5** mục; hệ thống tự chia hàng trong khung menu, không che nút.
- `Tests~/ManualSaveMenuRegression.cs`: kiểm thử lần lưu đầu, hủy ghi đè, xác nhận ghi đè và không tác động ô còn lại.

**Kiểm thử Unity trực tiếp cần làm:** trong màn chơi chọn Menu → Lưu thủ công → Ô 1; đổi thuộc tính rồi chọn lại Ô 1 để xác nhận ghi đè; sau đó vào Tải bản lưu → Save 1 và kiểm tra dữ liệu. Trước lần đầu nên sao lưu thư mục `Saves` để giữ ô lưu hiện tại. Chưa xác minh qua Unity Editor thật.

## Sửa lưu/khôi phục dạng chiến đấu Hero

**Nguyên nhân:** ba nút dạng đánh tác động `HeroController.attackMode`, nhưng save chỉ chứa `EntityDataSO`. Trước bản sửa, lưu lúc sử dụng Cung rồi tải lại sẽ giữ Modifier `stance:bow` nhưng chế độ đánh trở về dạng tồn tại trên Controller (thường là Cận).

**Bản sửa:**

- Trường `EntityDataSO.savedAttackMode` được Unity JSON lưu cùng tiến trình Hero chính. `GameManager.SetHeroAttackMode` đồng bộ ngay vào dữ liệu save và đánh dấu `isDirty` khi đổi thế.
- Lưu thủ công (hai ô), autosave định kỳ, autosave khi thua, autosave sau bắt đầu mới và autosave lúc thoát ứng dụng đều ghi chế độ đang dùng.
- Tải từ menu trong game (`TryLoadSlot`) và tiếp tục từ menu khởi động (`OnConfirmResetClicked`) đều gọi `RestoreSelectedStance` **trước khi** `CombatManager.SetupHeroInfo` tính HP. Khi tải Cung/Phép, nút đang chọn, Max HP và những Modifier `stance:bow`/`stance:magic` được khôi phục theo cùng chế độ.
- Save cũ 0.0.50/0.0.51 chưa có `savedAttackMode` sẽ mặc định **Cận chiến**, không kế thừa giá trị trong asset mẫu. Enum ngoài 0/1/2 bị coi là không hợp lệ và trở về Cận.
- **Không** ngẫu nhiên dạng đánh khi tạo nhân vật; ba nút thử nghiệm vẫn dùng được. Đồng đội B2 tiếp tục có dạng chiến đấu tách biệt; bản lưu vẫn chỉ lưu Hero chính.
- Đã thêm `Tests~/AttackModeSaveRegression.cs` với các trường hợp: đổi thế đánh dấu save cần ghi, lưu/tải từ menu, tiếp tục từ màn hình đầu và tương thích save cũ/hỏng.

**Kiểm thử Unity:** Chọn Cung, thêm `test_bow` +20 ATK, lưu Ô 1; thoát Play Mode, xóa `test_bow` trong asset mẫu, Play → Tải Ô 1. Hero phải tự vào Cung và thấy ATK cộng thêm ngay, không cần bấm chọn Cung. Thử lại với Phép và lưu tự động. Kiểm thử chạy trong Unity Editor trên máy người phát triển vẫn cần xác nhận.

## Sửa vị trí chữ sát thương Hero (kiểm thử từ các bản trước)

**Nguyên nhân từ mã/Scene:** `Hero_DMG_Text` neo vào `Hero` nhưng TextMeshPro của Scene đặt **căn trái**. Khung chữ rộng 130 nên glyph lệch khỏi thân. Ngoài ra `HeroController.FadeDamageTextRoutine` trước đó gọi `DamagePopupMotion.X`, đẩy chữ sang ngang tới 48 đơn vị; nhìn như chữ phát sinh từ phía trước Hero.

**Sửa trong `HeroController.cs`:**

- `AlignDamageTextToBody`: gắn `Hero_DMG_Text` vào đúng `heroRect`, `anchor/pivot=(0.5,0.5)`, `anchoredPosition=(0,0)`, `TextAlignmentOptions.Center`; chuyển chữ lên trước trong thứ tự vẽ bằng `SetAsLastSibling`.
- `ShowDamage` vẫn hiển thị lượng sát thương và đặt lại vị trí gốc khi có hit mới.
- `FadeDamageTextRoutine`: chữ sát thương Hero **bay lên thẳng đứng** từ giữa thân; không dùng `DamagePopupMotion.X`. Quái vật không bị thay đổi cách hiển thị.
- Không thay nguồn gây sát thương, độ lớn, logic HP, camera hoặc ảnh hưởng đến mỗi Hero/đồng đội khác.

**Bài test bổ sung:** `Tests~/HeroDamagePopupRegression.cs` kiểm tra neo và căn chữ ở giữa thân, không trôi ngang khi phát sinh sát thương, theo Hero khi di chuyển với tỷ lệ Canvas khác nhau, ẩn chữ khi chết/ẩn Hero và chữ của đồng đội không ảnh hưởng Hero chính.

**Kiểm thử trực tiếp:** Pull cùng nhánh, mở Play Mode, để quái đánh Hero ở cự ly gần và từ xa; quan sát chữ **xuất hiện giữa thân rồi đi thẳng lên**, ở cả Cận chiến/Cung/Phép. Sau đó thử gọi đồng đội B2.
