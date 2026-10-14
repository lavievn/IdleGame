using System;
using UnityEngine;

// Gameplay logic verification with Unity test doubles; UI still needs Play Mode.
partial class MotionRegression
{
    private static void ForgeTimes(EntityDataSO hero,string id,
        EquipmentRefinement.Operation op,int times)
    {
        string message;
        for(int i=0;i<times;i++)
            Check(EquipmentRefinement.TryApply(hero,id,op,out message),
                "forge action #"+i+" "+op+": "+message);
    }

    static void ForgeRegressionTests()
    {
        Run("0.0.52 forge: 3-6-12 rarity climb, only transitions reveal",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var seed=EquipmentRefinement.CreateForgeDemoSword();
            Check(EquipmentFoundation.IsValid(seed),"generated sword valid");
            Check(seed.affixSlotCount==4 && seed.extraStats.Count==4 &&
                seed.rarity==EquipmentRarity.Common &&
                seed.forgeStageTargetRarity==EquipmentRarity.Dia &&
                seed.naturalRefineLimit==21,
                "four fixed lines; Common initial, Dia cap at +21");
            Check(EquipmentRuntime.AddInstance(hero,seed),"insert sword");
            var item=EquipmentRuntime.Find(hero,seed.instanceId);
            Check(item!=seed && item.extraStats[0]!=seed.extraStats[0],
                "deep copy on acquire");
            Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,100),
                "grant 100 mats");
            Check(EquipmentRuntime.Equip(hero,item.instanceId,EquipmentSlot.Weapon),
                "equip sample");
            float[] value=new float[item.extraStats.Count];
            CombatStatId[] stat=new CombatStatId[item.extraStats.Count];
            for(int i=0;i<item.extraStats.Count;i++)
            {
                value[i]=item.extraStats[i].flat;
                stat[i]=item.extraStats[i].stat;
                Check(!item.extraStats[i].revealed,"initial affix sealed");
            }
            Check(EquipmentRefinement.ProgressPercent(item)==25,
                "Common / Dia bar is grade-only fraction");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.ItemLevelUp,1);
            Check(item.level==2 && !item.extraStats[0].revealed,
                "item level increase does not reveal");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.ItemLevelDown,1);
            Check(item.level==1 && EquipmentInventory.Count(hero,
                EquipmentRefinement.MaterialId)==99,"level down costs no extra material");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalUp,2);
            Check(item.rarity==EquipmentRarity.Common &&
                !item.extraStats[0].revealed &&
                EquipmentRefinement.ProgressPercent(item)==25 &&
                item.forgeStageProgress==2,
                "progress toward Hoang does not reveal or move grade bar");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalUp,1);
            Check(item.rarity==EquipmentRarity.Hoang &&
                item.forgeStageProgress==0 &&
                item.extraStats[0].revealed &&
                EquipmentFoundation.AffixState(item,item.extraStats[0])==
                    EquipmentAffixState.Active &&
                !item.extraStats[1].revealed,
                "third attempt advances one tier and reveals exactly one");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalUp,6);
            Check(item.rarity==EquipmentRarity.Huyen &&
                item.refineLevel==9 && item.extraStats[1].revealed &&
                !item.extraStats[2].revealed,"six more unlock Huyen only");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalUp,12);
            Check(item.rarity==EquipmentRarity.Dia &&
                item.refineLevel==21 && item.extraStats[2].revealed &&
                !item.extraStats[3].revealed &&
                EquipmentRefinement.ProgressPercent(item)==100,
                "twelve more reach Dia natural ceiling only");
            string message;
            Check(!EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.NormalUp,out message),
                "ordinary forge cannot bypass Dia");
            Check(EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==78,
                "twenty one refines + one level consume 22 materials");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialUp,out message),
                "special crosses cap");
            Check(item.rarity==EquipmentRarity.Thien &&
                item.forgeStageStartRarity==EquipmentRarity.Thien &&
                item.forgeStageTargetRarity==EquipmentRarity.Tien &&
                item.extraStats[3].revealed &&
                EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==68,
                "special +10 grants exactly one grade and new Tiên cap");
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialDown,out message),
                "special minus restores prior stage");
            Check(item.rarity==EquipmentRarity.Dia &&
                item.forgeStageTargetRarity==EquipmentRarity.Dia &&
                item.refineLevel==21 &&
                EquipmentFoundation.AffixState(item,item.extraStats[3])==
                    EquipmentAffixState.KnownLocked &&
                EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==68,
                "return from Thien relocks learned content without refund");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalDown,1);
            Check(item.rarity==EquipmentRarity.Huyen &&
                item.forgeStageProgress==11 && item.refineLevel==20 &&
                item.extraStats[2].revealed &&
                EquipmentFoundation.AffixState(item,item.extraStats[2])==
                    EquipmentAffixState.KnownLocked,
                "normal downgrade reverses single grade, doesn't erase discovery");
            for(int i=0;i<item.extraStats.Count;i++)
                Check(item.extraStats[i].flat==value[i] &&
                    item.extraStats[i].stat==stat[i],
                    "every rolled stat stays unchanged after all actions");
        });

        Run("0.0.52 forge: early special wastes ten but never jumps to cap",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var seed=EquipmentRefinement.CreateForgeDemoSword();
            Check(EquipmentRuntime.AddInstance(hero,seed),"add early special test");
            Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,100),
                "grant mats");
            var item=EquipmentRuntime.Find(hero,seed.instanceId);
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalUp,1);
            string message;
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialUp,out message),
                "early special accepted");
            Check(item.rarity==EquipmentRarity.Hoang &&
                item.forgeStageTargetRarity==EquipmentRarity.Tien &&
                item.forgeStageProgress==0 &&
                !item.extraStats[1].revealed &&
                EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==89,
                "only Common->Hoang, new cap Tiên, cost 1+10");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalUp,1);
            Check(!EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialDown,out message),
                "cannot rewind special with un-rewound regular steps");
            ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalDown,1);
            Check(EquipmentRefinement.TryApply(hero,item.instanceId,
                EquipmentRefinement.Operation.SpecialDown,out message),
                "undo early special");
            Check(item.rarity==EquipmentRarity.Common &&
                item.forgeStageProgress==1 &&
                item.specialForgeCount==0 &&
                EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==88,
                "reversible stage, non-refundable resources");
        });

        Run("0.0.52 forge: old equipment preserves the old rolls",()=>{
            var hero=HeroAt(1);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            var old=EquipmentFoundation.CreateDemoSword();
            old.refineLevel=3;
            old.extraStats.Add(new ItemStatLine {
                stat=CombatStatId.Attack,flat=7,revealed=true
            });
            Check(EquipmentRuntime.AddInstance(hero,old),"add legacy item");
            EquipmentRuntime.Normalize(hero);
            var item=EquipmentRuntime.Find(hero,old.instanceId);
            Check(item.forgeSchemaVersion==EquipmentRefinement.SchemaVersion &&
                item.extraStats.Count==1 && item.extraStats[0].flat==7,
                "old item unchanged, no new random lines");
            Check(item.powerBaseRefine==3 &&
                EquipmentRefinement.PowerMultiplier(item)==1f,
                "migration does not change current combat stats");
        });

        Run("0.0.52 forge: no materials means no change; Save roundtrip",()=>{
            var field=new Field();SaveManager save;string dir;
            MenuManager(field,out save,out dir);
            try
            {
                var hero=HeroAt(7);
                EquipmentInventory.InitializeNewRun(hero);
                EquipmentRuntime.InitializeNewRun(hero);
                var seed=EquipmentRefinement.CreateForgeDemoSword();
                Check(EquipmentRuntime.AddInstance(hero,seed),"add item");
                Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,9),
                    "nine mats available");
                var item=EquipmentRuntime.Find(hero,seed.instanceId);
                string message;
                Check(!EquipmentRefinement.TryApply(hero,item.instanceId,
                    EquipmentRefinement.Operation.SpecialUp,out message),
                    "special rejects nine mats");
                Check(item.rarity==EquipmentRarity.Common &&
                    EquipmentInventory.Count(hero,EquipmentRefinement.MaterialId)==9,
                    "failure is atomic");
                Check(EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId,91),
                    "top up to 100");
                ForgeTimes(hero,item.instanceId,EquipmentRefinement.Operation.NormalUp,3);
                Check(save.SaveGame(hero,SaveSlot.ManualSave1),"save new progress");
                var loaded=HeroAt(1);
                Check(save.LoadGame(loaded,SaveSlot.ManualSave1),"load new progress");
                var after=EquipmentRuntime.Find(loaded,seed.instanceId);
                Check(after!=null && after.rarity==EquipmentRarity.Hoang &&
                    after.forgeStageTargetRarity==EquipmentRarity.Dia &&
                    after.forgeStageProgress==0 && after.refineLevel==3 &&
                    after.extraStats[0].revealed && !after.extraStats[1].revealed &&
                    EquipmentInventory.Count(loaded,EquipmentRefinement.MaterialId)==97,
                    "grade, resource count, progression and discovery persist");
                for(int i=0;i<item.extraStats.Count;i++)
                    Check(after.extraStats[i].flat==item.extraStats[i].flat &&
                        after.extraStats[i].stat==item.extraStats[i].stat,
                        "no stat rerolls after save load");
            }
            finally {System.IO.Directory.Delete(dir,true);}
        });
    }
}
