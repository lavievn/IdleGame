using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;
partial class MotionRegression
{
    static Vector2 ControlCenter(RectTransform rect) { return rect.TransformPoint(rect.rect.center); }
    static void ClickNamed(UIManager ui,Transform root,string name)
    { foreach(var c in root.GetComponentsInChildren<CustomInteractable>(true))if(c.gameObject.name==name){ui.HandleMouseClick(ControlCenter(c.GetRect()));return;}throw new Exception("Missing control "+name); }
    static UIManager ReadableFixture(Field f)
    {
        var ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;ui.transparentWindow=new GameObject().AddComponent<TransparentWindow>();
        var canvas=new GameObject(true);((RectTransform)canvas.transform).sizeDelta=new Vector2(1920,1080);
        ui.systemMenu=new GameObject("SystemMenu",typeof(RectTransform));ui.systemMenu.transform.parent=canvas.transform;ui.systemMenu.SetActive(false);
        var trigger=new GameObject("MenuArea",typeof(RectTransform),typeof(Button));trigger.transform.parent=canvas.transform;
        var pause=new GameObject("BuffIcon",typeof(RectTransform),typeof(Image));pause.transform.parent=canvas.transform;
        var gm=f.combat.gameObject.AddComponent<GameManager>();Set(f.combat,"gameManager",gm);
        gm.eventLog=new GameObject("EventLog",typeof(RectTransform),typeof(Image)).GetComponent<Image>();gm.eventLog.transform.parent=canvas.transform;
        gm.eventLogText=new GameObject("LogText",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();gm.eventLogText.transform.parent=gm.eventLog.transform;gm.eventLogText.fontStyle=FontStyles.Bold;
        Set(gm,"runtimeHeroData",Get<EntityDataSO>(f.combat,"runtimeHeroData"));
        Call(ui,"Start");return ui;
    }
    static void Inside(RectTransform child,RectTransform root,string message)
    {
        Vector2 lo=root.InverseTransformPoint(child.TransformPoint(new Vector2(child.rect.xMin,child.rect.yMin)));
        Vector2 hi=root.InverseTransformPoint(child.TransformPoint(new Vector2(child.rect.xMax,child.rect.yMin+child.rect.height)));
        Check(lo.x>=root.rect.xMin-.1f&&lo.y>=root.rect.yMin-.1f&&hi.x<=root.rect.xMax+.1f&&hi.y<=root.rect.yMin+root.rect.height+.1f,message+": "+child.gameObject.name+" lo="+lo.x+","+lo.y+" hi="+hi.x+","+hi.y+" root="+root.rect.xMin+","+root.rect.yMin+","+root.rect.width+","+root.rect.height);
    }
    static void Patch54cTests()
    {
        Run("release click, button drag, return-to-start drag and cancelled gestures stay distinct",()=>{
            var g=new WindowPointerGesture();Vector2 p;
            g.Begin(new Vector2(200,300),new Vector2(220,330),new Vector2(20,20),true);
            Check(!g.Step(true,new Vector2(220,330),out p),"no activation at press");
            Check(g.Step(false,new Vector2(223,331),out p),"small jitter remains a click");
            g.Begin(new Vector2(200,300),new Vector2(220,330),new Vector2(20,20),true);
            Check(!g.Step(true,new Vector2(250,370),out p)&&g.IsDragging,"drag starts on button after threshold");Near(p.x,230,"grab offset X");Near(p.y,340,"grab offset Y");
            Check(!g.Step(false,new Vector2(220,330),out p),"returning to start cannot trigger original button");
            g.Begin(Vector2.zero,Vector2.zero,Vector2.zero,true);g.Cancel();Check(!g.Step(false,Vector2.zero,out p),"resize cancels pending click");
            g.Begin(Vector2.zero,Vector2.zero,Vector2.zero,false);g.Step(true,new Vector2(100,0),out p);Check(!g.IsDragging,"text selection is not a window drag");
        });
        Run("resize preserves safe origin, respects taskbar work area and negative monitor coordinates",()=>{
            var work=new Rect(0,0,1920,1040);var p=TransparentWindow.KeepInWorkArea(new Vector2(200,200),800,450,work);Near(p.x,200,"no centering");Near(p.y,200,"no bottom docking");
            p=TransparentWindow.KeepInWorkArea(new Vector2(1700,1000),800,450,work);Near(p.x,1120,"right clamp");Near(p.y,590,"taskbar excluded");
            p=TransparentWindow.KeepInWorkArea(p,250,141,work);Near(p.y,590,"shrinking preserves top-left instead of snapping to bottom");
            p=TransparentWindow.KeepInWorkArea(new Vector2(-1800,-200),500,281,new Rect(-1920,-1080,1920,1040));Near(p.x,-1800,"left monitor retained");Near(p.y,-321,"negative work area bottom");
        });
        Run("a completed UI click must release on the same control and a drag never resizes",()=>{
            var f=new Field();var ui=ReadableFixture(f);int clicks=0;
            var button=new GameObject("Probe",typeof(RectTransform),typeof(CustomInteractable));button.transform.parent=Get<RectTransform>(ui,"hudCanvas");var rt=(RectTransform)button.transform;rt.sizeDelta=new Vector2(60,24);rt.anchoredPosition=new Vector2(-300,100);
            var c=button.GetComponent<CustomInteractable>();c.onClickEvent=new UnityEngine.Events.UnityEvent();c.onClickEvent.AddListener(()=>clicks++);ui.RegisterInteractable(c);
            Vector2 center=ControlCenter(rt);ui.HandlePointerClick(center,center+new Vector2(100,0));Check(clicks==0,"release elsewhere ignored");ui.HandlePointerClick(center,center);Check(clicks==1,"one same-control click");
            var g=new WindowPointerGesture();Vector2 pos;g.Begin(Vector2.zero,Vector2.zero,center,true);g.Step(true,new Vector2(50,0),out pos);if(g.Step(false,new Vector2(50,0),out pos))ui.HandlePointerClick(center,center);Check(clicks==1,"drag cannot invoke a button");UIManager.Instance=null;
        });
        Run("readable HUD and DEVB remain on screen at 800 500 and 250 with regular 14px fonts",()=>{
            int oldW=Screen.width,oldH=Screen.height;
            try {foreach(int width in new[]{800,500,250}) {
                Screen.width=width;Screen.height=Mathf.RoundToInt(width*9f/16);var f=new Field();var ui=ReadableFixture(f);ui.ToggleSystemMenu();
                var root=Get<RectTransform>(ui,"hudCanvas");Inside((RectTransform)ui.systemMenu.transform,root,"system menu fits");
                foreach(var text in ui.systemMenu.GetComponentsInChildren<TextMeshProUGUI>()){Check(text.fontStyle==FontStyles.Normal&&!text.enableAutoSizing&&text.fontSize==14,"regular readable font");Inside((RectTransform)text.transform,(RectTransform)ui.systemMenu.transform,"menu text fits");}
                ui.OpenDevBalance();var dev=Get<DevBalanceUI>(ui,"devUI");Inside(dev.Root,root,"DEVB fits viewport");
                var fields=Get<TMP_InputField[]>(dev,"inputs");int visible=0;foreach(var field in fields)if(field.gameObject.activeInHierarchy){visible++;Inside((RectTransform)field.transform,dev.Root,"input fits");Check(field.pointSize==14,"input 14px");}
                Check((width==800?visible==8:width==250?visible==1:visible>1&&visible<8),"paginate rather than shrink inputs");
                foreach(var c in dev.Root.GetComponentsInChildren<CustomInteractable>())Inside(c.GetRect(),dev.Root,"dev control fits");
                Check(ui.IsTextInputAt(ControlCenter((RectTransform)fields[0].transform)),"native text editing exemption");dev.Close();UIManager.Instance=null;
            }} finally {Screen.width=oldW;Screen.height=oldH;Time.timeScale=1;}
        });
        Run("long logs and formulas paginate without deleting text or shrinking font",()=>{
            string text="";for(int i=0;i<40;i++)text+="Linh căn số "+i+" gây sát thương theo trọng số và cấp độ.\n";
            var pages=UIManager.ReadablePages(text,218,73);Check(pages.Count>20,"small viewport needs pages");
            string joined=string.Join(" ",pages.ToArray()).Replace('\n',' ');Check(joined.Contains("số 39")&&joined.Contains("Linh căn số 0"),"first and last data preserved");
            foreach(var page in pages)foreach(var line in page.Split('\n'))Check(line.Length<=27,"wrapped at readable width");
        });
        Run("DEVB growth applies at current level, preserves added points roots and HP percentage",()=>{
            var f=new Field();var hero=HeroAt(20);hero.addedDamage=7;hero.addedHealth=18;f.combat.SetupHeroInfo(hero);Set(f.combat,"currentHeroHP",f.combat.MaxHeroHP/2);var m=f.Monster(-200);f.Battle(m);
            int level=hero.currentLevel,points=hero.statPoints;var roots=new List<ElementType>(hero.spiritRoots);int monsterAtk=f.Enemies[0].data.baseDamage;
            var profile=DevBalanceProfile.Defaults(false,f.space,700);profile.attack=30;profile.attackPerLevel=3;profile.health=1000;profile.healthPerLevel=20;profile.attackSpeed=2;profile.attackSpeedPerLevel=.05f;profile.magicRange=800;
            CombatBalance.SetDevProfile(false,profile);f.combat.ApplyDevBalance(false);
            Check(hero.baseDamage==87&&hero.GetCalculatedDamage()==94,"base and per-level attack plus old allocated points");Check(hero.baseHealth==1380&&hero.addedHealth==18,"growth HP preserves allocated HP");
            Check(hero.currentLevel==level&&hero.statPoints==points&&hero.spiritRoots.Count==roots.Count,"identity level points intact");Near((float)f.combat.CurrentHeroHP/f.combat.MaxHeroHP,.5f,"HP percentage",.003f);
            Near(hero.baseAttackSpeed,2.95f,"explicit DEVB speed bypasses old 1.25 cap");Near(f.hero.moveSpeed,700,"movement immediate");Near(f.space.AttackRange(AttackMode.RangedMagic,true),800,"selected hero magic range");
            Check(f.Enemies[0].data.baseDamage==monsterAtk,"hero tab does not change enemy attack");int oldBase=hero.baseDamage;hero.AddExp(hero.expToNextLevel);Check(hero.baseDamage==oldBase+3,"override survives future level-up");
        });
        Run("DEVB monsters apply to current and later spawns independently of hero",()=>{
            var f=new Field();var m=f.Monster(-200);f.Battle(m);int level=f.Enemies[0].data.currentLevel;f.Enemies[0].currentHP=f.Enemies[0].maxHP/2;
            var p=DevBalanceProfile.Defaults(true,f.space,320);p.attack=40;p.attackPerLevel=2;p.health=200;p.healthPerLevel=30;p.attackSpeed=3;p.attackSpeedPerLevel=.01f;p.physicalRange=600;
            int heroAtk=Get<EntityDataSO>(f.combat,"runtimeHeroData").baseDamage;CombatBalance.SetDevProfile(true,p);f.combat.ApplyDevBalance(true);
            var e=f.Enemies[0];Check(e.data.baseDamage==40+2*(level-1)&&e.maxHP==200+30*(level-1),"live enemy rebuilt");Near(m.moveSpeed,320,"live movement updated");Check(Get<EntityDataSO>(f.combat,"runtimeHeroData").baseDamage==heroAtk,"hero independent");
            Near(f.space.AttackRange(AttackMode.RangedPhysical,false),600,"monster range");f.combat.ForceClearAllMonsters();var fresh=f.Monster(-300);f.Battle(fresh);Check(f.Enemies[0].data.baseDamage==p.AttackAt(f.Enemies[0].data.currentLevel),"future attack profile");Near(fresh.moveSpeed,320,"future movement profile");
        });
        Run("DEVB OK is atomic, invalid values and Cancel do not mutate live stats",()=>{
            string path=Application.persistentDataPath;string dir=Path.Combine(Path.GetTempPath(),"dev-form-"+Guid.NewGuid());Application.persistentDataPath=dir;
            try {var f=new Field();var ui=ReadableFixture(f);ui.OpenDevBalance();var dev=Get<DevBalanceUI>(ui,"devUI");var fields=Get<TMP_InputField[]>(dev,"inputs");
                fields[0].text="90";fields[6].text="NaN";ClickNamed(ui,dev.Root,"DevApply");Check(dev.IsOpen&&CombatBalance.HeroDev==null,"invalid form not partially applied");
                fields[6].text="-1";ClickNamed(ui,dev.Root,"DevApply");Check(CombatBalance.HeroDev==null,"invalid range rejected");
                ClickNamed(ui,dev.Root,"DevCancel");Check(CombatBalance.HeroDev==null&&Time.timeScale==1,"cancel restores prior pause without changes");
                ui.OpenDevBalance();fields[0].text="50";fields[2].text="1,5";ClickNamed(ui,dev.Root,"DevApply");Check(!dev.IsOpen&&CombatBalance.HeroDev.attack==50,"valid OK applied");Near(CombatBalance.HeroDev.attackSpeed,1.5f,"decimal comma accepted");Check(File.Exists(CombatBalance.DevSettingsPath),"independent dev settings saved");
                ui.SetPaused(true);ui.OpenDevBalance();dev.Close();Check(ui.IsPaused,"preexisting pause remains paused");ui.SetPaused(false);UIManager.Instance=null;
            } finally {CombatBalance.ResetDevProfiles();Application.persistentDataPath=path;Time.timeScale=1;if(Directory.Exists(dir))Directory.Delete(dir,true);}
        });
        Run("DEVB original resets are staged until OK and restore original speed limit",()=>{
            string path=Application.persistentDataPath;string dir=Path.Combine(Path.GetTempPath(),"dev-reset-"+Guid.NewGuid());Application.persistentDataPath=dir;
            try {var f=new Field();var ui=ReadableFixture(f);ui.OpenDevBalance();var dev=Get<DevBalanceUI>(ui,"devUI");Get<TMP_InputField[]>(dev,"inputs")[0].text="100";ClickNamed(ui,dev.Root,"DevApply");
                ui.OpenDevBalance();ClickNamed(ui,dev.Root,"DevDefaults");Check(CombatBalance.HeroDev.attack==100,"reset is only a draft");ClickNamed(ui,dev.Root,"DevApply");Check(CombatBalance.HeroDev==null,"default removes override");
                Check(CombatBalance.HeroAttack(1)==10&&CombatBalance.HeroSpeed(999)==1.25f,"real original formula restored");UIManager.Instance=null;
            } finally {CombatBalance.ResetDevProfiles();Application.persistentDataPath=path;Time.timeScale=1;if(Directory.Exists(dir))Directory.Delete(dir,true);}
        });
        Run("DEVB settings round-trip independently, reject corrupt and nonfinite input",()=>{
            string path=Application.persistentDataPath;string dir=Path.Combine(Path.GetTempPath(),"dev-save-"+Guid.NewGuid());Application.persistentDataPath=dir;
            try {var f=new Field();var p=DevBalanceProfile.Defaults(true,f.space,333);p.attack=99;CombatBalance.SetDevProfile(true,p);Check(CombatBalance.SaveDevProfiles(),"saved");CombatBalance.ResetDevProfiles();CombatBalance.LoadDevProfiles();Check(CombatBalance.HeroDev==null&&CombatBalance.MonsterDev.attack==99,"two independent profiles round trip");
                Check(CombatBalance.SaveDevProfiles(),"replace existing dev file");File.WriteAllText(CombatBalance.DevSettingsPath,"bad\n{}");CombatBalance.LoadDevProfiles();Check(CombatBalance.HeroDev==null&&CombatBalance.MonsterDev==null,"corruption cannot leave partial override");
                float n;foreach(string bad in new[]{"NaN","Infinity","1e99","","1,2,3"})Check(!DevBalanceUI.TryNumber(bad,out n),"invalid number "+bad);
            } finally {CombatBalance.ResetDevProfiles();Application.persistentDataPath=path;if(Directory.Exists(dir))Directory.Delete(dir,true);}
        });
        Run("DEVB clears old projectile power and charge without resetting wave progress",()=>{
            var f=new Field();f.hero.ChangeAttackMode(2);f.Battle(f.Monster(-200));CombatStep(f,2);Check(f.combat.PendingProjectileCount==1,"magic in flight");int count=f.Enemies.Count;
            var p=DevBalanceProfile.Defaults(false,f.space,600);CombatBalance.SetDevProfile(false,p);f.combat.ApplyDevBalance(false);
            Check(f.combat.PendingProjectileCount==0&&f.Enemies.Count==count,"same wave with old shots cancelled");Near(Get<float>(f.combat,"heroAttackTimer"),0,"charge reset");
        });
        Run("DEVB range changes feed both AI stop distance and attack eligibility",()=>{
            var f=new Field();var m=f.Monster(-180);var p=DevBalanceProfile.Defaults(false,f.space,150);p.meleeRange=200;CombatBalance.SetDevProfile(false,p);
            Check(f.hero.CanAttack(m,f.space),"new hero range eligible");float x=f.space.Position(f.hero.heroRect).x;f.hero.TickMovement(f.space,.2f);Near(f.space.Position(f.hero.heroRect).x,x,"AI stops at same new range");
            p=DevBalanceProfile.Defaults(true,f.space,150);p.meleeRange=200;CombatBalance.SetDevProfile(true,p);Check(m.CanAttack(f.hero,f.space),"new monster range eligible");
        });
        Run("world damage labels keep readable pixel size and regular font on small windows",()=>{
            int old=Screen.width;
            try {foreach(int width in new[]{800,500,250}) {
                Screen.width=width;var f=new Field();var label=new GameObject("Damage",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                label.transform.parent=f.ground;label.fontStyle=FontStyles.Bold;UIManager.ReadableWorldText(label);
                Near(label.fontSize*(width/1920f),12,"12px damage labels");Check(label.fontStyle==FontStyles.Normal&&!label.enableWordWrapping,"regular single-line damage");
            }}finally{Screen.width=old;}
        });
        Run("DEVB updating a dead hero never revives it and restoring loaded profiles uses original ranges",()=>{
            var f=new Field();f.hero.Die();Set(f.combat,"currentHeroHP",0);var p=DevBalanceProfile.Defaults(false,f.space,600);p.meleeRange=900;CombatBalance.SetDevProfile(false,p);f.combat.ApplyDevBalance(false);
            Check(f.hero.IsDead&&f.combat.CurrentHeroHP==0,"balance change is not resurrection");
            var defaults=DevBalanceProfile.Defaults(false,f.space,150);Near(defaults.meleeRange,35,"original range bypasses loaded override");
        });
        Run("editing the original draft never discards small intentional growth changes",()=>{
            string path=Application.persistentDataPath;string dir=Path.Combine(Path.GetTempPath(),"dev-small-growth-"+Guid.NewGuid());Application.persistentDataPath=dir;
            try {var f=new Field();var ui=ReadableFixture(f);ui.OpenDevBalance();var dev=Get<DevBalanceUI>(ui,"devUI");ClickNamed(ui,dev.Root,"DevDefaults");var fields=Get<TMP_InputField[]>(dev,"inputs");fields[3].text="0.00505";ClickNamed(ui,dev.Root,"DevApply");
                Check(CombatBalance.HeroDev!=null,"edited default stays an explicit override");Near(CombatBalance.HeroDev.attackSpeedPerLevel,.00505f,"small growth edit",.00000001f);
                ui.OpenDevBalance();float shown;Check(DevBalanceUI.TryNumber(fields[3].text,out shown),"growth still numeric");Near(shown,.00505f,"reopen retains precision",.00000001f);dev.Close();UIManager.Instance=null;
            }finally{CombatBalance.ResetDevProfiles();Application.persistentDataPath=path;Time.timeScale=1;if(Directory.Exists(dir))Directory.Delete(dir,true);}
        });
        CombatBalance.ResetDevProfiles();UIManager.Instance=null;Time.timeScale=1;
    }
}
