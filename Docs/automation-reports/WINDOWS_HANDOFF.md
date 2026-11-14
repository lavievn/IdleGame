# RC4 Foundation — Chơi thử trên Windows

Mở `D:\IdleGame\Builds\RC4Foundation\build\Play-RC4-Isolated.cmd`. Launcher chạy cửa sổ 800×450, chọn Direct3D 11 và dùng một hồ sơ riêng được lưu lại qua các lần mở. Giữ nguyên cả thư mục `build` cùng `IdleGameV10_Data` và các DLL. Chạy launcher để vào đúng hồ sơ kiểm thử.

## Kiểm tra bằng chuột và tai nghe

1. Chơi mới tại Hàn Vụ. Đọc hội thoại, thử hai lựa chọn nhận hoặc từ chối. Sau khi nhận, nhấn tên map, chọn khu vực và vào Cô Hồn Lâm. Thử HUD, Hòm đồ và Lò rèn ở 600×338, 800×450 và 1150×647.
2. Nhấn **Chẩn đoán · F8**. Trong **Environment Audio**, thử Day/Night và Dry/Light/Heavy/Storm. Đọc dòng tên OGG và lý do lớp được chọn, im hoặc chờ sự kiện. So sánh Morning ở Cô Hồn Lâm, Cricket ở map Meadow, và hai loop Rain tại Coast. Thử Travel khi mưa đang fade.
3. Chọn clip cần nghe riêng, thử Audition, Mute/Solo, chỉnh gain và Export JSON. Cột volume phản ánh mức của AudioSource sau fade. **Auto** xóa preview thời tiết/giờ. **Reset** xóa luôn gain thử. Xem [hướng dẫn debug](AUDIO_DEBUG_USER_GUIDE.md) để nạp lại preset.
4. Qua tab **Combat & Progression**. Thử đổi Kiếm/Cung/Trượng/Tay, đánh và dùng một võ học. So chỉ số, đòn đánh, loot và Mastery với hành vi nhìn thấy. Dùng Save/Load và Retry để kiểm tra kết quả.
5. Khi đủ điều kiện, thử Thiên Kiếp. Kiểm tra nhạc và sấm riêng. Thua giữ EXP dự trữ, thắng giải phóng một lần. Thoát rồi mở lại launcher để kiểm tra Save và ghi riêng lỗi thoát nếu có.

## Quay lại RC3

Đóng RC4 và mở `D:\IdleGame\Builds\RC3OvernightIntegration\build\Play-RC3-Isolated.cmd` như trước. Tham chiếu source RC3 là `5a22ed3f9c90cc173726b686efffb0baa92489e2`. Mỗi bản tiếp tục dùng profile riêng của nó.

Xem [báo cáo tích hợp](RC4_FOUNDATION_FINAL_REPORT.md) để đối chiếu SHA, log và kết quả chạy.
