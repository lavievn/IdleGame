# W2.2i — chẩn đoán dịch hình sau VFX

**Chưa xác định nguyên nhân trên Unity thật. Không có bản vá tọa độ, camera,
layout hoặc animation trong thay đổi này.** Người dùng đã nghiệm thu Camera
Multi-Zone; phép kiểm tra Mono mới không thay thế bằng chứng render của Unity.

## Những gì đã đối chiếu

- `CultivationSpriteBridge.ApplyLayout()` dùng điểm chân nguồn `(300,10)` trên
  toàn khung sprite 900×600 làm pivot, anchor dưới giữa, vị trí con bằng 0. Flip
  chỉ áp dụng lên Image thân. Các lần ApplyLayout dùng cùng phép đăng ký này.
- `HeroSpawnVfx.Advance()` neo vòng sáng nguồn tại UV `(0.5,0.09)` vào cùng chân
  Hero; glow có tâm cao hơn chân một nửa chiều cao thân. **Tâm glow không phải
  điểm chân** nên không dùng chênh lệch tâm glow để kết luận lỗi.
- `HeroSpawnVfx.Finish()` đổi alpha, khôi phục `Graphic.enabled`, ẩn VFX; không
  ghi transform. `HandleAnimation()` và `AttackFeedbackRoutine()` có đổi scale
  root; cần đối chiếu biên Image và điểm chân thay vì chỉ nhìn pivot/root position.
- Chưa có bằng chứng chứng minh layout, scale, đăng ký PNG, thứ tự rebuild UI,
  hoặc một transform cha là nguyên nhân của hiện tượng người dùng quan sát.

## Thu log Unity thật

1. Trên HeroController của Hero chính, bật **Trace Visual Position Changes**.
   Giữ **Render Trace Frame Count = 12** để lấy frame bàn giao và ít nhất 10
   frame sau đó. Không cần bật log logic cũ: snapshot mới đã chứa pan/Ground.
   Các cờ này mặc định tắt; không sửa scene/prefab để bật vĩnh viễn.
2. Bật cờ trước khi xác nhận Chơi mới/Tiếp tục. Thực hiện một lần để tái hiện;
   tắt cờ sau khi thu thập. Không dùng save quan trọng để thử Load/Reset.
3. Lấy các dòng **`[HeroRender]`** từ `VFX Begin before` đến
   `Gameplay release after first wave`, tiếp tục ít nhất 10 frame. Xuất log từ
   Console/Editor.log hoặc Player.log, giữ đầy đủ dòng, số frame và stage;
   không chỉ lấy `[HeroPosition]`. Đính kèm đoạn quay đúng thời điểm bị dịch.
4. Log có các mốc trước/sau Begin, sau mỗi Advance/layout, trước/sau Finish,
   trước/sau mở khóa và sinh wave, LateUpdate order 32000 (sau Ground order 200),
   mỗi `Canvas.willRenderCanvases` và `WaitForEndOfFrame`. Callback Canvas có thể
   chạy nhiều lần trong một frame, kể cả do ForceUpdateCanvases hiện có.
   Nó không bảo đảm là callback cuối cùng; đối chiếu thêm **EndOfFrame**.
5. `WaitForEndOfFrame` có thể ngừng khi chuyển từ Game View sang Scene View và
   không chạy trong một số chế độ batch. Nếu thiếu stage này, báo rõ; không coi
   LateUpdate/Canvas callback là phép đo chắc chắn sau toàn bộ render.

Probe chỉ đăng ký callback/coroutine khi bật cờ và có actor active. Cửa sổ đo
được gia hạn theo VFX thực tế, tối đa 120 frame mỗi lần arm; sau Finish/release
giữ 12 frame theo mặc định. Hết hạn, tắt cờ, disable/destroy đều tháo callback.
Không có hook Editor, không ghi file/save, không gọi ForceUpdateCanvases hay
thay transform/alpha/Time. Log dày có thể ảnh hưởng thời gian frame; cần giữ
`dt`, `unscaledDt` khi phân tích, không suy thời gian từ số dòng.

## Đọc snapshot trong cùng hệ tọa độ

- `logicPivot`, `logicFoot`, `bodyGraphic.reference`, `pixelReference` và
  `vfxGraphic.reference` đều có **World / Screen / Battle**. Battle là kết quả
  `BattleArea.InverseTransformPoint`; Screen dùng camera Canvas gốc của từng
  Image/root để cùng ra pixel màn hình. Ghi riêng Canvas/camera của mỗi Image
  nhằm nhận ra trường hợp bị chuyển sang Canvas khác. World Space Canvas có
  nhiều camera hoặc thiếu camera cần xác nhận projection trong Unity thật.
- `bodyGraphic.footDeltaScreen` và `vfxGraphic.footDeltaScreen` là chênh lệch
  với chân logic sau `GetPixelAdjustedRect()`. Đo cả rect gốc để nhận ra pixel
  rounding. Không so trực tiếp `anchored` của hai rect khác parent.
- Điểm thân là **điểm nguồn quy ước `(300,10)`**, không phải phép nhận diện
  pixel giày/chân có alpha trong PNG hoặc mesh cuối cùng của GPU. Probe không
  đọc alpha texture, không đổi import settings. Nếu các điểm vẫn trùng mà hình
  có vẻ dịch, cần kiểm tra đăng ký silhouette từng pose, biên Image và video;
  chưa được kết luận do transform.
- Mỗi rect ghi parent path, world/local, anchored, anchors, pivot, size,
  localScale, worldBasisX/Y (gồm scale/rotation của toàn chuỗi cha) và 4 góc
  chiếu lên màn hình. Ghi thêm alpha/enabled/active, pose, kích thước sprite,
  Canvas scale/camera, CanvasGroup alpha, toàn bộ ancestry, pan/appliedFrame,
  Ground/parallax phase và layout nền. `DisplayedFrame` giữ frame cuối đã gán
  sau Finish; `IsPlaying=false` phân biệt VFX đã ẩn.
- Nếu chỉ body reference đổi: đối chiếu body layout/parent. Nếu cả root/body/
  VFX cùng đổi nhưng Battle ổn định: đối chiếu ancestry/Canvas/camera. Nếu pan
  đổi: kiểm tra đúng appliedFrame trước khi quy lỗi presentation. Nếu chỉ góc
  Image đổi mà chân không đổi: đối chiếu scale/pose; chưa chứng minh teleport.

## Kiểm thử và giới hạn

`PATH=/workspace/.onboarding/bin:$PATH bash Tests~/run.sh --render-trace`

14 ca mô phỏng: mặc định tắt/giới hạn/tháo callback; 3 thế đánh × 600/800/1150
qua VFX Begin/frame 37/Finish/10 frame đứng yên; phát hiện thay đổi riêng Image
và cha; bật/tắt probe cho 1/3 Hero giữ pan, transform, HP, timer và dirty state;
điểm chân khi di chuyển/feedback Attack/Hit.

UnityStub chỉ mô phỏng biến đổi scale/translation và projection Overlay.
`GetPixelAdjustedRect()` trả rect gốc; callback Canvas và coroutine cuối frame
được test gọi thủ công, **không phải renderer, native layout hoặc lịch Unity**.
Không tuyên bố đã tái hiện hoặc sửa lỗi Unity từ các PASS này.

Kết quả Cloud của bản chẩn đoán: 14 ca mới PASS; tổng các nhóm liên quan chạy
riêng là 239 PASS (có bao gồm 14 ca mới). Toàn bộ runner đạt 499 PASS rồi dừng
ở lỗi có sẵn `MissingMemberException: GroundPresentation.fixedRect` trong
`TerrainRegression.cs:41`; các bài phía sau chưa chạy. Mỗi lần biên dịch
UNITY_EDITOR có 16 cảnh báo cũ. Biên dịch Mono với define UNITY_STANDALONE_WIN
có 3 cảnh báo cũ; đây không phải Unity compile hoặc Windows Player build.

Cần lấy log/kiểm tra thật: New/Continue/Load/Travel/Retry/Reset/Return Menu,
Melee/Archer/Magic, đứng yên/di chuyển, B2 thứ nhất/thứ hai khi đang di chuyển,
resize 600/800/1150 và Windows Player. Không thay đổi Multi-Zone, spawn 80%,
kích thước sprite/VFX, HP bar/popup, Attack/Hit hay Save/Load trong bản chẩn đoán.
