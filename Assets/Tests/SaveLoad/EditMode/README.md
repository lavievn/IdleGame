# Kiểm thử Edit Mode cho dữ liệu Save/Load — W1.6 / W1.7a

Mục tiêu là kiểm chứng serializer thật và các hàm xử lý dữ liệu thật của dự án,
không thay đổi gameplay hoặc giả định các lỗi W1.4 đã được sửa.

## Môi trường và assembly

- ProjectVersion: Unity **6000.5.0f1**, revision **88b47c5e7076**.
- Manifest/lock: Unity Test Framework **1.7.0**, `com.unity.ext.nunit` **2.1.0**.
- Assembly `IdleGame.SaveLoad.EditMode` chỉ dành cho **Editor**, đánh dấu
  `TestAssemblies`; không đưa NUnit/test vào Player.
- Runtime chưa có asmdef và thuộc `Assembly-CSharp`. Unity không cho asmdef
  tham chiếu trực tiếp predefined assembly này. `RuntimeData` dùng reflection
  để gọi đúng kiểu/hàm runtime, không sao chép implementation và không dùng
  `Tests~/UnityStub.cs` hoặc serializer mô phỏng. Nếu runtime đổi assembly,
  cập nhật bridge trong thư mục test này; kiểu/hàm không tìm thấy phải báo lỗi.
- Chưa tìm thấy Unity Editor trong môi trường Codex khi tạo bộ test. Chưa
  biên dịch, import asset hoặc thực thi các ca dưới đây bằng Unity thật.

## Cô lập dữ liệu

Mỗi ca được cấp một thư mục GUID riêng dưới:

`Path.GetTempPath()/IdleGame-Unity-SaveLoad/<GUID>/`

Fixture kiểm tra đường dẫn không trùng/nằm trong/chứa
`Application.persistentDataPath/Saves` trước khi tạo, ghi hoặc xóa thư mục.
Chỉ thư mục có marker sở hữu đúng GUID mới được dọn. Getter persistentDataPath
chỉ dùng kiểm tra vị trí; không đọc, ghi, sao lưu hoặc xóa save người chơi.

Nhóm 11 ca W1.6 không tạo GameObject, SaveManager, GameManager hoặc tải Scene. Vì vậy không có
`SaveManager.Awake()` tự chọn thư mục người chơi hay coroutine AutoSave phát sinh.
Chỉ dùng ScriptableObject tạm, `JsonUtility` thật và các hàm dữ liệu thuần;
đối tượng được DestroyImmediate khi teardown. Cấu hình DEVB trong RAM được lưu
và khôi phục, không đọc/ghi `dev_balance.jsonl`.

Fixture dựa vào thực thi tuần tự mặc định, không khai báo `Parallelizable`.
Không dùng `NonParallelizable`: Unity 6000.5.0f1 với cấu hình NUnit hiện tại
đã báo CS0246 cho attribute này. Việc bỏ attribute không bật chạy song song.
Các ca độc lập qua setup/teardown khi chạy tuần tự; chúng không an toàn khi chạy
đồng thời vì cùng thay đổi `CombatBalance.HeroDev`/`MonsterDev` và dùng Unity API.
Không bật parallel execution cho bộ này nếu chưa cô lập các trạng thái đó.

## Các ca được tạo

Có **11 ca NUnit** từ **10 phương thức** (một phương thức có hai TestCase).

| Nhóm | Ca |
| --- | --- |
| NativeJsonCharacterization — 5 ca | Snapshot hợp lệ qua file tạm; trường thiếu giữ giá trị đối tượng đích; trường thiếu trên đối tượng mới; giữ số version mới hơn trên field đã biết; field tương lai chưa có trong schema bị bỏ khi serialize lại. |
| SaveDataNormalization — 3 ca | Normalize hiện tại không đổi món/lịch sử và không nhân modifier; migration affix 0/forge 1 giữ roll/lịch sử; xây lại HP/ATK/tốc đánh/EXP yêu cầu từ nguồn gốc, cấp, điểm cộng, trait và trang bị. |
| SaveDataSafety — 3 ca | Không hạ version kho; không hạ version trang bị; không xóa tiến trình/lịch sử rèn có version mới hơn. |

Snapshot cố định có cấp 7, EXP 73, hai điểm chưa dùng, HP cộng 12, ATK cộng 2,
tăng trưởng cận chiến 450, vàng vượt Int32, kho, nghề, sách/kỹ năng hỗ trợ,
một kiếm có affix ẩn/hiện, lịch sử rèn và tiến trình world map. Không dùng RNG.
Các kỳ vọng chỉ số tính sẵn từ công thức được đọc tại commit `081dc911`:
HP nền 496, ATK nền 16, tốc đánh 1.03, EXP yêu cầu 232; HP ba thế 1554/410/608,
ATK ba thế 31/21/25. Không lấy kết quả hàm đang kiểm tra làm giá trị kỳ vọng.

Các ca characterization chỉ ghi nhận `JsonUtility` thực sự làm gì. PASS của
chúng **không** chứng nhận `LoadGame` xử lý thiếu trường hay version an toàn.
Các ca SaveDataSafety giữ nguyên yêu cầu bảo toàn dữ liệu và phải báo FAIL khi
runtime vi phạm. Người dùng đã báo kết quả Unity trước W1.7a: 8 PASS, 3 FAIL
do version kho/trang bị/rèn bị hạ. W1.7a thêm guard runtime, chưa được chạy lại
bằng Unity trong Codex. Không Ignore/Explicit test, không đổi kỳ vọng sang hành
vi phá dữ liệu để làm bộ test xanh.

## Bổ sung W1.7a

`EquipmentVersionGuardTests.cs` thêm **40 ca**, tổng cộng **51 ca NUnit**:

- 10 ca gọi guard/normalizer trực tiếp với từng version tương lai và hai giá trị
  isDirty; so sánh toàn bộ Hero và tham chiếu danh sách, không chỉ số version.
- 3 ca món không mặc/template chưa biết nằm cuối danh sách, chặn migration một phần.
- 1 ca guard thuần với danh sách null của legacy; 2 ca version âm bị từ chối.
- 5 ca Load từ chối mà giữ đích/file; 5 ca Save kiểu EntityDataSO bị từ chối;
  5 ca overload JSON thô không thể bỏ qua guard; 2 ca không tạo file mới khi từ chối.
- 5 ca dữ liệu hiện tại không được ghi đè slot chứa phiên bản tương lai.
- 2 ca SaveManager đọc legacy thiếu trường và round-trip snapshot hiện tại,
  bảo toàn roll, lịch sử rèn và tiến trình.

Các ca I/O mới tạo GameObject **inactive trước khi AddComponent SaveManager**,
kiểm tra Awake chưa chạy/Instance không đổi, rồi gán saveDirectory qua reflection
vào thư mục GUID của fixture. Không activate component, không chạy Scene,
không tạo GameManager và không gọi API xóa save. Đây là kiểm thử API đồng bộ
trong Edit Mode, không phải kiểm thử lifecycle/Play Mode.

Guard W1.7a kiểm tra inventory/equipment/affix/forge/checkpoint trước mutation;
không thay đổi JSON hoặc nhánh migration được hỗ trợ. Version thiếu vẫn là 0.
Version âm hoặc vượt trần bị từ chối. Normalize là no-op khi không tương thích;
SaveManager báo warning và không sửa đích, isDirty, file cuối hay file tạm.
Slot cũ có version tương lai cũng được bảo vệ khi dữ liệu ghi vào là hiện tại.
Không mở rộng thành bộ kiểm tra mọi loại JSON hỏng: slot hỏng không giải mã được
version vẫn có thể được thay thế theo cơ chế ghi hiện có.

Kiểm chứng Codex cho W1.7a: mcs biên dịch runtime cùng UnityStub thành công,
16 cảnh báo có sẵn; 7 kịch bản smoke cho guard PASS. Các nhóm Mono liên quan
chạy được 44 PASS rồi dừng ở `metadata roundtrips exactly` trong
`Tests~/AffixDiscoveryRegression.cs`. Biên dịch và chạy lại mã HEAD bằng bản sao
tạm cũng cho đúng 44 PASS và cùng lỗi, xác nhận chưa có bằng chứng lỗi mới do
W1.7a. Nhóm Forge phía sau chưa chạy. Không sửa bài cũ hoặc migration để ép PASS.
Các kết quả Mono này không phải kết quả của 51 ca NUnit/Unity; các ca đó chưa chạy
lại bằng Unity thật sau W1.7a.

## Chạy trong Unity

1. Mở bản sao checkout bằng đúng Unity 6000.5.0f1 và chờ import/compile.
2. Mở **Window → General → Test Runner → EditMode**.
3. Chọn assembly `IdleGame.SaveLoad.EditMode`, chạy đủ cả ba category.
4. Ghi số executed/PASS/FAIL cùng lỗi compiler; lưu XML/log ra thư mục tạm
   riêng, ngoài Assets và ngoài thư mục save người chơi.

Không chạy qua `Tests~/run.sh`: cơ chế đó dùng Mono và UnityStub, không chứng minh
được hành vi JsonUtility thật. Các `.meta` thư mục còn thiếu sẽ được Unity sinh
khi import; cần kiểm tra diff trước khi lưu các asset metadata sinh thêm.

## Chưa bao phủ và quyền runtime cần riêng

- Nhóm W1.6 không gọi SaveManager; W1.7a mới kiểm tra version rejection và hai
  đường đọc/ghi hợp lệ. Chưa bao phủ validation/defaults/migration đầy đủ,
  lựa chọn ô, lỗi `.tmp`/Replace, backup hoặc I/O thất bại giữa chừng.
- Không normalize world-map/catalog hoặc kiểm thử migration balance cũ; test map
  hiện chỉ xác nhận các trường serialized còn nguyên qua JsonUtility.
- Chưa có test giao dịch Load lỗi sau validation, khôi phục tham chiếu combat,
  Continue gặp file mới nhất hỏng, New Game ghi lỗi, coroutine AutoSave, Retry,
  xác nhận ghi đè, HP/timer/đạn hoặc `Destroy` trì hoãn.
- JsonUtility và normalizer dữ liệu có thể chạy Edit Mode. Các luồng gameplay,
  vòng đời và UI trên cần Play Mode, tiếp tục cô lập I/O trước Awake.
- Trước khi thêm integration test chạy lifecycle SaveManager, đề xuất một điểm
  truyền thư mục I/O trước Awake, mặc định vẫn dùng đường dẫn hiện tại. Phải xin
  phép riêng để sửa runtime; không dùng reflection đổi đường dẫn sau Awake vì
  lúc đó Awake đã có thể tạo thư mục save thật.
- W1.7a không bảo vệ các version balance/combat/map, không thêm rollback sau
  triển khai và không đổi Retry/New Game. Các mutator như TryApply/GrantSamples
  vẫn có thể tiếp tục sau no-op normalize nếu nhận dữ liệu không hỗ trợ trực tiếp;
  việc bảo vệ các lối vào đó thuộc phạm vi tiếp theo, không được coi đã hoàn tất.
