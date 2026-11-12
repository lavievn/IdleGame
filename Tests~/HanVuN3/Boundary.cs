using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Only external context and presentation services are doubles. Actual origin
// host methods are extracted unchanged from GameManager.cs by extract_host.py;
// actual UIManager.StoryOrigin.cs and all origin domain classes are compiled.
// These fixtures do not execute real GameManager/actor lifecycle or rendering.
public sealed class HeroController : MonoBehaviour
{
    public int SpawnVersion = 1;
}

public partial class GameManager : MonoBehaviour
{
    EntityDataSO runtimeHeroData;
    HeroController heroController;
    public EntityDataSO HeroData => runtimeHeroData;
    public bool IsGameplayHUDVisible = true;
    public bool HasTribulationLiability;
    public int CommitObservations;

    internal void BindFixture(EntityDataSO data)
    {
        runtimeHeroData=data;
        if(heroController==null)heroController=new GameObject("fixture actor").AddComponent<HeroController>();
    }
    internal void RespawnFixture() { heroController.SpawnVersion++; }
    // Commit is deliberately a sink. Nothing is serialized or written. A PASS
    // here is never a Save transaction, crash or durable exactly-once result.
    void CommitOriginChange(string narration) { CommitObservations++; }
}

public sealed class HudW3View
{
    public RectTransform Root;
    public static void Place(RectTransform r,float x,float y,float w,float h) { }
}

public partial class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    HudW3View w3;
    GameManager hudGame;
    RectTransform worldMapButton;
    readonly Dictionary<string,UnityAction> fixtureActions=new Dictionary<string,UnityAction>();
    GameManager GameForUI() => hudGame;
    void LayoutReadableUI() { }
    static void LayoutHudButton(RectTransform r,float size) { }
    static void ReadableText(TextMeshProUGUI text,float size) { }
    internal TextMeshProUGUI MakeLabel(Transform parent,string text,Vector2 position,Vector2 size)
    {
        var go=new GameObject("fixture label",typeof(RectTransform),typeof(TextMeshProUGUI));
        go.transform.SetParent(parent,false);
        var label=go.GetComponent<TextMeshProUGUI>();label.text=text;return label;
    }
    internal RectTransform SmallButton(Transform parent,string name,string text,Vector2 anchor,
        Vector2 pivot,Vector2 position,Vector2 size,UnityAction action)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        MakeLabel(go.transform,text,position,size);fixtureActions.Add(name,action);
        return (RectTransform)go.transform;
    }
    void BindClick(RectTransform rect,UnityAction action) { fixtureActions[rect.gameObject.name]=action; }
    internal void InitializeFixture(GameManager game)
    {
        hudGame=game;w3=new HudW3View{Root=(RectTransform)new GameObject("fixture root",typeof(RectTransform)).transform};
        Instance=this;InstallOriginPresentation();TickOriginPresentation();
    }
    internal void PressFixture(string name) { fixtureActions[name](); }
    internal UnityAction ActionFixture(string name) => fixtureActions[name];
    internal void TickFixture() { TickOriginPresentation(); }
}

public sealed class EnvironmentManager
{
    public float DefaultAttackRange(TuTienCore.AttackMode mode,bool hero)
    {throw new NotSupportedException("Environment is outside origin data tests");}
}
