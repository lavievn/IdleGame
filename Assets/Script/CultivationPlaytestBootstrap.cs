#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using TuTienCore;
using UnityEngine;

// Only the explicit Editor menu installs this in an ignored scene copy. No scheduler.
public sealed class CultivationPlaytestBootstrap : MonoBehaviour
{
    public GameObject managerOwner;public string profileRoot,configurationJson;
    public static CultivationPlaytestProfile ReadProfile(string json)
    {
        var definition=new CultivationPlaytestDefinition();JsonUtility.FromJsonOverwrite(json,definition);
        CultivationPlaytestProfile profile;string reason;
        if(!CultivationPlaytestProfile.TryCreate(definition,id=>{WorldMapNode node;return WorldMapCatalog.TryGet(id,out node);},out profile,out reason))
            throw new InvalidOperationException(reason);
        return profile;
    }
    public static void VerifyOwnedProfile(string root)
    {
        string path=Path.GetFullPath(root),parent=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp"));
        if(!string.Equals(Path.GetDirectoryName(path),parent,StringComparison.OrdinalIgnoreCase) || !Regex.IsMatch(Path.GetFileName(path),"^cultivation-playtest-[0-9a-f]{32}$"))throw new InvalidOperationException("Not an owned playtest path.");
        for(var p=new DirectoryInfo(path);p!=null;p=p.Parent)if(p.Exists && (p.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("No reparse paths.");
        if(File.ReadAllText(Path.Combine(path,".owner"))!="cultivation-playtest-v07")throw new InvalidOperationException("Invalid profile marker.");
    }
    void Awake()
    {
        VerifyOwnedProfile(profileRoot);
        if(managerOwner==null || managerOwner.activeSelf)throw new InvalidOperationException("Managers must be inactive before Awake.");
        var profile=ReadProfile(configurationJson);var save=managerOwner.GetComponent<SaveManager>();
        save.ConfigureSaveDirectoryBeforeAwake(Path.Combine(profileRoot,"saves"));save.ConfigureCultivationPlaytestBeforeAwake(profile,true);
        managerOwner.GetComponent<GameManager>().ConfigureCultivationPlaytest(profile);managerOwner.SetActive(true);
        Debug.Log("[Cultivation Playtest] Isolated profile: "+profileRoot+"; choose New Game, then Character. No player Save migration.");
    }
}
#endif
