using UnityEngine;

[CreateAssetMenu(fileName = "NewTag", menuName = "TuTienData/Tag Definition")]
public class TagSO : ScriptableObject
{
    [Header("ĐỊNH DANH TAG")]
    [Tooltip("ID dùng để code xử lý ngầm. VD: Basic_01")]
    public string tagID; 
    
    [Tooltip("Tên hiển thị ra UI cho người chơi")]
    public string displayName; 
    
    [Header("PHÂN LOẠI")]
    public TagCategory category;

    [Header("GIAO DIỆN (Nếu có)")]
    [Tooltip("Tên chuỗi định danh Coroutine đồ họa độc lập")]
    public string skinAnimationTrigger; 
    
    // Ghi đè hàm Equals để so sánh Tag siêu tốc bằng TagID thay vì so sánh nguyên Object
    public override bool Equals(object other)
    {
        if (other is TagSO otherTag)
            return string.Equals(this.tagID, otherTag.tagID, System.StringComparison.Ordinal);
        return false;
    }

    public override int GetHashCode()
    {
        return tagID != null ? tagID.GetHashCode() : 0;
    }
}

public enum TagCategory 
{
    SkillType,
    SkinEffect,
    EnvironmentBehavior,
    SystemLogic
}