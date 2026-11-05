using System;
using System.Collections.Generic;
using UnityEngine;

// Only visual references: no Item/Save/Combat changes.
// Create via Assets > Create > Tu Tien > UI > Inventory Art Theme.
// Assign to UIManager in the scene, then drag imported Sprites into fields.
[CreateAssetMenu(fileName = "InventoryArtTheme", menuName = "Tu Tien/UI/Inventory Art Theme")]
public sealed class InventoryArtTheme : ScriptableObject
{
    [Header("01. CUA SO KHO DO")]
    [Tooltip("Khung lon o ben phai. Nen trong suot ngoai khung.")]
    public Sprite inventoryWindowFrame;
    [Tooltip("Thanh tieu de cua cua so.")]
    public Sprite inventoryHeader;
    [Tooltip("Nen/khung khu vuc tam trang bi.")]
    public Sprite equipmentPanel;
    [Tooltip("Nen/khung khu vuc luoi tui do.")]
    public Sprite bagPanel;
    [Tooltip("Khung thong tin chi tiet vat pham.")]
    public Sprite itemDetailsPanel;
    [Tooltip("Khung tom tat chi so nho canh trang bi.")]
    public Sprite miniStatsPanel;

    [Header("02. CAC O VAT PHAM")]
    [Tooltip("Anh nen cho MOI o chua vat pham va o trong, su dung lai trong luoi.")]
    public Sprite bagSlot;
    [Tooltip("Anh nen cua o vat pham dang duoc chon; de trong de dung bagSlot.")]
    public Sprite selectedBagSlot;
    [Tooltip("Anh nen cho 8 o trang bi.")]
    public Sprite equipmentSlot;
    [Tooltip("Anh nen cac tab chua duoc chon.")]
    public Sprite normalTab;
    [Tooltip("Anh nen tab dang duoc chon.")]
    public Sprite selectedTab;
    [Tooltip("Anh nen cac nut: Trang bi, Thao, dong cua so...")]
    public Sprite actionButton;
    [Tooltip("Anh cho thanh cuon, khi bo sung ScrollRect.")]
    public Sprite scrollbarTrack;
    [Tooltip("Anh cho tay nam thanh cuon, khi bo sung ScrollRect.")]
    public Sprite scrollbarHandle;

    [Header("03. KHU VUC HERO / BAN DO")]
    [Tooltip("Nen khung phia tren trai khi mo kho; co the de trong neu dung splash theo map.")]
    public Sprite heroAreaBackground;
    [Tooltip("Khung trang tri tren vung hero; nen PNG trong suot o giua.")]
    public Sprite heroSplashFrame;
    [Tooltip("Bang ten map / banner dau khung.")]
    public Sprite mapTitlePlate;
    [Tooltip("Hoa van / the treo canh bang map.")]
    public Sprite hangingOrnament;
    [Tooltip("Anh Hero mac dinh de phu len splash map (khong bat buoc).")]
    public Sprite defaultHeroSplash;
    [Tooltip("Anh map mac dinh, chi dung khi khong co map khop danh sach.")]
    public Sprite defaultMapSplash;

    [Header("04. CAC MAU SPLASH MAP")]
    [Tooltip("World-map dùng worldMapId (ví dụ TV-20); save legacy dùng mapNumber. Thiếu ảnh/map khớp sẽ dùng ảnh mặc định.")]
    public List<InventoryMapArt> mapArts = new List<InventoryMapArt>();

    [Header("05. CACH HIEN THI")]
    [Tooltip("Chi bat neu sprite da cai Border o Sprite Editor (9-slice). Nen de tat voi tranh/anh ve nguyen tam.")]
    public bool slicedResizablePanels;
    [Tooltip("Giữ nguyên tỉ lệ cho tranh hero và tranh splash map (co the de vien trong).")]
    public bool preserveSplashAspect = true;
    [Range(0f, 1f)]
    [Tooltip("Do trong suot cua anh map. 1 la nguyen ban.")]
    public float mapOpacity = 1f;
    [Tooltip("Chon mau de nhin thay chu khi dung anh nen sang.")]
    public Color textColor = new Color(.89f,.91f,.83f,1f);
    public Color accentTextColor = new Color(.88f,.75f,.48f,1f);

    [Header("06. HUD W3 / CAC KHUNG DUNG CHUNG")]
    [Tooltip("Ảnh nền độc lập với text/hitbox. Bỏ trống để dùng màu mặc định.")]
    public Sprite hudPanelFrame;
    public Sprite hudHeader;
    public Sprite hudHeroCard;
    public Sprite hudAvatarFrame;
    [Tooltip("Ảnh đại diện tùy chọn; chỉ hiện với ô có Hero thực, giữ tỷ lệ ảnh.")]
    public Sprite hudHeroPortrait;
    public Sprite hudStatTrack;
    public Sprite hudHealthFill;
    public Sprite hudProgressFill;
    [NonSerialized] private int hudRevision;
    public int HudRevision => hudRevision;
    private void OnValidate() {unchecked {hudRevision++;}}

    // World-map identities and legacy sequential numbers are separate namespaces.
    // A modern save must never pick artwork from its stale legacy mapNumber.
    public InventoryMapArt FindMapForHero(EntityDataSO hero)
    {
        if(hero==null)return null;
        if(!TuTienCore.WorldMapCatalog.Enabled(hero))return FindMap(hero.mapNumber);
        if(mapArts==null || string.IsNullOrEmpty(hero.worldMapId))return null;
        foreach(var entry in mapArts)
            if(entry!=null && string.Equals(entry.worldMapId,hero.worldMapId,StringComparison.Ordinal))return entry;
        return null;
    }

    public InventoryMapArt FindMap(int mapNumber)
    {
        if (mapArts == null) return null;
        for(int i=0;i<mapArts.Count;i++)
        {
            var entry=mapArts[i];
            if(entry != null && string.IsNullOrEmpty(entry.worldMapId) && entry.mapNumber==mapNumber)return entry;
        }
        return null;
    }
}

[Serializable]
public sealed class InventoryMapArt
{
    [Tooltip("ID catalog world-map, ví dụ TV-20. Bỏ trống để cấu hình một map legacy bằng mapNumber.")]
    public string worldMapId = "";
    [Min(1)]
    [Tooltip("Số map legacy (worldMapVersion = 0), chỉ dùng khi worldMapId của entry trống.")]
    public int mapNumber = 1;
    [Tooltip("Ten map tuy chon de tham khao trong Inspector.")]
    public string displayName = "";
    [Tooltip("Tranh nen hien thi phia tren trai cua cua so mo rong.")]
    public Sprite mapSplash;
    [Tooltip("Anh hero rieng cho map nay (bo trong de dung defaultHeroSplash).")]
    public Sprite heroSplash;
    [Tooltip("Khung tieu de map rieng (bo trong de dung mapTitlePlate).")]
    public Sprite titlePlate;
    [Tooltip("Hoa van / the treo rieng cho map.")]
    public Sprite ornament;
}
