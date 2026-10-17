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
    }
}
