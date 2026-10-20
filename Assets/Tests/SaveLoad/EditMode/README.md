# Kiểm thử Edit Mode cho dữ liệu Save/Load — W1.6

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

Không tạo GameObject, SaveManager, GameManager hoặc tải Scene. Vì vậy không có
`SaveManager.Awake()` tự chọn thư mục người chơi hay coroutine AutoSave phát sinh.
Chỉ dùng ScriptableObject tạm, `JsonUtility` thật và các hàm dữ liệu thuần;
đối tượng được DestroyImmediate khi teardown. Cấu hình DEVB trong RAM được lưu
và khôi phục, không đọc/ghi `dev_balance.jsonl`. Fixture không chạy song song.

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
runtime vi phạm. Đọc mã hiện tại cho thấy ba ca này có khả năng FAIL vì
`EquipmentInventory.Normalize`, `EquipmentRuntime.Normalize` ép version về hiện tại
và `EquipmentRefinement.NormalizeMetadata` đặt lại dữ liệu rèn không cùng version.
Đây là dự đoán từ mã, chưa phải kết quả thực thi. Không Ignore/Explicit test,
không đổi kỳ vọng sang hành vi phá dữ liệu để làm bộ test xanh.

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

- Không gọi `SaveManager.LoadGame`: chưa kiểm tra validation, defaults/migration
  đầy đủ, lựa chọn ô, `.tmp`/Replace, backup hoặc bảo toàn file khi I/O thất bại.
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
- Guard version tương lai, giao dịch Load và ghi AutoSave an toàn cũng cần phạm
  vi sửa runtime riêng. Bộ test này chưa triển khai các phương án đó.
