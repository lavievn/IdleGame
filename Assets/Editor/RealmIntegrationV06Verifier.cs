#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TuTienCore;

// Explicit CLI verifier only; no startup hook and no player Save access.
public static class RealmIntegrationV06Verifier
{
    public static void VerifyCompilation()
    {
        foreach(var type in new[]{typeof(RealmProgressionEvaluator),typeof(RealmBreakthroughTransition),typeof(MartialArtsRegistry),typeof(BossEncounterCoordinator)})
        {
            if(type.Assembly!=typeof(EntityDataSO).Assembly)throw new InvalidOperationException("Unexpected assembly for "+type.FullName);
            Debug.Log("[REALM V06 BASELINE] Unity type loaded: "+type.FullName+" from "+type.Assembly.GetName().Name);
        }
        Debug.Log("[REALM V06 BASELINE] PASS Unity "+Application.unityVersion);
        EditorApplication.Exit(0);
    }
}
#endif
