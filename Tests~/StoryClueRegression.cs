using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void StoryClueRegressionTests()
    {
        Run("0.0.54 narrative is deterministic across 5 world regions",()=>{
            var h=new EntityDataSO {worldMapVersion=1, worldMapId="TV-20",
                storyVersion=1, storySeed=314159,
                storyFoundIds=new List<string>()};
            var regions=new HashSet<string>();
            foreach(var n in WorldMapCatalog.All)regions.Add(n.Continent);
            Check(regions.Count==5,"five regions");
            foreach(var region in regions)
            {
                var sites=new HashSet<string>();
                foreach(var id in new[]{StoryClues.Inscription,StoryClues.Jade,StoryClues.Seal})
                {
                    var site=StoryClues.Site(h,id,region);
                    WorldMapNode node;
                    Check(site==StoryClues.Site(h,id,region) &&
                          WorldMapCatalog.TryGet(site,out node) &&
                          !node.IsBoss && node.SuggestedLevel<=13 &&
                          node.Continent==region,"stable low-level location");
                    sites.Add(site);
                }
                Check(sites.Count==3,"unique per-region sites");
            }
        });
        Run("0.0.54 reverse discovery reaches true conclusion once",()=>{
            var h=new EntityDataSO{worldMapVersion=1,worldMapId="TV-20",
                storyVersion=1,storySeed=419,
                storyFoundIds=new List<string>(),worldMapWaveProgress=1};
            Check(StoryClues.OnCompletedWave(h).Count==1,"first wave rumor");
            Check(StoryClues.OnCompletedWave(h).Count==0,"no duplicate rumor");
            WorldMapNode current;
            Check(WorldMapCatalog.TryGet("TV-20",out current),"birth map exists");
            string[] order={StoryClues.Seal,StoryClues.Jade,StoryClues.Inscription};
            int[] waves={4,3,2};
            for(int i=0;i<order.Length;i++)
            {
                h.worldMapId=StoryClues.Site(h,order[i],current.Continent);
                h.worldMapWaveProgress=waves[i];
                var found=StoryClues.OnCompletedWave(h);
                Check(StoryClues.Has(h,order[i]),"clue recorded");
                Check(found.Count==(i==2?2:1),"final truth requires all sources");
            }
            Check(StoryClues.Has(h,StoryClues.Revelation),"complete story unlocked");
            Check(StoryClues.OnCompletedWave(h).Count==0,"no repeat");
            Check(StoryClues.Journal(h).Contains("CHÂN TƯỚNG"),"journal updated");
        });
        Run("0.0.54 story clues are isolated between manual saves and older saves",()=>{
            var f=new Field();SaveManager saves;string dir;
            MenuManager(f,out saves,out dir);
            try
            {
                var a=HeroAt(4);
                a.worldMapVersion=1;a.worldMapId="TV-20";
                a.storyVersion=1;a.storySeed=2468;
                a.storyFoundIds=new List<string>{StoryClues.Rumor,StoryClues.Jade};
                var b=HeroAt(7);
                b.worldMapVersion=1;b.worldMapId="TM-12";
                b.storyVersion=1;b.storySeed=9753;
                b.storyFoundIds=new List<string>{StoryClues.Seal};
                Check(saves.SaveGame(a,SaveSlot.ManualSave1),"slot 1 saves story A");
                Check(saves.SaveGame(b,SaveSlot.ManualSave2),"slot 2 saves story B");
                var loadedA=HeroAt(1);var loadedB=HeroAt(1);
                Check(saves.LoadGame(loadedA,SaveSlot.ManualSave1),"load A");
                Check(saves.LoadGame(loadedB,SaveSlot.ManualSave2),"load B");
                Check(loadedA.storySeed==2468 && loadedB.storySeed==9753 &&
                      StoryClues.Has(loadedA,StoryClues.Jade) &&
                      !StoryClues.Has(loadedB,StoryClues.Jade) &&
                      StoryClues.Has(loadedB,StoryClues.Seal),
                    "seeds and collected clues remain separate");

                var old=HeroAt(1);
                old.storyVersion=1;old.storySeed=999;
                old.storyFoundIds=new List<string>{StoryClues.Revelation};
                File.WriteAllText(Path.Combine(dir,"AutoSave.json"),
                    "{\"currentLevel\":2,\"currentExp\":0,\"worldMapVersion\":1,\"worldMapId\":\"TV-20\"}");
                Check(saves.LoadGame(old,SaveSlot.AutoSave),"pre-story world save loads");
                Check(old.storySeed!=0 && old.storySeed!=999 &&
                      old.storyFoundIds.Count==0,
                    "no narrative from template bleeds into legacy save");
                int initialSeed=old.storySeed;
                StoryClues.Normalize(old);
                Check(old.storySeed==initialSeed,"legacy seed remains fixed once assigned");
            }
            finally {Directory.Delete(dir,true);}
        });
    }
}
