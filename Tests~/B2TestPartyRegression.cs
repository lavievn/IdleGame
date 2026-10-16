using System;
using System.IO;
using System.Collections.Generic;
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

        Run("0.0.54 B2 roster serializes in every slot and reads old saves as empty",()=>{
            var f=new Field();SaveManager saves;string dir;
            MenuManager(f,out saves,out dir);
            try
            {
                var data=HeroAt(7);
                data.b2PartyVersion=1;
                data.b2CompanionModes=new List<int>{
                    (int)AttackMode.RangedPhysical,(int)AttackMode.RangedMagic
                };
                foreach(var slot in new[]{SaveSlot.AutoSave,SaveSlot.ManualSave1,SaveSlot.ManualSave2})
                {
                    Check(saves.SaveGame(data,slot),"slot writes companion roster: "+slot);
                    string json=File.ReadAllText(Path.Combine(dir,slot+".json"));
                    Check(json.Contains("\"b2CompanionModes\""),
                        "JSON includes B2 roster for "+slot);
                    var loaded=HeroAt(1);
                    loaded.b2CompanionModes=new List<int>{(int)AttackMode.Melee};
                    Check(saves.LoadGame(loaded,slot),"slot reads roster: "+slot);
                    Check(loaded.b2PartyVersion==1 &&
                        loaded.b2CompanionModes.Count==2 &&
                        loaded.b2CompanionModes[0]==(int)AttackMode.RangedPhysical &&
                        loaded.b2CompanionModes[1]==(int)AttackMode.RangedMagic,
                        "both companion roles restored from "+slot);
                }
                File.WriteAllText(Path.Combine(dir,"AutoSave.json"),
                    "{\"currentLevel\":2,\"currentExp\":0}");
                var legacy=HeroAt(1);
                legacy.b2PartyVersion=1;
                legacy.b2CompanionModes=new List<int>{(int)AttackMode.RangedMagic};
                Check(saves.LoadGame(legacy,SaveSlot.AutoSave),
                    "backward-compatible legacy save");
                Check(legacy.b2PartyVersion==0 && legacy.b2CompanionModes.Count==0,
                    "legacy save cannot invent B2 companions from template");
                legacy.b2PartyVersion=1;
                legacy.b2CompanionModes=new List<int>{0,99,2,1};
                legacy.NormalizeB2Party();
                Check(legacy.b2CompanionModes.Count==2 &&
                    legacy.b2CompanionModes[0]==0 &&
                    legacy.b2CompanionModes[1]==2,
                    "roster rejects invalid modes and caps additional heroes at two");
            }
            finally {Directory.Delete(dir,true);}
        });

        Run("0.0.54 manual LoadAndContinue rebuilds test-party combat registration",()=>{
            var f=new Field();SaveManager saves;string dir;
            var gm=MenuManager(f,out saves,out dir);
            try
            {
                Set(gm,"environmentManager",f.space);
                var original=HeroAt(5);
                Set(gm,"runtimeHeroData",original);
                Set(gm,"hasDeployed",true);
                gm.preGameUI.SetActive(false);
                var actor=new GameObject().AddComponent<HeroController>();
                actor.heroRect=(RectTransform)new GameObject(true).transform;
                actor.heroRect.parent=f.ground;
                Call(actor,"Awake");
                actor.ChangeAttackMode((int)AttackMode.RangedMagic);
                actor.SpawnHero(false);
                var memberData=HeroAt(5);
                Check(f.combat.RegisterAdditionalHero(actor,memberData)!=null,
                    "B2 test member registered before save");
                var memberType=typeof(GameManager).GetNestedType("TestCompanion",
                    BindingFlags.NonPublic);
                var member=Activator.CreateInstance(memberType,true);
                memberType.GetField("Controller").SetValue(member,actor);
                memberType.GetField("Data").SetValue(member,memberData);
                ((System.Collections.IList)Get<object>(gm,"testCompanions")).Add(member);
                Check(gm.TryManualSave(SaveSlot.ManualSave1),
                    "manual save captures live B2 roster");
                var json=File.ReadAllText(Path.Combine(dir,"ManualSave1.json"));
                Check(json.Contains("\"b2CompanionModes\""),
                    "B2 roles are stored in the SAME character snapshot");
                Check(gm.LoadAndContinue(SaveSlot.ManualSave1),
                    "manual load restores active session");
                Check(gm.TestCompanionCount==1 &&
                    f.combat.RegisteredHeroCount==2,
                    "one companion respawned and registered beside primary");
                var current=(System.Collections.IList)Get<object>(gm,"testCompanions");
                var restoredController=(HeroController)memberType.GetField("Controller")
                    .GetValue(current[0]);
                Check(restoredController.IsDeployed &&
                    restoredController.attackMode==AttackMode.RangedMagic &&
                    f.combat.StateFor(restoredController)!=null,
                    "saved companion mode and combat registration survived load");
                Check(Get<HeroController>(f.space,"cameraTarget")==f.hero,
                    "loaded companion does not steal camera follow");
            }
            finally {Directory.Delete(dir,true);}
        });

        Run("0.0.54 title Continue recreates both saved B2 companions",()=>{
            var f=new Field();SaveManager saves;string dir;
            var gm=MenuManager(f,out saves,out dir);
            try
            {
                Set(gm,"environmentManager",f.space);
                var snapshot=HeroAt(9);
                snapshot.b2PartyVersion=1;
                snapshot.b2CompanionModes=new List<int>{
                    (int)AttackMode.RangedPhysical,
                    (int)AttackMode.RangedMagic
                };
                Check(saves.SaveGame(snapshot,SaveSlot.AutoSave),
                    "autosave contains 2 companions");
                gm.OnContinueClicked();
                Check(gm.HasPendingConfirmation,"title Continue asks confirmation");
                gm.OnConfirmResetClicked();
                Check(gm.TestCompanionCount==2 &&
                    f.combat.RegisteredHeroCount==3 &&
                    gm.HeroData.b2CompanionModes.Count==2,
                    "title Continue restores 3 registered heroes from autosave");
                Check(Get<HeroController>(f.space,"cameraTarget")==f.hero,
                    "title Continue keeps the main camera target");
            }
            finally {Directory.Delete(dir,true);}
        });

        Run("B2 prototype: party limit is two additional Heroes",()=>{
            Check(GameManager.TestCompanionLimit==2,"only two extra actors may be summoned");
        });
    }
}
