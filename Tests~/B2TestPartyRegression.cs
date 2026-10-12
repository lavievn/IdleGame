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
        Run("B2 prototype: party limit is two additional Heroes",()=>{
            Check(GameManager.TestCompanionLimit==2,"only two extra actors may be summoned");
        });
    }
}
