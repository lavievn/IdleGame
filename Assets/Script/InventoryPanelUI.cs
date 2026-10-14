using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 0.0.52: docked Xianxia inventory: ink/jade/brass-inspired temporary
// vector surfaces, not pixel art. Final book/jade ornament sprites can
// replace only the presentation; item layout and battle logic stay intact.
public sealed class InventoryPanelUI : MonoBehaviour
{
    private sealed class Entry
    {
        public string key, name, id;
        public int quantity;
        public EquipmentInstance gear;
        public bool equipped;
        public bool legacy;
        public EquipmentSlot slot;
    }

    private static readonly EquipmentSlot[] Slots = {
        EquipmentSlot.Weapon,EquipmentSlot.Head,EquipmentSlot.Chest,
        EquipmentSlot.Boots,EquipmentSlot.Artifact,EquipmentSlot.Necklace,
        EquipmentSlot.RingLeft,EquipmentSlot.RingRight
    };
    private static readonly string[] SlotNames = {
        "Vũ khí","Mũ","Áo","Giày","Pháp bảo","Dây chuyền","Nhẫn trái","Nhẫn phải"
    };

    private UIManager ui;
    private RectTransform root, headerBacking, equipmentBacking, inventoryBacking, topBanner;
    private RectTransform detailBacking, miniStatsBacking;
    private RectTransform mapArtwork, heroArtwork, heroArtworkFrame, mapNamePlate, hangingOrnament;
    private TextMeshProUGUI header, summary, detail, bagStatus, bannerText;
    private RectTransform[] slotButtons = new RectTransform[8];
    private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[8];
    private RectTransform[] bagButtons = new RectTransform[20];
    private TextMeshProUGUI[] bagTexts = new TextMeshProUGUI[20];
    private RectTransform[] tabButtons = new RectTransform[3];
    private RectTransform equipButton, equipRightButton, removeButton;
    private TextMeshProUGUI equipText, equipRightText, removeText;
    private readonly List<Entry> entries = new List<Entry>();
    private int filter, page;
    private string selectedKey = "";
    private int lastWidth,lastHeight;
    private bool overlayFallback;

    public bool IsOpen => root != null && root.gameObject.activeSelf;
    public Transform Root => root;
    public Transform DecorationRoot => topBanner;

    private static RectTransform Backdrop(Transform parent,string name,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rt=(RectTransform)go.transform;
        rt.SetParent(parent,false);
        go.GetComponent<Image>().color=color;
        go.GetComponent<Image>().raycastTarget=false;
        return rt;
    }
    private static void Place(RectTransform rect,float x,float y,float w,float h)
    {
        if(rect==null)return;
        rect.anchorMin=rect.anchorMax=new Vector2(0f,1f);
        rect.pivot=new Vector2(0f,1f);
        rect.anchoredPosition=new Vector2(x,-y);
        rect.sizeDelta=new Vector2(w,h);
        rect.localScale=Vector3.one;
    }
    private static TextMeshProUGUI ButtonText(RectTransform rect)
    {
        return rect.GetComponentInChildren<TextMeshProUGUI>(true);
    }
    private static Color RarityColor(EquipmentRarity rarity)
    {
        switch(rarity)
        {
            case EquipmentRarity.Hoang:return new Color(.34f,.32f,.25f,1f);
            case EquipmentRarity.Huyen:return new Color(.17f,.32f,.41f,1f);
            case EquipmentRarity.Dia:return new Color(.19f,.38f,.29f,1f);
            case EquipmentRarity.Thien:return new Color(.34f,.26f,.52f,1f);
            case EquipmentRarity.Tien:return new Color(.49f,.38f,.18f,1f);
            default:return new Color(.15f,.24f,.27f,1f);
        }
    }
    private static void ColorButton(RectTransform rect,Color color)
    {
        if(rect!=null)
        {
            var image=rect.GetComponent<Image>();
            if(image!=null)image.color=color;
        }
    }
    private RectTransform MakeButton(string name,string caption,Action click)
    {
        var result=ui.SmallButton(root,name,caption,new Vector2(0f,1f),
            new Vector2(0f,1f),Vector2.zero,new Vector2(60f,36f),()=>click());
        ColorButton(result,new Color(.16f,.29f,.30f,.98f));
        return result;
    }
    private TextMeshProUGUI MakeText(string name,string caption)
    {
        var label=ui.MakeLabel(root,caption,Vector2.zero,new Vector2(100f,40f));
        label.gameObject.name=name;
        label.alignment=TextAlignmentOptions.TopLeft;
        label.raycastTarget=false;
        return label;
    }

    public static InventoryPanelUI Install(UIManager ui,RectTransform hud)
    {
        if(ui==null || hud==null)return null;
        var panel=ui.gameObject.AddComponent<InventoryPanelUI>();
        panel.ui=ui;
        panel.topBanner=Backdrop(hud,"052_ExpandedKnowledgeBanner",
            new Color(.048f,.105f,.119f,.965f));
        // Separate visual layers. All are disabled when no Sprite is assigned,
        // so a clean default look remains usable without artwork.
        panel.mapArtwork=Backdrop(panel.topBanner,"Art_MapSplash",Color.white);
        panel.heroArtwork=Backdrop(panel.topBanner,"Art_HeroSplash",Color.white);
        panel.heroArtworkFrame=Backdrop(panel.topBanner,"Art_HeroSplashFrame",Color.white);
        panel.mapNamePlate=Backdrop(panel.topBanner,"Art_MapNamePlate",Color.white);
        panel.hangingOrnament=Backdrop(panel.topBanner,"Art_MapHangingOrnament",Color.white);
        panel.bannerText=ui.MakeLabel(panel.topBanner,
            "HÀNH TRANG TU SĨ  ·  VẬT PHẨM & PHÁP KHÍ",
            Vector2.zero,new Vector2(460f,80f));
        panel.bannerText.alignment=TextAlignmentOptions.Center;
        panel.bannerText.raycastTarget=false;
        panel.root=Backdrop(hud,"052_ExpandedInventory",new Color(.046f,.082f,.098f,.99f));
        panel.headerBacking=Backdrop(panel.root,"SilkTitleBacking",
            new Color(.10f,.19f,.21f,.99f));
        panel.equipmentBacking=Backdrop(panel.root,"JadeEquipBacking",
            new Color(.078f,.145f,.16f,.96f));
        panel.inventoryBacking=Backdrop(panel.root,"InkBagBacking",
            new Color(.055f,.115f,.13f,.96f));
        panel.miniStatsBacking=Backdrop(panel.root,"Art_MiniHeroStatsFrame",Color.white);
        panel.detailBacking=Backdrop(panel.root,"Art_ItemDetailsFrame",Color.white);
        panel.header=panel.MakeText("InventoryTitle","LINH KHÍ  ·  TRANG BỊ & TÚI CÀN KHÔN");
        panel.summary=panel.MakeText("InventoryStats","");
        panel.detail=panel.MakeText("SelectedItemDetails","");
        panel.bagStatus=panel.MakeText("BagPage","");

        string[] filters={"Tất cả","Trang bị","Vật phẩm"};
        for(int i=0;i<3;i++)
        {
            int tab=i;
            panel.tabButtons[i]=panel.MakeButton("InventoryTab"+i,filters[i],
                ()=>panel.SwitchTab(tab));
        }
        for(int i=0;i<Slots.Length;i++)
        {
            int slot=i;
            panel.slotButtons[i]=panel.MakeButton("EquipmentSlot"+i,SlotNames[i],
                ()=>panel.SelectEquipped(Slots[slot]));
            panel.slotTexts[i]=ButtonText(panel.slotButtons[i]);
        }
        for(int i=0;i<panel.bagButtons.Length;i++)
        {
            int index=i;
            panel.bagButtons[i]=panel.MakeButton("BagCell"+i,"",
                ()=>panel.SelectBagCell(index));
            panel.bagTexts[i]=ButtonText(panel.bagButtons[i]);
        }

        panel.MakeButton("InventoryPrev","◀",panel.PreviousPage);
        panel.MakeButton("InventoryNext","▶",panel.NextPage);
        panel.MakeButton("InventoryGrantCharms","Hộ phù mẫu",panel.GrantCharms);
        panel.MakeButton("InventoryGrantSword","Kiếm mẫu",panel.GrantSword);
        panel.equipButton=panel.MakeButton("InventoryEquip","Trang bị",panel.EquipSelected);
        panel.equipRightButton=panel.MakeButton("InventoryEquipRingRight","Nhẫn phải",panel.EquipRight);
        panel.removeButton=panel.MakeButton("InventoryUnequip","Tháo",panel.RemoveSelected);
        panel.equipText=ButtonText(panel.equipButton);
        panel.equipRightText=ButtonText(panel.equipRightButton);
        panel.removeText=ButtonText(panel.removeButton);
        panel.MakeButton("InventoryClose","Đóng  ×",panel.Close);

        if(ui.transparentWindow!=null)
        {
            ui.transparentWindow.RegisterClickable(panel.root);
            ui.transparentWindow.RegisterClickable(panel.topBanner);
        }
        panel.Layout(Screen.width,Screen.height,Screen.width,Screen.height);
        panel.ApplyTheme();
        panel.root.gameObject.SetActive(false);
        panel.topBanner.gameObject.SetActive(false);
        return panel;
    }

    private RectTransform FindButton(string name)
    {
        foreach(Transform child in root)
            if(child.gameObject.name==name)return child as RectTransform;
        return null;
    }
    private GameManager Manager() { return UnityEngine.Object.FindFirstObjectByType<GameManager>(); }
    private EntityDataSO Hero()
    {
        var manager=Manager();
        return manager==null?null:manager.HeroData;
    }

    public void Open()
    {
        var gm=Manager();
        if(gm==null || !gm.IsGameplayHUDVisible)return;
        selectedKey="";
        page=0;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        Refresh();
    }
    public void Close()
    {
        if(root==null || !root.gameObject.activeSelf)return;
        root.gameObject.SetActive(false);
        if(topBanner!=null)topBanner.gameObject.SetActive(false);
        if(ui!=null)ui.OnInventoryDismissed();
    }

    private static string LabelForGear(EquipmentInstance item)
    {
        var template=EquipmentFoundation.FindTemplate(item.templateId);
        return template==null?item.templateId:template.name;
    }
    private void RebuildEntries(EntityDataSO hero)
    {
        entries.Clear();
        if(hero==null)return;
        // All physical gear has a unique key. Worn items are represented in
        // the left equipment panel only; bag shows the un-equipped ones.
        if(filter!=2 && hero.equipmentItems!=null)
            foreach(var item in hero.equipmentItems)
            {
                if(item==null || item.isEquipped)continue;
                entries.Add(new Entry {
                    key="gear:"+item.instanceId,id=item.instanceId,
                    name=LabelForGear(item),gear=item,quantity=1,
                    slot=EquipmentFoundation.FindTemplate(item.templateId)==null
                        ? EquipmentSlot.Weapon :
                        EquipmentFoundation.FindTemplate(item.templateId).slot
                });
            }
        if(hero.inventory!=null)
            foreach(var stack in hero.inventory)
            {
                if(stack==null || stack.quantity<=0)continue;
                var charm=EquipmentCatalog.Find(stack.itemId);
                if(filter==1 && charm==null)continue;
                if(filter==2 && charm!=null)continue;
                entries.Add(new Entry {
                    key="stack:"+stack.itemId,id=stack.itemId,
                    name=charm!=null?charm.name:stack.itemId,
                    legacy=charm!=null,quantity=stack.quantity,
                    slot=EquipmentSlot.Artifact
                });
            }
    }

    private Entry FindSelected()
    {
        var hero=Hero();
        if(hero==null || string.IsNullOrEmpty(selectedKey))return null;
        if(selectedKey.StartsWith("gear:",StringComparison.Ordinal))
        {
            var gear=EquipmentRuntime.Find(hero,selectedKey.Substring(5));
            if(gear==null)return null;
            var template=EquipmentFoundation.FindTemplate(gear.templateId);
            return new Entry {
                key=selectedKey,id=gear.instanceId,name=LabelForGear(gear),
                gear=gear,equipped=gear.isEquipped,
                slot=gear.equippedSlot==EquipmentSlot.RingRight ?
                    EquipmentSlot.RingRight :
                    (template==null?EquipmentSlot.Weapon:template.slot)
            };
        }
        if(selectedKey.StartsWith("worn:",StringComparison.Ordinal))
        {
            var definition=EquipmentCatalog.Find(selectedKey.Substring(5));
            if(definition==null || hero.equippedItemId!=definition.id)return null;
            return new Entry {
                key=selectedKey,id=definition.id,name=definition.name,
                legacy=true,equipped=true,slot=EquipmentSlot.Artifact
            };
        }
        foreach(var entry in entries)if(entry.key==selectedKey)return entry;
        return null;
    }
    private void SelectEquipped(EquipmentSlot slot)
    {
        var hero=Hero();
        if(hero==null)return;
        var item=EquipmentRuntime.Equipped(hero,slot);
        if(item!=null)selectedKey="gear:"+item.instanceId;
        else if(slot==EquipmentSlot.Artifact &&
            !string.IsNullOrEmpty(hero.equippedItemId))
            selectedKey="worn:"+hero.equippedItemId;
        else selectedKey="";
        Refresh();
    }
    private void SelectBagCell(int index)
    {
        var offset=page*bagButtons.Length+index;
        if(offset<0 || offset>=entries.Count)return;
        selectedKey=entries[offset].key;
        Refresh();
    }
    private void SwitchTab(int tab)
    {
        filter=tab;
        page=0;
        selectedKey="";
        Refresh();
    }
    private void NextPage()
    {
        if((page+1)*bagButtons.Length<entries.Count)page++;
        Refresh();
    }
    private void PreviousPage()
    {
        if(page>0)page--;
        Refresh();
    }
    private void GrantCharms()
    {
        var gm=Manager();
        if(gm!=null)gm.GrantInventorySamples();
        Refresh();
    }
    private void GrantSword()
    {
        var gm=Manager();
        if(gm!=null)gm.GrantDemoSword();
        Refresh();
    }
    private void EquipSelected()
    {
        var gm=Manager();
        var entry=FindSelected();
        if(gm==null || entry==null || entry.equipped)return;
        if(entry.gear!=null)
        {
            var slot=entry.slot;
            if(slot==EquipmentSlot.RingLeft &&
                EquipmentRuntime.Equipped(gm.HeroData,EquipmentSlot.RingLeft)!=null &&
                EquipmentRuntime.Equipped(gm.HeroData,EquipmentSlot.RingRight)==null)
                slot=EquipmentSlot.RingRight;
            gm.EquipGear(entry.id,slot);
        }
        else if(entry.legacy)gm.EquipInventoryItem(entry.id);
        Refresh();
    }
    private void EquipRight()
    {
        var gm=Manager();
        var entry=FindSelected();
        if(gm==null || entry==null || entry.gear==null ||
            entry.equipped || !EquipmentFoundation.IsSlotAllowed(
                entry.gear,EquipmentSlot.RingRight))return;
        gm.EquipGear(entry.id,EquipmentSlot.RingRight);
        Refresh();
    }
    private void RemoveSelected()
    {
        var gm=Manager();
        var entry=FindSelected();
        if(gm==null || entry==null || !entry.equipped)return;
        if(entry.gear!=null)gm.UnequipGear(entry.gear.equippedSlot);
        else if(entry.legacy)gm.UnequipInventoryItem();
        Refresh();
    }

    private string StatLine(ItemStatLine stat)
    {
        if(stat==null)return "";
        string value=stat.flat!=0f ?
            (stat.flat>0?"+":"")+stat.flat.ToString("0.##") : "";
        if(stat.percent!=0f)
            value+=" "+(stat.percent>0?"+":"")+(stat.percent*100f).ToString("0.##")+"%";
        if(stat.stat==CombatStatId.ElementDamagePercent ||
            stat.stat==CombatStatId.ElementResistance)
            value+=" ("+stat.element+")";
        return stat.stat+": "+value;
    }
    public void Refresh()
    {
        if(root==null)return;
        var hero=Hero();
        RebuildEntries(hero);
        page=Math.Max(0,Math.Min(page,(Math.Max(1,entries.Count)-1)/bagButtons.Length));
        var selected=FindSelected();

        for(int i=0;i<Slots.Length;i++)
        {
            var gear=EquipmentRuntime.Equipped(hero,Slots[i]);
            string title=SlotNames[i];
            string name="Trống";
            if(gear!=null)name=LabelForGear(gear);
            else if(Slots[i]==EquipmentSlot.Artifact && hero!=null &&
                !string.IsNullOrEmpty(hero.equippedItemId))
            {
                var charm=EquipmentCatalog.Find(hero.equippedItemId);
                name=charm==null?"Hộ phù":charm.name;
            }
            slotTexts[i].text=title+"\n"+name;
            ColorButton(slotButtons[i],gear!=null?RarityColor(gear.rarity):
                (name!="Trống" ? new Color(.32f,.34f,.22f,1f) :
                    new Color(.11f,.22f,.24f,1f)));
        }
        for(int i=0;i<bagButtons.Length;i++)
        {
            int absolute=page*bagButtons.Length+i;
            bool has=absolute<entries.Count;
            var entry=has?entries[absolute]:null;
            bagTexts[i].text=entry==null?"·":ShortName(entry.name)+
                (entry.quantity>1?"\nx"+entry.quantity :
                entry.gear==null?"":"\n"+EquipmentFoundation.RarityName(entry.gear.rarity));
            Color color=entry!=null && entry.gear!=null ?
                RarityColor(entry.gear.rarity) :
                entry!=null?new Color(.20f,.29f,.28f,1f):
                new Color(.095f,.17f,.19f,1f);
            if(entry!=null && entry.key==selectedKey)
                color=new Color(.58f,.46f,.21f,1f);
            ColorButton(bagButtons[i],color);
        }
        if(hero==null)
        {
            summary.text="Chưa có Hero.";
            detail.text="Chưa có vật phẩm.";
        }
        else
        {
            summary.text=lastHeight<440 ?
                "Cấp "+hero.currentLevel+" · Đã mặc "+CountWorn(hero)+"/8" :
                "Cấp "+hero.currentLevel+" · Vàng "+hero.gold+
                "\nHP "+hero.GetCalculatedHealth(hero.savedAttackMode)+
                " · ATK "+hero.GetCalculatedDamage(hero.savedAttackMode)+
                "\nĐồ trong kho: "+entries.Count+
                " · Trang bị: "+CountWorn(hero)+"/8";
            if(selected==null)
                detail.text="Chọn một ô trang bị hoặc vật phẩm để xem chi tiết.";
            else if(selected.gear!=null)
            {
                var item=selected.gear;
                detail.text=selected.name+" · "+EquipmentFoundation.RarityName(item.rarity)+
                    " · Cấp "+item.level+" · Rèn +"+item.refineLevel;
                foreach(var line in item.mainStats)detail.text+="\nChính: "+StatLine(line);
                foreach(var line in item.extraStats)
                {
                    var state=EquipmentFoundation.AffixState(item,line);
                    if(state==EquipmentAffixState.Hidden)
                        detail.text+="\nPhụ: ??? (Chưa khai mở)";
                    else if(state==EquipmentAffixState.KnownLocked)
                        detail.text+="\nPhụ [Khóa]: "+StatLine(line);
                    else detail.text+="\nPhụ: "+StatLine(line);
                }
                int totalLines=item.affixSchemaVersion==
                    EquipmentFoundation.AffixSchemaVersion
                    ? item.affixSlotCount : EquipmentFoundation.MaximumExtraLines(item.level);
                int activeLines=0;
                foreach(var line in item.extraStats)
                    if(EquipmentFoundation.AffixState(item,line)==
                        EquipmentAffixState.Active)activeLines++;
                detail.text+="\nDòng phụ kích hoạt: "+activeLines+"/"+totalLines;
            }
            else
            {
                var charm=EquipmentCatalog.Find(selected.id);
                detail.text=selected.name+(selected.equipped?" · Đang mặc":" · Số lượng "+selected.quantity)+
                    (charm!=null?"\n"+EquipmentCatalog.StatLabel(charm):"\nNguyên liệu / vật phẩm cộng dồn");
            }
        }
        int pages=Math.Max(1,(entries.Count+bagButtons.Length-1)/bagButtons.Length);
        bagStatus.text="TÚI ĐỒ · "+entries.Count+" món  ["+(page+1)+"/"+pages+"]";
        if(bannerText!=null)
            bannerText.text=hero==null?
                "HÀNH TRANG TU SĨ\nBảo vật trong túi Càn Khôn" :
                "HÀNH TRANG TU SĨ\n"+
                "Cấp "+hero.currentLevel+"  ·  Bản đồ "+hero.mapNumber+
                "  ·  Vàng "+hero.gold+"\n"+
                "Tâm pháp · Võ học · Luyện chế: sẽ mở trong các giai đoạn tiếp theo";
        equipText.text=selected!=null && !selected.equipped && (selected.gear!=null || selected.legacy) ?
            "Trang bị" : "—";
        removeText.text=selected!=null && selected.equipped?"Tháo":"—";
        equipRightText.text=selected!=null && selected.gear!=null && !selected.equipped &&
            EquipmentFoundation.IsSlotAllowed(selected.gear,EquipmentSlot.RingRight) ?
                "Nhẫn phải" : "—";
        ApplyTheme();
    }

    private static string ShortName(string name)
    {
        if(string.IsNullOrEmpty(name))return "?";
        return name.Length>13 ? name.Substring(0,12)+"…" : name;
    }
    private static int CountWorn(EntityDataSO hero)
    {
        int count=string.IsNullOrEmpty(hero.equippedItemId)?0:1;
        if(hero.equipmentItems!=null)
            foreach(var item in hero.equipmentItems)
                if(item!=null && item.isEquipped)count++;
        return count;
    }

    public void Layout(float combatWidth,float combatHeight,float windowWidth,float windowHeight)
    {
        if(root==null)return;
        lastWidth=Mathf.RoundToInt(windowWidth);
        lastHeight=Mathf.RoundToInt(windowHeight);
        float available=ManagementWindowLayout.SidePanelPixels(lastWidth);
        overlayFallback=available<ManagementWindowLayout.MinimumSidePanel;
        float w=overlayFallback ? Mathf.Min(windowWidth-10f,620f) : available;
        float h=overlayFallback ? Mathf.Min(windowHeight-10f,Mathf.Max(260f,windowHeight-10f))
            : windowHeight;
        float x=overlayFallback ? Mathf.Max(0f,(windowWidth-w)*.5f) : combatWidth;
        float y=overlayFallback ? Mathf.Max(0f,(windowHeight-h)*.5f) : 0f;
        Place(root,x,y,w,h);
        float bannerHeight=windowHeight-combatHeight;
        bool showBanner=ManagementWindowLayout.Expanded &&
            bannerHeight>40f && !overlayFallback;
        if(topBanner!=null)
        {
            topBanner.gameObject.SetActive(IsOpen && showBanner);
            if(showBanner)
            {
                Place(topBanner,0f,0f,combatWidth,bannerHeight);
                var bannerRect=(RectTransform)bannerText.transform;
                Place(bannerRect,25f,18f,
                    Mathf.Min(460f,combatWidth-48f),88f);
                Place(mapArtwork,0f,0f,combatWidth,bannerHeight);
                Place(heroArtwork,combatWidth*.44f,8f,
                    combatWidth*.40f,Mathf.Max(32f,bannerHeight-14f));
                Place(heroArtworkFrame,combatWidth*.42f,0f,
                    combatWidth*.46f,bannerHeight);
                Place(mapNamePlate,12f,10f,
                    Mathf.Min(490f,combatWidth*.62f),110f);
                Place(hangingOrnament,combatWidth-104f,5f,96f,
                    Mathf.Max(35f,bannerHeight-10f));
                UIManager.ReadableText(bannerText,Mathf.Min(19f,Mathf.Max(14f,combatWidth/45f)));
                bannerText.color=new Color(.81f,.85f,.73f,1f);
            }
        }
        Place(headerBacking,4f,4f,w-8f,31f);
        Place(equipmentBacking,5f,40f,Mathf.Min(188f,w*.36f),h-44f);
        Place(inventoryBacking,Mathf.Min(188f,w*.36f)+9f,40f,
            Mathf.Max(100f,w-Mathf.Min(188f,w*.36f)-15f),h-44f);
        Place((RectTransform)header.transform,12f,9f,w-110f,25f);
        Place(FindButton("InventoryClose"),w-94f,7f,86f,26f);

        float left=Mathf.Min(181f,Mathf.Max(135f,w*.34f));
        float gridX=left+15f;
        float gridW=Mathf.Max(150f,w-gridX-12f);
        Place((RectTransform)bagStatus.transform,gridX,65f,gridW-94f,26f);
        string[] tab={"InventoryTab0","InventoryTab1","InventoryTab2"};
        for(int i=0;i<3;i++)
        {
            float tabW=(gridW-8f)/3f;
            Place(tabButtons[i],gridX+i*(tabW+4f),36f,tabW,25f);
            ColorButton(tabButtons[i],filter==i?
                new Color(.25f,.46f,.40f,1f):new Color(.14f,.26f,.28f,1f));
        }
        Place(FindButton("InventoryPrev"),w-94f,64f,40f,26f);
        Place(FindButton("InventoryNext"),w-51f,64f,40f,26f);
        float slotW=(left-16f)/2f;
        float slotH=Mathf.Max(27f,Mathf.Min(48f,(h-185f)/4f-4f));
        float slotStep=slotH+4f;
        for(int i=0;i<Slots.Length;i++)
            Place(slotButtons[i],8f+(i%2)*(slotW+2f),
                72f+(i/2)*slotStep,slotW,slotH);
        float summaryY=72f+4f*slotStep+7f;
        Place((RectTransform)summary.transform,10f,summaryY,left-8f,
            Mathf.Max(16f,h-summaryY-78f));
        Place(miniStatsBacking,7f,Mathf.Max(35f,summaryY-6f),
            left+1f,Mathf.Max(25f,h-summaryY-70f));
        Place(FindButton("InventoryGrantCharms"),9f,h-63f,left-8f,26f);
        Place(FindButton("InventoryGrantSword"),9f,h-33f,left-8f,26f);

        const int cols=5, rows=4;
        float cellW=(gridW-12f)/cols;
        float cellH=Mathf.Max(25f,Mathf.Min(58f,(h-250f)/rows));
        float bagY=96f;
        for(int i=0;i<bagButtons.Length;i++)
            Place(bagButtons[i],gridX+(i%cols)*(cellW+2f),
                bagY+(i/cols)*(cellH+3f),cellW-2f,cellH);
        float detailY=bagY+rows*(cellH+3f)+4f;
        float actionY=h-39f;
        Place((RectTransform)detail.transform,gridX,detailY,gridW,Mathf.Max(18f,actionY-detailY-3f));
        Place(detailBacking,gridX-4f,detailY-4f,gridW+8f,
            Mathf.Max(24f,actionY-detailY+2f));
        float actionW=(gridW-8f)/3f;
        Place(equipButton,gridX,actionY,actionW,31f);
        Place(equipRightButton,gridX+actionW+4f,actionY,actionW,31f);
        Place(removeButton,gridX+2f*(actionW+4f),actionY,actionW,31f);
        StyleText();
        ApplyTheme();
    }
    // Apply all Sprite references by name, never by scene hierarchy search.
    // Sprite fields stay on UIManager via its persistent ScriptableObject
    // reference, since this panel is generated only when the game starts.
    private static void FrameArt(RectTransform rect,Sprite sprite,Color fallback,bool sliced)
    {
        if(rect==null)return;
        var image=rect.GetComponent<Image>();
        if(image==null)return;
        image.sprite=sprite;
        image.color=sprite!=null?Color.white:fallback;
        image.type=sprite!=null && sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect=false;
        image.raycastTarget=false;
    }

    private static void OptionalArt(RectTransform rect,Sprite sprite,
        bool preserveAspect,bool sliced=false,float opacity=1f)
    {
        if(rect==null)return;
        var image=rect.GetComponent<Image>();
        if(image==null)return;
        image.sprite=sprite;
        image.type=sliced && sprite!=null?Image.Type.Sliced:Image.Type.Simple;
        image.preserveAspect=preserveAspect;
        image.color=new Color(1f,1f,1f,Mathf.Clamp01(opacity));
        image.raycastTarget=false;
        rect.gameObject.SetActive(sprite!=null);
    }

    // A theme may be hot-swapped in the Inspector during Play Mode: reopen
    // the inventory, or call UIManager.ApplyInventoryArtFromInspector.
    public void ApplyTheme()
    {
        if(ui==null || root==null)return;
        InventoryArtTheme art=ui.inventoryArtTheme;
        bool sliced=art!=null && art.slicedResizablePanels;
        FrameArt(root,art==null?null:art.inventoryWindowFrame,
            new Color(.046f,.082f,.098f,.99f),sliced);
        FrameArt(headerBacking,art==null?null:art.inventoryHeader,
            new Color(.10f,.19f,.21f,.99f),sliced);
        FrameArt(equipmentBacking,art==null?null:art.equipmentPanel,
            new Color(.078f,.145f,.16f,.96f),sliced);
        FrameArt(inventoryBacking,art==null?null:art.bagPanel,
            new Color(.055f,.115f,.13f,.96f),sliced);
        OptionalArt(miniStatsBacking,art==null?null:art.miniStatsPanel,false,sliced);
        OptionalArt(detailBacking,art==null?null:art.itemDetailsPanel,false,sliced);
        FrameArt(topBanner,art==null?null:art.heroAreaBackground,
            new Color(.048f,.105f,.119f,.965f),sliced);

        for(int i=0;i<slotButtons.Length;i++)
        {
            var image=slotButtons[i]==null?null:slotButtons[i].GetComponent<Image>();
            Color fallback=image==null?Color.white:image.color;
            FrameArt(slotButtons[i],art==null?null:art.equipmentSlot,fallback,false);
        }
        for(int i=0;i<bagButtons.Length;i++)
        {
            int absolute=page*bagButtons.Length+i;
            bool selected=absolute<entries.Count && entries[absolute].key==selectedKey;
            Sprite sprite=art==null?null:
                (selected && art.selectedBagSlot!=null ? art.selectedBagSlot : art.bagSlot);
            var image=bagButtons[i]==null?null:bagButtons[i].GetComponent<Image>();
            Color fallback=image==null?Color.white:image.color;
            // Use a selected tint when there is no dedicated selected sprite.
            FrameArt(bagButtons[i],sprite,fallback,false);
            if(sprite!=null && selected && (art==null || art.selectedBagSlot==null))
                bagButtons[i].GetComponent<Image>().color=new Color(.94f,.85f,.57f,1f);
        }
        for(int i=0;i<tabButtons.Length;i++)
        {
            Sprite sprite=art==null?null:
                (i==filter && art.selectedTab!=null ? art.selectedTab : art.normalTab);
            FrameArt(tabButtons[i],sprite,i==filter?
                new Color(.25f,.46f,.40f,1f):new Color(.14f,.26f,.28f,1f),false);
        }
        foreach(string name in new [] {
            "InventoryClose","InventoryPrev","InventoryNext",
            "InventoryGrantCharms","InventoryGrantSword"
        })
        {
            var button=FindButton(name);
            FrameArt(button,art==null?null:art.actionButton,
                new Color(.16f,.29f,.30f,.98f),false);
        }
        FrameArt(equipButton,art==null?null:art.actionButton,
            new Color(.16f,.29f,.30f,.98f),false);
        FrameArt(equipRightButton,art==null?null:art.actionButton,
            new Color(.16f,.29f,.30f,.98f),false);
        FrameArt(removeButton,art==null?null:art.actionButton,
            new Color(.16f,.29f,.30f,.98f),false);

        EntityDataSO hero=Hero();
        InventoryMapArt map=art==null || hero==null?null:art.FindMap(hero.mapNumber);
        bool preserve=art==null || art.preserveSplashAspect;
        OptionalArt(mapArtwork,map!=null && map.mapSplash!=null?map.mapSplash:
            art==null?null:art.defaultMapSplash,preserve,false,
            art==null?1f:art.mapOpacity);
        OptionalArt(heroArtwork,map!=null && map.heroSplash!=null?map.heroSplash:
            art==null?null:art.defaultHeroSplash,true);
        OptionalArt(heroArtworkFrame,art==null?null:art.heroSplashFrame,false);
        OptionalArt(mapNamePlate,map!=null && map.titlePlate!=null?map.titlePlate:
            art==null?null:art.mapTitlePlate,false,sliced);
        OptionalArt(hangingOrnament,map!=null && map.ornament!=null?map.ornament:
            art==null?null:art.hangingOrnament,true);
        if(art!=null)
        {
            header.color=art.accentTextColor;
            summary.color=art.textColor;
            detail.color=art.textColor;
            bagStatus.color=art.accentTextColor;
        }
    }

    public void StyleText()
    {
        if(root==null)return;
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            UIManager.ReadableText(text,Mathf.Min(15f,Mathf.Max(11f,lastWidth/105f)));
            text.color=new Color(.89f,.91f,.83f,1f);
        }
        header.alignment=TextAlignmentOptions.Left;
        header.color=new Color(.88f,.75f,.48f,1f);
        bagStatus.color=new Color(.50f,.83f,.75f,1f);
        summary.alignment=TextAlignmentOptions.TopLeft;
        detail.alignment=TextAlignmentOptions.TopLeft;
        bagStatus.alignment=TextAlignmentOptions.Left;
    }
}
