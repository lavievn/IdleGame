# W1.8a / W1.8b / W1.8c — SaveManager trong Unity Editor Play Mode

Người dùng đã xác nhận bản trước W1.8c biên dịch được với Unity 6000.5.0f1 và
51/51 Edit Mode PASS. Play Mode bị chặn tại prebuild vì scene có root. Bản sửa
W1.8c chỉ được rà soát tĩnh trong Cloud, chưa biên dịch/chạy lại bằng Unity thật;
không có kết quả PASS Play Mode cho bản sửa này.
Không chạy test có I/O trong Cloud, không dùng dữ liệu save thật để chạy thử.

## Kiểm tra môi trường trực tiếp, không dùng trạng thái lượt trước

- Prebuild không còn SessionState hoặc cờ cho phép được giữ từ lượt trước.
  Hook duy nhất là PrebuildSetup gắn trên SaveLoadPlayModeTests. Không có callback
  toàn Editor, InitializeOnLoad, RuntimeInitializeOnLoadMethod hoặc sửa scene/build
  settings. Prebuild và UnitySetUp cùng gọi RequireCleanTestEnvironment;
  không tạo, kích hoạt, sửa hoặc tái sử dụng SaveManager/GameManager.
- Ngay đầu UnitySetUp, trước khi tạo scene/thư mục/marker, fixture kiểm tra lại
  các scene đang nạp, SaveManager/GameManager và singleton. Root có component
  ngoài chính sách bên dưới, missing script hoặc behaviour runtime khác kể cả
  trong DontDestroyOnLoad đều khiến fixture từ chối trước I/O.
- Chỉ hai assembly UnityEngine.TestRunner và UnityEditor.TestRunner được chấp
  nhận cho component runner. Tên GameObject không phải bằng chứng. Nếu UTF tạo
  scene/component khác dự kiến, fixture dừng trước I/O thay vì tự nới điều kiện.
- Cho phép root Camera thuần: không con, chỉ đúng kiểu built-in Transform,
  Camera và AudioListener tùy chọn, targetTexture phải null. Root Light thuần
  chỉ có Transform/Light built-in, không con và không cookie. Đây là các thành
  phần hiển thị native, không phải gameplay MonoBehaviour; tên root không được
  dùng để cho phép. Camera/Light thuần cũng được phép là lá trong cây TestRunner.
- Theo Inspector người dùng, Camera còn có Universal Additional Camera Data của
  URP 2D. Guard chỉ cho phép đúng kiểu UniversalAdditionalCameraData trong assembly
  Unity.RenderPipelines.Universal.Runtime: Base, stack rỗng, renderer index -1
  (Default), không volume trigger, post-processing hoặc screen override. Camera
  vẫn phải là lá, không targetTexture và không có script khác. Asset pipeline
  phải đúng UniversalRenderPipelineAsset; toàn bộ renderer data phải đúng
  Renderer2DData, không renderer feature. Không cho phép toàn bộ camera/URP theo tên.
- Guard đọc field bằng reflection, không gọi cameraStack/scriptableRenderer hoặc
  GetRenderer vì các getter đó có thể khởi tạo renderer hoặc phát log. Thiếu field,
  schema đổi hoặc cấu hình ngoài phạm vi sẽ từ chối trước I/O. Không thêm tham chiếu
  assembly URP vào asmdef, không sửa cấu hình Camera/pipeline để vượt guard.
- Không dùng rootCount == 0 hoặc scene.path rỗng để xác nhận scene có sẵn an
  toàn. Mọi scene đang nạp phải qua cùng kiểm tra nội dung; chỉ tên/path bootstrap
  không đủ. Assert rootCount == 0 vẫn giữ cho scene fixture vừa CreateScene,
  vì đó là bất biến của scene mới do chính fixture tạo, không phải scene của runner.
- Chỉ dành cho Editor Play Mode, môi trường không có gameplay. Không hỗ trợ Player,
  scene có component gameplay, chạy chung nhóm tải scene hoặc cấu hình bỏ reload chưa xác minh.
  Đã biết trạng thái scene tại lúc fixture bắt đầu không thể chứng minh ngược
  rằng một script bên ngoài chưa từng chạy trước đó; không coi guard là cách
  hoàn tác I/O của scene gameplay đã kích hoạt.

## Nguyên nhân và giới hạn đối chiếu vòng đời W1.8c

Prebuild cũ từ chối mọi root trong khi UnitySetUp đã cho phép root TestRunner;
cả hai còn kiểm tra path scene. Quan sát người dùng với UTF 1.7.0 xác nhận Camera
đã hiện diện khi prebuild kiểm tra, nhưng không chứng minh ai tạo Camera đó.
Không coi Main Camera là tên đặc quyền, không gán nguồn gốc cho nó khi chưa đủ dữ liệu.

Cloud không có source/DLL builtin UTF 1.7.0. Source tham khảo công khai UTF 1.4.6
([TaskList.cs](https://github.com/needle-mirror/com.unity.test-framework/blob/master/UnityEditor.TestRunner/TestRun/TaskList.cs),
[CreateBootstrapSceneTask.cs](https://github.com/needle-mirror/com.unity.test-framework/blob/master/UnityEditor.TestRunner/TestRun/Tasks/CreateBootstrapSceneTask.cs))
cho thấy runner có bước lưu trạng thái scene, tạo bootstrap, thêm controller/lưu
scene rồi mới gọi prebuild, sau đó mới EnterPlayMode và thực thi test. Đây là
bằng chứng về khả năng scene tại hook khác scene người dùng mở, KHÔNG phải xác
nhận thứ tự triển khai nội bộ 1.7.0. Bản sửa không dựa vào giả định bootstrap có
tên/path nào, được tạo vào bước nào, hoặc Camera chắc chắn do runner sinh ra.

Guard không sửa, xóa, disable hoặc tạo Camera/Light của scene có sẵn; chỉ đọc
component và các tham chiếu nêu trên. Thông báo từ chối ghi rõ scene, root, kiểu
component và assembly để phân tích tiếp, không nới policy theo tên đối tượng.

Các kiểu/field URP được đối chiếu source Unity Graphics 6000.5, package 17.5.0,
commit c930feab9f105738328d183bb3c32758d8da0ac3:
[UniversalAdditionalCameraData.cs](https://github.com/Unity-Technologies/Graphics/blob/c930feab9f105738328d183bb3c32758d8da0ac3/Packages/com.unity.render-pipelines.universal/Runtime/UniversalAdditionalCameraData.cs),
[UniversalRenderPipelineAsset.cs](https://github.com/Unity-Technologies/Graphics/blob/c930feab9f105738328d183bb3c32758d8da0ac3/Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRenderPipelineAsset.cs),
[ScriptableRendererData.cs](https://github.com/Unity-Technologies/Graphics/blob/c930feab9f105738328d183bb3c32758d8da0ac3/Packages/com.unity.render-pipelines.universal/Runtime/ScriptableRendererData.cs),
[Renderer2DData.cs](https://github.com/Unity-Technologies/Graphics/blob/c930feab9f105738328d183bb3c32758d8da0ac3/Packages/com.unity.render-pipelines.universal/Runtime/2D/Renderer2DData.cs).
Component URP này là MonoBehaviour có ExecuteAlways, không phải thành phần native;
vì vậy chỉ chấp nhận chính xác cấu hình đã rà soát. Package version khớp manifest/lock;
Assets/Settings/UniversalRP.asset chọn Renderer2D và Renderer2D.asset không có feature.
Các asset đó chỉ được đọc, không sửa. Source tham khảo không thay thế xác minh
DLL/cấu hình thực tế trên máy người dùng; guard kiểm tra lại tại cả hai thời điểm.

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

SaveLoadPlayModeTests.cs, các assertion và JSON fixture không đổi trong W1.8b/W1.8c:

1. Awake_UsesConfiguredDirectory_AndLocksIt: đường dẫn riêng, khóa sau Awake
   kể cả khi disable; cấu hình không tạo thư mục.
2. SaveLoad_RestoresCanonicalHeroEquipmentAndMap: EXP/điểm cộng/kho/vàng/trang
   bị/affix/lịch sử rèn/map, isDirty và file tạm đúng; Load không ghi lại snapshot.
3. FutureVersions_RejectSaveAndLoad_WithoutMutatingMemoryOrFiles: một ca với năm
   đầu vào inventory/equipment/affix/forge/checkpoint vượt trần; giữ RAM/tham
   chiếu/isDirty/file cuối/file tạm và bảo vệ slot future khỏi dữ liệu hiện tại.

Không Ignore/Explicit, không NonParallelizable hoặc Assert.Multiple. Chỉ dùng
NUnit cơ bản và UnityTest/UnitySetUp/UnityTearDown. Asmdef giữ references: [],
optionalUnityReferences: ["TestAssemblies"]; runtime Assembly-CSharp được tìm bằng reflection.
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
