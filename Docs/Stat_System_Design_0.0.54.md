# 0.0.54 — Hệ chỉ số chung và bốn hướng tu luyện

**Trạng thái ngày 15/10/2026:** Đã thống nhất **bộ khung và nguyên tắc** trong trao đổi. Đây là **thiết kế**, chưa phải thay đổi Gameplay/Save/Inspector. Hệ số tăng từng điểm, giá trị khởi đầu và công thức tăng trưởng theo cấp/cảnh giới cần cân bằng sau.

## 1. Mọi thực thể dùng cùng bộ chỉ số

Hero, nhân vật đồng hành, quái và Boss cùng một hệ định nghĩa chỉ số; không bắt buộc mọi giá trị khác 0 hoặc cùng hiển thị UI.

**Nhóm công kích:** công vật lý, công pháp thuật, tốc độ đánh, **chính xác (%)**, xác suất chí mạng, sát thương chí mạng, tăng sát thương vật lý (%), tăng sát thương pháp thuật (%).

**Nhóm sinh tồn:** HP tối đa, hồi HP, Giáp, Kháng phép, hút sinh lực (%).

**Nhóm linh lực & vận chiêu:** MP tối đa, hồi MP, tốc độ vận chiêu, giảm thời gian hồi chiêu (%).

**Nhóm cơ động:** tốc độ di chuyển, né tránh (%). Chính xác thuộc **Công phạt**, không phải Thân pháp.

**Nhóm trạng thái:** giảm thời gian chịu trạng thái bất lợi (debuff duration reduction).

**Ngũ hành (chỉ số nền bẩm sinh):** tăng sát thương và kháng riêng cho Kim, Mộc, Thủy, Hỏa, Thổ. `Vô` là loại linh căn độc lập, không đổi thành `Chaos`; không tạo hệ `Hỗn mang`. Giữ nguyên quy tắc khắc hệ/cấp linh căn hiện hành. Các modifier tạm thời/nguồn trang bị hoặc võ học được tính tách khỏi đặc tính bẩm sinh.

**Nhóm mở rộng chưa kích hoạt:** chặn đòn, giảm sát thương tổng, tăng vùng hiệu ứng, tăng tầm kỹ năng, tốc độ đạn, thời lượng kỹ năng, hồi HP khi đánh trúng/kết liễu, chỉ số liên quan triệu hồi, đa đòn, nhiều đạn… Chỉ tạo cơ chế/hệ số khi thực sự có hệ thống sử dụng, không tự copy mọi dòng từ trò chơi tham khảo.

## 2. Bốn hướng tu luyện (phương án B — đã chọn)

Một điểm vào **hướng** làm tăng **nhiều chỉ số liên quan**, không cộng trực tiếp từng chỉ số bằng điểm tự do:

| Hướng | Các chỉ số có thể nhận từ điểm hướng |
| --- | --- |
| Công phạt | Công vật lý, công pháp thuật, tốc độ đánh, sát thương chí mạng, **chính xác** |
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

**Đã chốt tạm (15/10/2026):**

- **Chính xác cơ bản của mọi nhân vật:** **80%** (khi chưa có nguồn cộng khác).
- **Chính xác thuộc hướng Công phạt**: khi người chơi đầu tư điểm Công phạt, Chính xác là một chỉ số được tăng cùng các chỉ số khác. **Mỗi điểm Công phạt tăng Chính xác bao nhiêu vẫn chưa chốt.**
- **Chênh lệch cấp độ** được xét khi A tấn công B: A cao hơn B một cấp thì Chính xác tăng **2 điểm phần trăm**; thấp hơn một cấp thì giảm **2 điểm phần trăm**. Chỉ tính tối đa **5 cấp chênh lệch mỗi chiều**, nên phần điều chỉnh cấp nằm trong **[-10, +10] điểm phần trăm**.
- **Trần Chính xác hiệu lực:** **100%**; **trần Né tránh hiệu lực:** **45%**. Hai giới hạn xét độc lập, sau đó chỉ tung **một lần** để xác định có trúng hay không.

```text
levelDifference = clamp(attacker.level - defender.level, -5, +5)
levelAccuracy   = levelDifference * 2 điểm phần trăm
accuracy        = clamp(80% + levelAccuracy
                        + accuracyFromCultivation
                        + accuracyFromEquipment
                        + accuracyFromOtherModifiers, 0%, 100%)
dodge           = clamp(defenderDodge, 0%, 45%)
hitRate         = clamp(accuracy - dodge, 0%, 100%)
```

Ví dụ **chưa cộng Công phạt, trang bị hoặc né tránh**:

| Cấp độ người tấn công so với mục tiêu | Chính xác | Xác suất trúng nếu mục tiêu né 0% |
| --- | ---: | ---: |
| Kém từ 5 cấp trở lên | 70% | 70% |
| Kém 2 cấp | 76% | 76% |
| Bằng cấp | 80% | 80% |
| Hơn 2 cấp | 84% | 84% |
| Hơn từ 5 cấp trở lên | 90% | 90% |

Nếu đối thủ né **25%**, cùng cấp và chưa cộng Chính xác thì tỉ lệ trúng = **80% - 25% = 55%**. Nếu né tối đa 45%, cùng cấp và chưa cộng Chính xác thì còn **35%**.

**Lưu ý cân bằng:** 80% cơ bản nghĩa là ngay cả khi mục tiêu không có né tránh, hai bên ngang cấp vẫn hụt 20% số đòn trung bình. Đây là giá trị người phát triển đã chọn, cần nghiệm thu cảm giác đòn đánh và hiệu ứng `Miss` trong Play Mode, không tự nâng lên 100%. Chênh lệch cấp độ thay đổi Chính xác, **không tự cộng hoặc trừ trực tiếp vào Né tránh** trong luật hiện tại.

- Công thức xử lý là **một lần xét trúng**, không tung thêm lượt né riêng sau khi đã xét Chính xác.
- Nguồn cộng khác và ngoại lệ kỹ năng (đòn chắc chắn trúng, phép diện rộng...) chưa chốt; các trường `accuracyFrom...` là ví dụ nguồn cộng, không phải công thức phân điểm đã khóa.
- Chênh lệch cấp **không** được cộng vô hạn và cũng không ảnh hưởng nguyên tắc ngũ hành/khắc hệ.
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
