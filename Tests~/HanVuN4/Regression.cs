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
            var volume = System.IO.Path.GetPathRoot(Environment.CurrentDirectory);
            Saves.ConfigureSaveDirectoryBeforeAwake(System.IO.Path.Combine(volume,
                "virtual", "owned-hanvu-n4", Guid.NewGuid().ToString("N")));
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
        Run("N4 every explicit story action remains retryable after failure and duplicates are no-ops",()=>{
            foreach(var action in new[]{"accept","decline","trip","woodcutter","observe","encounter","prompts"})
            {
                var f=new N4Fixture(OriginStage.Gather);
                f.Hero.originStory.herbalParticipation=HerbalParticipation.Offered;
                if(action=="woodcutter")f.Hero.worldMapId=StoryOrigin.HanVu.Forest;
                if(action=="encounter")Check(f.Game.OpenOriginEncounter()!=null,"pending observed");
                Check(f.Saves.SaveGame(f.Hero,SaveSlot.AutoSave),"fixture state");
                string before=N3Facts(f.Hero),disk=f.Saved;
                Func<bool> invoke=()=>{
                    switch(action) {
                        case "accept":return f.Game.TryChooseHerbs(true);
                        case "decline":return f.Game.TryChooseHerbs(false);
                        case "trip":f.Game.RecordOriginTrip();return f.Hero.originStory.heardOpeningTrip;
                        case "woodcutter":return f.Game.TryTalkToWoodcutter(1);
                        case "observe":return f.Game.OpenOriginEncounter()!=null;
                        case "encounter":return f.Game.TryChooseOriginEncounter(HanVuEncounters.Available(f.Hero).Id,EncounterChoice.Participated);
                        default:f.Game.ToggleOriginPrompts();return f.Hero.originStory.hideStoryPrompts;
                    }
                };
                f.Saves.FailNextOwnedWrite=SaveManager.OwnedWriteFault.BeforeReplace;
                Check(!invoke() && N3Facts(f.Hero)==before && f.Saved==disk,"failed "+action);
                Check(invoke() && N3Facts(f.Hero)!=before,"retry "+action);
                if(action=="prompts" || action=="encounter")continue; // Toggle is a new intent; resolved offer is unavailable.
                int writes=MemoryFiles.Replaces;invoke();Check(MemoryFiles.Replaces==writes,"duplicate "+action);
            }
        });
        Run("N4 automatic arrival wave and clue domain changes share the same persistence boundary",()=>{
            foreach(var action in new[]{"arrival","wave","knot","note"})
            {
                var f=new N4Fixture(OriginStage.Gather);
                if(action=="knot" || action=="note")f.Hero.worldMapId=StoryOrigin.HanVu.Forest;
                if(action=="wave")f.Hero.worldMapId=StoryOrigin.HanVu.Forest;
                string before=N3Facts(f.Hero);
                Func<EntityDataSO,bool> change=h=>action=="arrival"?StoryOrigin.Arrive(h)!=null:
                    action=="wave"?StoryOrigin.CreditWave(h,false,out _):action=="knot"?HanVuNarrative.ObserveKnot(h):HanVuNarrative.ObserveNote(h);
                f.Saves.FailNextOwnedWrite=SaveManager.OwnedWriteFault.BeforeWrite;
                Check(!f.Game.CommitFixture(change) && N3Facts(f.Hero)==before,"no publication "+action);
                Check(f.Game.CommitFixture(change) && N3Facts(f.Hero)!=before,"fresh confirmed action "+action);
            }
        });
        Run("N4 detached origin snapshot copies all current fields actors and encounter records",()=>{
            var h=N3Hero(OriginStage.Return,participation:HerbalParticipation.Accepted);
            h.originStory.introduced=h.originStory.heardCompanion=h.originStory.heardOpeningTrip=true;
            h.originStory.heardWoodcutter=h.originStory.sawTrailKnot=h.originStory.sawTrailNote=true;
            h.originStory.declinedHerbalOffer=h.originStory.hideStoryPrompts=true;
            HanVuEncounters.Observe(h,HanVuEncounters.Available(h).Id);
            var copy=StoryOrigin.CopyState(h.originStory);
            Check(NestedEquipmentSaveStub.Encode(copy)==N3Facts(h),"all fields roundtrip");
            Check(!ReferenceEquals(copy,h.originStory) && !ReferenceEquals(copy.guardian,h.originStory.guardian) &&
                !ReferenceEquals(copy.companion,h.originStory.companion) && !ReferenceEquals(copy.encounters,h.originStory.encounters) &&
                !ReferenceEquals(copy.encounters[0],h.originStory.encounters[0]),"all mutable origin nodes detached");
            string before=N3Facts(h);copy.guardian.name="fixture altered";copy.companion.name="fixture altered";
            copy.encounters[0].choice=EncounterChoice.Declined;copy.encounters.Add(new OriginEncounterRecord());
            Check(N3Facts(h)==before,"candidate mutations cannot leak through aliases");
            var root=h.originStory;StoryOrigin.CopyState(StoryOrigin.CopyState(root),root);
            Check(ReferenceEquals(root,h.originStory) && N3Facts(h)==before,"publication keeps root identity");
        });
        Run("N4 source changing during candidate capture is rejected before any write",()=>{
            foreach(var kind in new[]{"exp","hero","spawn","map"})
            {
                var f=new N4Fixture();var replacement=N3Hero();string before=N3Facts(f.Hero),disk=f.Saved;
                MemoryFiles.CloneHook=()=>{switch(kind){case "exp":f.Hero.currentExp++;break;case "hero":f.Game.BindFixture(replacement);break;
                    case "spawn":f.Game.RespawnFixture();break;default:f.Hero.worldMapId=StoryOrigin.HanVu.Forest;break;}};
                Check(!f.Game.TryOriginInteraction() && N3Facts(f.Hero)==before && f.Saved==disk,"rejected changed "+kind);
                Check(kind!="hero" || f.Game.HeroData==replacement,"replacement retained");
            }
        });
        Run("N4 reentrant duplicate cannot enter a second save while first command is staged",()=>{
            var f=new N4Fixture();bool duplicateAccepted=true;
            MemoryFiles.AfterTempFlush=()=>duplicateAccepted=f.Game.TryOriginInteraction();
            Check(f.Game.TryOriginInteraction() && !duplicateAccepted && MemoryFiles.Replaces==1 &&
                f.Hero.originStory.stage==OriginStage.SearchForest,"one commit");
        });
        Run("N4 unexpected context change inside synchronous writer never restores old candidate onto replacement runtime",()=>{
            foreach(var kind in new[]{"hero","spawn","map","exp"})
            {
                var f=new N4Fixture();var replacement=N3Hero();string before=N3Facts(f.Hero);
                MemoryFiles.AfterTempFlush=()=>{switch(kind){case "hero":f.Game.BindFixture(replacement);break;case "spawn":f.Game.RespawnFixture();break;
                    case "exp":f.Hero.currentExp++;break;default:f.Hero.worldMapId=StoryOrigin.HanVu.Forest;break;}};
                Check(!f.Game.TryOriginInteraction() && N3Facts(f.Hero)==before && f.Game.Log.Count==0,"RAM protected after "+kind);
                Check(kind!="hero" || f.Game.HeroData==replacement,"no old Hero restoration");
                // Deliberately NOT an atomicity claim: the file can already contain
                // the saved candidate. Production currently has no yield/callback here.
                Check(MemoryFiles.Replaces==1,"documents post-write publication limit");
            }
        });
        Run("N4 real UI keeps failed choice open for retry and invalidates it after modeled Load",()=>{
            var f=new N4Fixture(OriginStage.Gather);f.Hero.originStory.herbalParticipation=HerbalParticipation.Offered;
            var ui=new GameObject("N4 choice UI").AddComponent<UIManager>();ui.InitializeFixture(f.Game);
            ui.PressFixture("HanVuTalk");for(int i=0;i<5;i++)ui.PressFixture("HanVuNext");
            var accept=ui.ActionFixture("HanVuAccept");f.Saves.FailNextOwnedWrite=SaveManager.OwnedWriteFault.BeforeReplace;
            accept();Check(f.Hero.originStory.herbalParticipation==HerbalParticipation.Offered,"failed choice unconfirmed");
            accept();Check(f.Hero.originStory.herbalParticipation==HerbalParticipation.Accepted,"same view can retry");
            int writes=MemoryFiles.Replaces;accept();Check(MemoryFiles.Replaces==writes,"duplicate UI delegate is retired");
            Check(f.Game.LoadFixture(SaveSlot.AutoSave),"candidate Load");var loaded=f.Game.HeroData;string facts=N3Facts(loaded);
            accept();Check(N3Facts(loaded)==facts && MemoryFiles.Replaces==writes,"old UI cannot act after Load");
        });
        Run("N4 origin strict preflight rejects each missing current field wrong scalar and partial nested record",()=>{
            var f=new N4Fixture();
            Check(CultivationSaveJson.TryObject(f.Saved,out var root,out _),"root");
            Check(CultivationSaveJson.TryObject(root["originStory"],out var original,out _),"origin");
            foreach(var field in original.Keys)
            {
                var missing=new Dictionary<string,string>(original);missing.Remove(field);root["originStory"]=Json(missing);
                Check(!StoryOrigin.IsValidSnapshotJson(Json(root)),"missing "+field);
            }
            foreach(var pair in new[]{new[]{"version","3.0"},new[]{"introduced","1"},new[]{"stage","\"1\""},
                new[]{"guardian","{}"},new[]{"companion","null"},new[]{"encounters","[{}]"},
                new[]{"encounters","[{\"id\":\"hanvu.merchant_rumor\"}]"},
                new[]{"encounters","[{\"id\":\"hanvu.merchant_rumor\",\"choice\":\"0\"}]"}})
            {
                var bad=new Dictionary<string,string>(original);bad[pair[0]]=pair[1];root["originStory"]=Json(bad);
                Check(!StoryOrigin.IsValidSnapshotJson(Json(root)),"invalid "+pair[0]+" "+pair[1]);
            }
            Check(!StoryOrigin.IsValidSnapshotJson("{\"originStory\":null,\"originStory\":null}"),"duplicate root field");
        });
        Run("N4 nonempty encounter arrays escaped actor names and explicit legacy null/sentinel remain compatible",()=>{
            var f=new N4Fixture(OriginStage.Gather);f.Hero.originStory.guardian.name="Fixture \\\" text , } [ ";
            Check(f.Game.OpenOriginEncounter()!=null,"nonempty array persisted");
            Check(f.Game.TryChooseOriginEncounter(HanVuEncounters.Available(f.Hero).Id,EncounterChoice.Declined),"record choice");
            Check(StoryOrigin.IsValidSnapshotJson(f.Saved),"valid escaping/array");
            Check(CultivationSaveJson.TryObject(f.Saved,out var fields,out _),"root");
            foreach(string legacy in new[]{"null","{\"version\":0}","{\"version\":0,\"originId\":\"\"}"})
            {fields["originStory"]=legacy;Check(StoryOrigin.IsValidSnapshotJson(Json(fields)),"legacy "+legacy);}
        });
        Run("N4 unrelated invalid future and partial components still block publication and preserve every slot",()=>{
            foreach(var component in new[]{"equipment","realm","cultivation","skills","trial","climate","experience"})
            {
                var f=new N4Fixture();string before=N3Facts(f.Hero),disk=f.Saved;
                Check(CultivationSaveJson.TryObject(disk,out var fields,out _),"root");
                switch(component){case "equipment":fields["equipmentVersion"]="99";break;case "realm":fields.Remove("realmId");break;
                    case "cultivation":fields["cultivationJson"]="\"{\\\"version\\\":99}\"";break;
                    case "skills":fields["combatSkills"]="{\"version\":99}";break;
                    case "climate":fields["worldClimate"]="{\"version\":99,\"seed\":44,\"day\":2,\"dayFraction\":0.4}";break;
                    case "experience":fields["experienceJson"]="null";break;
                    default:fields["tribulationJson"]="\"{\\\"version\\\":99}\"";break;}
                string invalid=Json(fields);MemoryFiles.WriteAllBytes(f.Path,Encoding.UTF8.GetBytes(invalid));
                Check(!f.Game.LoadFixture(SaveSlot.AutoSave) && f.Game.HeroData==f.Hero && N3Facts(f.Hero)==before,"Load "+component);
                Check(!f.Game.TryOriginInteraction() && f.Saved==invalid && N3Facts(f.Hero)==before,"write "+component);
            }
        });
        Run("N4 retry preserves live Qi binding equipment Forge combat resources and manual slots",()=>{
            var f=new N4Fixture();
            var definition=new CultivationPlaytestDefinition {
                schemaVersion=1,version=1,id="playtest:hanvu-n4-owned",title="Boundary-only fixture",
                startingRealm="realm:luyen-khi",maximumFrameSeconds=.1,stepSeconds=.1,fallbackDensity=1,
                absorptionRate=1,absorptionEfficiency=1,refinementRate=1,refinementEfficiency=1,maximumEnergy=100,
                realms=new[]{new CultivationPlaytestRealm{id="realm:luyen-khi",name="Luyện Khí",tier=0,thresholds=new double[]{0,300,600,1000},successor=""}},
                qiSources=new[]{new CultivationQiSource{nodeId=StoryOrigin.HanVu.Village,density=1}}
            };
            Check(CultivationPlaytestProfile.TryCreate(definition,id=>{WorldMapNode n;return WorldMapCatalog.TryGet(id,out n);},out var profile,out var why),"fixture profile: "+why);
            f.Saves.ConfigureCultivationPlaytestBeforeAwake(profile,false);
            using(var binding=new CultivationGameplayBinding(f.Hero,profile,CultivationGameplayState.New(profile)))
            {
                Check(EquipmentRuntime.GrantDemoSword(f.Hero),"existing demo Forge fixture");
                var sword=f.Hero.equipmentItems[0];Check(EquipmentRuntime.Equip(f.Hero,sword.instanceId,EquipmentSlot.Weapon),"equipped");
                EquipmentInventory.Add(f.Hero,EquipmentRefinement.MaterialId,100);
                Check(EquipmentRefinement.TryApply(f.Hero,sword.instanceId,EquipmentRefinement.Operation.NormalUp,out _),"existing Forge action");
                f.Hero.combatSkills.mana=37;f.Hero.combatSkills.health=42;f.Hero.gold=12345;
                Check(f.Saves.SaveGame(f.Hero,SaveSlot.ManualSave1) && f.Saves.SaveGame(f.Hero,SaveSlot.ManualSave2),"manual fixtures");
                string manual1=MemoryFiles.ReadAllText(System.IO.Path.Combine(f.Saves.SaveDirectory,"ManualSave1.json"));
                string manual2=MemoryFiles.ReadAllText(System.IO.Path.Combine(f.Saves.SaveDirectory,"ManualSave2.json"));
                f.Saves.FailNextOwnedWrite=SaveManager.OwnedWriteFault.BeforeReplace;
                Check(!f.Game.TryOriginInteraction(),"failed narrative commit");
                for(int frame=0;frame<5;frame++)Check(binding.Advance(f.Hero,frame,.1,false,f.Hero.worldMapId)==CultivationReason.None,"live Qi");
                f.Hero.worldClimate = WorldClimateState.AtDawn(351);
                f.Hero.worldClimate.day = 2;
                f.Hero.worldClimate.dayFraction = 0.375;
                f.Hero.experienceJson = ExperienceProgress.Capture(new ExperienceSnapshot { reserve = "1100" });
                f.Hero.AddExp(250);EquipmentInventory.Add(f.Hero,EquipmentRefinement.MaterialId,3);f.Hero.mapVisits++;
                var bag=f.Hero.inventory;var gear=f.Hero.equipmentItems;var skills=f.Hero.combatSkills;
                string other=RootWithoutStory(f.Hero),qi=f.Hero.cultivationJson;
                Check(f.Game.TryOriginInteraction(),"fresh retry");
                Check(RootWithoutStory(JsonUtility.FromJson<EntityDataSO>(f.Saved))==other && RootWithoutStory(f.Hero)==other,"all unrelated serialized fields preserved");
                Check(ReferenceEquals(bag,f.Hero.inventory) && ReferenceEquals(gear,f.Hero.equipmentItems) && ReferenceEquals(skills,f.Hero.combatSkills),"no canonical component replacement");
                Check(binding.Advance(f.Hero,6,.1,false,f.Hero.worldMapId)==CultivationReason.None,"original Qi binding still owns canonical Hero");
                Check(f.Game.LoadFixture(SaveSlot.AutoSave),"load real component validators");
                var loaded=f.Game.HeroData;
                Check(loaded.worldClimate.seed == 351 && loaded.worldClimate.day == 2 &&
                    loaded.experienceJson == f.Hero.experienceJson &&
                    ExperienceProgress.TryRead(loaded.experienceJson,out var reserve) &&
                    ExperienceProgress.Amount(reserve) == 1100, "loaded climate and EXP reserve");
                Check(loaded.cultivationJson==qi && loaded.currentExp==JsonUtility.FromJson<EntityDataSO>(f.Saved).currentExp &&
                    loaded.gold==12345 && loaded.combatSkills.mana==37 && loaded.combatSkills.health==42 &&
                    loaded.equipmentItems[0].instanceId==sword.instanceId && loaded.equipmentItems[0].refineLevel==sword.refineLevel,"selected loaded component authorities");
                Check(MemoryFiles.ReadAllText(System.IO.Path.Combine(f.Saves.SaveDirectory,"ManualSave1.json"))==manual1 &&
                    MemoryFiles.ReadAllText(System.IO.Path.Combine(f.Saves.SaveDirectory,"ManualSave2.json"))==manual2,"manual slots untouched");
            }
        });
        Run("REGRESSION N4 unknown origin fields cannot be silently discarded by a supported writer",()=>{
            foreach(var location in new[]{"origin","guardian","encounter"})
            {
                var f=new N4Fixture(OriginStage.Gather);
                Check(f.Game.OpenOriginEncounter()!=null,"record fixture");
                string unknown=ReplaceStory(f.Saved,story=>{
                    if(location=="origin")story["unrecognizedOutcome"]="true";
                    else if(location=="guardian") {
                        Check(CultivationSaveJson.TryObject(story["guardian"],out var actor,out _),"actor");
                        actor["unrecognizedCondition"]="true";story["guardian"]=Json(actor);
                    }
                    else story["encounters"]="[{\"id\":\"hanvu.merchant_rumor\",\"choice\":0,\"unrecognizedReceipt\":1}]";
                    return Json(story);
                });
                MemoryFiles.WriteAllBytes(f.Path,Encoding.UTF8.GetBytes(unknown));
                Check(!f.Game.LoadFixture(SaveSlot.AutoSave) && f.Game.HeroData==f.Hero,"Load must preserve unknown "+location);
                Check(!f.Saves.SaveGame(f.Hero,SaveSlot.AutoSave) && f.Saved==unknown,"write must preserve unknown "+location);
            }
        });
    }
}
