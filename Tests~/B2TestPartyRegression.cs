using System;
using System.Reflection;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void B2TestPartyTests()
    {
        Run("B2 prototype: summoning a second Hero does not steal camera focus",()=>{
            var f = new Field();
            var second = new GameObject().AddComponent<HeroController>();
            second.heroRect = (RectTransform)new GameObject(true).transform;
            second.heroRect.parent = f.ground;
            Call(second,"Awake");
            second.SpawnHero(false);
            Check(second.IsDeployed, "companion deployed");
            Check(Get<HeroController>(f.space,"cameraTarget") == f.hero,
                "companion summon must not reset camera subject");
            Check(f.hero.IsDeployed,"primary Hero remains deployed");
        });
        Run("B2 prototype: temporary companion mode excludes the primary mode",()=>{
            var f = new Field();
            var gm = new GameObject().AddComponent<GameManager>();
            Set(gm,"heroController",f.hero);
            var method = typeof(GameManager).GetMethod("RandomUnusedTestMode",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Check(method != null,"test-only random mode picker exists");
            foreach (AttackMode primary in new[] {AttackMode.Melee,
                     AttackMode.RangedPhysical,AttackMode.RangedMagic})
            {
                f.hero.attackMode = primary;
                for (int i=0;i<30;i++)
                {
                    var chosen = (AttackMode)method.Invoke(gm,null);
                    Check(chosen != primary && (int)chosen >= 0 && (int)chosen < 3,
                        "summoned mode must differ from primary");
                }
            }
        });
        Run("0.0.54 map travel re-registers living companions without stealing camera",()=>{
            var f=new Field();
            var gm=new GameObject().AddComponent<GameManager>();
            Set(gm,"heroController",f.hero);
            Set(gm,"combatManager",f.combat);
            Set(gm,"environmentManager",f.space);

            var companion=new GameObject().AddComponent<HeroController>();
            companion.heroRect=(RectTransform)new GameObject(true).transform;
            companion.heroRect.parent=f.ground;
            Call(companion,"Awake");
            companion.ChangeAttackMode((int)AttackMode.RangedMagic);
            companion.SpawnHero(false);
            var extraData=new EntityDataSO {
                baseHealth=900,baseDamage=35,expToNextLevel=1000000
            };
            Check(f.combat.RegisterAdditionalHero(companion,extraData)!=null,
                "companion registered before travel");
            var memberType=typeof(GameManager).GetNestedType("TestCompanion",
                BindingFlags.NonPublic);
            Check(memberType!=null,"runtime party data available");
            var member=Activator.CreateInstance(memberType,true);
            memberType.GetField("Controller").SetValue(member,companion);
            memberType.GetField("Data").SetValue(member,extraData);
            var companions=(System.Collections.IList)Get<object>(gm,"testCompanions");
            companions.Add(member);
            var capture=typeof(GameManager).GetMethod("TravellingTestCompanions",
                BindingFlags.NonPublic|BindingFlags.Instance);
            var restore=typeof(GameManager).GetMethod("RestoreTestCompanionsAfterTravel",
                BindingFlags.NonPublic|BindingFlags.Instance);
            var travelling=capture.Invoke(gm,null);
            Check(((System.Collections.IList)travelling).Count==1,
                "only deployed heroes travel to new map");

            var nextHero=new EntityDataSO {
                baseHealth=1000,baseDamage=25,expToNextLevel=1000000
            };
            f.combat.SetupHeroInfo(nextHero); // Clears old companion registrations.
            Check(!companion.IsDeployed && f.combat.StateFor(companion)==null,
                "map reset removes combat bindings temporarily");
            f.hero.SpawnHero();
            restore.Invoke(gm,new[]{travelling});
            Check(companion.IsDeployed &&
                f.combat.StateFor(companion)!=null &&
                f.combat.RegisteredHeroCount==2,
                "companion active and bound to new battle");
            Check(companion.attackMode==AttackMode.RangedMagic,
                "companion attack role survives travel");
            Check(Get<HeroController>(f.space,"cameraTarget")==f.hero,
                "companion does not displace primary camera subject");
            Check(gm.TestCompanionCount==1,
                "travelling does not destroy temporary party");
        });

        Run("B2 prototype: party limit is two additional Heroes",()=>{
            Check(GameManager.TestCompanionLimit==2,"only two extra actors may be summoned");
        });
    }
}
