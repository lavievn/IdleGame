using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using TuTienCore;
using UnityEngine;

partial class MotionRegression
{
    static int auditPassed,auditFailed;
    static void Check(bool value,string message) {if(!value)throw new Exception(message);}
    static void Near(float value,float expected,string message) {Check(Math.Abs(value-expected)<.001f,message+": "+value+" != "+expected);}
    static void Run(string name,Action test)
    {
        try {test();auditPassed++;Console.WriteLine("PASS "+name);}
        catch(Exception ex) {auditFailed++;Console.WriteLine("FAIL "+name+": "+ex.Message);}
    }
    // Same fixture initialization as BalanceRegression, without its unrelated
    // manager/combat/I/O cases. AddExp itself remains production code.
    static EntityDataSO HeroAt(int level)
    {var hero=ScriptableObject.CreateInstance<EntityDataSO>();hero.currentLevel=level;hero.ApplyHeroBalance();return hero;}
    static readonly JavaScriptSerializer auditJson=new JavaScriptSerializer {MaxJsonLength=4*1024*1024};
    static readonly Dictionary<int,CultivationPlaytestProfile> auditProfiles=new Dictionary<int,CultivationPlaytestProfile>();
    static WorldQiDefinition auditWorld;
    static T ReadAuditData<T>(string name) {return auditJson.Deserialize<T>(File.ReadAllText("Assets/Resources/"+name+".json"));}
    static CultivationPlaytestProfile AuditProfile(int version=3)
    {
        CultivationPlaytestProfile cached;if(auditProfiles.TryGetValue(version,out cached))return cached;
        Resources.Register("WorldMapCatalog",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapCatalog.txt")));
        Resources.Register("WorldMapPolygonSeeds",new TextAsset(File.ReadAllText("Assets/Resources/WorldMapPolygonSeeds.txt")));
        var definition=ReadAuditData<CultivationProductionDefinition>("CultivationProductionV"+version);
        var world=ReadAuditData<WorldQiDefinition>(version==1?"WorldQiDistributionV1":"WorldQiDistributionV2");
        WorldQiDistribution table;WorldQiReason why;
        Check(WorldQiDistribution.TryCreate(world,WorldMapCatalog.All.Select(n=>n.Id),WorldMapCatalog.All.Where(n=>n.IsStart).Select(n=>n.Id),out table,out why),"world data: "+why);
        CultivationPlaytestProfile profile;CultivationConfigurationReason reason;string detail;
        Check(CultivationProductionConfiguration.TryCreate(definition,world,table,id=>{WorldMapNode n;return WorldMapCatalog.TryGet(id,out n);},
            out profile,out reason,out detail,version==3?ReadAuditData<CultivationCapacityDefinition>("CultivationCapacityV1"):null),"production profile: "+reason+" "+detail);
        auditProfiles.Add(version,profile);if(version==3)auditWorld=world;return profile;
    }
    static void Main(string[] args)
    {
        if(args.Length>0 && args[0]=="--simulate") {AuditSimulations(args.Length>1?args[1]:null);return;}
        CultivationCoreTests();BloodEssenceTests();CultivationBreakthroughTests();CultivationPersistenceTests();
        DualProgressionAuditTests();
        Console.WriteLine("RESULT "+auditPassed+" PASS, "+auditFailed+" FAIL (production data APIs / UnityStub; no SaveManager or save I/O)");
        Console.WriteLine("REQUIREMENT GAPS: EXP reserve/release; Weapon/Skill Mastery; production successors/trials/Blood; within-node spatial Qi; automatic injury treatment.");
        if(auditFailed>0)Environment.Exit(1);
    }
}
