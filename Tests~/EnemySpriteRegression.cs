using System;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

partial class MotionRegression
{
    static System.Reflection.FieldInfo EnemyAppearanceRandom => typeof(MonsterController).GetField("appearanceRandom",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
    static void ForceMeleeAppearanceFixture(MonsterController m,bool wolf)
    {
        // Field.Monster selects its requested mode AFTER simulated OnEnable.
        // Recreate that selected-mode boundary; no production override is added.
        int seed=0;while((new System.Random(seed).Next(2)==0)!=wolf)seed++;
        EnemyAppearanceRandom.SetValue(null,new System.Random(seed));
        m.attackMode=AttackMode.Melee;Set(m,"spriteIdentityChosen",false);
        CultivationSpriteBridge.AttachMonster(m);
    }
    static void EnemySpriteTests()
    {
        var oldRandom=EnemyAppearanceRandom.GetValue(null);
        using(var resources=new SpriteResources())
        {
            var wolf=new Texture2D[3];string[] poses={"Normal","Attack","Hit"};
            for(int i=0;i<3;i++)Resources.Register("CultivationSprites/Enemy_Wolf_"+poses[i],wolf[i]=new Texture2D(900,600,TextureFormat.RGBA32,false));
            try
            {
                foreach(bool useWolf in new[]{false,true})
                Run("melee Enemy keeps one random appearance across Normal/Attack/Hit without double tint / "+useWolf,()=>{
                    var f=new Field();var m=f.Monster(-20);var image=m.gameObject.AddComponent<Image>();ForceMeleeAppearanceFixture(m,useWolf);
                    var data=B2Data("Identity does not select presentation");data.race=RaceType.NhanToc;data.monsterAnimal="";
                    var p=f.space.Position(m.Rect);var size=m.Rect.sizeDelta;m.SetIdentityVisual(data);
                    var bridge=m.Rect.GetComponent<CultivationSpriteBridge>();
                    Check(!image.enabled && bridge.BodyGraphic.sprite.texture==(useWolf?wolf[0]:resources.Texture("CultivationSprites/Enemy_Swordsman_Normal")),"selected appearance, legacy source hidden");
                    Near(bridge.BodyGraphic.color.r,1,"no extra tint");Near(bridge.BodyGraphic.color.g,1,"no double tint");
                    Near(bridge.BodyReferenceFootUV.y,(useWolf?34f:10f)/600,"registered foot");
                    m.PlayAttackFeedback();Check(bridge.BodyGraphic.sprite.texture==(useWolf?wolf[1]:resources.Texture("CultivationSprites/Enemy_Swordsman_Attack")),"same family Attack");
                    Time.time+=.17f;Call(bridge,"Update");m.ShowDamage(5);
                    Check(bridge.BodyGraphic.sprite.texture==(useWolf?wolf[2]:resources.Texture("CultivationSprites/Enemy_Swordsman_Hit")),"same family Hit");
                    Near(f.space.Position(m.Rect).x,p.x,"logical X unchanged");Near(f.space.Position(m.Rect).y,p.y,"logical Y unchanged");Check(m.Rect.sizeDelta.Equals(size),"hitbox untouched");
                    var key=m.SpriteIdentityKey;
                    for(int i=0;i<10;i++) {m.SetIdentityVisual(data);CultivationSpriteBridge.AttachMonster(m);Check(m.SpriteIdentityKey==key,"no reroll within life/mode");}
                });
                foreach(var mode in new[]{AttackMode.RangedPhysical,AttackMode.RangedMagic})
                Run("ranged Enemy retains role artwork and never becomes Wolf / "+mode,()=>{
                    var f=new Field();var m=f.Monster(-20,mode);m.gameObject.AddComponent<Image>();
                    var data=B2Data("Lang");data.race=RaceType.YeuThu;data.monsterAnimal="Lang";m.SetIdentityVisual(data);
                    Check(m.SpriteIdentityKey==null,"only melee randomizes");
                    Check(m.Rect.GetComponent<CultivationSpriteBridge>().BodyGraphic.sprite.texture==resources.Texture("CultivationSprites/Enemy_"+SpriteClass(mode)+"_Normal"),"ranged role retained");
                });
                Run("pooled activation rerolls appearance with cosmetic RNG only",()=>{
                    var f=new Field();var m=f.Monster(-20);m.gameObject.AddComponent<Image>();
                    var expected=new System.Random(778);EnemyAppearanceRandom.SetValue(null,new System.Random(778));
                    bool seenWolf=false,seenHero=false;
                    for(int i=0;i<32;i++) {
                        Call(m,"OnDisable");Call(m,"OnEnable");
                        if(m.attackMode==AttackMode.Melee) {
                            bool roll=expected.Next(2)==0;Check((m.SpriteIdentityKey=="Wolf")==roll,"one cosmetic roll per melee spawn");seenWolf|=roll;seenHero|=!roll;
                        }
                    }
                    Check(seenWolf && seenHero,"seeded fixture exercises both variants");
                    UnityEngine.Random.InitState(542);int next=UnityEngine.Random.Range(0,10000);UnityEngine.Random.InitState(542);
                    m.attackMode=AttackMode.Melee;Set(m,"spriteIdentityChosen",false);CultivationSpriteBridge.AttachMonster(m);
                    Check(UnityEngine.Random.Range(0,10000)==next,"visual choice cannot alter combat RNG");
                });
                Run("Enemy missing dedicated pose prefers baked role then tinted Hero fallback",()=>{
                    Resources.Unregister("CultivationSprites/Enemy_Wolf_Attack");
                    var f=new Field();var m=f.Monster(-20);m.gameObject.AddComponent<Image>();ForceMeleeAppearanceFixture(m,true);
                    var bridge=m.Rect.GetComponent<CultivationSpriteBridge>();m.PlayAttackFeedback();
                    Check(bridge.BodyGraphic.sprite.texture==resources.Texture("CultivationSprites/Enemy_Swordsman_Attack"),"authored role fallback");Near(bridge.BodyGraphic.color.g,1,"baked fallback not tinted");
                    Resources.Unregister("CultivationSprites/Enemy_Swordsman_Attack");ForceMeleeAppearanceFixture(m,false);ForceMeleeAppearanceFixture(m,true);m.PlayAttackFeedback();
                    Check(bridge.BodyGraphic.sprite.texture==resources.Texture("CultivationSprites/Swordsman_Attack"),"Hero fallback");Near(bridge.BodyGraphic.color.g,.7f,"only Hero fallback tinted");Near(bridge.BodyReferenceFootUV.y,10f/600,"fallback Hero foot");
                    Resources.Register("CultivationSprites/Enemy_Swordsman_Attack",resources.Texture("CultivationSprites/Enemy_Swordsman_Attack"));Resources.Register("CultivationSprites/Enemy_Wolf_Attack",wolf[1]);
                });
            }
            finally {foreach(var pose in poses)Resources.Unregister("CultivationSprites/Enemy_Wolf_"+pose);EnemyAppearanceRandom.SetValue(null,oldRandom);}
        }
    }
}
