# Định hướng core gameplay & vòng lặp chơi — Bách Luyện Tiên Đồ

> **Trạng thái: BẢN LÀM VIỆC / CÓ THỂ SỬA ĐỔI — ghi nhận ngày 25/10/2026.** Đây là hướng phát triển và bộ tiêu chí thảo luận, **không phải đặc tả triển khai**. Các mục ghi `Đề xuất` chưa được phê duyệt thành tính năng. Không tự thay đổi gameplay, save, camera, UI hoặc cân bằng chỉ vì tài liệu này.

## Quyết định chính thức cập nhật — 05/11/2026

Các quyết định dưới đây đã được người dùng chốt, ưu tiên khi đối chiếu các đề xuất cũ. Chỉ ghi nhận thiết kế trong lần cập nhật này, không cấp quyền tự sửa gameplay hoặc schema.

- Không có class cố định; một nhân vật chính tự chiến đấu, tự do chọn vũ khí/công pháp/võ học.
- Trong thiết kế cuối cùng, **kiểu đánh/vũ khí không quyết định MaxHealth**. Hệ số HP theo Melee/Bow/Magic hiện tại là cơ chế tạm và cần được thay thế trong một nhiệm vụ riêng về sau; chưa chốt công thức thay thế và không sửa HP ở nhiệm vụ này.
- HP cộng thêm trong hệ trang bị sẽ đến từ nguồn phù hợp như giáp, trang sức, phù, ngọc và khảm nạm. Đây là định hướng nguồn chỉ số, chưa phải yêu cầu triển khai các hệ ngọc/khảm hoặc mở thêm template thưởng.
- Chưa phát triển công pháp, võ học, kỹ năng kích hoạt hoặc đột phá cảnh giới mới. Giữ nguyên nền tảng đang có; các nhánh roadmap liên quan được hoãn đến nhiệm vụ riêng.
- Staff và các template phòng thủ chưa đưa vào bảng thưởng trước khi có đặc tả chỉ số phù hợp; catalog và đồ đang tồn tại giữ nguyên.

### Gear Drop prototype đã phê duyệt

| Template ID | Cấp vật phẩm | Main stat | Phẩm chất |
|---|---:|---|---|
| `sample:iron_sword` | 1 | Attack +4 | Common |
| `sample:basic_bow` | 1 | Attack +4 | Common |

Equipment Enabled, chance **0,08**, chọn **Uniform** hai template, mỗi lần thành công một gear. Material Enabled, chance **0,60**, lượng **1–2** `material:refinement`. Đây là **balance prototype**, chưa phải cân bằng cuối cùng.

Đã đối chiếu MainGame ở HEAD `719ca69`: cấu hình đúng các giá trị trên, `controlledLootTest = false`; không sửa scene hoặc triển khai lại. [Roadmap hiện hành](architecture/INTEGRATION_ROADMAP.md) ghi rõ các phần đã hoãn. Không sửa Combat, Save schema hoặc balance chỉ từ cập nhật định hướng này.

## 1. Tuyên ngôn trải nghiệm

**Người chơi quyết định nhân vật hướng đến con đường nào; nhân vật tự trải qua hành trình ấy; kết quả mở ra những lựa chọn tiếp theo.**

- Trọng tâm là **nuôi dưỡng, quan sát và định hướng** nhân vật tu tiên, không phải thao tác liên tục.
- Chiến đấu, khám phá và trưởng thành vẫn diễn ra trong thế giới rộng lớn; nhân vật **không phải trung tâm bắt buộc** và không được bảo đảm trở thành mạnh nhất.
- Khi quay lại, người chơi cần thấy diễn biến có ý nghĩa, không chỉ một con số tăng. **Điều này chưa có nghĩa game đã mô phỏng tiến trình khi đóng ứng dụng.**
- Quyết định chuẩn bị phải ảnh hưởng thật đến kết quả; yếu tố may rủi và thất bại được phép tồn tại nhưng phải có khả năng hiểu và học hỏi.
- Giữ tinh thần idle: không bắt xác nhận thưởng sau mỗi wave, không ép checklist hằng ngày, không buộc can thiệp liên tục giữa trận.

## 2. Core loop đề xuất

```text
CHUẨN BỊ / ĐỊNH HƯỚNG
  ├─ Trang bị, võ học, cách đầu tư sức mạnh
  └─ Chọn nơi tiếp tục farm hoặc thử khu vực mới
             ↓
TỰ KHÁM PHÁ (di chuyển / quãng đường / phát hiện)
             ↓
TỰ CHIẾN ĐẤU (đối thủ / thắng / thua / hồi sinh)
             ↓
TÍCH LŨY & PHẢN HỒI (EXP / chiến lợi phẩm / diễn biến)
             ↓
ĐÁNH GIÁ LỰA CHỌN
  ├─ Tiếp tục farm ở map hiện tại
  ├─ Điều chỉnh trang bị/võ học/rèn
  └─ Tự quyết định chuyển tới map đã mở
             ↺
```

- **Không cần một lựa chọn thủ công sau mỗi wave.** Một lần chuẩn bị có thể phục vụ nhiều wave.
- **Không tự chuyển map** sau khi đủ điều kiện mở map mới. Quy tắc world-map hiện hành là farm tại map đang đứng cho đến khi người chơi quyết định travel.
- Wave khám phá hiện dùng **quãng đường đi chung** theo mã đã đối chiếu trong snapshot 25/10, không quay lại thiết kế cứ 5 giây gọi wave; vẫn cần kiểm thử trong Unity.
- Cái chết có hậu quả và cơ chế quay lại/tiếp tục, **không wipe toàn bộ save**. Chi tiết checkpoint W2.3 chưa được chốt/tích hợp đầy đủ.
- Không ngầm khẳng định game đã hỗ trợ offline progression.

## 3. Ba tầng nhịp chơi

| Tầng | Vòng lặp | Kết quả người chơi cần nhận ra |
| --- | --- | --- |
| **Ngắn — wave** | Di chuyển → gặp quái → chiến đấu → nhận EXP/vật phẩm → tiếp tục | Chiến đấu rõ ràng, dễ quan sát, ít thao tác |
| **Trung — map** | Farm ổn định → phát triển → mở điều kiện → thử map khác nếu muốn | Biết vì sao mạnh hơn, vì sao thất bại, vì sao nên thử hướng mới |
| **Dài — hành trình** | Tích lũy võ học/trang bị, khám phá vùng đất, những lần gặp gỡ và diễn biến | Nhân vật có dấu ấn riêng trong thế giới, không chỉ tăng chỉ số |

**Ưu tiên thiết kế:** Làm tầng **trung — map** có chiều sâu và chơi được trước khi mở rộng tầng dài.

## 4. Ba nhóm quyết định chính

1. **Phát triển theo hướng nào?** Chọn trang bị, lối đánh và võ học; tiếp tục giữ định hướng đòn đánh thường vật lý vô hệ, vật lý/pháp thuật đầu tư riêng, linh căn áp vào các kỹ năng tương ứng. Tác dụng chiến đấu phải quan sát/đo được.
2. **Ở lại hay mạo hiểm tiến lên?** Farm khu vực quen thuộc để tích lũy ổn định hoặc chủ động thử map khó hơn; thất bại không đồng nghĩa xóa sạch tiến trình.
3. **Giữ món đang tốt hay đánh cược tiềm năng?** Dùng cơ chế phẩm trang bị, rèn và dòng phụ ẩn đã thiết kế để tạo lựa chọn có đánh đổi. Không tự thêm tiền tệ hoặc thay tỷ lệ rèn khi chưa được yêu cầu.

Ba nhóm liên kết: **map → thử thách và tài nguyên → cách phát triển → khả năng vượt map tiếp theo**.

## 5. Phản hồi tối thiểu để vòng lặp có ý nghĩa (ĐỀ XUẤT)

- **Sau nhặt đồ:** có thể so sánh thông số và thấy hiệu quả trang bị sau khi thay đổi.
- **Sau thất bại:** có bằng chứng dễ hiểu về nguyên nhân khả dĩ (loại sát thương, khả năng chịu đòn, tầm đánh, chiêu khắc chế), không đưa ra chẩn đoán giả khi thiếu dữ liệu.
- **Sau nhiều wave:** thấy EXP, vật phẩm và tiến triển đáng chú ý; tránh cửa sổ nhận thưởng liên tục.
- **Về sau:** nhật ký hành trình ghi **sự kiện thực sự xảy ra**, không bịa các sự kiện chỉ để tạo cảm giác thế giới sống.
- Thông tin phải rõ ràng ở mức người chơi phổ thông, còn log kỹ thuật giữ chế độ riêng/ẩn mặc định như định hướng HUD W3.

## 6. Mẫu kiểm chứng nhỏ: hai map liền kề (ĐỀ XUẤT THỬ NGHIỆM)

Chưa cần sản xuất đủ 133 map để xác minh core loop. Chọn hai map hiện có với độ khó có thể cảm nhận:

1. Map A farm tương đối ổn định, xuất hiện EXP/vật phẩm/trang bị có thể dùng.
2. Người chơi được quyền mở và **chủ động** tới map B khó hơn.
3. Thử thách ở B có nguyên nhân tương đối dễ quan sát, có thể thất bại.
4. Người chơi điều chỉnh trang bị/võ học/hướng đầu tư hoặc quay lại farm A rồi thử tiếp.
5. Sau một phiên chơi, người chơi có thể nói được: **mạnh hơn ở đâu, vì sao vừa thua, tiếp theo muốn làm gì**.

Không áp mốc thời lượng hay tỷ lệ thắng cố định trước khi thử; thu thập quan sát từ người chơi thật. Đây là **kịch bản kiểm nghiệm**, không phải nội dung đã sản xuất.

## 7. Lộ trình ưu tiên, bám trạng thái 25/10/2026

**Gần — dùng những gì đã có:**
- W3 audit HUD → triển khai bố cục đã chốt → hoàn thiện luồng **Inventory → Equipment → Skill**.
- Đảm bảo nhặt/so sánh/trang bị/nhận thay đổi chỉ số và chiến đấu là một chuỗi sử dụng được.
- Nghiệm thu wave theo quãng đường, world-map, entry/camera, UI 600/800/1150 và Windows Player theo từng phần.

**Kế tiếp — kiểm chứng core loop:**
- Thử vòng farm A → thử thách B → thất bại/thành công → điều chỉnh build.
- Cải thiện phản hồi kết quả và lý do thất bại bằng dữ liệu thật.
- Rà soát checkpoint/Save/Load W2.3 **như một công việc riêng trước khi dựa vào tiến trình dài hạn**, tránh mất tiến trình hoặc nhân thưởng lặp.

**Sau đó — mở chiều sâu (CHƯA CHỐT THỰC HIỆN):**
- Nhật ký hành trình, phát hiện thông tin mới về map/thế giới, sự kiện NPC có trạng thái.
- Các diễn biến không nhất thiết mang lại vật phẩm; NPC/thế giới có mục tiêu riêng, không chỉ phục vụ Hero.
- Tiến triển offline hoặc tổng kết khi quay lại **chỉ sau khi định nghĩa rõ luật thời gian, save và giới hạn mô phỏng**.
- Tự động chiến đấu thông minh, võ học chiều sâu, chiến lược dài hạn theo các tài liệu thiết kế riêng.

## 8. Những thứ chưa nên thêm vào core

- Daily checklist, thể lực cản farm, nút nhận thưởng mỗi wave.
- Tự chuyển sang map mới ngay khi đủ điều kiện.
- Nhiều tiền tệ/tài nguyên chỉ để tạo thêm việc bấm.
- Độ khó hoặc lượng map tăng vô hạn mà không tạo thêm lựa chọn có ý nghĩa.
- Sự kiện ngẫu nhiên liên tục chỉ để gây chú ý, không để lại hậu quả/ý nghĩa.

Đây là nguyên tắc chọn lọc phạm vi, không phải lời tuyên bố cấm vĩnh viễn mọi cơ chế tương tự.

## 9. Những câu hỏi còn mở — PHẢI THẢO LUẬN TRƯỚC KHI CODE

- **Vai trò đội hình:** B2/ba Hero đang là hệ thử nghiệm. Core loop tạm ưu tiên làm rõ hành trình một nhân vật chính; chưa chốt hai đồng đội là nhân vật dài hạn, cách lưu tiến trình hay trang bị riêng.
- **Offline:** Có mô phỏng khi đóng game hay chỉ chạy lúc game mở? Quy tắc thời gian, sự kiện và phần thưởng thế nào?
- **Tần suất quyết định:** Bao lâu mới xuất hiện một lựa chọn thực sự đáng cân nhắc, để người chơi không thấy bị ép quản lý?
- **Sự kiện/NPC:** Cách ghi nhận lịch sử, tính nhất quán sau load/rollback, và khả năng thế giới có diễn biến độc lập?
- **Thất bại/checkpoint:** Cần thống nhất với thiết kế W2.3 rồi mới coi là hành vi sản phẩm cuối cùng.
- **Giữ chân dài hạn:** Những cột mốc tu luyện, mối quan hệ và phát hiện nào tạo khác biệt mà không biến game thành chuỗi quest bắt buộc?

## 10. Phân loại trạng thái và nguyên tắc cập nhật

- **Đã có mã** (theo tài liệu/mã snapshot 25/10): khám phá/chiến đấu tự động, wave theo quãng đường, world-map 133 node, hệ chỉ số, inventory/equipment/skill ở mức độ khác nhau, ba Hero thử nghiệm, VFX/camera. Có mã **không đồng nghĩa** đã nghiệm thu toàn bộ Unity/Player.
- **Đã chốt định hướng nền:** idle quan sát, người chơi chọn hướng phát triển, không ép thao tác giữa wave, không tự chuyển map, thua không xóa toàn bộ save, thế giới rộng hơn Hero.
- **Đề xuất đang thử/đánh giá:** bộ ba nhóm quyết định, vòng kiểm nghiệm hai map, phản hồi thất bại, nhật ký hành trình, phát hiện/sự kiện.
- **Chưa chốt/chưa triển khai đầy đủ:** vai trò đội hình dài hạn, W2.3 checkpoint, tiến triển offline và mô phỏng thế giới độc lập.

Khi một ý tưởng thay đổi, cập nhật phần trạng thái/tính ưu tiên trong tài liệu này trước hoặc cùng lúc với đặc tả riêng. **Tài liệu này không trao quyền cho Codex tự triển khai feature.**

### Tài liệu đối chiếu
- [README](../README.md) — trạng thái sản phẩm hiện tại.
- [Tiến trình 25/10/2026](PROGRESS_2026-10-25.md) — W2/W3 và những gì đã nghiệm thu.
- [Quy tắc Codex](../AGENTS.md) và [kiến trúc đã kiểm chứng](../.agents/skills/idle-game-project-rules/references/architecture.md).
