# Phase 4 — Tinh Huyết và căn cơ

**Domain đã kiểm chứng. Runtime PENDING_DESIGN.**

- Bỏ thời lượng Thiêu Đốt hardcode. Duration do `CultivationRules` cung cấp. Thiếu thời lượng thì từ chối kích hoạt trước khi tiêu charge hoặc hồi HP. Fixture 30 giây kiểm tương thích, fixture 8 giây kiểm thời lượng có thể thay đổi.
- Các kiểm thử giữ khóa trước Kim Đan, cap 1–4 ở Kim Đan, HP sống dưới 30%, hồi đầy lần đầu, sức mạnh tạm thời +1 realm cho ability hỗ trợ, gấp đôi chỉ số tuyệt đối theo whitelist, top-up chỉ cộng thời gian, trừ 10% căn cơ tham chiếu mỗi charge và settle đúng một lần. Đã kiểm deficit chuyển thành injury, hồi phục căn cơ bằng tu luyện, treatment độc lập, death/travel/retry và nạp lại ở cấp domain.
- **PASS**. Domain 57/57, log `D:\IdleGame\Logs\GameplayFoundation-Phase4-Domain.log`. Unity native JsonUtility/fixture file qua `DualProgressionV06Verifier.RunNative`, log `D:\IdleGame\Logs\Unity-Native-20261114-020024.log`. Assertion cũ đòi khóa cả tu luyện production, đã sửa để xác minh đúng khóa riêng của Blood.
- **PENDING_DESIGN**. Chưa có Gate tới Kim Đan, thời lượng production, rate cô đọng, rate dưỡng thương, bảng capacity cho các realm cao và policy ghi HP/nợ nguyên tử khi Active, crash hoặc đổi slot. Các profile chơi thật vẫn chặn Blood. Domain fixture không xác nhận gameplay MainGame hay Windows của Thiêu Đốt.
