# Bách Luyện Tiên Đồ — Development 0.1.6

## Weapon-Granted Skills & Combat Resources

Bắt đầu từ `82bb07672fdd21174d4b640a075cd465abfb4d5f` trên `development_0.1.5`. Đối chiếu báo cáo 0.1.5, mẫu và instance trang bị, Forge, vòng đánh tự động, phòng ngự, đạn và nổ phép, UI cùng giao dịch Save/Thiên Quan. Nhịp vận chiêu cũ của Staff/B2 không trừ tài nguyên. Linh lực đã nằm trong dữ liệu tu luyện và có hồi phục riêng. Mana chiến đấu dùng ô HUD và stat hồi Mana có sẵn.

### Chơi thử

Mở `D:/IdleGame/Builds/Development016Playable/build/IdleGameV10.exe`. Vào **Võ học → Nhận 3 vũ khí mẫu DEV**, rồi trang bị một món trong **Hòm đồ**. Mở lại **Võ học**, bấm **Thêm vào hàng** và đóng bảng để Hero tiếp tục. Dùng **Trước/Sau** chọn dòng, **Lên/Xuống** đổi thứ tự và **Bỏ** xóa dòng. Dòng thiếu nguồn hiện **Không khả dụng** và tự dùng lại khi trang bị đúng nguồn.

| Vũ khí Hoàng (Uncommon) | Kỹ năng | Vận / hồi | Mana | Tác dụng |
| --- | --- | --- | ---: | --- |
| Kiếm Đâm | Đâm kiếm | 0,5 s / 5 s | 0 | Vật lý một lần trên mỗi mục tiêu trong hình chữ nhật phía trước. |
| Cung Liên Hoàn | Liên Hoàn Tiễn | 0,5 s / 5 s | 0 | Ba mũi liên tiếp chọn đích ngẫu nhiên phía trước, có thể trùng. Mũi tên bám định danh đích, đổi đích khi cần và dừng nếu hết mục tiêu hợp lệ. |
| Trượng Hỏa Cầu | Hỏa Cầu Thuật | 1 s / 5 s | 5 | Dùng đường nổ phép cũ với bán kính 250 đơn vị BattleArea. Đòn thường của Staff dùng luồng sáng cong, bám một đích và không tốn Mana. |

Ba mẫu có chỉ số chính +10 và cấp kỹ năng bằng ID trên template. Dùng bậc **Hoàng** hiện có, cấp mẫu từ nút DEV để thử. Dòng phụ Common cũ được giữ trong Save cho Forge và không cộng bonus khi còn Common. Vùng Đâm kiếm 100×35 px được quy đổi một lần ở mốc thiết kế 800 px với BattleArea rộng 1920, thành 240×84 đơn vị gameplay. Các kỹ năng dùng pipeline sát thương và phòng ngự hiện hành.

Scheduler bắt đầu từ cursor đã lưu, kiểm tra nguồn, vũ khí, hồi chiêu, Mana và đích rồi lấy kỹ năng hợp lệ đầu tiên. Nếu bỏ qua hết hàng, Hero đánh thường. Khi đòn được thi triển, game mới trừ Mana, bắt đầu hồi chiêu và tiến cursor. Khoản hồi chiêu theo ID kỹ năng, giữ qua tháo/lắp và Save/Load. Nguồn **Đã học** và **Trang bị** tách riêng, batch này có ba kỹ năng từ vũ khí.

Mana tối đa và ban đầu 100, hồi 2/s. HP tự hồi 10/s trong lúc khám phá, hồi tự nhiên bằng 0 khi đang đánh. Snapshot lưu HP, Mana, hàng, cursor và hồi chiêu còn lại. Save thiếu thành phần mới dùng mặc định 100 Mana, HP theo cách mở Save cũ và hàng rỗng. Load/khởi động lại giữ tài nguyên từ snapshot, không tính thời gian đã đóng game. **Retry** chủ động hồi sinh HP và giữ Mana/hồi chiêu. Quyết toán Thiên Quan dùng lần hồi sinh có sẵn.

### Kiểm tra

| Lượt | Kết quả |
| --- | --- |
| Unity 6000.5.0f1 và Windows Development Mono2x | Build thành công, 0 lỗi và 58 cảnh báo. |
| MainGame Play Mode | Đâm kiếm đúng vùng, ba mũi Cung, Staff một đích, Hỏa Cầu 5 Mana, queue và đổi vũ khí, hồi tài nguyên, Save/Load/Retry, Thiên Quan sang Trúc Cơ. |
| Windows Development Player | Qua cùng smoke test, gồm bám đích di chuyển, khóa hồi tài nguyên trong menu và tải snapshot cũ thiếu trường combatSkills. |
| Tiến trình Windows mới | Khôi phục đúng HP/Mana chưa đầy, hàng, cursor và hồi chiêu của ô thử nghiệm từ tiến trình trước. |

Mono/UnityStub qua 37 assertion Thiên Quan. Kiểm tra metadata Unity có 0 lỗi và 0 cảnh báo. Kiểm tra nút theo vùng bấm và handler, chưa kiểm tra chuột/phím native. Các trận smoke dùng Save có dấu sở hữu và đặt quái theo tình huống. Cảnh báo VideoPlayer lúc rời màn mở đầu và timestamp H.264 đã có từ 0.1.5.

- Mã `e1de41c34836d4d12337fb40a820ce73bc86aab8`. Kiểm thử và HEAD dùng để build `298235b248ee2b7129e2620243bb88b879c4ba4c`.
- Git tại bàn giao `development_0.1.5`, đi trước origin 4 commit gồm hotfix hình ảnh và ba commit 0.1.6. Giữ các thay đổi sẵn có của Build Profile, font và metadata địa hình. SHA commit tài liệu và đường dẫn log tại `D:/IdleGame/Builds/Development016Playable/handoff.json`.
