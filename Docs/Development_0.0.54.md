# IdleGame — Khởi động 0.0.54: Inventory / Equipment

**Mốc kế thừa:** 0.0.53 đã được người phát triển nghiệm thu công cụ căn chỉnh Opening UI trong Unity Play Mode ngày 15/10/2026, sau đó nhánh `feature/0.0.53` được fast-forward lên `main` tại commit `aef390677a94a026b77de45e4a137bfddb98e4af` (189 commit so với main cũ, không có xung đột). Nhánh `feature/0.0.54` được tạo từ commit đó. `main` giữ `bundleVersion: 0.0.53`, phiên bản đang phát triển đổi thành `0.0.54`.

## Checkpoint: đã có trong mã trước khi bắt đầu .54

- **Inventory:** cửa sổ kho, ô đồ, bảng chi tiết, so sánh chỉ số trang bị, 8 vị trí trang bị gồm 2 nhẫn độc lập, bảng rèn có modal, trang mặc/tháo và bộ dữ liệu Sprite `InventoryArtTheme`. Kho hiện phân trang, chưa dùng ScrollRect dù có trường texture dành sẵn cho thanh cuộn.
- **Thiết kế UI:** công cụ viền đo kích thước Inventory (màu số đo đỏ) đã có. `UIManager.inventoryUseArtTheme=false` là chế độ sử dụng màu UI mặc định; asset Sprite vẫn lưu để bật lại. Chưa tích hợp công cụ kéo-thả và lưu bố cục trực tiếp trong Play Mode cho Inventory; bộ công cụ đã được nghiệm thu ở Opening là cơ sở tham chiếu.
- **Equipment:** cá thể đồ có mã riêng, phẩm chất Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên, chỉ số chính/phụ, trạng thái dòng `Hidden`/`KnownLocked`/`Active`, nhận diện tiềm năng và trần luyện tự nhiên, Save/Load dữ liệu rèn.
- **Forge thử nghiệm (bộ luật ngày 14/10):** rèn thường +1 lượt mất 1 vật liệu; rèn đặc biệt +1 phẩm mất 10 vật liệu; mỗi lần đủ ngưỡng chỉ tăng một bậc phẩm; ngưỡng thử nghiệm 3/6/12/24/48; dòng phụ chỉ khám phá sau khi tăng phẩm thành công, tụt phẩm chuyển dòng từng biết thành `KnownLocked`. Người phát triển đã kiểm thử một phần và chưa báo lỗi ở các trường hợp được thử.
- **Opening UI 0.0.53:** công cụ Editor căn chỉnh runtime `Tools > Bach Luyen Tien Do > Opening Layout (Play Mode)` cho 13 RectTransform, lưu vào `OpeningArtTheme.asset`, chống reset bố cục và tạo backup trước khi ghi. Người phát triển xác nhận hoạt động tốt và muốn cân nhắc áp dụng phương pháp cho toàn bộ UI.

## Phần chưa chốt / chưa xây dựng

1. **Inventory UI:** hoàn thiện mỹ thuật và bố cục trong cửa sổ 600/800/1150; xem xét tái sử dụng công cụ Play Mode drag+save theo từng vùng, bảo vệ nút bấm và các bố cục responsive. Kho thực tế chưa phải lưới cuộn ScrollRect 50+ ô.
2. **Rèn mạo hiểm:** cơ chế cược có nguy cơ mất vật phẩm vĩnh viễn đã được định hướng, nhưng xác suất thành công, mức mất, nguồn nguyên liệu, cân bằng và giới hạn vượt trần chưa chốt; không được tự thêm RNG hủy đồ vào mã.
3. **Sinh đồ/rơi đồ thực:** bảng loại vật phẩm, ngũ hành, số lượng dòng phụ khi sinh, sức mạnh, tỷ lệ rơi và cách phân rã/chế tác chưa hoàn chỉnh.
4. **Khả năng tương thích:** không được phá schema Save cũ, Modifier/CombatStat, kiểm tra quyền modal của bảng rèn và điều khiển UI của Windows overlay.

## Nguyên tắc phát triển .54

- Chỉ chỉnh mã trên `feature/0.0.54`; `main` là checkpoint 0.0.53.
- Trước khi đổi hành vi Forge/Equipment, thảo luận và chốt chi tiết thiết kế; các tham số 3/6/12/24/48 chỉ là giá trị thử nghiệm.
- Giữ cách căn chỉnh trực quan ở Play Mode là **phương án ưu tiên nghiên cứu** cho UI tiếp theo; không tự sao chép nguyên `OpeningCinematicUI` sang kho vì Inventory thay đổi theo modal/trang và tỉ lệ cửa sổ khác nhau.
- Duy trì test hồi quy tại `Tests~` và Unity Play Mode/Windows trước khi nghiệm thu. Những test chưa được chạy ở môi trường trợ lý không được ghi là đã qua.
- Nếu người phát triển có file `OpeningArtTheme.asset` đã chỉnh trên máy nhưng chưa push được, dữ liệu tọa độ cục bộ đó KHÔNG nằm trong GitHub; cần đồng bộ sau, tránh ghi đè toàn bộ asset mà không đối chiếu diff.

**Tài liệu liên quan:** `Docs/Forge_Test_0.0.52.md`, `Docs/Equipment_Affix_Discovery_0.0.52.md`, `Docs/Equipment_0.0.52_Schema.md`, `Docs/UI_Debug_Workspace_0.0.52.md`, `Docs/Opening_PlayMode_Layout_0.0.53.md`.
