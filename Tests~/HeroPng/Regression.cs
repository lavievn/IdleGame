using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;
using Bridge=CultivationSpriteBridge;
static class HeroPngRegression
{
    const string Root="Assets/Resources/";
    static int tests;
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    static void Near(float a,float b,string reason){Check(Math.Abs(a-b)<.001f,reason+": "+a+" != "+b);}
    static void Run(string name,Action test){test();tests++;Console.WriteLine("PASS "+name);}
    static void Call(Bridge b,string method){typeof(Bridge).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(b,null);}
    static T Get<T>(Bridge b,string field){return (T)typeof(Bridge).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(b);}
    static Texture2D Frame(string path){return Resources.Load<Texture2D>(path);}
    static void At(Bridge b,float time){Time.time=time;Call(b,"Update");}
    static HeroController Hero()
    {
        Time.time=10f;Time.timeScale=1f;Time.frameCount++;
        var go=new GameObject("Owned Hero",typeof(RectTransform),typeof(Image));
        var h=go.AddComponent<HeroController>();h.heroRect=(RectTransform)go.transform;
        var bar=new GameObject("Owned HP",typeof(RectTransform));bar.transform.SetParent(h.heroRect,false);
        var fill=new GameObject("Owned fill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(bar.transform,false);
        h.hpFillImage=fill.GetComponent<Image>();h.hpFillImage.fillAmount=.625f;
        Bridge.AttachHero(h);return h;
    }
    static Bridge B(HeroController h){return h.heroRect.GetComponent<Bridge>();}
    static string Prefix(Bridge b,string clip)
    {
        var p=Get<object>(b,"heroProfile");return (string)p.GetType().GetField(clip+"Prefix").GetValue(p);
    }
    static void Move(HeroController h,HeroState state,float distance)
    {h.CurrentState=state;h.IsExplorationMovement=state==HeroState.Returning;h.MovementFrame=Time.frameCount;h.MovementDistanceThisFrame=distance;Call(B(h),"Update");}
    static int SpriteCount(){int n=0;foreach(var o in UnityEngine.Object.objects)if(o is Sprite)n++;return n;}
    static void Main()
    {
        Resources.Register(Bridge.DefaultHeroTexturePath,new Texture2D(900,600,TextureFormat.RGBA32,false));
        Resources.Register("HeroAnimation/SharedHero",new TextAsset(File.ReadAllText(Root+"HeroAnimation/SharedHero.json")));
        foreach(var path in Directory.GetFiles(Root+"HeroAnimation","*.png",SearchOption.AllDirectories))
            Resources.Register(path.Substring(Root.Length,path.Length-Root.Length-4),new Texture2D(360,500,TextureFormat.RGBA32,false));
        Run("full sequences bind in numeric export order and Idle loops at configured FPS",()=>{
            var h=Hero();var b=B(h);var idle=Get<Sprite[]>(b,"normalFrames");
            Check(idle.Length==48 && Get<Sprite[]>(b,"walkFrames").Length==17 && Get<Sprite[]>(b,"runFrames").Length==11 && Get<Sprite[]>(b,"hitFrames").Length==27,"counts");
            for(int i=0;i<48;i++)Check(idle[i].texture==Frame(Prefix(b,"idle")+i.ToString("D5")),"numeric order "+i);
            At(b,10.05f);Check(b.BodyGraphic.sprite==idle[1],"24 FPS");
            At(b,12.01f);Check(b.BodyGraphic.sprite==idle[0],"wrap");
        });
        Run("movement telemetry boundary chooses Walk/Run; stationary, stale and undeployed choose Idle",()=>{
            var h=Hero();var b=B(h);
            Move(h,HeroState.Returning,3f);Check(b.CurrentMotion==Bridge.Motion.Walk,"exploration Walk");
            Move(h,HeroState.Approaching,.1f);Check(b.CurrentMotion==Bridge.Motion.Run,"approach Run, no speed threshold");
            Move(h,HeroState.Combat,.1f);Check(b.CurrentMotion==Bridge.Motion.Run,"last approach step entering range still Run");
            Move(h,HeroState.Approaching,0f);Check(b.CurrentMotion==Bridge.Motion.Idle,"blocked approach Idle");
            Move(h,HeroState.Returning,4f);Time.frameCount++;Call(b,"Update");Check(b.CurrentMotion==Bridge.Motion.Idle,"stale movement Idle");
            h.IsDeployed=false;Move(h,HeroState.Returning,4f);Check(b.CurrentMotion==Bridge.Motion.Idle,"undeployed Idle");
        });
        Run("Walk/Run loop; pause holds frame and clock; no frame loads during playback",()=>{
            var h=Hero();var b=B(h);Move(h,HeroState.Returning,1);
            At(b,10.05f);Check(b.BodyGraphic.sprite==Get<Sprite[]>(b,"walkFrames")[1],"walk frame");
            At(b,10.72f);Check(b.BodyGraphic.sprite==Get<Sprite[]>(b,"walkFrames")[0],"walk wrap");
            Move(h,HeroState.Approaching,1);At(b,11.19f);Check(b.BodyGraphic.sprite==Get<Sprite[]>(b,"runFrames")[0],"run wrap");
            Time.timeScale=0;var frame=b.BodyGraphic.sprite;Time.frameCount++;Call(b,"Update");Check(b.BodyGraphic.sprite==frame && b.CurrentMotion==Bridge.Motion.Run,"pause");
            Time.timeScale=1;int loads=Resources.LoadCallCount;for(int i=0;i<100;i++)At(b,Time.time+.016f);Check(loads==Resources.LoadCallCount,"cached playback");
        });
        Run("Attack keeps old Normal single frame for .16s, blocks Hit, returns to movement",()=>{
            var h=Hero();var b=B(h);Move(h,HeroState.Approaching,1);
            Bridge.Attack(h.heroRect);var deadline=Get<float>(b,"returnAt");Near(deadline,10.16f,"old attack window");
            Check(b.BodyGraphic.sprite.texture==Frame(Bridge.DefaultHeroTexturePath),"existing frame, not Hit or new art");
            Bridge.Hit(h.heroRect);Check(b.CurrentPose==Bridge.Pose.Attack && Get<float>(b,"returnAt")==deadline,"Hit ignored not queued");
            At(b,10.17f);Check(b.CurrentPose==Bridge.Pose.Normal && b.BodyGraphic.sprite.texture!=Frame(Bridge.DefaultHeroTexturePath) && b.CurrentMotion==Bridge.Motion.Run,"resume Run");
        });
        Run("Hit plays all 27 frames once; Attack can interrupt; disable/reset clears old state",()=>{
            var h=Hero();var b=B(h);Bridge.Hit(h.heroRect);
            At(b,10.88f);Check(b.BodyGraphic.sprite==Get<Sprite[]>(b,"hitFrames")[26],"last Hit frame");
            At(b,10.91f);Check(b.CurrentPose==Bridge.Pose.Normal,"one-shot end");
            Bridge.Hit(h.heroRect);Bridge.Attack(h.heroRect);Check(b.CurrentPose==Bridge.Pose.Attack,"Attack wins");
            h.heroRect.gameObject.SetActive(false);Check(b.CurrentPose==Bridge.Pose.Normal && b.CurrentMotion==Bridge.Motion.Idle,"disable clears");
            h.heroRect.gameObject.SetActive(true);Bridge.AttachHero(h);Check(b.CurrentPose==Bridge.Pose.Normal,"reuse clear");
        });
        Run("same Image/clip set across modeled weapon modes; no root/HP/opacity changes",()=>{
            var h=Hero();var b=B(h);var body=b.BodyGraphic;var first=Get<Sprite[]>(b,"normalFrames");
            var root=h.heroRect;var pos=root.anchoredPosition;var size=root.sizeDelta;var hpPos=h.hpFillImage.transform.parent.position;
            foreach(var mode in new[]{AttackMode.Melee,AttackMode.RangedPhysical,AttackMode.RangedMagic,AttackMode.Melee})
            {h.attackMode=mode;Bridge.AttachHero(h);Check(b.BodyGraphic==body && Get<Sprite[]>(b,"normalFrames")==first,"identity independent of weapon boundary");}
            Bridge.SetBodyOpacity(root,.3f);
            foreach(var state in new[]{HeroState.Returning,HeroState.Approaching})
            {Move(h,state,1);At(b,Time.time+.09f);Near(b.BodyGraphic.color.a,.3f,"opacity preserved");Near(body.rectTransform.pivot.x,220f/360,"fixed feet x");Near(body.rectTransform.pivot.y,17f/500,"fixed feet y");Near(body.rectTransform.sizeDelta.x,60,"full canvas width");}
            Bridge.Hit(root);Bridge.Attack(root);At(b,Time.time+.2f);
            Check(root.anchoredPosition.Equals(pos) && root.sizeDelta.Equals(size) && root.localScale.Equals(Vector3.one),"logic geometry");
            Check(h.hpFillImage.transform.parent.position.Equals(hpPos),"HP position");Near(h.hpFillImage.fillAmount,.625f,"HP amount");
            Check(root.GetComponentsInChildren<Image>(true).Length==3,"one body plus source and HP");
        });
        Run("missing frame rejects only that whole clip; falls back to legacy frame, no leaked partial Sprites",()=>{
            var sample=Hero();string path=Prefix(B(sample),"walk")+"00008";var texture=Frame(path);Resources.Unregister(path);
            int before=SpriteCount();var h=Hero();var b=B(h);
            Check(Get<Sprite[]>(b,"walkFrames")==null && Get<Sprite[]>(b,"normalFrames").Length==48,"partial rejected, Idle retained");
            Check(SpriteCount()-before==1+48+11+27,"partial Sprites destroyed");
            Move(h,HeroState.Returning,1);Check(b.BodyGraphic.sprite.texture==Frame(Bridge.DefaultHeroTexturePath),"legacy fallback for missing Walk");
            Resources.Register(path,texture);
        });
        Run("destroy releases every owned Sprite; missing profile keeps old presentation",()=>{
            var h=Hero();var b=B(h);int before=SpriteCount();Call(b,"OnDestroy");Check(SpriteCount()==before-104,"legacy plus 103 owned frames released");
            var profile=Resources.Load<TextAsset>("HeroAnimation/SharedHero");Resources.Unregister("HeroAnimation/SharedHero");
            h=Hero();b=B(h);Bridge.Hit(h.heroRect);Near(Get<float>(b,"returnAt"),10.2f,"old fallback Hit window");
            Check(b.BodyGraphic.sprite.texture==Frame(Bridge.DefaultHeroTexturePath),"old bitmap");Resources.Register("HeroAnimation/SharedHero",profile);
        });
        Run("Monster retains its own pose assets, timing, registration and facing",()=>{
            foreach(var pose in new[]{"Normal","Attack","Hit"})Resources.Register("CultivationSprites/Enemy_Wolf_"+pose,new Texture2D(900,600,TextureFormat.RGBA32,false));
            var go=new GameObject("Owned Wolf",typeof(RectTransform),typeof(Image));var m=go.AddComponent<MonsterController>();m.Rect=(RectTransform)go.transform;m.SpriteIdentityKey="Wolf";
            Bridge.AttachMonster(m);var b=go.GetComponent<Bridge>();Bridge.Attack(m.Rect);
            Check(b.BodyGraphic.sprite.texture==Frame("CultivationSprites/Enemy_Wolf_Attack"),"monster Attack");
            Bridge.Hit(m.Rect);Check(b.CurrentPose==Bridge.Pose.Attack,"same precedence");At(b,Time.time+.17f);Bridge.Hit(m.Rect);
            Check(b.BodyGraphic.sprite.texture==Frame("CultivationSprites/Enemy_Wolf_Hit"),"monster Hit");Near(Get<float>(b,"returnAt")-Time.time,.2f,"old timing");Near(b.BodyReferenceFootUV.y,34f/600,"wolf feet");
        });
        Console.WriteLine(tests+" bridge boundary scenarios PASS (Mono/test doubles; NOT Unity/gameplay/Windows).");
    }
}
