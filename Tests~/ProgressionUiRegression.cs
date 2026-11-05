using System;
using UnityEngine;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static UIManager ProgressionUi(out Field f,out GameManager gm)
    {
        f=WeaponField(out gm);
        var ui=new GameObject().AddComponent<UIManager>();UIManager.Instance=ui;Call(ui,"Start");
        return ui;
    }
    static void ProgressionUiTests()
    {
        Run("progression DEVB visibility reads live configuration without mutating Hero",()=>{
            Field f;GameManager gm;var ui=ProgressionUi(out f,out gm);
            var h=gm.HeroData;string before=JsonUtility.ToJson(h,false);
            ui.OpenHeroStats();Check(Get<TextMeshProUGUI>(ui,"progressionSummary").text.Contains("Baseline Hero"),"baseline label");
            var dev=DevBalanceProfile.Defaults(false,f.space,150);dev.attack=1000;CombatBalance.SetDevProfile(false,dev);
            ui.RefreshCharacterProgression();Check(Get<TextMeshProUGUI>(ui,"progressionSummary").text.Contains("DEVB Hero đang override"),"live override label");
            ui.IsolatedValidationProfile=true;ui.RefreshCharacterProgression();Check(Get<TextMeshProUGUI>(ui,"progressionSummary").text.Contains("THỬ NGHIỆM CÔ LẬP"),"explicit verifier context");
            Check(JsonUtility.ToJson(h,false)==before,"visibility never changes Hero/save data");
            CombatBalance.ResetDevProfiles();ui.RefreshCharacterProgression();Check(ui.CharacterSourceStatus.Contains("Baseline Hero"),"live baseline after runtime fixture reset");
        });
        Run("progression EXP points allocation keeps HP ratio attack clock and authority",()=>{
            Field f;GameManager gm;var ui=ProgressionUi(out f,out gm);var h=gm.HeroData;
            h.AddExp(CombatBalance.RequiredExp(1));Check(h.currentLevel==2 && h.statPoints==1,"one point per level");
            f.combat.RefreshRegisteredHeroStats(f.hero,false);var state=f.combat.CurrentHeroCombatState;
            state.CurrentHP=state.MaxHP/2;state.AttackTimer=.42f;int life=state.LifeVersion,oldMax=state.MaxHP,oldHP=state.CurrentHP;
            ui.OpenHeroStats();ui.SetPaused(true);
            Get<CustomInteractable>(ui,"allocateHealthInput").onClickEvent.Invoke();
            Check(h.statPoints==0 && h.addedHealth==6 && h.isDirty && state.MaxHP==oldMax+6,"canonical API +6 and dirty");
            Check(state.CurrentHP==Mathf.RoundToInt((float)oldHP/oldMax*state.MaxHP) && state.CurrentHP<state.MaxHP,"existing ratio rule, no full heal");
            Near(state.AttackTimer,.42f,"no clock reset");Check(state.LifeVersion==life,"no new life");
            string after=JsonUtility.ToJson(h,false);Get<CustomInteractable>(ui,"allocateHealthInput").onClickEvent.Invoke();
            Check(JsonUtility.ToJson(h,false)==after && !gm.AllocateHeroPhysicalAttack(),"repeat with zero points has no mutation");
            Check(!Get<CustomInteractable>(ui,"allocateHealthInput").enabled,"disabled hit target at zero");
            string summary=Get<TextMeshProUGUI>(ui,"progressionSummary").text;
            Check(summary.Contains("HP: "+state.CurrentHP+"/"+state.MaxHP) && summary.Contains("Physical Attack: "+WeaponBasicAttack.Physical(h)) && summary.Contains("EXP: "+h.currentExp+"/"+h.expToNextLevel),"essential progression values remain visible outside paged details");
            h.AddExp(h.expToNextLevel);f.combat.RefreshRegisteredHeroStats(f.hero,false);int before=WeaponBasicAttack.Physical(h);ui.SetPaused(false);ui.RefreshCharacterProgression();
            Get<CustomInteractable>(ui,"allocatePhysicalInput").onClickEvent.Invoke();
            Check(h.addedDamage==1 && h.statPoints==0 && WeaponBasicAttack.Physical(h)==before+1 && WeaponBasicAttack.Magical(h)==0,"one physical point, no magic allocation");
            var defender=HeroAt(1);Check(PrototypeDamage(h,defender,AttackMode.Melee)>PrototypeDamage(defender,defender,AttackMode.Melee),"real damage evaluator consumes allocated physical source");
            var sword=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,4);EquipmentRuntime.AddInstance(h,sword);
            Check(gm.EquipGear(sword.instanceId,EquipmentSlot.Weapon) && WeaponBasicAttack.Physical(h)==before+5,"gear adds once on allocation");
            Check(gm.UnequipGear(EquipmentSlot.Weapon) && WeaponBasicAttack.Physical(h)==before+1 && h.addedHealth==6 && h.addedDamage==1,"unequip preserves allocation");
        });
        Run("progression rejects stale dead Hero and rebinds after new run",()=>{
            Field f;GameManager gm;var ui=ProgressionUi(out f,out gm);var old=gm.HeroData;old.statPoints=2;
            var next=HeroAt(1);next.statPoints=1;Set(gm,"runtimeHeroData",next);
            Check(!gm.AllocateHeroHealth() && old.statPoints==2 && next.statPoints==1,"stale combat binding rejected");
            f.combat.SetupHeroInfo(next);Check(gm.AllocateHeroPhysicalAttack() && next.addedDamage==1 && old.addedDamage==0,"new canonical Hero only");
            next.statPoints=1;f.combat.CurrentHeroCombatState.CurrentHP=0;
            Check(!gm.AllocateHeroHealth() && next.addedHealth==0,"cannot revive by allocation");
            gm.heroDataSO=HeroAt(1);typeof(GameManager).GetMethod("InitHeroData",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(gm,new object[]{false});ui.OpenHeroStats();ui.RefreshCharacterProgression();
            Check(gm.HeroData.statPoints==0 && gm.HeroData.addedHealth==0 && gm.HeroData.addedDamage==0,"new run resets allocations");
            Check(!Get<CustomInteractable>(ui,"allocatePhysicalInput").enabled,"new run cannot mutate retired Hero");
            ui.OpenDamageInfo();Check(!Get<RectTransform>(ui,"allocatePhysicalButton").gameObject.activeSelf,"allocation hidden on combat log");
        });
        Run("progression realm display follows existing boundary mapping without mutation",()=>{
            Field f;GameManager gm;var ui=ProgressionUi(out f,out gm);var h=gm.HeroData;
            int[] levels={1,10,11,100,101};string[] names={"Luyện Khí","Luyện Khí","Trúc Cơ","Chân Tiên","Tiên Đế"};
            ui.OpenHeroStats();for(int i=0;i<levels.Length;i++) {
                h.currentLevel=levels[i];string before=JsonUtility.ToJson(h,false);ui.RefreshCharacterProgression();
                string label=Get<TextMeshProUGUI>(ui,"progressionSummary").text;
                Check(label.Contains("legacy/prototype") && label.Contains(names[i]),"level "+levels[i]+" label is explicitly legacy and derives from existing rank function");
                Check(JsonUtility.ToJson(h,false)==before,"presentation grants no buff or persisted realm state");
            }
        });
    }
}
