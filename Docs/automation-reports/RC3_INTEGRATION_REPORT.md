# CP5 — Báo cáo tích hợp RC3

**PARTIAL — Windows Development đã sẵn sàng cho nghiệm thu thủ công.** Auto-review từ chối cập nhật Morning trên bản đồ đồi núi vì yêu cầu đó khác chỉ dẫn trước về nhóm này. Tinh Huyết tiếp tục khóa trong gameplay theo phạm vi RC3.

## Nền và checkpoint

Checkout `D:\IdleGame\Projects\RC3OvernightIntegration`, nhánh `feature/rc3-overnight-integration`, tách từ Draft PR #28 tại `8aaff49397af73a8f705c27dd094317c7bcee1ae`. Bản Windows được build từ `ebdfc41afd6dde51796421e3b9e6432e00058917`. Commit CP5 chỉ chứa báo cáo và handoff. SHA đầy đủ của commit bàn giao nằm ở HEAD của Draft PR RC3.

| Checkpoint | Commit code | Kết quả |
|---|---|---|
| CP1 | `4651755` | Đối chiếu HEAD local/remote, checkout sạch, kiểm 95 file RC2 và phục hồi 67 file vendor. [Audit](RC3_BASELINE_AUDIT.md) |
| CP2 | `d30cfc8` | Sau khi nhận lời, lần đầu nhấn ô Cô Hồn Lâm được đánh dấu sẽ Travel và hiện mục tiêu lá bạc. Các ô khác và các lượt sau giữ quy trình chọn đích. [Opening Quest](OPENING_QUEST_UX_REPORT.md) |
| CP3 | `d75c1ac` | Xác minh 191 OGG khớp ZIP, CRC và asset hash. Meadow 2 loop + cricket lúc khô, Morning 3 loop cho rừng khô, crossfade hai nhóm, gain chỉnh theo clip và family. Morning ở đồi núi **BLOCKED**. [Audio audit](AUDIO_MANIFEST_AUDIT.md) |
| CP4 | `ebdfc41` | Nhãn Qi hiện tốc độ nguồn tại vị trí Hero, menu dừng đồng hồ tu luyện. Chỉnh fixture Unity theo thiết kế và hành vi đã có, thêm test ranh giới HP Tinh Huyết. [Regression matrix](REGRESSION_MATRIX.md) · [Tinh Huyết](TINH_HUYET_READINESS.md) |
| CP5 | Commit bàn giao | Windows RC3 riêng, rollback và checklist nghiệm thu. [Windows handoff](RC3_WINDOWS_HANDOFF.md) |

## Kết quả xác minh

| Nhóm | Baseline → RC3 | Bằng chứng |
|---|---|---|
| Domain | EXP/Qi/Mastery/Blood 57 → 58 PASS, N3 30 → 31 PASS, N4 35 → 36 PASS. Opening 12 PASS, ItemLoot 19 PASS, Audio 44 PASS. Các runner này báo 0 FAIL và không có ca skip được báo. | `D:\IdleGame\Logs\RC3-ProgressionDomain.log`, `RC3-HanVuN3.log`, `RC3-HanVuN4.log`, `RC3-OpeningDomain.log`, `RC3-ItemLootFoundation.log`, `RC3-AudioDomain.log` |
| Unity 6000.5.0f1, Edit Mode | 50 PASS / 1 FAIL → **51 PASS / 0 FAIL / 0 SKIP** | `D:\IdleGame\Logs\RC3-unity-EditMode.xml` |
| Unity Play Mode | 124 PASS / 10 FAIL → **136 PASS / 0 FAIL / 0 SKIP**. Tách thêm hai ca ranged sight với mode cố định. | `D:\IdleGame\Logs\RC3-unity-PlayMode-final.xml` |
| MainGame và fixture Unity native | Opening/audio handler **PASS**. Tinh Huyết JsonUtility qua fixture **PASS**, gameplay khóa. | `D:\IdleGame\Logs\Unity-MainGameHanVu-20261114-024434.log`, `Unity-Native-20261114-145758.log` |
| Windows automated | **6/6 PASS và exit 0**. HanVuOrigin 10 checks, Integrated 10, Playable015 7, Playable015Replay 1, Playable016 6, Development017 5. Xác minh cả Save/Load/Retry, Thiên Kiếp thắng/thua, mở lại EXE và giữ kết quả Mastery. | `D:\IdleGame\Logs\RC3-Windows-*.log` và `profiles\*\evidence\player-result.json` trong thư mục build RC3 |
| Windows teardown | WM_CLOSE trên RTX 2080/D3D11 thoát 0, không có lỗi Application trùng đường dẫn EXE trong lượt đó. | `D:\IdleGame\Builds\RC3OvernightIntegration\windows-native-close.json` |
| Human playtest | **NOT VERIFIED**. Harness phát âm thanh thật và kiểm tra nguồn tiếng, thao tác qua handler. Chuột/phím thực tế, cảm nhận UI và chất lượng phối âm còn chờ nghiệm thu. | [Checklist](RC3_WINDOWS_HANDOFF.md) |

Mô phỏng Hàn Vụ tại 1 Qi/s đạt 3.600 tu vi sau 3.600,1 giây active. Windows kiểm đồng hồ thật và các tình huống Thiên Kiếp bằng owned fixture. Mốc mô phỏng chỉ áp cho cấu hình và vị trí đã thử.

## Asset và bản Windows

ZIP SHA-256 `650aa81e8cc3d9d4b6ad668e6338d7001c11a825a3c05d25be1cc2f80037918a`. Build Unity 6000.5.0f1, Mono2x, 0 lỗi, 58 cảnh báo. Launcher `D:\IdleGame\Builds\RC3OvernightIntegration\build\Play-RC3-Isolated.cmd` dùng profile riêng. `Assembly-CSharp.dll` SHA-256 `EE78B8EB2A8C21ACA64A42397F3093A2D9C17B8D6018B19639F7F0E810DAF3E6`, `resources.assets` SHA-256 `7C30BDB063A20B1B36BCCF051B51669C547EEBB85C01E3B91B20A2D7562007F5`.

## File thay đổi và quyết định còn mở

- Opening: `HanVuOrigin.cs`, `UIManager.StoryOrigin.cs`, `WorldMapPanelUI.cs`, `HanVuOriginSmoke.cs`, `StoryOriginRegression.cs`.
- Audio: `EnvironmentAudioManifest.json`, `EnvironmentAudioRules.cs`, `EnvironmentAudioPlayer.cs`, `EnvironmentAudioRulesTest.cs`, `IntegratedPlaytestSmoke.cs`.
- Progression: `CultivationGameplayState.cs`, `GameManager.Cultivation.cs`, `UIManager.Cultivation.cs`, `Development015Smoke.cs`, `ExpBottleneckMastery/Regression.cs`, `BloodEssenceRegression.cs`.
- Native regression: `B2SpawnPositionTests.cs`, `HudW3Tests.cs`, `MonsterAwarenessTests.cs`, `JsonUtilitySaveLoadTests.cs`. Tài liệu ở `Docs/automation-reports`.
- Cần xác nhận áp Morning cho 53 map đồi núi theo đặc tả RC3. Coast weather dùng preset hai slot trong kiểm thử, giới hạn cuối còn **NEEDS_REVIEW**. Mapping Meadow Rain/Storm, map Lodges, 12 clip chưa nhận diện và mix cuối vẫn chờ quyết định.
- Tinh Huyết cần đường progression tới Kim Đan và chính sách ghi nợ/HP khi đang Thiêu Đốt trước khi mở gameplay.

Draft PR riêng, base là nhánh của PR #28. Bản build, rollback và Save cá nhân của các phiên bản trước giữ nguyên.
