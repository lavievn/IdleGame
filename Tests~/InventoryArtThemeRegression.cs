using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void InventoryArtThemeRegressionTests()
    {
        Run("0.0.52 inspector art profile finds only the matching map and keeps fallback safe",()=>{
            var theme=new InventoryArtTheme();
            theme.defaultMapSplash=new Sprite();
            theme.mapArts=new List<InventoryMapArt> {
                new InventoryMapArt {mapNumber=1,displayName="Map 1",mapSplash=new Sprite()},
                new InventoryMapArt {mapNumber=3,displayName="Map 3",heroSplash=new Sprite()}
            };
            Check(theme.FindMap(1)!=null && theme.FindMap(1).mapNumber==1,
                "map 1 gets its own splash");
            Check(theme.FindMap(2)==null,
                "map 2 has no explicit entry; fallback remains available");
            Check(theme.FindMap(3).heroSplash!=null && theme.defaultHeroSplash==null,
                "per-map hero sprite can be assigned independently");
            theme.mapArts=null;
            Check(theme.FindMap(1)==null && theme.defaultMapSplash!=null,
                "empty map collection does not cause an exception or remove fallback");
        });

        Run("0.0.52 art assignments do not modify Hero progression or inventory",()=>{
            var hero=HeroAt(3);
            EquipmentInventory.InitializeNewRun(hero);
            EquipmentRuntime.InitializeNewRun(hero);
            int level=hero.currentLevel;
            int attack=hero.GetCalculatedDamage(AttackMode.Melee);
            int stacks=hero.inventory.Count;
            var art=new InventoryArtTheme {
                bagSlot=new Sprite(),
                equipmentSlot=new Sprite(),
                itemDetailsPanel=new Sprite(),
                defaultMapSplash=new Sprite()
            };
            Check(art.bagSlot!=null && art.equipmentSlot!=null &&
                  art.itemDetailsPanel!=null && art.defaultMapSplash!=null,
                "Sprite references are independently assignable");
            Check(hero.currentLevel==level &&
                  hero.GetCalculatedDamage(AttackMode.Melee)==attack &&
                  hero.inventory.Count==stacks,
                "visual configuration does not mutate gameplay data");
        });
    }
}
