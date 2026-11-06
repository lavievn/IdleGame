#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using TMPro;
using TuTienCore;
using UnityEngine;
public sealed partial class MainGameFoundationProbe
{
    IEnumerator CultivationV11B()
    {
        phase=1;Require(gm.Cultivation.Profile.Version==2,"V2 New canonical Hero");
        if(!string.IsNullOrEmpty(replayProfile)){
            var slot=(SaveSlot)Enum.Parse(typeof(SaveSlot),replaySlotName);var raw=ScriptableObject.CreateInstance<EntityDataSO>();JsonUtility.FromJsonOverwrite(File.ReadAllText(Path.Combine(SavePath,replaySlotName+".json")),raw);string replayExpected=raw.cultivationJson;Destroy(raw);
            Require(gm.LoadAndContinue(slot),"Fresh-process V2 Load");yield return Ready();Require(gm.HeroData.cultivationJson==replayExpected&&gm.Cultivation.Profile.Version==2,"Exact saved state, no offline grant");string paused=gm.HeroData.cultivationJson;yield return new WaitForSecondsRealtime(.4f);Require(gm.HeroData.cultivationJson==paused,"Restart pause");Pass("V11B separate Unity process retains exact V2 owner/state/clock and immutable migration metadata; no offline grant.");Status(1,"PASS");yield break;
        }
        WorldMapNode start;Require(WorldMapCatalog.TryGet(gm.HeroData.worldMapId,out start)&&start.IsStart&&gm.Cultivation.Profile.EnvironmentalQiPerSecond(start.Id)==1&&gm.Cultivation.Current.Foundation.Amount==0,"New actual start baseline");
        CultivationPlaytestProfile p1;WorldQiDistribution table;CultivationConfigurationReason code;string why;Require(CultivationProductionConfiguration.TryLoadVersion(1,out p1,out table,out code,out why),"Historical V1 config");
        string changed=null;foreach(var n in WorldMapCatalog.All)if(!n.IsBoss&&p1.EnvironmentalQiPerSecond(n.Id)!=gm.Cultivation.Profile.EnvironmentalQiPerSecond(n.Id)){changed=n.Id;break;}Require(changed!=null,"Different-rate fixture node");
        var fixture=Instantiate(gm.HeroData);fixture.worldMapId=changed;WorldMapCatalog.Normalize(fixture);
        var state=new CultivationSession(Guid.NewGuid(),Guid.NewGuid(),p1.Catalog,p1.Rules,new CultivationSnapshot(new CultivationFoundation(p1.StartingRealm,1234.5),new UnrefinedQiReserve(1000),new SpiritualEnergy(37.5),new BloodEssenceState(0,0),new FoundationInjuryState(.125)));
        var fb=new CultivationGameplayBinding(fixture,p1,state);Require(fb.Advance(fixture,0,.037,false,changed)==CultivationReason.None,"Old fractional fixture");string snapshot=fixture.cultivationJson;
        Require(saves.SaveGame(fixture,SaveSlot.ManualSave1)&&saves.SaveGame(fixture,SaveSlot.ManualSave2),"Owned V1 snapshots");fb.Dispose();Destroy(fixture);
        string file=Path.Combine(SavePath,"ManualSave1.json"),original=Convert.ToBase64String(File.ReadAllBytes(file)),other=Convert.ToBase64String(File.ReadAllBytes(Path.Combine(SavePath,"ManualSave2.json")));
        Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"V1 Load remains supported");yield return Ready();Require(gm.Cultivation.Profile.Version==1&&gm.HeroData.cultivationJson==snapshot,"Loaded V1 keeps original rules, no silent retag");
        var old=gm.Cultivation;var canonical=gm.HeroData;ui.OpenHeroStats();yield return null;ui.RefreshCharacterProgression();Press("CultivationConfigurationUpgrade");Require(gm.IsCultivationMigrationPending,"Character upgrade action opens existing consent");
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)}){
            yield return Resize(size.x,size.y);foreach(var t in gm.confirmationPopup.GetComponentsInChildren<TextMeshProUGUI>(true)){t.ForceMeshUpdate();Require(!t.isTextOverflowing,"Config consent fits "+size.x+": "+t.text);}
            foreach(var b in gm.confirmationPopup.GetComponentsInChildren<CustomInteractable>(true)){var r=b.GetRect();var point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));Require(ui.HitControl(point)==b,"Modal owns hit-test");}yield return Capture("v11-config-consent-"+size.x);
        }
        gm.OnCancelResetClicked();Require(!gm.IsCultivationMigrationPending&&gm.HeroData==canonical&&gm.Cultivation==old&&gm.HeroData.cultivationJson==snapshot&&Convert.ToBase64String(File.ReadAllBytes(file))==original,"Cancel unchanged canonical and file");
        Require(gm.RequestSelectedCultivationConfigurationUpgrade(),"Reopen explicit selected snapshot");saves.FailNextCultivationMigration="BeforeReplace";gm.OnConfirmResetClicked();Require(gm.HeroData==canonical&&gm.Cultivation==old&&Convert.ToBase64String(File.ReadAllBytes(file))==original,"Accepted failed write preserves canonical");
        Require(gm.RequestSelectedCultivationConfigurationUpgrade(),"Retry explicit consent");gm.OnConfirmResetClicked();yield return Ready();Require(old.IsDisposed&&gm.Cultivation.Profile.Version==2&&gm.Cultivation.OwnerId==state.OwnerId&&gm.Cultivation.Current.Foundation.Amount==1234.5&&gm.Cultivation.Current.Reserve.Amount==1000&&gm.Cultivation.PendingSeconds==.037,"Committed candidate replaces canonical only after verify");
        Require(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(SavePath,"ManualSave2.json")))==other&&Directory.GetFiles(SavePath,"ManualSave1.cultivation-*.snapshot.bak").Length==1,"Only selected slot upgraded, immutable backup retained");
        Pass("V11B real canonical V1 Load stays V1; Character action, modal4 sizes/hit-test, cancel and failed-write preserve state; accepted V2 upgrade preserves owner/resources/.037old-source checkpoint. Fixture state/node explicitly injected, not naturally earned.");Status(1,"PASS");
        phase=2;var b2=gm.Cultivation;int level=gm.HeroData.currentLevel;float exp=gm.HeroData.currentExp;Call(gm,"CancelNextWave");combat.ForceClearAllMonsters();gm.activeMonsters.Clear();double elapsed=b2.ActiveSeconds;
        ui.CloseDamageInfo();ui.SetPaused(false);yield return Until(()=>b2.ActiveSeconds>=elapsed+.5,10,"Real V2 clock");ui.SetPaused(true);
        double expected=2234.5+p1.EnvironmentalQiPerSecond(changed)*.037+b2.Profile.EnvironmentalQiPerSecond(changed)*(b2.ProcessedSeconds-.037);
        Require(Math.Abs(b2.Current.Foundation.Amount+b2.Current.Reserve.Amount-expected)<1e-7&&gm.HeroData.currentLevel==level&&gm.HeroData.currentExp==exp,"One canonical clock, no duplicate source/Level grant");
        Require(gm.TryManualSave(SaveSlot.ManualSave1),"V2 Save");string current=gm.HeroData.cultivationJson;Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"V2 Load");yield return Ready();Require(gm.HeroData.cultivationJson==current&&gm.Cultivation.Epoch!=b2.Epoch,"Fresh binding exact checkpoint");
        Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Manual old snapshot rollback");yield return Ready();Require(gm.Cultivation.Profile.Version==1&&gm.HeroData.cultivationJson==snapshot,"Manual rollback selects V1 snapshot, not silent upgrade");Require(gm.LoadAndContinue(SaveSlot.ManualSave1),"Return V2");yield return Ready();
        Pass("V11B actual Update clock conserves Qi across pending V1 source and V2 steps, no EXP grant; exact Save/repeated Load/fresh epoch and intentional manual V1 rollback PASS.");Status(2,"PASS");
        phase=3;ui.OpenHeroStats();foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)}){yield return Resize(size.x,size.y);ui.RefreshCharacterProgression();var t=(TextMeshProUGUI)Get(ui,"progressionSummary");t.ForceMeshUpdate();Require(!t.isTextOverflowing,"V2 Character fit "+size.x);yield return Capture("v11-v2-character-"+size.x);}ui.CloseDamageInfo();Pass("V11B Character Qi source/refinement independently displayed at4 native GameView sizes; no overflow.");Status(3,"PASS");
        phase=4;var retired=gm.Cultivation;string checkpoint=gm.HeroData.cultivationJson;Call(combat,"ApplyIncomingDamage",1000000,"[V11 fixture defeat]",null,null);yield return null;gm.OnRetryClicked();yield return Ready();Require(retired.IsDisposed&&gm.HeroData.cultivationJson==checkpoint&&gm.Cultivation.Profile.Version==2&&combat.CurrentHeroCombatState.Data==gm.HeroData&&Get(space,"cameraTarget")==hero,"V2 Retry lifecycle");Pass("V11B Retry canonical/combat/camera identity and exact V2 checkpoint PASS. Native mouse NOT claimed.");Status(4,"PASS");
    }
}
#endif
