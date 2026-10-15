# IdleGame — Khởi động 0.0.54: Inventory / Equipment

**Mốc kế thừa:** 0.0.53 đã được người phát triển nghiệm thu công cụ căn chỉnh Opening UI trong Unity Play Mode ngày 15/10/2026, sau đó nhánh `feature/0.0.53` được fast-forward lên `main` tại commit `aef390677a94a026b77de45e4a137bfddb98e4af` (189 commit so với main cũ, không có xung đột). Nhánh `feature/0.0.54` được tạo từ commit đó. `main` giữ `bundleVersion: 0.0.53`, phiên bản đang phát triển đổi thành `0.0.54`.

## Checkpoint: đã có trong mã trước khi bắt đầu .54

- **Inventory:** cửa sổ kho, ô đồ, bảng chi tiết, so sánh chỉ số trang bị, 8 vị trí trang bị gồm 2 nhẫn độc lập, bảng rèn có modal, trang mặc/tháo và bộ dữ liệu Sprite `InventoryArtTheme`. Kho hiện phân trang, chưa dùng ScrollRect dù có trường texture dành sẵn cho thanh cuộn.
- **Thiết kế UI:** công cụ viền đo kích thước Inventory (màu số đo đỏ) đã có. `UIManager.inventoryUseArtTheme=false` là chế độ sử dụng màu UI mặc định; asset Sprite vẫn lưu để bật lại. Chưa tích hợp công cụ kéo-thả và lưu bố cục trực tiếp trong Play Mode cho Inventory; bộ công cụ đã được nghiệm thu ở Opening là cơ sở tham chiếu.
- **Equipment:** cá thể đồ có mã riêng, phẩm chất Phổ thông → Hoàng → Huyền → Địa → Thiên → Tiên, chỉ số chính/phụ, trạng thái dòng `Hidden`/`KnownLocked`/`Active`, nhận diện tiềm năng và trần luyện tự nhiên, Save/Load dữ liệu rèn.
- **Forge thử nghiệm (bộ luật ngày 14/10):** rèn thường +1 lượt mất 1 vật liệu; rèn đặc biệt +1 phẩm mất 10 vật liệu; mỗi lần đủ ngưỡng chỉ tăng một bậc phẩm; ngưỡng thử nghiệm 3/6/12/24/48; dòng phụ chỉ khám phá sau khi tăng phẩm thành công, tụt phẩm chuyển dòng từng biết thành `KnownLocked`. Người phát triển đã kiểm thử một phần và chưa báo lỗi ở các trường hợp được thử.
- **Opening UI 0.0.53:** công cụ Editor căn chỉnh runtime `Tools > Bach Luyen Tien Do > Opening Layout (Play Mode)` cho 13 RectTransform, lưu vào `OpeningArtTheme.asset`, chống reset bố cục và tạo backup trước khi ghi. Người phát triển xác nhận hoạt động tốt và muốn cân nhắc áp dụng phương pháp cho toàn bộ UI.

## Trạng thái UI — đang triển khai, tạm chưa phải đầu việc chính (.54)

- **Inventory / Information / các giao diện cùng nhóm:** bộ asset thiết kế **chưa hoàn thiện**, hiện đang được chuẩn bị. Giữ nguyên layout code, giao diện placeholder và các công cụ đo hiện có; không xem UI art là đã nghiệm thu.
- **Căn chỉnh Play Mode:** phương pháp đã nghiệm thu với Opening ở 0.0.53 và dự kiến mở rộng cho các UI khác, nhưng **hoãn việc chuyển Inventory sang công cụ chỉnh/lưu mới** cho tới khi có bố cục/asset hoàn chỉnh để tránh chỉnh lặp lại.
- **Trạng thái công việc:** ĐANG TRIỂN KHAI / CHỜ ASSET. Không khóa luồng phát triển gameplay, Equipment hay dữ liệu.
- **Mốc quay lại:** khi có khung ảnh, nút và asset thực tế cho Inventory, Information; lúc đó thử trực tiếp trong Play Mode ở ba kích thước cửa sổ trước khi chốt.

## Phần chưa chốt / chưa xây dựng

1. **Inventory UI [đang triển khai / chờ asset]:** hoàn thiện mỹ thuật và bố cục trong cửa sổ 600/800/1150; sau đó xem xét tái sử dụng công cụ Play Mode drag+save theo từng vùng, bảo vệ nút bấm và bố cục responsive. Kho thực tế chưa phải lưới cuộn ScrollRect 50+ ô.
2. **Rèn mạo hiểm:** cơ chế cược có nguy cơ mất vật phẩm vĩnh viễn đã được định hướng, nhưng xác suất thành công, mức mất, nguồn nguyên liệu, cân bằng và giới hạn vượt trần chưa chốt; không được tự thêm RNG hủy đồ vào mã.
3. **Sinh đồ/rơi đồ thực:** bảng loại vật phẩm, ngũ hành, số lượng dòng phụ khi sinh, sức mạnh, tỷ lệ rơi và cách phân rã/chế tác chưa hoàn chỉnh.
4. **Khả năng tương thích:** không được phá schema Save cũ, Modifier/CombatStat, kiểm tra quyền modal của bảng rèn và điều khiển UI của Windows overlay.


## Đầu việc tiếp theo sau khi tạm hoãn UI asset

**Ưu tiên tiếp theo: Equipment và cách cộng chỉ số theo loại sát thương.** Rà soát phân biệt nguồn sát thương vật lý/pháp thuật, giá trị chỉ số gốc và cộng từ trang bị, preview mặc/tháo và Save/Load, rồi thảo luận quy tắc trước khi sửa gameplay. Đây là nền tảng cần ổn định trước khi mở rộng sinh đồ, rơi đồ và võ học.

**Chưa tự triển khai** xác suất thất bại, mất vật phẩm, bảng drop hay random chỉ số mới. Các phần đó còn thiếu quy tắc đã thống nhất; Forge thử nghiệm hiện tại được giữ nguyên.

## 15/10/2026 — Chốt khung chỉ số chung và bốn hướng tu luyện

- Đã thống nhất **phương án B**: đầu tư điểm theo bốn hướng **Công phạt, Hộ thể, Thân pháp, Linh tu**; mỗi điểm cộng nhiều chỉ số trong cùng hướng. Tăng trưởng tự nhiên theo cấp/cảnh giới và điểm tu luyện là hai nguồn riêng.
- UI chính chỉ thể hiện bốn tổng hướng; **hover/chọn vào đúng hướng/chỉ số** mới hiển thị các chỉ số đóng góp và giải thích chi tiết. Thiết kế art của Inventory/Information vẫn đang chờ, **chưa triển khai giao diện mới**.
- **Chính xác thuộc Công phạt**, cơ bản **80%**; khi đánh kẻ cao/thấp cấp hơn thì điều chỉnh **-2/+2 điểm phần trăm mỗi cấp**, giới hạn chênh lệch trong ±5 cấp (tối đa ±10 điểm phần trăm). Tổng Chính xác tối đa **100%**, Né tránh tối đa **45%** (mốc tạm); xác suất trúng = clamp(chính xác hiệu lực − né tránh hiệu lực, 0–100%), chỉ xét một lần. Hệ số Chính xác tăng theo mỗi điểm Công phạt **chưa chốt**.
- Chỉ số hồi phục có giới hạn nhưng **chưa chốt con số**. Tạo một chỉ số chung giảm thời gian **debuff** cho xuất huyết/choáng/làm chậm, chưa chốt mức trần.
- **Kim/Mộc/Thủy/Hỏa/Thổ và linh căn bẩm sinh** giữ nguyên khi sinh ra; không mở nhánh phân điểm hệ, không thêm Chaos. Các hiệu ứng/võ học/trang bị có thể tác động chỉ số hiệu lực, không sửa linh căn gốc.
- Chưa sửa `CombatStats`, `CombatDamage`, `EntityDataSO`, `SaveManager` hoặc sửa gameplay; việc di trú điểm tăng HP/ATK kiểu cũ cần thiết kế trước. **Chi tiết:** [Stat_System_Design_0.0.54.md](Stat_System_Design_0.0.54.md).

## 15/10/2026 — Cảnh giới và Thiên Kiếp: xác nhận phương án B

- **Level và cảnh giới độc lập:** EXP/Level tiếp tục tăng cả khi chưa đột phá. Cảnh giới chỉ lên khi vượt Thiên Kiếp thành công, không còn tự suy ra từ `(Level-1)/10`.
- **30 cấp nội bộ mỗi đại cảnh giới là mốc thiết kế thử:** 1–9 Sơ kỳ, 10–19 Trung kỳ, 20–29 Hậu kỳ, 30 bình cảnh. Khi thắng Thiên Kiếp, ghi lại Level lúc đó làm mốc bắt đầu tiến trình đại cảnh giới mới; không cho lượng Level farm dư bỏ qua cảnh giới mới.
- **Thiên Kiếp ý tưởng ban đầu:** hai chặng 10 giây chống chịu và 10 giây phản công; chưa chốt hệ số, phần thưởng và hình phạt khi thất bại.
- **Trang bị vượt yêu cầu** vẫn có thể mặc nhưng chịu debuff/giảm hiệu suất; **một số võ học** có điều kiện mở khóa cứng (ví dụ Thần thức đòi Hóa Thần).
- Đây mới là chốt **mô hình tiến trình**, chưa sửa mã hoặc luật Save. Xem [Realm_Progression_Design_0.0.54.md](Realm_Progression_Design_0.0.54.md).

## Nguyên tắc phát triển .54

- Chỉ chỉnh mã trên `feature/0.0.54`; `main` là checkpoint 0.0.53.
- Trước khi đổi hành vi Forge/Equipment, thảo luận và chốt chi tiết thiết kế; các tham số 3/6/12/24/48 chỉ là giá trị thử nghiệm.
- Giữ cách căn chỉnh trực quan ở Play Mode là **phương án ưu tiên nghiên cứu** cho UI tiếp theo; không tự sao chép nguyên `OpeningCinematicUI` sang kho vì Inventory thay đổi theo modal/trang và tỉ lệ cửa sổ khác nhau.
- Duy trì test hồi quy tại `Tests~` và Unity Play Mode/Windows trước khi nghiệm thu. Những test chưa được chạy ở môi trường trợ lý không được ghi là đã qua.
- Nếu người phát triển có file `OpeningArtTheme.asset` đã chỉnh trên máy nhưng chưa push được, dữ liệu tọa độ cục bộ đó KHÔNG nằm trong GitHub; cần đồng bộ sau, tránh ghi đè toàn bộ asset mà không đối chiếu diff.

**Tài liệu liên quan:** `Docs/Forge_Test_0.0.52.md`, `Docs/Equipment_Affix_Discovery_0.0.52.md`, `Docs/Equipment_0.0.52_Schema.md`, `Docs/UI_Debug_Workspace_0.0.52.md`, `Docs/Opening_PlayMode_Layout_0.0.53.md`.
