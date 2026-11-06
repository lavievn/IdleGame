#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CultivationPlaytestLauncher
{
    public const string ConfigurationPath="Assets/Editor/CultivationPlaytestV07.json";
    [MenuItem("Bach Luyen/Playtests/Cultivation v0.7 (isolated)")]
    public static void StartInteractive(){StartProfile(ConfigurationPath);}
    [MenuItem("Bach Luyen/Playtests/Cultivation balance v1 - 60 minutes (isolated)")]
    public static void StartBalance(){StartProfile("Assets/Editor/CultivationBalanceV1.json");}
    static void StartProfile(string configurationPath)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop the current Play session first.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene work before opening the playtest.");
        string json=File.ReadAllText(configurationPath);CultivationPlaytestBootstrap.ReadProfile(json);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity");
        var owners=UnityEngine.Object.FindObjectsByType<SaveManager>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if(owners.Length!=1)throw new InvalidOperationException("Expected one MainGame SaveManager.");
        owners[0].gameObject.SetActive(false);
        var bootstrap=new GameObject("Owned cultivation playtest bootstrap").AddComponent<CultivationPlaytestBootstrap>();
        bootstrap.managerOwner=owners[0].gameObject;bootstrap.configurationJson=json;
        bootstrap.profileRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../.utmp/cultivation-playtest-"+Guid.NewGuid().ToString("N")));
        // Validate ancestors before creating this owned directory.
        for(var d=new DirectoryInfo(bootstrap.profileRoot);d!=null;d=d.Parent)if(d.Exists && (d.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("No reparse paths.");
        Directory.CreateDirectory(bootstrap.profileRoot);File.WriteAllText(Path.Combine(bootstrap.profileRoot,".owner"),"cultivation-playtest-v07");
        CultivationPlaytestBootstrap.VerifyOwnedProfile(bootstrap.profileRoot);
        if(!EditorSceneManager.SaveScene(scene,"Assets/InitTestSceneFoundationValidation.unity"))throw new InvalidOperationException("Could not save owned scene copy.");
        EditorApplication.isPlaying=true;
    }
}
#endif
