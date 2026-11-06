using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore;

// Real bridge, controllers and CombatManager compiled against UnityStub.
// No native importer, rendering, Unity callback scheduling or scene/save I/O.
partial class MotionRegression
{
    sealed class SpriteResources : IDisposable
    {
        readonly Dictionary<string,Texture2D> textures = new Dictionary<string,Texture2D>();
        readonly float time=Time.time, delta=Time.deltaTime, timeScale=Time.timeScale;
        public SpriteResources()
        {
            foreach (string prefix in new[]{"", "Enemy_"})
                foreach (string type in new[]{"Swordsman", "Archer", "Mage"})
                    foreach (string pose in new[]{"Normal", "Attack", "Hit"})
                    {
                        string path="CultivationSprites/"+prefix+type+"_"+pose;
                        var texture=new Texture2D(900,600,TextureFormat.RGBA32,false);
                        textures.Add(path,texture);Resources.Register(path,texture);
                    }
            Time.timeScale=1f;
        }
        public Texture2D Texture(string path) { return textures[path]; }
        public void Dispose()
        {
            foreach (var pair in textures) Resources.Unregister(pair.Key);
            Time.time=time;Time.deltaTime=delta;Time.timeScale=timeScale;
        }
    }

    sealed class SpriteSubject
    {
        public readonly Field field;
        public readonly HeroController hero;
        public readonly MonsterController monster;
        public readonly RectTransform rect;
        public readonly CultivationSpriteBridge bridge;
        public readonly string key;
        public SpriteSubject(bool enemy, AttackMode mode)
        {
            Time.time=10f;
            field=new Field();
            key=(enemy?"Enemy_":"")+SpriteClass(mode);
            if (enemy)
            {
                monster=field.Monster(-30,mode);rect=monster.Rect;
                rect.gameObject.AddComponent<Image>();
                CultivationSpriteBridge.AttachMonster(monster);
            }
            else
            {
                hero=field.hero;rect=hero.heroRect;
                rect.gameObject.AddComponent<Image>();hero.ChangeAttackMode((int)mode);
            }
            bridge=rect.GetComponent<CultivationSpriteBridge>();
        }
        public void Attack() { if (hero!=null) hero.PlayAttackFeedback(); else monster.PlayAttackFeedback(); }
        public void Hit() { if (hero!=null) hero.ShowDamage(7); else monster.ShowDamage(7); }
        public void At(float time) { Time.time=time;Call(bridge,"Update"); }
    }

    static string SpriteClass(AttackMode mode)
    {
        return mode==AttackMode.Melee?"Swordsman":mode==AttackMode.RangedPhysical?"Archer":"Mage";
    }
    static Image SpriteBody(RectTransform rect)
    {
        return Get<Image>(rect.GetComponent<CultivationSpriteBridge>(),"image");
    }
    static void SpritePose(RectTransform rect,SpriteResources resources,string key,string pose)
    {
        var image=SpriteBody(rect);
        Check(image!=null&&image.sprite!=null,"sprite body exists");
        bool enemy=key.StartsWith("Enemy_",StringComparison.Ordinal);
        var bridge=rect.GetComponent<CultivationSpriteBridge>();
        Check(bridge.CurrentPose.ToString()==pose,"Action request/expiry remains "+pose);
        string path=enemy?"CultivationSprites/"+key+"_"+pose:CultivationSpriteBridge.DefaultHeroTexturePath;
        Check(image.sprite.texture==resources.Texture(path),enemy?key+" must show "+pose:"Hero action request retains its one default bitmap");
        Near(image.sprite.rect.width,900,"full canvas width, not automatic fragment");
        Near(image.sprite.rect.height,600,"full canvas height");
    }

    static void SpritePresentationTests()
    {
        int before=tests;
        using (var resources=new SpriteResources())
        {
            foreach (bool enemy in new[]{false,true})
                foreach (AttackMode mode in new[]{AttackMode.Melee,AttackMode.RangedPhysical,AttackMode.RangedMagic})
                {
                    Run("sprites "+(enemy?"Monster ":"Hero ")+mode+" protects Attack and drops overlapping Hit",()=>{
                        var s=new SpriteSubject(enemy,mode);
                        SpritePose(s.rect,resources,s.key,"Normal");
                        s.Attack();SpritePose(s.rect,resources,s.key,"Attack");
                        float end=Get<float>(s.bridge,"returnAt");Near(end,10.16f,"independent Attack display duration");
                        for (int i=1;i<=15;i++)
                        {
                            Time.time=10f+i*.01f;s.Hit();
                            SpritePose(s.rect,resources,s.key,"Attack");
                            Near(Get<float>(s.bridge,"returnAt"),end,"incoming damage never extends Attack");
                        }
                        s.At(10.17f);SpritePose(s.rect,resources,s.key,"Normal");
                        s.At(10.3f);SpritePose(s.rect,resources,s.key,"Normal"); // no deferred Hit
                        s.Hit();SpritePose(s.rect,resources,s.key,"Hit");
                        Near(Get<float>(s.bridge,"returnAt"),10.5f,"Hit has its own display duration");
                        s.Attack();SpritePose(s.rect,resources,s.key,"Attack"); // Attack beats existing Hit
                        s.At(10.47f);SpritePose(s.rect,resources,s.key,"Normal");
                    });
                }

            foreach (bool enemy in new[]{false,true})
            {
                Run("sprites "+(enemy?"Monster":"Hero")+" new high-speed strikes refresh Attack, hits do not",()=>{
                    var s=new SpriteSubject(enemy,AttackMode.Melee);s.Attack();
                    for (int i=1;i<=10;i++)
                    {
                        Time.time=10f+i*.05f;s.Hit();s.Attack();
                        Near(Get<float>(s.bridge,"returnAt"),Time.time+.16f,"new strike starts a new fixed display window");
                        SpritePose(s.rect,resources,s.key,"Attack");
                    }
                    s.At(10.67f);SpritePose(s.rect,resources,s.key,"Normal");
                    s.At(11f);SpritePose(s.rect,resources,s.key,"Normal");
                });
                Run("sprites "+(enemy?"Monster":"Hero")+" expired Attack permits Hit before next Update",()=>{
                    var s=new SpriteSubject(enemy,AttackMode.Melee);s.Attack();
                    Time.time=Get<float>(s.bridge,"returnAt");s.Hit();SpritePose(s.rect,resources,s.key,"Hit");
                    s.At(10.38f);SpritePose(s.rect,resources,s.key,"Normal");
                });
                Run("sprites "+(enemy?"Monster":"Hero")+" death immediately cancels presentation",()=>{
                    foreach (bool attack in new[]{false,true})
                    {
                        var s=new SpriteSubject(enemy,AttackMode.Melee);
                        if (attack) s.Attack(); else s.Hit();
                        if (enemy) s.monster.MarkDead(); else s.hero.Die();
                        SpritePose(s.rect,resources,s.key,"Normal");Near(Get<float>(s.bridge,"returnAt"),0,"deadline cleared on death");
                        s.Attack();s.Hit();SpritePose(s.rect,resources,s.key,"Normal");
                    }
                });
                Run("sprites "+(enemy?"Monster":"Hero")+" hide and reuse do not retain poses",()=>{
                    var s=new SpriteSubject(enemy,AttackMode.RangedMagic);s.Attack();
                    if (enemy) s.monster.gameObject.SetActive(false); else s.hero.HideHero();
                    SpritePose(s.rect,resources,s.key,"Normal");
                    CultivationSpriteBridge.Attack(s.rect);CultivationSpriteBridge.Hit(s.rect);
                    SpritePose(s.rect,resources,s.key,"Normal");
                    if (enemy)
                    {
                        s.monster.gameObject.SetActive(true);
                        SpritePose(s.rect,resources,"Enemy_"+SpriteClass(s.monster.attackMode),"Normal");
                    }
                    else
                    {
                        s.hero.ChangeAttackMode(1);s.hero.SpawnHero();
                        SpritePose(s.rect,resources,"Archer","Normal");
                    }
                    Near(Get<float>(s.bridge,"returnAt"),0,"reuse deadline cleared");
                });
                Run("sprites "+(enemy?"Monster":"Hero")+" bridge disable clears current Attack",()=>{
                    var s=new SpriteSubject(enemy,AttackMode.Melee);s.Attack();
                    s.bridge.enabled=false;Call(s.bridge,"OnDisable");
                    SpritePose(s.rect,resources,s.key,"Normal");s.Attack();s.Hit();
                    SpritePose(s.rect,resources,s.key,"Normal");
                    s.bridge.enabled=true;s.At(10.05f);SpritePose(s.rect,resources,s.key,"Normal");
                });
                Run("sprites "+(enemy?"Monster":"Hero")+" movement does not block Hit outside Attack",()=>{
                    var s=new SpriteSubject(enemy,AttackMode.Melee);
                    if (enemy) s.monster.currentState=MonsterState.Approaching;
                    else { s.hero.CurrentState=HeroState.Approaching;s.hero.HandleAnimation(true,false); }
                    s.Hit();SpritePose(s.rect,resources,s.key,"Hit");
                    s.At(10.21f);SpritePose(s.rect,resources,s.key,"Normal");
                });
            }

            Run("sprites Hero mode change resets action request while retaining default identity",()=>{
                var s=new SpriteSubject(false,AttackMode.Melee);
                var body=SpriteBody(s.rect);
                for (int i=0;i<3;i++)
                {
                    s.Attack();s.hero.ChangeAttackMode((i+1)%3);
                    string key=SpriteClass(s.hero.attackMode);SpritePose(s.rect,resources,key,"Normal");
                    s.Hit();s.hero.ChangeAttackMode(i);SpritePose(s.rect,resources,SpriteClass((AttackMode)i),"Normal");
                    Check(SpriteBody(s.rect)==body,"mode changes reuse this actor's Image");
                }
            });
            Run("sprites Monster stance reroll resets display without another random roll",()=>{
                var s=new SpriteSubject(true,AttackMode.Melee);var body=SpriteBody(s.rect);
                const int seed=9842;UnityEngine.Random.InitState(seed);
                int expected=UnityEngine.Random.Range(0,3),next=UnityEngine.Random.Range(0,3);
                UnityEngine.Random.InitState(seed);s.Attack();s.monster.InitRandomAttackMode();
                Check((int)s.monster.attackMode==expected,"original stance roll preserved");
                Check(UnityEngine.Random.Range(0,3)==next,"sprite binding must not roll random stance again");
                SpritePose(s.rect,resources,"Enemy_"+SpriteClass(s.monster.attackMode),"Normal");
                Check(SpriteBody(s.rect)==body,"reroll reuses body");
            });
            Run("sprites simultaneous melee keeps both Attack poses, damage and popups",()=>{
                var s=new SpriteSubject(false,AttackMode.Melee);var f=s.field;
                var m=f.Monster(-30);m.Rect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachMonster(m);
                f.hero.hpFillImage=new GameObject(true).AddComponent<Image>();
                m.hpFillImage=new GameObject(true).AddComponent<Image>();
                f.hero.dmgTextPrototype=new GameObject(true).AddComponent<TextMeshProUGUI>();
                m.dmgTextPrototype=new GameObject(true).AddComponent<TextMeshProUGUI>();
                f.Battle(m);f.hero.TickMovement(f.space,0);m.TickMovement(f.space,0);
                int heroHP=f.combat.CurrentHeroHP,monsterHP=f.Enemies[0].currentHP;
                CombatStep(f,.01f);
                Check(f.combat.CurrentHeroHP<heroHP && f.Enemies[0].currentHP<monsterHP,"protection is visual, no damage immunity");
                SpritePose(s.rect,resources,"Swordsman","Attack");
                SpritePose(m.Rect,resources,"Enemy_Swordsman","Attack");
                // StartCoroutine immediately executes the first fade step before
                // yielding (in Unity too), so alpha is already Alpha(deltaTime).
                float popupAlpha=DamagePopupMotion.Alpha(Time.deltaTime);
                Check(popupAlpha>0f,"first popup frame must be visible");
                Near(f.hero.dmgTextPrototype.alpha,popupAlpha,"Hero popup follows unchanged fade");
                Near(m.dmgTextPrototype.alpha,popupAlpha,"Monster popup follows unchanged fade");
                Check(f.hero.dmgTextPrototype.text.StartsWith("-") && m.dmgTextPrototype.text.StartsWith("-"),"damage amounts displayed");
                Near(f.hero.hpFillImage.fillAmount,(float)f.combat.CurrentHeroHP/f.combat.MaxHeroHP,"Hero bar follows damage");
                Near(m.hpFillImage.fillAmount,(float)f.Enemies[0].currentHP/f.Enemies[0].maxHP,"Monster bar follows damage");
            });
            foreach (AttackMode mode in new[]{AttackMode.RangedPhysical,AttackMode.RangedMagic})
            {
                foreach (bool enemy in new[]{false,true})
                Run("sprites "+(enemy?"Monster ":"Hero ")+mode+" actual projectile launch shows Attack",()=>{
                    var s=new SpriteSubject(false,enemy?AttackMode.Melee:mode);var f=s.field;
                    var m=f.Monster(-200,mode);m.Rect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachMonster(m);
                    f.space.SetPosition(m.Rect,new Vector2(-200,0));
                    f.Battle(m);f.hero.TickMovement(f.space,0);m.TickMovement(f.space,0);
                    int h=f.combat.CurrentHeroHP, hp=f.Enemies[0].currentHP;
                    CombatStep(f,mode==AttackMode.RangedMagic?2f:1f);
                    Check(f.combat.PendingProjectileCount>0,"attack starts at projectile launch");
                    RectTransform r=enemy?m.Rect:f.hero.heroRect;
                    SpritePose(r,resources,(enemy?"Enemy_":"")+SpriteClass(mode),"Attack");
                    Check(f.combat.CurrentHeroHP==h && f.Enemies[0].currentHP==hp,"projectiles do not damage early");
                    var state=f.combat.CurrentHeroCombatState;float timer=state.AttackTimer;
                    Time.time+=.17f;Call(r.GetComponent<CultivationSpriteBridge>(),"Update");
                    SpritePose(r,resources,(enemy?"Enemy_":"")+SpriteClass(mode),"Normal");
                    Near(state.AttackTimer,timer,"sprite clock cannot modify cooldown");
                });
            }
            Run("sprites B2 controllers own their Images, textures and pose deadlines",()=>{
                var s=new SpriteSubject(false,AttackMode.Melee);var f=s.field;
                var second=B2Hero(f,-190,AttackMode.RangedMagic);second.heroRect.gameObject.AddComponent<Image>();
                CultivationSpriteBridge.AttachHero(second);f.combat.RegisterAdditionalHero(second,B2Data("Sprite companion"));
                var firstBody=SpriteBody(s.rect);var secondBody=SpriteBody(second.heroRect);
                Check(firstBody!=secondBody && firstBody.transform.parent==s.rect && secondBody.transform.parent==second.heroRect,"Images belong to each actor");
                s.Attack();second.ShowDamage(7);
                SpritePose(s.rect,resources,"Swordsman","Attack");SpritePose(second.heroRect,resources,"Mage","Hit");
                second.PlayAttackFeedback();s.hero.ChangeAttackMode(1);
                SpritePose(s.rect,resources,"Archer","Normal");SpritePose(second.heroRect,resources,"Mage","Attack");
                second.Die();SpritePose(second.heroRect,resources,"Mage","Normal");SpritePose(s.rect,resources,"Archer","Normal");
                foreach (string name in new[]{"sourceImage","sourceWasEnabled","image"})
                    Check(typeof(CultivationSpriteBridge).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)
                        .IsDefined(typeof(SerializeField),false),"native Unity cloning must remap "+name);
            });
            Run("sprites body scaling and facing leave logical geometry and UI siblings unchanged",()=>{
                var s=new SpriteSubject(false,AttackMode.Melee);var r=s.rect;
                var hp=new GameObject(true).AddComponent<Image>();hp.transform.SetParent(r,false);
                var popup=new GameObject(true).AddComponent<TextMeshProUGUI>();popup.transform.SetParent(r,false);
                Vector2 size=r.sizeDelta,pos=r.anchoredPosition;Vector3 scale=r.localScale;
                s.bridge.heroScale=1.3f;s.bridge.monsterScale=2f;
                CultivationSpriteBridge.Attack(r);CultivationSpriteBridge.Hit(r);Call(s.bridge,"Update");
                Near(r.sizeDelta.x,size.x,"logic width");Near(r.sizeDelta.y,size.y,"logic height");
                Near(r.anchoredPosition.x,pos.x,"logic x");Near(r.anchoredPosition.y,pos.y,"logic y");
                Near(r.localScale.x,scale.x,"root facing");Near(hp.transform.localScale.x,1,"bar facing");Near(popup.transform.localScale.x,1,"popup facing");
                Check(hp.transform.parent==r && popup.transform.parent==r,"UI parents");
                var body=SpriteBody(r);Near(body.rectTransform.localScale.x,-1.3f,"Hero body faces left independently");
                Near(body.rectTransform.localScale.y,1.3f,"Hero scale independent of Monster");
                Near(body.rectTransform.sizeDelta.x,150,"full frame width");Near(body.rectTransform.sizeDelta.y,100,"full frame height");
                Check(!body.raycastTarget,"preview does not consume UI clicks");
            });
            Run("sprites camera pans actors and Ground equally while Attack stays protected",()=>{
                var s=new SpriteSubject(false,AttackMode.Melee);var f=s.field;
                var m=f.Monster(-30);m.Rect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachMonster(m);
                s.Attack();m.PlayAttackFeedback();
                float h=f.space.Position(s.rect).x,x=f.space.Position(m.Rect).x,g=f.space.Position(f.grass).x;
                Time.deltaTime=.1f;Call(f.space,"LateUpdate");float pan=f.space.Position(s.rect).x-h;
                Check(pan>0,"default pan continues");Near(f.space.Position(m.Rect).x-x,pan,"Monster follows camera");Near(f.space.Position(f.grass).x-g,pan,"Ground follows camera");
                s.Hit();m.ShowDamage(7);SpritePose(s.rect,resources,"Swordsman","Attack");SpritePose(m.Rect,resources,"Enemy_Swordsman","Attack");
            });
        }
        Console.WriteLine("Sprite presentation: "+(tests-before)+" PASS (Mono/UnityStub, not Unity Editor).");
    }
}
