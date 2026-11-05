# Nhóm 03 — Phản hồi Pause cho nút B2 thử nghiệm

- Ưu tiên/phạm vi: P2, chỉ trạng thái hiển thị/tương tác của nút B2 trong Công cụ.
- Mốc đầu: `a389ee0`, nhóm 2 đã commit và working tree không có thay đổi lạ.
- Bằng chứng: GameManager.CycleTestCompanions từ chối khi Time.timeScale = 0, nhưng UI vẫn hiện nút gọi/thu hồi như đang dùng được.
- Tiêu chí: khi Pause, nút vô hiệu và ghi “Tiếp tục để sử dụng”; Resume trả đúng nhãn gọi/thu hồi, giữ callback, chặn click-through và không đổi dữ liệu.
- Sửa: đồng bộ riêng Pause của control, áp màu vô hiệu qua theme hiện có; cache count/trạng thái để không ghi lại text mỗi frame. Callback và các guard spawn khác không thay đổi.
- PASS: 2/2 Unity Play Mode (ô trống và đủ hai B2), gồm reapply theme/Resume/hit-test/callback một lần/JSON không đổi; Mono `--formation` 39 nhóm.
- FAIL: 0; vòng 1 đạt, không có vòng hai. NOT RUN: Windows Player/click thật; native callback marker xác minh dispatch, không coi là spawn B2 end-to-end.
- Risk review: thấp, không chạm GameManager, AI, combat, camera, inventory hoặc save. Đã đủ 3 nhóm; dừng triển khai, chỉ chốt báo cáo cuối.
