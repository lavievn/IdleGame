# Nhóm 02 — Modal cũ chặn Retry / trạng thái entry

- Ưu tiên/phạm vi: P1, vòng đời Menu và bảng chi tiết không tự Pause (Hero/log/damage).
- Mốc đầu: `9f1a828`; nhóm 1 đã commit, không có thay đổi ngoài nhiệm vụ.
- Tiêu chí: khi Game Over/entry/title/confirmation chiếm màn hình, hai modal này không giữ hit-test; Retry nhận đúng một callback; không đổi Pause.
- Bằng chứng: HitControl lọc input vào systemMenu/damagePanel còn active, trong khi GameOver được vẽ phía trên. Refresh chỉ dọn Inventory trước thay đổi này.
- Sửa tối thiểu: dùng CloseDamageInfo/CloseSystemMenu khi GameManager không còn cho hiện gameplay HUD; không đổi GameManager, combat, save hoặc Time.timeScale.
- PASS: 9/9 Unity Play Mode trên Unity 6000.5.0f1 (8 tổ hợp modal/Pause + 1 entry/snapshot); Mono `--desktop-ui` 59 nhóm, gồm toàn bộ Load đã được mở khóa bởi nhóm 1.
- FAIL: 0. Vòng 1 đạt; không có vòng sửa thứ hai. NOT RUN: click Windows thật và Player.
- Native test không tạo SaveManager; tái hiện ranh giới trạng thái UI và routing Retry, không gọi gameplay Retry/Save. Mono Load dùng profile tạm riêng sẵn có.
- Risk review: thấp, chỉ giải phóng các modal không sở hữu Pause tại ranh giới hiện có. Không có lỗi Save/Load chưa rõ nguyên nhân; đủ an toàn thực hiện một nhóm P2 độc lập cuối.
- Tạo/đổi snapshot không xóa lịch sử log; mở lại detail lấy snapshot mới. Không dựng lại hierarchy hoặc đổi ưu tiên input khi gameplay vẫn hoạt động.
