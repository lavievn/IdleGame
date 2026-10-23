# W2.2m B — BGM/SFX độc lập

Nguyên nhân cũ: HeroCombatSfx.ApplyVolume nhân trực tiếp MapBgmController.CurrentVolume.
Home chỉ có một track; gameplay chỉ có nút ±/mute nhạc. Không thể mute BGM
và giữ SFX. Không phải lỗi decode clip.

AudioVolumeSettings là nguồn thiết lập chung, dùng PlayerPrefs:
- Giữ nguyên IdleGame.BgmVolume, mặc định .6 như phiên bản trước.
- Thêm IdleGame.SfxVolume, mặc định 1 nếu chưa tồn tại.
- Preview cập nhật RAM PlayerPrefs; Save lúc thả chuột hoặc action xác nhận,
  không flush xuống đĩa ở mỗi frame kéo. Không liên quan JSON save nhân vật.

MapBgmController đọc riêng BGM. musicVolume chỉ là mirror Inspector; hai source,
group selection, thời gian/công thức crossfade không đổi. HeroCombatSfx đọc riêng
SFX; gain theo nhóm, pool 4 voice/pitch/RNG của W2.2l giữ nguyên. BGM=0 không
mute SFX; SFX=0 không mute nhạc. Âm opening đi qua bus nhạc hiện có.

AudioVolumeBar dùng cùng track/fill/knob và toán tọa độ cho cả hai UI. Settings
Home thêm hàng SFX; gameplay thêm hai slider và reset cả hai. Nút ±/mute BGM
cũ vẫn dùng được. Không sửa theme, logo, Splash hoặc layout Home đã lưu.
Fill neo trái với anchoredPosition=0, tránh cộng thừa nửa chiều rộng track.
UIManager/TransparentWindow dùng chung đường capture/drag/release đã có;
gameplay kiểm tra HitControl để không xuyên modal, Home giữ guard entry/popup.
Ẩn/đóng menu kết thúc drag. Layout đọc giá trị bus hiện tại, không reset volume.

9 ca Mono ở --audio-volumes (6 mới + 3 legacy) kiểm tra legacy key, kênh mới,
Home/gameplay tại 600/800/1150, live drag/flush release, row/bounds/fill,
đồng bộ ngược, modal, BGM/SFX mute độc lập, playhead và clock crossfade không
restart. Nhóm --hero-sfx giữ 17 assertion tương đương nhưng dùng kênh SFX mới
thay cho kỳ vọng dùng chung BGM đã được thay đổi theo thiết kế. Không nới sai số.

Giới hạn: Mono dùng PlayerPrefs/AudioSource/TMP/Canvas mô phỏng. Chưa kiểm tra
flush thật qua lần khởi động Player khác, mixer nghe thực tế, TMP glyph hoặc
native Win32 capture. Nghiệm thu Unity/Windows: kéo cả hai slider, BGM0/SFX100
và ngược lại, Settings→Gameplay→Settings, ba kích thước, reset, thoát/mở lại,
crossfade đang chạy, popup/modal và Home artwork giữ nguyên.
