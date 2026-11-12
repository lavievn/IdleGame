using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    sealed class N4Fixture
    {
        internal readonly GameManager Game;
        internal readonly SaveManager Saves;
        internal readonly EntityDataSO Hero;
        internal readonly string Path;
        internal N4Fixture(OriginStage stage=OriginStage.Return)
        {
            Hero=N3Hero(stage,participation:HerbalParticipation.Accepted);
            Game=new GameObject("N4 origin host").AddComponent<GameManager>();
            Saves=new GameObject("N4 memory saver").AddComponent<SaveManager>();
            Saves.ConfigureSaveDirectoryBeforeAwake("/virtual/owned-hanvu-n4/"+Guid.NewGuid().ToString("N"));
            Path=System.IO.Path.Combine(Saves.SaveDirectory,"AutoSave.json");
            Game.BindPersistenceFixture(Hero,Saves);
            Check(Saves.SaveGame(Hero,SaveSlot.AutoSave),"seed complete snapshot");
            Hero.isDirty=false;MemoryFiles.Replaces=0;
        }
        internal string Saved=>MemoryFiles.ReadAllText(Path);
    }
    static string RootWithoutStory(EntityDataSO hero)
    {
        Check(CultivationSaveJson.TryObject(JsonUtility.ToJson(hero),out var fields,out _),"root object");
        fields.Remove("originStory");fields.Remove("isDirty");return Json(fields);
    }
    static string Json(Dictionary<string,string> fields)=>"{"+string.Join(",",fields.Select(p=>"\""+p.Key+"\":"+p.Value))+"}";
    static string ReplaceStory(string root,Func<Dictionary<string,string>,string> change)
    {
        Check(CultivationSaveJson.TryObject(root,out var fields,out _),"root parse");
        Check(CultivationSaveJson.TryObject(fields["originStory"],out var story,out _),"story parse");
        var replacement=change(story);if(replacement==null)fields.Remove("originStory");else fields["originStory"]=replacement;return Json(fields);
    }
    static void N4Tests()
    {
        Run("N4 successful save publishes medicine once and preserves canonical Hero/story identity",()=>{
            var f=new N4Fixture();var story=f.Hero.originStory;string other=RootWithoutStory(f.Hero);
            Check(f.Game.TryOriginInteraction() && f.Game.HeroData==f.Hero && ReferenceEquals(f.Hero.originStory,story),"canonical identity");
            Check(f.Hero.originStory.stage==OriginStage.SearchForest && RootWithoutStory(f.Hero)==other,"only story result");
            var saved=JsonUtility.FromJson<EntityDataSO>(f.Saved);
            Check(saved.originStory.stage==OriginStage.SearchForest && RootWithoutStory(saved)==other,"whole saved result");
            int writes=MemoryFiles.Replaces;f.Game.TryOriginInteraction();StoryOrigin.Journal(f.Hero);
            Check(MemoryFiles.Replaces==writes,"duplicate/replay do not write another transition");
        });
        Run("REGRESSION N4 failure before write leaves RAM and durable snapshot unchanged",()=>{
            var f=new N4Fixture();string before=N3Facts(f.Hero),disk=f.Saved;
            f.Saves.FailNextOwnedWrite=SaveManager.OwnedWriteFault.BeforeWrite;
            Check(!f.Game.TryOriginInteraction(),"operation must not acknowledge a failed save");
            Check(N3Facts(f.Hero)==before && f.Saved==disk && MemoryFiles.Replaces==0,"no early publication");
        });
        Run("REGRESSION N4 interrupted temp write leaves story and durable snapshot unchanged",()=>{
            var f=new N4Fixture();string before=N3Facts(f.Hero),disk=f.Saved;
            MemoryFiles.FailDuringWrite=true;
            Check(!f.Game.TryOriginInteraction() && N3Facts(f.Hero)==before && f.Saved==disk,"partial temp is not a committed story");
        });
        Run("REGRESSION N4 failure before replace leaves RAM and prior file intact",()=>{
            var f=new N4Fixture();string before=N3Facts(f.Hero),disk=f.Saved;
            f.Saves.FailNextOwnedWrite=SaveManager.OwnedWriteFault.BeforeReplace;
            Check(!f.Game.TryOriginInteraction() && N3Facts(f.Hero)==before && f.Saved==disk,"no RAM-only success");
        });
        Run("REGRESSION N4 canonical story stays unchanged while candidate temp is being written",()=>{
            var f=new N4Fixture();bool observed=false,isolated=false,newTemp=false;
            MemoryFiles.AfterTempFlush=()=>{
                observed=true;isolated=f.Hero.originStory.stage==OriginStage.Return;
                var temp=JsonUtility.FromJson<EntityDataSO>(MemoryFiles.ReadAllText(System.IO.Path.Combine(f.Saves.SaveDirectory,"AutoSave.tmp")));
                newTemp=temp.originStory.stage==OriginStage.SearchForest;
            };
            Check(f.Game.TryOriginInteraction() && observed && isolated && newTemp,"canonical was changed while new temp was only staged");
        });
        Run("REGRESSION N4 fresh retry includes newer EXP loot and world progress",()=>{
            var f=new N4Fixture();f.Saves.FailNextOwnedWrite=SaveManager.OwnedWriteFault.AfterTempWrite;
            f.Game.TryOriginInteraction();
            f.Hero.AddExp(250);EquipmentInventory.Add(f.Hero,EquipmentRefinement.MaterialId,7);
            var item=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,4);Check(EquipmentRuntime.AddInstance(f.Hero,item),"loot fixture");
            f.Hero.mapVisits+=3;string expected=RootWithoutStory(f.Hero);
            Check(f.Game.TryOriginInteraction(),"retry acknowledged only when saved");
            var saved=JsonUtility.FromJson<EntityDataSO>(f.Saved);
            Check(saved.originStory.stage==OriginStage.SearchForest && RootWithoutStory(saved)==expected && RootWithoutStory(f.Hero)==expected,"no cached candidate overwrites newer unrelated state");
        });
        Run("REGRESSION N4 existing CAS preserves a competing newer file and does not publish rejected story",()=>{
            var f=new N4Fixture();string old=N3Facts(f.Hero);
            var newer=JsonUtility.FromJson<EntityDataSO>(f.Saved);newer.currentExp+=17;string competing=JsonUtility.ToJson(newer);
            MemoryFiles.AfterTempFlush=()=>MemoryFiles.WriteAllBytes(f.Path,Encoding.UTF8.GetBytes(competing));
            Check(!f.Game.TryOriginInteraction() && f.Saved==competing && N3Facts(f.Hero)==old,"rejected CAS cannot be RAM success");
        });
        Run("N4 GameManager Load candidate path restores once and repeated Load never reruns medicine",()=>{
            var f=new N4Fixture();Check(f.Game.TryOriginInteraction(),"save success");string saved=f.Saved;int writes=MemoryFiles.Replaces;
            for(int i=0;i<3;i++)
            {
                Check(f.Game.LoadFixture(SaveSlot.AutoSave),"real load validators/candidate host path");
                Check(f.Game.HeroData.originStory.stage==OriginStage.SearchForest && f.Saved==saved && MemoryFiles.Replaces==writes,"restore is read-only");
            }
        });
        Run("N4 legacy absent and v1 v2 v3 preserve identities without new effects",()=>{
            foreach(int version in new[]{0,1,2,3})
            {
                var f=new N4Fixture(OriginStage.Gather);string identity=f.Hero.originStory.guardian.id+f.Hero.originStory.guardian.name;
                string root=ReplaceStory(f.Saved,story=>{
                    if(version==0)return null;
                    story["version"]=version.ToString();
                    if(version<3)foreach(var name in new[]{"heardOpeningTrip","heardWoodcutter","sawTrailKnot","sawTrailNote"})story.Remove(name);
                    if(version<2)foreach(var name in new[]{"herbalParticipation","declinedHerbalOffer","hideStoryPrompts","encounters"})story.Remove(name);
                    return Json(story);
                });
                MemoryFiles.WriteAllBytes(f.Path,Encoding.UTF8.GetBytes(root));
                Check(f.Game.LoadFixture(SaveSlot.AutoSave),"legacy load "+version);
                var loaded=f.Game.HeroData;
                Check(version==0?loaded.originStory==null:loaded.originStory.version==3 && loaded.originStory.guardian.id+loaded.originStory.guardian.name==identity && !loaded.originStory.heardWoodcutter,"identity and absence "+version);
                Check(MemoryFiles.ReadAllText(f.Path)==root,"loading never writes effects");
            }
        });
        Run("N4 future and invalid origin Load never replace canonical Hero",()=>{
            foreach(bool future in new[]{true,false})
            {
                var f=new N4Fixture();string before=N3Facts(f.Hero);
                string bad=ReplaceStory(f.Saved,story=>{story[future?"version":"herbWaves"]=future?"99":"999";return Json(story);});
                MemoryFiles.WriteAllBytes(f.Path,Encoding.UTF8.GetBytes(bad));
                Check(!f.Game.LoadFixture(SaveSlot.AutoSave) && f.Game.HeroData==f.Hero && N3Facts(f.Hero)==before && f.Game.PublishedLoads==0,"rejected candidate keeps canonical");
            }
        });
        Run("REGRESSION N4 partial current origin component is rejected before canonical swap",()=>{
            var f=new N4Fixture(OriginStage.Gather);string before=N3Facts(f.Hero);
            string partial=ReplaceStory(f.Saved,story=>{story.Remove("declinedHerbalOffer");return Json(story);});
            MemoryFiles.WriteAllBytes(f.Path,Encoding.UTF8.GetBytes(partial));
            Check(!f.Game.LoadFixture(SaveSlot.AutoSave) && f.Game.HeroData==f.Hero && N3Facts(f.Hero)==before,"missing field silently defaulted and replaced canonical");
        });
        Run("REGRESSION N4 supported Hero cannot overwrite a future origin component",()=>{
            var f=new N4Fixture();string future=ReplaceStory(f.Saved,story=>{story["version"]="99";return Json(story);});
            MemoryFiles.WriteAllBytes(f.Path,Encoding.UTF8.GetBytes(future));
            Check(!f.Saves.SaveGame(f.Hero,SaveSlot.AutoSave) && f.Saved==future,"future origin must be preserved like other versioned components");
        });
    }
}
