using TuTienCore;

// Canonical Hero component only. No successor, trial, buff or extra progression clock.
public static class RealmProgressionAuthority
{
    public const int ComponentVersion=1;
    public const string LuyenKhiId="realm:luyen-khi";
    public const string LuyenKhiName="Luyện Khí";
    private static readonly LegacyLevelRealmProgressionEvaluator evaluator=new LegacyLevelRealmProgressionEvaluator(new[]{LuyenKhiId});
    public static void Initialize(EntityDataSO hero)
    {
        hero.realmComponentVersion=ComponentVersion;hero.realmId=LuyenKhiId;hero.realmEntryLevel=1;hero.isDirty=true;
    }
    public static bool TryValidate(int version,string id,int entry,int total,out string reason)
    {
        if(version!=ComponentVersion) {reason="Unsupported Realm component version: "+version;return false;}
        var result=evaluator.Evaluate(id,entry,total);
        reason=result.IsValid?null:"Invalid Realm state: "+result.Reason;
        return result.IsValid;
    }
    public static bool TryEvaluate(EntityDataSO hero,out RealmProgressionResult result)
    {
        result=null;string reason;
        if(hero==null || !TryValidate(hero.realmComponentVersion,hero.realmId,hero.realmEntryLevel,hero.currentLevel,out reason))return false;
        result=evaluator.Evaluate(hero.realmId,hero.realmEntryLevel,hero.currentLevel);return true;
    }
    public static string Describe(EntityDataSO hero)
    {
        RealmProgressionResult result;
        if(!TryEvaluate(hero,out result))return "Cảnh giới không hợp lệ";
        string stage=result.MinorStage==RealmMinorStage.SoKy?"Sơ kỳ":result.MinorStage==RealmMinorStage.TrungKy?"Trung kỳ":
            result.MinorStage==RealmMinorStage.HauKy?"Hậu kỳ":"Viên mãn";
        return LuyenKhiName+" · "+stage+(result.AtBottleneck?"":" "+result.InternalLevel);
    }
}
