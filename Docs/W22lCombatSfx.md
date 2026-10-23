# W2.2l — audit và tích hợp âm thanh Hero

## Nguồn và điểm cắt

Giữ ZIP nguyên vẹn tại `SourceAssets~/W22l/sfx.zip` (không import vào Unity).
Không có project/sidecar trong ZIP. Marker đọc được trực tiếp từ ID3 CHAP của
3 MP3 dài qua ffprobe; không suy diễn marker từ tên file hoặc waveform.
Ba Shoot có XMP của Premiere (duration/timecode và lịch sử project), nhưng
không có xmpDM:Tracks/markers, cue points hay region truy xuất được. Timecode
đó không được dùng làm điểm cắt file hiện tại; từng Shoot là một clip riêng.

| Nguồn | Định dạng | Hz/kênh | Duration container | Số vùng |
|---|---|---|---:|---:|
| magic.mp3 | MP3 | 48000/stereo | 16.992s | 8 |
| sword.mp3 | MP3 | 48000/stereo | 18.960s | 10 |
| sword2.mp3 | MP3 | 48000/stereo | 15.432s | 8 |
| shoot1.mp3 | MP3 | 48000/stereo | 1.109333s | 1, không có marker |
| shoot2.mp3 | MP3 | 48000/stereo | 1.157333s | 1, không có marker |
| shoot3.mp3 | MP3 | 48000/stereo | 0.989333s | 1, không có marker |

Duration container gồm MP3 padding, khác số sample giải mã. Bản JSON audit
ghi cả hai, SHA nguồn/PCM, mốc marker, điểm cắt thực, đỉnh dBFS và fade.
CSV `SourceAssets~/W22l/sfx-cuts.csv` liệt kê toàn bộ 29 đầu ra và start/end.
`Tools/prepare_combat_sfx.py` tái tạo từ ZIP bằng ffmpeg/ffprobe/numpy sẵn có.

Trong từng region gốc, chỉ bỏ khoảng lặng dưới -70dBFS, giữ 10ms pre-roll
và 80ms sau sample còn tín hiệu. Không vượt ranh giới marker sang đòn khác.
Mép có sample >-80dBFS dùng fade 1ms để giảm click; không normalize, compressor
hoặc nâng gain. Không có file rỗng hoặc PCM trùng nhau. Một số vùng Sword
có noise/tail chạm biên marker: giữ toàn vùng thay vì cắt reverb theo ngưỡng
cao. Cần nghe nghiệm thu để quyết định có cần chỉnh marker/tail nguồn.

Nhóm cuối trong `Resources/CombatSfx`:

- Sword: sword_ch00…09 và sword2_ch00…07 (18 clip).
- Shoot: shoot1, shoot2, shoot3 (3 clip).
- Magic: magic_ch00…07 (8 clip).

Đầu ra WAV PCM16, 48kHz stereo; AudioImporter v8 như metadata Unity 6000
hiện có, PCM/DecompressOnLoad, preload trong defaultSettings, không
normalize/downmix/streaming. Ưu tiên tránh decode mỗi đòn;
đổi ADPCM/Vorbis cần đo nghe/memory trên Player sau, không tự đổi codec nguồn.

## Sự kiện và bus runtime

CombatManager.Start tạo HeroCombatSfx một lần, cache 3 nhóm Resources.LoadAll
và 4 AudioSource 2D. Mỗi sự kiện Hero hợp lệ chọn bằng RNG riêng (không tiêu
thụ Unity Random của damage/AI); tránh lặp clip ngay liền trước nếu có lựa chọn.
Không tạo source, Sprite hoặc tải AudioClip trong đòn đánh.

Sword gọi ở nhánh melee ngay trước Impact/damage. Shoot/Magic gọi sau
projectiles.Add trong Launch(fromHero=true), cùng frame phóng đạn. Không gọi
từ PlayAttackFeedback hoặc lúc bridge đổi frame; không phát ở impact của đạn,
không phát cho quái. Hiện gameplay không có sự kiện giương cung riêng:
Attack animation bắt đầu ở callback đòn được chấp nhận, cùng frame release.
Frame 2 chứa mũi tên trong artwork đến sau ~53ms; không dời damage/release
để khớp tranh, không thêm wind-up gameplay. Cần nghiệm thu nghe/nhìn khoảng
chênh thuần trình bày này trong Unity, không tuyên bố đã đồng bộ từng pixel.

Bus có tối đa 4 voice; khi đầy bỏ SFX mới, không steal đuôi đang phát và không
bỏ đòn gameplay. Gain mỗi voice = sfxVolume × volume Settings hiện có ×
groupGain / 4; tổng gain không vượt bus volume. sfxVolume mặc định .8, Sword
gain .5 (nguồn to hơn), Shoot/Magic 1; không normalize file. Pitch variation
mặc định 0, Inspector cho bật tối đa ±3%. Volume Settings hiện dùng key
IdleGame.BgmVolume: nhạc và SFX cùng chịu điều khiển này theo yêu cầu.

Pause dùng AudioSource.Pause/UnPause, không phát lại từ đầu. Reset attack,
SetupHeroInfo/Load/Retry, clear-to-menu và disable dừng voice; Update loại
owner chết/ẩn/đổi mode/đổi life/spawn hoặc không còn đăng ký. Không có hàng
đợi âm thanh. Thiếu cả nhóm thì im lặng, combat vẫn chạy.

## Kiểm thử và nghiệm thu

Mono `--hero-sfx` gồm 17 ca: event mỗi mode, release trước impact, wind-up/
không mục tiêu/pause im lặng, B2, cap/reuse/cache, volume/headroom, pause giữ
playhead, death/hide/mode/life/spawn/disable, rebind/clear, RNG và timer không
đổi, thiếu clip vẫn phóng đạn. Stub không decode/phát hoặc đo mixer.

`--archer-animation` có 5 ca; nhóm sprite cũ có 28 ca. Thêm 6 ca Unity Play
Mode kiểm tra import 9 texture theo index và 29 clip preload stereo/48kHz.
Các ca native chưa chạy/compile bằng Editor trong Cloud. Test import không
phát tiếng, không tạo manager và không truy cập save.

Full Mono: 609 PASS rồi dừng ở lỗi cũ MissingMemberException
GroundPresentation.fixedRect (TerrainRegression.cs:41), các ca sau chưa chạy.
Formation/B2: 39 PASS; desktop UI: 59 PASS. Biên dịch UNITY_EDITOR có 16
cảnh báo cũ; define UNITY_STANDALONE_WIN có 3 cảnh báo cũ. Đây là compile với
UnityStub, không phải build Player. `--archer-entry`: 41 ca PASS khi chạy lại
hợp đồng lane/entry/B2 với đủ 9 resource sequence, thay cho cấu hình fallback
một frame. Đây là kiểm tra cấu hình bổ sung, không cộng vào 609 ca full runner.

Trên Unity và Windows Player cần:

1. Chạy Presentation Play Mode tests trong scene trống. Kiểm tra import,
   đủ 9 frame và 29 AudioClip; Console không có lỗi.
2. Archer Normal→Attack→Normal, Hit→Normal, đòn liên tiếp/tốc đánh cao;
   kiểm tra chân/độ lớn, không caro, không kẹt frame 1, không ảnh hưởng role khác.
3. Ba Hero đồng thời: đúng nhóm âm, mỗi release tối đa một tiếng, không nghe
   lặp theo frame hoặc quá to; nghe transient/tail và tinh chỉnh bus gain nếu cần.
4. Volume 0/giữa/tối đa; pause/resume không phát lại từ đầu. Return Menu,
   Load/Retry/Travel/Reset, đổi mode và B2 không có âm thanh tồn dư.
5. Profiler Player: 4 source của bus, không Resources.Load/AudioSource mới mỗi
   đòn; đo memory preload, GC và cảm giác đồng bộ frame 2/tiếng bắn. Dùng dữ
   liệu thử đã sao lưu khi kiểm tra đường gameplay; Cloud không chạm save thật.
