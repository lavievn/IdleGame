# Overnight — 4. Tích hợp / cách tái lập

- Lần cuối: Unity **6000.5.0f1**, MainGame Update thật, profile `.utmp/foundation-56a543746eb545d9a6d1ec768723ed18`. `result.json` không có failure; cả 4 pha của verifier PASS trong cùng phiên. Không dùng kết quả cũ thay kết quả này.
- Chuỗi đã chạy: khởi tạo → auto combat/kill → cấp gear + VFX → Inventory/equip/unequip → damage trace → Forge trên đồ đang đeo → Manual/Auto Save → Load → combat dùng chỉ số đã khôi phục → kill NoDrop không tăng gear.
- Tổng test bổ sung: native Play Mode 12/12; native Edit Mode 51/51; Mono Forge 4/4, loot 23/23. Không có ca đang FAIL ở lần chốt. Các lỗi đầu của verifier và cách sửa ghi ở báo cáo pha 3, không che bằng assertion yếu hơn.
- Ảnh thật: `outputs/overnight-foundation/phase1-hud.png`, `phase1-inventory-default.png`, `phase2-drop-paused.png`, `phase2-landed.png`, `phase2-flight-fallback.png`, `phase2-awarded-equipped.png`, `phase3-forged-equipped.png`, `phase3-loaded-inventory.png`, `phase4-final-restored-loop.png`. Cùng thư mục có result.json, XML native, log Mono và tóm tắt tích hợp; không commit log/save/ảnh tạm vào Git.
- Quan sát ảnh ở 800×450 và mở rộng 1360×680. Không tuyên bố nghiệm thu mọi frame VFX, chuột thật, Windows Player, hoặc ảnh 600/1150 trên mã hiện tại. Pha 4 tổng thể **PARTIAL** vì profile thử dùng cờ loot có sẵn và nguyên liệu từ công cụ dev, chưa có đặc tả vòng thưởng production.

## Tái lập an toàn

Đóng phiên Unity đang có trước khi chạy. Không gắn component vào scene gameplay thông thường. Chạy Editor ghim với `-projectPath <worktree> -foundationValidationRun -foundationSourceHead <HEAD> -executeMethod MainGameFoundationProbe.RunIsolated -logFile <log ngoài repo>`. Verifier từ chối scene bẩn/Play đang chạy; tạo bản sao scene ignored và thư mục save GUID mới, cấu hình SaveManager trước Awake. Không dùng cờ `-batchmode -nographics` cho verifier cần Game View/screenshot này.

Sau khi chạy, mở rõ `Assets/Scenes/MainGame.unity` trước khi chơi thử. Last Scene của Editor có thể còn trỏ bản sao `InitTestSceneFoundationValidation`, chỉ dành cho verifier CLI; không dùng nó làm scene sản phẩm. Profile/evidence owned được giữ lại, không dọn bằng cách đụng tới Saves người chơi.

Không push, đổi nhánh, merge, thay cờ production, schema, balance, AI, camera hoặc artwork. Hoàn thành pha 4 và dừng; các giới hạn nghiệm thu chuyển vào FINAL_REPORT.
