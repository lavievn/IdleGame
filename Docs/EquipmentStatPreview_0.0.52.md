# 0.0.52 — Nền so sánh trang bị độc lập với giao diện

**Nhánh chức năng:** `feature/0.0.52-equipment-stats`. Nhánh xuất phát từ `feature/0.0.52-inventory-equipment` và **không sửa** nhánh `feature/0.0.52-ui-workspace`, `0.0.52-inventory-equipment` hay `main`.

## Đã triển khai

- `CombatStats.GetWithModifiers` và `GetIntWithModifiers`: cùng công thức với `CombatStats.Get`, chỉ khác là nhận danh sách modifier giả định để tính thử. Không thay đổi danh sách gắn trên Hero.
- `EquipmentStatPreview.Current(hero,mode)`: xuất toàn bộ chỉ số hiện tại, đồng thời tách `withoutEquipment` (chưa tính trang bị) và `equippedBonus`.
- `EquipmentStatPreview.TryEquip(hero,instanceId,slot,mode,out preview)`: thử trước khi thay đồ, có ràng buộc loại ô, 8 ô độc lập, hai nhẫn, trang bị có điều kiện theo dạng chiến đấu và thuộc tính nguyên tố.
- `EquipmentStatPreview.TryEquipLegacy(hero,itemId,mode,out preview)`: tính thử cả hai Hộ phù HP/ATK hiện tại.
- `EquipmentStatPreview.TryUnequip(hero,slot,mode,out preview)`: so sánh khi tháo một ô trang bị (cả Hộ phù cũ ở ô Pháp bảo).
- Mỗi `EquipmentStatLine` chứa `stat`, `element`, `withoutEquipment`, `current`, `preview`, `equippedBonus`, `difference`, `isDerivedGameplayTotal`.
- **Chỉ HP / ATK / Tốc đánh** có giá trị tổng cơ bản phù hợp công thức gameplay ở thời điểm hiện tại. Các thuộc tính chưa có chỉ số nền riêng của Hero (Giáp, Kháng phép, Tốc chạy ...) đang trả phần modifier bổ sung với nền 0; `isDerivedGameplayTotal=false` để UI không nhầm đó là tổng tốc độ thật của Controller.
- Chỉ số tỷ lệ như giảm hồi chiêu, sát thương nguyên tố dùng **tỷ lệ thập phân**, ví dụ `.08` là +8%; khi dựng UI cần nhân 100 để hiển thị phần trăm.
- **Không đổi trạng thái:** không đổi `isEquipped`, `equippedSlot`, túi đồ, `combatStatModifiers`, `isDirty`, HP hiện tại hay bản lưu. Các dòng chỉ số được dựng bằng mô phỏng, không ghi vào Hero.

### Ví dụ tích hợp UI sau này

```csharp
EquipmentStatComparison compare;
if (EquipmentStatPreview.TryEquip(hero, gear.instanceId,
        EquipmentSlot.Weapon, hero.savedAttackMode, out compare))
{
    EquipmentStatLine attack = compare.Find(CombatStatId.Attack);
    // attack.withoutEquipment = ATK không tính đồ (ví dụ 8)
    // attack.equippedBonus   = lượng đang cộng nhờ đồ (ví dụ +10)
    // attack.current         = chỉ số hiện tại (ví dụ 18)
    // attack.preview         = nếu mặc món đang chọn (ví dụ 25)
    // attack.difference      = thay đổi thêm so với hiện tại (ví dụ +7)
}
```

Lưu ý UI nên hiển thị `withoutEquipment` + `equippedBonus` = `current`, nhưng nếu chọn món mới thì so `current` với `preview`. Hai so sánh có ý nghĩa khác nhau; không nên cộng nhầm hiệu số hai lần. Không lấy chỉ số từ tên/độ hiếm đơn thuần, phải lấy các giá trị của instance thực tế.

## Phạm vi chưa làm

- Bảng sinh ngẫu nhiên theo cấp/phẩm chất, hệ số sức mạnh và tỷ lệ rơi cho cấp Hoàng/Huyền/Địa: **chưa chốt cân bằng**, không tự thêm.
- Trang bị/võ học/luyện chế tiêu hao vàng, nguyên liệu, nghề phụ: chưa bật.
- UI dạng bảng đầy đủ, bộ lọc 50 ô và thanh cuộn: đang triển khai song song trên nhánh UI riêng.

## Kiểm tra

`Tests~/EquipmentStatPreviewRegression.cs` kiểm thử dự đoán sau mặc/tháo bằng kết quả thật, giữ nguyên dữ liệu Hero, kiếm đổi +10→+17, hai nhẫn chuyển ô, Hỏa theo stance, hộ phù cũ và xung đột Pháp bảo.

**Chưa biên dịch hoặc chạy Unity Play Mode từ môi trường trợ lý**. Cần kiểm thử bằng Unity hoặc môi trường C# phù hợp trước khi gộp vào nhánh UI.
