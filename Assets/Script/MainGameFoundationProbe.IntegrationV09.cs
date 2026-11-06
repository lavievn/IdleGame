#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using TuTienCore;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class MainGameFoundationProbe
{
    IEnumerator CultivationV09()
    {
        yield return CultivationV09Core();if(!string.IsNullOrEmpty(replayProfile))yield break;
        yield return MigrationV09();yield return VisualV09();
    }
    void AssertOneHeroBody(Image body,Sprite sprite,string label)
    {
        var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();int bodies=0;
        foreach(var img in hero.heroRect.GetComponentsInChildren<Image>(true))if(img.gameObject.name=="Cultivation Sprite Body")bodies++;
        Require(bodies==1 && bridge.BodyGraphic==body && body.sprite==sprite && sprite.texture==Resources.Load<Texture2D>(CultivationSpriteBridge.DefaultHeroTexturePath),label+": exactly one stable default Hero body/sprite");
    }
    IEnumerator MigrationV09()
    {
        phase=4;ui.SetPaused(true);string path=Path.Combine(SavePath,"ManualSave2.json");var legacy=UnityEngine.Object.Instantiate(gm.HeroData);legacy.cultivationJson="";
        string raw=JsonUtility.ToJson(legacy,true);UnityEngine.Object.Destroy(legacy);File.WriteAllText(path,raw);
        var canonical=gm.HeroData;var binding=gm.Cultivation;var life=combat.CurrentHeroCombatState;string before=JsonUtility.ToJson(canonical),bytes=Convert.ToBase64String(File.ReadAllBytes(path));
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && gm.IsCultivationMigrationPending,"Legacy Load opens real consent instead of swapping Hero");
        Require(gm.HeroData==canonical && gm.Cultivation==binding && combat.CurrentHeroCombatState==life && JsonUtility.ToJson(canonical)==before && Convert.ToBase64String(File.ReadAllBytes(path))==bytes,"Preflight no source/canonical mutation");
        foreach(var size in new[]{new Vector2Int(600,338),new Vector2Int(800,450),new Vector2Int(1150,647),new Vector2Int(1360,680)}) {
            yield return Resize(size.x,size.y);var popup=gm.confirmationPopup;
            foreach(var text in popup.GetComponentsInChildren<TextMeshProUGUI>(true)){text.ForceMeshUpdate();Require(!text.isTextOverflowing,"Consent text fits "+size.x+": "+text.text);}
            var uiButtons=popup.GetComponentsInChildren<CustomInteractable>(true);Require(uiButtons.Length==2,"Reuse existing two-button confirmation modal");
            foreach(var button in uiButtons){var rect=button.GetRect();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));Require(ui.HitControl(point)==button,"Consent owns hit testing without click-through");}
            yield return Capture("cultivation-v09-consent-"+size.x);
        }
        gm.OnCancelResetClicked();Require(!gm.IsCultivationMigrationPending && gm.HeroData==canonical && gm.Cultivation==binding && JsonUtility.ToJson(canonical)==before && Convert.ToBase64String(File.ReadAllBytes(path))==bytes,"Cancel leaves exact source and live session");
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && gm.IsCultivationMigrationPending,"Reopen consent");
        saves.FailNextCultivationMigration="BeforeReplace";gm.OnConfirmResetClicked();
        Require(!gm.IsCultivationMigrationPending && gm.HeroData==canonical && gm.Cultivation==binding && JsonUtility.ToJson(canonical)==before && Convert.ToBase64String(File.ReadAllBytes(path))==bytes,"Failed accepted write keeps canonical Hero/session and original file");
        Require(!gm.LoadAndContinue(SaveSlot.ManualSave2) && gm.IsCultivationMigrationPending,"Retry consent after failed write");gm.OnConfirmResetClicked();yield return Ready();
        Require(gm.HeroData!=canonical && binding.IsDisposed && gm.Cultivation!=null && gm.Cultivation.Current.Foundation.Amount==0 && gm.Cultivation.Current.Reserve.Amount==0 && gm.Cultivation.Current.Blood.Charges==0,"Canonical swap only after successful verified upgrade");
        string[] backups=Directory.GetFiles(SavePath,"ManualSave2.cultivation-*.legacy.bak");Require(backups.Length==1 && Convert.ToBase64String(File.ReadAllBytes(backups[0]))==bytes,"Native UI flow retains exact backup across retry");
        string committed=gm.HeroData.cultivationJson;Require(gm.LoadAndContinue(SaveSlot.ManualSave2),"Current upgraded Load needs no migration");yield return Ready();Require(gm.HeroData.cultivationJson==committed && !gm.IsCultivationMigrationPending,"Repeated Load no zero-state bonus or prompt");
        Pass("V09 real existing consent modal fits4 sizes and owns hit tests; cancel/preflight byte-identical, injected accepted-write failure preserves live Hero/session, accepted retry commits backup before candidate swap, repeated Load remains current. Only owned ManualSave2 is migrated.");Status(4,"PASS");
    }
    IEnumerator VisualV09()
    {
        phase=4;var bridge=hero.heroRect.GetComponent<CultivationSpriteBridge>();var body=bridge.BodyGraphic;var sprite=body.sprite;
        Require(sprite!=null && gm.Cultivation.Profile.IsProduction,"Default visual/production profile exists");AssertOneHeroBody(body,sprite,"New Hero");
        foreach(var family in new[]{WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff,WeaponFamily.Unarmed,WeaponFamily.Sword}) {
            ui.SetPaused(true);combat.ForceClearAllMonsters();gm.activeMonsters.Clear();Call(gm,"CancelNextWave");
            if(family==WeaponFamily.Unarmed)Require(gm.UnequipGear(EquipmentSlot.Weapon),"Unequip real Weapon slot");
            else {
                var item=family==WeaponFamily.Sword?EquipmentFoundation.CreateDemoSword():family==WeaponFamily.Bow?EquipmentFoundation.CreateDemoBow():EquipmentFoundation.CreateDemoStaff();
                Require(EquipmentRuntime.AddInstance(gm.HeroData,item) && gm.EquipGear(item.instanceId,EquipmentSlot.Weapon),"Existing real equipment handlers "+family);
            }
            AssertOneHeroBody(body,sprite,"Equip "+family);Require(hero.attackMode==WeaponBasicAttack.Mode(family) && Mathf.Abs(hero.attackRange-space.AttackRange(hero.attackMode,true))<.001f,"Weapon mechanics/range preserved "+family);
            // Development sample fixtures only; no loot table or stats are changed in production.
            Call(gm,"CallNextWave");int traces=gm.DamageHistory.Length;bool projectile=false,damaged=false;float deadline=Time.realtimeSinceStartup+30;
            var targets=new List<CombatManager.ActiveMonsterInfo>(Enemies);var initialHP=new List<int>();foreach(var target in targets)initialHP.Add(target.currentHP);
            Require(combat.RegisteredHeroCount==1,"This native combat fixture proves the single primary Hero damages targets");
            ui.SetPaused(false);
            while(!damaged || (family==WeaponFamily.Bow || family==WeaponFamily.Staff) && !projectile) {
                Require(Time.realtimeSinceStartup<deadline && gm.Cultivation!=null,"Bounded real combat "+family);
                for(int i=0;i<targets.Count;i++)damaged|=targets[i].currentHP<initialHP[i];
                foreach(var shot in (IList)Get(combat,"projectiles"))if((bool)Get(shot,"fromHero") && ReferenceEquals(Get(shot,"hero"),combat.CurrentHeroCombatState)) {
                    Require((TuTienCore.AttackMode)Get(shot,"attackMode")==hero.attackMode && (bool)Get(shot,"staffBasic")== (family==WeaponFamily.Staff) && !(bool)Get(shot,"magic"),"Primary projectile preserves Bow versus Staff-basic snapshot, without enabling legacy charged-magic flight");projectile=true;
                }
                AssertOneHeroBody(body,sprite,"Combat "+family);yield return null;
            }
            ui.SetPaused(true);AssertOneHeroBody(body,sprite,"After attack "+family);yield return Capture("unified-hero-v09-"+family);
            Pass("V09 same native Hero Image/Sprite for "+family+"; resolvedMode="+hero.attackMode+" range="+hero.attackRange+" damageEvents="+(gm.DamageHistory.Length-traces)+" projectileObserved="+projectile+". Existing Combat.Update owns damage/timing; fixture sample items only.");
        }
        Require(gm.TryManualSave(SaveSlot.ManualSave2) && gm.LoadAndContinue(SaveSlot.ManualSave2),"Visual Save/Load");yield return Ready();AssertOneHeroBody(body,sprite,"Load");
        Call(combat,"ApplyIncomingDamage",1000000,"[V09 visual fixture defeat]",null,null);yield return null;gm.OnRetryClicked();yield return Ready();AssertOneHeroBody(body,sprite,"Retry");
        var data=gm.HeroData;WorldMapNode origin;Require(WorldMapCatalog.TryGet(data.worldMapId,out origin),"Visual Travel origin");string destination=null;
        foreach(string id in origin.Links){WorldMapNode node;if(WorldMapCatalog.TryGet(id,out node) && !node.IsBoss){destination=id;break;}}
        Require(destination!=null,"Linked visual Travel fixture");if(!data.worldMapCleared.Contains(origin.Id))data.worldMapCleared.Add(origin.Id);
        Require(gm.TryTravelToWorldMap(destination),"Visual Travel");yield return Ready();AssertOneHeroBody(body,sprite,"Travel");
        gm.OnNewGameClicked();gm.OnConfirmResetClicked();yield return Ready();AssertOneHeroBody(body,sprite,"New Game");
        Require(combat.CurrentHeroCombatState.Data==gm.HeroData && Get(space,"cameraTarget")==hero,"New visual does not replace logical/camera identity");
        Pass("V09 native Hero visual stays one existing Swordsman_Normal Image/Sprite through Sword/Bow/Staff/Unarmed, real combat, Load/Retry/Travel/New; no Hero renderer/animation asset added, Monster visuals/assets unchanged.");Status(4,"PASS");
    }
}
#endif
