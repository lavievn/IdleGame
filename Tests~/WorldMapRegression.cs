using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TuTienCore;

partial class MotionRegression
{
    static void WorldMapRegressionTests()
    {
        // run.sh starts from repo root, with a pure Unity test double.
        var data=File.ReadAllText("Assets/Resources/WorldMapCatalog.txt");
        var seeds=File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt");
        Resources.Register("WorldMapCatalog",new TextAsset(data));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(seeds));
        Run("indexed map positions import all 133 seeds without fabricated source artwork",()=>{
            int count=0,uncertain=0;
            foreach (string line in seeds.Split('\n'))
            {
                if (line.Length==0 || line[0]=='#') continue;
                count++;
                if(line.Contains("INFERRED"))uncertain++;
            }
            Check(count==133,"133 prototype seed positions");
            Check(uncertain==6,"six ambiguous names remain explicitly flagged");
            string geometry=File.ReadAllText("Assets/Resources/WorldMapPolygons.json");
            Check(geometry.Contains("\"artworkWidth\": 1448") &&
                geometry.Contains("\"artworkHeight\": 1086"),
                "indexed source dimension metadata");
            Check(geometry.Contains("\"reviewed\": false"),
                "generated polygons are not claimed reviewed");
        });
        Run("world catalog has 133 unique maps, seven animals and five approved starts",()=>{
            Check(WorldMapCatalog.All.Count==133,"133 maps loaded");
            var ids=new HashSet<string>();
            var starts=new HashSet<string>();
            var bosses=new HashSet<string>();
            foreach(WorldMapNode node in WorldMapCatalog.All)
            {
                Check(ids.Add(node.Id),"unique map id "+node.Id);
                Check(node.Animals.Count==7 &&
                    new HashSet<string>(node.Animals).Count==7,"seven different species "+node.Id);
                if(node.IsStart)starts.Add(node.Id);
                if(node.IsBoss)bosses.Add(node.Id);
                Check(node.SuggestedLevel>=1,"fixed map level for "+node.Id);
                foreach(string link in node.Links)
                {
                    WorldMapNode other;
                    Check(WorldMapCatalog.TryGet(link,out other) && other.Links.Contains(node.Id),
                        "reciprocal temporary edge "+node.Id+" "+link);
                }
            }
            Check(starts.SetEquals(new[]{"TV-20","TL-16","XD-11","TM-12","HT-17"}),"5 start map IDs");
            WorldMapNode indexedStart;
            Check(WorldMapCatalog.TryGet("TV-20",out indexedStart),
                "TV-20 exists in original image");
            Check(Math.Abs(indexedStart.PreviewPosition.x - 219f/1448f)<.0002f &&
                Math.Abs(indexedStart.PreviewPosition.y - (1f-330f/1086f))<.0002f,
                "world map uses actual indexed pixel center, not fake coordinate grid");
            Check(bosses.SetEquals(new[]{"HT-27","TV-39","TM-25","XD-03","TL-02","HT-01"}),"6 endgame boss IDs");
            foreach(var boss in bosses)
            {
                WorldMapNode node;
                Check(WorldMapCatalog.TryGet(boss,out node) && node.Links.Count==1,"boss has only one edge");
                Check(!starts.Contains(node.Links[0]),"no starter immediately borders boss");
            }
        });
        Run("new world Hero remains in current map, loops wave 1 after fifth clear",()=>{
            var hero=new EntityDataSO { mapProgressVersion=1 };
            Check(WorldMapCatalog.StartNew(hero),"new World Map setup");
            string birthplace=hero.worldMapId;
            WorldMapNode current;Check(WorldMapCatalog.TryGet(birthplace,out current)&&current.IsStart,
                "random birthplace is an approved start");
            var neighbor=current.Links[0];
            Check(WorldMapCatalog.TravelBlockReason(hero,neighbor)!=null,
                "neighbor hidden / travel blocked before clear");
            for(int wave=1;wave<5;wave++)
            {
                Check(!hero.CompleteWave(),"wave "+wave+" must not clear");
                Check(hero.worldMapId==birthplace,"do not auto travel on incomplete wave");
                Check(hero.worldMapLoopWave==wave+1,"advance local combat wave");
            }
            Check(hero.CompleteWave(),"fifth wave clears map for first time");
            Check(hero.worldMapId==birthplace,"map stays on same node after clear");
            Check(hero.worldMapLoopWave==1,"wave loops back to 1 after the clear");
            Check(WorldMapCatalog.IsCleared(hero,birthplace),"clear state persists");
            Check(WorldMapCatalog.TravelBlockReason(hero,neighbor)==null,
                "adjacent non-boss map travel unlocked");
            for(int wave=0;wave<15;wave++)hero.CompleteWave();
            Check(hero.worldMapId==birthplace && hero.worldMapLoopWave==1,
                "can farm three full loops without automatic travel");
            Check(hero.worldMapCleared.Count==1,"repeating wave does not add duplicate clears");
            Check(WorldMapCatalog.TryTravel(hero,neighbor),"manual travel after clear");
            Check(hero.worldMapId==neighbor && hero.worldMapWaveProgress==0 &&
                hero.worldMapLoopWave==1,"new map starts at wave 1 only when user chooses it");
            Check(hero.worldMapVisited.Contains(birthplace) && hero.worldMapVisited.Contains(neighbor),
                "visited history persists across map travel");
        });
        Run("boss endgame is gated and world progress JSON is backward-compatible",()=>{
            var hero=new EntityDataSO { mapProgressVersion=1 };
            Check(WorldMapCatalog.StartNew(hero),"world start");
            string original=hero.worldMapId;
            hero.worldMapCleared.Add(original);
            hero.worldMapWaveProgress=5;
            var json=JsonUtility.ToJson(hero,true);
            var loaded=new EntityDataSO { worldMapVersion=0,worldMapId="INVALID" };
            JsonUtility.FromJsonOverwrite(json,loaded);
            loaded.NormalizeMapProgress();
            Check(loaded.worldMapVersion==1 && loaded.worldMapId==original,"world fields round-trip");
            Check(loaded.worldMapVisited.Contains(original) &&
                loaded.worldMapCleared.Contains(original),"persist visited + cleared map lists");
            var legacy=new EntityDataSO { mapProgressVersion=1,mapNumber=5 };
            legacy.NormalizeMapProgress();
            Check(legacy.worldMapVersion==0 && legacy.mapNumber==5,"legacy sequential progression kept");
            var boss=new WorldMapNode();
            Check(WorldMapCatalog.TryGet("XD-03",out boss),"endgame map exists");
            Check(boss.IsBoss,"marked endgame");
            Check(WorldMapCatalog.TravelBlockReason(hero,boss.Id)!=null,
                "endgame cannot be bypassed simply through a direct jump");
        });
    }
}
