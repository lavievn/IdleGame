# Overnight — 2. Loot / Inventory / Equipment

- Thêm `Assets/Script/MainGameFoundationProbe.cs`: verifier Editor chạy CLI rõ ràng, không startup hook và không có trong Player. Dùng bản sao MainGame ignored, SaveManager inactive và profile GUID/marker trước Awake; kiểm tổ tiên trước con để từ chối reparse. Không đọc/ghi/xóa save người chơi.
- PASS trong profile controlled-loot hiện có: combat Update thật hạ quái → cấp instance duy nhất → VFX; gọi lại callback chết thật không cấp thêm. Pause giữ nguyên tuổi/vị trí VFX; thứ tự rơi ≥0,6 s, chờ tới ≥1,1 s, bay tách ground và thu nhỏ; target thiếu dùng mép phải. Kết thúc VFX không cộng thêm đồ.
- Chọn đồ trong Inventory thật; equip/unequip không mất hoặc nhân bản, equip lại tăng chỉ số. Combat tự động ghi nhận tổng sát thương nguồn 1010 từ trang bị thật. Sau Load và rèn, nguồn sát thương 1012 được combat sử dụng.
- Mono `--loot-visuals`: 23/23 PASS (`LootDropPresentationTests`, `CombatLootTests`), gồm NoDrop/thất bại/callback cũ/cleanup và kill → Inventory → equip.
- Giới hạn: `controlledLootTest` production vẫn mặc định Off; `CombatLootRewards` trả NoDrop khi tắt. Không có đặc tả loot production để tự bật hay thiết kế tỷ lệ. Vì vậy vòng có thưởng được xác nhận trong profile thử, chưa kết luận vòng production hoàn thiện. Resize/input native và mọi tình huống trực quan không nằm trong ca đã chạy vẫn NOT RUN.
- Rủi ro trước nhóm sau: thấp; chỉ thêm verifier/test, không đổi luật thưởng, camera, AI, EXP hoặc schema. Không push.
