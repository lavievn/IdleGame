#if UNITY_EDITOR
using System;
using System.IO;
using TuTienCore;
using UnityEditor;
using UnityEngine;
public static class DevelopmentV12Verifier
{
    public static void Tribulation()
    {
        int exit=0;try{var checks=HeavenlyTribulationFixtures.Run();string root=Path.Combine(Environment.GetEnvironmentVariable("IDLEGAME_HOME")??"D:/IdleGame","Automation/v12/stage-b");Directory.CreateDirectory(root);File.WriteAllLines(Path.Combine(root,"native-tribulation.txt"),checks);Debug.Log("V12_TRIBULATION_PASS "+checks.Count);}
        catch(Exception e){Debug.LogException(e);exit=2;}EditorApplication.Exit(exit);
    }
    public static void Measure()
    {
        int exit=0;try{
            string root=Path.Combine(Environment.GetEnvironmentVariable("IDLEGAME_HOME")??"D:/IdleGame","Automation/v12/stage-a");Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root,"native-measurements.json"),JsonUtility.ToJson(CultivationV12Measurements.Run(),true));
            Debug.Log("V12_MEASURE_PASS "+root);
        }catch(Exception e){Debug.LogException(e);exit=2;}EditorApplication.Exit(exit);
    }
}
#endif
