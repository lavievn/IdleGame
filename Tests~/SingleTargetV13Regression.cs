using System;
using System.Collections.Generic;
using TuTienCore;
partial class MotionRegression
{
 static void SingleTargetV13Tests(){foreach(var family in new[]{WeaponFamily.Unarmed,WeaponFamily.Sword,WeaponFamily.Bow,WeaponFamily.Staff})foreach(int count in new[]{2,3})foreach(bool overlap in new[]{false,true})
 Run("v13 one basic release kills one of "+count+" "+family+" overlap="+overlap,()=>{
  GameManager gm;var f=WeaponField(out gm);if(family!=WeaponFamily.Unarmed)WearWeapon(gm,WeaponInBag(gm,family));
  var monsters=new List<MonsterController>();for(int i=0;i<count;i++)monsters.Add(f.Monster(-30-(overlap?0:i*4)));
  LootBattle(f,gm,monsters.ToArray());var lives=new List<CombatManager.ActiveMonsterInfo>(f.Enemies);
  foreach(var target in lives){target.currentHP=1;target.maxHP=1;target.data.baseDamage=0;target.attackTimer=1000;}
  f.combat.CurrentHeroCombatState.AttackTimer=0;CombatStep(f,.001f);
  if(family==WeaponFamily.Bow||family==WeaponFamily.Staff){Check(f.combat.PendingProjectileCount==1,"one release");f.combat.CurrentHeroCombatState.AttackTimer=1000;CombatStep(f,.4f);}
  int dead=0;foreach(var target in lives)if(target.currentHP==0)dead++;
  Check(dead==1&&f.Enemies.Count==count-1,"one victim only; no wave-wide death");
 });}
}
