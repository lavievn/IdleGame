#if UNITY_EDITOR
using System;
// Offline policy API only. No production GameManager/clock/UI path calls it.
[Serializable] public sealed class CultivationRefinementModelV10
{public string id,kind;public double baseCapacity,bonusMaximum,bonusScale;}
[Serializable] public sealed class CultivationRefinementCatalogV10
{public int schemaVersion,version,worldQiVersion;public string id,worldQiId;public CultivationRefinementModelV10[] models;}
public static class CultivationRefinementModelsV10
{
    public static double ResolveInputCapacity(CultivationRefinementModelV10 model,double ambientQi,double characterCapacity=1,double techniqueMultiplier=1)
    {
        if(model==null || !TuTienCore.CultivationNumber.NonNegative(ambientQi) || !TuTienCore.CultivationNumber.NonNegative(characterCapacity) || characterCapacity<=0 || !TuTienCore.CultivationNumber.NonNegative(techniqueMultiplier) || techniqueMultiplier<=0 || !TuTienCore.CultivationNumber.NonNegative(model.baseCapacity) || model.baseCapacity<=0 || !TuTienCore.CultivationNumber.NonNegative(model.bonusMaximum) || model.bonusMaximum>.25 || !TuTienCore.CultivationNumber.NonNegative(model.bonusScale) || model.bonusScale<=0)throw new ArgumentException("Explicit finite bounded refinement policy required.");
        double cap;
        if(model.id=="A" && model.kind=="reserve-only" && model.bonusMaximum==0)cap=model.baseCapacity;
        else if(model.id=="B" && model.kind=="limited-ambient")cap=model.baseCapacity*(1+model.bonusMaximum*(1-Math.Exp(-Math.Max(0,ambientQi-1)/model.bonusScale)));
        else if(model.id=="C" && model.kind=="capacity-driven" && model.bonusMaximum==0)cap=characterCapacity*techniqueMultiplier;
        else throw new ArgumentException("Unknown refinement model or unsupported policy.");
        if(!TuTienCore.CultivationNumber.NonNegative(cap) || cap<=0 || cap>100)throw new ArgumentException("Fixture capacity out of bounds.");return cap;
    }
}
#endif
