# UI & Windows Player Validation — 05/11/2026

## 1. Branch / commit

- Worktree: `C:\Users\hason\.codex\worktrees\loot-vfx-validation\IdleGame`.
- Branch: `feature/monster-awareness-ambush`; bắt đầu `5f29173cdbc2282ebba5f2ff43b0e0aac3332990`, working tree sạch.
- Mã cuối đã kiểm/build: `3886cbfbec994be8d7136165bf4832d8a6551548`.
- Commit riêng: `0fdf267` sửa độ chính xác fixture distance-wave; `fbc1575` sửa fixture primary staff/Magic; `3886cbf` sửa Inventory/Forge và test UI. Commit chứa báo cáo này chỉ bổ sung tài liệu.
- Không push/merge/đổi nhánh. Không thay loot production, Forge reward, Save schema, balance, AI, camera, sprite hoặc scene sản phẩm.

## 2. Ma trận độ phân giải

Ảnh trực tiếp từ Unity **6000.5.0f1**, dùng bản sao cô lập của `Assets/Scenes/MainGame.unity`. PASS trong bảng chỉ là quan sát bố cục, **không phải chứng nhận input**.

| Game View | HUD/button trong màn hình | Inventory/Equipment compact | Forge compact | Inventory/Forge expanded thực tế | Chuột native |
|---|---|---|---|---|---|
| 600×338 | PASS ảnh | PASS ảnh, 5 ô/trang | PASS ảnh, hàng nút không chồng | PASS ảnh, 1160×680 | NOT RUN |
| 800×450 | PASS ảnh | PASS ảnh | PASS ảnh | PASS ảnh, **1360×680** | NOT RUN |
| 1150×647 | PASS ảnh | PASS ảnh | PASS ảnh | PASS ảnh, 1710×680 | NOT RUN |
| 1360×680 | PASS ảnh | PASS ảnh | PASS ảnh | PASS ảnh, 1920×680 | NOT RUN |

- Compact Inventory là fallback khi Game View vẫn giữ kích thước gốc sau yêu cầu mở rộng; expanded được xác nhận bằng kích thước Game View thực. Không coi yêu cầu resize là cửa sổ đã đổi kích thước.
- Dữ liệu trang bị/stock/HP lấy từ Hero runtime trong profile thử. Stress tên dài dùng template Bow hiện có, đổi tên **chỉ trong bộ nhớ fixture**, không sửa catalog/asset/schema. Caption dùng ellipsis; tên dài không đẩy nút hoặc thông tin Forge sang vùng kế.
- Đã chụp Character, HUD, Inventory, Forge; ảnh gồm trước/sau sửa. Trạng thái panel được dựng bằng handler/hit-test để chụp. **Không dùng các lệnh dựng ảnh này làm PASS mở/đóng modal, Pause/Resume hoặc input native.**
- Resize khi loot đang Collecting: đã chạy combat/loot thật, đổi Game View 1360×680 → 800×450 bằng công cụ hiện có; ảnh thấy kiếm tiếp tục bay. Đây là quan sát Editor có điều khiển kích thước, **không phải resize Windows bằng chuột** hoặc nghiệm thu toàn bộ animation.
- Retry: chụp được nút sau khi hạ Hero trong fixture cô lập; bấm Retry native NOT RUN. Travel native NOT RUN; không tự thay luật mở map. Fixture Mono có chạy Retry/Travel, tách khỏi kết luận UI native.

## 3. Những trường hợp dùng input native

**Không có ca gameplay/UI nào hoàn thành bằng chuột hoặc bàn phím thật.** Đã dùng computer-use chọn/quan sát cửa sổ, nhưng Windows Security/Firewall che cả Player và Editor; không gửi click vào game qua hộp thoại này.

Đã đề nghị người dùng tự đóng hộp thoại. Skill `computer-use`, `docs/guidance.md` quy định: “Do not act on security or privacy permission requests.” Không cấp quyền mạng, sửa Firewall, tự bấm Allow/Cancel hoặc suy diễn handler thành native PASS. Ảnh bằng chứng: `outputs/player-validation/firewall-blocked.png`.

## 4. Windows Player

- **PASS build** Windows x64, Unity 6000.5.0f1, backend hiện có **Mono2x**, MainGame. Windows Build Support đã có; không cài module/package/SDK.
- Bản cuối từ `3886cbf`: `BuildResult.Succeeded`, **0 errors / 49 warnings**, 604,807,656 bytes, `BuildOptions.None`.
- Executable: `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\player-validation\final-player\IdleGameValidation.exe`.
- Bản build thử dùng company `IdleGameValidation`, product GUID riêng, đã đối chiếu `app.info` và marker trước lần chạy. Saves và PlayerPrefs tách khỏi profile sản phẩm; company/product trong ProjectSettings được khôi phục. Profile bản cuối: `C:\Users\hason\AppData\LocalLow\IdleGameValidation\Run-55f517ecd65a4493adac9ae867f73de8`.
- Hai bản trước sửa UI (Development và Release) đã tạo process/cửa sổ nhưng đều gặp hộp thoại Firewall. Đã dừng đúng hai process do tác vụ mở. **Startup tương tác, UI, Combat và Save/Load trong Player: BLOCKED/NOT RUN.** Executable cuối chưa launch lại vì trở ngại này vẫn còn; không lấy build PASS làm gameplay PASS.

## 5. Lỗi đã sửa

- `Assets/Script/InventoryPanelUI.cs`: ở 600×338, ô túi 25 px khiến chữ tràn hàng; dùng một hàng 58 px và phân trang có sẵn. Chặn chữ tràn khỏi khung bằng ellipsis, giữ dữ liệu và hitbox.
- Cùng file: Forge thấp từng ép thumbnail vào header và dồn footer khỏi frame. Bố trí thumbnail cạnh mô tả ở cửa sổ thấp, giữ hàng nút đọc được; khung cao tính đủ chiều cao body/footer sau header.
- Cùng file: giới hạn chữ làm lộ trường hợp caption nút nhỏ không đủ một dòng sau padding. Giảm riêng padding dọc của caption stretch; không đổi kích thước nút/callback. Ảnh sau sửa thấy lại đầy đủ `+`, `−`, Chi tiết/So sánh và điều hướng.
- `Assets/Tests/Presentation/PlayMode/InventoryComparisonTests.cs`: thêm bốn ca đúng kích thước yêu cầu, kiểm bag page/cell, phần tử Forge trong frame, tách header, chiều cao nút và khoảng chữ theo metrics font thật. Không thêm dependency TMP vào asmdef; dùng bridge reflection hiện có.
- `Tests~/DistanceWaveRegression.cs`: lỗi tái hiện tại `raw=999.99996948242188`, HUD float đã làm tròn thành `1000`. Test so ngưỡng bằng bộ đếm double như runtime, giữ nguyên ngưỡng/tolerance và assertion không sinh wave sớm.
- `Tests~/MagicPersistenceRegression.cs`: primary sau entry lấy mode từ gear nên không thể ép tay không thành mage. Fixture trang bị staff qua handler thật, kiểm live Hero data và identity đạn `staffBasic && !magic`. Runtime đã quy định staff là đạn đánh thường; recovery hủy nó. Giữ chín ca legacy Magic và tám ca primary/entry, không đổi Combat để thỏa test cũ.

## 6. Kiểm thử và giới hạn

| Kiểm tra | Kết quả thực chạy |
|---|---|
| Mono `--distance-waves` | **PASS 34/34** sau sửa fixture |
| Mono `--magic-persistence` | **PASS 17/17** sau sửa fixture |
| Unity Play Mode `InventoryComparisonTests` | **PASS 13/13** sau sửa bố cục |
| Bốn ca compact có thêm assertion caption | **PASS 4/4** trên mã caption cuối; không cộng thành 17 ca riêng |
| Mono `--inventory-compare` | **PASS 18/18** trên mã UI cuối |
| Windows build mã UI cuối | **PASS**, 0 lỗi / 49 cảnh báo |
| Input native, Windows gameplay/Save-Load | **BLOCKED**, chưa chạy được |
| Travel/Retry bằng chuột, native resize khi loot bay | **NOT RUN** |

- Hai FAIL lịch sử đã tái hiện trước sửa. Những lỗi biên dịch trong phần chuẩn bị fixture/test (tham chiếu GameManager/TMP, shadow biến tạm và Vector2 operator của Mono stub) đã được xử lý; không phải lỗi runtime sản phẩm. Native xác nhận font/RectTransform thật; Mono không thay thế Player.
- Sau 13/13, ảnh phát hiện lỗi caption mới nên chỉ chạy lại bốn ca ảnh hưởng cùng nhóm Mono UI; không lặp toàn bộ Save/Load, loot, AI hoặc các bộ đã PASS mà mã không đổi.
- **Lịch sử, đã được xử lý ở mục 9:** auto-review từng chặn vòng bổ sung `EscapeLegacyMagic`. Người dùng đã cấp ngoại lệ riêng; ca B2 legacy mới PASS, nhóm Magic hiện 18/18. Đề xuất tạm cũ ngoài Git không được áp dụng nguyên xi.
- `git diff --check` PASS; checker metadata được ghi riêng trong thư mục tests. Không đọc/ghi/xóa save thật. Không giữ helper build hay thay đổi staging trong diff sản phẩm.

## 7. Ảnh / log

Gốc artifact: `C:\Users\hason\Documents\Codex\2026-11-04\truy\outputs\player-validation`.

- `before/`, `after/`: HUD, Character, Inventory/Forge compact/expanded; tên file có kích thước hoặc `expanded-from-<width>`. `after/flight-resized.png`, `after/retry-visible.png` là quan sát có setup, không phải thao tác native.
- `tests/ui-round2.xml`, `tests/caption-round2.xml`: Unity; `distance-waves-round2.log`, `magic-staff-recovery.log`, `inventory-caption-round2.log`: Mono; `visual-staging-summary.txt`: các lệnh dựng ảnh.
- `final-player/build-summary.txt`, `final-player/IdleGameValidation_Data/app.info`: build và identity.
- Log chi tiết ngoài Git: `C:\Users\hason\Documents\Codex\2026-11-04\truy\work\player-validation\windows-final.log`, `player.log`, `player-release.log`, `editor-caption-final.log` và các log round tương ứng. Không commit log, binary, save hoặc scene copy.

## 8. Người dùng cần kiểm tra thủ công

1. Tự đóng hộp thoại Windows Security; không cần tự cấp quyền mạng chỉ để xác nhận tác vụ này.
2. Chạy executable thử có identity riêng ở trên; kiểm startup → UI → combat → Manual/Auto Save → thoát/khởi động lại/Load trong **profile thử**. Không dùng bản TT1 hoặc save thật để nghiệm thu.
3. Bấm button, paging, chọn/trang bị/tháo, mở/đóng Forge/modal, Pause/Resume ở bốn kích thước; kiểm caption và chặn click xuyên.
4. Kiểm resize Windows lúc loot bay, Retry và Travel với điều kiện mở map hiện có. Loot production vẫn Off/NoDrop; không bật cờ sản phẩm để thay luật thưởng.
5. Legacy AoE + Escape đã có regression riêng ở mục 9; không cần tiếp tục vòng sửa tự động cho ca này.

Khi mở Editor, chọn rõ `Assets/Scenes/MainGame.unity`, không chơi trên `InitTestSceneFoundationValidation` là bản sao CLI. Bản hiện tại đủ điều kiện build và xem thử UI; chưa đủ bằng chứng nghiệm thu input/Windows gameplay. Hoàn thành báo cáo và dừng, không push/merge.

## 9. EscapeLegacyMagic — follow-up được cấp phép

- Branch giữ nguyên; bắt đầu tại `c79f0df`. Commit test mới: **`450801d9eb4a971cfabe47393f1e6d23c01b09c6`**. Không đổi mã trong Assets, Save schema, balance, gameplay hoặc chính sách hệ thống.
- **Nguyên nhân gốc:** assertion lịch sử `same-map recovery preserves visual` được dùng khi projectile thực tế là primary staff (`staffBasic=true`, `magic=false`). `Launch` phân biệt staff đánh thường với legacy AoE; `RetireProjectile` chỉ giữ visual khi `magic=true`. Vì vậy kỳ vọng giữ Magic không hợp lệ cho staff. Ở HEAD `c79f0df`, case staff đã PASS; case tên `EscapeLegacyMagic` chưa tồn tại và chưa từng có kết quả FAIL riêng.
- **Phân loại:** lỗi giả định/thiếu coverage của fixture; **không tái hiện lỗi production**. Không sửa runtime để ép PASS, không đổi các assertion hợp lệ của 17 ca trước.
- **Điều kiện kiểm riêng:** MainGame entry fixture hiện có, thêm B2 `RangedMagic` qua `RegisterAdditionalHero`, giữ nguyên binding vũ khí của primary; đặt target trong tầm. Xác nhận duy nhất một projectile thuộc đúng B2 với `magic=true`, `staffBasic=false`, rồi gọi `GameManager.RecoverEscapedWave()` khi nó đang bay. Không ép primary sang chế độ legacy như đề xuất tạm cũ.
- **Kết quả:** ca riêng `--escape-legacy-magic` **PASS 1/1 ngay lần đầu** trên runtime không đổi của `c79f0df`. Recovery xóa khả năng damage, chuyển đúng cùng visual sang detached flight, không nhảy clock/biến mất sớm, kết thúc một lần kèm burst; không tăng EXP/progress/gear và hai slot Manual của fixture giữ nguyên byte. Không có vòng sửa gameplay sau tái hiện vì không phát hiện lỗi.
- **Hồi quy trực tiếp:** `--magic-persistence` **PASS 18/18** (ca mới + 17 ca cũ), chạy một lượt sau đó để kiểm cleanup và hai loại projectile cùng tồn tại đúng hợp đồng. Đây đều là **Mono/UnityStub**, không phải Unity/Windows acceptance.
- **File:** `Tests~/MagicPersistenceRegression.cs` thêm test B2 với guard identity/lifecycle/reward; `Tests~/MotionRegression.cs` thêm selector riêng; báo cáo này cập nhật trạng thái. Fixture dùng đường dẫn GUID tạm hiện có, không truy cập save người chơi.
- **Chưa xác minh:** destroyed-object semantics và hình ảnh trong Unity/Player, input native. Không chạy lại UI/Player hoặc can thiệp Firewall; các giới hạn ở mục 3–4 giữ nguyên. Không kết luận các phần đó PASS từ Mono.
- **Log:** `C:\Users\hason\Documents\Codex\2026-11-04\truy\work\escape-legacy-magic\targeted.log`, `magic-related.log`; bản sao và summary ở `outputs/escape-legacy-magic/`. `git diff --check` PASS. Hoàn thành ngoại lệ đã cấp, không push/merge và dừng.
