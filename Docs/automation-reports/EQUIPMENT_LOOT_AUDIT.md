# RC4 — Equipment, Loot và Forge

Catalog chính có 13 template. Có 8 slot runtime, gồm hai slot nhẫn chia một template có thể equip cả hai. Ba mẫu Hoàng cấp skill theo ID, ba vũ khí phổ thông và bảy template phòng thủ/tài nguyên/phụ kiện khác. Bùa HP và Bùa ATK cũ thuộc catalog Inventory legacy, được giữ riêng trong bảng CSV. Icon riêng theo template, yêu cầu Level/Cảnh giới và substat template chưa chốt được ghi UNDEFINED. Item level là metadata và khác điều kiện tối thiểu để trang bị.

Item instance có ID ổn định, template ID, rarity, affix có điều kiện và snapshot rèn. Rebuild không biến stat dẫn xuất thành nguồn thứ hai. Normalize giữ object hợp lệ và có guard khi gặp schema tương lai. UI giữ source item theo instance ID để Forge và Save nói về cùng vật.

MainGame vẫn roll vật liệu 60% (1–2) và trang bị 8% độc lập, chỉ Common Thiết Kiếm/Cung Lv1 ATK +4, chọn đều. Exclusion Staff, các mẫu skill và slot phòng thủ giữ nguyên. Loot nhận trước hiệu ứng bay. Duplicate callback và full stack được chặn ở reward layer.

Forge dùng một nguyên liệu cho cấp/rèn thường, 10 cho rèn đặc biệt. Mốc prototype 3/6/12/24/48 và reverse theo snapshot. Không có roll thất bại hoặc hoàn trả. Không mở thao tác Forge mới và không thay Save schema.

Bảng RC4 workbook ghi riêng giá trị MainGame, prototype thử và trường trống. Bộ ItemLootFoundation bao phủ kết quả độc lập, ID trùng, quá giới hạn, stale map receipt, gear/Forge state và future versions. Native smoke Development017 xác minh trải nghiệm qua handler.
