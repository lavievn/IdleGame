# IdleGame — game tu tiên tự động trên desktop

**Phiên bản đang kiểm thử: 0.0.4.54.d_prefinal. Chưa kết thúc tiến trình 0.0.4.54.**

Game nhập vai tu tiên 2D/2.5D chạy trong cửa sổ trong suốt trên Windows. Nhân vật tự di chuyển, tìm đối thủ, chiến đấu và tăng cấp; người chơi theo dõi hành trình, lựa chọn cách đánh và về sau xây dựng nhân vật qua linh căn, trang bị, vũ khí và võ học. Mục tiêu là một game có thể chạy bên cạnh công việc, ít chiếm màn hình và tài nguyên.

Đây là **định hướng sản phẩm**, không phải tuyên bố đã hoàn thiện mọi tính năng hoặc đã tối ưu chạy nền dài giờ.

## Trạng thái bản hiện tại

Bản này nối tiếp toàn bộ 0.0.4.54c. Mã đang ở nhánh `feature/0.0.4.54.d_prefinal`, chưa merge vào `main`. Xem [ghi chú bản sửa](Docs/Release_0.0.4.54.d_prefinal.md) và [tài liệu phát triển](Project_info_dev.txt) để biết chi tiết và lịch sử quyết định.

Ba thay đổi chính:

- **Thu/phóng giữ nguyên tọa độ chiến đấu:** Ground dùng khung logic cố định; chỉ tỷ lệ hiển thị đổi theo cửa sổ. Không sinh lại quái, đặt lại vị trí, đổi tầm đánh hay xóa đạn chỉ vì resize.
- **Cứu wave bị kẹt:** khi một quái sống ra hẳn mép phải, hủy các quái/đạn cũ và sinh lại wave đang dở sau độ trễ bình thường. Giữ số quái ban đầu, cấp/HP/EXP của hero và tiến trình map; quái mới được random lại. Không coi quái thoát là đã giết, không cộng EXP cho thao tác cứu wave. EXP từ các quái thực sự đã giết trong wave vẫn giữ.
- **Thất bại quay về đầu cụm:** map 1–5 → 1; 6–10 → 6; 11–15 → 11. Bắt đầu từ wave 1, giữ nhân vật và chủ đề cụm; tên/địa hình map được chọn lại trong chủ đề đó. Ghi checkpoint vào autosave ngay khi chết. Tổng lượt vào map được lưu riêng để đường tăng sát thương quái không bị tua lùi.

Cứu wave là lớp dự phòng, không thay thế việc sửa nguyên nhân tọa độ. Nếu nó kích hoạt thường xuyên, cần tiếp tục tìm lỗi di chuyển/tìm mục tiêu.

## Các tính năng đã có mã

| Nhóm | Hiện trạng |
| --- | --- |
| Chuyển động | Hero đi trái, quái đi phải; tìm đối thủ gần nhất phù hợp hướng, dừng và đánh khi đủ tầm riêng. Quái tính thêm chuyển động nền. |
| Camera và nền | Camera trôi mặc định khi hero ở trong vùng đỏ, điều chỉnh tại biên; nền theo chuyển động hero; đất–cỏ cuộn lặp, nhiều vật trang trí. |
| Chiến đấu | Cận chiến, cung, phép; cung giảm sát thương ngẫu nhiên và có thể hụt mục tiêu đang di chuyển; phép vận công rồi bắn một đòn nổ vùng, có thanh tiến trình xanh. |
| Linh căn | Ngũ hành, nhất đến ngũ trọng, sát thương hỗn hợp theo tỷ trọng; Vô độc lập và không khắc hệ. Hero/quái random linh căn; xung khắc giữa tộc đang bỏ qua. |
| Định danh | Giới tính, tên ghép cho nhân vật; tên quái theo linh căn/tộc/loài; màu hệ pha vào hình nhân vật/quái. |
| Map và quái | 5 wave/map, cụm 5 map theo chủ đề; tên Hán Việt và địa hình; loài quái theo môi trường. Cứ 5 map thường có 1 map khó với sát thương quái ×2, EXP ×3. |
| Tăng trưởng | EXP tăng theo cấp số nhân, HP/ATK/tốc đánh có mức tăng; chỉ hồi 5–10 HP khi giết quái, không hồi tự động khi đứng yên. |
| Lưu và menu | Tiếp tục/Chơi mới có xác nhận; hai ô lưu tay và autosave; tải/reset/thoát/kích thước cửa sổ; Pause/Resume. |
| Thông tin | Nhật ký, chỉ số hero, lịch sử công thức sát thương và đòn hụt; UI chữ thường, bảng dài phân trang. |
| DEVB | Chỉnh trực tiếp thông số cơ bản và tăng mỗi cấp cho Hero/Quái; tầm riêng từng kiểu đánh; lưu cấu hình riêng, Gốc → OK khôi phục mặc định. |
| Windows overlay | Trong suốt, không viền, kéo từ ground/nút/log/UI, click xuyên vùng trống; các mức 800/500/250, xử lý vị trí sau resize. |

“Đã có mã” không đồng nghĩa mọi trường hợp đã nghiệm thu trên Windows. Đặc biệt, lỗi resize/gameplay của bản cũ do người dùng vừa báo là lý do vẫn cần bản prefinal này.

## Các phần dự tính thực hiện

- **Hình ảnh và hiệu ứng:** thiết kế hero/quái theo tộc/giới tính, sprite và hoạt ảnh hoàn chỉnh, map theo địa hình, vật trang trí, hiệu ứng đòn đánh/linh căn, biểu hiện sát thương, UI nhất quán và âm thanh. Hiện phần lớn hình là placeholder, hiệu ứng đơn giản để kiểm thử.
- **Xây dựng nhân vật:** UI phân bổ điểm, trang bị/kho/rơi đồ, vũ khí, võ học, kỹ năng, phòng thủ/crit, hiệu ứng theo thời gian và khống chế. Một số enum/API đã có nhưng vòng chơi đầy đủ chưa triển khai.
- **Đối thủ và đội hình:** elite/boss, hành vi riêng, đối sách, đồng đội và quản lý nhiều nhân vật.
- **Hành trình tu tiên:** xuất thân/cốt truyện, cảnh giới và đột phá thực sự, huyết thống/sinh sản, Luân Hồi, Lò Bát Quái, tiến trình ngoại tuyến. Các ý tưởng này còn cần chốt đặc tả.
- **Hoàn thiện sản phẩm:** kiểm thử Windows/DPI/nhiều màn hình, an toàn lưu dữ liệu, đo tài nguyên/chạy dài giờ, đóng gói/phát hành. Đồng bộ thời gian thực là ý tưởng chưa chốt phạm vi, chưa có hệ nhiều người chơi.

Ưu tiên trước khi sang 0.0.5: xác nhận resize không phá trận, wave không kẹt, chết/thử lại/lưu đúng checkpoint; sau đó chốt phạm vi vòng trang bị/võ học tối thiểu và kế hoạch thay placeholder.

## Ước lượng tiến độ toàn sản phẩm

Theo yêu cầu hiện tại, **hình ảnh/hoạt ảnh/hiệu ứng và phần trình bày chiếm 60–70% tổng khối lượng**, nhóm còn lại chiếm 30–40%. Mức hoàn thành phần hình ảnh/hiệu ứng **10–15% do người dùng cung cấp**, chưa phải kết quả đo asset hoặc nghiệm thu.

| Nhóm công việc | Trọng số tổng | Hoàn thành trong nhóm | Căn cứ |
| --- | --- | --- | --- |
| Hình ảnh, hoạt ảnh, hiệu ứng và trình bày | 60–70% | 10–15% | Ước lượng của người dùng; hiện còn nhiều placeholder. |
| Gameplay, dữ liệu, UI chức năng, kỹ thuật và kiểm thử | 30–40% | 35–45% | Ước lượng phát triển: đã có vòng chiến đấu cơ bản, còn thiếu nhiều hệ sản phẩm và nghiệm thu môi trường đích. |

Công thức: `tiến độ = trọng số hình ảnh × mức hoàn thành hình ảnh + trọng số phần còn lại × mức hoàn thành phần còn lại`.

- Mốc trung tâm: `65% × 12,5% + 35% × 40% = 22,125%` → **khoảng 22%**.
- Với hai nhóm có trọng số cộng đúng 100%, khoảng ước lượng là **17,5–27%**.

Đây là **ước lượng khối lượng của toàn bộ định hướng**, không phải phần trăm số dòng mã, số tính năng, mức sẵn sàng phát hành hay dự báo thời gian. Phần 35–45% là đánh giá sơ bộ, chưa có danh mục công việc và tiêu chí nghiệm thu đầy đủ. Khi chốt phạm vi 0.0.5 hoặc mở rộng ý tưởng, cần tính lại; không dùng việc hoàn tất bản sửa 0.0.4.54 để gọi toàn game gần hoàn thành.

## Mở dự án và kiểm thử

1. Dùng Unity ghi trong `ProjectSettings/ProjectVersion.txt` (hiện `6000.5.0f1`). Gói ZIP là **source project**, không phải executable Windows.
2. Nên mở project riêng để thử, hoặc sao lưu/commit bản local trước khi đồng bộ. Giữ các tệp `.meta` và chỉnh sửa riêng của scene.
3. Mở scene `Assets/Scenes/MainGame.unity`. Unity Hub quản lý/mở project; tải thay đổi Git vẫn cần Git hoặc GitHub Desktop.
4. Build Windows vào thư mục mới để thử overlay, kéo cửa sổ, taskbar và DPI. Play Mode không xác nhận được hành vi WinAPI.
5. Ưu tiên đổi `800 → 500 → 250 → 800` lúc đang vận công, đạn đang bay, nhiều quái đang áp sát và khi tạm dừng. Khoảng cách/tầm/HP/wave phải giữ đúng; cung vẫn được phép hụt nếu mục tiêu thực sự di chuyển.
6. Thử quái thoát phải: chỉ replay wave đang dở, không chuyển map hoặc cộng EXP giả. Thử chết ở đầu/giữa/cuối cụm, đóng/mở và tải autosave.

## Kiểm chứng và giới hạn

Bản này đã qua **139 tình huống kiểm tra logic** với API Unity mô phỏng, gồm tái hiện lỗi neo tọa độ cũ, resize và đạn, cứu wave, checkpoint và chuyển save. Có 720 lượt mô phỏng cân bằng; kết quả mặc định giữ như .54c. Nhánh Windows được kiểm tra biên dịch với API mô phỏng.

Môi trường xử lý chưa có Unity Editor/Windows native, nên **chưa xác nhận bản build thực tế hết lỗi**. Kết quả logic không thay thế thử trên máy người dùng, hình ảnh render, profiling hoặc nghiệm thu dài giờ. Xem `Tests~/test-results.txt`.

README này thay cho `WINDOWS_OVERLAY_FIX_0.0.4.51f.txt`. Hướng dẫn vá 0.0.4.51f cũ là lịch sử trong Git; không dùng các kích thước/menu cũ làm hành vi hiện tại. Tài liệu bàn giao đầy đủ vẫn ở [Project_info_dev.txt](Project_info_dev.txt).
