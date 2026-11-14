# RC4 — Mastery và Linh căn

Mastery v1 tách weapon family ID và skill registry ID. Basic được tính khi thực thi hợp lệ. Skill chỉ được tính khi thực thi, nhiều impact vẫn một event. Runtime nhận đúng người chơi, phiên combat, đời Hero và action sequence. Snapshot chỉ chứa counter. Save cũ thiếu trường bắt đầu rỗng. Forge, Travel, Retry và Load không cấp counter. Debug hiển thị số đếm và không áp damage hay tốc đánh.

Linh căn được lưu theo danh sách element, tier và weight. Nhân Tộc có thể có nhiều linh căn. SynergyMath giữ cycle khắc hiện có Kim→Mộc→Thổ→Thủy→Hỏa→Kim, trường hợp khác trung tính. Công thức runtime hiện ±30% lúc đồng tier và điều chỉnh theo tier gap. Không xem tham số runtime cũ là cân bằng đã duyệt cho RC4. Chưa ghép linh căn thành class hoặc tự buộc vũ khí/võ học theo một hành.

Hướng thiết kế tiếp: data record nhận stable ID, source weapon/skill, element tags, compatibility, affinity/counter, requirement và reward. Giá trị XP/điểm, mốc, stack policy và effect chưa có thiết kế được duyệt. Tinh Huyết vẫn khóa và không mở Kim Đan. Workbook phân biệt dữ liệu hiện có và dữ liệu chưa định nghĩa.
