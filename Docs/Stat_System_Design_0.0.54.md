# 0.0.54 — Hệ chỉ số chung và bốn hướng tu luyện

**Trạng thái ngày 15/10/2026:** Đã thống nhất **bộ khung và nguyên tắc** trong trao đổi. Đây là **thiết kế**, chưa phải thay đổi Gameplay/Save/Inspector. Hệ số tăng từng điểm, giá trị khởi đầu và công thức tăng trưởng theo cấp/cảnh giới cần cân bằng sau.

## 1. Mọi thực thể dùng cùng bộ chỉ số

Hero, nhân vật đồng hành, quái và Boss cùng một hệ định nghĩa chỉ số; không bắt buộc mọi giá trị khác 0 hoặc cùng hiển thị UI.

**Nhóm công kích:** công vật lý, công pháp thuật, tốc độ đánh, xác suất chí mạng, sát thương chí mạng, tăng sát thương vật lý (%), tăng sát thương pháp thuật (%).

**Nhóm sinh tồn:** HP tối đa, hồi HP, Giáp, Kháng phép, hút sinh lực (%).

**Nhóm linh lực & vận chiêu:** MP tối đa, hồi MP, tốc độ vận chiêu, giảm thời gian hồi chiêu (%).

**Nhóm cơ động & trúng đòn:** tốc độ di chuyển, chính xác (%), né tránh (%).

**Nhóm trạng thái:** giảm thời gian chịu trạng thái bất lợi (debuff duration reduction).

**Ngũ hành (chỉ số nền bẩm sinh):** tăng sát thương và kháng riêng cho Kim, Mộc, Thủy, Hỏa, Thổ. `Vô` là loại linh căn độc lập, không đổi thành `Chaos`; không tạo hệ `Hỗn mang`. Giữ nguyên quy tắc khắc hệ/cấp linh căn hiện hành. Các modifier tạm thời/nguồn trang bị hoặc võ học được tính tách khỏi đặc tính bẩm sinh.

**Nhóm mở rộng chưa kích hoạt:** chặn đòn, giảm sát thương tổng, tăng vùng hiệu ứng, tăng tầm kỹ năng, tốc độ đạn, thời lượng kỹ năng, hồi HP khi đánh trúng/kết liễu, chỉ số liên quan triệu hồi, đa đòn, nhiều đạn… Chỉ tạo cơ chế/hệ số khi thực sự có hệ thống sử dụng, không tự copy mọi dòng từ trò chơi tham khảo.

## 2. Bốn hướng tu luyện (phương án B — đã chọn)

Một điểm vào **hướng** làm tăng **nhiều chỉ số liên quan**, không cộng trực tiếp từng chỉ số bằng điểm tự do:

| Hướng | Các chỉ số có thể nhận từ điểm hướng |
| --- | --- |
| Công phạt | Công vật lý, công pháp thuật, tốc độ đánh, sát thương chí mạng |
| Hộ thể | HP tối đa, Giáp, Kháng phép, hồi HP, hút sinh lực |
| Thân pháp | Tốc độ di chuyển, né tránh, tỷ lệ chí mạng |
| Linh tu | MP tối đa, hồi MP, tốc độ vận chiêu, giảm hồi chiêu |

**Không chốt:** + bao nhiêu cho mỗi điểm; mức giảm dần; cấp/cảnh giới cấp bao nhiêu điểm; tổng điểm; giới hạn từng nhánh; chi phí reset. Không lập bảng hệ số giả định rồi âm thầm áp dụng vào game.

**Tăng trưởng tự nhiên:** HP tối đa, công vật lý/pháp thuật nền, MP tối đa và các chỉ số cơ bản đã bàn sẽ có tăng trưởng độc lập theo cấp/cảnh giới. Điểm tu luyện là **một nguồn bổ sung**, không được ghi đè chỉ số tự nhiên hoặc trang bị. Các thuộc tính phát sinh khác còn để cân bằng sau.

**Tái phân bổ:** hướng đến vật phẩm kiểu Tẩy Tủy Đan/Tái Mạch Đan/Hoán Cốt; chỉ hoàn trả phần điểm tu luyện, không reset cấp/cảnh giới, linh căn gốc, tài sản hay võ học. Cơ chế và độ hiếm vật phẩm chưa chốt.

## 3. Giao diện (UI art vẫn đang triển khai)

- Bảng phân bổ chính chỉ có **4 chỉ số tổng hợp**: tên hướng + số điểm đã đầu tư (các chi tiết không cần chiếm chỗ cố định).
- Khi **hover** hoặc **chọn/click** đúng mục tiêu, hiện bảng thông tin con với danh sách chỉ số ảnh hưởng, giá trị hiện tại và phần tăng thêm từ hướng đó; click/select là lối dùng khi không có chuột hover.
- Khi cần xem chi tiết **một chỉ số** riêng, hover/select vào đúng chỉ số đó để hiển thị giải thích, công thức/nguồn cộng; tránh hiển thị tất cả thông tin liên tục.
- Không thay asset UI hoặc bố cục Inventory/Information hiện tại trước khi bộ mỹ thuật hoàn thiện. Chưa triển khai tooltip runtime ở bước ghi tài liệu này.

## 4. Giới hạn và phép tính trúng đòn

**Đã chốt tạm:** chính xác không quá **100%**, né tránh không quá **45%** trong hệ thống cơ bản (chưa áp dụng hệ số cấp).

```text
accuracy = clamp(accuracy, 0%, 100%)
dodge    = clamp(dodge, 0%, 45%)
hitRate  = clamp(accuracy - dodge, 0%, 100%)
```

- Đây là **một lần xét trúng**, không dùng thêm lần tung né riêng sau khi đã xét chính xác.
- Chưa chốt giá trị chính xác bẩm sinh, nguồn cộng chính xác, tác dụng buff/debuff và trường hợp đòn không thể né.
- **Chênh lệch cấp độ hai bên:** dự định có hiệu ứng điều chỉnh khả năng né/trúng sau này. **Chưa chốt công thức hoặc dấu hiệu ứng**, không áp dụng tạm hệ số vô căn cứ.
- **Hồi phục và né tránh cần giới hạn:** với HP/MP hồi phục, người phát triển cũng muốn giới hạn để tránh vượt cân bằng. Chưa chốt giới hạn hồi HP/s, hồi MP/s hoặc hút sinh lực; không dùng 45% né để suy ra các mức trần này.
- **Cần làm rõ sau:** ý tưởng xét chênh lệch cấp độ cho hồi phục không thể diễn giải giống né tránh (hồi phục có thể hoạt động khi không có đối thủ). Không gắn phụ thuộc `target.level` cho hồi phục nếu chưa có luật rõ ràng.

## 5. Giảm thời gian trạng thái bất lợi

Dùng **một chỉ số chung** `DebuffDurationReduction` thay cho nhiều chỉ số `tránh choáng`, `tránh xuất huyết`, `kháng làm chậm` theo loại.

- Ví dụ người chơi bị **Xuất huyết 5 giây**, chỉ số này giảm **thời gian tồn tại** của trạng thái bất lợi, không tự động giảm sát thương mỗi tick hoặc giảm tỷ lệ áp trạng thái.
- Công thức minh họa cần chốt sau (ví dụ `effectiveDuration = baseDuration × (1 - reduction)`), chưa xác định giá trị trần và trường hợp miễn nhiễm.
- Các ngoại lệ (Boss, trạng thái không thể giảm, hiệu ứng cưỡng chế, cộng dồn) chưa được quyết định.
- Không tạo chỉ số độc lập `tránh khống chế` vào bộ chung hiện tại.

## 6. Tương thích code hiện tại / thứ tự triển khai

- `CombatStatId.Attack` vẫn đang là một chỉ số chung; `CombatDamage.Resolve` tính base attack theo `AttackMode`, chưa phân biệt base vật lý/pháp thuật độc lập.
- `CombatStatId` đã có HP, Giáp, Kháng phép, tăng sát thương và kháng theo ngũ hành, tốc đánh/di chuyển/vận chiêu/giảm hồi chiêu, hồi HP/MP; hiện chưa có đầy đủ `MaxMana`, `PhysicalAttack`, `MagicalAttack`, `Accuracy`, `Dodge`, `CriticalChance`, `CriticalDamage`, `LifeLeech`, `DebuffDurationReduction`.
- `EntityDataSO` đang có điểm tăng HP/ATK kiểu cũ, chưa có phân bổ theo bốn hướng.
- **Không đổi thứ tự các giá trị enum đã lưu** nếu dùng Unity JsonUtility và ID enum. Nên bổ sung các giá trị mới ở cuối danh sách, tạo lớp quy đổi phiên bản Save, và chỉ chuyển hệ tăng điểm cũ sau khi chốt thuật toán chuyển đổi/hoàn trả.
- Đề nghị thứ tự tiếp theo: (1) schema chỉ số chung không phá save, (2) công vật lý/pháp thuật độc lập, (3) độ chính xác–né tránh/chí mạng, (4) buff/debuff và trần, (5) phân bổ điểm/tooltip, (6) cân bằng theo cấp/cảnh giới. Chỉ bắt đầu code sau khi xác nhận phạm vi triển khai.

**Ghi chú:** Chỉ số ngũ hành là phần **bẩm sinh cố định khi nhân vật sinh ra**. Điều này không cấm trang bị hay hiệu ứng thời gian thực tăng sức mạnh nguyên tố, nhưng tuyệt đối không sửa thông tin linh căn gốc khi cộng điểm tu luyện.
