# W1.8a / W1.8b — SaveManager trong Unity Editor Play Mode

Bản mã đang chờ phê duyệt, chỉ được rà soát tĩnh trong Cloud. Chưa biên dịch hoặc
chạy ba ca bằng Unity thật; không có số PASS/FAIL Unity cho bộ Play Mode này.
Không chạy test có I/O trong Cloud, không dùng dữ liệu save thật để chạy thử.

## Kiểm tra môi trường trực tiếp, không dùng trạng thái lượt trước

- Prebuild không còn SessionState hoặc cờ cho phép được giữ từ lượt trước.
  Hook duy nhất là PrebuildSetup gắn trên SaveLoadPlayModeTests. Không có callback
  toàn Editor, InitializeOnLoad, RuntimeInitializeOnLoadMethod hoặc sửa scene/build
  settings. Prebuild chỉ kiểm tra một scene Empty chưa lưu và không có manager;
  không tạo, kích hoạt, sửa hoặc tái sử dụng SaveManager/GameManager.
- Ngay đầu UnitySetUp, trước khi tạo scene/thư mục/marker, fixture kiểm tra lại
  các scene đang nạp, SaveManager/GameManager và singleton. Root có component
  ngoài Transform/TestRunner, missing script hoặc behaviour runtime khác kể cả
  trong DontDestroyOnLoad đều khiến fixture từ chối trước I/O.
- Chỉ hai assembly UnityEngine.TestRunner và UnityEditor.TestRunner được chấp
  nhận cho component runner. Tên GameObject không phải bằng chứng. Nếu UTF tạo
  scene/component khác dự kiến, fixture dừng trước I/O thay vì tự nới điều kiện.
- Chỉ dành cho Editor Play Mode, môi trường trống. Không hỗ trợ Player, scene
  gameplay, chạy chung nhóm tải scene hoặc các cấu hình bỏ reload chưa xác minh.
  Đã biết trạng thái scene tại lúc fixture bắt đầu không thể chứng minh ngược
  rằng một script bên ngoài chưa từng chạy trước đó; không coi guard là cách
  hoàn tác I/O của scene gameplay đã kích hoạt.

## Đường dẫn theo instance trước Awake

Mỗi ca có Path.GetTempPath()/IdleGame-SaveLoad-PlayMode/<GUID>/ riêng, phải chưa
có sẵn. Fixture tạo marker .fixture-owner chứa đúng GUID. SaveManager được thêm
vào GameObject inactive, nhận <root>/Saves qua ConfigureSaveDirectoryBeforeAwake,
rồi mới được kích hoạt. Thư mục con phải chưa có sau cấu hình và phải được Awake
của instance riêng tạo rỗng; lúc đó mới ghi nhận quyền dọn thư mục con.

Trước tạo/ghi và mỗi API Save/Load: kiểm tra marker, đường dẫn instance và các
file đích. persistentDataPath chỉ dùng như chuỗi để đối chiếu vị trí bị cấm;
không kiểm tra tồn tại, liệt kê hoặc đọc dữ liệu player Saves. Guard từ chối
đường dẫn trùng/nằm trong/chứa player Saves và ReparsePoint trên nhánh tạm;
kiểm tra từ tổ tiên xuống trước khi truy cập con qua symlink/junction.
Không dùng slot tùy ý: chỉ AutoSave, ManualSave1, ManualSave2.

SaveManager.cs giữ các thay đổi W1.8a: cấu hình không I/O, khóa từ đầu Awake,
không fallback khi cấu hình sai; default gameplay không đổi. W1.8b không sửa
thêm SaveManager, logic Save/Load/AutoSave/Retry, JSON hoặc migration.

## Cleanup theo danh sách file, không đệ quy

- createdFiles chỉ nhận tên từ sáu file cố định: AutoSave.json/.tmp,
  ManualSave1.json/.tmp, ManualSave2.json/.tmp. Fixture ghi file thành công mới
  ghi nhận; typed SaveGame trả true mới ghi nhận file .json đầu ra.
- Load, Save bị từ chối và overload JSON thô không được tự nhận file mới. File
  tạm còn lại sau lỗi hoặc file ngoài danh sách tạo được giữ để kiểm tra.
- CheckInventory chỉ liệt kê root và Saves, không duyệt đệ quy. Root chỉ được
  chứa marker và thư mục Saves đã ghi nhận tạo; Saves chỉ được chứa file phẳng
  thuộc createdFiles. Bất kỳ thư mục con/file lạ/link nào, kể cả tên cố định
  nhưng chưa ghi nhận tạo, đều làm dừng trước khi bắt đầu xóa.
- Mỗi File.Delete dữ liệu lấy đường dẫn từ FilePath(name), đồng thời có cả
  danh sách tên cố định, createdFiles, marker và kiểm tra đường dẫn. Kiểm tra
  lại inventory trước mỗi lần xóa. Marker được xóa riêng ở bước cuối, sau khi
  kiểm tra root chỉ còn marker và marker đúng GUID do fixture tạo.
- Chỉ Directory.Delete(SaveDirectory, false) và Directory.Delete(Root, false),
  khi thư mục thuộc fixture và rỗng. Không có Directory.Delete(..., true),
  không xóa thư mục cha chung, không cố xử lý mục bất thường.
- Hủy SaveManager trong DontDestroyOnLoad và các ScriptableObject, chờ frame,
  dỡ scene fixture rồi mới dọn file. Không có GameManager/coroutine quit-save.

Các check theo đường dẫn không phải sandbox chống tiến trình ngoài thay đổi
filesystem đồng thời. Không khẳng định nguyên tử giữa check và I/O, hoặc xử lý
mọi alias vật lý/bind mount. Nếu phát hiện chuyển hướng/mất marker/mục bất thường,
fixture dừng; mục bất thường có sẵn lúc bắt đầu cleanup giữ nguyên toàn bộ cây.
Nếu có thay đổi ngoài trong lúc cleanup, các bước đã hoàn tất không được hoàn tác;
phần còn lại được giữ, thao tác xóa thư mục không rỗng sẽ thất bại.

## Ba ca được giữ nguyên

SaveLoadPlayModeTests.cs, các assertion và JSON fixture không đổi trong W1.8b:

1. Awake_UsesConfiguredDirectory_AndLocksIt: đường dẫn riêng, khóa sau Awake
   kể cả khi disable; cấu hình không tạo thư mục.
2. SaveLoad_RestoresCanonicalHeroEquipmentAndMap: EXP/điểm cộng/kho/vàng/trang
   bị/affix/lịch sử rèn/map, isDirty và file tạm đúng; Load không ghi lại snapshot.
3. FutureVersions_RejectSaveAndLoad_WithoutMutatingMemoryOrFiles: một ca với năm
   đầu vào inventory/equipment/affix/forge/checkpoint vượt trần; giữ RAM/tham
   chiếu/isDirty/file cuối/file tạm và bảo vệ slot future khỏi dữ liệu hiện tại.

Không Ignore/Explicit, không NonParallelizable hoặc Assert.Multiple. Chỉ dùng
NUnit cơ bản và UnityTest/UnitySetUp/UnityTearDown. Assembly tham chiếu
UnityEngine.TestRunner; runtime Assembly-CSharp được tìm bằng reflection.
Bộ 51 ca Edit Mode không bị sửa.

## Những phần chưa xác minh hoặc chưa triển khai

- Không có Unity Editor 6000.5.0f1 trong Cloud; chưa xác nhận compile/discovery,
  vòng đời inactive → Awake, prebuild UTF 1.7.0, runner whitelist hoặc I/O trên
  từng hệ điều hành. Kiểm tra tĩnh/Mono không thay thế Unity thật.
- Overload runtime nhận slotName vẫn có hành vi Path.Combine cũ; fixture chặn
  ba tên cố định, không mở rộng W1.8b thành sandbox cho mọi người gọi runtime.
- Chưa xây dựng fixture GameManager/CombatManager: JSON hỏng/map sai giữ Hero,
  Load checkpoint dọn đạn/timer/HP, AutoSave giữ hai slot thủ công, ghi AutoSave
  lỗi không báo thành công hoặc hủy phiên chơi. Chỉ giữ thiết kế, không sửa các
  hệ thống đó trong giai đoạn này.
