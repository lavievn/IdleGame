# B2 — Định tuyến chiến đấu theo từng Hero

**Nhánh:** `feature/b2-combat-target-routing`, tách từ **B1 đã được chấp nhận** tại `7487646d355bad1f79e810eb76968a02555acb47`. **Trạng thái:** đã có mã và bộ kiểm thử mới; chờ Unity kiểm thử trước khi chốt. `main`, B1 và `.54g` không sửa.

**Nhánh nghiên cứu tiếp theo (tách riêng, chưa nghiệm thu):** `feature/b2-skill-foundation` bổ sung hợp đồng nền tảng kỹ năng, linh lực, bảo hộ tuyệt kỹ và điều kiện khựng thật từ kỹ năng/vũ khí/nội tại. Xem [B2_SkillCombatFoundation.md](B2_SkillCombatFoundation.md). **B2 gốc không bị thay đổi**.

## Mục tiêu và phạm vi

B1 đã tách trạng thái một Hero ra thành `HeroCombatState` nhưng vẫn sử dụng một Hero trong toàn bộ `CombatManager`. B2 nối ba khâu **tác nhân gây sát thương → mục tiêu chọn → HP/nhịp đánh của đối tượng nhận** theo từng Hero. Cho phép **hai Hero được đăng ký bằng API runtime** cùng đánh, nhận sát thương và chết riêng; chưa thiết kế hệ đội hình, UI tuyển đồng đội, tự spawn hay lưu nhiều nhân vật.

### API và trạng thái

- `CombatManager.RegisterAdditionalHero(HeroController,EntityDataSO)`: gọi sau khi actor đã được `SpawnHero()`; tạo/bắt đầu `HeroCombatState` độc lập. Hai lần đăng ký lại cùng Hero tương ứng lượt sống mới; không tự sinh bản sao `HeroController`/Scene.
- `CombatManager.StateFor(HeroController)`, `RegisteredHeroCount`, `HasLivingHero`, `ActiveHeroMovementSpeed`: tra cứu trạng thái, số người tham chiến, điều kiện trận còn Hero sống và vận tốc Hero sống ưu tiên. `CurrentHeroCombatState`, `CurrentHeroHP`, `MaxHeroHP` vẫn đại diện **Hero chính duy nhất** để không phá GameManager/UI hiện tại.
- `UnregisterAdditionalHero(HeroController)`: gỡ trạng thái và ẩn actor khỏi danh sách Hero hoạt động, để Monster AI/camera không khóa vào mục tiêu không còn chiến đấu. `SetupHeroInfo` cho game mới/Retry dọn đồng đội thử nghiệm trước khi bắt đầu life mới.
- `OnRegisteredHeroModeChanged(HeroController)`: tính lại MaxHP/tỷ lệ HP cho đúng Hero và reset nhịp đánh của Hero đó; `OnHeroAttackModeChanged()` giữ nguyên cho Hero chính.
- `HeroCombatState.LifeVersion` tăng khi `BeginLife`; `HeroController.SpawnVersion` tăng khi `SpawnHero`; `MonsterController.SpawnVersion` tăng khi `OnEnable` (hỗ trợ object pooling). Đạn chụp phiên bản khi bắn và không gây tác dụng lên đối tượng tái sinh cùng GameObject.

### Luồng chiến đấu

- `CombatManager` giữ sổ đăng ký `Dictionary<HeroController,HeroCombatState>`. Trong mỗi `Update`, tất cả Hero sống chạy `TickRegisteredHeroAttack` với **AttackTimer, Data, Controller và mode riêng**. Mỗi Hero tự tìm quái hợp lệ gần nhất trong tầm dựa vào tọa độ của chính mình.
- Quái ưu tiên `MonsterController.CurrentTarget` do AI đã chọn. Nếu Hero đó không đăng ký/chết/ngoài tầm, tìm Hero đã đăng ký gần nhất **trong tầm đánh**; không mặc định chuyển sát thương sang Hero chính.
- Đạn mang nguồn Hero hoặc đích Hero (tùy chiều), snapshot sát thương, vị trí đích tại thời điểm bắn và phiên bản sinh mệnh; đạn cung có thể hụt nếu đối tượng rời điểm trúng, phép của Hero giữ vùng nổ tại tọa độ đã nhắm khi quái gốc chết (như .54g). Đạn của đối tượng chết, không đăng ký hoặc đã tái sinh bị hủy.
- Khi Hero hạ quái, **EXP và hồi máu cận chiến thuộc về Hero gây đòn kết liễu**. Quy tắc chia EXP chung cho tổ đội chưa được chốt; hiện không chia EXP tự động cho Hero chính khi đồng đội hạ quái. Mốc UI/save của GameManager vẫn dùng dữ liệu Hero chính.
- Một Hero chết chỉ bị loại khỏi nhóm hoạt động; **Game Over và xóa quái khi không còn Hero nào sống**. `GameManager.LateUpdate` phát hiện wave trốn thoát nếu còn bất kỳ Hero nào, thay vì kiểm tra riêng Hero chính. Khi chạy lại wave, Hero chính đã chết không được tự hồi sinh. Camera chuyển sang Hero còn sống khi chủ thể cũ chết; nếu không còn ai sống, cơ chế camera xác một Hero trước đó được giữ.

### Giữ tương thích gameplay một Hero

`SetupHeroInfo` vẫn ràng buộc `primaryHeroState`; trường/properties B1 đã được bộ test cũ đọc qua `Tests~/MotionRegression.cs`, không tạo HP/timer dự phòng. `GameManager` chưa thay schema SaveManager/EntityDataSO, chưa có UI quản lý Hero thứ hai. B2 chỉ thay mã khi có actor đăng ký ngoài Hero chính và những kiểm tra an toàn khi tái sinh. `Assets/Scenes/MainGame.unity` **chỉ bổ sung hai thông số camera từ hotfix đã được người phát triển chấp nhận**, không thêm Hero vào Scene hay thay cấu trúc SaveManager.

## Kiểm thử đã bổ sung

`Tests~/B2CombatRoutingRegression.cs` có **14 ca** gồm: hai trạng thái HP/timer riêng; hai Hero đánh quái khác nhau; quái cận đánh Hero gần nhất; đạn quái cố định mục tiêu; Hero đổi life qua đăng ký lại hoặc SpawnHero; quái pool tái kích hoạt; hủy đạn khi caster chết; Hero chính chết vẫn giữ trận cho đồng đội; đổi thế của đồng đội không ảnh hưởng chính; EXP dành cho người kết liễu; wave thoát vẫn hồi khi chỉ còn đồng đội; unregister hủy đường sát thương cũ.

Mã kiểm thử tự động được gọi từ `Tests~/MotionRegression.cs`; `Tests~/run.sh` đã bao gồm tệp runtime `HeroCombatState.cs` ở B1 và dùng `Tests~/*.cs`, nên tự nhận cả bộ test B2 và bộ hotfix, không cần sửa danh sách riêng.

**Giới hạn xác minh:** đã kiểm tra tĩnh cấu trúc, vị trí callsite và các hợp đồng API; chưa chạy `mcs/mono`, Unity Play Mode hoặc Windows EXE trên môi trường kết nối GitHub này. Không tuyên bố các test mới đã PASS trên C#. Người phát triển cần **Pull nhánh B2 → Unity Console → thử gameplay một Hero và kiểm thử thêm 2 Hero bằng API, hoặc chạy `bash Tests~/run.sh` ở máy có Mono**.

## Đồng bộ hotfix .54g đã được kiểm thử (12/10/2026)

Người phát triển đã xác nhận **hotfix camera và hướng đi Y hoạt động ổn** trên `fix/0.0.4.54g-smooth-focus-free-y` tại commit tài liệu `26ffb4f1cc8f9f0c284fe5b8ddc2c63152b14642` (mã hotfix `88098e0bfc47771f12dd32524c5e37cfbf24dcf6`). Hai thay đổi này đã được **đồng bộ chọn lọc vào B2**; việc người phát triển chấp nhận hotfix **không có nghĩa toàn bộ B2 đã nghiệm thu**.

- **Camera:** giữ trung bình cộng tọa độ X các Hero sống + quái sống trong viewport; lọc tốc độ thay đổi độ lệch tâm nhóm so với Hero qua `CombatFocusSmoother`. Thông số mặc định `cameraCombatFocusSmoothTime=0.7s`, `cameraCombatFocusMaxSpeed=240` đơn vị/giây. `CurrentCameraFocusX` tiếp tục là tâm thô, `CurrentCameraSmoothedFocusX` là đích sau lọc. Deadzone 38%, follow 0.2s, nghỉ 40%/0.6s giữ nguyên. Có thể bật/tắt `cameraUseCombatCenter`.
- **Giữ Y:** Hero không có quái chỉ chạy về trái; bỏ lệnh kéo `Y` về 0. Cận chiến chỉ tiến về Y của quái khi đang ngoài tầm X; cung/phép vẫn tuân thủ khoảng đánh cũ.
- **Giữ riêng B2:** `EnvironmentManager` còn logic camera chuyển sang Hero sống khi chủ thể đang theo bị hạ; khi thay Hero theo dõi thì **reset cả DeadzoneCamera và CombatFocusSmoother**. `HeroController.SpawnVersion` của B2 vẫn giữ để hủy đạn thuộc kiếp sống cũ. Không dùng nguyên `EnvironmentManager` của hotfix đè lên B2.
- **Hồi quy:** thừa hưởng 21 tình huống camera (tăng 2 so với 19 của .54g), thêm 4 tình huống Y từ `Tests~/CameraAndLaneHotfixRegression.cs`, giữ 6 bài B1, và 14 bài B2 (**thêm 1 bài** kiểm tra đổi chủ thể camera sau khi Hero chính chết lúc bộ lọc tâm đang có vận tốc). Bộ runner `Tests~/MotionRegression.cs` gọi đủ các suite.
- **Trạng thái:** đã ghép và kiểm tra mã nguồn tĩnh; **chưa chạy Unity/Mono hoặc bản Windows của nhánh B2 đã đồng bộ**. Không tự merge vào B1, `.54g` hoặc `main`.

## Tiêu chí nghiệm thu B2

1. Bản một Hero: camera theo tâm combat, cận/cung/phép, vận sức, đạn đang bay, hạ quái, EXP/heal, map/wave, chết/Retry, save cũ: **không hồi quy** so với checkpoint B1.
2. Khi thêm Hero thứ hai thủ công bằng API, hai Hero có nhịp đánh/máu khác nhau và đánh được các mục tiêu phù hợp. Quái gây sát thương vào đúng Hero đã chọn.
3. Đạn của Hero/quái bị hạ, bị hủy đăng ký hoặc sinh lại không thể gây sát thương cho bản thể mới; đạn bay không bất ngờ đổi mục tiêu sang Hero chính.
4. Mất một Hero không xóa trận nếu đồng đội còn sống; wave chạy tiếp và cả đội chết mới Game Over. Camera không kẹt vào xác khi còn Hero khác.
5. Bộ hồi quy và Unity không lỗi. Nếu không đạt, sửa trên nhánh B2 **không gộp main hoặc B1**.

### Những gì chưa làm

- Chưa có spawn Hero thứ hai trong Scene mặc định, giao diện chọn/thay đổi tổ đội, tư cách Hero chính mới, hồi sinh đồng đội, hoặc cấu trúc lưu **danh sách Hero**. Trong UI hiện tại, tiếp tục chơi một Hero như bình thường.
- Chưa có quyết định chia EXP đội, drop/đồ/skill, đội hình giữ cự ly, điều kiện Game Over nâng cao, camera đóng khung toàn đội ở cửa sổ quá hẹp. Những việc đó phải được thiết kế/kiểm thử ở lát cắt tiếp theo, không suy ra tự động từ B2.
