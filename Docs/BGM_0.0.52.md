# BGM 0.0.52 — Quy tắc nhạc nền và kiểm thử (14/10/2026)

## Dữ liệu nguồn và trạng thái

- ZIP cập nhật `BGM(1).zip` đã được mở và kiểm tra giải mã: **19 bản nhạc map + 1 bản opening**. Cả 20 tệp có đuôi `.m4a` nhưng mã hóa audio là **Opus trong MP4**; để Unity nhập đáng tin cậy hơn đã chuẩn bị bản chuyển sang **OGG/Vorbis**, giữ nguyên thời lượng/tên nền.
- Nhóm **BGM1, BGM2, BGM3, BGM5** có 4 bài (`BGMn.ogg`, `BGMn (1).ogg`, `(2)`, `(3)`). **BGM4 chỉ có 3 bài** (bản nền, (1), (2)); thiếu 1 bài theo mục tiêu ≥4.
- `opening.ogg` là nhạc màn hình đầu (Chơi mới / Tiếp tục và thông báo xác nhận); **chưa làm cảnh opening mới**.
- **Lưu ý phân phối:** các tệp nhị phân OGG trong ZIP người dùng gửi không thể chuyển trực tiếp qua kết nối GitHub hiện tại. Mã tích hợp đã được commit ở nhánh riêng, **chưa có tệp nhạc trong GitHub** và chưa thể xác nhận âm thanh thực trong Unity. Cần chép bộ `Assets/BGM` vào dự án và đưa lên GitHub bằng máy người phát triển (hoặc phương thức đẩy file binary tương đương) trước khi tuyên bố hoàn thành và merge.

## Ánh xạ Suno và vùng 5 map

Thứ tự nhóm theo cuộc trao đổi `Tạo prompt nhạc Suno`:

| Nhạc | Địa hình cảm hứng | Vùng 5 map đang dùng cho phát nhạc |
| --- | --- | --- |
| BGM1 | Đồng bằng | Bình Nguyên |
| BGM2 | Cao nguyên | Cao Sơn (ghép tạm theo cảm hứng cao nguyên) |
| BGM3 | Đầm lầy | U Trạch |
| BGM4 | Rừng rậm | Sơn Lâm |
| BGM5 | Sa mạc | Hoang Mạc |
| BGM6–8 | Đồi núi, Hồ nước, Biển | Chờ sản xuất và thiết kế mapping đầy đủ |

**Chú ý cấu trúc thật của game:** `TerrainType` có tám địa hình và có thể đổi theo từng map nhỏ, còn `RegionTheme` có sáu chủ đề giữ ổn định cả cụm **5 map**. Để đáp ứng quy tắc “nhạc cùng chủ đề suốt 5 map” mà không sửa tiến trình gameplay, bộ phát nhạc tạm khóa playlist theo `RegionTheme`; không đọc `mapTerrain` trực tiếp cho mỗi map. Vùng **Hải Vực** không có nhạc tới khi bổ sung nhóm tương ứng, tránh phát sai chủ đề. Khi thêm nhạc cho Đồi núi/Hồ nước/Biển nên bàn lại phân chia vùng/địa hình nếu muốn nhạc chuẩn từng môi trường hơn.

## Triển khai trong mã

- `Assets/Script/MapBgmController.cs`: cài tự động trong `GameManager.Start`, tự tìm trạng thái màn hình đầu/gameplay, hai `AudioSource` luân phiên chuyển âm **10 giây**, ngẫu nhiên trong nhóm và không phát lại ngay bài cũ nếu có bài khác.
- Chuyển từ opening sang nhạc chủ đề khi nhân vật bắt đầu chơi; đổi nhạc khi **chuyển chủ đề 5 map**, không đổi vì wave/map nhỏ; bài kết thúc tự chọn bài tiếp theo trong nhóm với 10 giây chồng âm.
- `Assets/Script/MapBgmCatalog.cs`: định nghĩa catalog âm thanh để Unity đóng gói thật vào bản build.
- `Assets/Editor/MapBgmCatalogBuilder.cs`: tự quét `Assets/BGM/*.ogg`, gán vào `Assets/Resources/MapBgmCatalog.asset` khi Unity Editor nhập asset. Có menu **Tools → IdleGame → Rebuild BGM Catalog** để làm lại thủ công nếu cần. Không nhân đôi các tệp nhạc. Mã bỏ qua .m4a để tránh codec/khả năng nhập không đồng đều.
- Các nhóm chưa có tệp sẽ im lặng, không làm hỏng gameplay, không phát nhầm nhạc nhóm khác.

## Kiểm thử trước khi phát hành/merge

1. Giải nén **bộ OGG đã chuyển đổi** vào thư mục dự án `Assets/BGM`; xóa các bản âm thanh lỗi cùng tên trong thư mục này trước khi chép. Chú ý không giải nén chồng `Assets/Assets/BGM`.
2. Mở Unity để nhập asset, kiểm tra Console báo `[BGM] Audio catalog rebuilt` với `4/4/4/3/4 tracks; opening=True`. Kiểm tra tệp `Assets/Resources/MapBgmCatalog.asset` được tạo.
3. Play Mode: màn hình Chơi mới / Tiếp tục có opening; khi bấm chọn và xác nhận, chuyển sang nhóm nhạc của vùng bằng chuyển âm 10 giây.
4. Theo dõi đến gần cuối bài: chọn bài khác cùng nhóm, chồng âm trong 10 giây; không phát trùng ngay nếu có ≥2 bài.
5. Sang map 2–5 trong cùng vùng: không đổi playlist; sang map 6 chọn vùng mới: chuyển nhóm theo vùng, không dừng hoặc phát lại nhạc ngẫu nhiên mỗi wave.
6. Thử vùng Hải Vực chưa có nhạc; không báo lỗi NullReferenceException, có thể chuyển từ nhạc cũ sang im lặng sau 10 giây.
7. Kiểm tra Play Mode, build Windows, thoát/retry, load Save, pause, tab không có nhạc rò rỉ/âm lượng tăng bất thường.
8. Bổ sung **BGM4 thứ tư** trong lần cập nhật sau, không cần sửa mã điều khiển.

**Nghiệm thu:** kiểm tra codec/ZIP từ máy trợ lý **không** thay thế kiểm thử Unity hoặc xác nhận rằng file audio đã lên GitHub. Không sửa giao diện opening hoặc các hệ thống võ học/trang bị trong thay đổi này.
