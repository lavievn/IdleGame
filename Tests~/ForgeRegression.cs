using System;
using UnityEngine;

// Test-only: exercises state, consumption, undo, fixed affixes and save.
partial class MotionRegression
{
    static void ForgeRegressionTests()
    {
        Run("0.0.52 forge: fixed once-rolled sword and resource accounting",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var sword=EquipmentRefinement.CreateForgeDemoSword();
            Check(EquipmentFoundation.IsValid(sword),"new forge sword valid");
            Check(sword.affixSlotCount==4 && sword.extraStats.Count==4,
                "all hidden affix data generated once");
            foreach(var line in sword.extraStats)
                Check(!line.revealed,"affixes start sealed");
            Check(EquipmentRuntime.AddInstance(hero,sword),"sample item admitted");
            var item=EquipmentRuntime.Find(hero,sword.instanceId);
            Check(item!=sword && item.extraStats[0]!=sword.extraStats[0],
                "owned instance is a deep clone");
            Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,100),
                "add 100 test materials");
            Check(EquipmentRuntime.Equip(hero,item.instanceId,EquipmentSlot.Weapon),
                "equip forge sword");
            var initialValues=new float[item.extraStats.Count];
            var stats=new CombatStatId[item.extraStats.Count];
            for(int i=0;i<item.extraStats.Count;i++)
            {initialValues[i]=item.extraStats[i].flat;stats[i]=item.extraStats[i].stat;}
            string msg;
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.ItemLevelUp,out msg),"item level +1");
            Check(item.level==2 && EquipmentInventory.Count(hero,
                EquipmentRefinement.MaterialId)==99,"level upgrade consumes exactly one");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.ItemLevelDown,out msg),"item level -1");
            Check(item.level==1 && EquipmentInventory.Count(hero,
                EquipmentRefinement.MaterialId)==99,"minus never refunds");
            for(int i=0;i<6;i++)
                Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                    EquipmentRefinement.Operation.NormalUp,out msg),"normal refine +");
            Check(item.refineLevel==6 && EquipmentRefinement.ProgressPercent(item)==60,
                "regular stage progresses 60 percent");
            Check(EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==93,
                "six normal actions use six materials");
            Check(item.rarity==EquipmentRarity.Common,"rarity not rerolled midway");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialUp,out msg),"10 material breakthrough");
            Check(item.rarity==EquipmentRarity.Dia &&
                item.forgeStageStartRarity==EquipmentRarity.Dia &&
                item.forgeStageTargetRarity>EquipmentRarity.Dia,
                "special immediately reaches old ceiling and opens a higher stage");
            Check(item.specialForgeCount==1 && item.forgeHistory.Count==1 &&
                EquipmentRefinement.ProgressPercent(item)>=20 &&
                EquipmentRefinement.ProgressPercent(item)<=40,
                "new stage starts at a fresh estimated percentage");
            Check(EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==83,
                "special costs exactly ten");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.NormalUp,out msg),"normal refine next stage");
            Check(!EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialDown,out msg),
                "cannot undo special while current stage has regular upgrades");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.NormalDown,out msg),"undo normal first");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialDown,out msg),"undo special stage");
            Check(item.rarity==EquipmentRarity.Common &&
                item.refineLevel==6 && item.specialForgeCount==0 &&
                EquipmentRefinement.ProgressPercent(item)==60,
                "special rollback restores exact previous stage");
            Check(EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==82,
                "undo operations refund nothing");
            for(int i=0;i<item.extraStats.Count;i++)
                Check(item.extraStats[i].flat==initialValues[i] &&
                    item.extraStats[i].stat==stats[i],
                    "refinement never rerolls existing affix values or types");
        });

        Run("0.0.52 forge: rarity downgrade relocks a known affix",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var sword=EquipmentRefinement.CreateForgeDemoSword();
            sword.extraStats[0].requiredRarity=EquipmentRarity.Dia;
            Check(EquipmentRuntime.AddInstance(hero,sword),"create rarity-gated sword");
            Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,100),
                "add materials");
            var item=EquipmentRuntime.Find(hero,sword.instanceId);
            var gated=item.extraStats[0];
            Check(EquipmentFoundation.AffixState(item,gated)==EquipmentAffixState.Hidden,
                "gated affix starts hidden");
            string msg;
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.NormalUp,out msg),"normal +1");
            Check(EquipmentFoundation.AffixState(item,gated)==EquipmentAffixState.Hidden,
                "normal improvement alone cannot reveal rarity gated item");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialUp,out msg),"special leap to Dia");
            Check(EquipmentFoundation.AffixState(item,gated)==EquipmentAffixState.Active,
                "special success reveals stat when all prerequisites are met");
            float rolled=gated.flat;
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialDown,out msg),"special minus");
            Check(gated.revealed && gated.flat==rolled &&
                EquipmentFoundation.AffixState(item,gated)==EquipmentAffixState.KnownLocked,
                "rarity downgrade dims stat without reroll or hiding it");
        });

        Run("0.0.52 forge: failed purchase does not consume or upgrade",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var sword=EquipmentRefinement.CreateForgeDemoSword();
            Check(EquipmentRuntime.AddInstance(hero,sword),"item admitted");
            Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,9),
                "nine units available");
            var item=EquipmentRuntime.Find(hero,sword.instanceId);
            string msg;
            Check(!EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialUp,out msg),"missing special resources");
            Check(EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==9 &&
                item.specialForgeCount==0 && item.forgeHistory.Count==0 &&
                item.rarity==EquipmentRarity.Common,
                "denied action leaves inventory and item unchanged");
        });

        Run("0.0.52 forge: old saves retain their prior affix rolls",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var legacy=EquipmentFoundation.CreateDemoSword();
            legacy.refineLevel=3;
            legacy.extraStats.Add(new ItemStatLine {
                stat=CombatStatId.Attack,flat=7,revealed=true
            });
            Check(EquipmentRuntime.AddInstance(hero,legacy),"add old version");
            EquipmentRuntime.Normalize(hero);
            var item=EquipmentRuntime.Find(hero,legacy.instanceId);
            Check(item.forgeSchemaVersion==EquipmentRefinement.SchemaVersion &&
                item.extraStats.Count==1 && item.extraStats[0].flat==7,
                "legacy normalization keeps original single affix and roll");
            Check(item.powerBaseRefine==3 && EquipmentRefinement.PowerMultiplier(item)==1f,
                "old weapon keeps exact current power after migration");
        });

        Run("0.0.52 forge: save/load keeps materials, ceilings and discovery",()=>{
            var f=new Field();SaveManager save;string dir;
            MenuManager(f,out save,out dir);
            try
            {
                var hero=HeroAt(7);
                EquipmentInventory.InitializeNewRun(hero);
                EquipmentRuntime.InitializeNewRun(hero);
                var sword=EquipmentRefinement.CreateForgeDemoSword();
                Check(EquipmentRuntime.AddInstance(hero,sword),"forge item created");
                Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,100),
                    "grant consumables");
                string msg;
                for(int i=0;i<5;i++)
                    Check(EquipmentRefinement.TryApply(hero,sword.instanceId,
                        EquipmentRefinement.Operation.NormalUp,out msg),"forge step");
                Check(EquipmentRefinement.TryApply(hero,sword.instanceId,
                    EquipmentRefinement.Operation.SpecialUp,out msg),"new stage");
                var before=EquipmentRuntime.Find(hero,sword.instanceId);
                Check(save.SaveGame(hero,SaveSlot.ManualSave1),"save forge");
                var loaded=HeroAt(1);
                Check(save.LoadGame(loaded,SaveSlot.ManualSave1),"load forge");
                var after=EquipmentRuntime.Find(loaded,sword.instanceId);
                Check(after!=null && after.rarity==before.rarity &&
                    after.refineLevel==before.refineLevel &&
                    after.forgeStageProgress==before.forgeStageProgress &&
                    after.forgeStageBasePercent==before.forgeStageBasePercent &&
                    after.forgeStageTargetRarity==before.forgeStageTargetRarity &&
                    after.forgeHistory.Count==1 &&
                    after.specialForgeCount==before.specialForgeCount,
                    "item forge history survives JSON roundtrip");
                Check(EquipmentInventory.Count(loaded,EquipmentRefinement.MaterialId)==85,
                    "spent materials persist through save");
                for(int i=0;i<after.extraStats.Count;i++)
                    Check(after.extraStats[i].flat==before.extraStats[i].flat &&
                        after.extraStats[i].revealed==before.extraStats[i].revealed,
                        "saved latent affixes stay identical");
            }
            finally {System.IO.Directory.Delete(dir,true);}
        });
    }
}
