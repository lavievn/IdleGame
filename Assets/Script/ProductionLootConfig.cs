using System;
using System.Collections.Generic;

// Configuration only, not a save schema or a source of balancing defaults.
// Entries are mutually exclusive outcomes of one roll; the remainder is NoDrop.
// Nothing is enabled until a designer supplies and explicitly configures a table.
[Serializable]
public sealed class ProductionLootConfig
{
    public enum Kind { Unconfigured, CommonEquipment, ForgeMaterial }
    [Serializable]
    public sealed class Rule
    {
        public Kind kind;
        public string id = "";
        public double probability;
        public int quantity;
        public int itemLevel;
        public float mainStat;
    }
    public struct Reward
    {
        public Kind kind;
        public string id, instanceId;
        public int quantity, itemLevel;
        public float mainStat;
    }
    public bool configured;
    public List<Rule> entries = new List<Rule>();

    public bool Validate(out string error)
    {
        error="";
        if(!configured || entries==null || entries.Count==0)
        {error="Production reward table is not configured.";return false;}
        double sum=0;
        foreach(var rule in entries)
        {
            if(rule==null || double.IsNaN(rule.probability) || double.IsInfinity(rule.probability) ||
                rule.probability<=0 || rule.probability>1)
            {error="Each reward needs an explicit probability in (0,1].";return false;}
            sum+=rule.probability;
            if(rule.kind==Kind.CommonEquipment)
            {
                if(EquipmentFoundation.FindTemplate(rule.id)==null || rule.quantity!=1 ||
                    rule.itemLevel<1 || rule.itemLevel>EquipmentFoundation.MaximumItemLevel ||
                    float.IsNaN(rule.mainStat) || float.IsInfinity(rule.mainStat) || rule.mainStat<=0)
                {error="Equipment reward needs a catalog ID, quantity 1, valid level and explicit positive main stat.";return false;}
            }
            else if(rule.kind==Kind.ForgeMaterial)
            {
                if(rule.id!=EquipmentRefinement.MaterialId || rule.quantity<1 || rule.quantity>EquipmentInventory.MaxQuantity)
                {error="Material reward needs the existing Forge material ID and a supported quantity.";return false;}
            }
            else {error="Reward kind is not configured.";return false;}
        }
        if(sum>1) {error="Exclusive reward probabilities must sum to at most 1.";return false;}
        return true;
    }
    public bool TrySelect(double roll,out Reward reward,out string error)
    {
        reward=default(Reward);
        if(!Validate(out error))return false;
        if(double.IsNaN(roll) || double.IsInfinity(roll) || roll<0 || roll>=1)
        {error="Reward roll must be in [0,1).";return false;}
        double cumulative=0;
        foreach(var rule in entries)
        {
            cumulative+=rule.probability;
            if(roll>=cumulative)continue;
            reward=new Reward {kind=rule.kind,id=rule.id,quantity=rule.quantity,itemLevel=rule.itemLevel,mainStat=rule.mainStat};
            return true;
        }
        return false; // An explicit NoDrop remainder; never grant a default item.
    }
}
