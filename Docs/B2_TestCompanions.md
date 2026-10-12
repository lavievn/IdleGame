# B2 — Gọi đồng đội thử nghiệm (chưa phải hệ thống tổ đội chính thức)

**Nhánh:** `feature/b2-combat-target-routing`, cập nhật 12/10/2026.

## Cách dùng

- Unity Editor → Play → bắt đầu game → nhấn nút nhỏ dưới ba lựa chọn đánh, nhãn `[B2] Gọi +1 (0/2)`.
- Nhấn lần thứ nhất: 2 Hero đang có mặt. Nhấn lần thứ hai: 3 Hero (giới hạn 1 chính + 2 đồng đội).
- Ở trạng thái 2/2, nút đổi thành `[B2] Thu hồi (2/2)`; nhấn để xóa đồng đội và trở về 1 Hero.
- Đồng đội thứ nhất chọn ngẫu nhiên một trong hai dạng KHÁC Hero chính; đồng đội thứ hai nhận dạng chưa có, để ba Hero có đủ Cận chiến/Cung/Phép (trừ khi người chơi đổi dạng của Hero chính sau khi gọi).
- Đồng đội dùng hình Hero chính làm placeholder nhưng tô màu khác, máu/timer/đạn/mục tiêu và EXP được quản lý riêng bằng API B2. Chưa có chỉ số đồng đội trong HUD, chưa có bám đội hình/di chuyển theo đội hoặc cứu đồng đội.
- Để thử Hero chết riêng: cho quái đánh vào một đồng đội hoặc gọi `CombatManager.ApplyDamageToRegisteredHero(actor, damage, source)` từ mã kiểm thử.
- Thu hồi sẽ `UnregisterAdditionalHero`, hủy visual/controller/data thử nghiệm; đạn cũ của actor bị gỡ bỏ không còn gây sát thương sau khi đã bị unregister.
- Nút chỉ xuất hiện trong gameplay (không ở Start, Game Over, các màn hình popup và menu). Khi Pause, nút hiện nhưng không tạo/thu hồi vì `Time.timeScale==0`.
- Retry, New Game, tải save, hủy/disable GameManager sẽ dọn đồng đội thử nghiệm. Save không thay đổi và không chứa đội hình B2.

## Phạm vi mã

- `HeroController.SpawnHero(bool followCamera)`: bổ sung overload, mặc định `SpawnHero()` tương thích; đồng đội dùng `SpawnHero(false)` để không giành camera.
- `GameManager`: bản sao RectTransform Hero nằm trong Ground, một controller mới đứng riêng, dữ liệu runtime độc lập; các đường ref HP/text được map vào con tương ứng thay vì vô tình trỏ về Hero chính.
- `UIManager`: nút tạo động qua `SmallButton` và `CustomInteractable`, tự chạy theo layout và hit testing của Windows overlay.
- `Tests~/B2TestPartyRegression.cs`: ba bài kiểm tra về focus camera, loại bỏ dạng tấn công trùng Hero chính, và giới hạn 3 Hero; tự gọi từ `MotionRegression`.

## Cần kiểm thử trong Unity Editor

1. Start game một Hero → Console sạch; nhấn 1, 2 lần rồi thu hồi. Kiểm tra đủ 2/3 actor cùng lúc, màu phân biệt và các dạng đánh khác nhau.
2. Chuyển Hero chính sang Kiếm/Cung/Phép trước khi gọi lại: đồng đội phải dùng hai dạng còn lại. Thử đạn cung và phép đồng thời.
3. Để một đồng đội hạ quái: EXP thuộc đồng đội đó, không cộng trùng cho chính; quan sát HP/hành vi chọn đích độc lập. Quái đánh mỗi Hero nhận damage đúng đối tượng.
4. Khi Hero chính chết, đồng đội vẫn chiến đấu và camera chuyển đúng Hero sống; chỉ Game Over khi cả ba đã chết.
5. Thu hồi khi có đạn đang bay, giết/quay vòng wave, resize 600/800/1150 và pause; không được gây ghost hit, mất mục tiêu hoặc reset camera.
6. Sau khi có 2 đồng đội → Retry, Start New, Continue/Load: không có visual ma hoặc đối tượng thử nghiệm còn đăng ký; save cũ phải hoạt động.

**Trạng thái:** mã nguồn đã cập nhật trên GitHub, chỉ kiểm tra tĩnh; máy sửa không có Unity hoặc Mono để chạy C# runner/Editor. Chưa được xem là nghiệm thu nhiều Hero.

## Lựa chọn thiết kế để bàn sau B2

Cơ chế cốt truyện/kỳ ngộ tuyển tối đa hai đồng đội, không bật/tắt tùy ý, có thể tuyệt giao vĩnh viễn là hướng trải nghiệm tiềm năng nhưng **chưa chốt**. Nên phân biệt đồng đội rời đội, tử vong, mất lòng tin và vĩnh viễn không thể tuyển lại. Nếu cho bỏ vĩnh viễn, cần UX xác nhận nhiều bước và cân nhắc hệ quả người chơi bỏ lỡ nhân vật; chưa thêm quy tắc này vào B2.
