#if UNITY_EDITOR
using System;
using System.IO;
using TuTienCore;
using UnityEditor;
using UnityEngine;
public static class DevelopmentV12Verifier
{
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
