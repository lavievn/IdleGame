using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManager
{
    private const int SkillQueuePageSize = 5;
    private const float SkillPanelWidth = 556f, SkillPanelHeight = 322f;
    private static readonly Color SkillUnavailable = new Color(.53f,.55f,.57f);
    private static readonly Color SkillSelected = new Color(.97f,.76f,.42f);

    private sealed class SkillSlot
    {
        public RectTransform Root;
        public Image Frame;
        public Outline Outline;
        public SkillSlotIcon Icon;
        public TextMeshProUGUI Number, Caption;
    }

    private RectTransform skillPanel, skillAdd, skillGrant, skillUp, skillDown, skillRemove, skillQueuePrev, skillQueueNext;
    private TextMeshProUGUI skillQueueHeading, skillDetailTitle, skillDescription, skillDamage, skillStats, skillRequirements;
    private readonly SkillSlot[] queueSlots = new SkillSlot[SkillQueuePageSize];
    private SkillSlot[] librarySlots;
    private EntityDataSO skillOwner;
    private EquipmentInstance drawnSkillWeapon;
    private int librarySelection, queueSelection, queuePage, drawnSkillWidth, drawnSkillHeight;
    private bool inspectingQueue, wasPausedBeforeSkills;

    bool SkillPanelOpen => skillPanel != null && skillPanel.gameObject.activeSelf;
    internal bool CombatResourcesMenuOpen => systemMenu != null && systemMenu.activeInHierarchy;

    internal void OpenWeaponSkills()
    {
        var game = GameForUI();
        if (game == null || !game.IsGameplayHUDVisible || game.HasTribulationLiability || game.HeroData == null ||
            game.HeroData.combatSkills == null || SkillPanelOpen) return;
        if (skillPanel == null) CreateSkillPanel();
        CloseDamageInfo();
        CloseSystemMenu();
        CloseWorldMapMenu();
        wasPausedBeforeSkills = IsPaused;
        skillOwner = game.HeroData;
        librarySelection = queueSelection = queuePage = 0;
        inspectingQueue = false;
        var weapon = WeaponBasicAttack.Equipped(skillOwner);
        for (int i = 0; i < WeaponSkillRegistry.All.Count; i++)
            if (WeaponSkillRegistry.All[i].IsAvailable(skillOwner, weapon)) { librarySelection = i; break; }
        skillPanel.gameObject.SetActive(true);
        skillPanel.SetAsLastSibling();
        SetPaused(true);
        RenderWeaponSkills();
    }

    void CloseWeaponSkills(bool restorePause)
    {
        if (!SkillPanelOpen) return;
        skillPanel.gameObject.SetActive(false);
        if (restorePause) SetPaused(wasPausedBeforeSkills);
        skillOwner = null;
    }

    void CreateSkillPanel()
    {
        skillPanel = Surface(hudCanvas,"WeaponSkillsPanel",SurfaceKind.Frame);
        BindClick(skillPanel,()=>{});
        SurfaceAt("SkillHeader",SurfaceKind.Header,0,0,SkillPanelWidth,34);
        SkillText(skillPanel,"SkillTitle","VÕ HỌC  ·  TỰ THI TRIỂN",14,5,370,23,16);
        SkillButton("SkillClose","Đóng",470,5,74,24,()=>CloseWeaponSkills(true));

        var queue = SurfaceAt("SkillQueue",SurfaceKind.Card,8,38,540,94);
        skillQueueHeading = SkillText(queue,"SkillQueueHeading","",9,3,310,17,12);
        for (int i = 0; i < queueSlots.Length; i++)
        {
            int slot = i;
            queueSlots[i] = CreateSkillSlot(queue,"SkillQueueSlot"+i,13+i*61,22,44,59,()=>SelectQueueSlot(slot));
        }
        skillUp = SkillButton("SkillUp","Lên",334,65,58,22,()=>MoveSelectedSkill(-1));
        skillDown = SkillButton("SkillDown","Xuống",399,65,63,22,()=>MoveSelectedSkill(1));
        skillRemove = SkillButton("SkillRemove","Bỏ",469,65,70,22,RemoveSelectedSkill);
        skillQueuePrev = SkillButton("SkillQueuePrev","‹ Trước",334,94,91,22,()=>PageQueue(-1));
        skillQueueNext = SkillButton("SkillQueueNext","Sau ›",448,94,91,22,()=>PageQueue(1));

        var library = SurfaceAt("SkillLibrary",SurfaceKind.Card,8,136,540,74);
        SkillText(library,"SkillLibraryHeading","KHO VÕ HỌC",9,2,290,17,12);
        librarySlots = new SkillSlot[WeaponSkillRegistry.All.Count];
        for (int i = 0; i < librarySlots.Length; i++)
        {
            int slot = i;
            librarySlots[i] = CreateSkillSlot(library,"SkillLibrarySlot"+i,37+i*105,20,37,103,()=>SelectLibrarySlot(slot));
        }
        skillAdd = SkillButton("SkillAdd","Thêm vào hàng",356,154,183,23,AddSelectedSkill);
        skillGrant = SkillButton("SkillGrantDev","Nhận 3 vũ khí mẫu DEV",356,181,183,21,GrantSkillWeapons);

        var details = SurfaceAt("SkillDetails",SurfaceKind.Track,8,214,540,100);
        skillDetailTitle = SkillText(details,"SkillDetailTitle","",9,4,310,18,14);
        skillDescription = SkillText(details,"SkillDescription","",9,23,309,26,11);
        skillDamage = SkillText(details,"SkillDamage","",9,50,310,26,11);
        skillStats = SkillText(details,"SkillStats","",9,79,310,16,11);
        skillRequirements = SkillText(details,"SkillRequirements","",328,7,203,85,11);
        skillRequirements.richText = true;
    }

    RectTransform SurfaceAt(string name,SurfaceKind style,float x,float y,float w,float h)
    {
        var rect=Surface(skillPanel,name,style);
        SkillPlace(rect,x,y,w,h);
        return rect;
    }

    TextMeshProUGUI SkillText(Transform parent,string name,string text,float x,float y,float w,float h,float size)
    {
        var label=MakeLabel(parent,text,Vector2.zero,Vector2.one);
        label.name=name;
        label.alignment=TextAlignmentOptions.TopLeft;
        label.fontSize=size;
        label.enableAutoSizing=false;
        label.richText=false;
        SkillPlace(label.rectTransform,x,y,w,h);
        return label;
    }

    RectTransform SkillButton(string name,string label,float x,float y,float w,float h,Action action)
    {
        var button=SmallButton(skillPanel,name,label,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.one,()=>action());
        SkillPlace(button,x,y,w,h);
        return button;
    }

    SkillSlot CreateSkillSlot(RectTransform row,string name,float x,float y,float size,float captionWidth,Action action)
    {
        var rect=(RectTransform)new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Outline)).transform;
        rect.SetParent(row,false);
        SkillPlace(rect,x,y,size,size);
        BindClick(rect,()=>action());
        var icon=new GameObject("SkillIcon",typeof(RectTransform),typeof(CanvasRenderer),typeof(SkillSlotIcon))
            .GetComponent<SkillSlotIcon>();
        icon.transform.SetParent(rect,false);
        icon.raycastTarget=false;
        SkillPlace(icon.rectTransform,7,7,size-14,size-14);
        var number=SkillText(rect,"Order","",3,2,20,12,9);
        var caption=SkillText(rect,"Caption","",(size-captionWidth)*.5f,size+1,captionWidth,14,10);
        caption.alignment=TextAlignmentOptions.Top;
        caption.textWrappingMode=TextWrappingModes.NoWrap;
        caption.overflowMode=TextOverflowModes.Ellipsis;
        return new SkillSlot { Root=rect, Frame=rect.GetComponent<Image>(), Outline=rect.GetComponent<Outline>(),
            Icon=icon, Number=number, Caption=caption };
    }

    static void SkillPlace(RectTransform rect,float x,float y,float w,float h)
    {
        var topLeft=new Vector2(0,1);
        PlaceUI(rect,topLeft,topLeft,new Vector2(x,-y),new Vector2(w,h));
    }

    void RefreshWeaponSkills()
    {
        if (!SkillPanelOpen) return;
        var game=GameForUI();
        if (game==null || game.HeroData!=skillOwner || !game.IsGameplayHUDVisible || game.HasTribulationLiability)
        {
            CloseWeaponSkills(false);
            return;
        }
        if (drawnSkillWidth!=Screen.width || drawnSkillHeight!=Screen.height ||
            drawnSkillWeapon!=WeaponBasicAttack.Equipped(skillOwner))
            RenderWeaponSkills();
    }

    void RenderWeaponSkills()
    {
        drawnSkillWidth=Screen.width;
        drawnSkillHeight=Screen.height;
        drawnSkillWeapon=WeaponBasicAttack.Equipped(skillOwner);
        var progress=skillOwner.combatSkills;
        int count=progress.queue.Count;

        int lastPage=count/SkillQueuePageSize;
        queuePage=Mathf.Clamp(queuePage,0,lastPage);
        PlaceUI(skillPanel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(SkillPanelWidth,SkillPanelHeight));
        skillPanel.localScale=Vector3.one*Mathf.Min(1f,
            (ManagementWindowLayout.CombatPixels(Screen.width)-12f)/SkillPanelWidth,(Screen.height-10f)/SkillPanelHeight);

        skillQueueHeading.text=$"DANH SÁCH THI TRIỂN  ·  {count}  ·  Trang {queuePage+1}/{lastPage+1}";
        for (int i=0;i<queueSlots.Length;i++)
        {
            int index=queuePage*SkillQueuePageSize+i;
            bool occupied=index<count;
            var skill=occupied?WeaponSkillRegistry.Find(progress.queue[index]):null;
            RenderSkillSlot(queueSlots[i],skill,occupied,inspectingQueue && queueSelection==index,
                (index+1).ToString(),occupied?"Không rõ":"Trống");
        }
        for (int i=0;i<librarySlots.Length;i++)
            RenderSkillSlot(librarySlots[i],WeaponSkillRegistry.All[i],true,!inspectingQueue && librarySelection==i,"","");

        bool selectedQueue=inspectingQueue && queueSelection<count;
        var selected=inspectingQueue
            ? selectedQueue?WeaponSkillRegistry.Find(progress.queue[queueSelection]):null
            : WeaponSkillRegistry.All[librarySelection];
        skillUp.gameObject.SetActive(selectedQueue && queueSelection>0);
        skillDown.gameObject.SetActive(selectedQueue && queueSelection+1<count);
        skillRemove.gameObject.SetActive(selectedQueue);
        skillQueuePrev.gameObject.SetActive(queuePage>0);
        skillQueueNext.gameObject.SetActive(queuePage<lastPage);
        skillAdd.gameObject.SetActive(!inspectingQueue && selected!=null &&
            selected.IsAvailable(skillOwner,drawnSkillWeapon) && !progress.queue.Contains(selected.Id));
        skillGrant.gameObject.SetActive(GameManager.DevelopmentItemGrantsEnabled);
        RenderSkillDetails(selected,selectedQueue,progress);
    }

    void RenderSkillSlot(SkillSlot slot,WeaponSkill skill,bool occupied,bool selected,string number,string fallback)
    {
        bool usable=skill!=null && skill.IsAvailable(skillOwner,drawnSkillWeapon);
        bool grey=occupied && !usable;
        var theme=inventoryUseArtTheme?inventoryArtTheme:null;
        var sprite=theme==null || grey?null:selected && theme.selectedBagSlot!=null?theme.selectedBagSlot:theme.bagSlot;
        var slotColor=grey?new Color(.16f,.17f,.18f):usable?new Color(.13f,.27f,.28f):new Color(.07f,.11f,.12f);
        ApplySurfaceArt(slot.Frame,sprite,slotColor,theme!=null && theme.slicedResizablePanels);
        slot.Outline.effectColor=selected?SkillSelected:new Color(.29f,.34f,.34f);
        slot.Outline.effectDistance=selected?new Vector2(2,2):Vector2.one;
        slot.Icon.SetSkill(skill?.Effect,grey);
        slot.Number.text=number;
        slot.Number.color=selected?SkillSelected:grey?SkillUnavailable:Color.white;
        slot.Caption.text=skill?.Name ?? fallback;
        slot.Caption.color=grey?SkillUnavailable:Color.white;
    }

    void RenderSkillDetails(WeaponSkill skill,bool occupied,CombatSkillProgress progress)
    {
        if (skill==null)
        {
            skillDetailTitle.text=occupied?"Kỹ năng không rõ":"Ô thi triển trống";
            skillDescription.text=occupied?"Mục này được giữ trong hàng đợi.":"Chọn một kỹ năng ở Kho võ học, sau đó bấm Thêm vào hàng.";
            skillDamage.text=skillStats.text="";
            skillRequirements.text=occupied?"<color=#FF8888>Không khả dụng: không tìm thấy dữ liệu kỹ năng</color>":"";
            return;
        }
        skillDetailTitle.text=skill.Name;
        skillDescription.text=skill.Description;
        skillDamage.text=skill.DamageDescription();
        skillStats.text=$"{skill.Cost:g} {skill.Resource}   ·   Vận {SkillSeconds(skill.CastTime)}   ·   Hồi {SkillSeconds(skill.Cooldown)}";
        bool usable=skill.IsAvailable(skillOwner,drawnSkillWeapon);
        var requirements=new StringBuilder(usable
            ? skill.Source==CombatSkillSource.EquipmentGranted?"Trang bị cấp · Dùng được":"Đã học · Dùng được"
            : "<color=#FF8888>Không khả dụng · Tham khảo</color>");
        foreach (var condition in skill.Requirements(skillOwner,drawnSkillWeapon))
        {
            requirements.AppendLine();
            if (!condition.Met) requirements.Append("<color=#FF8888>");
            requirements.Append(condition.Stage==SkillRequirementStage.Cast?"Khi thi triển: ":"Yêu cầu: ").Append(condition.Label);
            if (!condition.Met) requirements.Append("</color>");
        }
        float remaining=progress.Remaining(skill.Id);
        if (remaining>0f) requirements.AppendLine().Append("Hồi chiêu còn ").Append(SkillSeconds(remaining));
        skillRequirements.text=requirements.ToString();
    }

    static string SkillSeconds(float value) => value.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture).Replace('.',',')+" giây";

    void SelectQueueSlot(int slot)
    {
        inspectingQueue=true;
        queueSelection=queuePage*SkillQueuePageSize+slot;
        RenderWeaponSkills();
    }

    void SelectLibrarySlot(int slot)
    {
        inspectingQueue=false;
        librarySelection=slot;
        RenderWeaponSkills();
    }

    void PageQueue(int delta)
    {
        queuePage+=delta;
        SelectQueueSlot(0);
    }

    void AddSelectedSkill()
    {
        if (!SkillPanelOpen || inspectingQueue) return;
        var selected=WeaponSkillRegistry.All[librarySelection];
        if (!selected.IsAvailable(skillOwner,WeaponBasicAttack.Equipped(skillOwner)) ||
            !skillOwner.combatSkills.Add(selected.Id)) return;
        skillOwner.isDirty=true;
        queueSelection=skillOwner.combatSkills.queue.Count-1;
        queuePage=queueSelection/SkillQueuePageSize;
        inspectingQueue=true;
        RenderWeaponSkills();
    }

    void RemoveSelectedSkill()
    {
        var progress=skillOwner.combatSkills;
        if (!inspectingQueue || queueSelection>=progress.queue.Count || !progress.Remove(progress.queue[queueSelection])) return;
        skillOwner.isDirty=true;
        RenderWeaponSkills();
    }

    void MoveSelectedSkill(int delta)
    {
        if (!inspectingQueue || !skillOwner.combatSkills.Move(queueSelection,delta)) return;
        skillOwner.isDirty=true;
        queueSelection+=delta;
        queuePage=queueSelection/SkillQueuePageSize;
        RenderWeaponSkills();
    }

    void GrantSkillWeapons()
    {
        GameForUI()?.GrantWeaponSkillSamples();
        RenderWeaponSkills();
    }
}

// Compact line icons use the existing UI mesh path. The Inventory skin supplies the slots.
internal sealed class SkillSlotIcon : MaskableGraphic
{
    private SkillEffect? effect;

    internal void SetSkill(SkillEffect? value,bool grey)
    {
        effect=value;
        color=grey?new Color(.59f,.61f,.64f):
            value==SkillEffect.ArrowVolley?new Color(.98f,.83f,.49f):
            value==SkillEffect.LegacyFireball?new Color(1f,.62f,.33f):new Color(.72f,.91f,1f);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        switch(effect)
        {
            case SkillEffect.PiercingThrust:
                Line(mesh,.18f,.18f,.86f,.86f); Line(mesh,.27f,.51f,.51f,.27f);
                Line(mesh,.52f,.80f,.86f,.86f); Line(mesh,.86f,.86f,.80f,.52f);
                break;
            case SkillEffect.ArrowVolley:
                Line(mesh,.25f,.14f,.72f,.50f); Line(mesh,.72f,.50f,.25f,.86f);
                Line(mesh,.25f,.86f,.25f,.14f); Line(mesh,.08f,.50f,.91f,.50f);
                Line(mesh,.72f,.64f,.91f,.50f); Line(mesh,.91f,.50f,.72f,.36f);
                break;
            case SkillEffect.LegacyFireball:
                Line(mesh,.51f,.13f,.21f,.34f); Line(mesh,.21f,.34f,.24f,.58f);
                Line(mesh,.24f,.58f,.53f,.91f); Line(mesh,.53f,.91f,.56f,.56f);
                Line(mesh,.56f,.56f,.73f,.68f); Line(mesh,.73f,.68f,.82f,.37f);
                Line(mesh,.82f,.37f,.51f,.13f);
                break;
        }
    }

    private void Line(VertexHelper mesh,float ax,float ay,float bx,float by)
    {
        Rect bounds=rectTransform.rect;
        var a=new Vector2(bounds.xMin+ax*bounds.width,bounds.yMin+ay*bounds.height);
        var b=new Vector2(bounds.xMin+bx*bounds.width,bounds.yMin+by*bounds.height);
        var edge=new Vector2(a.y-b.y,b.x-a.x).normalized*1.25f;
        int i=mesh.currentVertCount;
        mesh.AddVert(a-edge,color,Vector2.zero); mesh.AddVert(a+edge,color,Vector2.zero);
        mesh.AddVert(b+edge,color,Vector2.zero); mesh.AddVert(b-edge,color,Vector2.zero);
        mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i,i+2,i+3);
    }
}
