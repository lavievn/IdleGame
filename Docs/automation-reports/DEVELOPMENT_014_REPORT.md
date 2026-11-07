# Bách Luyện Tiên Đồ — Development 0.1.4

## 1. Nguồn và bàn giao

- Repo `lavievn/IdleGame`, worktree `D:/IdleGame/Project`, nhánh `development_0.1.3`. Baseline sạch `ead64010a5f0ca0f2ab263b77152348da88cc5a3`. Unity `6000.5.0f1`.
- `dc4db9b` nhập asset và ghi hash. `c82bdec` tích hợp môi trường, Thiên Quan và Thiên Lôi. `e4bbd79` bổ sung kiểm thử runtime và clock. Commit tài liệu cuối nằm trên các commit này. SHA của commit báo cáo được ghi ở handoff ngoài repo.
- Nguồn test cuối và build là `e4bbd79`. Working tree sạch sau khi trả lại các tệp Unity sinh trong lượt test và commit tài liệu. Nhánh cục bộ đi trước remote 4 commit.
- Player Development Mono2x tại `D:/IdleGame/Builds/Development014/build/IdleGameV10.exe`. Build **PASS**, 0 lỗi, 57 cảnh báo trong build report. SHA-256 EXE `e3c3a181dcb8bab365f7dae4788c8f8164dd1dfa8a5e51d5a717c04b1fc3f7e5`, Assembly-CSharp `e27ca8045bdb659f12212e7b22bf1a5f0e5f2301e646815921e59b7d23cc121f`.

## 2. Asset và bố cục

Bộ đầu vào là bốn tệp rời người dùng xác nhận trên Desktop. SHA-256 của từng nguồn và từng asset nhập được đối chiếu trước khi tích hợp. Không có manifest gốc để xác thực độc lập. PNG fallback được trích từ video A ở 2,000 giây. [SHA-256 và đường dẫn](../architecture/TRIBULATION_014_ASSET_MANIFEST.json)

| Nguồn | Asset trong Unity | Kiểm tra |
| --- | --- | --- |
| `ground_wall.png` | `Resources/Environment/Tribulation/TribulationGround.png` | 1672×941, alpha 0–255 |
| `wall.png` | `Resources/Environment/Tribulation/HeavenlyGate.png` | 1672×941, alpha 0–255 |
| `vid_wall1.mp4` | `StreamingAssets/Tribulation/TribulationSky_A.mp4` | H.264 640×360, 24 fps, 8,043 s, có AAC ở nguồn |
| `vid_wall2.mp4` | `StreamingAssets/Tribulation/TribulationSky_B.mp4` | H.264 640×360, 24 fps, 8,043 s, có AAC ở nguồn |
| Khung từ A | `Resources/Environment/Tribulation/TribulationSkyOriginal.png` | PNG 640×360, hiển thị trước frame video đầu |

- **PASS** Hero áp sát từ phải sang trái. Tâm ảnh Thiên Quan giữ tại 35% từ mép trái, sai số kiểm tra dưới 2 px ở cả bốn kích thước. Ảnh tường cao 220% so với bố cục 288 đơn vị. Điểm nhận đòn vẫn ở trong thân đá, nằm lệch về phía Hero. Khung nhận đòn không lấy theo toàn bộ PNG.
- **PASS** Ground phủ vùng encounter một lần, giữ tỷ lệ và cùng cao độ chân tường. Phần xa và đá nứt dưới chân đến từ cùng PNG. Camera trial giữ khung ngang trong trận rồi khôi phục vòng đời camera/map khi quyết toán.
- **PASS** A mặc định và loop qua điểm cuối, B được chọn ở trial Bow, PNG dự phòng hiển thị khi chưa có frame và trong cấu hình Still của Staff. Chỉ một decoder và một RenderTexture 640×360 khi phát. Audio bị tắt. Pause giữ frame video. Lớp sét nằm trên actor, HUD nằm trên sét. [Hợp đồng môi trường và playback](../architecture/HEAVENLY_TRIBULATION_ENVIRONMENT_014.md)
- **NOT RUN** Không tạo lỗi decoder ở cấp OS với file nguồn hợp lệ. Có code fallback khi thiếu file, có lỗi prepare/phát hoặc quá thời gian nhận frame. Hai MP4 nguồn bị Media Foundation cảnh báo timestamp H.264, nhưng phát và lặp được. Chi tiết video mềm ở cửa sổ lớn do giới hạn nguồn 640×360.

## 3. Thiên Lôi và kết quả

Một lịch theo game-time cung cấp warning, impact và recovery. Warning fixture 0,26 s. Impact lấy đúng vị trí Hero, tạo mesh tia gấp khúc, các nhánh và sparks, rồi chạy một lần qua đường gây sát thương hiện hữu. Tia trong video độc lập với lịch đòn. Trace lưu encounter, strike, target life/spawn, Hero life, battle version và HP trước/sau. Hitch vượt cả cửa sổ warning vẫn phải đi qua warning và không dồn đòn. Pause đóng băng lịch. Terminal hoặc life đổi sẽ hủy lịch và tài nguyên của encounter.

- **PASS** Bốn lần impact Sword có cặp HP `600→594`, `594→588`, `588→582`, `582→576`, đúng identity và strike ID 1–4. Trace nằm trong `lightning-impacts.csv`. Số HP trên HUD và hình ảnh impact được chụp cùng lúc.
- **PASS** Combat thật của Sword, Bow, Staff, Unarmed. Có projectile Bow/Staff. Unarmed yếu nhận 6 HP mỗi đòn và thua. Hero cùng HP với Armor fixture nhận 1 HP mỗi đòn và thắng. Không có ngưỡng Level hoặc xác suất thắng. Luồng trúng tường dùng damage popup, flash, và vệt vỡ ngắn qua màn chuyển sau khi Save thành công.
- **PASS** Regression 16 tình huống đòn đơn với 2–3 quái chồng/tách. Mỗi release xác nhận một hit và một kill. Legacy AoE đối chứng hạ 3 mục tiêu theo cơ chế cũ. Không tái hiện lỗi nhiều kill từ một basic attack.
- **PASS** Kiểm thử domain gồm stale owner/target/life, receipt hết hạn và trùng, double KO xử thua, giới hạn phạt và kiểm tra không mất reserve. Bằng chứng double KO nằm ở domain fixture.
- **PASS** Trial yếu thua với trừ 500 căn cơ theo fixture và hồi sinh. Trial thắng sang `fixture:truc-co` với căn cơ 0, capacity 3/2. Các trial giữ reserve 1234, EXP/Level, vật phẩm, trang bị, Forge, Injury, Blood và tiến độ map. Sword được giữ attack windup để quan sát đủ một vòng video, nên thời gian Sword ở CSV là lượt nghiệm thu trình chiếu.
- **PASS** Save thất bại trước Replace giữ nguyên Hero, session và checkpoint. Retry ghi đúng một lần. Native còn kiểm tra file đang khóa, snapshot cạnh tranh, đối soát sau Replace, backup bất biến và Load kết quả không phát lại phần thưởng hoặc hình phạt. [Hợp đồng transaction 0.1.3](../architecture/TRIBULATION_SAVE_TRANSACTION_V1.md)
- **PASS** Owned trial chặn lần thử thứ hai, manual Save, Load, Travel và Menu trong lúc còn liability. Pause dừng damage và clock. Sau quyết toán, các lớp trial, decoder, sét chờ và projectile biến mất, Hero được rebind và Travel hoạt động. Matrix thường bao phủ Retry, Menu/Continue, New, Save/Load và các panel Character, Inventory, Forge, World Map.

Kết quả trên Windows Player dùng fixture Level 1. HP/đòn là lần trúng đầu tiên sau phòng ngự.

| Fixture | Kết quả | Giây Combat | HP lúc quyết toán | HP/đòn |
| --- | --- | ---: | ---: | ---: |
| weak | thua | 4.004 | 0 | 6 |
| sword | thắng | 10.815 | 540 | 6 |
| bow | thắng | 2.992 | 588 | 6 |
| staff | thắng | 4.387 | 576 | 6 |
| unarmed | thắng | 4.208 | 576 | 6 |
| same-hp-armor | thắng | 8.415 | 16 | 1 |

## 4. Verification theo lớp

| Lớp | Kết quả | Bằng chứng |
| --- | --- | --- |
| Mono regression | **PASS** 16 nhóm mục tiêu, 209 dòng PASS theo nhóm, gồm 3 test lịch sét mới. Suite gate ghi thêm 37 assertion | `mono/*.log` |
| Mono OpeningFontRegression | **FAIL baseline đã biết** `HUD never overwrites popup font: 18 != 16` tái hiện | `mono/opening-font.log` |
| Unity Native | **PASS** 54 kiểm tra domain và I/O thật trên Save riêng, trước lần chỉnh lớp vẽ vỡ phong ấn cuối | `native/v13-native.txt` |
| MainGame Play Mode | **PASS** sáu trial, A/B/Still, pause, bốn kích thước, damage, retry ghi lỗi, hồi sinh và Travel | `main-game/result.json`, `trial-combat.csv`, `lightning-impacts.csv` |
| Windows Player method-driven | **PASS** sáu trial thật, decode A/B, PNG, pause, four-size HUD và impact có HP trace | `windows/trial` |
| Windows fresh process | **PASS** tiến trình mới khôi phục đúng hai Save terminal, giữ nguyên file Active và chặn resume | `windows/restart` |
| Windows normal world | **PASS** farm 30 giây, bốn vũ khí, panel, Save/migration, Retry, Menu, New và Travel | `windows/matrix` |
| Native mouse/keyboard, retail/IL2CPP, mất điện giữa ghi | **NOT RUN** | Kiểm tra nút ở tầng handler và vùng hit-test |
| Thiên Kiếp/Trúc Cơ production | **BLOCKED**, gate **OFF** | Cần config successor, balance, policy căn cơ và chính sách Active khi crash/resume/cancel được duyệt |

## 5. Ảnh và hiệu năng

Gốc bằng chứng ngoài repo `D:/IdleGame/Automation/v014/evidence`. Thư mục `main-game` và `windows/trial` có `v014-impact-600.png`, `v014-impact-800.png`, `v014-impact-1150.png`, `v014-impact-1360.png`, `v014-warning-600.png`, `v014-video-b.png`, `v014-fallback.png`, `v014-seal-break.png`, `v13-success-hud.png` và `v13-failure-hud.png`. Ảnh vỡ phong ấn được chụp trên màn chuyển map; ảnh kết quả chụp sau khi world đã quay lại.

Windows Player farm tự nhiên 800×450 trong hai tiến trình tuần tự trên cùng máy, Unity 6000.5.0f1 và cùng harness. Mỗi lượt bắt đầu với New và profile riêng.

| Chỉ số | 0.1.3 | 0.1.4 |
| --- | ---: | ---: |
| Thời gian thực (s) | 30.001 | 30.000 |
| Số frame | 4291 | 4317 |
| Frame mean (ms) | 6.992 | 6.949 |
| Frame P95 (ms) | 6.938 | 6.938 |
| Main Thread mean (ms) | 6.982 | 6.939 |
| GC allocation (byte/frame) | 11323 | 10893 |
| GC gen0 | 43 | 42 |
| Checkpoint writes | 3 | 3 |

Hai Hero đều sống hết cửa sổ. Combat tự nhiên có 30 và 11 sự kiện sát thương. Một cặp mẫu cho kết quả gần nhau; số byte/frame giảm trong lượt 0.1.4 nhưng chưa đủ để kết luận cải thiện. Cả hai recorder CPU/GC hợp lệ, phép đo allocation riêng của Cultivation.Advance không hỗ trợ trên runtime này.

## 6. Các điểm còn lại

- Kiểm tra metadata Unity có 0 lỗi và 3 cảnh báo thiếu meta đã biết ở Lake/Sea/Steppe.
- Cảnh báo mở game cũ và Unity Editor Search xuất hiện trong log MainGame. Báo cáo không coi chúng là lỗi Combat Thiên Kiếp. Font baseline vẫn mở.
- **NOT RUN** Chưa có phép đo tách riêng chi phí video/mesh của một trận giống hệt trước và sau. Phép đo farm ở trên nhằm phát hiện hồi quy ngoài encounter. MP4 đã xác minh playback và lifecycle bằng trận thật.
- Đề xuất 0.1.5 là duyệt điều kiện production trước, sau đó làm pass đo chi phí decoder riêng và nghiệm thu thao tác native. Chưa triển khai 0.1.5.

Build, log, screenshot, profile fixture và cache nằm ngoài commit. Handoff `D:/IdleGame/Automation/v014/handoff.json` ghi commit cuối và trạng thái thực tế.
