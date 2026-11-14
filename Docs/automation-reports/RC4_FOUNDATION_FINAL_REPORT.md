# RC4 Foundation — Báo cáo tích hợp

**Nhánh** `feature/rc4-combat-foundation` · **Project** `D:\IdleGame\Projects\RC4Foundation`
**Baseline** Draft PR #29, `5a22ed3f9c90cc173726b686efffb0baa92489e2` · **Source Windows** `52518b8101fed1d25ca59ec6dc14586677e4ef09`

## Checkpoint

| CP | Kết quả | Commit và hồ sơ |
|---|---|---|
| CP1 | Xác minh PR #29, RC3 và asset. 191/191 OGG khớp hash đã kiểm kê. Khôi phục vendor local bằng .meta gốc. | `4cb8b59`, [baseline](RC4_BASELINE.md) |
| CP2 | Morning một nguồn mặc định, Cricket chỉ đêm khô, Coast giữ nền và loop thời tiết riêng. Tách cấu hình Light, Heavy và Storm. | `bf206b5`, [audio](RC31_AUDIO_CORRECTIONS.md) |
| CP3 | Bảng F8 đọc nguồn Unity thật, lý do lớp im, lịch phát, Mute/Solo, gain, nghe thử, preview và JSON. | `5d06809`, `52518b8`, [hướng dẫn](AUDIO_DEBUG_USER_GUIDE.md) |
| CP4 | Audit nguồn stat, vũ khí, damage, recalculation và persistence. | `62250d9`, [stat](STAT_PIPELINE_AUDIT.md) |
| CP5 | Kiểm kê trang bị và kiểm tra loot độc lập, ownership, Forge, Inventory và Save. | `37949ba`, [trang bị và loot](EQUIPMENT_LOOT_AUDIT.md) |
| CP6 | Bảng combat đọc dữ liệu runtime. Mastery tiếp tục ghi sự kiện, Tinh Huyết tiếp tục khóa. | `5d06809`, `7eab1a3`, [Mastery và ngũ hành](MASTERY_ELEMENTAL_READINESS.md) |
| CP7 | CSV 51 hàng và 16 cột dành cho buổi cân bằng. | `6650031`, `52518b8`, [CSV](RC4_COMBAT_DESIGN_WORKBOOK.csv) |
| CP8 | Windows Development Build và kiểm thử theo [ma trận RC4](REGRESSION_MATRIX.md). | [Bàn giao Windows](WINDOWS_HANDOFF.md) |

## Thay đổi runtime

- Morning chỉ dùng ở các map rừng đang được gán và giữ chu kỳ 120 giây. Giới hạn một nguồn thực yêu cầu fade-out rồi fade-in trên nguồn tái sử dụng. Meadow khô giữ hai loop nền, chỉ bổ sung Cricket vào ban đêm. Wet Meadow độc quyền trong family. Coast giữ 3 loop nền, sự kiện 10–15 giây, tối đa hai nguồn thời tiết và giới hạn tổng gain trong crossfade.
- Climate tiếp tục cung cấp LightRain và HeavyRain. Tạm dùng ngưỡng `0,85` trong cường độ HeavyRain để chọn profile âm thanh Storm. Gain, giới hạn nguồn, nhịp sấm, clip allowlist và fade chỉnh độc lập. Các quyết định cần nghe và chốt nằm trong [danh sách thiết kế](RC4_DESIGN_DECISIONS.md).
- Bảng F8 trong Development/Editor lấy mức phát thực tế từ AudioSource và số liệu từ combat hiện hành. Preview điều chỉnh riêng audio. JSON preset được lưu riêng với Save. Trường damage breakdown dùng trace hiện có, mức chi tiết tùy loại đòn. Code gameplay giữ công thức combat, tỷ lệ loot, Save schema và các quy tắc progression của RC3.

## Kết quả

**COMPLETE** cho các checkpoint triển khai, audit và kiểm thử tự động. Full Unity Edit Mode 51/51, Play Mode 136/136 và Windows Player 6/6, bằng baseline RC3. MainGame 10/10 nhóm. Hàn Vụ Windows xác nhận mute giữ lịch scheduler và xuất/nhập gain JSON trong bộ nhớ. Các lượt chạy dùng hồ sơ riêng. Xem [ma trận](REGRESSION_MATRIX.md) cho từng fixture và giới hạn bằng chứng.

## Build và bàn giao

- Mở `D:\IdleGame\Builds\RC4Foundation\build\Play-RC4-Isolated.cmd`. Windows 64-bit Development, Unity 6000.5.0f1, Mono2x, 1.061.816.717 byte, 0 lỗi, 58 cảnh báo. Giữ RC3, RC2 và fallback builds tại chỗ.
- Log build `D:\IdleGame\Builds\RC4Foundation\build.log`. Log test dưới `D:\IdleGame\Logs`. Chữ ký SHA-256 của EXE là `E3C3A181DCB8BAB365F7DAE4788C8F8164DD1DFA8A5E51D5A717C04B1FC3F7E5`, Assembly-CSharp.dll là `D5BC03491A8F16E1ED9BD3118FD8127DBA1B1AD27920308930EBB2B5910FE318`, resources.assets là `E294AA8EE2C7B3FB26E1E961E0BCFAB789C4174719847482EAC951E55256662D`.
- Âm lượng, tần suất sấm và ngưỡng Storm vẫn là thông số thử. Giữ nguyên 12 OGG chưa xác định. Human visual/audio và thao tác chuột/phím thật **NOT VERIFIED**. Dùng checklist trong [bàn giao](WINDOWS_HANDOFF.md).

Launcher đã được mở với đúng mode và profile. Một lượt đóng cửa sổ Player bằng yêu cầu đóng của Windows trả mã 0, chưa tái hiện crash lúc thoát ở lượt đó. Log `D:\IdleGame\Logs\RC4-final-LauncherClose.log`. Không dùng kiểm tra này làm bằng chứng tương tác người chơi.
