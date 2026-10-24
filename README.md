# Bách Luyện Tiên Đồ

Dự án game tu tiên 2D idle, phát triển bằng Unity. Nhân vật tự khám phá, chiến đấu và tiến triển trong một thế giới rộng lớn; trọng tâm là quan sát hành trình, không ép tương tác liên tục.

> **Trạng thái (25/10/2026): ĐANG PHÁT TRIỂN / KIỂM THỬ NỘI BỘ — CHƯA PHẢI BẢN ALPHA.** Những mốc có chữ “Alpha Accepted” trong trao đổi trước chỉ biểu thị **nghiệm thu tạm thời một hạng mục**, không phải cấp độ phát hành của toàn bộ trò chơi.

## Nhánh và mốc an toàn
- `feature/0.0.54`: mốc ổn định trước giai đoạn hoàn thiện hình ảnh, không tự ý merge.
- `feature/w2-combat-presentation`: nhánh đang phát triển, tích hợp asset, chiến đấu và môi trường.
- `main`: không tự động đồng bộ từ các nhánh thử nghiệm.

## Trạng thái hệ thống (tổng kết 24–25/10/2026)
| Phần | Hiện trạng |
| --- | --- |
| Vòng lặp chiến đấu | Có Hero/Monster, ba cách tấn công, projectile, wave theo quãng đường; tiếp tục kiểm thử tương tác thực tế |
| Camera & spawn | Multi-Zone camera; spawn VFX; giới hạn bước di chuyển để giảm giật khi frame dài |
| Nhân vật | Sprite/animation Archer, ba Hero thử nghiệm; B2, chuyển làn Y cho đánh xa; chưa hoàn thiện toàn bộ animation nhân vật |
| Âm thanh | BGM và SFX tách âm lượng ở Home/Gameplay; ba nhóm SFX Sword/Shoot/Magic |
| Môi trường | Chín bộ ảnh bốn layer: Plains, Forest, Mountain, Desert, Swamp, Snow, Steppe, Lake, Sea; mapping runtime có cho các loại map và một số map đặc thù theo ID |
| Bản đồ thế giới | Catalog 133 map, điều hướng/zoom có triển khai; cần tiếp tục nghiệm thu chức năng/UI |
| Home | Đã đạt mức chấp nhận nội bộ về bố cục; còn khả năng tinh chỉnh thẩm mỹ |
| UI gameplay | **Chưa hoàn thiện**; có bố cục định hướng mới, chưa triển khai HUD mới |
| Inventory / Equipment / Skill | Đã có logic và UI một phần; cần audit, hoàn thiện luồng tương tác để chơi được |
| Save/Load | Đã qua nhiều đợt rà soát; chưa chốt lại chính sách checkpoint mới; tránh thay đổi ngoài phạm vi |
| Độ hoàn thiện sản phẩm | **Prototype / development build**, chưa đủ điều kiện gọi là Alpha hay bản chơi hoàn chỉnh |

### Mapping art
- Đồng bằng → Plains; Rừng rậm → Forest; Đồi núi/Cao nguyên → Mountain.
- Sa mạc → Desert; Đầm lầy → Swamp; Hồ nước → Lake; Biển → Sea.
- Thảo nguyên `TL-25` → Steppe (giữ loại gameplay Đồng bằng).
- Các map lạnh `TL-01`, `TL-03`, `TL-04` → Snow (giữ loại gameplay Đồi núi).
- Artwork tham khảo/tác phẩm nguồn ở `Assets/StreamingAssets/ArtSource/Plains/`, asset runtime ở `Assets/Resources/Environment/`.

**Giới hạn thẩm mỹ:** hiện chỉ bộ Đồng bằng đã được người phát triển tự xử lý hai mép để đạt seamless theo yêu cầu. Các bộ khác còn lộ đường nối khi lặp, sẽ chỉnh thủ công sau. Có thể cần kiểm tra alpha, seam và tương quan điểm chân trên Unity/Windows.

## Bố cục gameplay HUD đã thống nhất định hướng (chưa code)
- Ba thẻ Hero góc **trên phải**: avatar, tên, cấp độ, HP và Mana.
- **Bên trái**: tên map theo chiều dọc và wave hiện tại/tổng wave; không hiển thị loại địa hình.
- **Mép phải**: khu vực hệ thống Nhân vật, Hòm đồ, Võ học, Bản đồ, Menu.
- **Giữa phía dưới**: vùng ưu tiên trình diễn nhân vật/quái/animation chiến đấu, không phải giới hạn tọa độ bắt buộc.
- Hai góc dưới: dự phòng giao diện riêng.
- Thanh log trên cùng **ẩn theo mặc định**, chỉ bật khi người chơi yêu cầu; tách log ngắn gọn và chế độ debug kỹ thuật.
- Giữ cơ chế chỉnh UI trong Play Mode; kiểm tra kích cỡ cửa sổ 600/800/1150.
- Chưa thay đổi camera hoặc di chuyển để ép Hero vào vùng trình diễn.

## Ưu tiên tiếp theo (W3)
1. Audit HUD/UI hiện có, các panel, hệ thống mở/đóng và chồng lớp; lập bản đồ phụ thuộc trước khi sửa.
2. Dựng bố cục HUD mới và ba thẻ Hero, tên map/wave, toolbar hệ thống, log ẩn mặc định.
3. Audit và hoàn thiện Inventory → Equipment → Skill, giúp người chơi nhặt/so sánh/trang bị và thấy tiến triển thực.
4. Kiểm thử Unity Edit Mode/Play Mode và Windows Player, hiệu năng, scale UI, lưu thiết lập.

## Nguyên tắc đóng góp
Không coi Mono/stub PASS là Unity native PASS. Không khẳng định tính năng hoàn thiện khi chưa nghiệm thu trong Unity; ưu tiên sửa từng phần, commit riêng và không merge lên main hoặc nhánh ổn định khi chưa được yêu cầu.

Xem [tổng kết tiến trình](Docs/PROGRESS_2026-10-25.md) để biết mốc đã thực hiện, giới hạn và việc còn lại.
