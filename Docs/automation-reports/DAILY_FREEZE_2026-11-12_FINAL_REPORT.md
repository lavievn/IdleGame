# Daily Freeze — 12/11/2026

**RC PARTIAL.** Windows Development RC2 chạy từ `82f5fc026ea22dee634a18798a3f5103d69eee91`. Bốn smoke Windows và lượt đóng cửa sổ độc lập đều thoát mã 0. Edit Mode còn 1 lỗi và Play Mode còn 10 lỗi Presentation. Release EXP thành công có bằng chứng domain, chưa có một lượt Gate thành công trên Windows. Giữ nhánh freeze làm ref review.

[Launcher hồ sơ riêng](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/build/Play-Integrated-Isolated.cmd>) · [EXE](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/build/IdleGameV10.exe>) · [BuildReport](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/build/build-result.json>) · [Log build](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/build.log>).

## Nền và phạm vi tích hợp

Nền nhánh `integration/hero-hanvu-weather-audio` ở `3e4e1eff01da22d868514889e939bc726b5587bc`. Bản 11/11 trong IntegratedPlaytest chạy từ `a747735b5ccc3003043d611422f8360ad449cb3c`. Bản đã nghiệm thu gần hơn trong IntegratedPrefixMix chạy từ `350049ed6c3680ebbae821aaaa8367c8a9c9d116`. Checkpoint local `checkpoint/daily-freeze-2026-11-12`. Ref bàn giao `integration/daily-freeze-2026-11-12`. Commit thử nghiệm sau build `22e95929245342d2745e25c0e81085be75b45d46` chỉ cập nhật fixture Play Mode.

| PR | Exact head | Nội dung | Bằng chứng tích hợp |
| --- | --- | --- | --- |
| #12 | `5d41014eccd4e88d561c8fdb85fb7934ae248170` | Hero PNG animation | Trong nền |
| #13 | `5915829aa2510a739bde64addf0a41b705c1b8eb` | Hàn Vụ foundation | Trong nền |
| #16 | `c00dae628a7ea47ca18b9b16dfd13bd86fd9b216` | Sky, thời tiết, Rain Maker, audio | Trong nền |
| #17 | `cc271451faccd4c2ba7d9ef679a93c7627bc5f5c` | Lời kể người đốn củi và HUD | Merge bbb0100 |
| #18 | `9349f5fb1e6d32c3062520d12f422be18e7159cd` | Catalog Item/Loot và guard Forge | Merge e4b9fa7 |
| #19 | `2b88eb59c0c0d2c5c1620a0ffd2fd9191bdbcefd` | Codec tu luyện khi tái sử dụng DTO | Qua #20 |
| #20 | `7601c372d82f034e4e7165cb000cc400fabbde84` | EXP reserve và hợp đồng Mastery | Merge 2c97c30 |
| #21 | `0ab8c88033ea9c4d97a7be4ed03705acad69f675` | README, AGENTS và Narrative Baseline | Qua #23 |
| #22 | `d8e08eb397446d4fdd6da9ed59d25df02025b1cb` | Kiến trúc và audit narrative | Qua #23 |
| #23 | `382f389558ad956d2dc991df9ea91acdd16762fc` | Scope Hàn Vụ và handoff | Merge 03d3224 |
| #24 | `5063c3500b4cece64281d6ef3c1689484158c4ea` | N3 chặn callback cũ | Merge 81a6cde |
| #25 | `bb51121330ac1fc5aae9e55d17051e42ee3e8db5` | N4 commit snapshot trước công bố | Merge 63656ba |

#12, #13 và #16 là tổ tiên của nền. Các PR còn lại được đối chiếu patch-id và overlap trước khi nhập theo chuỗi phụ thuộc. GitHub chưa có PR mới hơn #25 lúc đối chiếu. Xung đột N4 và EXP nằm ở SaveManager và EntityDataSO. Nhánh freeze hợp nhất điều kiện đọc, ghi và giữ slot với origin, world climate, EXP và các component hiện có. Giữ Origin SaveVersion 3, hồ sơ tu luyện và dữ liệu trang bị. AGENTS giữ phần Karpathy và các quyết định ngày 12/11.

## Save, progression và kiểm thử

- Domain N3 kiểm tra callback cũ sau đổi Hero, map và spawn. N4 kiểm tra lỗi trước ghi, trong tệp tạm, trước replace, giữ canonical state khi thử lại, từ chối dữ liệu future hoặc field origin chưa nhận diện. N4 cũng xác nhận đồng hồ và EXP sống sót qua giao dịch mốc truyện cùng Qi, equipment, kho đồ, Forge, combatSkills và các slot thủ công. I/O trong nhóm này là boundary trong bộ nhớ.
- Unity Play Mode SaveLoad **4/4** trên fixture có profile tu luyện thật. Dùng SaveManager thật với tệp riêng. Ba bài Save bị chặn ở RC1 vì fixture thiếu component tu luyện do Awake bật profile hiện hành.
- Windows Integrated kiểm tra New Game, Travel, Save/Load, Retry, EXP bank tại Gate, giữ reserve sau thất bại và chống sấm cũ. Windows Hàn Vụ kiểm tra Accept, Decline, Ignore, sắc thuốc, lời kể, journal, idempotency và save v1/v2. Windows Development017 kiểm tra inventory, loot, forge và chỉ số.
- EXP 50 nhóm bao gồm các ca Dual. N4, N3 và Hàn Vụ nền cũng có ca chung. Kết quả để riêng theo bộ. Mastery mới có hợp đồng dữ liệu, chưa có trao điểm. Blood gameplay chưa mở.

| Domain | Kết quả | Bằng chứng |
| --- | --- | --- |
| Hàn Vụ nền | 11 PASS | [Log](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/domain-Story-r2.log>) |
| N3 | 30 PASS | [Log](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/domain-N3-r2.log>) |
| N4 | 35 PASS | [Log](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/domain-N4-r4.log>) |
| Dual Progression | 29 PASS | [Log](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/domain-Dual-r2.log>) |
| EXP và Mastery | 50 PASS | [Log](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/domain-Exp-r2.log>) |
| Item/Loot/Forge | 19 PASS | [Log](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/domain-Items-r2.log>) |

Unity 6000.5.0f1 compile **PASS**, Windows Development Mono2x **PASS**, 0 lỗi, 58 cảnh báo. MainGame Integrated, Hàn Vụ và Development017 thoát mã 0 trong Editor. [Log kết quả MainGame](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/Editor-runner.log>).

| Windows | Kiểm tra | Renderer | Exit | Bằng chứng |
| --- | --- | --- | --- | --- |
| Integrated | 9 PASS | NVIDIA GeForce RTX 2080 (ID=0x1e87) | 0 | [Kết quả](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/profiles/idlegame-v10-9c5980462465447cb19f75c847c55543/evidence/player-result.json>) |
| HanVuOrigin | 7 PASS | Microsoft Basic Render Driver (ID=0x8c) | 0 | [Kết quả](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/profiles/idlegame-v10-7486072220294902a575c0342e90fa90/evidence/player-result.json>) |
| Development017 | 5 PASS | Microsoft Basic Render Driver (ID=0x8c) | 0 | [Kết quả](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/profiles/idlegame-v10-82f07f3c5a234cd69bebf927378d0609/evidence/player-result.json>) |
| WorldClimateVendor | 14 PASS | NVIDIA GeForce RTX 2080 (ID=0x1e87) | 0 | [Kết quả](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/profiles/idlegame-v10-8fa4a6f89d574bfaae4ad7d377f453dd/evidence/player-result.json>) |

WM_CLOSE riêng trên RTX 2080 **PASS**, exit 0, Windows Event Log không có lỗi cho EXE ở lượt đóng đó. [Bằng chứng teardown](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/windows-native-close.json>). Kích thước 600×338, 800×450 và 1150×647 đã chạy trong smoke. Các thao tác gọi handler và kiểm tra trạng thái runtime. Chuột/phím vật lý và chất lượng âm qua loa còn NOT RUN.

## Lỗi còn lại

- Edit Mode 50/51. Một test derived health kỳ vọng 1554, runtime trả 1058. Công thức health của nền được giữ. [XML Edit Mode](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/unity-EditMode.xml>).
- Play Mode 124/134. B2 bước đi dài (3), HUD detail/loot target (2) và nhận diện quái (5). So sánh diff xác nhận chuyển động và thuật toán awareness được giữ từ nền. Chưa tái chạy toàn bộ các bài này trên Unity của nền trước freeze. [XML Play Mode](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/unity-PlayMode-owned-save.xml>).
- Lỗi mới đã xử lý ở fixture. Khôi phục Hàn Vụ giữ Pause nên lượt replay cần chủ động Resume trước khi đo hội thoại. Fixture Forge kiểm tra item hợp lệ trước khi gắn version tương lai, sau đó xác nhận runtime từ chối dùng item và dữ liệu lịch sử vẫn nguyên vẹn. Không chỉnh cân bằng hoặc nới Save guard.

## Asset, SHA256 và rollback

Đã so lại 478 tệp local, gồm HDR sky, Rain Maker, config vendor, 12 WAV BGM, 190 OGG nguồn và đoạn sấm Thiên Quan. Không có sai lệch byte sau import. 128 metadata của Hero, terrain và script mới đã giữ GUID trong Git. Quét 24 YAML và 414 script refs, 0 GUID trùng và 0 tham chiếu project/package thiếu sau nhận diện 2 tham chiếu built-in Unity. Smoke xác nhận HDR, rain, gió, splash, ripple, 133 map, nhóm 20 clip và crossfade. Bộ vendor và audio có giấy phép vẫn thuộc manifest local. [Danh sách asset và hash](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/local-asset-sha256.json>).

| Artifact | SHA256 |
| --- | --- |
| build/build-result.json | `4aa6356109cb12521faa8111db938f479e859a2a9c66edfa6be2d52ffc908f40` |
| build/IdleGameV10.exe | `e3c3a181dcb8bab365f7dae4788c8f8164dd1dfa8a5e51d5a717c04b1fc3f7e5` |
| build/IdleGameV10_Data/Managed/Assembly-CSharp.dll | `5cd0d8f713b80ea834c4c3700b782d300c06e85fc41e70908c271d7a234211cc` |
| build/IdleGameV10_Data/resources.assets.resS | `3b627c20ec91f3c23ef333d7f1e7591e83997865a610794e0749aeaf20a7a6d1` |
| build/IdleGameV10_Data/resources.resource | `7ed9aeae7883ff83f8d797ff83ea094baec23bd1c42dc2f4bd186fbbd7ed8eec` |
| build/Play-Integrated-Isolated.cmd | `ff2cb10b1b2cd0b21a68949b2fc46c62cfd44baa85e5810ab39005292e2c5559` |
| build/UnityPlayer.dll | `46eaec25318cdc190e820e4b22cbbbc4d9a52c284e9e2d8e46b642a29ca383fd` |

[Toàn bộ 375 checksum của RC2](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/RC2/artifact-sha256.json>) · [Manifest JSON trong Git](evidence/daily-freeze-2026-11-12/manifest.json).

Rollback bằng launcher cũ của 11/11 với hồ sơ đã có của bản đó. Đã so lại cả 750 tệp của hai bản rollback, 0 thay đổi. Giữ các hồ sơ thử riêng của từng build. [Launcher 11/11](<D:/IdleGame/Builds/IntegratedPlaytest/build/Play-Integrated-Isolated.cmd>) · [Launcher IntegratedPrefixMix](<D:/IdleGame/Builds/IntegratedPrefixMix/build/Play-Integrated-Isolated.cmd>) · [Checksum rollback](<D:/IdleGame/Builds/DailyFreeze_2026-11-12/rollback-sha256.json>).

## File production thay đổi so với nền

- Assets/DataScript/EntityDataSO.cs
- Assets/Script/CombatManager.WeaponSkills.cs
- Assets/Script/CombatManager.cs
- Assets/Script/CultivationGameplayState.cs
- Assets/Script/CultivationPersistence.cs
- Assets/Script/EquipmentFoundation.cs
- Assets/Script/ExperienceProgress.cs
- Assets/Script/GameManager.Cultivation.cs
- Assets/Script/GameManager.Tribulation.cs
- Assets/Script/GameManager.cs
- Assets/Script/HanVuNarrative.cs
- Assets/Script/HanVuOrigin.cs
- Assets/Script/HudW3View.cs
- Assets/Script/MasteryFoundation.cs
- Assets/Script/SaveManager.Tribulation.cs
- Assets/Script/SaveManager.cs
- Assets/Script/UIManager.StoryOrigin.cs
