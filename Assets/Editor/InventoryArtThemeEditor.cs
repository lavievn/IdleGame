#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Editor-only presentation. The runtime works without this inspector class.
[CustomEditor(typeof(InventoryArtTheme))]
public sealed class InventoryArtThemeEditor : Editor
{
    private static bool showWindow=true, showCells=true, showHero=true,
        showMaps=true, showDisplay=true;
    private static readonly string[] WindowFields={
        "inventoryWindowFrame","inventoryHeader","equipmentPanel","bagPanel",
        "itemDetailsPanel","miniStatsPanel"};
    private static readonly string[] CellFields={
        "bagSlot","selectedBagSlot","equipmentSlot","normalTab","selectedTab",
        "actionButton","scrollbarTrack","scrollbarHandle"};
    private static readonly string[] HeroFields={
        "heroAreaBackground","heroSplashFrame","mapTitlePlate",
        "hangingOrnament","defaultHeroSplash","defaultMapSplash"};
    private static readonly string[] DisplayFields={
        "slicedResizablePanels","preserveSplashAspect","mapOpacity",
        "textColor","accentTextColor"};

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox(
            "0.0.52 — Asset giao diện\n" +
            "Gán ảnh PNG đã import dưới dạng Sprite (2D and UI). " +
            "Mọi ô bỏ trống đều có màu nền dự phòng.\n" +
            "Chọn UIManager trong Scene và kéo Theme này vào trường Inventory Art Theme.",
            MessageType.Info);
        Section("01 · CỬA SỔ & KHUNG",ref showWindow,WindowFields);
        Section("02 · Ô VẬT PHẨM & NÚT",ref showCells,CellFields);
        Section("03 · HERO & BẢN ĐỒ",ref showHero,HeroFields);

        showMaps=EditorGUILayout.Foldout(showMaps,"04 · SPLASH THEO MAP NUMBER",true);
        if(showMaps)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(
                "Mỗi phần tử đặt đúng số EntityDataSO.mapNumber. " +
                "Chưa có ảnh riêng thì tự dùng defaultMapSplash, " +
                "defaultHeroSplash, mapTitlePlate và hangingOrnament.",
                MessageType.None);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mapArts"),true);
            EditorGUI.indentLevel--;
        }
        Section("05 · HIỂN THỊ",ref showDisplay,DisplayFields);

        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space(8);
        var theme=(InventoryArtTheme)target;
        if(theme.slicedResizablePanels)
        {
            foreach(var property in WindowFields)
            {
                var sprite=(Sprite)typeof(InventoryArtTheme).GetField(property)
                    .GetValue(theme);
                if(sprite!=null && sprite.border==Vector4.zero)
                {
                    EditorGUILayout.HelpBox(property+
                        ": Chưa cài Border trong Sprite Editor. " +
                        "Tắt Sliced hoặc thiết lập viền 9-slice trước khi Play.",
                        MessageType.Warning);
                }
            }
        }
        if(theme.mapArts!=null)
        {
            var ids=new System.Collections.Generic.HashSet<int>();
            foreach(var map in theme.mapArts)
            {
                if(map==null)continue;
                if(map.mapNumber<1 || !ids.Add(map.mapNumber))
                    EditorGUILayout.HelpBox(
                        "Map Number không hợp lệ hoặc trùng: "+map.mapNumber,
                        MessageType.Warning);
            }
        }
    }
    private void Section(string title,ref bool open,string[] fields)
    {
        open=EditorGUILayout.Foldout(open,title,true);
        if(!open)return;
        EditorGUI.indentLevel++;
        foreach(var name in fields)
        {
            var field=serializedObject.FindProperty(name);
            if(field!=null)EditorGUILayout.PropertyField(field,true);
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
    }
}
#endif
