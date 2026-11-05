# Overnight — 1. UI/HUD

- Phạm vi: kiểm UI đang có trên MainGame thật; chỉ sửa nếu xác nhận lỗi. Không phát hiện lỗi chức năng cần sửa trong các ca đã chạy.
- PASS bằng handler/hit-test thật: Pause/Resume, Character/đóng, World Map/đóng, bật/tắt log, Menu → Tools → âm lượng/đóng và DEVB/hủy. Inventory mở rộng cửa sổ, che HUD phía dưới, đóng và khôi phục trạng thái Pause trước đó.
- Inventory dùng dữ liệu Hero runtime. Nền màu mặc định kiểm với art theme tắt; không thay asset, theme hoặc callback production.
- Hình MainGame 800×450 và Inventory mở rộng 1360×680 nằm trong `outputs/overnight-foundation/` của workspace. Chưa bấm chuột thật/Windows Player; không coi handler là bằng chứng kiểm input native. Không chạy lại bộ ảnh 600/800/1150 vì không sửa layout; kích thước 600 và 1150 trên mã hiện tại là NOT RUN về trực quan trong phiên này.
- Native `InventoryComparisonTests`: 9/9 PASS, gồm thay nhẫn, preview không mutate và kiểm vùng chi tiết không chồng nút tại các Rect kích thước test. Đây không thay thế ảnh trực quan.
- Rủi ro trước nhóm sau: thấp; không thay UI/gameplay/save. Giữ nguyên các chức năng đã triển khai; Võ học vẫn vô hiệu đúng đặc tả.
