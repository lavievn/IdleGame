# System Menu / xác nhận Reset — hotfix Windows

Chạy kiểm tra Mono tập trung: `bash Tests~/run.sh --desktop-ui`.
Các nhóm cũ vẫn giữ assertion; kiểm tra menu cập nhật danh sách thành sáu chức
năng cũ cộng nút Close có callback riêng, thay vì chỉ tăng số lượng.

15 kịch bản mới bao phủ ba kích thước 600/800/1150, hai thứ tự khởi tạo
UIManager/StartMenuUI, đóng menu nhiều lần, Back của các trang con, đổi kích
thước, UI phía sau không giành click và Reset qua hai bộ dispatch click.
Opening đã ẩn với `entryLocked=true`, `openingMenuReady=false` khi mở Reset.
Hủy giữ RAM và các file; xác nhận chỉ thay AutoSave, giữ hai ô thủ công nguyên
byte. Chặn callback lặp từ Button/CustomInteractable sau khi xử lý xong.

Các bài mới dùng runtime thật với UnityStub, thư mục GUID tạm từ MenuManager,
không gọi SaveManager.Awake và không sử dụng save thật. Cleanup chỉ xóa ba
slot có tên cố định rồi xóa thư mục rỗng, không xóa đệ quy. Không kiểm chứng
JsonUtility native, EventSystem scheduling, thứ tự vẽ Canvas hay Win32 bằng Mono.

Cần nghiệm thu lại trong Unity Play Mode và Windows Player:

1. Ở mỗi kích thước, mở/đóng menu ba lần; Save/Load/Size/Overwrite → Quay lại.
2. Reset khi đang chơi → Hủy; nhân vật và tiến trình không đổi, tiếp tục thao tác.
3. Với dữ liệu thử đã sao lưu an toàn: Reset → Xác nhận; nhân vật mới cấp 1,
   AutoSave cập nhật, hai ô thủ công vẫn tải được dữ liệu cũ.
4. Thử cạnh/góc các nút, chuột có jitter nhỏ và DPI Windows đang sử dụng;
   nút không kéo cửa sổ khi click, HUD phía sau không nhận click xuyên popup.
5. Continue/New Game từ title vẫn có opening, Save/Load và ReturnTitle hoạt động;
   xem Console, kiểm tra popup hiển thị trên các panel và không nhận click hai lần.

Không thay GameManager, SaveManager, dữ liệu JSON hoặc quy tắc Reset/New Game.
