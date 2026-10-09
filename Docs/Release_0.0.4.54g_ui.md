# IdleGame 0.0.4.54g_ui — Khôi phục bảng chỉ số và hiệu ứng hiển thị

**Cơ sở**: `feature/0.0.4.54f` tại `8ab08b64016fad4f57cc9dc38153bb4bc1b4cad4`. `.54g` đầu tiên sửa hiển thị; **bổ sung cuối `.54g`** sửa cả bù trừ chuyển động quái đứng đánh. Không đổi công thức cân bằng, tầm đánh, thuật toán đạn, tiến trình map hay save.

## Bảng chỉ số / log

- Bảng HUD lấy lại **toàn bộ** `HeroController.FullStatDetails` thay vì chỉ 3 dòng HP/ATK/EXP.
- Giữ cấp, dạng đánh, HP, EXP, sát thương nền và cộng, chi tiết sát thương theo từng linh căn, tốc độ di chuyển, tốc đánh, thời gian mỗi đòn và tầm đánh.
- Bảng neo trên **bên phải**, bên dưới Chỉ số–DEVB–Tạm dừng; log rút gọn neo trên **bên trái** và bắt đầu cùng cao độ. Ba nút Cận chiến/Cung/Phép đặt dưới log, không đè lên bảng chỉ số.
- Bố cục có chiều cao thay đổi theo số dòng dữ liệu; đọc lại bố cục khi số dòng linh căn/chỉ số thay đổi, không chỉ khi resize.
- Giữ trạng thái modal của .54f: không đè lên màn hình Bắt đầu, xác nhận, Game Over hoặc menu chi tiết. Cửa sổ 600/800/1150 đều sử dụng cùng nguyên tắc chia cột.
- Khung chỉ số được giữ nguyên trên giao diện để theo dõi cân bằng trong giai đoạn thử logic; về sau dự kiến chuyển vào nút riêng.

## Tên bản đồ

- Xuất hiện chữ lớn **giữa cửa sổ** ngay khi vào game và mỗi lần tiến sang bản đồ mới.
- Đứng giữa **3 giây**, sau đó đồng thời thu chữ nhỏ và dịch chuyển trong **0,85 giây** (nội suy tăng tốc/giảm tốc mượt).
- Vị trí đích tạm: nhãn nhỏ phía trên vùng chơi, **cột trái dưới log và nút chọn dạng đánh**; không đè bảng chỉ số cột phải. Nhãn nhỏ còn hiện cho đến lần chuyển map tiếp theo.
- Đóng/ẩn khi quay về màn hình bắt đầu, khi Hero chết hoặc khi giao diện modal đang mở. Không chạm vào map progress, camera hoặc Ground.

## Chữ sát thương Hero và quái

- Chữ hiển thị ngay lúc nhận damage, bay **ra xa hướng xuất phát của đòn đánh** (quái đánh Hero / Hero đánh quái; nổ phép theo tâm vùng nổ) và nhô lên thành quỹ đạo cong.
- Độ mờ giảm đều và kết thúc trong **1 giây**; không sửa sát thương thực nhận, tầm đánh, đạn hoặc HP.
- Vị trí chữ được khôi phục khi hiệu ứng kết thúc hoặc quái tái sử dụng trong pool.
- Dùng quy tắc chuyển động chỉ dành cho giao diện trong `BattleMotion.cs` để bảo vệ các tính toán di chuyển/hitbox của game.

## Đối chiếu và kiểm thử

- Thêm `Tests~/Patch54gRegression.cs` (5 kiểm tra): đủ trường chỉ số, chia cột không chồng nhau, 3 giây giữ tên map và thời gian di chuyển, hiển thị tên map trong giao diện giả lập, hướng nảy + độ mờ 1 giây.
- Đây là kiểm tra nguồn/mô phỏng; **chưa chạy thành công Unity Editor hoặc Windows build** trong môi trường này. Chưa thể bảo đảm hiển thị thực tế không còn tràn ở tất cả trường hợp nhiều linh căn.
- Khi thử trên máy: kiểm tra 600/800/1150; khởi đầu/Game Over/DEVB/Menu; tăng cấp và nhiều linh căn; lần đầu deploy, sau 5 wave chuyển map; bị đánh từ hai phía; tần suất nhiều lần trúng sát thương; resize đang bay đạn.
- Nếu cần cải thiện cân bằng sau đó, làm phiên bản khác, **không pha vào bản UI này**.

## Bổ sung hoàn thiện .54g — Wave, địa hình, damage popup, quái đứng đánh

**Nguồn:** người phát triển thử bản .54g và phản hồi thiếu Wave/địa hình, chữ damage lệch Hero/quái, quái ranged đứng đánh trôi lệch Ground khi Hero cận tiến tới.

- **Tên map** giữ hoạt ảnh giới thiệu cũ (giữa màn hình 3 giây, thu nhỏ/dịch 0,85 giây). Khi đã thu nhỏ, ngay bên dưới thêm nhãn debug gồm **Bản đồ N · Wave X/5**, và **Địa hình: ...** (nguồn `EntityDataSO.mapNumber`, `completedWavesInMap`, `WorldNames.Terrain(mapTerrain)`). Mỗi wave cập nhật lại nhãn qua `RestoreProgress`; không khởi động lại hoạt ảnh khi wave thay đổi trong cùng map.
- **Chữ damage Hero**: tại scene, `Hero_DMG_Text` là một đối tượng anh em của Hero nằm trực tiếp trên Ground với tọa độ thiết kế cố định; runtime chuyển RectTransform này thành con của `heroRect`, chỉnh neo giữa thân, vị trí khởi đầu theo chiều cao cơ thể. Giữ hướng bật ngược nguồn sát thương/fade trong 1 giây.
- **Chữ damage quái**: `Monster_DMG_Text` nằm dưới `Monster_Hp_Fill` trong prefab, nên chịu offset của thanh HP. Runtime chuyển thành con trực tiếp của `MonsterController.Rect`, neo giữa thân và phục hồi tọa độ gốc khi trả về pool. Không cần sửa trực tiếp scene/prefab.
- **Chuyển động quái đánh xa**: `EnvironmentManager.Update` vốn tính `groundMinusCamera = backgroundPan - cameraPan`, song `BattleMotion.MonsterApproach` bỏ qua phần bù khi quái đủ tầm đánh. Phần bù mới áp dụng **ở mọi trạng thái**; bước tự đi (tốc độ riêng) bị giới hạn theo khoảng cách còn lại, riêng phần bù vận chuyển Ground không bị cắt mất. Quái không có target cũng nhận bù để đi cùng lớp Ground. `PanWorld(cameraPan)` và `PanEnvironment(backgroundPan)` giữ nguyên.
- **Test bổ sung:** thay kỳ vọng quái đang đứng đánh từ cameraPan sang backgroundPan trong `Tests~/GroundRelativeMotionRegression.cs`. Thêm `Tests~/Patch54hRegression.cs` (4 nhóm): Hero cận di chuyển tới quái ranged, chênh tốc độ Ground–camera; bù tốc khi đứng đánh; vị trí cha của popup damage Hero/quái; wave+terrain debug.
- **Tài liệu dự án:** đồng bộ lại README.MD, Project_info_dev.txt và xóa tài liệu cũ `WINDOWS_OVERLAY_FIX_0.0.4.51f.txt` khỏi nhánh. Nội dung liên quan WinAPI, MenuArea, kéo/resize vẫn có trong README và Git history.

### Nguy cơ cần tiếp tục kiểm thử

- Popup text hiện **gắn với đối tượng nhận sát thương**; khi quái chết và bị trả về pool ngay lập tức, nhãn có thể bị ẩn trước khi đủ 1 giây. Muốn hiển thị trọn cú kết liễu cần tách popup sang quản lý hiệu ứng độc lập trong phiên nâng cấp VFX sau. Không nói rằng mọi nhãn trúng đòn đã fade trọn vẹn ở mọi trường hợp.
- Quái đang dừng đánh cần thử khi Hero di chuyển nhiều tốc độ, lúc camera gần biên, Ground/hero vận tốc khác nhau, thay đổi chế độ và pause. Các bài test mô phỏng chỉ xác minh toán học/theo stub, không thay thế Unity Play Mode.
- Chưa chạy Unity Editor hoặc Windows EXE trong môi trường sửa; không tự tuyên bố bản vá hết lỗi. Gộp main là thao tác chủ động của người phát triển.

### Lộ trình sau khi nghiệm thu

1. **Trang bị/item**: thông số cộng (HP/ATK/phòng thủ, giảm sát thương, damage theo hệ và vũ khí), cộng/trừ có thể tách base; lưu và tương thích save.
2. **Slot item**, placeholder UI để nối item schema, ô trang bị/kho.
3. **Skill**: placeholder và hệ tự động dùng kỹ năng theo thứ tự ưu tiên; nhân vật có skill gần/xa, cần phân lớp skill tách khỏi thế đánh mặc định.
4. **Âm thanh**: placeholder sự kiện/asset cho hit, cast, UI, map.
5. **Tool biên tập asset giao diện**: import/gắn animation/effect/sprite/âm thanh cho nhân vật, skill, vũ khí, với schema và kiểm tra reference trước khi xây.

Chưa triển khai các hệ này trong .54g; chưa chốt số ô item, bảng kỹ năng, công thức thưởng item hoặc định dạng nhập tool.
