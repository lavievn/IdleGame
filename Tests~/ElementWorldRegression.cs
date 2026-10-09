using System;
using System.Collections.Generic;
using System.IO;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static void ElementWorldTests()
    {
        Run("all 25 tier matchups match approved counter and resistance table",()=>{
            float[] bonus={.075f,.075f,.15f,.225f,.30f,.33f,.36f,.39f,.42f};
            float[] penalty={.42f,.39f,.36f,.33f,.30f,.27f,.24f,.21f,.18f};
            for(int a=1;a<=5;a++)for(int d=1;d<=5;d++) {
                Near(SynergyMath.GetElementalMultiplier(ElementType.Thuy,a,ElementType.Hoa,d),1+bonus[a-d+4],"advantage table");
                Near(SynergyMath.GetElementalMultiplier(ElementType.Hoa,a,ElementType.Thuy,d),1-penalty[a-d+4],"resistance table");
                Near(SynergyMath.GetElementalMultiplier(ElementType.Vo,a,ElementType.Hoa,d),1,"void outgoing");
                Near(SynergyMath.GetElementalMultiplier(ElementType.Hoa,a,ElementType.Vo,d),1,"void incoming");
                Near(SynergyMath.GetElementalMultiplier(ElementType.Hoa,a,ElementType.Moc,d),1,"unrelated roots neutral");
            }
        });
        Run("weighted damage handles hybrid attacker and hybrid defender without multiplying base attack twice",()=>{
            var a=HeroAt(1);a.baseDamage=100;a.spiritRoots=new List<ElementType>{ElementType.Thuy,ElementType.Hoa};
            a.rootTiers=new List<int>{3,3};a.rootWeights=new List<float>{.6f,.4f};
            var d=HeroAt(1);d.spiritRoots=new List<ElementType>{ElementType.Kim};
            Check(SynergyMath.Damage(a,d,AttackMode.Melee,1)==112,"60 neutral plus 40 counter");
            d.spiritRoots=new List<ElementType>{ElementType.Kim,ElementType.Hoa};d.rootTiers=new List<int>{3,3};d.rootWeights=new List<float>{.5f,.5f};
            Near(SynergyMath.GetElementalMultiplier(a,d),1.15f,"weighted matrix");
            Check(SynergyMath.Damage(a,d,AttackMode.Melee,1)==115,"actual hybrid damage");
        });
        Run("generated profiles have bounded tiers, unique roots and normalized shares",()=>{
            bool voidSeen=false;bool unequalSeen=false;
            for(int n=0;n<1000;n++) {
                var a=HeroAt(1);a.race=RaceType.ConLai;SynergyMath.GenerateRootProfile(a);float sum=0;
                Check(a.spiritRoots.Count==2,"two root test profiles");
                for(int i=0;i<2;i++){Check(a.rootTiers[i]>=1&&a.rootTiers[i]<=5,"tier bounded");sum+=a.rootWeights[i];if(a.spiritRoots[i]==ElementType.Vo)voidSeen=true;}
                Near(sum,1,"shares sum to one");Check(a.rootWeights[0]>=.19999f&&a.rootWeights[0]<=.80001f,"80 20 cap");
                if(a.rootWeights[0]>.5f)Check(a.rootTiers[0]>a.rootTiers[1],"higher root has higher weight");
                unequalSeen|=a.rootWeights[0]!=.5f;
            }
            Check(voidSeen&&unequalSeen,"void and dominant root appear");
        });
        Run("animal names use dominant root, hybrid race marker and preserve human names",()=>{
            Check(WorldNames.AnimalNames.Length==50,"50 animals");
            var a=HeroAt(1);a.race=RaceType.YeuThu;a.spiritRoots=new List<ElementType>{ElementType.Hoa};
            Check(WorldNames.MonsterName(a,"Trư")=="Hỏa Trư","pure beast example");
            a.race=RaceType.ConLai;a.hybridSecondaryRace=RaceType.LinhThe;
            Check(WorldNames.MonsterName(a,"Trư")=="Hỏa Linh Trư","hybrid example");
            a.spiritRoots=new List<ElementType>{ElementType.Hoa,ElementType.Thuy};a.rootTiers=new List<int>{3,4};a.rootWeights=new List<float>{.4f,.6f};
            Check(WorldNames.MonsterName(a,"Trư")=="Thủy Linh Trư","weighted dominant name");
            a.race=RaceType.NhanToc;a.entityName="Hàn Lâm";Check(WorldNames.RandomMonsterName(a)=="Hàn Lâm","human exception");
        });
        Run("map names stay stable within wave and retry, change only on five wave transition",()=>{
            var a=HeroAt(1);a.NormalizeMapProgress();string name=a.mapName;var terrain=a.mapTerrain;
            Check(!string.IsNullOrEmpty(name)&&!name.StartsWith("Map "),"named first map");
            a.NormalizeMapProgress();Check(a.mapName==name&&a.mapTerrain==terrain,"retry stable");
            for(int i=0;i<4;i++){a.CompleteWave();Check(a.mapName==name,"not changed mid map");}
            a.CompleteWave();Check(a.mapNumber==2&&a.mapName!=name,"transition gets new name");
            var seen=new HashSet<TerrainType>();for(int i=0;i<200;i++){WorldNames.AssignMap(a);seen.Add(a.mapTerrain);}
            Check(seen.Count==6,"all terrain placeholders selectable");
        });
        Run("monster scaling reaches exact 20 map thresholds and caps at map 100",()=>{
            int[] maps={1,19,20,39,40,59,60,79,80,99,100,101,int.MaxValue};
            float[] scales={.5f,.5f,.6f,.6f,.7f,.7f,.8f,.8f,.9f,.9f,1,1,1};
            for(int i=0;i<maps.Length;i++)Near(WorldNames.MonsterDamageScale(maps[i]),scales[i],"ramp boundary");
            var f=new Field();var a=HeroAt(1);f.combat.SetupHeroInfo(a);
            for(int i=0;i<maps.Length;i++){a.mapNumber=maps[i];Set(f.combat,"currentHeroHP",400);typeof(CombatManager).GetMethod("DealDamageToHero",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(f.combat,new object[]{100});Check(Get<int>(f.combat,"currentHeroHP")==400-Mathf.RoundToInt(scales[i]*100),"live incoming damage");}
        });
        Run("save round trip preserves tiers weights map names and legacy loads cannot retain stale profiles",()=>{
            var f=new Field();SaveManager save;string dir;MenuManager(f,out save,out dir);
            try {
                var a=HeroAt(10);a.spiritRoots=new List<ElementType>{ElementType.Hoa,ElementType.Thuy};a.rootTiers=new List<int>{2,5};a.rootWeights=new List<float>{.2f,.8f};a.mapProgressVersion=1;a.mapNumber=40;a.NormalizeMapProgress();
                Check(save.SaveGame(a,SaveSlot.ManualSave1),"save profile");var read=HeroAt(1);Check(save.LoadGame(read,SaveSlot.ManualSave1),"read profile");
                Check(read.rootTiers[0]==2&&read.rootTiers[1]==5,"tiers persisted");Near(read.rootWeights[1],.8f,"weights persisted");Check(read.mapName==a.mapName&&read.mapTerrain==a.mapTerrain&&read.mapNumber==40,"map persisted");
                File.WriteAllText(Path.Combine(dir,"ManualSave1.json"),"{\"currentLevel\":10,\"spiritRoots\":[3],\"mapProgressVersion\":1,\"mapNumber\":20}");
                Check(save.LoadGame(read,SaveSlot.ManualSave1),"legacy read");Check(read.rootTiers.Count==1&&read.rootTiers[0]==3,"legacy defaults to tam");Near(read.rootWeights[0],1,"legacy full share");
                Check(!string.IsNullOrEmpty(read.mapName)&&read.mapNumber==20,"old progress preserved with named placeholder");
            } finally {Directory.Delete(dir,true);}
        });
    }
}
