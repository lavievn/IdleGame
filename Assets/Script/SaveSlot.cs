/// <summary>
/// Quản lý định danh các Slot lưu trữ để tránh lỗi sai chính tả khi gọi I/O.
/// Không chứa logic, chỉ dùng để phân loại.
/// </summary>
public enum SaveSlot
{
    AutoSave,
    ManualSave1,
    ManualSave2
}