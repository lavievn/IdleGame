# W2.2o — Hạ phần hiển thị Ground

`GroundPresentation.groundDropPixels` mặc định 20, chỉ áp dụng cho bộ artwork
địa hình (ban đầu Plains, sau tích hợp thêm Forest/Mountain). Tính là pixel màn hình
Unity `Screen`, quy đổi qua `ManagementWindowLayout.BattleCanvasScale`, không phải
20 world unit hoặc 20 đơn vị RectTransform ở mọi độ phân giải.

Với BattleArea rộng logic 1920: cửa sổ 600/800/1150 lần lượt dịch hình xuống
64 / 48 / 33,3913 đơn vị logic, cùng tương ứng 20 pixel hiển thị. Resize tính lại;
khi mở inventory vẫn dùng chiều rộng gameplay compact. Mọi tile Ground cùng
mức hạ. Sky, hai lớp background, sàn logic và vị trí chân nhân vật không đổi.
Có thể chỉnh giá trị trên GroundPresentation trong Inspector lúc chạy.

`Tests~/run.sh --plains`: 11 PASS bằng Mono/UnityStub, gồm ba ca mới kiểm tra
quy đổi pixel tại 600/800/1150 và không dịch các lớp/nhân vật khác. Chưa chạy
Unity/Windows Player; cần nghiệm thu vị trí mặt đất thực tế dưới chân sprite.
