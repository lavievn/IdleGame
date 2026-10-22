# Kiểm thử vị trí B2 bằng Unity Play Mode

Chưa chạy hoặc biên dịch bằng Unity Editor trong Cloud. Ba ca NUnit dùng
`TestCase`/`Assert.That`, không dùng `Assert.Multiple` hay `NonParallelizable`.
Assembly chỉ dành cho test; truy cập Assembly-CSharp và Image qua reflection.

Chạy trong scene không có manager/actor gameplay. Fixture từ chối scene đang có
SaveManager, GameManager, CombatManager, EnvironmentManager, HeroController hoặc
MonsterController, kể cả inactive. Không có prebuild hook, chuyển scene hay I/O.
Fixture không tạo SaveManager và không gọi New Game/Load/Reset.

Các ca đồng bộ dùng Instantiate, Awake trì hoãn và Canvas thật: gọi hai đồng đội,
kiểm tra biên toàn bộ Image (kể cả phần alpha), parent/anchor/pivot, Image độc lập,
không chồng nhau và không đổi Hero/camera target/focus lúc gọi. GameManager và
CombatManager bị disable trước khi kích hoạt để không tự chạy Start/Update;
chỉ khởi tạo CombatManager theo lời gọi tường minh. Dọn đúng các object đã tạo.

Ba tỷ lệ Canvas tương ứng 600/800/1150 được kiểm tra bằng Canvas thật, không phải
resize cửa sổ Windows. Chưa bao phủ render, chuyển động nhiều frame, combat,
Retry/Load hay camera handoff thật. Cần kiểm thử gameplay theo checklist trong
`Tests~/PositionHotfix.md`; PASS của fixture không thay thế nghiệm thu này.
