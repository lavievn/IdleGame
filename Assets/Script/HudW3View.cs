using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.UI;

// A cached view of existing actors/map data. Never creates actors or rewards.
internal sealed class HudW3View
{
    internal readonly RectTransform Root;
    private readonly UIManager ui;
    private readonly Card[] cards = new Card[3];
    private readonly RectTransform map, distance;
    private readonly TextMeshProUGUI mapName, wave, distanceLabel;
    private readonly Image distanceFill;
    private string lastMap;
    private int lastWave=-1;
    private int lastDistance=-1,lastTarget=-1;
    private bool lastExploring;

    private sealed class Card
    {
        internal RectTransform Root, Header, Avatar, HP, Mana;
        internal Image Portrait, Fill;
        internal TextMeshProUGUI Name, Level, Health, ManaLabel, EmptyAvatar;
        internal EntityDataSO Data;
        internal string LastName;
        internal int LastLevel=-1,LastHP=-1,LastMax=-1;
        internal bool LastDead;
    }

    internal HudW3View(UIManager owner, Transform parent)
    {
        ui=owner;
        Root=Rect(parent,"HUD W3");
        Root.anchorMin=Vector2.zero; Root.anchorMax=Vector2.one;
        Root.offsetMin=Root.offsetMax=Vector2.zero;
        for(int i=0;i<cards.Length;i++)
        {
            var c=cards[i]=new Card();
            c.Root=ui.Surface(Root,"Hero Card "+i,UIManager.SurfaceKind.Card);
            ui.BindClick(c.Root,()=>{});
            c.Header=ui.Surface(c.Root,"Header",UIManager.SurfaceKind.Header);
            c.Avatar=ui.Surface(c.Root,"Avatar Frame",UIManager.SurfaceKind.Avatar);
            var portrait=Rect(c.Avatar,"Portrait");
            c.Portrait=portrait.gameObject.AddComponent<Image>();
            c.Portrait.raycastTarget=false; c.Portrait.preserveAspect=true;
            portrait.anchorMin=Vector2.zero; portrait.anchorMax=Vector2.one;
            portrait.offsetMin=new Vector2(2,2); portrait.offsetMax=new Vector2(-2,-2);
            c.EmptyAvatar=Text(c.Avatar,"Avatar placeholder","—");
            c.Name=Text(c.Header,"Hero Name","");
            c.Level=Text(c.Root,"Level","");
            c.HP=ui.Surface(c.Root,"HP Track",UIManager.SurfaceKind.Track);
            c.Fill=ui.Surface(c.HP,"HP Fill",UIManager.SurfaceKind.Health).GetComponent<Image>();
            c.Fill.rectTransform.anchorMin=Vector2.zero;
            c.Fill.rectTransform.anchorMax=Vector2.one;
            c.Fill.rectTransform.offsetMin=c.Fill.rectTransform.offsetMax=Vector2.zero;
            c.Health=Text(c.HP,"HP Value","");
            c.Mana=ui.Surface(c.Root,"Mana Disabled",UIManager.SurfaceKind.Track);
            c.ManaLabel=Text(c.Mana,"Mana Value","Mana —");
            c.ManaLabel.color=new Color(.53f,.65f,.67f,1);
        }
        map=ui.Surface(Root,"Map Information",UIManager.SurfaceKind.Frame);
        ui.BindClick(map,()=>{});
        mapName=Text(map,"Vertical Map Name","");
        wave=Text(map,"Current Wave","");
        distance=ui.Surface(Root,"Exploration Feedback",UIManager.SurfaceKind.Track);
        ui.BindClick(distance,()=>{});
        distanceFill=ui.Surface(distance,"Distance Fill",UIManager.SurfaceKind.Progress).GetComponent<Image>();
        distanceFill.rectTransform.anchorMin=Vector2.zero;
        distanceFill.rectTransform.anchorMax=Vector2.one;
        distanceFill.rectTransform.offsetMin=distanceFill.rectTransform.offsetMax=Vector2.zero;
        distanceLabel=Text(distance,"Actual Distance","");
    }

    private static RectTransform Rect(Transform parent,string name)
    {
        var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
        r.SetParent(parent,false);return r;
    }
    private TextMeshProUGUI Text(Transform parent,string name,string value)
    {
        var t=ui.MakeLabel(parent,value,Vector2.zero,Vector2.zero);
        t.gameObject.name=name; t.enableAutoSizing=false; t.enableWordWrapping=false;
        t.overflowMode=TextOverflowModes.Ellipsis;
        t.richText=false;
        Stretch(t.rectTransform,2,0);
        return t;
    }
    private static void Stretch(RectTransform r,float x,float y)
    {
        r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);
        r.offsetMin=new Vector2(x,y);r.offsetMax=new Vector2(-x,-y);
    }
    internal static void Place(RectTransform r,float x,float y,float w,float h)
    {
        r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        r.localScale=Vector3.one;
    }
    internal void Layout(float width,float height,HudW3Layout l)
    {
        float margin=Mathf.Max(4,l.margin),gap=Mathf.Max(2,l.gap);
        float cw=Mathf.Clamp(l.heroCardWidth,120,Mathf.Max(120,(width-margin-170-2*gap)/3));
        float ch=Mathf.Max(72,l.heroCardHeight);
        float left=width-margin-3*cw-2*gap;
        for(int i=0;i<cards.Length;i++)
        {
            var c=cards[i];
            Place(c.Root,left+i*(cw+gap),margin,cw,ch);
            Place(c.Header,1,1,cw-2,20);
            Place(c.Avatar,5,25,32,32);
            Place(c.Level.rectTransform,39,22,cw-43,16);
            Place(c.HP,41,39,cw-46,16);
            Place(c.Mana,41,57,cw-46,15);
            c.Name.fontSize=l.textSize; c.Level.fontSize=l.textSize-1;
            c.Health.fontSize=11;c.ManaLabel.fontSize=10;c.EmptyAvatar.fontSize=18;
        }
        float mapHeight=Mathf.Min(Mathf.Max(100,l.mapHeight),height-l.mapTop-72);
        Place(map,margin,l.mapTop,l.mapWidth,mapHeight);
        Place(mapName.rectTransform,4,5,l.mapWidth-8,mapHeight-32);
        mapName.fontSize=l.textSize+2;mapName.enableWordWrapping=true;
        Place(wave.rectTransform,2,mapHeight-24,l.mapWidth-4,20);wave.fontSize=l.textSize;
        Place(distance,margin,l.mapTop+mapHeight+gap,l.mapWidth,32);
        distanceLabel.fontSize=11;distanceLabel.enableWordWrapping=true;
    }
    internal void Render(GameManager game,CombatManager combat,InventoryArtTheme art)
    {
        for(int i=0;i<cards.Length;i++)
        {
            HeroCombatState state=combat==null?null:i==0?combat.CurrentHeroCombatState:
                combat.StateFor(game==null?null:game.TestCompanionAt(i-1));
            RenderCard(cards[i],state,i,art);
        }
        var data=game==null?null:game.HeroData;
        string title=MapName(data);
        if(title!=lastMap)
        {
            lastMap=title;
            // Stack words in reading order, retaining Vietnamese diacritics.
            mapName.text=title.Replace(' ','\n');
        }
        int current=MapWave(data);
        if(current!=lastWave) {lastWave=current;wave.text=current==0?"Wave —":"Wave "+current+"/5";}
        int target=game==null?0:Mathf.CeilToInt(game.CurrentDistancePerWave);
        int progress=game==null?0:Mathf.FloorToInt(Mathf.Clamp(game.ExplorationDistance,0,target));
        bool exploring=game!=null && game.IsWaitingForExploration;
        if(progress!=lastDistance || target!=lastTarget || exploring!=lastExploring)
        {
            lastDistance=progress;lastTarget=target;lastExploring=exploring;
            distanceLabel.text=(exploring?"Khám phá":"Chiến đấu")+"\n"+progress+"/"+target;
            distanceFill.rectTransform.anchorMax=new Vector2(target>0?(float)progress/target:0,1);
        }
    }
    private static string MapName(EntityDataSO data)
    {
        if(data==null)return "Chưa vào map";
        WorldMapNode node;
        if(WorldMapCatalog.Enabled(data))return WorldMapCatalog.TryGet(data.worldMapId,out node)?node.Name:"Map "+data.worldMapId;
        return string.IsNullOrWhiteSpace(data.mapName)?"Bản đồ "+data.mapNumber:data.mapName;
    }
    private static int MapWave(EntityDataSO data)
    {
        return data==null?0:Mathf.Clamp(WorldMapCatalog.Enabled(data)?data.worldMapLoopWave:data.completedWavesInMap+1,1,5);
    }
    private static void RenderCard(Card c,HeroCombatState state,int index,InventoryArtTheme art)
    {
        var data=state==null?null:state.Data;
        bool empty=data==null;
        string name=empty?"Chưa có Hero":data.entityName;
        int level=empty?0:data.currentLevel,hp=empty?0:state.CurrentHP,max=empty?0:state.MaxHP;
        bool dead=!empty && hp<=0;
        if(data!=c.Data || name!=c.LastName || level!=c.LastLevel || hp!=c.LastHP || max!=c.LastMax || dead!=c.LastDead)
        {
            c.Data=data;c.LastName=name;c.LastLevel=level;c.LastHP=hp;c.LastMax=max;c.LastDead=dead;
            c.Name.text=name;
            c.Level.text=empty?"Ô trống":(dead?"Đã gục · ":index>0?"B2 · ":"")+"Cấp "+level;
            c.Health.text=empty?"HP —":"HP "+hp+"/"+max;
            c.Fill.rectTransform.anchorMax=new Vector2(max>0?Mathf.Clamp01((float)hp/max):0,1);
            c.Name.color=dead?new Color(1,.56f,.5f):empty?new Color(.55f,.64f,.65f):new Color(.92f,.89f,.75f);
        }
        Sprite portrait=empty || art==null?null:art.hudHeroPortrait;
        if(c.Portrait.sprite!=portrait)c.Portrait.sprite=portrait;
        c.Portrait.enabled=portrait!=null;
        c.EmptyAvatar.enabled=portrait==null;
    }
}
