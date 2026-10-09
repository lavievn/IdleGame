using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Debug form in pixel space, paginated rather than shrinking its eight inputs.
public sealed class DevBalanceUI
{
    public RectTransform Root { get; private set; }
    public bool IsOpen => Root.gameObject.activeSelf;
    public string LastMessage { get; private set; }
    private readonly UIManager owner;
    private readonly TMP_InputField[] inputs = new TMP_InputField[8];
    private readonly RectTransform[] rows = new RectTransform[8];
    private readonly string[] names = { "ATK cơ bản", "ATK thêm/cấp", "Tốc đánh gốc", "Tốc đánh thêm/cấp", "Tốc di chuyển", "Tầm đánh", "HP cơ bản", "HP thêm/cấp" };
    private readonly string[,] drafts = new string[2,8], ranges = new string[2,3];
    private readonly DevBalanceProfile[] defaults = new DevBalanceProfile[2];
    private readonly DevBalanceProfile[] templates = new DevBalanceProfile[2];
    private readonly bool[] resetRequested = new bool[2];
    private bool wasPaused;
    private int target, mode, page, rowsPerPage = 8;
    private TextMeshProUGUI title, modeText, pageText;
    private RectTransform heroTab, monsterTab, resetButton, modeButton, prev, next, okay, cancel;

    public DevBalanceUI(UIManager ui, RectTransform canvas)
    {
        owner = ui;
        var hero = UnityEngine.Object.FindFirstObjectByType<HeroController>();
        defaults[0] = DevBalanceProfile.Defaults(false,EnvironmentManager.Instance,hero != null ? hero.moveSpeed : 300);
        defaults[1] = DevBalanceProfile.Defaults(true,EnvironmentManager.Instance,150);
        var go = new GameObject("DevBalancePanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        Root = (RectTransform)go.transform; Root.SetParent(canvas,false);
        go.GetComponent<Image>().color = new Color(.04f,.06f,.1f,.98f); go.GetComponent<Image>().raycastTarget = false;
        title = owner.MakeLabel(Root,"DEVB",Vector2.zero,new Vector2(300,18));
        heroTab = Button("DevHero","Hero",()=>SelectTarget(0)); monsterTab = Button("DevMonster","Quái",()=>SelectTarget(1));
        resetButton = Button("DevDefaults","Gốc",ResetDraft);
        modeButton = Button("DevMode","",()=>{Capture();mode=(mode+1)%3;Fill();Layout();}); modeText = modeButton.GetComponentInChildren<TextMeshProUGUI>();
        for(int i=0;i<8;i++)
        {
            var row = new GameObject("DevRow"+i,typeof(RectTransform)); rows[i] = (RectTransform)row.transform; rows[i].SetParent(Root,false);
            var label = owner.MakeLabel(rows[i],names[i],new Vector2(66,-13),new Vector2(130,26)); label.alignment = TextAlignmentOptions.TopLeft;
            var inputGO = new GameObject("DevInput"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(TMP_InputField));
            var rt = (RectTransform)inputGO.transform; rt.SetParent(rows[i],false);
            inputGO.GetComponent<Image>().color = new Color(.12f,.19f,.27f,1);
            inputs[i] = inputGO.GetComponent<TMP_InputField>(); inputs[i].targetGraphic = inputGO.GetComponent<Image>();
            var viewportGO = new GameObject("Viewport",typeof(RectTransform),typeof(RectMask2D));
            var viewport = (RectTransform)viewportGO.transform; viewport.SetParent(rt,false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.pivot = new Vector2(.5f,.5f); viewport.sizeDelta = new Vector2(-8,-2); viewport.anchoredPosition = Vector2.zero;
            var text = owner.MakeLabel(viewport,"",Vector2.zero,Vector2.zero); var tr = (RectTransform)text.transform;
            tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.pivot=new Vector2(.5f,.5f);tr.anchoredPosition=Vector2.zero;tr.sizeDelta=Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;
            inputs[i].textViewport = viewport; inputs[i].textComponent = text; inputs[i].pointSize = 14;
            inputs[i].contentType = TMP_InputField.ContentType.Standard; inputs[i].lineType = TMP_InputField.LineType.SingleLine;
            inputs[i].characterLimit = 24;
            var field = inputs[i];owner.BindClick(rt,()=>{if(!field.isFocused)field.ActivateInputField();});
        }
        prev = Button("DevPrevious","‹",()=>ChangePage(-1)); next = Button("DevNext","›",()=>ChangePage(1));
        pageText = owner.MakeLabel(Root,"",Vector2.zero,new Vector2(32,22));
        okay = Button("DevApply","OK",Apply); cancel = Button("DevCancel","Hủy",Close);
        if(owner.transparentWindow!=null)owner.transparentWindow.RegisterClickable(Root);
        go.SetActive(false);
    }
    private RectTransform Button(string name,string label,UnityEngine.Events.UnityAction action)
    { return owner.SmallButton(Root,name,label,new Vector2(0,1),new Vector2(0,1),Vector2.zero,new Vector2(60,24),action); }
    public bool InputAt(Vector2 point)
    {
        foreach(var field in inputs)if(field.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)field.transform,point))return true;
        return false;
    }
    public void Open()
    {
        var hero = UnityEngine.Object.FindFirstObjectByType<HeroController>();
        if(hero!=null)mode=(int)hero.attackMode;
        for(int t=0;t<2;t++) {
            templates[t]=(t==0?CombatBalance.HeroDev:CombatBalance.MonsterDev)?.Copy()??defaults[t].Copy();
            SetDraft(t,templates[t]);resetRequested[t]=false;
        }
        wasPaused=owner.IsPaused;owner.SetPaused(true);
        target=0;page=0;LastMessage="";Root.gameObject.SetActive(true);Root.SetAsLastSibling();Fill();Layout();
    }
    public void Close() { Root.gameObject.SetActive(false);owner.SetPaused(wasPaused); }
    private void SetDraft(int t,DevBalanceProfile p)
    {
        float[] values={p.attack,p.attackPerLevel,p.attackSpeed,p.attackSpeedPerLevel,p.movementSpeed,p.Range((TuTienCore.AttackMode)mode),p.health,p.healthPerLevel};
        for(int i=0;i<8;i++)drafts[t,i]=values[i].ToString("0.#########",CultureInfo.InvariantCulture);
        for(int i=0;i<3;i++)ranges[t,i]=p.Range((TuTienCore.AttackMode)i).ToString("0.#########",CultureInfo.InvariantCulture);
    }
    private void Capture() { for(int i=0;i<8;i++)drafts[target,i]=inputs[i].text; ranges[target,mode]=inputs[5].text; }
    private void Fill() { for(int i=0;i<8;i++)inputs[i].text=i==5?ranges[target,mode]:drafts[target,i]; }
    private void SelectTarget(int t) {Capture();target=t;page=0;LastMessage="";Fill();Layout();}
    private void ChangePage(int step) {Capture();page=Mathf.Clamp(page+step,0,(7/rowsPerPage));Layout();}
    private void ResetDraft() {templates[target]=defaults[target].Copy();SetDraft(target,templates[target]);resetRequested[target]=true;LastMessage="Gốc — bấm OK để áp dụng";Fill();Layout();}
    public static bool TryNumber(string value,out float result)
    {
        return float.TryParse((value??"").Trim().Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out result) &&
            !float.IsNaN(result)&&!float.IsInfinity(result);
    }
    private void Apply()
    {
        Capture();float[] v=new float[8];
        for(int i=0;i<8;i++)if(!TryNumber(drafts[target,i],out v[i])){LastMessage="Kiểm tra: "+names[i];page=i/rowsPerPage;Layout();return;}
        float[] minimum={1,0,.1f,0,0,1,1,0},maximum={100000,10000,20,2,20000,20000,1000000,100000};
        for(int i=0;i<8;i++)if(v[i]<minimum[i]||v[i]>maximum[i]) {
            LastMessage=names[i]+": "+minimum[i].ToString("0.##",CultureInfo.InvariantCulture)+"–"+maximum[i].ToString("0.##",CultureInfo.InvariantCulture);
            page=i/rowsPerPage;Layout();return;
        }
        var p=templates[target].Copy();p.attack=v[0];p.attackPerLevel=v[1];p.attackSpeed=v[2];p.attackSpeedPerLevel=v[3];p.movementSpeed=v[4];p.health=v[6];p.healthPerLevel=v[7];
        for(int i=0;i<3;i++){float range;if(!TryNumber(ranges[target,i],out range)){LastMessage="Tầm đánh không hợp lệ";Layout();return;}p.SetRange((TuTienCore.AttackMode)i,range);}
        if(!p.IsValid()){LastMessage="Số ngoài giới hạn cho phép";Layout();return;}
        bool useOriginal=resetRequested[target]&&SameProfile(p,defaults[target]);
        CombatBalance.SetDevProfile(target==1,useOriginal?null:p);
        var combat=UnityEngine.Object.FindFirstObjectByType<CombatManager>();if(combat!=null)combat.ApplyDevBalance(target==1);
        if(useOriginal) {
            if(target==0){var hero=UnityEngine.Object.FindFirstObjectByType<HeroController>();if(hero!=null)hero.moveSpeed=defaults[0].movementSpeed;}
            else foreach(var monster in MonsterController.ActiveMonsters)if(monster!=null)monster.moveSpeed=defaults[1].movementSpeed;
        }
        bool saved=CombatBalance.SaveDevProfiles();
        LastMessage=saved?"Đã áp dụng và lưu DEVB":"Đã áp dụng; chưa lưu được cấu hình";
        var gm=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(gm!=null)gm.UpdateEventLog(LastMessage+" ("+(target==0?"Hero":"Quái")+").");
        Close();
    }
    private static bool SameProfile(DevBalanceProfile a,DevBalanceProfile b)
    {
        float[] aa={a.attack,a.attackPerLevel,a.attackSpeed,a.attackSpeedPerLevel,a.movementSpeed,a.health,a.healthPerLevel,a.meleeRange,a.physicalRange,a.magicRange};
        float[] bb={b.attack,b.attackPerLevel,b.attackSpeed,b.attackSpeedPerLevel,b.movementSpeed,b.health,b.healthPerLevel,b.meleeRange,b.physicalRange,b.magicRange};
        for(int i=0;i<aa.Length;i++) { float original;TryNumber(bb[i].ToString("0.#########",CultureInfo.InvariantCulture),out original);if(aa[i]!=original)return false; } return true;
    }
    public void Layout()
    {
        float width=Mathf.Min(480,Screen.width-12),height=Mathf.Min(420,Screen.height-12);
        Root.anchorMin=Root.anchorMax=Root.pivot=new Vector2(.5f,.5f);Root.anchoredPosition=Vector2.zero;Root.sizeDelta=new Vector2(width,height);
        rowsPerPage=Mathf.Clamp((int)((height-96)/28),1,8);page=Mathf.Clamp(page,0,7/rowsPerPage);
        Position((RectTransform)title.transform,4,4,width-8,18);
        title.text=string.IsNullOrEmpty(LastMessage)?"DEVB · "+(target==0?"Hero":"Quái"):LastMessage;
        Position(heroTab,4,24,64,22);Position(monsterTab,72,24,64,22);Position(resetButton,width-64,24,60,22);
        Position(modeButton,4,50,width-8,22);modeText.text="Tầm: "+(mode==0?"Cận chiến":mode==1?"Cung":"Phép")+" · đổi";
        for(int i=0;i<8;i++) {
            bool visible=i>=page*rowsPerPage&&i<(page+1)*rowsPerPage;rows[i].gameObject.SetActive(visible);
            Position(rows[i],4,76+(i%rowsPerPage)*28,width-8,26);
            var label=rows[i].GetComponentInChildren<TextMeshProUGUI>(true); Position((RectTransform)label.transform,2,0,(width-12)*.58f,26);label.alignment=TextAlignmentOptions.TopLeft;
            Position((RectTransform)inputs[i].transform,(width-12)*.6f,0,(width-12)*.4f,26);
        }
        float bottom=height-28;
        Position(prev,4,bottom,24,24);Position((RectTransform)pageText.transform,30,bottom,34,24);Position(next,66,bottom,24,24);
        pageText.text=(page+1)+"/"+(7/rowsPerPage+1);
        Position(okay,width-140,bottom,64,24);Position(cancel,width-72,bottom,64,24);
        foreach(var text in Root.GetComponentsInChildren<TextMeshProUGUI>(true))UIManager.ReadableText(text,14);
        foreach(var rect in new[]{heroTab,monsterTab,resetButton,modeButton,prev,next,okay,cancel}) {
            var text=(RectTransform)rect.GetComponentInChildren<TextMeshProUGUI>(true).transform;text.anchorMin=Vector2.zero;text.anchorMax=Vector2.one;text.sizeDelta=new Vector2(-6,-2);text.anchoredPosition=Vector2.zero;text.pivot=new Vector2(.5f,.5f);
        }
    }
    private static void Position(RectTransform rect,float x,float top,float width,float height)
    {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-top);rect.sizeDelta=new Vector2(width,height);}
}
