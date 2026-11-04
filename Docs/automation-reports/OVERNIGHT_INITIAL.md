# Overnight — trạng thái ban đầu (05/11/2026)

- Branch: `feature/monster-awareness-ambush`; HEAD `58476a70e8b481c36f1ea72887469d9491635bbb`; working tree sạch. Không checkout/reset/clean, không push trong phiên này.
- Unity ghim 6000.5.0f1; kiểm tra process không thấy Unity.exe đang chạy. Chỉ mở Editor riêng nếu trạng thái này còn đúng lúc khởi chạy.
- Đã có: HUD W3/nền màu-theme, loot receipt, Inventory/Equipment/Forge, Auto/Manual Save và Load. Kết quả cũ chỉ là bối cảnh, không dùng thay validation lần này.
- Gần nhất: `9f1a828` sửa fixture Load theo vũ khí thật; `a389ee0` giải phóng modal chặn Retry; `11aee13` trạng thái B2 khi Pause. Báo cáo trước lưu trong lịch sử Git và các GROUP_0*.md.
- Khoảng trống xác minh: chưa chạy vòng MainGame thật Combat → loot → equip/forge → stats/combat → save/load; chưa kiểm Save/Load khi Inventory/Forge mở. Player/chuột thật trên mã gần nhất chưa chạy.
- Kế hoạch: dùng bản sao MainGame, SaveManager inactive và profile GUID/marker trước Awake; kiểm UI bằng handler/hit-test trên panel thật, combat/loot tự chạy bằng Update thật. Chỉ dùng controlled-loot profile sẵn có trong bản sao, không đổi scene/cờ production hoặc tỷ lệ rớt.
- Tối đa hai vòng sửa–kiểm thử cho mỗi lỗi. Dừng nếu không bảo đảm an toàn dữ liệu/kiến trúc/Git; hoàn thành giai đoạn 4 thì dừng.
- Báo cáo dùng `Docs/automation-reports` là tên canonical hiện có; `docs/...` cùng đường dẫn trên Windows.
