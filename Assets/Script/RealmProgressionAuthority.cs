using TuTienCore;

// Retired v1 Save compatibility only. These fields are NOT cultivation authority.
// Never infer cultivation/minor stage from Level in MainGame.
public static class LegacyRealmSnapshotCompatibility
{
    public const int ComponentVersion=1;
    public const string LuyenKhiId="realm:luyen-khi";
    public const string LuyenKhiName="Luyện Khí";
    public static void Initialize(EntityDataSO hero)
    {
        hero.realmComponentVersion=ComponentVersion;hero.realmId=LuyenKhiId;hero.realmEntryLevel=1;hero.isDirty=true;
    }
    public static bool TryValidate(int version,string id,int entry,int total,out string reason)
    {
        if(version!=ComponentVersion) {reason="Unsupported Realm component version: "+version;return false;}
        // Validate historical v1 metadata without using Level to derive a realm/stage.
        bool valid=id==LuyenKhiId && entry>=1 && total>=entry && total<=999;
        reason=valid?null:"Invalid legacy Realm snapshot metadata.";return valid;
    }
    public static bool TryEvaluateLegacyLevelSnapshot(EntityDataSO hero,out RealmProgressionResult result)
    {
        result=null;string reason;
        if(hero==null || !TryValidate(hero.realmComponentVersion,hero.realmId,hero.realmEntryLevel,hero.currentLevel,out reason))return false;
        result=new LegacyLevelRealmProgressionEvaluator(new[]{LuyenKhiId}).Evaluate(hero.realmId,hero.realmEntryLevel,hero.currentLevel);return true;
    }
    public static string Describe(EntityDataSO hero)
    {return hero!=null && hero.realmId==LuyenKhiId?LuyenKhiName+" · "+CultivationIntegrationGate.Status:"Realm v1 không hợp lệ";}
}
