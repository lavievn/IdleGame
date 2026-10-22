# Kiểm thử sprite W2.1a

Chạy riêng nhóm mới: `bash Tests~/run.sh --sprites` (cần `mcs` và `mono`).
Chạy lệnh không có đối số để chạy tiếp các nhóm cũ. Không bỏ qua hoặc sửa
assertion cũ; toàn bộ suite hiện vẫn bị chặn tại `GroundPresentation.fixedRect`
trong `TerrainRegression.cs`. Không coi toàn bộ suite là PASS.

28 kịch bản mới chạy bridge, HeroController, MonsterController và CombatManager
thật trên UnityStub. Không đọc/ghi save, không tạo scene gameplay. UnityStub chỉ
ghi nhận texture/rect khi tạo Sprite; không kiểm chứng importer hay hình ảnh.

- Attack hiển thị 0,16 giây, Hit hiển thị 0,2 giây, dùng thời gian game hiện tại.
- Hit trong Attack bị bỏ qua, không xếp hàng và không gia hạn Attack.
- Attack mới thay Hit ngay; một đòn mới có thể bắt đầu cửa sổ hiển thị mới.
- Chết, ẩn, đổi thế đánh, disable bridge và tái sử dụng xóa pose đang diễn ra.
- Không thay damage, thời điểm phóng đạn, cooldown, AI hoặc tọa độ chiến đấu.

Đã bao phủ sáu tổ hợp phe/thế đánh, đòn liên tiếp, biên hết hạn trước Update,
đang di chuyển nhận Hit, chết/ẩn/spawn, reroll quái đúng một lần, giao chiến
cận chiến đồng thời vẫn giảm HP và hiện popup, phóng tên/phép, hai Hero B2,
scale/flip riêng và camera pan đồng bộ. Các tham chiếu Image được kiểm tra là
serialized để Unity có thể remap khi clone; Mono không xác minh native cloning.

Cần nghiệm thu Unity thật trên nhánh tích hợp:

1. Hero kiếm và quái đánh đồng thời; HP/popup cập nhật, Attack không bị Hit che.
2. Hero/quái cung và phép hiện Attack đúng lúc phóng; đang chờ/di chuyển nhận Hit.
3. Nhận nhiều hit trong Attack rồi ngừng đánh: về Normal, không phát lại Hit.
4. Chết, đổi thế đánh, ẩn/spawn và quái pooled: không còn pose cũ.
5. Tạo đồng đội B2 bằng UI thật: Image/pose độc lập, không nhân đôi hình; kiểm tra
   hướng, tỷ lệ, tint quái, HP bar, popup và camera. Kiểm tra Console và Player build.

Chưa chạy Unity Editor, bộ 51 Edit Mode hoặc 3 Play Mode Save/Load ở W2.1a.
Các file kiểm thử Save/Load và runtime Tuần 1 được giữ nguyên từ nền ổn định.
