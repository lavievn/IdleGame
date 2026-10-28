using System;
using System.Collections.Generic;
using TuTienCore;

// Family belongs to the template, not display text or a new saved item field.
public enum WeaponFamily { Unarmed, Sword, Bow, Staff }
public static class WeaponBasicAttack
{
    public static EquipmentInstance Equipped(EntityDataSO hero) {return EquipmentRuntime.Equipped(hero,EquipmentSlot.Weapon);}
    public static WeaponFamily Family(EquipmentInstance item)
    {
        var template=item==null?null:EquipmentFoundation.FindTemplate(item.templateId);
        return template!=null && EquipmentFoundation.IsValid(item)?template.weaponFamily:WeaponFamily.Unarmed;
    }
    public static AttackMode Mode(WeaponFamily family)
    {return family==WeaponFamily.Bow?AttackMode.RangedPhysical:family==WeaponFamily.Staff?AttackMode.RangedMagic:AttackMode.Melee;}
    public static AttackMode Resolve(EntityDataSO hero) {return Mode(Family(Equipped(hero)));}
    public static string Identity(EntityDataSO hero) {var item=Equipped(hero);return item==null?"":item.instanceId;}
    // Shared CombatStats evaluator; Attack keeps its serialized ID and is physical only.
    public static int Value(EntityDataSO hero,CombatStatId stat,AttackMode mode,IEnumerable<CombatStatModifier> mods)
    {
        int raw=stat==CombatStatId.Attack?hero.baseDamage+hero.addedDamage:0;
        return Math.Max(stat==CombatStatId.Attack?1:0,CombatStats.GetIntWithModifiers(hero,mods,stat,raw,
            ElementType.Vo,new[]{CombatStats.StanceTag(mode)}));
    }
    public static int Physical(EntityDataSO hero) {return Value(hero,CombatStatId.Attack,Resolve(hero),hero.combatStatModifiers);}
    public static int Magical(EntityDataSO hero) {return Value(hero,CombatStatId.MagicalAttack,Resolve(hero),hero.combatStatModifiers);}
    public static int DamageSource(EntityDataSO hero) {return Resolve(hero)==AttackMode.RangedMagic?Magical(hero):Physical(hero);}
    public static float Interval(AttackMode mode,float speed)
    {
        // Staff is an ordinary attack, using the Hero base interval (no cast clock).
        return CombatBalance.AttackInterval(mode==AttackMode.RangedMagic?AttackMode.Melee:mode,
            speed,CombatBalance.HeroDev!=null);
    }
}
