# Phase 1 — Thiên Kiếp và EXP dự trữ

Base `a1b9e64f19c748499bf66277c7569f136625903a`. Checkout riêng `D:\IdleGame\Projects\GameplayFoundationFourPhases`.

- **IMPLEMENTED**. EXP chiến đấu và tu vi độc lập. EXP chỉ vào dự trữ khi realm thực chạm bình cảnh và có Thiên Kiếp được cấu hình. Điều kiện không dùng Level 45. Candidate Save giải phóng EXP sau khi thắng, sử dụng cùng attempt ID với kết quả Thiên Kiếp. Khi thua, EXP giữ nguyên. Retry và Travel giữ component EXP theo cùng Hero.
- **FIXED**. `EntityDataSO.AddExp` bỏ qua lệnh EXP bằng 0 hoặc âm trước khi tính tiến cấp. Thêm test replay, sát Level 999 và hai lần đột phá liên tiếp với cấu hình thử.
- **PARTIAL**. Đường chơi DEV chỉ cấu hình Luyện Khí → Trúc Cơ. Chuỗi đột phá thứ hai hiện được xác minh ở domain fixture. Cảnh giới và Gate tiếp theo cần dữ liệu gameplay riêng.
- **PASS**. Domain UnityStub 52/52. Unity MainGame 6000.5.0f1 exit 0, thử bằng handler trên hồ sơ sở hữu. Kiếm thắng và tay không thua. Đã xác minh lỗi ghi candidate, commit lại, EXP giải phóng đúng một lần, Load terminal, hồi sinh và cleanup. Log `D:\IdleGame\Logs\Unity-MainGame015-20261114-014518.log`.
- Lượt Editor đầu kiểm tra nguồn BGM trước khi fade hoàn tất. Smoke test nay chờ chính clip Thiên Kiếp dừng. Không cần đổi BGM runtime. Unity Search ghi lỗi index ở lượt import đầu.
- Windows Development Player và regression cuối batch chờ ở lần kiểm thử tích hợp.
