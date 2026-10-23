# W2.2m A — Enemy sprites

Nguyên nhân đã xác nhận: AttachMonster trước đây chỉ truyền ToClass(attackMode)
vào Init, tạo path Enemy_Swordsman/Archer/Mage. Không có đường nào tạo path
Enemy_Wolf. InitRandomAttackMode chạy trong OnEnable trước khi StartBattle gán
identity; SetIdentityVisual chỉ tint Image cũ đã bị bridge ẩn, không bind species.
Không phải lỗi tên file hay importer chưa được chứng minh bằng Cloud.

Audit Resources/CultivationSprites: 12 Enemy PNG: Swordsman, Archer, Mage,
Wolf × Normal/Attack/Hit. Tất cả 900×600, TextureImporter textureType=8,
spriteMode=2, metadata có GUID hợp lệ không trùng. Full-frame Texture2D loader
không phụ thuộc các automatic Sprite slices. Các bộ role Enemy là bản Hero
đã phủ đỏ sẫm trong PNG; Wolf có artwork riêng. Không sửa PNG hoặc metadata.

Theo yêu cầu bổ sung: mỗi quái Melee chọn 50/50 giữa Wolf và dạng Hero cận
chiến (Enemy_Swordsman đã xử lý màu). Cung/phép giữ artwork theo role. Không
dựa vào race/animal để đổi combat identity. Một RNG System.Random riêng chọn
presentation sau khi OnEnable xác định attackMode; không tiêu thụ Unity
Random của combat/AI. Giữ family suốt lượt sống; rebind identity/pose không
reroll. Pool activation xóa lựa chọn cũ để chọn lại; mode thay đổi cũng bind
lựa chọn phù hợp. Không đổi attack mode, tỷ lệ spawn hoặc dữ liệu map.

Thứ tự mỗi pose: Enemy_species → Enemy_role → Hero_species tinted → Hero_role
tinted → source Image khi không có Normal. Enemy PNG luôn Image trắng (không
phủ đỏ hai lần). Chỉ Hero fallback có tint (.8,.7,.7); không sửa HP/text.
Wolf dùng registration chân source (300,34), đo soles row565/566, thay vì mốc
Hero (300,10). Chỉ pivot Image thân thay đổi; root/hitbox/position/scale/HP bar
giữ nguyên. Mỗi pose fallback sử dụng registration tương ứng artwork đó.

6 ca Mono mới kiểm tra hai family Melee × 3 pose, ranged không đổi, tint,
foot registration, pool chọn lại và thiếu pose/fallback. RNG fixture có seed
cố định, không dựa vào xác suất để ép đủ hai family. Nhóm sprite cũ giữ nguyên
assertion. Unity/Windows chưa chạy: kiểm tra nhiều quái Melee thấy cả hai dạng,
Normal/Attack/Hit cùng family, chân/kích thước/hướng, wave/Retry/Load và pool.
