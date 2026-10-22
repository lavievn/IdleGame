# W2.2 — chẩn đoán camera và kiểm tra vị trí B2

Chưa nghiệm thu camera hoặc B2 bằng Unity/Windows thật. W2.2h đã phê duyệt thay
đường phản ứng camera: Multi-Zone mặc định bật, dùng một vận tốc cho cả entry và
gameplay. Không sửa combat, dữ liệu Save/Load, formation hay checkpoint/wave.

## Camera W2.2h

`useMultiZoneCamera=true` bỏ qua DeadzoneCamera, tiến camera khi đứng đánh và
limiter Camera Entry Handoff cũ. Các đường cũ được giữ làm chế độ so sánh rõ ràng
khi tắt cờ; các test camera cũ chọn legacy tường minh. Không chạy hai bộ pan đồng
thời. `--multi-zone` kiểm tra riêng thuật toán mới.

Green/Yellow/Red là **toàn chiều rộng vùng / chiều rộng BattleArea**: 0,20/0,45/
0,75, đối xứng quanh 50%. Với Ground 1920, các nửa biên là 192/432/720. Hàm
SmoothStep nối tốc độ 6→30→225→450, ra ngoài Red tăng tới 900 đơn vị Ground/giây.
`minFollowSpeed=30` là tốc độ tại biên Green (gần tâm bằng 20% giá trị đó), không
phải sàn bắt buộc khi đã đứng yên. `maxFollowSpeed=450`, `correctionSpeed=900`.
Giới hạn gia tốc 900, giảm tốc 1200 đơn vị/giây². Tốc độ còn bị giới hạn theo
quãng đường phanh `sqrt(2 * deceleration * khoảng cách)`; tích phân từng bước
không quá 1/120 giây, chốt nghỉ trong sai số số học 0,001 Ground. Không Lerp vị trí.
`maxCameraStep=48` giới hạn pan của một frame bị treo; không đổi deltaTime của AI.

Giữ cách chọn cameraTarget là Hero sống và tùy chọn tâm combat hiện có (bao gồm
quái sống trên màn hình khi `cameraUseCombatCenter` bật). Không thêm quái làm
cameraTarget. Bộ lọc offset tâm nhóm vẫn có tham số cũ; khi đổi Hero làm mốc,
Rebase giữ tâm tuyệt đối và vận tốc lọc. Chỉ reset lúc chuẩn bị map mới hoặc
không còn Hero; không reset khi một Hero chết trong nhóm. Pan được lập một lần
ở Update và dùng chung cho actor/projectile/Ground/parallax ở LateUpdate.

## B2

Đường `CreateTestCompanion` cũ cộng khoảng cách X và Y ±25 mà không kiểm tra biên.
Ground trong MainGame cao 100 đơn vị; sprite có thể vượt mép trên/ngang.
Sau Awake thật, bridge được gắn đúng loại, scale feedback của clone được trả về 1,
rồi chọn chỗ trống trong BattleArea bằng bốn góc Image chuyển qua hệ BattleArea.
Sử dụng toàn bộ frame có alpha làm biên bảo thủ; không đổi PNG, scale hiển thị,
HP bar, damage text, tọa độ Hero chính hoặc camera target. Không có chỗ thì từ
chối tạo đồng đội; không teleport Hero cũ để nhường chỗ. Chỉ áp dụng khi spawn,
không chặn chuyển động/combat sau đó. Tâm nhóm vẫn do camera gameplay hiện có tính.

`bash Tests~/run.sh --position-hotfix`: 12 tổ hợp 600/800/1150, lane thấp/cao,
vị trí 80%/sát mép, mỗi ca gọi đủ hai đồng đội sau khi Hero thực sự di chuyển;
kiểm tra biên sprite, không chồng nhau, Image riêng, focus/velocity không bị ghi
trong thao tác triệu hồi, resize và thu hồi. Một ca thêm kiểm tra từ chối khi
BattleArea không đủ chỗ. Đây là Mono/UnityStub: Instantiate, Awake và Canvas
không phải native Unity. Stub cũ không mô phỏng sao chép RectTransform hoặc
Awake trì hoãn đầy đủ, vì vậy các PASS trước đây không chứng minh được vị trí thật.

Ba ca Unity mới ở `Assets/Tests/Presentation/PlayMode/` kiểm tra cloning, Awake
và Canvas native, nhưng chưa chạy/biên dịch trong Cloud. Không tạo SaveManager
hoặc truy cập file save. Tỷ lệ Canvas không thay thế kiểm thử cửa sổ Windows.

## Thu log Unity cho lỗi toàn cảnh

1. Trên HeroController chính, bật **Trace Position Changes**, giữ Frame Count 30.
   Không bật Collapse trong Console. Ghi HEAD đang chạy và kích thước cửa sổ.
2. Từ Main Menu chọn Chơi mới/Tiếp tục. Thu các dòng `[HeroPosition]` từ
   `VFX complete before gameplay release`, `Camera entry began`, qua thời điểm
   giật và ít nhất 30 frame sau VFX. Chế độ mới ghi `Camera entry unified-follow`;
   không có chuyển quyền sang một bộ điều khiển camera thứ hai. Legacy vẫn ghi
   `matched-release` và 30 frame sau nó; không cần sửa dữ liệu save.
3. Lặp lại solo và sau khi gọi đủ hai đồng đội; log spawn có nhãn
   `B2 companion after bounded spawn`. Tắt trace sau khi thu dữ liệu.

Chẩn đoán giữ ít nhất 30 frame sau VFX và sau release/cancel, kể cả khi cửa sổ cũ
đã hết. Handoff được ghi tối đa 120 frame đầu, cộng đuôi 30 frame, tránh log vô
hạn khi bị kẹt/pause. Trace mặc định tắt. Trạng thái camera không được sửa bởi log.

So sánh các stage trong cùng frame:

- `Environment.Update`: Hero tự đi và pan dự kiến, số lần tính plan.
- `Environment.LateUpdate after pan`: pan/background thực áp dụng; không phải
  dịch chuyển Transform của Camera Unity trong Canvas Overlay.
- `Ground.LateUpdate after layout/depth`: layout sau component order 200.
  Ghi cả anchor/world/size của tile đất và hai lớp parallax, cùng kích thước layout.
- Snapshot có raw/limited pan, vận tốc trước/sau plan, thời gian match, lý do
  release, Ground/Canvas/Hero transform và vị trí màn hình. Phase wrap từ gần 0
  sang gần chiều rộng tile tự nó không chứng minh một cú giật hình ảnh.

Chưa có log Unity của lỗi cũ để xác định giật ở đầu, trong hay cuối handoff.
Multi-Zone là thay đổi thiết kế được phê duyệt trong W2.2h, không phải tuyên bố
đã chứng minh nguyên nhân native. Cần log và nghiệm thu lại để xác minh kết quả.

## Nghiệm thu Unity/Windows cần thực hiện

Không dùng save quan trọng. Kiểm tra 600/800/1150: solo và ba Hero, hai lần gọi B2
khi đứng/đang đi, đúng vị trí và không chồng lúc spawn; sau đó đi, đánh và tập hợp.
Kiểm tra VFX → wave đầu, Return Menu/Continue, Load và Game Over/Retry không giữ
actor/pose cũ. Theo dõi camera, HP bar/popup và các lớp nền. Các thao tác gameplay
này chưa được nghiệm thu lại trong Cloud. Suite Mono đầy đủ vẫn có blocker cũ
`GroundPresentation.fixedRect` tại `TerrainRegression.cs:41`; không gọi toàn suite PASS.
