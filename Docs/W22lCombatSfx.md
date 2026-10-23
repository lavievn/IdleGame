# W2.2l — audit và tích hợp âm thanh Hero

## Nguồn và điểm cắt

Giữ ZIP nguyên vẹn tại `SourceAssets~/W22l/sfx.zip` (không import vào Unity).
Không có project/sidecar trong ZIP. Marker đọc được trực tiếp từ ID3 CHAP của
3 MP3 dài qua ffprobe; không suy diễn marker từ tên file hoặc waveform.

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

Đầu ra WAV PCM16, 48kHz stereo; AudioImporter PCM/DecompressOnLoad,
preload, không normalize/downmix/streaming. Ưu tiên tránh decode mỗi đòn;
đổi ADPCM/Vorbis cần đo nghe/memory trên Player sau, không tự đổi codec nguồn.
