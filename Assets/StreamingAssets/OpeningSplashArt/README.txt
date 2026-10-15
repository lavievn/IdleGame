SPLASH ART TRAI / PHAI — QUY TAC MOI 0.0.53

Thu muc: Assets/StreamingAssets/OpeningSplashArt/

BAT BUOC DAT TEN:
  L_*.png / L_*.jpg / L_*.mp4  -> splash TRAI
  R_*.png / R_*.jpg / R_*.mp4  -> splash PHAI
Khong phan biet chu hoa/thuong (l_, r_ cung duoc).
Chu dau ten file phai la L hoac R; .png .jpg .jpeg .mp4 duoc ho tro.

Moi lan vao opening, moi ben chon NGAU NHIEN trong danh sach
file dung tien to ben do. Khong tu doi sang file khac giua chung.
Khung bat dau fade in o giay 27 va khong pan/zoom.
Neu thieu L hoac R, ben tuong ung hien placeholder.

KHONG con fallback sang Assets/StreamingAssets/OpeningSplash/splash.mp4.
Hai MP4 da push truoc co the chua co L/R, can doi ten
TRONG UNITY (de giu file .meta) truoc khi thay doi co hieu luc.
Khong can Catalog hay gan vao Inspector.

Kich thuoc khung: ngang = 62% cua so, cao = 76% cua so;
khung cham day va chi tran ra hai mep trai/phai.
Ten doi tuong trong Hierarchy co duoi [WIDTHxHEIGHT UI].

Cloud / mountain width/height co the sua trong
Assets/Resources/OpeningArtTheme.asset (moc tham chieu 800x450).
