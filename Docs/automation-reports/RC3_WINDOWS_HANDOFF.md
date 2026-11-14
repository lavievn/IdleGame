# RC3 Windows Development — Bàn giao

Ưu tiên mở `D:\IdleGame\Builds\RC3OvernightIntegration\build\Play-RC3-Isolated.cmd`. Launcher dùng hồ sơ riêng, D3D11 và cửa sổ 800×450. EXE là `IdleGameV10.exe` cùng thư mục. Không chuyển riêng EXE khỏi `IdleGameV10_Data` và các DLL đi kèm.

## Nghiệm thu bằng chuột và tai nghe

1. Chơi mới. Đọc sáu câu mở đầu rồi chọn Nhận lời. Đóng hộp thoại và nhấn tên map trên HUD. Nhấn thử ô khác, rồi nhấn ô Cô Hồn Lâm được đánh dấu. Xác nhận game chỉ Travel ở lần nhấn đích, vào combat tự động và hiện mục tiêu lá bạc.
2. Lặp ở 600×338, 800×450, 1150×647. Kiểm HUD, map, menu, Inventory, Forge và đóng panel. Thử từ chối nhiệm vụ trong một lượt riêng.
3. Nghe Coast, Meadow, rừng. Dùng nút preview đổi Day/Night và Clear/Rain/Storm. Kiểm chuyển tiếp, âm nền, độ dễ nghe của overlay và mức âm lượng Meadow. Kiểm riêng Huyền Ảnh Cốc, nơi RC3 vẫn dùng Primeval khi khô.
4. Lưu tay rồi Load. Thử Retry sau khi thua và kiểm lịch sử map, quest, trang bị, Qi và kỹ năng. Khi đạt Viên mãn, nhận EXP chiến đấu vào dự trữ rồi thử Độ Kiếp. Thắng giải phóng EXP một lần. Thua giữ dự trữ. Thoát rồi mở lại launcher để kiểm Save.
5. Tinh Huyết vẫn khóa. Chưa có hành động để thử trong RC3.

## Rollback

Đóng RC3 và mở lại bản PR #28 ở `D:\IdleGame\Builds\GameplayFoundationFourPhases\build\IdleGameV10.exe` hoặc launcher cũ của RC2/11-11 với hồ sơ của chính bản đó. Giữ thư mục hồ sơ RC3 tách biệt. Source rollback dùng checkout đã có hoặc ref PR #28 `8aaff49397af73a8f705c27dd094317c7bcee1ae`. Không chép Save từ RC3 qua profile cũ.
