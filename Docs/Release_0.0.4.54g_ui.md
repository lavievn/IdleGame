# IdleGame 0.0.4.54g_ui — Khôi phục bảng chỉ số và hiệu ứng hiển thị

**Cơ sở**: `feature/0.0.4.54f` tại `8ab08b64016fad4f57cc9dc38153bb4bc1b4cad4`. `.54g` đầu tiên sửa hiển thị; **bổ sung cuối `.54g`** sửa cả bù trừ chuyển động quái đứng đánh. Không đổi công thức cân bằng, tầm đánh, thuật toán đạn, tiến trình map hay save.

## Bảng chỉ số / log

- Bảng HUD lấy lại **toàn bộ** `HeroController.FullStatDetails` thay vì chỉ 3 dòng HP/ATK/EXP.
- Giữ cấp, dạng đánh, HP, EXP, sát thương nền và cộng, chi tiết sát thương theo từng linh căn, tốc độ di chuyển, tốc đánh, thời gian mỗi đòn và tầm đánh.
- Bảng neo trên **bên phải**, bên dưới Chỉ số–DEVB–Tạm dừng; log rút gọn neo trên **bên trái** và bắt đầu cùng cao độ. Ba nút Cận chiến/Cung/Phép đặt dưới log, không đè lên bảng chỉ số.
- Bố cục có chiều cao thay đổi theo số dòng dữ liệu; đọc lại bố cục khi số dòng linh căn/chỉ số thay đổi, không chỉ khi resize.
- Giữ trạng thái modal của .54f: không đè lên màn hình Bắt đầu, xác nhận, Game Over hoặc menu chi tiết. Cửa sổ 600/800/1150 đều sử dụng cùng nguyên tắc chia cột.
- Khung chỉ số được giữ nguyên trên giao diện để theo dõi cân bằng trong giai đoạn thử logic; về sau dự kiến chuyển vào nút riêng.

## Tên bản đồ

- Xuất hiện chữ lớn **giữa cửa sổ** ngay khi vào game và mỗi lần tiến sang bản đồ mới.
- Đứng giữa **3 giây**, sau đó đồng thời thu chữ nhỏ và dịch chuyển trong **0,85 giây** (nội suy tăng tốc/giảm tốc mượt).
- Vị trí đích tạm: nhãn nhỏ phía trên vùng chơi, **cột trái dưới log và nút chọn dạng đánh**; không đè bảng chỉ số cột phải. Nhãn nhỏ còn hiện cho đến lần chuyển map tiếp theo.
- Đóng/ẩn khi quay về màn hình bắt đầu, khi Hero chết hoặc khi giao diện modal đang mở. Không chạm vào map progress, camera hoặc Ground.

## Chữ sát thương Hero và quái

- Chữ hiển thị ngay lúc nhận damage, bay **ra xa hướng xuất phát của đòn đánh** (quái đánh Hero / Hero đánh quái; nổ phép theo tâm vùng nổ) và nhô lên thành quỹ đạo cong.
- Độ mờ giảm đều và kết thúc trong **1 giây**; không sửa sát thương thực nhận, tầm đánh, đạn hoặc HP.
- Vị trí chữ được khôi phục khi hiệu ứng kết thúc hoặc quái tái sử dụng trong pool.
- Dùng quy tắc chuyển động chỉ dành cho giao diện trong `BattleMotion.cs` để bảo vệ các tính toán di chuyển/hitbox của game.

## Đối chiếu và kiểm thử

- Thêm `Tests~/Patch54gRegression.cs` (5 kiểm tra): đủ trường chỉ số, chia cột không chồng nhau, 3 giây giữ tên map và thời gian di chuyển, hiển thị tên map trong giao diện giả lập, hướng nảy + độ mờ 1 giây.
- Đây là kiểm tra nguồn/mô phỏng; **chưa chạy thành công Unity Editor hoặc Windows build** trong môi trường này. Chưa thể bảo đảm hiển thị thực tế không còn tràn ở tất cả trường hợp nhiều linh căn.
- Khi thử trên máy: kiểm tra 600/800/1150; khởi đầu/Game Over/DEVB/Menu; tăng cấp và nhiều linh căn; lần đầu deploy, sau 5 wave chuyển map; bị đánh từ hai phía; tần suất nhiều lần trúng sát thương; resize đang bay đạn.
- Nếu cần cải thiện cân bằng sau đó, làm phiên bản khác, **không pha vào bản UI này**.

## Bổ sung hoàn thiện .54g — Wave, địa hình, damage popup, quái đứng đánh

**Nguồn:** người phát triển thử bản .54g và phản hồi thiếu Wave/địa hình, chữ damage lệch Hero/quái, quái ranged đứng đánh trôi lệch Ground khi Hero cận tiến tới.

- **Tên map** giữ hoạt ảnh giới thiệu cũ (giữa màn hình 3 giây, thu nhỏ/dịch 0,85 giây). Khi đã thu nhỏ, ngay bên dưới thêm nhãn debug gồm **Bản đồ N · Wave X/5**, và **Địa hình: ...** (nguồn `EntityDataSO.mapNumber`, `completedWavesInMap`, `WorldNames.Terrain(mapTerrain)`). Mỗi wave cập nhật lại nhãn qua `RestoreProgress`; không khởi động lại hoạt ảnh khi wave thay đổi trong cùng map.
- **Chữ damage Hero**: tại scene, `Hero_DMG_Text` là một đối tượng anh em của Hero nằm trực tiếp trên Ground với tọa độ thiết kế cố định; runtime chuyển RectTransform này thành con của `heroRect`, chỉnh neo giữa thân, vị trí khởi đầu theo chiều cao cơ thể. Giữ hướng bật ngược nguồn sát thương/fade trong 1 giây.
- **Chữ damage quái**: `Monster_DMG_Text` nằm dưới `Monster_Hp_Fill` trong prefab, nên chịu offset của thanh HP. Runtime chuyển thành con trực tiếp của `MonsterController.Rect`, neo giữa thân và phục hồi tọa độ gốc khi trả về pool. Không cần sửa trực tiếp scene/prefab.
- **Chuyển động quái đánh xa**: `EnvironmentManager.Update` vốn tính `groundMinusCamera = backgroundPan - cameraPan`, song `BattleMotion.MonsterApproach` bỏ qua phần bù khi quái đủ tầm đánh. Phần bù mới áp dụng **ở mọi trạng thái**; bước tự đi (tốc độ riêng) bị giới hạn theo khoảng cách còn lại, riêng phần bù vận chuyển Ground không bị cắt mất. Quái không có target cũng nhận bù để đi cùng lớp Ground. `PanWorld(cameraPan)` và `PanEnvironment(backgroundPan)` giữ nguyên.
- **Test bổ sung:** thay kỳ vọng quái đang đứng đánh từ cameraPan sang backgroundPan trong `Tests~/GroundRelativeMotionRegression.cs`. Thêm `Tests~/Patch54hRegression.cs` (4 nhóm): Hero cận di chuyển tới quái ranged, chênh tốc độ Ground–camera; bù tốc khi đứng đánh; vị trí cha của popup damage Hero/quái; wave+terrain debug.
- **Tài liệu dự án:** đồng bộ lại README.MD, Project_info_dev.txt và xóa tài liệu cũ `WINDOWS_OVERLAY_FIX_0.0.4.51f.txt` khỏi nhánh. Nội dung liên quan WinAPI, MenuArea, kéo/resize vẫn có trong README và Git history.

### Nguy cơ cần tiếp tục kiểm thử

- Popup text hiện **gắn với đối tượng nhận sát thương**; khi quái chết và bị trả về pool ngay lập tức, nhãn có thể bị ẩn trước khi đủ 1 giây. Muốn hiển thị trọn cú kết liễu cần tách popup sang quản lý hiệu ứng độc lập trong phiên nâng cấp VFX sau. Không nói rằng mọi nhãn trúng đòn đã fade trọn vẹn ở mọi trường hợp.
- Quái đang dừng đánh cần thử khi Hero di chuyển nhiều tốc độ, lúc camera gần biên, Ground/hero vận tốc khác nhau, thay đổi chế độ và pause. Các bài test mô phỏng chỉ xác minh toán học/theo stub, không thay thế Unity Play Mode.
- Chưa chạy Unity Editor hoặc Windows EXE trong môi trường sửa; không tự tuyên bố bản vá hết lỗi. Gộp main là thao tác chủ động của người phát triển.

### Lộ trình sau khi nghiệm thu

1. **Trang bị/item**: thông số cộng (HP/ATK/phòng thủ, giảm sát thương, damage theo hệ và vũ khí), cộng/trừ có thể tách base; lưu và tương thích save.
2. **Slot item**, placeholder UI để nối item schema, ô trang bị/kho.
3. **Skill**: placeholder và hệ tự động dùng kỹ năng theo thứ tự ưu tiên; nhân vật có skill gần/xa, cần phân lớp skill tách khỏi thế đánh mặc định.
4. **Âm thanh**: placeholder sự kiện/asset cho hit, cast, UI, map.
5. **Tool biên tập asset giao diện**: import/gắn animation/effect/sprite/âm thanh cho nhân vật, skill, vũ khí, với schema và kiểm tra reference trước khi xây.

Chưa triển khai các hệ này trong .54g; chưa chốt số ô item, bảng kỹ năng, công thức thưởng item hoặc định dạng nhập tool.


## Lịch sử: camera mềm theo vùng đỏ (đã thay thế)

**Yêu cầu đã làm rõ (10/10/2026):** Vùng đỏ chỉ là ngưỡng cảnh báo camera cần đuổi theo; Hero được phép đi vượt qua, không bị chặn/ghim ngay tại rìa. Camera bắt đầu điều chỉnh **ngay khi vượt biên** và tốc độ đuổi **tăng dần trong 1 giây**; không có khoảng chờ bất động 1 giây. Khi bắt kịp, đưa Hero về vị trí khoảng **62% chiều ngang Ground** rồi **thoát chế độ đuổi**, chuyển mượt về tốc độ cuộn nền bình thường để Hero lại tự do trôi trong vùng đỏ. Nếu vượt biên lần nữa thì kích hoạt lượt đuổi tiếp theo. Đây là sửa lỗi được người dùng báo sau khi bản trước khóa Hero lâu dài ở 62%.

- `BattleMotion.cs`: thêm `SoftZoneCamera`, ba trạng thái `Normal`, `Accelerating`, `Releasing`. Trong vùng đỏ tiếp tục cuộn theo `scrollSpeed` như mốc cũ. Ngoài vùng đỏ dùng ramp smoothstep 1 giây để tăng/điều chỉnh tốc độ đuổi theo vị trí Hero. Sai số mục tiêu được tính từ tọa độ **trước bước di chuyển** để không đếm hai lần quãng Hero đi.
- Không chặn theo mép đỏ; chỉ **giới hạn ở mép cửa sổ thực** với khoảng an toàn nhỏ, ngăn Hero biến mất khi chạy quá nhanh. Biên trái camera cần tăng tốc cuộn sang phải; ở biên phải có thể cần giảm tốc và dịch ngược nhẹ để đưa Hero trở lại vùng đích.
- Khi gần đích, `Releasing` hòa tốc độ camera về `scrollSpeed` trong **0,4 giây**, rồi chuyển lại `Normal` hoàn toàn; không duy trì bám bằng quãng Hero đi. Hero tiếp tục trôi theo chênh lệch tốc độ Hero/cuộn nền, có thể vượt biên lần hai và được đuổi tiếp. Đúng bằng tốc độ cuộn thì Hero sẽ đứng tương đối yên do hai vận tốc bằng nhau, không phải do khóa.
- `EnvironmentManager.cs`: mặc định `useSoftZoneCamera=true`, `cameraAccelerationSeconds=1f`, `cameraPreferredX=.62f`. Để kiểm chứng lại hành vi cũ có thể tắt cờ `useSoftZoneCamera`.
- `EnvironmentManager.Update` tính một lần cặp `cameraPan/backgroundPan` và lưu cho `LateUpdate`. Điều này bắt buộc vì thuật toán tính chuyển động quái dựa trên **backgroundPan-cameraPan cùng một khung**, không được gọi cập nhật trạng thái camera lần thứ hai trong `LateUpdate`.
- Camera xác chết vẫn dùng cơ chế cũ; `FollowHero` khi spawn/retry xóa trạng thái gia tốc; pause không tăng đồng hồ. Không thay các hệ số sát thương/HP, skill, wave, map, save, tọa độ logic hay cấu trúc Ground.
- `Tests~/MotionRegression.cs`: bộ test camera cũ có chủ đích đặt `useSoftZoneCamera=false` để giữ kết quả lịch sử. `Tests~/CameraFollowRegression.cs`: 6 bài kiểm tra cho tăng tốc 1 giây, hai biên, **thoát chế độ đuổi**, nhiều lượt đuổi, Hero chậm hơn camera, pause/reset và quái/Ground cùng bước dịch.
- **Không khôi phục commit lỗi đã revert**: bản thử trước dùng 1 giây *chờ* rồi nội suy vị trí hồi, không đúng ý định và từng gây lỗi. Thuật toán mới đuổi *ngay lập tức với tốc độ tăng từ từ*.

### Bắt buộc kiểm tra bằng Unity thực

Hero nhanh hơn/tương đương/chậm hơn `scrollSpeed`, Hero dừng khi đánh quái xa, resize/pause/chết/Retry giữa lúc camera đang tăng tốc; kiểm tra quái đứng đánh không trôi so với Ground. Hãy quan sát liệu 1 giây tăng tốc và cảm giác Hero trở về vùng khoảng 62% có mượt hay chưa; thông số này vẫn là giá trị thử nghiệm. Chưa có xác nhận biên dịch hoặc chạy build Unity/Windows ở môi trường thực tế.

**Sửa lỗi hoàn thiện (11/10/2026):** Người phát triển xác nhận bản trước chỉ chạy đúng lượt camera đầu tiên, sau đó Hero bị giữ mãi ở điểm lệch phải. Nguyên nhân xác nhận trong mã: trạng thái `Following` không có điều kiện thoát, đặt `cameraPan = heroWalkDistance` cho mọi khung hình. Đã loại trạng thái bám vô hạn và sửa phép tính vị trí đích để lượt đuổi có thể hoàn tất. Chưa được kiểm tra lại bằng Unity thực tế.

## Camera hiện hành .54g: Deadzone + SmoothDamp

**Nguyên lý mới được người phát triển chốt:** Camera không có vận tốc cuộn nền độc lập khi Hero sống. Camera đứng yên nếu Hero nằm trong vùng chết giữa màn hình; chỉ bám theo với quán tính SmoothDamp khi Hero đi ra ngoài vùng chết. Khi Hero dừng bắn, camera giảm tốc về 0. Không ghim Hero vào một tọa độ cố định và không kéo trái–phải vô hạn.

- DeadzoneCamera thay thế hoàn toàn trạng thái Normal/Accelerating/Releasing trong BattleMotion.cs. Dùng công thức 1D tương đương Unity Mathf.SmoothDamp để dễ kiểm thử headless.
- Mặc định vùng chết rộng 38% chiều ngang Ground (nới từ 10% sau phản hồi camera dính tâm quá sớm), tâm 50%, smoothTime=0.2 giây; có thể chỉnh cameraDeadzoneRatio, cameraPreferredX và cameraSmoothTime trong EnvironmentManager.
- Vùng đỏ cũ được giữ làm ngưỡng mềm bên ngoài: khi Hero vượt ra, SmoothDamp phản ứng nhanh hơn (smoothTime x0.55, tối thiểu 0.08 giây). Không khóa Hero vào biên đỏ. Mép vật lý màn hình mới là ranh giới bảo vệ.
- Quan trọng: trong chế độ mới backgroundPan=cameraPan; Ground, Hero, quái, đạn/hiệu ứng đều nhận cùng dịch camera, cộng thêm chuyển động riêng của đối tượng. Không giữ công thức nền trôi bằng heroWalkDistance khi camera đứng trong Deadzone.
- Vẫn dùng một cặp pan tính trong Update và áp dụng ở LateUpdate. Camera khi Hero chết tiếp tục nhánh lịch sử. FollowHero/Retry reset vận tốc. Deadzone tính pan theo vị trí tương đối mỗi khung hình, không tích lũy camera offset vô hạn (tránh mất chính xác số thực khi game chạy nhiều giờ); useSoftZoneCamera=false vẫn cho phép đối chiếu camera cũ.
- Tests~/CameraFollowRegression.cs được thay bằng 10 trường hợp kiểm tra deadzone tĩnh, bắt đầu SmoothDamp, dừng bắn, đi-dừng lặp, chạy nhanh vượt đỏ, điều chỉnh bên phải, pause/reset và quái đứng đánh bám Ground.

**Chưa nghiệm thu:** cần chạy Unity Editor và Windows thực với kích thước 600/800/1150, nhiều tốc độ Hero, quái đánh xa, pause, resize và Hero chết/Retry. Mô phỏng 30/60/144 FPS không thay thế Play Mode.

**Điều chỉnh Deadzone theo phản hồi mới (11/10/2026):** Người dùng xác nhận nguyên lý theo Hero/giảm tốc khi đứng bắn đang khá ổn, nhưng với Hero tốc độ 700, vùng chết cũ 10% khiến camera bám ngay khi Hero vừa qua tâm trái. Giữ thời gian SmoothDamp **0,2 giây**, tăng `cameraDeadzoneRatio` lên **0,38** và cho phạm vi cấu hình đến 0,45; `DeadzoneCamera` còn giới hạn nửa bề rộng vùng chết để không vượt ngưỡng đỏ ngoài. Với Ground 1.000, tâm 0, biên đỏ trái -227,5, biên đỏ phải +197,5 (scene MainGame), camera bắt đầu bám gần x=-190 thay vì -50. Giá trị camera được khai báo tường minh trong `Assets/Scenes/MainGame.unity`; xác minh ở Inspector sau khi Pull. Thêm hồi quy cho tốc độ 700, Hero đi được 180 đơn vị mà camera chưa cuộn, đi–dừng và các khung hình 30/60/144 FPS. Chưa thay thuật toán SmoothDamp hoặc hành vi khi Hero dừng. Chưa thử Unity Play Mode.


## Camera .54g — Hồi về mốc 40% khi Hero dừng sau khi chạy (11/10/2026)

**Động lực:** quan sát video gameplay tham khảo, Hero chạy nhanh có thể tiến gần rìa trái, khi dừng bắn thì nhân vật cùng Ground từ từ dịch phải và cuối cùng ổn định gần vùng giữa. Bản .54g trước chỉ đưa về mép Deadzone -190 (trên Ground giả định rộng 1000) nên hiệu ứng hồi hơi ít.

**Đã thêm, không thay chuyển động lúc chạy:**
- `DeadzoneCamera.Pan` nhận `heroMovedThisFrame`, `idleRestRatio`, `idleSmoothTime`; lưu tối thiểu hai trạng thái `wasWalking` và `idleRecovering`, không có máy trạng thái cuộn nền tự động.
- **Đang chạy:** giữ `cameraDeadzoneRatio=.38` và `cameraSmoothTime=.2`. Chỉ khi ra Deadzone mới bám, biên đỏ vẫn là vùng tăng độ nhạy.
- **Đang chạy → dừng:** nếu Hero đứng **bên trái** mốc 40% chiều ngang Ground, kích hoạt một lần đưa Hero về gần 40% bằng SmoothDamp với `cameraIdleSmoothTime=.6`. Vận tốc camera được giữ liên tục từ trạng thái chạy sang lúc dừng, và cuối cùng về 0; Ground, quái và đạn dùng chung pan như trước.
- **Hero chậm dừng trước mốc 40% hoặc đứng yên từ lúc spawn:** không cưỡng bức lùi/trượt nhân vật về đúng 40% (tránh cảm giác camera dịch vô lý, đặc biệt ở tốc độ thấp).
- **Hero đi lại giữa lúc camera đang hồi:** hủy hồi ngay để chuyển lại bám Deadzone. Pause không cập nhật vận tốc/trạng thái, Retry/Spawn xóa trạng thái cũ; không đảo chiều vì cố đạt một tọa độ tuyệt đối.
- `EnvironmentManager` cho chỉnh `cameraIdleRestX=.4`, `cameraIdleSmoothTime=.6`; hai trường có giá trị được ghi rõ vào `Assets/Scenes/MainGame.unity`.
- Bổ sung 3 regression trong `Tests~/CameraFollowRegression.cs` (tổng 13): Hero chậm đứng yên không bị kéo; đang hồi mà chạy tiếp; kiểm tra Hero và Ground cùng lùi trong runtime stub. Các test đã có thay kỳ vọng dừng về mốc 40%. Thử mô phỏng số học 30/60/144 FPS, nhưng **chưa có kết quả Unity Editor/Windows build thực**.

**Nghiệm thu:** kiểm tra Hero tốc độ 700 đi tới gần biên trái rồi dừng đánh: Hero và Ground trượt về bên phải rồi dừng, không trượt đến mép đỏ phải; Hero tốc độ thấp dừng gần tâm không bị camera ép sang trái; Hero đang hồi mà đi tiếp không gây giật hoặc đảo chiều; quái tầm xa và đạn vẫn cùng Ground khi camera hồi. Nếu thời gian 0.6s chưa giống video, chỉ tinh chỉnh `cameraIdleSmoothTime` trước khi đổi các quy tắc khác.


## Camera .54g — Thử mục tiêu tâm trung bình mọi nhân vật đang giao tranh

**Yêu cầu:** giữ Deadzone 38%, follow SmoothDamp 0,2s, hồi vị trí 40% trong 0,6s theo cơ chế đã xác nhận từ trước; chỉ đổi tọa độ mục tiêu camera từ Hero đơn lẻ sang tâm mọi đối tượng chiến đấu.

**Định nghĩa thực tế để tránh nền làm camera sai:** `CombatCenterX` lấy **trung bình cộng tọa độ Ground X** của tất cả Hero đang được triển khai và quái còn sống **nằm trong khung hiển thị thật**. Mỗi nhân vật trọng số bằng nhau. Không tính cỏ/texture/terrain, VFX/đạn, xác, đối tượng bị pool tắt, hoặc quái vừa sinh còn ngoài màn hình. Không cấp phát mảng hoặc tìm GameObject mỗi khung: dùng `HeroController.ActiveHeroes` và `MonsterController.ActiveMonsters`.

**Các bảo đảm:**
- Có công tắc `EnvironmentManager.cameraUseCombatCenter`, mặc định **true** và khai báo rõ trong `MainGame.unity`. Tắt → chạy đúng cơ chế nhắm Hero cũ mà không phải revert mã. Có thể kiểm tra vị trí và số thành viên tính vào tâm qua `CurrentCameraFocusX` và `CurrentCameraFocusCount`.
- `DeadzoneCamera.Pan` giữ overload cũ, thêm overload mới có `protectedHeroX`. Độ lệch Deadzone, smooth follow, hồi 40% tính từ **tọa độ tâm nhóm**; nhưng mép an toàn của màn hình vẫn giới hạn dựa vào **vị trí Hero thật**. Không buộc Hero vào mép đỏ.
- Tâm được lấy sau khi di chuyển Hero và trước khi quái TickMovement, có thể trễ quái một khung hình, nhằm giữ nguyên thứ tự AI và cặp pan chung của Update/LateUpdate. Không thay combat, AI, Ground/đạn, save hay tốc độ các nhân vật.
- Khi số quái tham gia khác nhau, trung bình cộng có thể lệch về phía có nhiều quái: **đây là phép thử**, không phải khẳng định luôn đem lại bố cục đẹp. Spawn/despawn và quái băng qua mép hiển thị khiến tâm mục tiêu thay đổi; SmoothDamp làm mềm dịch chuyển camera nhưng không thể triệt tiêu mọi chuyển động đó. Hãy thử nhiều quái / sinh wave / chết đồng thời và so sánh với chế độ theo Hero.
- Khi không còn quái, tâm chỉ là trung bình các Hero sống; nếu chỉ có một Hero thì giống thuật toán cũ. Camera xác chết giữ cơ chế lịch sử.

**Hồi quy:** có thêm 6 bài thử về phép trung bình, nhiều Hero, công tắc quay về camera Hero, quái ra/vào viewport hoặc chết, và giới hạn Hero + đồng bộ Ground. Bộ camera có tổng **19 bài hồi quy** trong Tests~/CameraFollowRegression.cs. Việc qua kiểm tra nguồn/mô phỏng không thay cho Unity Play Mode/Windows thực tế.

**Cách thử nhanh:** `Fetch/Pull` nhánh feature/0.0.4.54g, trong Scene MainGame mở `EnvironmentManager`, bật/tắt `Camera Use Combat Center`; chạy một Hero vs một quái, nhiều quái, quái cung đứng đánh, Hero 700 dừng bắn, đổi 600/800/1150 và hết wave. Quan sát Hero có ra mép thật, Ground và quái có bị lệch, hoặc camera xoay chiều khi quái chết không.
