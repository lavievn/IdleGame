using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

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
    private RectTransform equipButton, equipRightButton, removeButton, forgeButton;
    private TextMeshProUGUI equipText, equipRightText, removeText;
    private RectTransform forgeOverlay,forgeFrame,forgeCell,forgeCellInside,forgeProgressBack,forgeProgressFill;
    private RectTransform[] forgePlus=new RectTransform[3],forgeMinus=new RectTransform[3];
    private TextMeshProUGUI forgeTitle,forgeCellTitle,forgeCellLevel,forgeCellCount,
        forgeInfo,forgeProgressLabel,forgeMaterialLabel,forgeMessage;
    private string forgeItemId="",lastForgeMessage="";
    private readonly List<Entry> entries = new List<Entry>();
    private int filter, page;
    private string selectedKey = "";
    private int lastWidth,lastHeight;
    private bool overlayFallback;
    private int bagPageSize=20, detailPage;
    private bool showComparison=true;
    private float refreshElapsed;
    private string detailIdentity="";
    private readonly List<string> detailPages=new List<string>();
    private RectTransform detailInfoButton, detailCompareButton, detailPrevious, detailNext;
    private TextMeshProUGUI detailPageLabel;

    // Visual-only guides on UI work branch. Cached per RectTransform to avoid
    // rebuilding GameObjects whenever inventory tabs or window sizes change.
    private sealed class LayoutGuide
    {
        public RectTransform rect;
        public Image shade;
        public Image[] edges;
        public TextMeshProUGUI title;
        public TextMeshProUGUI sizeText;
    }
    private readonly Dictionary<RectTransform,LayoutGuide> layoutGuides =
        new Dictionary<RectTransform,LayoutGuide>();
    private bool lastLayoutDebugEnabled;

    public bool IsOpen => root != null && root.gameObject.activeSelf;
    public bool IsForgeOpen => forgeOverlay != null && forgeOverlay.gameObject.activeInHierarchy;
    public Transform ForgeRoot => forgeOverlay;
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
    private TextMeshProUGUI MakeForgeText(string name,string caption)
    {
        var label=ui.MakeLabel(forgeOverlay,caption,Vector2.zero,new Vector2(200f,30f));
        label.gameObject.name=name;
        label.alignment=TextAlignmentOptions.Center;
        label.raycastTarget=false;
        return label;
    }
    private RectTransform MakeForgeButton(string name,string caption,Action click)
    {
        var button=ui.SmallButton(forgeOverlay,name,caption,new Vector2(0f,1f),
            new Vector2(0f,1f),Vector2.zero,new Vector2(50f,30f),()=>click());
        ColorButton(button,new Color(.17f,.31f,.33f,1f));
        return button;
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

        panel.detailInfoButton=panel.MakeButton("InventoryDetailInfo","Chi tiết",()=>panel.SetDetailView(false));
        panel.detailCompareButton=panel.MakeButton("InventoryDetailCompare","So sánh",()=>panel.SetDetailView(true));
        panel.detailPrevious=panel.MakeButton("InventoryDetailPrevious","<",()=>panel.ChangeDetailPage(-1));
        panel.detailNext=panel.MakeButton("InventoryDetailNext",">",()=>panel.ChangeDetailPage(1));
        panel.detailPageLabel=panel.MakeText("InventoryDetailPage","");
        panel.MakeButton("InventoryPrev","<",panel.PreviousPage);
        panel.MakeButton("InventoryNext",">",panel.NextPage);
        panel.MakeButton("InventoryGrantCharms","Hộ phù mẫu",panel.GrantCharms);
        panel.MakeButton("InventoryGrantSword","Kiếm rèn mẫu",panel.GrantSword);
        panel.MakeButton("InventoryGrantMaterials","Nguyên liệu +100",panel.GrantMaterials);
        panel.equipButton=panel.MakeButton("InventoryEquip","Trang bị",panel.EquipSelected);
        panel.equipRightButton=panel.MakeButton("InventoryEquipRingRight","Nhẫn phải",panel.EquipRight);
        panel.removeButton=panel.MakeButton("InventoryUnequip","Tháo",panel.RemoveSelected);
        panel.forgeButton=panel.MakeButton("InventoryForge","Rèn",panel.OpenForge);
        panel.equipText=ButtonText(panel.equipButton);
        panel.equipRightText=ButtonText(panel.equipRightButton);
        panel.removeText=ButtonText(panel.removeButton);
        panel.MakeButton("InventoryClose","Đóng  ×",panel.Close);

        // Modal is the last child of the inventory root: dim and intercept
        // clicks on the original inventory without changing its layout.
        panel.forgeOverlay=Backdrop(panel.root,"Forge_ModalDim",
            new Color(.012f,.021f,.027f,.88f));
        panel.forgeOverlay.GetComponent<Image>().raycastTarget=true;
        ui.BindClick(panel.forgeOverlay,()=>{});
        panel.forgeFrame=Backdrop(panel.forgeOverlay,"Forge_Window",
            new Color(.075f,.13f,.15f,1f));
        ui.Skin(panel.forgeFrame,UIManager.SurfaceKind.Frame);
        ui.PanelHeader(panel.forgeFrame,30);
        panel.forgeCell=Backdrop(panel.forgeOverlay,"Forge_RarityBorder",Color.gray);
        panel.forgeCellInside=Backdrop(panel.forgeOverlay,"Forge_ItemBackground",
            new Color(.045f,.085f,.10f,1f));
        panel.forgeProgressBack=Backdrop(panel.forgeOverlay,"Forge_ProgressTrack",
            new Color(.08f,.16f,.18f,1f));
        panel.forgeProgressFill=Backdrop(panel.forgeOverlay,"Forge_ProgressFill",
            new Color(.40f,.64f,.50f,1f));
        panel.forgeTitle=panel.MakeForgeText("Forge_Title","LÒ LUYỆN KHÍ · THỬ NGHIỆM");
        panel.forgeCellTitle=panel.MakeForgeText("Forge_ItemName","");
        panel.forgeCellLevel=panel.MakeForgeText("Forge_ItemLevel","");
        panel.forgeCellCount=panel.MakeForgeText("Forge_ForgeCount","");
        panel.forgeInfo=panel.MakeForgeText("Forge_Stats","");
        panel.forgeProgressLabel=panel.MakeForgeText("Forge_ProgressText","");
        panel.forgeMaterialLabel=panel.MakeForgeText("Forge_Materials","");
        panel.forgeMessage=panel.MakeForgeText("Forge_Message","");
        for(int i=0;i<3;i++)
        {
            int row=i;
            panel.forgeMinus[i]=panel.MakeForgeButton("Forge_Down"+i,"−",
                ()=>panel.ChangeForge(row,false));
            panel.forgePlus[i]=panel.MakeForgeButton("Forge_Up"+i,"+",
                ()=>panel.ChangeForge(row,true));
        }
        panel.MakeForgeButton("Forge_Close","Đóng ×",panel.CloseForge);
        panel.forgeOverlay.gameObject.SetActive(false);

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
        detailPage=0;showComparison=true;
        page=0;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        Refresh();
    }
    public void Close()
    {
        if(root==null || !root.gameObject.activeSelf)return;
        if(forgeOverlay!=null)forgeOverlay.gameObject.SetActive(false);
        forgeItemId="";
        root.gameObject.SetActive(false);
        if(topBanner!=null)topBanner.gameObject.SetActive(false);
        if(ui!=null)ui.OnInventoryDismissed();
    }

    private static string LabelForGear(EquipmentInstance item)
    {
        var template=EquipmentFoundation.FindTemplate(item.templateId);
        return EquipmentRefinement.DisplayName(item);
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
                    name=charm!=null?charm.name:
                        stack.itemId==EquipmentRefinement.MaterialId ?
                        EquipmentRefinement.MaterialName : stack.itemId,
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
                slot=gear.isEquipped && gear.equippedSlot==EquipmentSlot.RingRight ?
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
        var offset=page*bagPageSize+index;
        if(index<0 || index>=bagPageSize || offset<0 || offset>=entries.Count)return;
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
        if((page+1)*bagPageSize<entries.Count) {page++;selectedKey="";}
        Refresh();
    }
    private void PreviousPage()
    {
        if(page>0) {page--;selectedKey="";}
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
        if(gm!=null)gm.GrantForgeTestSword();
        Refresh();
    }
    private void GrantMaterials()
    {
        var gm=Manager();
        if(gm!=null)gm.GrantForgeMaterials();
        Refresh();
    }
    private void EquipSelected()
    {
        var gm=Manager();
        var entry=FindSelected();
        if(gm==null || entry==null || entry.equipped)return;
        if(entry.gear!=null)
        {
            var slot=EquipDestination(gm.HeroData,entry.gear);
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

    private static Color ForgeBorderColor(EquipmentRarity rarity)
    {
        switch(rarity)
        {
            case EquipmentRarity.Hoang:return new Color(.48f,.48f,.49f,1f); // iron
            case EquipmentRarity.Huyen:return new Color(.67f,.75f,.83f,1f); // silver
            case EquipmentRarity.Dia:return new Color(.87f,.66f,.30f,1f); // gold
            case EquipmentRarity.Thien:return new Color(.24f,.72f,.52f,1f); // emerald
            case EquipmentRarity.Tien:return new Color(.74f,.85f,.96f,1f); // platinum
            default:return new Color(.36f,.25f,.16f,1f); // wood
        }
    }

    private void OpenForge()
    {
        var selected=FindSelected();
        if(selected==null || selected.gear==null || forgeOverlay==null)return;
        forgeItemId=selected.gear.instanceId;
        lastForgeMessage="";
        forgeOverlay.gameObject.SetActive(true);
        forgeOverlay.SetAsLastSibling();
        LayoutForge(root.rect.width,root.rect.height);
        RefreshForge();
    }
    private void CloseForge()
    {
        if(forgeOverlay!=null)forgeOverlay.gameObject.SetActive(false);
        forgeItemId="";
        lastForgeMessage="";
        // Preserve selected item and return to the underlying inventory.
        if(root!=null && root.gameObject.activeSelf)Refresh();
    }
    private void ChangeForge(int row,bool increase)
    {
        var gm=Manager();
        if(gm==null || string.IsNullOrEmpty(forgeItemId))return;
        EquipmentRefinement.Operation op;
        if(row==0) op=increase ? EquipmentRefinement.Operation.ItemLevelUp :
            EquipmentRefinement.Operation.ItemLevelDown;
        else if(row==1) op=increase ? EquipmentRefinement.Operation.NormalUp :
            EquipmentRefinement.Operation.NormalDown;
        else op=increase ? EquipmentRefinement.Operation.SpecialUp :
            EquipmentRefinement.Operation.SpecialDown;
        string message;
        gm.ApplyForgeTest(forgeItemId,op,out message);
        lastForgeMessage=message;
        Refresh();
    }

    private void RefreshForge()
    {
        if(forgeOverlay==null || !forgeOverlay.gameObject.activeSelf)return;
        var hero=Hero();
        var item=EquipmentRuntime.Find(hero,forgeItemId);
        if(item==null){CloseForge();return;}
        int material=EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId);
        forgeTitle.text="LÒ LUYỆN KHÍ  ·  "+EquipmentFoundation.RarityName(item.rarity);
        forgeCellTitle.text=EquipmentRefinement.DisplayName(item);
        forgeCellLevel.text="Lv."+item.level;
        forgeCellLevel.alignment=TextAlignmentOptions.TopRight;
        forgeCellCount.text="+"+item.refineLevel+"  |  ĐB "+item.specialForgeCount;
        forgeCellCount.alignment=TextAlignmentOptions.BottomRight;
        Color border=ForgeBorderColor(item.rarity);
        ColorButton(forgeCell,border);
        ColorButton(forgeProgressFill,border);
        int count=0;
        string details="";
        foreach(var line in item.extraStats)
        {
            var state=EquipmentFoundation.AffixState(item,line);
            if(state==EquipmentAffixState.Active)count++;
            details+="\n"+(state==EquipmentAffixState.Hidden ?
                "???  · Chưa khám phá" :
                state==EquipmentAffixState.KnownLocked ?
                "<color=#8A9A9D>[Khóa] "+StatLine(item,line)+"</color>" :
                "<color=#BFE9D4>"+StatLine(item,line)+"</color>");
        }
        forgeInfo.text="Chính: "+StatLine(item,item.mainStats[0])+
            "\nDòng phụ: "+count+"/"+item.extraStats.Count+details;
        forgeProgressLabel.text="Phẩm: "+EquipmentFoundation.RarityName(item.rarity)+
            " / Trần: "+EquipmentFoundation.RarityName(item.forgeStageTargetRarity)+
            "  ·  "+EquipmentRefinement.ProgressPercent(item)+"%";
        forgeMaterialLabel.text="Nguyên liệu rèn: "+material+
            "     (+ cấp/rèn: 1  ·  + đặc biệt: 10)";
        forgeMessage.text=lastForgeMessage;
        LayoutForge(root.rect.width,root.rect.height);
        forgeOverlay.SetAsLastSibling();
    }

    private void LayoutForge(float width,float height)
    {
        if(forgeOverlay==null)return;
        float w=Mathf.Max(240f,width),h=Mathf.Max(240f,height);
        Place(forgeOverlay,0f,0f,w,h);
        float fw=Mathf.Min(w-14f,440f),fh=Mathf.Min(h-14f,510f);
        float fx=(w-fw)*.5f,fy=(h-fh)*.5f;
        Place(forgeFrame,fx,fy,fw,fh);
        float scale=Mathf.Min(1f,fh/510f);
        float left=fx+12f,inner=fw-24f;
        Place((RectTransform)forgeTitle.transform,left,fy+9f,inner-80f,28f);
        Place(FindForgeButton("Forge_Close"),fx+fw-81f,fy+7f,73f,27f);
        float cellWidth=128f*scale,cellHeight=124f*scale;
        float cx=fx+(fw-cellWidth)*.5f, cy=fy+37f*scale;
        Place(forgeCell,cx,cy,cellWidth,cellHeight);
        Place(forgeCellInside,cx+4f,cy+4f,cellWidth-8f,cellHeight-8f);
        Place((RectTransform)forgeCellLevel.transform,cx+cellWidth-60f,cy+8f,54f,22f);
        Place((RectTransform)forgeCellCount.transform,cx+5f,cy+cellHeight-28f,cellWidth-10f,23f);
        Place((RectTransform)forgeCellTitle.transform,left,cy+cellHeight+4f,inner,26f);
        float detailsY=cy+cellHeight+32f;
        float detailsH=102f*scale;
        Place((RectTransform)forgeInfo.transform,left,detailsY,inner,detailsH);
        forgeInfo.alignment=TextAlignmentOptions.TopLeft;
        float start=detailsY+detailsH+6f;
        string[] captions={"Cấp vật phẩm","Rèn thường","Rèn đặc biệt"};
        string[] values={forgeItemId==""?"":GetForgeValue(0),
            forgeItemId==""?"":GetForgeValue(1),forgeItemId==""?"":GetForgeValue(2)};
        float rowStep=39f*scale;
        for(int i=0;i<3;i++)
        {
            Place(forgeMinus[i],fx+fw-102f,start+i*rowStep,29f,29f*scale);
            Place(forgePlus[i],fx+fw-36f,start+i*rowStep,29f,29f*scale);
            var label=GetForgeRowLabel(i);
            Place((RectTransform)label.transform,left,start+i*rowStep,fw-130f,29f*scale);
            label.text=captions[i]+"  "+values[i];
            label.alignment=TextAlignmentOptions.MidlineLeft;
        }
        float barY=start+rowStep*3+3f;
        Place((RectTransform)forgeProgressLabel.transform,left,barY,inner,25f*scale);
        Place(forgeProgressBack,left,barY+27f*scale,inner,12f*scale);
        var item=EquipmentRuntime.Find(Hero(),forgeItemId);
        float pct=item==null?0:EquipmentRefinement.ProgressPercent(item)/100f;
        Place(forgeProgressFill,left,barY+27f*scale,inner*pct,12f*scale);
        Place((RectTransform)forgeMaterialLabel.transform,left,barY+46f*scale,inner,25f*scale);
        Place((RectTransform)forgeMessage.transform,left,barY+73f*scale,inner,30f*scale);
    }
    private RectTransform FindForgeButton(string name)
    {
        foreach(Transform child in forgeOverlay)
            if(child.gameObject.name==name)return child as RectTransform;
        return null;
    }
    private TextMeshProUGUI GetForgeRowLabel(int row)
    {
        string name="Forge_Row"+row;
        foreach(Transform child in forgeOverlay)
            if(child.gameObject.name==name)return child.GetComponent<TextMeshProUGUI>();
        return MakeForgeText(name,"");
    }
    private string GetForgeValue(int row)
    {
        var item=EquipmentRuntime.Find(Hero(),forgeItemId);
        if(item==null)return "—";
        if(row==0)return item.level.ToString();
        if(row==2)return "+"+item.specialForgeCount;
        if(item.rarity>=item.forgeStageTargetRarity)
            return "+"+item.refineLevel+"  [Đã chạm trần]";
        return "+"+item.refineLevel+"  ("+item.forgeStageProgress+
            "/"+EquipmentRefinement.RequiredAttempts(item.rarity)+")";
    }

    private static EquipmentSlot EquipDestination(EntityDataSO hero,EquipmentInstance item)
    {
        var template=EquipmentFoundation.FindTemplate(item.templateId);
        var slot=template==null?EquipmentSlot.Weapon:template.slot;
        if(slot==EquipmentSlot.RingLeft &&
            EquipmentRuntime.Equipped(hero,EquipmentSlot.RingLeft)!=null &&
            EquipmentRuntime.Equipped(hero,EquipmentSlot.RingRight)==null)
            return EquipmentSlot.RingRight;
        return slot;
    }

    // UI action eligibility, not a second stat calculator. All numbers below
    // come exclusively from EquipmentStatPreview; never normalize/mutate here.
    private static bool TryComparison(EntityDataSO hero,string key,bool right,
        out EquipmentStatComparison comparison,out string reason)
    {
        comparison=null;reason="Vật phẩm không còn hợp lệ hoặc không có trong kho.";
        if(hero==null || string.IsNullOrEmpty(key))return false;
        bool ok;
        if(key.StartsWith("gear:",StringComparison.Ordinal))
        {
            var item=EquipmentRuntime.Find(hero,key.Substring(5));
            if(!EquipmentFoundation.IsValid(item))return false;
            var slot=item.isEquipped?item.equippedSlot:
                right?EquipmentSlot.RingRight:EquipDestination(hero,item);
            if(!EquipmentFoundation.IsSlotAllowed(item,slot))
            {reason="Không thể trang bị vào ô này.";return false;}
            if(item.isEquipped)
            {
                if(EquipmentRuntime.Equipped(hero,slot)!=item)return false;
                ok=EquipmentStatPreview.TryUnequip(hero,slot,WeaponBasicAttack.Resolve(hero),out comparison);
            }
            else
            {
                if(slot==EquipmentSlot.Artifact && !string.IsNullOrEmpty(hero.equippedItemId))
                {reason="Cần tháo hộ phù đang mặc trước khi dùng Pháp bảo.";return false;}
                ok=EquipmentStatPreview.TryEquip(hero,item.instanceId,slot,WeaponBasicAttack.Resolve(hero),out comparison);
            }
        }
        else
        {
            bool worn=key.StartsWith("worn:",StringComparison.Ordinal);
            if(!worn && !key.StartsWith("stack:",StringComparison.Ordinal))return false;
            string id=key.Substring(worn?5:6);
            if(EquipmentCatalog.Find(id)==null)
            {reason="Vật phẩm này không thể trang bị.";return false;}
            if(worn)
            {
                if(hero.equippedItemId!=id)return false;
            }
            else
            {
                if(EquipmentInventory.Count(hero,id)<=0)return false;
                if(hero.equippedItemId==id)
                {reason="Hộ phù này đã được trang bị.";return false;}
                if(EquipmentRuntime.Equipped(hero,EquipmentSlot.Artifact)!=null)
                {reason="Cần tháo Pháp bảo đang mặc trước khi dùng hộ phù.";return false;}
            }
            // Legacy operations must return the old stack to the bag first.
            if(!string.IsNullOrEmpty(hero.equippedItemId) &&
                EquipmentInventory.Count(hero,hero.equippedItemId)>=EquipmentInventory.MaxQuantity)
            {reason="Kho đã đầy loại hộ phù cần trả về; chưa thể thay hoặc tháo.";return false;}
            ok=worn?EquipmentStatPreview.TryUnequip(hero,EquipmentSlot.Artifact,
                WeaponBasicAttack.Resolve(hero),out comparison):
                EquipmentStatPreview.TryEquipLegacy(hero,id,WeaponBasicAttack.Resolve(hero),out comparison);
        }
        if(ok)reason="";
        return ok;
    }

    private static string ComparisonValue(CombatStatId stat,float value)
    {
        bool ratio=stat==CombatStatId.ElementResistance || stat==CombatStatId.ElementDamagePercent ||
            stat==CombatStatId.PhysicalDamagePercent || stat==CombatStatId.MagicalDamagePercent ||
            stat==CombatStatId.CooldownReductionPercent;
        return (ratio?value*100f:value).ToString("0.###",
            System.Globalization.CultureInfo.InvariantCulture)+(ratio?"%":"");
    }
    private static string ComparisonName(CombatStatId stat)
    {
        switch(stat)
        {
            case CombatStatId.MaxHealth:return "HP tối đa";
            case CombatStatId.Attack:return "Physical Attack";
            case CombatStatId.MagicalAttack:return "Magical Attack";
            case CombatStatId.Armor:return "Giáp";
            case CombatStatId.MagicResistance:return "Kháng phép";
            case CombatStatId.ElementResistance:return "Kháng nguyên tố";
            case CombatStatId.PhysicalDamagePercent:return "Sát thương vật lý";
            case CombatStatId.MagicalDamagePercent:return "Sát thương phép";
            case CombatStatId.ElementDamagePercent:return "Sát thương nguyên tố";
            case CombatStatId.AttackSpeed:return "Tốc đánh (chỉ số)";
            case CombatStatId.MovementSpeed:return "Di chuyển (bonus)";
            case CombatStatId.CastSpeed:return "Tốc thi triển (bonus)";
            case CombatStatId.CooldownReductionPercent:return "Giảm hồi chiêu";
            case CombatStatId.HealthRegeneration:return "Hồi HP/giây";
            case CombatStatId.ManaRegeneration:return "Hồi Mana (bonus)";
            default:return "Hồi HP khi hạ quái";
        }
    }
    private void AddComparisonPages(EntityDataSO hero,bool right)
    {
        EquipmentStatComparison comparison;string reason;
        if(!TryComparison(hero,selectedKey,right,out comparison,out reason))
        {detailPages.Add(reason);return;}
        string action=(comparison.isUnequip?"Tháo":right?"Nút Nhẫn phải":"Trang bị")+
            " · "+SlotNames[Array.IndexOf(Slots,comparison.slot)];
        if(comparison.currentMode!=comparison.mode)
            detailPages.Add(action+"\nĐánh thường\nCurrent: "+comparison.currentMode+
                "\nPreview: "+comparison.mode);
        int changed=0;
        foreach(var line in comparison.lines)
        {
            if(Math.Abs(line.difference)<.00001f)continue;
            changed++;
            string name=ComparisonName(line.stat)+(line.element==ElementType.Vo?"":" · "+line.element);
            // One affected stat per page keeps even the compact fallback readable.
            detailPages.Add(action+"\n"+name+"\nCurrent: "+ComparisonValue(line.stat,line.current)+
                " → Preview: "+ComparisonValue(line.stat,line.preview)+
                "\nDelta: <color="+(line.difference>0f?"#75DF93":"#FF9292")+">"+
                (line.difference>0f?"+":"")+ComparisonValue(line.stat,line.difference)+"</color>");
        }
        if(changed==0)detailPages.Add(action+"\n<color=#C3CCC7>Current = Preview · Delta: 0"+
            "\nKhông đổi chỉ số trong thế hiện tại.</color>");
    }
    private void RefreshDetail(EntityDataSO hero,Entry selected)
    {
        string identity=selectedKey+":"+showComparison;
        if(identity!=detailIdentity) {detailIdentity=identity;detailPage=0;}
        detailPages.Clear();
        if(selected==null)
        {
            selectedKey="";
            detailPages.Add("Chọn vật phẩm để xem chi tiết / so sánh.");
        }
        else if(showComparison)
        {
            AddComparisonPages(hero,false);
            if(selected.gear!=null && !selected.equipped &&
                EquipmentFoundation.IsSlotAllowed(selected.gear,EquipmentSlot.RingRight) &&
                EquipDestination(hero,selected.gear)!=EquipmentSlot.RingRight)
                AddComparisonPages(hero,true);
        }
        else
        {
            // Paginate plain item prose, never cut TMP rich-text tags in half.
            string prose=detail.text.Replace("<color=#8A9A9D>","").Replace("</color>","");
            detailPages.AddRange(UIManager.ReadablePages(prose,detail.rectTransform.rect.width,
                detail.rectTransform.rect.height));
        }
        detailPage=Math.Max(0,Math.Min(detailPage,detailPages.Count-1));
        detail.text=detailPages[detailPage];
        detailPageLabel.text=(detailPage+1)+"/"+detailPages.Count;
    }
    private void SetDetailView(bool compare) {showComparison=compare;detailPage=0;Refresh();}
    private void ChangeDetailPage(int step)
    {
        detailPage=Math.Max(0,Math.Min(detailPage+step,detailPages.Count-1));Refresh();
    }

    private string StatLine(EquipmentInstance item,ItemStatLine stat)
    {
        if(stat==null)return "";
        float power=EquipmentRefinement.PowerMultiplier(item);
        float flat=stat.flat*power,percent=stat.percent*power;
        bool ratio=stat.stat==CombatStatId.ElementDamagePercent ||
            stat.stat==CombatStatId.PhysicalDamagePercent ||
            stat.stat==CombatStatId.MagicalDamagePercent ||
            stat.stat==CombatStatId.CooldownReductionPercent ||
            stat.stat==CombatStatId.ElementResistance;
        string value=flat!=0f ?
            (flat>0?"+":"")+(ratio ? (flat*100f).ToString("0.##")+"%" :
                flat.ToString("0.##")) : "";
        if(percent!=0f)
            value+=" "+(percent>0?"+":"")+(percent*100f).ToString("0.##")+"%";
        if(stat.stat==CombatStatId.ElementDamagePercent ||
            stat.stat==CombatStatId.ElementResistance)
            value+=" ("+stat.element+")";
        return (stat.stat==CombatStatId.Attack || stat.stat==CombatStatId.MagicalAttack?
            ComparisonName(stat.stat):stat.stat.ToString())+": "+value;
    }
    public void Refresh()
    {
        if(root==null)return;
        var hero=Hero();
        RebuildEntries(hero);
        page=Math.Max(0,Math.Min(page,(Math.Max(1,entries.Count)-1)/bagPageSize));
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
            int absolute=page*bagPageSize+i;
            bagButtons[i].gameObject.SetActive(i<bagPageSize);
            bool has=i<bagPageSize && absolute<entries.Count;
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
                "\nHP "+hero.GetCalculatedHealth(WeaponBasicAttack.Resolve(hero))+
                " · Physical "+WeaponBasicAttack.Physical(hero)+" · Magical "+WeaponBasicAttack.Magical(hero)+
                "\nĐồ trong kho: "+entries.Count+
                " · Trang bị: "+CountWorn(hero)+"/8";
            if(selected==null)
                detail.text="Chọn một ô trang bị hoặc vật phẩm để xem chi tiết.";
            else if(selected.gear!=null && !EquipmentFoundation.IsValid(selected.gear))
                detail.text="Vật phẩm không hợp lệ hoặc chưa được hỗ trợ.";
            else if(selected.gear!=null)
            {
                var item=selected.gear;
                detail.text=selected.name+" · "+EquipmentFoundation.RarityName(item.rarity)+
                    " · Cấp "+item.level+" · Rèn +"+item.refineLevel;
                foreach(var line in item.mainStats)detail.text+="\nChính: "+StatLine(item,line);
                foreach(var line in item.extraStats)
                {
                    var state=EquipmentFoundation.AffixState(item,line);
                    if(state==EquipmentAffixState.Hidden)
                        detail.text+="\nPhụ: ??? (Chưa khai mở)";
                    else if(state==EquipmentAffixState.KnownLocked)
                        detail.text+="\n<color=#8A9A9D>Phụ [Khóa]: "+StatLine(item,line)+"</color>";
                    else detail.text+="\nPhụ: "+StatLine(item,line);
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
        RefreshDetail(hero,selected);
        int pages=Math.Max(1,(entries.Count+bagPageSize-1)/bagPageSize);
        bagStatus.text="TÚI ĐỒ · "+entries.Count+" món  ["+(page+1)+"/"+pages+"]";
        if(bannerText!=null)
            bannerText.text=hero==null?
                "HÀNH TRANG TU SĨ\nBảo vật trong túi Càn Khôn" :
                "HÀNH TRANG TU SĨ\n"+
                "Cấp "+hero.currentLevel+"  ·  Vàng "+hero.gold+"\n"+
                HudW3View.MapName(hero)+"  ·  Wave "+HudW3View.MapWave(hero)+"/5\n"+
                "Tâm pháp · Võ học · Luyện chế: sẽ mở trong các giai đoạn tiếp theo";
        if(forgeButton!=null)forgeButton.gameObject.SetActive(selected!=null && selected.gear!=null);
        equipText.text=selected!=null && !selected.equipped && (selected.gear!=null || selected.legacy) ?
            "Trang bị" : "—";
        removeText.text=selected!=null && selected.equipped?"Tháo":"—";
        equipRightText.text=selected!=null && selected.gear!=null && !selected.equipped &&
            EquipmentFoundation.IsSlotAllowed(selected.gear,EquipmentSlot.RingRight) ?
                "Nhẫn phải" : "—";
        ApplyTheme();
        UpdateLayoutDebug();
        if(forgeOverlay!=null && forgeOverlay.gameObject.activeSelf)RefreshForge();
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
                    Mathf.Min(460f,combatWidth-48f),Mathf.Min(132f,Mathf.Max(24f,bannerHeight-36f)));
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
            Mathf.Max(16f,h-summaryY-108f));
        Place(miniStatsBacking,7f,Mathf.Max(35f,summaryY-6f),
            left+1f,Mathf.Max(25f,h-summaryY-100f));
        Place(FindButton("InventoryGrantCharms"),9f,h-93f,left-8f,26f);
        Place(FindButton("InventoryGrantSword"),9f,h-63f,left-8f,26f);
        Place(FindButton("InventoryGrantMaterials"),9f,h-33f,left-8f,26f);

        const int cols=5;
        int rows=h<540f?2:4;
        bagPageSize=cols*rows;
        float cellW=(gridW-12f)/cols;
        float cellH=Mathf.Max(25f,Mathf.Min(58f,(h-300f)/rows));
        float bagY=96f;
        for(int i=0;i<bagButtons.Length;i++)
            Place(bagButtons[i],gridX+(i%cols)*(cellW+2f),
                bagY+(i/cols)*(cellH+3f),cellW-2f,cellH);
        float detailY=bagY+rows*(cellH+3f)+4f;
        float actionY=h-39f;
        // Reserve dedicated rows for detail navigation and Forge, above the actions.
        float detailControlsY=actionY-55f;
        Place((RectTransform)detail.transform,gridX,detailY,gridW,
            Mathf.Max(18f,detailControlsY-detailY-3f));
        float viewW=Mathf.Max(50f,(gridW-116f)*.5f);
        Place(detailInfoButton,gridX,detailControlsY,viewW,23f);
        Place(detailCompareButton,gridX+viewW+3f,detailControlsY,viewW,23f);
        Place(detailPrevious,gridX+gridW-108f,detailControlsY,24f,23f);
        Place((RectTransform)detailPageLabel.transform,gridX+gridW-81f,detailControlsY+3f,52f,20f);
        Place(detailNext,gridX+gridW-26f,detailControlsY,24f,23f);
        Place(detailBacking,gridX-4f,detailY-4f,gridW+8f,
            Mathf.Max(24f,actionY-detailY+2f));
        float actionW=(gridW-8f)/3f;
        Place(equipButton,gridX,actionY,actionW,31f);
        Place(equipRightButton,gridX+actionW+4f,actionY,actionW,31f);
        Place(removeButton,gridX+2f*(actionW+4f),actionY,actionW,31f);
        Place(forgeButton,gridX+gridW-75f,actionY-29f,75f,25f);
        StyleText();
        LayoutForge(w,h);
        ApplyTheme();
        UpdateLayoutDebug();
        Refresh();
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
        image.type=sprite!=null && sliced && sprite.border!=Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
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
        image.type=sliced && sprite!=null && sprite.border!=Vector4.zero?Image.Type.Sliced:Image.Type.Simple;
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
        // Keep all assigned Sprite assets, but intentionally ignore them while
        // the UI layout is being prototyped with its original dark-teal palette.
        InventoryArtTheme art=ui.inventoryUseArtTheme ? ui.inventoryArtTheme : null;
        bool sliced=art!=null && art.slicedResizablePanels;
        FrameArt(root,art==null?null:art.inventoryWindowFrame,
            new Color(.046f,.082f,.098f,.99f),sliced);
        FrameArt(headerBacking,art==null?null:art.inventoryHeader,
            new Color(.10f,.19f,.21f,.99f),sliced);
        FrameArt(equipmentBacking,art==null?null:art.equipmentPanel,
            new Color(.078f,.145f,.16f,.96f),sliced);
        FrameArt(inventoryBacking,art==null?null:art.bagPanel,
            new Color(.055f,.115f,.13f,.96f),sliced);
        FrameArt(miniStatsBacking,art==null?null:art.miniStatsPanel,new Color(.078f,.145f,.16f,.96f),sliced);
        FrameArt(detailBacking,art==null?null:art.itemDetailsPanel,new Color(.055f,.115f,.13f,.96f),sliced);
        FrameArt(topBanner,art==null?null:art.heroAreaBackground,
            new Color(.048f,.105f,.119f,.965f),sliced);

        for(int i=0;i<slotButtons.Length;i++)
        {
            var image=slotButtons[i]==null?null:slotButtons[i].GetComponent<Image>();
            Color fallback=image==null?Color.white:image.color;
            if(art==null)
            {
                var slotHero=Hero();
                var gear=EquipmentRuntime.Equipped(slotHero,Slots[i]);
                bool legacy=i==4 && slotHero!=null &&
                    !string.IsNullOrEmpty(slotHero.equippedItemId);
                fallback=gear!=null?RarityColor(gear.rarity):
                    legacy?new Color(.32f,.34f,.22f,1f):
                    new Color(.11f,.22f,.24f,1f);
            }
            FrameArt(slotButtons[i],art==null?null:art.equipmentSlot,fallback,false);
        }
        for(int i=0;i<bagButtons.Length;i++)
        {
            int absolute=page*bagPageSize+i;
            bool selected=absolute<entries.Count && entries[absolute].key==selectedKey;
            Sprite sprite=art==null?null:
                (selected && art.selectedBagSlot!=null ? art.selectedBagSlot : art.bagSlot);
            var image=bagButtons[i]==null?null:bagButtons[i].GetComponent<Image>();
            Color fallback=image==null?Color.white:image.color;
            if(art==null)
            {
                var entry=absolute<entries.Count?entries[absolute]:null;
                fallback=entry!=null && entry.gear!=null?
                    RarityColor(entry.gear.rarity):
                    entry!=null?new Color(.20f,.29f,.28f,1f):
                    new Color(.095f,.17f,.19f,1f);
                if(entry!=null && entry.key==selectedKey)
                    fallback=new Color(.58f,.46f,.21f,1f);
            }
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
            "InventoryGrantCharms","InventoryGrantSword","InventoryGrantMaterials","InventoryForge"
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
        InventoryMapArt map=art==null || hero==null?null:art.FindMapForHero(hero);
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
        else
        {
            header.color=new Color(.88f,.75f,.48f,1f);
            summary.color=new Color(.89f,.91f,.83f,1f);
            detail.color=new Color(.89f,.91f,.83f,1f);
            bagStatus.color=new Color(.50f,.83f,.75f,1f);
        }
    }

    // RectTransform is measured AFTER Layout and asset application. Height x
    // Width is shown exactly in that order, in Unity canvas units (pixels for
    // our ScreenSpaceOverlay Canvas with scaleFactor 1).
    private static RectTransform CreateGuidePart(Transform parent,string name,
        Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),
            typeof(CanvasRenderer),typeof(Image));
        var rect=(RectTransform)go.transform;
        rect.SetParent(parent,false);
        var image=go.GetComponent<Image>();
        image.color=color;
        image.raycastTarget=false; // never steals a click from item controls
        return rect;
    }

    private LayoutGuide FindOrCreateGuide(RectTransform target)
    {
        LayoutGuide guide;
        if(layoutGuides.TryGetValue(target,out guide))return guide;
        guide=new LayoutGuide();
        guide.rect=CreateGuidePart(target,"__052_LayoutGuide",
            ui.inventoryDebugFill);
        guide.rect.anchorMin=Vector2.zero;
        guide.rect.anchorMax=Vector2.one;
        guide.rect.pivot=new Vector2(.5f,.5f);
        guide.rect.anchoredPosition=Vector2.zero;
        guide.rect.sizeDelta=Vector2.zero;
        guide.rect.SetAsLastSibling();
        guide.shade=guide.rect.GetComponent<Image>();
        guide.edges=new Image[4];
        for(int i=0;i<4;i++)
        {
            var edge=CreateGuidePart(guide.rect,"Edge_"+i,
                ui.inventoryDebugOutline);
            guide.edges[i]=edge.GetComponent<Image>();
        }
        var labelGO=new GameObject("LayoutGuideLabel",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        var labelRect=(RectTransform)labelGO.transform;
        labelRect.SetParent(guide.rect,false);
        labelRect.anchorMin=labelRect.anchorMax=new Vector2(0f,1f);
        labelRect.pivot=new Vector2(0f,1f);
        labelRect.anchoredPosition=new Vector2(4f,-3f);
        guide.title=labelGO.GetComponent<TextMeshProUGUI>();
        guide.title.raycastTarget=false;
        guide.title.alignment=TextAlignmentOptions.TopLeft;
        guide.title.overflowMode=TextOverflowModes.Overflow;
        guide.title.enableWordWrapping=false;
        var sizeGO=new GameObject("LayoutSizeBottomRight",
            typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        var sizeRect=(RectTransform)sizeGO.transform;
        sizeRect.SetParent(guide.rect,false);
        sizeRect.anchorMin=sizeRect.anchorMax=new Vector2(1f,0f);
        sizeRect.pivot=new Vector2(1f,0f);
        sizeRect.anchoredPosition=new Vector2(-3f,3f);
        guide.sizeText=sizeGO.GetComponent<TextMeshProUGUI>();
        guide.sizeText.raycastTarget=false;
        guide.sizeText.alignment=TextAlignmentOptions.BottomRight;
        guide.sizeText.overflowMode=TextOverflowModes.Overflow;
        guide.sizeText.enableWordWrapping=false;
        layoutGuides.Add(target,guide);
        return guide;
    }

    private static void SetGuideEdge(Image image,float x,float y,
        float width,float height,Color color)
    {
        var rect=(RectTransform)image.transform;
        rect.anchorMin=rect.anchorMax=new Vector2(0f,1f);
        rect.pivot=new Vector2(0f,1f);
        rect.anchoredPosition=new Vector2(x,-y);
        rect.sizeDelta=new Vector2(width,height);
        image.color=color;
        image.raycastTarget=false;
    }

    private void Guide(RectTransform target,string name,bool nineSliceRecommended)
    {
        if(target==null)return;
        var item=FindOrCreateGuide(target);
        item.rect.gameObject.SetActive(true);
        item.shade.color=ui.inventoryDebugFill;
        item.shade.raycastTarget=false;
        float w=Mathf.Max(1f,target.rect.width);
        float h=Mathf.Max(1f,target.rect.height);
        Color color=ui.inventoryDebugOutline;
        float thickness=Mathf.Min(2f,Mathf.Min(w,h)*.08f);
        SetGuideEdge(item.edges[0],0f,0f,w,thickness,color);
        SetGuideEdge(item.edges[1],0f,h-thickness,w,thickness,color);
        SetGuideEdge(item.edges[2],0f,0f,thickness,h,color);
        SetGuideEdge(item.edges[3],w-thickness,0f,thickness,h,color);
        var img=target.GetComponent<Image>();
        string actual=img==null || img.sprite==null?"chưa gán":
            img.type==Image.Type.Sliced?"Sliced":"Simple";
        int height=Mathf.RoundToInt(h),width=Mathf.RoundToInt(w);
        bool compact=w<140f || h<64f;
        float fontSize=compact?Mathf.Min(11f,Mathf.Max(7f,h*.26f)):12f;

        // Region name: top-left, separately from the red measurements.
        item.title.text=name;
        item.title.color=ui.inventoryDebugText;
        UIManager.ReadableText(item.title,fontSize);
        item.title.alignment=TextAlignmentOptions.TopLeft;
        item.title.enableWordWrapping=false;
        item.title.overflowMode=TextOverflowModes.Overflow;
        var nameRect=(RectTransform)item.title.transform;
        nameRect.sizeDelta=new Vector2(Mathf.Max(1f,w-8f),
            Mathf.Min(Mathf.Max(12f,h*.40f),26f));

        // Size: always bottom-right, HEIGHT x WIDTH, never painted over title.
        // Keep the 9-slice note on its own second line only when space permits.
        item.sizeText.text=compact
            ?height+"x"+width
            :height+"x"+width+"\n9S:"+
                (nineSliceRecommended?"Nên":"Không")+" · "+actual;
        item.sizeText.color=ui.inventoryDebugSizeText;
        UIManager.ReadableText(item.sizeText,fontSize);
        item.sizeText.alignment=TextAlignmentOptions.BottomRight;
        item.sizeText.enableWordWrapping=false;
        item.sizeText.overflowMode=TextOverflowModes.Overflow;
        var sizeRect=(RectTransform)item.sizeText.transform;
        sizeRect.sizeDelta=new Vector2(Mathf.Max(1f,w-7f),
            compact?Mathf.Min(h-3f,18f):Mathf.Min(43f,h*.43f));
    }

    // Safe for both Inspector toggling during Play and the normal Layout path.
    // All rectangles are real RectTransform values, not inferred screenshot
    // pixels. Small cells use a one-line label to keep their UI clickable.
    public void UpdateLayoutDebug()
    {
        if(ui==null || root==null)return;
        bool enabled=ui.inventoryLayoutDebug;
        lastLayoutDebugEnabled=enabled;
        // First hide previously rendered labels: a region can switch between
        // its Sprite backing and plain text fallback while Theme is edited.
        foreach(var visual in layoutGuides.Values)
            if(visual.rect!=null)visual.rect.gameObject.SetActive(false);
        if(!enabled)return;
        Guide(root,"[01] INVENTORY TỔNG",true);
        Guide(headerBacking,"[02] THANH TIÊU ĐỀ",true);
        Guide(equipmentBacking,"[03] KHUNG TRANG BỊ",true);
        Guide(inventoryBacking,"[04] KHUNG TÚI ĐỒ",true);
        Guide(miniStatsBacking!=null && miniStatsBacking.gameObject.activeSelf ?
            miniStatsBacking:(RectTransform)summary.transform,
            "[05] CHỈ SỐ",true);
        Guide(detailBacking!=null && detailBacking.gameObject.activeSelf ?
            detailBacking:(RectTransform)detail.transform,
            "[06] CHI TIẾT VẬT PHẨM",true);
        Guide(topBanner,"[07] VÙNG HERO / MAP",false);
        Guide(mapNamePlate!=null && mapNamePlate.gameObject.activeSelf ?
            mapNamePlate:(RectTransform)bannerText.transform,
            "[08] TIÊU ĐỀ MAP",false);
        Guide(slotButtons[0],"[09] Ô TRANG BỊ",false);
        Guide(bagButtons[0],"[10] Ô TÚI ĐỒ",false);
        Guide(tabButtons[0],"[11] TAB",true);
        Guide(FindButton("InventoryClose"),"[12] NÚT",true);
    }

    private void LateUpdate()
    {
        // Unscaled polling also sees external gear/stat changes while Inventory pauses play.
        if(IsOpen)
        {
            refreshElapsed+=Time.unscaledDeltaTime;
            if(refreshElapsed>=.2f) {refreshElapsed=0f;Refresh();}
        }
        if(ui!=null && lastLayoutDebugEnabled!=ui.inventoryLayoutDebug)
            UpdateLayoutDebug();
    }

    public void PrintLayoutSizes()
    {
        if(root==null)return;
        string[] names={"Inventory","Header","Equipment","Bag","MiniStats",
            "Details","HeroMap","MapName","EquipCell","BagCell","Tab","Close"};
        RectTransform[] rects={root,headerBacking,equipmentBacking,inventoryBacking,
            miniStatsBacking,detailBacking,topBanner,mapNamePlate,
            slotButtons[0],bagButtons[0],tabButtons[0],FindButton("InventoryClose")};
        for(int i=0;i<rects.Length;i++)
        {
            var rect=rects[i];
            if(rect==null)continue;
            Debug.Log("[UI SIZE 0.0.52] "+names[i]+": Cao x Rộng = "+
                Mathf.RoundToInt(rect.rect.height)+" x "+
                Mathf.RoundToInt(rect.rect.width));
        }
    }

    public void StyleText()
    {
        if(root==null)return;
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            UIManager.ReadableText(text,Mathf.Clamp(root.rect.width/45f,11f,14f));
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
