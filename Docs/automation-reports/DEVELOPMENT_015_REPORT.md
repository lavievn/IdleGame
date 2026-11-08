# Bách Luyện Tiên Đồ — Development 0.1.5

## A. Audit baseline

Worktree `D:/IdleGame/Project`, nhánh `development_0.1.3`, baseline sạch `8ecf3c4e2eb8c99ed7c2fd7087025bb113303580`. Handoff cuối 0.1.4 khớp HEAD. Chín commit từ `ead6401` bao phủ môi trường, tia sét procedural, đường vào ải 800 đơn vị, cả hai BGM và lựa chọn theo encounter. Bảy asset trong manifest khớp SHA-256. Audit xác nhận video A/B, PNG dự phòng, target tĩnh cuộn cùng terrain, tỷ lệ 300% và hạ 100 px, điểm tường ổn định ở 35%, một AudioSource có loop và cả hai bài. Transaction fixture, Save isolation và bằng chứng UI bốn cỡ được đối chiếu. [Báo cáo 0.1.4](DEVELOPMENT_014_REPORT.md)

## B. Performance Windows

Hai lượt chạy trên cùng máy, Windows Development Mono2x và Unity `6000.5.0f1`. Farm dùng profile production v3 trên map thường, bắt đầu New, 800×450 và dùng nhịp frame mặc định. Trial dùng cùng Hero Sword, cùng encounter đã vào tầm, cùng 800×450 và giới hạn 60 FPS. Giữ đòn đánh Hero chờ tới khi đo xong. Sét vẫn trừ HP qua Combat, audio tắt bằng Pause nguồn riêng, mesh tắt qua Graphic và decoder tắt qua PNG. Sau mỗi đổi mode, chờ warmup 1,25 s sau frame video đầu. Mười cửa sổ khoảng 6 s được lặp theo thứ tự xuôi rồi ngược. Số HP giảm tự nhiên theo từng strike. Mỗi cửa sổ đo trong trial không ghi Save. Các điều kiện farm và trial khác nhau về frame pacing, profile và workload.

GPU NVIDIA GeForce RTX 2080, Direct3D11, nhạc HeavenlyGateThemeAlternate. Bảng lấy trung bình chỉ số của hai cửa sổ trial cho từng mode. Farm có một cửa sổ 30 s.

| Mode | Frame mean/P95/P99 ms | Main Thread mean/P95/P99 ms | CPU process ms/frame | GC B/frame | Gen0 / 2 mẫu |
| --- | ---: | ---: | ---: | ---: | ---: |
| Farm | 6.946/6.938/6.938 | 6.939/7.214/7.438 | — | 11132 | 43 / 1 mẫu |
| PNG | 16.671/16.686/16.861 | 16.570/16.690/16.877 | 3.220 | 11018 | 8 |
| Sky A | 16.669/16.674/16.738 | 16.568/16.678/16.737 | 4.025 | 11019 | 8 |
| Sky B | 16.671/16.680/16.758 | 16.569/16.686/16.769 | 3.614 | 11017 | 7 |
| A + mesh | 16.699/16.679/16.758 | 16.597/16.682/16.743 | 3.597 | 11017 | 7 |
| A + mesh + BGM | 16.668/16.669/16.694 | 16.566/16.667/16.697 | 3.441 | 11017 | 8 |

- Các lượt có 6–6 strike thật. Mỗi trial có một object sét. Các lượt bật mesh dựng 180–187 lần/cửa sổ, tối đa 280 vertex và 48.2–53.6 µs/lần dựng. Tạo hình strike 1.9–14.2 µs/strike.
- Video A/B có đúng một VideoPlayer và RenderTexture 640×360, PNG dùng 0/0. MP4 hỏng thuộc fixture ghi nhận 1 callback lỗi và quay về PNG, decoder/RT thu hồi. Pause đóng băng frame/strike và resume tiếp tục phát.
- Main Thread và frame gồm thời gian pacing, nên gần 16,7 ms ở giới hạn 60 FPS. CPU process tổng hợp các thread, giúp quan sát chênh lệch theo lớp nhưng chưa tách thread Media Foundation, GPU hay chi phí nền. GC 11017–11020 B/frame là số toàn Player, gồm HUD và Combat. Hai mẫu mỗi mode chưa đủ để khẳng định clip nào nhanh hơn hoặc quy GC cho riêng decoder.
- Baseline benchmark chạy trước sửa receipt, lượt cuối sau các sửa. Dữ liệu thô của cả hai được giữ cùng các lần đo farm. Không áp dụng tối ưu decoder chỉ từ số liệu cap FPS.

## C. Lifecycle và Save

- Khi HP hai phía còn dương, live frame chỉ tiến đồng hồ. Baseline gọi `Accept(Guid.NewGuid())` mỗi frame và giữ GUID trong tập dedup suốt attempt. Giới hạn nhận receipt vào quan sát terminal giữ thứ tự xử double KO, provenance, clock và schema. Sáu trial kiểm tra đồng hồ tiến trong khi tập receipt còn rỗng ở pha đang sống.
- Windows fixture quan sát warning, impact, recovery, pause, damage chính xác một lần và việc damage dừng sau trial. Native domain bao phủ owner/epoch/target/life sai, callback trùng/cũ và double KO. Các bài test domain được phân biệt với combat Windows thật.
- Retry/New trong Active giữ nguyên exact AutoSave hash và cùng Hero. Guard Load, Save, Menu, Travel chặn khi còn liability. Lỗi ghi trước Replace giữ Hero/session và retry cùng candidate. Native verifier bao phủ file bị khóa, lỗi trước/sau replace, snapshot cạnh tranh, backup và terminal idempotence.
- Thắng và thua khôi phục đúng Foundation dự kiến, reserve, EXP/Level, trang bị, inventory và Forge. Trial yếu thua và hồi sinh. Trial thắng dùng successor fixture. Replay Windows dùng tiến trình mới với hai terminal Save thật và bản sao Active đã ghi trong trial để kiểm tra fail-closed. Mất điện hoặc kill đúng điểm Replace **NOT RUN**.
- 18 ảnh chụp số đếm trước/trong/sau sáu lượt. Decoder/RT/VFX riêng về 0 sau mọi lượt. Unity allocated đầu/cuối 333.9/417.4 MB, Mono used 9.0/9.6 MB. Mức Unity tăng theo các lượt nạp đầu và gần ổn định cuối chuỗi. Đây là ảnh chụp heap tổng, chưa tách cache engine hay native decoder và chưa chứng minh leak-free.
- [Các phương án quit/crash/resume](../architecture/TRIBULATION_INTERRUPTION_POLICY_OPTIONS_015.md)

## D. Production readiness

Tám mẫu Hero thực, 4 cấp độ và 4 WeaponFamily. Config A quan sát được 8 thắng, 0 thua. Xem thời gian trong tầm, HP/strike/hit theo mẫu trong đề xuất và CSV. Config B và bảng Trúc Cơ là phương án duyệt.

Hai mô hình penalty đề xuất là 360 Foundation theo tỷ lệ đại cảnh giới và 600 theo độ dài khoảng tiểu cảnh giới. Mô phỏng World Qi v2, Capacity v1, reserve 0/1.234 cho thấy thời gian phục hồi phụ thuộc rate và reserve. Bảng Trúc Cơ có hai phương án absorption/refinement. Các số liệu dành cho duyệt. Production **OFF**. [Đề xuất cân bằng](../architecture/TRIBULATION_BALANCE_PROPOSAL_015.md) · [Điều kiện mở production](../architecture/TRIBULATION_PRODUCTION_READINESS_015.md)

## E. Regression và nghiệm thu

| Lớp | Kết quả |
| --- | --- |
| Mono | **PASS** 18 nhóm, 247 dòng PASS theo nhóm, có nhóm dùng chung assertion. |
| OpeningFontRegression | **PASS** sau khi bỏ lệnh đặt font 16 khi chỉ đổi nhãn nút. Baseline 0.1.4 và HEAD trước sửa đều tái hiện 18 != 16. Assertion cũ giữ nguyên, có thêm kiểm tra đổi nhãn consent và wrap. |
| Unity Native | **PASS** 54 kiểm tra gate/Save; capacity và tương thích World Qi/config qua verifier riêng. |
| MainGame Play Mode | **PASS** sáu trial, bốn cỡ, hai bài nhạc, A/B/Still, pause, 800 đơn vị, lỗi ghi/Retry, thắng/thua và lifecycle. |
| Windows Player method-driven | **PASS** Matrix thường, Combat bốn vũ khí, đơn mục tiêu/AoE, Character/Inventory/Forge/World Map, Travel/New/Retry, Trial, replay và benchmark. |
| Windows native mouse | **BLOCKED** Cửa sổ TT1 của build cuối và popup font đã quan sát ở 800×450. Hai lần công cụ kiểm tra điểm chuột đều trả cửa sổ ChatGPT bên dưới thay vì game, kể cả sau khi đưa TT1 ra trước. Không có click Độ Kiếp được xác nhận. |
| Windows native keyboard, IL2CPP và mất điện | **NOT RUN**. |
| Production | **OFF**, cần duyệt balance, successor và policy gián đoạn. |

Các log engine vẫn có cảnh báo timestamp H.264 và cảnh báo môi trường mở game đã có ở các lượt trước. Lượt native hết hạn chờ input. Runner ghi exit Windows -1073741819 sau khi Player ghi kết quả timeout. Nguồn lỗi shutdown chưa được xác định. Các lượt Windows method-driven kết thúc thành công. Asset trình chiếu đã qua playback, corrupt fallback và cleanup. Số heap tổng cần kiểm tra soak dài nếu dùng để xác nhận quản lý cache. Kiểm tra metadata có 0 lỗi và 3 cảnh báo baseline Lake/Sea/Steppe.

## Bàn giao

- `2834ec6` Add owned Windows trial profiling and lifecycle evidence
- `50c4724` Submit trial receipts only for terminal HP observations
- `cd7caa3` Record controlled balance samples and censored fights
- `4414f69` Preserve opening popup typography when changing labels
- `cbc9cb5` Record process CPU and native confirmation evidence
- Commit tài liệu cuối và working tree sạch được xác minh trong handoff.

Bằng chứng ở `D:/IdleGame/Automation/v015/evidence`. Handoff ngoài repo tại `D:/IdleGame/Automation/v015/handoff.json`, gồm SHA commit báo cáo cuối. Build, hồ sơ Save kiểm thử và log nằm ngoài repo.

Ưu tiên đề xuất cho 0.1.6 là chốt hành vi Active khi gián đoạn, duyệt penalty/successor và chạy pilot cân bằng nhiều seed cùng phép đo CPU/GPU bỏ giới hạn frame. Gate chỉ nên được xem xét sau các quyết định và phép thử tương thích Save.
