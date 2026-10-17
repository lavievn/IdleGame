# 0.0.54 — Dị văn: Đêm Táng Linh Mạch (nguyên mẫu kể chuyện ngược)

**Trạng thái:** đã tích hợp mã lên nhánh `0.0.54_map_poly`; chưa chạy trong Unity Play Mode / bản build Windows tại môi trường trợ lý.

## Thiết kế cốt truyện mẫu

**Nguyên nhân (sự kiện đã kết thúc 300 năm trước):** Thiên Huyền Tông khai thác linh mạch cổ quá mức, làm nứt phong ấn. Trưởng lão Tạ Vô Trần cắt ba nhánh linh mạch để phong tỏa tà khí. Một thành trì bị hy sinh; Tạ Vô Trần chết. Tông môn đổ toàn bộ tội lỗi cho ông.

**Các hệ quả:**
- H1 — Linh mạch cạn kiệt, xuất hiện tà khí và yêu thú dị biến.
- H2 — Thành trì bị hủy, hậu duệ và các di vật phân tán.
- H3 — Thiên Huyền Tông dựng bia cáo tội, che giấu quá trình khai thác phong ấn.
- H4 — Ngọc giản / trận văn còn sót lại cho thấy Tạ Vô Trần cố ngăn tai kiếp; ông đã cứu nhiều vùng nhưng không hoàn toàn vô tội.

**Mục đích:** người chơi khám phá các sự thật đã tồn tại trong thế giới, không quay về thay đổi lịch sử. Cốt truyện là hoạt động tùy chọn; farm/di chuyển/cấp quái không bị tác động.

## Hệ thống nguyên mẫu triển khai

- `Assets/Script/StoryClues.cs`: bộ định vị di tích theo seed và sự kiện khám phá; không dùng `Random.Range` mỗi wave.
- **4 dấu vết cơ bản:**
  1. `tanglinh.rumor`: tự gặp sau wave hoàn thành đầu tiên trên bất kỳ World Map nào (không cần tìm tọa độ).
  2. `tanglinh.inscription`: bia đá; đạt từ 2 wave đã hoàn thành tại map được gán.
  3. `tanglinh.jade`: ngọc giản; đạt từ 3 wave tại map được gán.
  4. `tanglinh.seal`: trận văn; đạt từ 4 wave tại map được gán.
- **Suy luận:** `tanglinh.revelation` mở khi có cả bia đá + ngọc giản + trận văn, theo bất kỳ thứ tự nào. Tự thêm một dòng kết luận, không bắt người chơi tới Boss mới.
- Trong **mỗi đại khu**, ba map vị trí bằng chứng được chọn từ những map có `SuggestedLevel <= 13`, không phải Boss; nếu có ít nhất 3 map phù hợp thì ba điểm riêng biệt. Một bằng chứng có thể có nhiều nguồn/bản sao; chỉ cần phát hiện một nguồn là đủ cho mỗi nhân vật.
- **Seed:** New Game sinh một lần; Save cũ ở World Map chưa có truyện nhận một seed ổn định từ tên nhân vật và map đã lưu, được ghi vào lần Save tiếp theo. Các Save cũ chế độ map tuần tự vẫn giữ nguyên, chưa được kích hoạt Dị văn.
- **Dị văn:** nút nhỏ trên HUD, tái sử dụng panel đọc văn bản nhiều trang đang có; hiện manh mối đã biết và địa điểm truy tìm tiếp tại đại khu hiện tại. Trước khi gặp manh mối đầu tiên, không tiết lộ tên câu chuyện.
- **Log:** manh mối được ghi vào `FullEventHistory`, hiển thị trong ô log chiến đấu trong khoảng 8 giây rồi khôi phục log sát thương. Nhật ký Dị văn bền vững qua Save/Load, còn log sự kiện chiến đấu hiện có không lưu lâu dài như trước.
- `GameManager.OnMonsterDied` chỉ kiểm tra manh mối sau `CompleteWave()` thành công. Không cấp khi quái vượt màn hình và wave bị reset, không tự chuyển map, không sửa spawn/combat/equipment.

## Save & hồi quy

Thêm 3 trường trong `EntityDataSO`:
- `storyVersion: int`
- `storySeed: int`
- `storyFoundIds: List<string>`

`SaveManager.LoadGame` đặt tất cả về mặc định trước `JsonUtility.FromJsonOverwrite` (tránh di truyền dữ liệu từ Inspector / slot trước), rồi `StoryClues.Normalize` tạo seed tương thích cho Save World cũ. Các trường được ghi qua serializer JSON đang có, không sinh file lưu phụ.

`Tests~/StoryClueRegression.cs` bổ sung ba kiểm thử: seed xác định và không chọn Boss ở năm đại khu; nhặt ba vật chứng ngược thứ tự và suy luận chỉ một lần; slot 1/slot 2 cách ly, Save World cũ không kế thừa manh mối Inspector. `Tests~/run.sh` đã thêm file engine vào lệnh biên dịch.

## Cách kiểm tra thực tế

1. `git switch 0.0.54_map_poly` và `git pull --ff-only origin 0.0.54_map_poly`.
2. Trong Unity, Chơi mới; hoàn thành 1 wave, nhìn thông báo Dị văn. Nhấn **Dị văn** để xem những thông tin đã biết và gợi ý map.
3. Hoàn thành 5 wave nơi sinh để mở quyền di chuyển. Tới các map được gợi ý, hoàn thành tối thiểu 2/3/4 wave để tìm dấu vết. Có thể gặp theo thứ tự khác nhau.
4. Lưu Save 1; đi thêm tìm manh mối; lưu Save 2; Load lại từng ô, đảm bảo cả vị trí di tích lẫn các dấu vết đã biết không trộn nhau.
5. Để game tự farm tại một map đã nhận manh mối: manh mối không được spam khi vòng 5 wave lặp lại.
6. Kiểm tra bố cục và nút Dị văn tại ba độ phân giải 600 / 800 / 1150 trong Editor và build Windows.

## Những gì cố tình chưa triển khai

- Chưa có di tích 3D/2D, vật phẩm ngọc giản thật, cửa sổ nhận nhiệm vụ, NPC tương tác, Boss Phệ Linh Cổ Thú hay phần thưởng.
- Chưa có graph điều kiện đa tầng cho hàng chục truyện; bản thử mới có điều kiện AND của ba chứng cứ, và nguồn manh mối độc lập.
- Chưa làm hệ thế lực mô phỏng sống, diễn tiến offline hay AI sinh truyện tự do.
- Chưa sửa 133 map, bảng quái, cảnh giới/Thiên Kiếp, UI bản đồ hoặc các số liệu cân bằng.

**Tiêu chí nghiệm thu:** có cảm giác khám phá một câu chuyện từ các dấu vết rải rác, mà người chỉ muốn idle có thể bỏ qua toàn bộ. Nếu trải nghiệm chưa ổn, điều chỉnh cách xuất hiện/log/nhật ký trước khi bổ sung độ phức tạp.
