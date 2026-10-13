# 0.0.52 — Thay Ground 2 / Ground 3 bằng tranh nền tu tiên

**Đã nối trong mã:** `GroundPresentation.Install` ưu tiên Sprite tự gán trong `EnvironmentManager`. Nếu không gán, nó tự tìm hai Sprite mới trong `Assets/Resources/Parallax`; nếu chưa có, vẫn dùng hai Sprite xem thử cũ để tránh cảnh rỗng.

| Thứ tự từ sau ra trước | Tên Sprite/đường dẫn | Tốc độ / kích thước |
|---|---|---|
| Sau cùng (Ground 3 / Sky) | `Resources/Parallax/SkyDistant.png` | 10% Ground, `distantLayerScale = 1.69` |
| Trước Sky (Ground 2 / Mountain) | `Resources/Parallax/MountainNear.png` | **30% Ground**, `farLayerScale = 1.3` |
| Cận cảnh | `Grass1` | 100% Ground, giữ nguyên |
| Nhân vật / quái / hiệu ứng | Giữ cơ chế gốc | Không đổi |

**Định dạng ảnh nguồn:** `sky.png` là PNG RGBA đã có nền trong suốt; ảnh `moutain.webp` được chuyển sang PNG RGBA để Unity có thể nhập Sprite thông thường. Cả hai được cắt bỏ viền trong suốt thừa trước khi đặt vào `Resources`, tránh làm silhouette quá nhỏ. Dải Ground được lặp ba ảnh theo chiều ngang như cơ chế trước, không can thiệp vị trí tác nhân hay logic camera.

## Thiết lập trên máy

Ảnh nhị phân không được thêm bằng thao tác sửa văn bản GitHub. Bộ **`IdleGame_0.0.52_Parallax_Sky_Mountain.zip`** chứa chính xác cấu trúc:

```
Assets/
  Resources/
    Parallax/
      SkyDistant.png
      SkyDistant.png.meta
      MountainNear.png
      MountainNear.png.meta
```

Đưa cả thư mục `Assets` từ ZIP vào **thư mục gốc dự án Unity**, không vào `Assets/Assets`. Hai file `.meta` phải đi kèm PNG, để giữ GUID khi đưa lên Git. Sau khi Unity import, bấm Play: mã sẽ tự nhận hai Sprite từ Resources, không cần gán Scene. Nếu thích gán thủ công thì mở object `EnvironmentManager`, dùng hai ô `Custom Distant Mountains` (Sky) và `Custom Far Trees` (Mountain); cách gán Inspector có quyền ưu tiên cao hơn Resources.

**Kiểm thử:** Vào map bất kỳ, xác nhận ảnh núi vàng xa nhất, ảnh núi xanh trước nó, tốc độ núi xanh 30% so với Grass1, Hero/quái đòn đánh giữ nguyên. Kéo thay đổi cửa sổ và mở/đóng Kho đồ, kiểm tra phông nền không làm reset vị trí map. Nếu thiếu Sprite thì Console không báo lỗi, dùng ảnh xem thử cũ. Không có kiểm thử Unity Play Mode trực tiếp ở môi trường trợ lý.

**Chưa cập nhật:** hai tệp ảnh nhị phân **chưa nằm trong GitHub branch**, chỉ có mã tra cứu và tài liệu; cần thêm chúng từ ZIP sau khi Pull. Không nên xác nhận rằng branch tự có hai PNG mới khi chưa import.
