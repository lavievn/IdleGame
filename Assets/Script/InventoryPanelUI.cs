using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Prototype 0.0.52 panel. Reuses the runtime HUD and native click dispatch,
// so no Inspector wiring or scene prefab replacement is necessary.
public sealed class InventoryPanelUI : MonoBehaviour
{
    private UIManager ui;
    private RectTransform root;
    private TextMeshProUGUI title, details;
    private RectTransform[] actions;

    public bool IsOpen => root != null && root.gameObject.activeSelf;
    public Transform Root => root;

    public static InventoryPanelUI Install(UIManager ui, RectTransform hud)
    {
        if (ui == null || hud == null) return null;
        var panel = ui.gameObject.AddComponent<InventoryPanelUI>();
        panel.ui = ui;
        var container = new GameObject("052_Inventory",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(Image));
        panel.root = (RectTransform)container.transform;
        panel.root.SetParent(hud,false);
        panel.root.anchorMin = panel.root.anchorMax = new Vector2(.5f,.5f);
        panel.root.pivot = new Vector2(.5f,.5f);
        container.GetComponent<Image>().color = new Color(.08f,.12f,.18f,.98f);
        panel.title = ui.MakeLabel(panel.root,"KHO ĐỒ / TRANG BỊ",Vector2.zero,new Vector2(400,30));
        panel.details = ui.MakeLabel(panel.root,"",Vector2.zero,new Vector2(400,68));
        panel.details.enableWordWrapping = true;
        panel.actions = new RectTransform[] {
            ui.SmallButton(panel.root,"EquipHealth","Trang bị: HP +100",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),()=>panel.Equip(EquipmentCatalog.HealthCharm)),
            ui.SmallButton(panel.root,"EquipAttack","Trang bị: ATK +20",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),()=>panel.Equip(EquipmentCatalog.AttackCharm)),
            ui.SmallButton(panel.root,"UnequipItem","Tháo về kho",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),panel.Unequip),
            ui.SmallButton(panel.root,"GrantSamples","Nhận 2 vật phẩm thử (1 lần)",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),panel.Grant),
            ui.SmallButton(panel.root,"DemoSword","Nhận Thiết Kiếm thử (1 lần)",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),panel.GrantSword),
            ui.SmallButton(panel.root,"EquipSword","Trang bị Thiết Kiếm",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),panel.EquipSword),
            ui.SmallButton(panel.root,"RemoveSword","Tháo vũ khí",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),panel.RemoveSword),
            ui.SmallButton(panel.root,"InventoryClose","Đóng",
                new Vector2(.5f,1f),new Vector2(.5f,1f),Vector2.zero,
                new Vector2(300,32),panel.Close)
        };
        if (ui.transparentWindow != null)
            ui.transparentWindow.RegisterClickable(panel.root);
        panel.Layout(Screen.width,Screen.height);
        container.SetActive(false);
        return panel;
    }

    private GameManager Manager() { return Object.FindFirstObjectByType<GameManager>(); }

    public void Open()
    {
        var gm = Manager();
        if (gm == null || !gm.IsGameplayHUDVisible) return;
        Refresh();
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
    }

    public void Close()
    {
        if (root != null) root.gameObject.SetActive(false);
    }

    private void Equip(string id)
    {
        var gm = Manager();
        if (gm != null) gm.EquipInventoryItem(id);
        Refresh();
    }

    private void Unequip()
    {
        var gm = Manager();
        if (gm != null) gm.UnequipInventoryItem();
        Refresh();
    }

    private void Grant()
    {
        var gm = Manager();
        if (gm != null) gm.GrantInventorySamples();
        Refresh();
    }

    private void GrantSword()
    {
        var gm=Manager();
        if(gm!=null) gm.GrantDemoSword();
        Refresh();
    }

    private void EquipSword()
    {
        var gm=Manager();
        if(gm==null || gm.HeroData==null || gm.HeroData.equipmentItems==null) return;
        foreach(var item in gm.HeroData.equipmentItems)
            if(item!=null && !item.isEquipped &&
                item.templateId==EquipmentFoundation.DemoSword)
            {
                gm.EquipGear(item.instanceId,EquipmentSlot.Weapon);
                break;
            }
        Refresh();
    }

    private void RemoveSword()
    {
        var gm=Manager();
        if(gm!=null) gm.UnequipGear(EquipmentSlot.Weapon);
        Refresh();
    }

    public void Refresh()
    {
        if (details == null) return;
        var gm = Manager();
        EntityDataSO hero = gm == null ? null : gm.HeroData;
        if (hero == null) { details.text = "Chưa có nhân vật."; return; }
        var equipped = EquipmentCatalog.Find(hero.equippedItemId);
        details.text = "Đang trang bị: " + (equipped == null ? "Trống" :
            equipped.name + " (" + EquipmentCatalog.StatLabel(equipped) + ")") +
            "\nTrong kho: " + EquipmentCatalog.Health.name + " x" +
            EquipmentInventory.Count(hero,EquipmentCatalog.HealthCharm) +
            "  |  " + EquipmentCatalog.Attack.name + " x" +
            EquipmentInventory.Count(hero,EquipmentCatalog.AttackCharm) +
            (hero.testItemsGranted ? " · Quà hộ phù: đã nhận" :
                " · Quà hộ phù: chưa nhận") +
            "\n" + SlotRow(hero,EquipmentSlot.Weapon,"Vũ khí") +
            "   |   " + SlotRow(hero,EquipmentSlot.Head,"Mũ") +
            "\n" + SlotRow(hero,EquipmentSlot.Chest,"Áo") +
            "   |   " + SlotRow(hero,EquipmentSlot.Boots,"Giày") +
            "\n" + SlotRow(hero,EquipmentSlot.Artifact,"Pháp bảo") +
            "   |   " + SlotRow(hero,EquipmentSlot.Necklace,"Dây chuyền") +
            "\n" + SlotRow(hero,EquipmentSlot.RingLeft,"Nhẫn 1") +
            "   |   " + SlotRow(hero,EquipmentSlot.RingRight,"Nhẫn 2") +
            "\nThiết Kiếm trong kho: " + CountLooseSword(hero) +
            " · Thử kiếm: " + (hero.demoSwordGranted ? "đã nhận" : "chưa nhận");
    }

    private string SlotRow(EntityDataSO hero,EquipmentSlot slot,string title)
    {
        var item=EquipmentRuntime.Equipped(hero,slot);
        if(item==null)
        {
            if(slot==EquipmentSlot.Artifact &&
                !string.IsNullOrEmpty(hero.equippedItemId))
                return title+": "+EquipmentCatalog.Find(hero.equippedItemId)?.name;
            return title+": —";
        }
        var definition=EquipmentFoundation.FindTemplate(item.templateId);
        return title+": "+(definition==null ? "?" : definition.name)+
            " ["+EquipmentFoundation.RarityName(item.rarity)+" "+item.level+"]";
    }

    private static int CountLooseSword(EntityDataSO hero)
    {
        int count=0;
        if(hero.equipmentItems!=null) foreach(var item in hero.equipmentItems)
            if(item!=null && !item.isEquipped &&
                item.templateId==EquipmentFoundation.DemoSword) count++;
        return count;
    }

    public void Layout(float width,float height)
    {
        if (root == null) return;
        float w = Mathf.Min(520f,Mathf.Max(220f,width-14f));
        float h = Mathf.Min(510f,Mathf.Max(250f,height-14f));
        root.sizeDelta = new Vector2(w,h);
        root.anchoredPosition = Vector2.zero;
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(.5f,1f);
        titleRect.pivot = new Vector2(.5f,.5f);
        titleRect.anchoredPosition = new Vector2(0f,-18f);
        titleRect.sizeDelta = new Vector2(w-12f,28f);
        var detailRect = (RectTransform)details.transform;
        detailRect.anchorMin = detailRect.anchorMax = new Vector2(.5f,1f);
        detailRect.pivot = new Vector2(.5f,.5f);
        detailRect.anchoredPosition = new Vector2(0f,-105f);
        detailRect.sizeDelta = new Vector2(w-18f,145f);
        float actionTop = 190f;
        float row = (h-actionTop)/actions.Length;
        for(int i=0;i<actions.Length;i++)
        {
            var b = actions[i];
            b.anchorMin = b.anchorMax = new Vector2(.5f,1f);
            b.pivot = new Vector2(.5f,1f);
            b.sizeDelta = new Vector2(w-22f,Mathf.Max(12f,row-3f));
            b.anchoredPosition = new Vector2(0f,-actionTop-i*row);
        }
    }
}
