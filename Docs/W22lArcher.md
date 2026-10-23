# W2.2l — Archer 3 frame

Nguồn ZIP nguyên vẹn nằm ở `SourceAssets~/W22l/sprites_archer.zip`, ngoài thư
mục import Unity. `Tools/prepare_archer_frames.py` tái tạo 9 PNG trong
Resources/CultivationSprites; manifest `SourceAssets~/W22l/archer-registration.json`
ghi checksum, kích thước, crop, chân nguồn, hệ số scale và file đầu ra từng ảnh.

Ảnh nguồn có ba cỡ: 900×600, 1448×1086, và Attack 530×407/494×394. Alpha thật
có ở cả 9 ảnh. atk1/atk3 có RGB caro ở vùng alpha=0; ảnh ghép trên nền màu
không hiện caro. Không chroma-key xám (sẽ làm mất chi tiết trang phục).
Clear RGB vô hình và giữ alpha/antialias; crop vùng support alpha>10/255 cộng
8px, loại nhiễu alpha rất mờ ở xa thân. Không tẩy hiệu ứng mũi tên.

Canvas đầu ra thống nhất 900×600, registration chân (300,10) từ dưới trái,
khớp bridge hiện có. Scale đồng nhất hai trục, không co theo vũ khí/effect.
Mốc crown/sole được đo và xem đối chiếu trên ảnh có lưới; sai số làm tròn
raster khoảng 1px. Normal đứng cao 376px như Normal gốc; Attack khoảng
366–376px; Hit cúi người khoảng 338–340px, không kéo người cúi thành dáng đứng.
Đây là registration thủ công, cần nghiệm thu cảm giác chuyển pose trong Unity;
không khẳng định hình dáng các ảnh AI riêng lẻ giống nhau tuyệt đối.

TextureImporter: single sprite, alpha transparency, no mipmap, no compression,
NPOT giữ nguyên, clamp/bilinear. Bridge vẫn tạo full-frame Sprite từ Texture2D,
không phụ thuộc slicing tự động. Mage/Swordsman/Enemy PNG cũ không đổi.
