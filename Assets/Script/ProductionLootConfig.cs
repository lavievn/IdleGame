using System;
using System.Collections.Generic;

[Serializable]
public sealed class MaterialRewardConfig
{
    public bool enabled;
    public double chance;
    public string materialId="";
    public int minQuantity,maxQuantity;
}
[Serializable]
public sealed class EquipmentRewardConfig
{
    public enum SelectionPolicy { Unconfigured, Uniform }
    [Serializable]
    public sealed class GearTemplate
    {
        public string templateId="";
        public int itemLevel;
        public float mainStat;
    }
    public bool enabled;
    public double chance;
    public SelectionPolicy selectionPolicy;
    public List<GearTemplate> templates=new List<GearTemplate>();
}

// Inspector configuration, not player-save data. MainGame authors its prototype
// explicitly; constructors never activate or invent a balance configuration.
[Serializable]
public sealed class ProductionLootConfig
{
    public enum Kind { Unconfigured, CommonEquipment, ForgeMaterial }
    public struct Reward
    {
        public Kind kind;
        public string id,instanceId;
        public int quantity,itemLevel;
        public float mainStat;
    }
    public bool configured;
    public MaterialRewardConfig material=new MaterialRewardConfig();
    public EquipmentRewardConfig equipment=new EquipmentRewardConfig();
    static void Chance(double value)
    {
        if(double.IsNaN(value) || double.IsInfinity(value) || value<0 || value>1)
            throw new InvalidOperationException("Chance must be in [0,1].");
    }
    static double Unit(Func<double> next)
    {
        double value=next();
        if(double.IsNaN(value) || double.IsInfinity(value) || value<0 || value>=1)
            throw new InvalidOperationException("Reward roll must be in [0,1).");
        return value;
    }
    public Reward RollMaterial(Func<double> next)
    {
        if(material==null || !material.enabled)return default(Reward);
        double chance=material.chance;string id=material.materialId;
        int min=material.minQuantity,max=material.maxQuantity;
        Chance(chance);
        if(id!=EquipmentRefinement.MaterialId || min<1 || max<min || max>EquipmentInventory.MaxQuantity)
            throw new InvalidOperationException("Expected Forge material ID and quantity range 1..9999.");
        if(chance==0 || Unit(next)>=chance)return default(Reward);
        int quantity=min==max?min:min+(int)(Unit(next)*(max-min+1));
        return new Reward {kind=Kind.ForgeMaterial,id=id,quantity=quantity};
    }
    public Reward RollEquipment(Func<double> next)
    {
        if(equipment==null || !equipment.enabled)return default(Reward);
        double chance=equipment.chance;Chance(chance);
        if(equipment.selectionPolicy!=EquipmentRewardConfig.SelectionPolicy.Uniform ||
            equipment.templates==null || equipment.templates.Count==0)
            throw new InvalidOperationException("Equipment needs an explicit selection policy and templates.");
        var choices=new Reward[equipment.templates.Count];var ids=new HashSet<string>(StringComparer.Ordinal);
        for(int i=0;i<choices.Length;i++)
        {
            var t=equipment.templates[i];
            if(t==null || EquipmentFoundation.FindTemplate(t.templateId)==null || !ids.Add(t.templateId) ||
                t.itemLevel<1 || t.itemLevel>EquipmentFoundation.MaximumItemLevel ||
                float.IsNaN(t.mainStat) || float.IsInfinity(t.mainStat) || t.mainStat<=0)
                throw new InvalidOperationException("Each unique catalog template needs an explicit valid level and positive main stat.");
            choices[i]=new Reward {kind=Kind.CommonEquipment,id=t.templateId,quantity=1,itemLevel=t.itemLevel,mainStat=t.mainStat};
        }
        if(chance==0 || Unit(next)>=chance)return default(Reward);
        return choices[choices.Length==1?0:(int)(Unit(next)*choices.Length)];
    }
}
