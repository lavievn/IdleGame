using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using TuTienCore;

partial class MotionRegression
{
    static InventoryPanelUI ComparisonPanel(out GameManager gm,out EntityDataSO hero,out Field field)
    {
        field=new Field();
        gm=new GameObject().AddComponent<GameManager>();
        hero=HeroAt(1);
        EquipmentInventory.InitializeNewRun(hero);EquipmentRuntime.InitializeNewRun(hero);
        Set(gm,"runtimeHeroData",hero);Set(gm,"hasDeployed",true);
        Set(gm,"heroController",field.hero);Set(gm,"combatManager",field.combat);
        field.combat.SetupHeroInfo(hero);
        var ui=new GameObject().AddComponent<UIManager>();
        var hud=(RectTransform)new GameObject(true).transform;
        var panel=InventoryPanelUI.Install(ui,hud);
        panel.Open();return panel;
    }
    static EquipmentStatComparison Compare(EntityDataSO hero,string key,bool right=false)
    {
        object[] args={hero,key,right,null,null};
        bool ok=(bool)typeof(InventoryPanelUI).GetMethod("TryComparison",
            BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
        Check(ok,"comparison allowed: "+args[4]);return (EquipmentStatComparison)args[3];
    }
    static string CompareFailure(EntityDataSO hero,string key,bool right=false)
    {
        object[] args={hero,key,right,null,null};
        Check(!(bool)typeof(InventoryPanelUI).GetMethod("TryComparison",
            BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args),"comparison rejected");
        Check(args[3]==null,"no false numeric result");return (string)args[4];
    }
    static void SelectComparison(InventoryPanelUI panel,string key)
    {Set(panel,"selectedKey",key);panel.Refresh();}
    static string CompareText(InventoryPanelUI panel) {return Get<TextMeshProUGUI>(panel,"detail").text;}
    static void InventoryComparisonTests()
    {
        Run("W3.1b.1 charm HP/ATK replacement and removal match actual totals without preview HP mutation",()=>{
            GameManager gm;EntityDataSO hero;Field f;var panel=ComparisonPanel(out gm,out hero,out f);
            f.combat.CurrentHeroCombatState.CurrentHP=37;hero.isDirty=false;
            var mods=hero.combatStatModifiers;int count=mods.Count;
            var hp=Compare(hero,"stack:"+EquipmentCatalog.HealthCharm);
            Near(hp.Find(CombatStatId.MaxHealth).difference,100,"HP charm delta");
            SelectComparison(panel,"stack:"+EquipmentCatalog.HealthCharm);
            Check(CompareText(panel).Contains("Current:") && CompareText(panel).Contains("Preview:") &&
                CompareText(panel).Contains("#75DF93"),"UI current preview green delta");
            Check(!hero.isDirty && hero.combatStatModifiers==mods && mods.Count==count &&
                f.combat.CurrentHeroCombatState.CurrentHP==37,"preview does not change HP/modifiers/dirty");
            Check(gm.EquipInventoryItem(EquipmentCatalog.HealthCharm),"equip HP via real handler");
            Near(hero.GetCalculatedHealth(hero.savedAttackMode),hp.Find(CombatStatId.MaxHealth).preview,"real HP total");
            var atk=Compare(hero,"stack:"+EquipmentCatalog.AttackCharm);
            Near(atk.Find(CombatStatId.MaxHealth).difference,-100,"replace loses HP");
            Near(atk.Find(CombatStatId.Attack).difference,20,"replace gains ATK");
            Check(gm.EquipInventoryItem(EquipmentCatalog.AttackCharm),"replace charm");
            Near(hero.GetCalculatedDamage(hero.savedAttackMode),atk.Find(CombatStatId.Attack).preview,"real ATK total");
            var off=Compare(hero,"worn:"+EquipmentCatalog.AttackCharm);
            SelectComparison(panel,"worn:"+EquipmentCatalog.AttackCharm);
            Check(CompareText(panel).Contains("#FF9292"),"unequip red delta");
            Check(gm.UnequipInventoryItem(),"remove charm");
            Near(hero.GetCalculatedDamage(hero.savedAttackMode),off.Find(CombatStatId.Attack).preview,"unequip matches");
        });
        Run("W3.1b.1 gear swap uses canonical runtime and selected worn gear previews removal",()=>{
            GameManager gm;EntityDataSO hero;Field f;var panel=ComparisonPanel(out gm,out hero,out f);
            var a=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,10);
            var b=EquipmentFoundation.CreateCommon(EquipmentFoundation.DemoSword,1,19);
            EquipmentRuntime.AddInstance(hero,a);EquipmentRuntime.AddInstance(hero,b);
            gm.EquipGear(a.instanceId,EquipmentSlot.Weapon);
            var compare=Compare(hero,"gear:"+b.instanceId);
            Near(compare.Find(CombatStatId.Attack).difference,9,"replace not stack");
            SelectComparison(panel,"gear:"+b.instanceId);Call(panel,"EquipSelected");
            Near(hero.GetCalculatedDamage(hero.savedAttackMode),compare.Find(CombatStatId.Attack).preview,"button equals prediction");
            panel.Refresh();Check(CompareText(panel).Contains("Tháo"),"after equip UI previews unequip");
            var off=Compare(hero,"gear:"+b.instanceId);Call(panel,"RemoveSelected");
            Near(hero.GetCalculatedDamage(hero.savedAttackMode),off.Find(CombatStatId.Attack).preview,"gear remove matches");
            Check(hero.equipmentItems.Count==2,"no lost/duplicated gear");
        });
        Run("W3.1b.1 rings auto-right, explicit right, and both-occupied replacement use actual button slot",()=>{
            GameManager gm;EntityDataSO hero;Field f;var panel=ComparisonPanel(out gm,out hero,out f);
            var rings=new List<EquipmentInstance>();
            foreach(float bonus in new[]{3f,5f,9f}) {
                var ring=EquipmentFoundation.CreateCommon("base:ring",1,bonus);
                EquipmentRuntime.AddInstance(hero,ring);rings.Add(ring);
            }
            Check(Compare(hero,"gear:"+rings[0].instanceId).slot==EquipmentSlot.RingLeft,"first ring left");
            SelectComparison(panel,"gear:"+rings[0].instanceId);Call(panel,"EquipSelected");
            var right=Compare(hero,"gear:"+rings[1].instanceId);
            Check(right.slot==EquipmentSlot.RingRight,"second ring auto right");
            SelectComparison(panel,"gear:"+rings[1].instanceId);Call(panel,"EquipSelected");
            Near(hero.GetCalculatedDamage(hero.savedAttackMode),right.Find(CombatStatId.Attack).preview,"auto-right totals");
            var leftSwap=Compare(hero,"gear:"+rings[2].instanceId);
            var rightSwap=Compare(hero,"gear:"+rings[2].instanceId,true);
            Near(leftSwap.Find(CombatStatId.Attack).difference,6,"left replacement delta");
            Near(rightSwap.Find(CombatStatId.Attack).difference,4,"right replacement delta");
            SelectComparison(panel,"gear:"+rings[2].instanceId);
            Check(Get<List<string>>(panel,"detailPages").Exists(x=>x.Contains("Nút Nhẫn phải")),"explicit right is visible too");
            Call(panel,"EquipRight");Near(hero.GetCalculatedDamage(hero.savedAttackMode),
                rightSwap.Find(CombatStatId.Attack).preview,"explicit right button equals preview");
            var off=Compare(hero,"gear:"+rings[2].instanceId);
            Check(off.slot==EquipmentSlot.RingRight && off.isUnequip,"worn right ring remove");
        });
        Run("W3.1b.1 blocked Artifact, full legacy return stack, invalid and non-equipment show reasons",()=>{
            GameManager gm;EntityDataSO hero;Field f;var panel=ComparisonPanel(out gm,out hero,out f);
            var art=EquipmentFoundation.CreateCommon("base:artifact",1,35);
            EquipmentRuntime.AddInstance(hero,art);gm.EquipInventoryItem(EquipmentCatalog.HealthCharm);
            Check(CompareFailure(hero,"gear:"+art.instanceId).Contains("hộ phù"),"legacy collision reason");
            EquipmentInventory.Add(hero,EquipmentCatalog.HealthCharm,EquipmentInventory.MaxQuantity);
            Check(CompareFailure(hero,"worn:"+EquipmentCatalog.HealthCharm).Contains("đầy"),"full bag prevents unequip");
            Check(CompareFailure(hero,"stack:"+EquipmentCatalog.AttackCharm).Contains("đầy"),"full bag prevents replacement");
            hero.inventory.Find(x=>x.itemId==EquipmentCatalog.HealthCharm).quantity=1;
            gm.UnequipInventoryItem();gm.EquipGear(art.instanceId,EquipmentSlot.Artifact);
            Check(CompareFailure(hero,"stack:"+EquipmentCatalog.AttackCharm).Contains("Pháp bảo"),"modern collision reason");
            Check(Compare(hero,"gear:"+art.instanceId).isUnequip,"worn artifact removal allowed");
            var sword=EquipmentFoundation.CreateDemoSword();EquipmentRuntime.AddInstance(hero,sword);
            Check(CompareFailure(hero,"gear:"+sword.instanceId,true).Contains("ô"),"invalid ring destination");
            EquipmentInventory.Add(hero,EquipmentRefinement.MaterialId);
            SelectComparison(panel,"stack:"+EquipmentRefinement.MaterialId);
            Check(CompareText(panel).Contains("không thể trang bị"),"material has reason, no fake totals");
            EquipmentRuntime.Find(hero,sword.instanceId).level=0;
            Check(CompareFailure(hero,"gear:"+sword.instanceId).Length>0,"invalid gear rejected");
        });
        Run("W3.1b.1 hidden/locked affixes do not leak into comparison; no change is neutral",()=>{
            GameManager gm;EntityDataSO hero;Field f;var panel=ComparisonPanel(out gm,out hero,out f);
            var sword=EquipmentRefinement.CreateForgeDemoSword();EquipmentRuntime.AddInstance(hero,sword);
            var item=EquipmentRuntime.Find(hero,sword.instanceId);
            foreach(var line in item.extraStats) {line.revealed=false;line.flat=9876;}
            hero.isDirty=false;SelectComparison(panel,"gear:"+item.instanceId);
            Check(!string.Join("\n",Get<List<string>>(panel,"detailPages").ToArray()).Contains("9876"),"no hidden value");
            foreach(var line in item.extraStats) {line.revealed=true;line.requiredRefineLevel=20;}
            SelectComparison(panel,"gear:"+item.instanceId);
            Check(!string.Join("\n",Get<List<string>>(panel,"detailPages").ToArray()).Contains("9876"),"locked values not in totals");
            // Neutral main stat exercises neutral comparison without relying on removed stance UI.
            item.mainStats[0].flat=0;
            hero.savedAttackMode=AttackMode.RangedPhysical;panel.Refresh();
            Check(CompareText(panel).Contains("Delta: 0"),"inactive stance neutral");
            Check(!hero.isDirty && !item.isEquipped,"all preview remains pure");
        });
        Run("W3.1b.1 refresh/filter/page/reopen clears stale preview and observes external data",()=>{
            GameManager gm;EntityDataSO hero;Field f;var panel=ComparisonPanel(out gm,out hero,out f);
            SelectComparison(panel,"stack:"+EquipmentCatalog.AttackCharm);
            float oldDelta=Time.unscaledDeltaTime;
            int before=hero.GetCalculatedDamage(hero.savedAttackMode);
            hero.addedDamage+=7;hero.isDirty=false;
            Time.unscaledDeltaTime=.21f;Call(panel,"LateUpdate");
            Check(CompareText(panel).Contains("Current: "+(before+7)) && !hero.isDirty,
                "external stat change refreshes current without dirtying data");
            Call(panel,"NextPage"); // only one page: same selection stays valid
            hero.inventory.Clear();Call(panel,"LateUpdate");Time.unscaledDeltaTime=oldDelta;
            Check(!CompareText(panel).Contains("Preview:"),"removed item preview cleared on poll");
            EquipmentInventory.Add(hero,EquipmentCatalog.AttackCharm);SelectComparison(panel,"stack:"+EquipmentCatalog.AttackCharm);
            typeof(InventoryPanelUI).GetMethod("SwitchTab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,new object[]{2});
            Check(!CompareText(panel).Contains("Preview:"),"filter resets preview");
            panel.Close();panel.Open();Check(!CompareText(panel).Contains("Preview:"),"reopen clears old selection");
            for(int i=0;i<22;i++)EquipmentRuntime.AddInstance(hero,EquipmentFoundation.CreateDemoSword());
            typeof(InventoryPanelUI).GetMethod("SwitchTab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,new object[]{0});
            SelectComparison(panel,"gear:"+hero.equipmentItems[0].instanceId);Call(panel,"NextPage");
            Check(!CompareText(panel).Contains("Preview:"),"bag page resets preview");
        });
        Run("W3.1b.1 compare and navigation stay above Forge/Equip at compact and expanded sizes",()=>{
            GameManager gm;EntityDataSO hero;Field f;var panel=ComparisonPanel(out gm,out hero,out f);
            foreach(int width in new[]{600,800,1150}) foreach(bool expanded in new[]{false,true}) {
                ManagementWindowLayout.SetCompact(width,width*9/16);
                if(expanded)ManagementWindowLayout.Expand(2400,1080);
                int w=expanded?ManagementWindowLayout.TargetWidth:width,h=expanded?ManagementWindowLayout.TargetHeight:width*9/16;
                panel.Layout(width,width*9/16,w,h);
                var text=Get<TextMeshProUGUI>(panel,"detail").rectTransform;
                var nav=Get<RectTransform>(panel,"detailCompareButton");
                var forge=Get<RectTransform>(panel,"forgeButton");var equip=Get<RectTransform>(panel,"equipButton");
                Check(text.rect.height>=55,"enough height for four compare lines at "+width+" expanded="+expanded+" actual="+text.rect.height);
                Check(-text.anchoredPosition.y+text.rect.height<=-nav.anchoredPosition.y,"text above navigation");
                Check(-nav.anchoredPosition.y+nav.rect.height<=-forge.anchoredPosition.y,"navigation above forge");
                Check(-forge.anchoredPosition.y+forge.rect.height<=-equip.anchoredPosition.y,"forge above equip");
                ManagementWindowLayout.Collapse();ManagementWindowLayout.ResolveResize(width,width*9/16);
            }
        });
    }
}
