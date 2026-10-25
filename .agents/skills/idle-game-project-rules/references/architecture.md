# Bằng chứng kiến trúc — snapshot 25/10/2026

Đối chiếu mã tại `adcae91c9f35a510b6f813728cc5703445056dc7`. Các đường dẫn trong
bảng tính từ gốc repo. Tên hàm là điểm tra cứu; đọc lại mã khi phiên bản thay đổi.
Đây là kiểm tra tĩnh, không phải chứng nhận Unity/Windows Player cho snapshot.

## Đã triển khai trong mã

| Khu vực | Nguồn và điểm vào | Phụ thuộc cần giữ |
|---|---|---|
| Scene/build | `ProjectSettings/EditorBuildSettings.asset`; `Assets/Scenes/MainGame.unity` | MainGame là scene global duy nhất enabled; SampleScene vẫn tồn tại |
| Phiên bản | `ProjectSettings/ProjectVersion.txt`; `Packages/manifest.json` | Unity 6000.5.0f1, UTF 1.7.0, URP 17.5.0; không nâng/hạ theo upstream |
| Điều phối | `Assets/Script/GameManager.cs`: Start, SetupPreGameUI, DeployHero, TryLoadSlot, OnRetryClicked, CompleteGameplayEntry | Hero template → runtimeHeroData; bind combat/UI; hủy entry/wave cũ trước đổi trạng thái |
| Entry | `GameManager.OnOpeningRevealCompleted`, `CompleteGameplayEntry`; `HeroSpawnVfx.Begin/Advance/Finish`; `StartMenuUI.EnterGameplayImmediately`; `OpeningCinematicUI` | Hero tại 80% BattleArea trước VFX; unscaled time khi khóa gameplay; lỗi/hủy không được kẹt pause/black cover |
| Hero | `Assets/Script/HeroController.cs`: Awake, SpawnHero, TickMovement, LateUpdate, HideHero | Mỗi actor có RectTransform/life identity; ActiveHeroes; B2 clone và parent độc lập |
| Enemy/pool | `Assets/Script/MonsterController.cs`; `MonsterSpawner`; `CombatManager.StartBattle` | OnEnable/life identity, target hợp lệ và dọn projectile/callback cũ khi pool tái dùng |
| Combat | `Assets/Script/CombatManager.cs`: SetupHeroInfo, StartBattle, Update, LateUpdate, OnDisable; `HeroCombatState`; `CombatBalance`; `CombatDamage` | HP/attack timer theo Hero, routing mục tiêu, danh sách monster, projectile và stat tính lại |
| Di chuyển/camera | `Assets/Script/EnvironmentManager.cs`: Update, LateUpdate, Position, SetPosition, HeroMovementDeltaTime, ResetCameraForMapEntry | Update tick actor và lập pan; LateUpdate áp pan; không nhầm tọa độ màn hình với BattleArea |
| Hình actor | `CultivationSpriteBridge`; `HeroSpawnVfx`; `HeroRenderProbe` trong `Assets/Script/` | Chỉ body Image nhận scale/flip/fade; foot logic và render cần đo cùng hệ; probe cuối render là chẩn đoán |
| Đội hình/Y | `HeroFormation`; `HeroLaneMotion`; `GameManager.CreateTestCompanion` | Không teleport; dt di chuyển có clamp riêng; ranged lane giữ mục tiêu, an toàn foot Y, không cộng Y vào exploration |
| Environment | `Assets/Script/GroundPresentation.cs`: Install, RefreshTerrain, LayoutTiles, PlaceLoop, TileEdges | Chín cache bốn lớp; nguồn và runtime riêng; render width có scale; không thêm spacing fudge |
| World/wave | `WorldMapCatalog.BaseTerrain/ApplyMap/Normalize/CompleteWave`; `GameManager.CallNextWave/OnMonsterDied/CancelNextWave`; đoạn cập nhật exploration trong LateUpdate | 133 node; world-map travel thủ công, clear không tự đổi node; wave kế tiếp dựa quãng X chung, wave đầu sau VFX không đợi ngưỡng |
| UI/input | `UIManager.ConfigureSystemMenu/LayoutReadableUI`; `StartMenuUI.MakeButton/Label/Layout`; `OpeningArtTheme`; `CustomInteractable`; `ManagementWindowLayout` | Canvas/TMP, desktop input và layout opening có quy tắc riêng; không ghi đè global font/layout tùy tiện |
| Dữ liệu | `Assets/DataScript/EntityDataSO.cs`; `HeroCombatState.Data`; `CombatManager.SetupHeroInfo` | Dữ liệu runtime Hero chính là nguồn renderer đọc; base data, modifier trang bị và stat tính lại không được cộng hai lần |
| Save | `Assets/Script/SaveManager.cs`: ConfigureSaveDirectoryBeforeAwake, Awake, SaveGame, LoadGame, WriteToFile, TryGetLatestSlot | Default /Saves/; custom path khóa sau Awake, không I/O lúc cấu hình; raw JSON cũng qua validator |
| Schema | `EquipmentFoundation.TryValidateSaveVersions`, `EquipmentInventory.Normalize`, `EquipmentRuntime.Normalize`, `EquipmentRefinement.NormalizeMetadata` | Inventory 1, equipment 3, affix 1, forge 2 là phiên bản hiện tại; future guard trước mutation, giữ migration cũ |

## Thứ tự thực thi cần đọc đúng

Attribute thực tế: `EnvironmentManager` -200, `CombatManager` 100,
`GroundPresentation` 200. Environment.Update tick movement và lập kế hoạch camera;
Combat.Update xử lý combat; các LateUpdate tương ứng áp pan, cập nhật presentation.
GameManager điều phối entry/wave ở Update/LateUpdate mặc định. HeroController có
LateUpdate riêng. Không có cam kết rằng Awake/Start giữa các object cùng order
được sắp theo thứ tự xuất hiện trong file hoặc hierarchy.

## Save hiện tại: không biến mong muốn thành bảo đảm

- `SaveGame(EntityDataSO, SaveSlot)` validate → serialize → WriteToFile; chỉ khi
  thành công mới xóa isDirty. Luồng raw JSON trong WriteToFile cũng validate.
- `LoadGame(EntityDataSO, SaveSlot)` kiểm tra JSON/future schema trước gán nhiều
  field nhưng vẫn có thao tác overwrite/normalize trước một số lỗi muộn; không
  tuyên bố direct call là transaction. GameManager.TryLoadSlot dùng bản clone rồi
  chỉ thay live data sau khi Load/snapshot checks thành công.
- `GameManager.AutoSaveRoutine`, `OnApplicationQuit`, `TryManualSave` và
  `SaveManager.TryGetLatestSlot` phải được đọc cùng nhau trước thay Continue.
  Không mặc định AutoSave là snapshot mới nhất, hoặc isDirty chỉ thuộc một slot.
- `EntityDataSO.CompleteWave()` dòng 199–208 phân luồng: worldMapVersion >= 1
  gọi WorldMapCatalog và farm nguyên node; legacy version 0 vẫn tăng mapNumber
  và AssignMap sau năm wave. “Không tự chuyển node” không phải cam kết giống
  nhau cho save legacy. Giữ nhánh tương thích, không sửa trong task skills.
- Khi chết, `EntityDataSO.RestartRegionAfterDefeat()` và RestoreProgress cập nhật
  tiến trình; OnRetryClicked gọi DeployHero và entry hiện tại;
  chính sách checkpoint chi tiết W2.3 vẫn đang thảo luận. Không sửa quy tắc
  New Game/Reset giữ ManualSave1/2 nếu không có yêu cầu mới rõ ràng.
- `Assets/Tests/SaveLoad/PlayMode/README.md` mô tả fixture I/O cô lập; các test
  JsonUtility/guard tại `Assets/Tests/SaveLoad/EditMode/` không thay thế tích hợp
  GameManager/combat/UI đầy đủ. Các test dùng reflection vì runtime ở Assembly-CSharp.

## Environment và các ngoại lệ hiện có

`GroundPresentation.RefreshTerrain` chọn Plains/Forest/Mountain/Desert/Swamp/
Lake/Sea qua TerrainType. Map lạnh TL-01/TL-03/TL-04 chọn Snow; TL-25 chọn Steppe
mà không đổi terrain combat. Sky đứng yên, Ground2/Ground3 0.09/0.03; skyScale 3.0.
Ground mặc định hạ 20 pixel hiển thị, Desert/Swamp hạ thêm 20 qua cấu hình hiện có.
Không dùng thông số cũ trước các commit cập nhật terrain làm kỳ vọng kiểm thử.

## Đang phát triển/chờ nghiệm thu

`README.md` phần đầu và `Docs/PROGRESS_2026-10-25.md` ghi trạng thái mới nhất:
chưa Alpha; HUD, luồng Inventory/Equipment/Skill chưa hoàn chỉnh; artwork ngoài
Plains còn seam; nguyên nhân frame dài sau VFX chưa profile; chưa nghiệm thu
Unity/Windows end-to-end mọi địa hình. Kết quả 51 Edit Mode/3 Play Mode lịch sử
W1 là kết quả người dùng báo ở mốc cũ, không phải test đã chạy lại bởi task skills.

## Chỉ thiết kế/thảo luận

Checkpoint W2.3, HUD W3, ba thẻ Hero/Mana, layout/log mới và phát triển cảnh giới
không được triển khai chỉ từ việc đọc skill. `Docs/Realm_Progression_Design_0.0.54.md`,
`Docs/Stat_System_Design_0.0.54.md` và roadmap có mục dự kiến; đối chiếu mã trước
khi xác định hiện trạng. Nếu README lịch sử khác phần trạng thái mới, ghi rõ khác
biệt và dùng mã hiện tại cùng yêu cầu người dùng để quyết định trong phạm vi task.
