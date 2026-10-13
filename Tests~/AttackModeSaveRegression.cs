using System;
using System.IO;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void AttackModeSaveRegressionTests()
    {
        Run("0.0.51 switching stance marks primary Hero save as dirty",()=>{
            var f=new Field();
            SaveManager saves;string dir;
            var gm=MenuManager(f,out saves,out dir);
            try
            {
                var hero=HeroAt(3);
                Set(gm,"runtimeHeroData",hero);
                f.combat.SetupHeroInfo(hero);
                Set(gm,"hasDeployed",true);
                hero.isDirty=false;
                gm.SetHeroAttackMode(1);
                Check(f.hero.attackMode==AttackMode.RangedPhysical,
                    "three stance buttons still switch controller");
                Check(hero.savedAttackMode==AttackMode.RangedPhysical,
                    "selected bow written to primary Hero data");
                Check(hero.isDirty,"mode change triggers autosave");
                gm.SetHeroAttackMode(2);
                Check(hero.savedAttackMode==AttackMode.RangedMagic,
                    "subsequent magic switch replaces bow");
                gm.SetHeroAttackMode(42);
                Check(hero.savedAttackMode==AttackMode.RangedMagic,
                    "invalid stance cannot enter save");
            }
            finally { Directory.Delete(dir,true); }
        });

        Run("0.0.51 in-game manual slot restores bow before stance-based HP setup",()=>{
            var f=new Field();
            SaveManager saves;string dir;
            var gm=MenuManager(f,out saves,out dir);
            try
            {
                var hero=HeroAt(6);
                hero.AddCombatModifier(new CombatStatModifier {
                    stat=CombatStatId.MaxHealth,flat=100f,
                    requiredTags=new System.Collections.Generic.List<string>{"stance:bow"}
                });
                Set(gm,"runtimeHeroData",hero);
                f.combat.SetupHeroInfo(hero);
                Set(gm,"hasDeployed",true);
                gm.SetHeroAttackMode(1);
                Check(gm.TryManualSave(SaveSlot.ManualSave1),"manual bow save");
                gm.SetHeroAttackMode(2);
                Check(gm.LoadAndContinue(SaveSlot.ManualSave1),"in-game Load returns success");
                Check(f.hero.attackMode==AttackMode.RangedPhysical,"bow restored on load");
                Check(gm.HeroData.savedAttackMode==AttackMode.RangedPhysical,
                    "loaded data and controller agree");
                Check(f.combat.MaxHeroHP==gm.HeroData.GetCalculatedHealth(AttackMode.RangedPhysical),
                    "combat max HP initialized using restored stance");
            }
            finally { Directory.Delete(dir,true); }
        });

        Run("0.0.51 startup Continue also restores magic stance",()=>{
            var f=new Field();
            SaveManager saves;string dir;
            var gm=MenuManager(f,out saves,out dir);
            try
            {
                var hero=HeroAt(10);
                hero.savedAttackMode=AttackMode.RangedMagic;
                Check(saves.SaveGame(hero,SaveSlot.AutoSave),"seed autosave");
                f.hero.ChangeAttackMode(0);
                gm.OnContinueClicked();
                Check(gm.HasPendingConfirmation,"continue confirmation");
                gm.OnConfirmResetClicked();
                Check(f.hero.attackMode==AttackMode.RangedMagic,
                    "startup continue restores magic before deployment");
                Check(f.combat.MaxHeroHP==gm.HeroData.GetCalculatedHealth(AttackMode.RangedMagic),
                    "correct magic stance HP after Continue");
            }
            finally { Directory.Delete(dir,true); }
        });

        Run("0.0.51 legacy/corrupted saves default to melee regardless of template",()=>{
            var f=new Field();
            SaveManager saves;string dir;
            MenuManager(f,out saves,out dir);
            try
            {
                var template=HeroAt(1);
                template.savedAttackMode=AttackMode.RangedMagic;
                File.WriteAllText(Path.Combine(dir,"ManualSave1.json"),
                    "{\"currentLevel\":3,\"currentExp\":0}");
                Check(saves.LoadGame(template,SaveSlot.ManualSave1),"old save loaded");
                Check(template.savedAttackMode==AttackMode.Melee,
                    "missing mode defaults to melee, not template magic");
                File.WriteAllText(Path.Combine(dir,"ManualSave1.json"),
                    "{\"currentLevel\":3,\"savedAttackMode\":87}");
                Check(saves.LoadGame(template,SaveSlot.ManualSave1),"invalid enum save loaded");
                Check(template.savedAttackMode==AttackMode.Melee,
                    "invalid enum sanitized");
            }
            finally {Directory.Delete(dir,true);}
        });
    }
}
