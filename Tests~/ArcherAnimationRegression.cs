using System;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    sealed class ArcherFrames : IDisposable
    {
        public readonly Texture2D[,] Frames=new Texture2D[3,3];
        public ArcherFrames()
        {
            for(int p=0;p<3;p++)for(int i=0;i<3;i++) {
                var texture=Frames[p,i]=new Texture2D(900,600,TextureFormat.RGBA32,false);
                Resources.Register(Path(p,i),texture);
            }
        }
        static string Path(int p,int i) {return "CultivationSprites/Archer_"+new[]{"Normal","Attack","Hit"}[p]+"_"+(i+1);}
        public void Dispose() {for(int p=0;p<3;p++)for(int i=0;i<3;i++)Resources.Unregister(Path(p,i));}
    }
    // Historical CLI name retained for harness compatibility. V0.9 retires the weapon-linked Archer samples.
    static void ArcherAnimationTests() {UnifiedHeroVisualTests();}
    static void UnifiedHeroVisualTests()
    {
        using(var resources=new SpriteResources())using(var oldSamples=new ArcherFrames()) {
            Run("v09 one Hero bitmap and Image survive every attack-mode action request",()=>{
                var s=new SpriteSubject(false,AttackMode.Melee);var body=s.bridge.BodyGraphic;var sprite=body.sprite;var texture=resources.Texture(CultivationSpriteBridge.DefaultHeroTexturePath);
                Check(sprite.texture==texture,"Existing default Swordsman Normal selected, no new art");
                foreach(var mode in new[]{AttackMode.RangedPhysical,AttackMode.RangedMagic,AttackMode.Melee}) {
                    s.hero.ChangeAttackMode((int)mode);s.hero.SpawnHero();s.Attack();s.Hit();s.At(Time.time+.3f);
                    Check(s.bridge.BodyGraphic==body && body.sprite==sprite && body.sprite.texture==texture,"Same Image AND Sprite instance regardless of mode/action");
                    Check(Get<Sprite[]>(s.bridge,"normalFrames")==null && Get<Sprite[]>(s.bridge,"attackFrames")==null && Get<Sprite[]>(s.bridge,"hitFrames")==null,"No legacy mode sequence drives Hero");
                }
            });
            Run("v09 real equipment Sword Bow Staff Unarmed retains bitmap and distinct behavior",()=>{
                GameManager gm;var f=WeaponField(out gm);f.hero.heroRect.gameObject.AddComponent<Image>();CultivationSpriteBridge.AttachHero(f.hero);
                var body=f.hero.heroRect.GetComponent<CultivationSpriteBridge>().BodyGraphic;var sprite=body.sprite;var start=f.space.Position(f.hero.heroRect);
                foreach(var family in new[]{WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff}) {
                    WearWeapon(gm,WeaponInBag(gm,family));
                    Check(f.hero.attackMode==WeaponBasicAttack.Mode(family),"Weapon behavior remains "+family);
                    Near(f.hero.attackRange,f.space.AttackRange(f.hero.attackMode,true),"Same combat range authority");
                    Check(f.hero.heroRect.GetComponent<CultivationSpriteBridge>().BodyGraphic==body && body.sprite==sprite,"Equip does not select another character");
                    Check(f.space.Position(f.hero.heroRect).Equals(start),"No positioning workaround");
                }
                Check(gm.UnequipGear(EquipmentSlot.Weapon) && f.hero.attackMode==AttackMode.Melee && body.sprite==sprite,"Unarmed returns mechanics only");
                WearWeapon(gm,WeaponInBag(gm,WeaponFamily.Sword));Check(body.sprite==sprite,"Unarmed to Sword still same character");
            });
            Run("v09 Hero lifecycle retires action state without extra body or legacy texture load",()=>{
                var s=new SpriteSubject(false,AttackMode.RangedMagic);var image=s.bridge.BodyGraphic;var sprite=image.sprite;
                for(int i=0;i<20;i++){s.Attack();s.hero.Die();s.hero.SpawnHero();s.hero.ChangeAttackMode(i%3);}
                Check(s.bridge.BodyGraphic==image && image.sprite==sprite && s.bridge.CurrentPose==CultivationSpriteBridge.Pose.Normal,"No stale pose or visual after Retry/mode bind");
                int bodies=0;foreach(var child in s.rect.GetComponentsInChildren<Image>(true))if(child.gameObject.name=="Cultivation Sprite Body")bodies++;
                Check(bodies==1,"Exactly one owned Hero body Image");
            });
            Run("v09 Monster role pose assets remain independent",()=>{
                foreach(var mode in new[]{AttackMode.Melee,AttackMode.RangedPhysical,AttackMode.RangedMagic}) {
                    var enemy=new SpriteSubject(true,mode);enemy.Attack();SpritePose(enemy.rect,resources,"Enemy_"+SpriteClass(mode),"Attack");
                    enemy.At(10.17f);enemy.Hit();SpritePose(enemy.rect,resources,"Enemy_"+SpriteClass(mode),"Hit");
                }
            });
            Run("v09 missing legacy Archer samples cannot hide or replace the default Hero",()=>{
                Resources.Unregister("CultivationSprites/Archer_Attack_2");Resources.Unregister("CultivationSprites/Mage_Normal");
                var s=new SpriteSubject(false,AttackMode.RangedPhysical);s.Attack();s.hero.ChangeAttackMode(2);
                Check(s.bridge.BodyGraphic.sprite.texture==resources.Texture(CultivationSpriteBridge.DefaultHeroTexturePath),"Hero no longer depends on class sample presence");
            });
        }
    }
}
