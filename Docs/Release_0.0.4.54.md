# 0.0.4.54 — bản kiểm thử linh căn và tên thế giới

Phát triển trên toàn bộ nền 0.0.4.52, giữ menu phân trang, lưu tải, camera, terrain,
hiệu ứng chiến đấu và cân bằng EXP trước đó. Phiên bản dự án: 0.0.4.54.
Unity ghi trong ProjectVersion.txt: 6000.5.0f1. Gói bàn giao là dự án Unity đầy đủ;
không có Unity Editor/Windows trong môi trường thực hiện nên chưa xuất được EXE
hoặc xác nhận Play Mode. Không gộp main trước khi người dùng kiểm thử.

## Linh căn và sát thương

- Thêm Vô vào cuối enum, giữ nguyên số thứ tự linh căn trong save cũ.
- Hero mới và mỗi đời quái random độc lập, không mô phỏng cha mẹ/sinh sản.
- Giữ số căn 60% một, 25% hai, 10% ba, 5% bốn; tộc ConLai hai căn.
- Chọn căn không trùng trong Kim/Mộc/Thủy/Hỏa/Thổ/Vô; trong lượt chọn mỗi căn
  có cơ hội ngang nhau. Độc/Băng trong save cũ vẫn đọc được và trung tính.
- Mỗi căn random đều Nhất/Nhị/Tam/Tứ/Ngũ trọng (1–5). Đây là thông số thử mặc
  định; chưa có bảng độ hiếm do người dùng quy định. Tam trọng là mốc giữa.
- Hai căn: tỷ trọng căn thứ nhất = 50% + 10% * chênh trọng, chặn 20%–80%; căn
  thứ hai nhận phần còn lại. Một căn 100%, ba/bốn căn chia đều. Tỷ trọng này
  dựa trên trọng random hiện tại, không suy ra cấp hoặc tộc cha mẹ.
- Sát thương hỗn hợp = sát thương cơ sở * tổng(tỷ trọng căn người đánh * tỷ
  trọng căn mục tiêu * hệ số tương tác của cặp căn). Không nhân thêm thưởng
  ATK cơ bản theo trọng và không áp phạt tổng ATK khi nhiều căn.
- Kim khắc Mộc, Mộc khắc Thổ, Thổ khắc Thủy, Thủy khắc Hỏa, Hỏa khắc Kim.
  Cặp không liên quan/cùng hệ/Vô có hệ số 1, bất kể trọng.

| Trọng người đánh − mục tiêu | Khắc mục tiêu | Bị mục tiêu khắc |
|---:|---:|---:|
| −4 | 1.075 | 0.58 |
| −3 | 1.075 | 0.61 |
| −2 | 1.15 | 0.64 |
| −1 | 1.225 | 0.67 |
| 0 | 1.30 | 0.70 |
| +1 | 1.33 | 0.73 |
| +2 | 1.36 | 0.76 |
| +3 | 1.39 | 0.79 |
| +4 | 1.42 | 0.82 |

Sát thương chiến đấu vẫn có dao động 85%–100%, hệ số đánh phép 1.8, làm tròn
và tối thiểu 1 HP. Vì vậy số sát thương trên log không luôn đúng ví dụ ATK100.
Trọng và tỷ trọng hiện trong log nhận dạng để có thể kiểm thử.

## Tên quái

50 từ tên động vật trong WorldNames.AnimalNames. Quái không thuộc Nhân tộc:
linh căn trội + dấu tộc lai nếu có + tên động vật. Ví dụ Hỏa Trư, Hỏa Linh Trư.
Chọn căn có tỷ trọng lớn nhất; hòa tỷ trọng chọn trọng cao hơn; vẫn hòa chọn
căn đầu tiên trong danh sách random. Nhân tộc giữ cách ghép tên người theo giới.

Tộc ConLai random dấu Nhân/Thú/Ma/Linh để đặt tên; không coi đây là dữ liệu
cha mẹ thực tế. Chưa thêm dòng info “lai giữa A(linh căn X) và B(linh căn Y)”.
Theo giải thích mới nhất, dòng đó chỉ là dự định mô tả; không dùng để tính
sát thương quái. Quy tắc sinh con F1/F2/F3 dành cho người chơi vẫn là thiết kế,
chưa triển khai và công thức trọng F1 chưa được chốt.

## Map và sức mạnh quái

- 30 tên Hán Việt, 5 tên mỗi địa hình: đồng bằng, cao nguyên, đầm lầy, rừng rậm,
  sa mạc, đồi núi. Tên/địa hình là dữ liệu giữ chỗ; chưa đổi hình map hoặc loại quái.
- Random tên lần đầu và sau mỗi 5 đợt hoàn tất. Không chọn lại đúng tên vừa rời;
  các tên cũ vẫn có thể xuất hiện về sau. Chết/retry/load không random lại.
- Bỏ tên “Map số” trên log, thay bằng tên và địa hình. Số thứ tự lượt vào map
  vẫn giữ nội bộ để cân bằng, bất kể địa hình/tên bị lặp.
- Hệ số sát thương quái: min(1, 0.5 + 0.1 * floor(mapNumber / 20)). Map đầu là
  lượt 1: map1–19=50%, 20–39=60%, 40–59=70%, 60–79=80%, 80–99=90%, 100+=100%.
  Mốc tính theo lượt vào map (mapNumber), không theo số tên map khác nhau.
- Áp hệ số cho đòn quái đánh hero, cả cận chiến và đạn; không giảm HP, EXP,
  tốc đánh hoặc sát thương hero. Map khó vẫn ×2 sát thương quái và ×3 EXP,
  giữ chu kỳ 5 thường/1 khó. Ví dụ map6 khó: 50% ×2 = 100% mức chuẩn.

## Save cũ và cách thử

Save .52 giữ level/EXP/tộc/tên/căn/tiến trình. Các trọng thiếu mặc định Tam,
tỷ trọng thiếu chia đều; map chưa có tên được gán một lần rồi lưu cùng save.
Save không có mapProgressVersion giữ cách chuyển đổi trước đó: bắt đầu map1.
Linh căn mới chỉ random khi Chơi mới/reset hoặc sinh quái, không random lại hero
khi load. Chơi mới/reset có xác nhận và xóa ba slot như nền .52.

1. Giải nén ra thư mục mới, mở dự án 0.0.4.54 trong Unity Hub; mở scene MainGame.
2. Kiểm tra Chơi mới: log hiện tên, tộc, trọng/tỷ trọng và tên/địa hình map.
3. Giết hết 5 đợt: đổi tên map một lần; chết/retry/load giữ tên map và số đợt.
4. Kiểm tra quái người giữ tên người; quái khác có tiền tố hệ và tên động vật.
5. Đặt cấu hình runtime trong Inspector để thử Thủy1→Hỏa5, Thủy5→Hỏa1,
   Hỏa1→Thủy5, Hỏa5→Thủy1 và Vô hai chiều. Nếu đổi danh sách căn, sửa cả danh
   sách trọng/tỷ trọng cùng số phần tử; tổng tỷ trọng bằng 1.
6. Thử profile Thủy60%/Hỏa40% trước mục tiêu một/hai căn; thử cả ba cách đánh.
7. Thử mốc map19/20,39/40,79/80,99/100 và map khó; thử save/load giữ profile.
8. Đánh giá độ dễ đầu game: giảm ATK quái có thể khiến hồi 5–10HP mỗi lần giết
   bù hết sát thương nhận vào, đặc biệt khi đánh xa. Không tự cân bằng thêm
   ngoài mức giảm người dùng yêu cầu; mô phỏng không chứng minh bất tử.

## Kiểm chứng

Tests~ dùng mã gameplay thực với bộ mô phỏng API Unity, không thay Unity Editor.
Có kiểm tra toàn bộ 25 cặp trọng, trung tính Vô, sát thương hỗn hợp, profile,
tên quái, chuyển tên map, các mốc giảm sát thương và lưu tải/migration. Mô phỏng
chiến đấu ghi tỷ lệ chết ở map1 và map100, không yêu cầu đầu game phải chết
trong mọi nhóm mẫu sau khi chủ ý giảm sát thương. Kết quả cuối: Tests~/test-results.txt.
Kiểm tra biên dịch nhánh Windows dùng API mô phỏng; chưa chạy native Windows.

Kết quả: 85 kịch bản hồi quy PASS; 720 lượt mô phỏng (map1/map100). Nhánh
biên dịch UNITY_STANDALONE_WIN PASS với API mô phỏng. Tại map1, cấp1 dạng
đánh xa vật lý/phép chết0/20 lượt; tại map100 cả18nhóm cấp/cách đánh đều
có ca chết trong mẫu. Không suy ra bất tử hay mức cân bằng cuối từ mẫu này.
