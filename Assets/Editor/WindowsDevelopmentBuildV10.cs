#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using TuTienCore;
public static class WindowsDevelopmentBuildV10
{
    [Serializable] sealed class Result {public string unity,scene,output,result,backend;public int errors,warnings;public long bytes;public double seconds;public bool development,scriptDebugging;}
    public static void Build()
    {
        string output=null;var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="-ownedV10Build")output=Path.GetFullPath(args[i+1]);
        if(output==null || Path.GetFileName(output)!="IdleGameV10.exe" || !File.Exists(Path.Combine(Path.GetDirectoryName(output),".owner")) || File.ReadAllText(Path.Combine(Path.GetDirectoryName(output),".owner"))!="idlegame-v10-build")throw new InvalidOperationException("Explicit marker-owned build output required.");
        if(output.StartsWith(Path.GetFullPath(Path.Combine(Application.dataPath,".."))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Build outside source repository.");
        foreach(var scene in UnityEngine.SceneManagement.SceneManager.GetAllScenes())if(scene.isDirty)throw new InvalidOperationException("Unsaved scene work.");
        var scenes=EditorBuildSettings.scenes;if(scenes.Length!=1 || !scenes[0].enabled || scenes[0].path!="Assets/Scenes/MainGame.unity")throw new InvalidOperationException("Unexpected production build scene list.");
        CultivationPlaytestProfile p;WorldQiDistribution q;CultivationConfigurationReason reason;string detail;
        if(!CultivationProductionConfiguration.TryLoad(out p,out q,out reason,out detail))throw new InvalidOperationException("Production configuration "+reason+" "+detail);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenes[0].path},target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.Development|BuildOptions.AllowDebugging});
        var s=report.summary;var r=new Result{unity=Application.unityVersion,scene=scenes[0].path,output=output,result=s.result.ToString(),errors=(int)s.totalErrors,warnings=(int)s.totalWarnings,bytes=(long)s.totalSize,seconds=s.totalTime.TotalSeconds,development=true,scriptDebugging=true,backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString()};
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-result.json"),JsonUtility.ToJson(r,true));
        Debug.Log("[WINDOWS V10 BUILD] "+r.result+" "+output);EditorApplication.Exit(s.result==BuildResult.Succeeded?0:2);
    }
}
#endif
