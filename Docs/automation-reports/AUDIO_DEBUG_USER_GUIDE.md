# Environment Audio Debug

Trong gameplay, chọn **Chẩn đoán · F8** hoặc nhấn F8, rồi chọn **Environment Audio**. Danh sách hiện các nguồn thật, tên OGG, family, variant, loop/one-shot, trạng thái, gain và âm lượng sau fade. Số nguồn gồm nguồn đang rảnh trong pool. Danh sách lớp cho biết đang phát, chờ nguồn, chờ sự kiện hoặc bị loại theo điều kiện. Bộ đếm Morning, Meadow, Coast Event và Thunder dùng thời gian scheduler. Clip hết điều kiện hoặc đang nhường nguồn vẫn hiện trong lúc fade.

- Chọn family hoặc clip bằng nút mũi tên. Mute, Solo và slider thay đổi mixer thử trong bộ nhớ. Bộ đếm và lượt xoay clip tiếp tục. Có thể bỏ Solo và Mute bằng chính nút tương ứng.
- **Audition** nghe riêng file đã chọn qua một nguồn tái sử dụng. **Stop clip** dừng nghe thử. **Auto** bỏ override Day/Night và audio weather.
- Day/Night, Dry/Light/Heavy/Storm ở đây chỉ thử âm thanh. Nút khí hậu HUD cũ vẫn dùng để xem cả hình ảnh. Preview tiếp tục khi đóng bảng trong gameplay và dừng khi hệ ambience dừng. Save chỉ lưu trạng thái climate thật.
- **Export JSON** viết file riêng vào `Application.persistentDataPath/Diagnostics/RC4-environment-audio.json`. Đường dẫn thực hiện sau khi lưu. Giữ bản sao ngoài trước khi xuất một preset khác. **Import JSON** đọc lại file này và từ chối ID không thuộc manifest hoặc gain ngoài 0–1. **Reset** bỏ toàn bộ thay đổi thử.
- Tab **Combat & Progression** chỉ đọc dữ liệu thật. Bảng và dữ liệu debug được loại khi compile Release.
