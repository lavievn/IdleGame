using System;
using System.Linq;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static EntityDataSO N3Hero(OriginStage stage=OriginStage.Gather,string map=null,
        HerbalParticipation participation=HerbalParticipation.Offered)
    {
        var h=ScriptableObject.CreateInstance<EntityDataSO>();StoryOrigin.InitializeNewHero(h);
        WorldMapCatalog.StartNew(h,StoryOrigin.HanVu.Village);h.ApplyHeroBalance();
        h.worldMapId=map??StoryOrigin.HanVu.Village;
        h.originStory.stage=stage;h.originStory.herbalParticipation=participation;
        h.originStory.herbWaves=stage>=OriginStage.Return?5:0;
        h.originStory.searchWaves=stage==OriginStage.Complete?5:0;
        if(participation==HerbalParticipation.Declined)h.originStory.declinedHerbalOffer=true;
        Check(StoryOrigin.IsValid(h),"valid fixture");h.isDirty=false;return h;
    }
    // Structural equality only; not a native serializer/SaveManager test.
    static string N3Facts(EntityDataSO h) => NestedEquipmentSaveStub.Encode(h.originStory);
    static bool M1FromExistingFacts(EntityDataSO h) => StoryOrigin.Journal(h).Any(x=>
        x=="Ghi nhận · Người nuôi dưỡng tạm dịu cơn ho sau khi uống thuốc.");

    static void N3DomainTests()
    {
        Run("N3 Return can resolve only at village; repeat interaction does not consume herbs twice",()=>{
            var h=N3Hero(OriginStage.Return,StoryOrigin.HanVu.Forest,HerbalParticipation.Accepted);
            string before=N3Facts(h);
            Check(!StoryOrigin.CanInteract(h) && StoryOrigin.Interact(h)==null && N3Facts(h)==before && !h.isDirty,"wrong map rejected");
            h.worldMapId=StoryOrigin.HanVu.Village;
            Check(!M1FromExistingFacts(h) && StoryOrigin.Interact(h)!=null && h.originStory.stage==OriginStage.SearchForest &&
                M1FromExistingFacts(h) && StoryOrigin.ItemCount(h,StoryOrigin.HanVu.HerbId)==0,"one medicine transition");
            before=N3Facts(h);h.isDirty=false;
            for(int i=0;i<10;i++)StoryOrigin.Interact(h);
            Check(N3Facts(h)==before && !h.isDirty,"repeat talks only read the resolved facts");
        });
        Run("N3 M1 and guardian condition remain derivable at all later stages without a new field",()=>{
            foreach(OriginStage stage in Enum.GetValues(typeof(OriginStage)))
            {
                var h=N3Hero(stage,participation:HerbalParticipation.Accepted);string before=N3Facts(h);
                bool reached=stage==OriginStage.SearchForest || stage==OriginStage.SearchElite || stage==OriginStage.Complete;
                Check(M1FromExistingFacts(h)==reached,"guardian journal projection at "+stage);
                foreach(var kind in new[]{HanVuConversation.Opening,HanVuConversation.BeforeMedicine,HanVuConversation.AfterMedicine})
                    HanVuNarrative.Lines(h,kind);
                StoryOrigin.Journal(h);StoryOrigin.Hint(h);StoryOrigin.HintMap(h);StoryOrigin.ItemCount(h,StoryOrigin.HanVu.HerbId);
                Check(N3Facts(h)==before && !h.isDirty,"queries/replay are side-effect free at "+stage);
            }
        });
        Run("N3 forest action requires the right map participation and SearchForest stage",()=>{
            foreach(var stage in new[]{OriginStage.Gather,OriginStage.Return,OriginStage.SearchForest,OriginStage.SearchElite,OriginStage.Complete})
            {
                var h=N3Hero(stage,StoryOrigin.HanVu.Forest,HerbalParticipation.Accepted);
                string before=N3Facts(h);bool can=stage==OriginStage.SearchForest;
                Check(StoryOrigin.CanInteract(h)==can,"exact forest stage");
                Check((StoryOrigin.Interact(h)!=null)==can,"interaction matches eligibility");
                if(can)
                    Check(h.originStory.stage==OriginStage.SearchElite && StoryOrigin.Interact(h)==null && M1FromExistingFacts(h),"once; M1 survives");
                else Check(N3Facts(h)==before,"no mutation outside stage");
            }
            foreach(var p in new[]{HerbalParticipation.Offered,HerbalParticipation.Declined})
            {
                var h=N3Hero(map:StoryOrigin.HanVu.Forest,participation:p);
                Check(!StoryOrigin.CanInteract(h) && StoryOrigin.Interact(h)==null,"no implicit opt-in");
            }
        });
        Run("N3 decline is idempotent and reversible; travel and reads never choose a branch",()=>{
            var h=N3Hero();Check(StoryOrigin.ChooseHerbs(h,false),"explicit decline");string declined=N3Facts(h);h.isDirty=false;
            Check(!StoryOrigin.ChooseHerbs(h,false) && N3Facts(h)==declined && !h.isDirty,"duplicate decline rejected");
            Check(WorldMapCatalog.TryTravel(h,StoryOrigin.HanVu.Forest),"travel under normal map rules");
            for(int i=0;i<10;i++){h.CompleteWave();StoryOrigin.Journal(h);HanVuNarrative.Lines(h,HanVuConversation.Woodcutter);StoryOrigin.CreditWave(h,false,out _);}
            Check(N3Facts(h)==declined,"ignore/travel/farm cannot accept");
            Check(WorldMapCatalog.TryTravel(h,StoryOrigin.HanVu.Village),"return after map clear");
            Check(StoryOrigin.ChooseHerbs(h,true) && h.originStory.declinedHerbalOffer && !StoryOrigin.ChooseHerbs(h,false),"later accept keeps history; no opt-out rewrite");
        });
        Run("N3 EXP and idle time do not confirm a choice or create M1",()=>{
            foreach(var p in new[]{HerbalParticipation.Offered,HerbalParticipation.Declined})
            {
                var h=N3Hero(participation:p);string before=N3Facts(h);
                for(int i=0;i<30;i++)
                {
                    Time.time+=780;Time.frameCount++;h.AddExp(100);
                    StoryOrigin.Journal(h);StoryOrigin.Hint(h);HanVuEncounters.Available(h);
                }
                Check(h.currentLevel>1 && N3Facts(h)==before && !M1FromExistingFacts(h),"EXP/time remain separate");
            }
        });
        Run("N3 actual cultivation binding refines Qi without choosing or advancing the medicine quest",()=>{
            var d=new CultivationPlaytestDefinition {
                schemaVersion=1,version=1,id="playtest:hanvu-n3-qi",title="Owned data fixture",
                startingRealm="realm:luyen-khi",maximumFrameSeconds=.1,stepSeconds=.1,fallbackDensity=1,
                absorptionRate=1,absorptionEfficiency=1,refinementRate=1,refinementEfficiency=1,maximumEnergy=100,
                realms=new[]{new CultivationPlaytestRealm{id="realm:luyen-khi",name="Luyện Khí",tier=0,thresholds=new double[]{0,300,600,1000},successor=""}},
                qiSources=new[]{new CultivationQiSource{nodeId=StoryOrigin.HanVu.Village,density=1}}
            };
            Check(CultivationPlaytestProfile.TryCreate(d,id=>{WorldMapNode n;return WorldMapCatalog.TryGet(id,out n);},out var p,out var why),"fixture profile: "+why);
            foreach(var participation in new[]{HerbalParticipation.Offered,HerbalParticipation.Declined,HerbalParticipation.Accepted})
            {
                var h=N3Hero(participation:participation);string before=N3Facts(h);
                using(var b=new CultivationGameplayBinding(h,p,CultivationGameplayState.New(p)))
                {
                    for(int frame=0;frame<100;frame++)Check(b.Advance(h,frame,.1,false,h.worldMapId)==CultivationReason.None,"actual active Qi tick");
                    Check(b.Current.Foundation.Amount>0 && h.cultivationJson.Length>0 && N3Facts(h)==before && !M1FromExistingFacts(h),"Qi publishes only its own component");
                }
            }
        });
    }

    sealed class N3View
    {
        internal readonly GameManager Game;
        internal readonly UIManager UI;
        internal N3View(EntityDataSO hero)
        {
            Game=new GameObject("origin host fixture").AddComponent<GameManager>();Game.BindFixture(hero);
            UI=new GameObject("origin UI fixture").AddComponent<UIManager>();UI.InitializeFixture(Game);
        }
        internal void LastLine(int next) {for(int i=0;i<next;i++)UI.PressFixture("HanVuNext");}
    }
    static void N3InteractionTests()
    {
        Run("N3 actual dialogue controls: close is neutral and replay does not take the quest",()=>{
            var h=N3Hero();var f=new N3View(h);string before=N3Facts(h);
            f.UI.PressFixture("HanVuTalk");f.UI.PressFixture("HanVuDismiss");f.UI.PressFixture("HanVuAccept");
            Check(N3Facts(h)==before && f.Game.CommitObservations==0,"closed callback has no choice");
            f.UI.PressFixture("HanVuJournal");f.UI.PressFixture("HanVuReplay");
            Check(N3Facts(h)==before,"replay only opens dialogue");
            f.LastLine(5);f.UI.PressFixture("HanVuDecline");
            Check(h.originStory.herbalParticipation==HerbalParticipation.Declined,"explicit decline");
            f.UI.PressFixture("HanVuTalk");f.LastLine(5);f.UI.PressFixture("HanVuAccept");f.UI.PressFixture("HanVuAccept");
            Check(h.originStory.herbalParticipation==HerbalParticipation.Accepted && h.originStory.declinedHerbalOffer &&
                f.Game.CommitObservations==2,"reversible decline and one accept");
        });
        Run("N3 real UI records only displayed opening and woodcutter testimony",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuTalk");f.LastLine(3);f.UI.TickFixture();
            Check(!h.originStory.heardOpeningTrip,"line before testimony");f.UI.PressFixture("HanVuDismiss");
            f.UI.PressFixture("HanVuTalk");f.LastLine(4);f.UI.TickFixture();f.UI.TickFixture();
            Check(h.originStory.heardOpeningTrip && f.Game.CommitObservations==1,"one displayed trip fact");
            h.worldMapId=StoryOrigin.HanVu.Forest;f.Game.RespawnFixture();f.UI.TickFixture();
            f.UI.PressFixture("HanVuWoodcutter");f.UI.TickFixture();f.UI.PressFixture("HanVuDismiss");
            Check(!h.originStory.heardWoodcutter,"closed at question");
            f.UI.PressFixture("HanVuWoodcutter");f.LastLine(1);f.UI.TickFixture();f.UI.TickFixture();
            Check(h.originStory.heardWoodcutter && f.Game.CommitObservations==2,"one displayed answer");
        });
        Run("REGRESSION old opening callback after Hero replacement cannot accept for the new Hero before UI tick",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuTalk");f.LastLine(5);
            var loaded=N3Hero();f.Game.BindFixture(loaded);string before=N3Facts(loaded);
            f.UI.PressFixture("HanVuAccept");
            Check(N3Facts(loaded)==before && f.Game.CommitObservations==0,"stale choice changed replacement Hero");
        });
        Run("REGRESSION old opening callback after same-Hero respawn cannot accept before UI tick",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuTalk");f.LastLine(5);
            f.Game.RespawnFixture();string before=N3Facts(h);f.UI.PressFixture("HanVuAccept");
            Check(N3Facts(h)==before && f.Game.CommitObservations==0,"stale choice survived respawn");
        });
        Run("REGRESSION old opening callback after travel round trip cannot accept in reused village context",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuTalk");f.LastLine(5);
            h.worldMapId=StoryOrigin.HanVu.Forest;f.Game.RespawnFixture();
            h.worldMapId=StoryOrigin.HanVu.Village;f.Game.RespawnFixture();
            string before=N3Facts(h);f.UI.PressFixture("HanVuAccept");
            Check(N3Facts(h)==before && f.Game.CommitObservations==0,"map equality alone admitted stale choice");
        });
        Run("REGRESSION stale medicine callback cannot trigger the new Hero forest action",()=>{
            var h=N3Hero(OriginStage.Return,participation:HerbalParticipation.Accepted);var f=new N3View(h);
            f.UI.PressFixture("HanVuTalk");f.LastLine(2);
            var loaded=N3Hero(OriginStage.SearchForest,StoryOrigin.HanVu.Forest,HerbalParticipation.Accepted);
            f.Game.BindFixture(loaded);string before=N3Facts(loaded);f.UI.PressFixture("HanVuAccept");
            Check(N3Facts(loaded)==before && f.Game.CommitObservations==0,"old medicine intent became forest interaction");
        });
        Run("REGRESSION stale encounter callback cannot choose the new Hero pending encounter",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuEncounter");
            var loaded=N3Hero();HanVuEncounters.Observe(loaded,HanVuEncounters.Available(loaded).Id);
            f.Game.BindFixture(loaded);string before=N3Facts(loaded);int commits=f.Game.CommitObservations;
            f.UI.PressFixture("HanVuAccept");
            Check(N3Facts(loaded)==before && f.Game.CommitObservations==commits,"stale encounter changed replacement Hero");
        });
        Run("REGRESSION missed hidden frame after respawn cannot display and credit stale testimony",()=>{
            var h=N3Hero(map:StoryOrigin.HanVu.Forest);var f=new N3View(h);
            f.UI.PressFixture("HanVuWoodcutter");f.LastLine(1);
            f.Game.RespawnFixture();f.UI.TickFixture();
            Check(!h.originStory.heardWoodcutter && f.Game.CommitObservations==0,"stale answer credited in new actor life");
        });
        Run("N3 current medicine action resolves once and fresh forest action still works",()=>{
            var h=N3Hero(OriginStage.Return,participation:HerbalParticipation.Accepted);var f=new N3View(h);
            f.UI.PressFixture("HanVuTalk");f.LastLine(2);f.UI.PressFixture("HanVuAccept");
            Check(h.originStory.stage==OriginStage.SearchForest && f.Game.CommitObservations==1,"valid medicine action");
            f.UI.PressFixture("HanVuAccept");Check(f.Game.CommitObservations==1,"duplicate did not repeat");
            h.worldMapId=StoryOrigin.HanVu.Forest;f.Game.RespawnFixture();f.UI.TickFixture();f.UI.PressFixture("HanVuTalk");
            Check(h.originStory.stage==OriginStage.SearchElite && M1FromExistingFacts(h) && f.Game.CommitObservations==2,"fresh interaction after travel");
        });
        Run("N3 UI idle frames and unavailable host never confirm a pending choice",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuTalk");f.LastLine(5);
            for(int i=0;i<30;i++){Time.time+=780;Time.frameCount++;f.UI.TickFixture();}
            Check(h.originStory.herbalParticipation==HerbalParticipation.Offered && !M1FromExistingFacts(h),"no timeout choice");
            f.Game.IsGameplayHUDVisible=false;f.UI.PressFixture("HanVuAccept");
            Check(h.originStory.herbalParticipation==HerbalParticipation.Offered,"hidden host cannot choose");
        });
        Run("N3 context rejects default wrong owner story replacement and trial liability",()=>{
            var h=N3Hero();var f=new N3View(h);
            Check(!f.Game.IsCurrentOriginInteraction(default) &&
                !f.Game.IsCurrentOriginInteraction(f.Game.CaptureOriginInteraction(N3Hero())),"uncaptured/wrong owner");
            var captured=f.Game.CaptureOriginInteraction(h);Check(f.Game.IsCurrentOriginInteraction(captured),"live owner context");
            f.Game.HasTribulationLiability=true;Check(!f.Game.IsCurrentOriginInteraction(captured),"trial guard");f.Game.HasTribulationLiability=false;
            h.originStory=N3Hero().originStory;
            Check(!f.Game.IsCurrentOriginInteraction(captured),"same Hero with replaced story cannot accept an old intent");
            Check(f.Game.IsCurrentOriginInteraction(f.Game.CaptureOriginInteraction(h)),"fresh context remains usable");
        });
        Run("REGRESSION retained old choice delegate cannot consume a replacement Hero fresh conversation",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuTalk");f.LastLine(5);
            var old=f.UI.ActionFixture("HanVuAccept");var loaded=N3Hero();f.Game.BindFixture(loaded);f.UI.TickFixture();
            f.UI.PressFixture("HanVuTalk");f.LastLine(5);old();
            Check(loaded.originStory.herbalParticipation==HerbalParticipation.Offered && f.Game.CommitObservations==0,"old delegate consumed a new conversation intent");
            f.UI.PressFixture("HanVuAccept");Check(loaded.originStory.herbalParticipation==HerbalParticipation.Accepted,"fresh intent still works");
        });
        Run("REGRESSION retained old choice delegate is retired even after close and reopen in the same life",()=>{
            var h=N3Hero();var f=new N3View(h);f.UI.PressFixture("HanVuTalk");f.LastLine(5);
            var old=f.UI.ActionFixture("HanVuAccept");f.UI.PressFixture("HanVuDismiss");
            f.UI.PressFixture("HanVuTalk");f.LastLine(5);old();
            Check(h.originStory.herbalParticipation==HerbalParticipation.Offered && f.Game.CommitObservations==0,"closed intent was reusable against a new view");
            f.UI.PressFixture("HanVuAccept");Check(h.originStory.herbalParticipation==HerbalParticipation.Accepted,"fresh view still works");
        });
    }
}
