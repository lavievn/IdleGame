# Rèn thử nghiệm — 0.0.52 (nhánh feature/0.0.52-refinement)

## Luồng UI
- Trong Kho đồ, chọn trang bị → **Rèn**. Cửa sổ rèn phủ tối lên cửa sổ kho.
- Nút **Nguyên liệu +100** thêm 100 `material:refinement` vào túi (cộng dồn; tối đa 9999). Dữ liệu Save theo danh sách inventory.
- Nút **Kiếm rèn mẫu** tạo thêm một kiếm thử mới mỗi lần bấm, kể cả khi đã có kiếm cũ. Kiếm cũ và save cũ không bị chỉnh sửa giá trị dòng.
- Ba cặp tăng/giảm: cấp vật phẩm, số lần rèn thường (`refineLevel`), số chặng rèn đặc biệt.
- **Tăng cấp +1**: trừ 1 vật liệu. **Rèn thường +1**: trừ 1 vật liệu. **Rèn đặc biệt +1**: trừ 10 vật liệu. Giảm không hoàn vật liệu.
- Không có rủi ro thất bại, vỡ trang bị, chi phí vàng, hay nghề phụ trong bản thử này.

## Tiến trình phẩm chất (tham số thử nghiệm, KHÔNG phải cân bằng cuối)
- 6 cấp: Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên.
- Kiếm mẫu có tiềm năng ẩn Tiên và trần chặng đầu **Địa**; khác biệt giữa trần chặng và `potentialRarity` là có chủ ý.
- Mỗi chặng có tối đa 10 lần rèn thường; tới 100% thì nâng phẩm tới trần của chặng. Số lần không vượt giới hạn cá nhân `naturalRefineLimit`.
- Rèn đặc biệt luôn hoàn tất trần phẩm chất chặng cũ ngay lập tức và bắt đầu một chặng mới. Trần chặng mới được chọn từ cấp cao hơn (thử: 65% +1 bậc, 35% +2 bậc nếu còn chỗ). Tiến độ ước lượng chặng mới bắt đầu 20–40%, ngẫu nhiên một lần và lưu vào Save.
- Khi đã là Tiên phẩm thì không thể đột phá tiếp.
- Dấu trừ rèn đặc biệt trả về chính xác trạng thái chặng trước; phải đưa số lần rèn thường tại chặng hiện tại về 0 trước. Không hoàn vật liệu và không xóa kiến thức về dòng đã mở.
- `highestRefineLevel`, `hasBeenRefined`, danh sách `forgeHistory`, trần và % được lưu trên cá thể vật phẩm.

## Kiếm và thuộc tính mẫu
- Tên theo phẩm: Mộc Kiếm / Thiết Kiếm / Hàn Ngân Kiếm / Kim Quang Kiếm / Thanh Ngọc Kiếm / Tiên Linh Kiếm.
- 4 dòng phụ thử nghiệm, có sẵn toàn bộ từ thời điểm tạo item (0 dòng bị thêm hoặc random lại khi rèn). Các loại ngẫu nhiên từ ATK, tốc độ đánh, sát thương hệ Kim, sát thương vật lý; mỗi loại xuất hiện tối đa 1 lần trên kiếm mẫu.
- Mỗi dòng có ngưỡng cấp trang bị, cấp rèn và phẩm chất riêng; khi đủ điều kiện, lần nâng thành công mới mở `revealed`. Khi tụt dưới ngưỡng, dòng đã mở chuyển trạng thái KnownLocked, không cộng chỉ số và không biến lại thành ???.
- Sức mạnh thực tế nhân theo cấp/phẩm/số lần rèn (chỉ số gốc đã random giữ nguyên). `EquipmentRuntime` và `EquipmentStatPreview` dùng cùng phép tính.
- Trường `powerBase*` giúp trang bị Save cũ giữ nguyên sức mạnh sau chuyển đổi, không tự nâng hay random lại.

## Cần thử trong Unity
1. Pull nhánh mới, chờ compile, Play → Kho đồ → **Nguyên liệu +100**, thử để ý số lượng trong túi.
2. **Kiếm rèn mẫu** → chọn kiếm → **Rèn**. Tăng cấp/rèn lần lượt: số nguyên liệu trừ 1, dòng ??? mở rồi khi trừ sẽ chuyển **[Khóa]**, không hoàn nguyên liệu.
3. Sau 6 lần rèn thường: tiến độ 60%. **Rèn đặc biệt**: tiêu 10, lên Địa và bắt đầu chặng mới 20–40%; sau đó rèn thường 1 lần, thử không cho trừ rèn đặc biệt tới khi giảm rèn thường về 0.
4. Rèn kiếm khi đang trang bị: so sánh sức mạnh Hero, đảm bảo không cộng dồn modifier.
5. Lưu tay, tải lại và kiểm tra ID, các dòng, đã mở/khóa, phí nguyên liệu, tên phẩm, từng chặng và giới hạn vẫn giữ.
6. Tải Save cũ có kiếm cũ không có dòng phụ: số dòng cũ giữ nguyên, không tự sinh các dòng mới. Muốn thử các dòng ẩn, dùng **Kiếm rèn mẫu** mới.

**Lưu ý:** `Tests~/ForgeRegression.cs` đã viết nhưng chưa chạy compiler Mono hoặc Unity Editor ở môi trường trợ lý. Không merge về main khi chưa Play Mode, thử cả màn hình nhỏ và chạy regression.
