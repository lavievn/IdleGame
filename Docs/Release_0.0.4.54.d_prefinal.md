# 0.0.4.54.d_prefinal — tọa độ resize, cứu wave và checkpoint

Nền: toàn bộ 0.0.4.54c, cây mã GitHub `2cdac65a15f0e31592d0ec5d090d12c19076ddd2`. Chưa final, chưa merge main.

## Nguyên nhân có bằng chứng trong mã

Ground trong MainGame neo giãn ngang `(0,0) → (1,0)`, hero neo phải `(1,0)`, quái neo trái `(0,0)`. Canvas cũ Scale With Screen Size, độ phân giải tham chiếu 1920×1080, Match 0.5. Khi kích thước logic của Ground đổi, hai gốc neo dịch khác nhau; tầm đánh còn tính theo Width. Đạn giữ start/end trong tọa độ Ground nên điểm rơi không đi theo dịch neo riêng của mục tiêu. Thay đổi tỷ lệ khung hình/rounding có thể làm lộ lỗi; không có bằng chứng rằng resize thực sự reload scene hay spawn lại quái.

AI chỉ tiếp cận một chiều, bỏ qua mục tiêu phía sau ngoài tầm. GameManager chỉ gọi CompleteWave khi tất cả đối tượng trong activeMonsters bị hạ. Quái bị bỏ lại, trôi khỏi mép phải vẫn còn trong danh sách nên chặn wave mới. Các mắt xích này đã đối chiếu mã; chưa có trace Unity thực tế để kết luận đây là nguyên nhân duy nhất.

## Sửa không gian chiến đấu

EnvironmentManager chạy ở thứ tự -200, tạo Stable Battle Canvas trước khi triển khai hero. Ground được chuyển vào canvas độc lập và giữ chiều rộng logic theo referenceResolution.x (MainGame: 1920), chiều cao logic cũ. Nếu không có CanvasScaler thích ứng thì lấy chiều rộng hiện có; fallback 1920 khi chưa có kích thước hợp lệ.

Ground neo giữa dưới với kích thước cố định. Canvas dùng Screen Space Overlay, scaleFactor = Screen.width / logicalWidth; Unity điều khiển rect Canvas. Update chỉ cập nhật phép chiếu khi chiều rộng màn hình thay đổi, kể cả khi pause. Không chỉnh actor, không sinh lại quái, không xóa đạn/hồi chiêu, không đổi tầm hay random lại trang trí khi resize. Tỷ lệ hiển thị đổi đồng nhất, tọa độ local của hero/quái/đạn và vị trí mục tiêu giữ nguyên.

Tài liệu API chính thức đã đối chiếu:
- https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/canvas/scalefactor
- https://docs.unity.cn/Packages/com.unity.ugui%401.0/api/UnityEngine.UI.CanvasScaler.html

Bố cục HUD vẫn theo điểm ảnh như .54c. Cơ chế giữ vị trí native window trong .54c giữ nguyên; đây là sửa tọa độ chiến đấu, không phải thay đổi thêm thuật toán WinAPI.

## Cứu wave

Sau bước camera, GameManager kiểm tra wave đang chạy, hero sống/đã triển khai, không pause và không có coroutine chờ. Khi mép trái toàn thân quái đã qua mép phải Ground cộng 32 đơn vị, hủy cả wave còn lại và đạn cũ qua CombatManager, đưa hero về HomeX, sinh lại cùng số quái sau độ trễ wave hiện hành. Quái mới random lại loài/hệ/chỉ số theo logic thường.

Giữ HP hiện tại, EXP/level/điểm, map và completedWavesInMap. EXP/hồi máu từ quái đã hạ trước đó giữ; thao tác reset không thưởng EXP hoặc hồi máu. Không gọi OnMonsterDied/CompleteWave cho quái thoát. Ngắm và chu kỳ tấn công được reset để không dùng đối tượng/đạn thuộc lượt cũ. Chỉ có một coroutine chờ; chết/load/reset hủy retry và số lượng retry đang giữ.

Không kích hoạt với quái mới sinh ngoài mép trái hoặc còn một phần thân ở vùng biên. Nếu cứu wave lặp nhiều lần, phải điều tra tiếp AI/motion; không che lỗi bằng cách coi quái thoát là đã chết.

## Chết và thử lại

Ngay khi chết: mapNumber = floor((mapNumber−1)/5)×5+1; completedWavesInMap = 0. Giữ theme/index của cụm, tạo lại tên/địa hình trong theme đó, cập nhật khó/thường theo vị trí map. Giữ nhân vật/linh căn/cấp/EXP/điểm. Ghi checkpoint vào autosave trước khi hiển thị Game Over, để thoát game rồi Tiếp tục cũng dùng checkpoint. Retry triển khai lại nhân vật đầy HP theo luồng cũ. Callback chết trùng không ghi thêm lượt.

Tách mapVisits (tổng lượt vào map) khỏi mapNumber (vị trí/checkpoint). Map bình thường mới hoặc quay về đầu cụm sau chết tăng mapVisits một lần; cứu wave không tăng. Sát thương quái dùng mapVisits, vẫn 50% lúc đầu, +10 điểm phần trăm mỗi 20 lượt đến tối đa 100%. Save cũ thiếu trường này lấy số map cũ làm giá trị ban đầu; giá trị template được xóa trước load. Không phục hồi thứ tự/tên chính xác của 5 map đã random trước đó vì bản cũ chưa lưu lịch sử cụm.

## Tài liệu và tiến độ

Đổi WINDOWS_OVERLAY_FIX_0.0.4.51f.txt thành README.MD. README tóm tắt tính năng đã có, thiếu, hướng tu tiên/overlay, cách thử và ước lượng tổng tiến độ. Theo trọng số người dùng: hình ảnh 60–70%, hoàn thành nhóm này 10–15%; nhóm còn lại ước lượng 35–45%. Tổng trung tâm khoảng 22%, khoảng 17,5–27%. Đây là đánh giá khối lượng, chưa có tiêu chí nghiệm thu để đo chính xác.

## Kiểm tra

139 regression đạt với API Unity mô phỏng; 10 tình huống mới gồm reproducer anchor, tọa độ/range/target/trang trí sau resize, cung/phép đang bay, pause, quái thoát, biên/mép trái, hủy retry, checkpoint/đếm lượt/save cũ. 720 mô phỏng mặc định giữ kết quả .54c. Nhánh UNITY_STANDALONE_WIN biên dịch được với API mô phỏng.

Chưa có Unity Editor hoặc Windows native ở môi trường xử lý. ZIP là toàn bộ source, chưa có EXE mới. Cần thử thật:
1. 800→500→250→800 liên tục với cung/phép đang bay, vận công dở và 3 quái. Không có lệch tương đối, quái đứng yên không hụt chỉ vì resize, không đổi wave/HP.
2. Resize lúc pause và thay đổi tỷ lệ khung Game View. Ground vẫn trải ngang; HUD vẫn đọc được; không xáo lại cỏ.
3. Quái sống thoát hẳn phải: log replay, không tăng wave/map, không spawn kép, không hồi HP; wave mới đánh và hoàn tất bình thường. Thử chết/load/reset trong khoảng chờ.
4. Chết ở map 1/5/6/8/10/11: về 1/1/6/6/6/11, wave 1; Retry và tải autosave sau restart đều đúng.
5. Native: DPI, taskbar, kéo từ UI, DEVB nhập số; bản sửa này không thay thế nghiệm thu cửa sổ của .54c.
