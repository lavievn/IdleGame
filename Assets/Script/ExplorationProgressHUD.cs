using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Alpha HUD diagnostic: displays only the actual shared exploration distance
// credited by GameManager (not raw Hero speed, camera pan or elapsed time).
public sealed class ExplorationProgressHUD : MonoBehaviour
{
    private GameManager manager;
    private Image fill;
    private TextMeshProUGUI label;
    private float lastDistance=-1f, lastTarget=-1f;
    private bool lastExploring;
    private const float BarHeight=19f;

    public static void Install(GameManager gm)
    {
        if(gm==null || gm.eventLog==null || gm.eventLogText==null)return;
        var parent=gm.eventLog.transform;
        var existing=parent.Find("Exploration Progress");
        if(existing!=null)return;
        var go=new GameObject("Exploration Progress",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(Image));
        var root=(RectTransform)go.transform;
        root.SetParent(parent,false);
        // A strip immediately ABOVE the damage/event log, sharing its width
        // and visibility, without interfering with log button hitboxes.
        root.anchorMin=new Vector2(0f,1f);
        root.anchorMax=new Vector2(1f,1f);
        root.pivot=new Vector2(.5f,0f);
        root.anchoredPosition=new Vector2(0f,5f);
        root.sizeDelta=new Vector2(0f,BarHeight);
        var backdrop=go.GetComponent<Image>();
        backdrop.color=new Color(.055f,.085f,.13f,.88f);
        backdrop.raycastTarget=false;
        var hud=go.AddComponent<ExplorationProgressHUD>();
        hud.manager=gm;

        var fg=new GameObject("Distance Fill",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(Image));
        var bar=(RectTransform)fg.transform;
        bar.SetParent(root,false);
        bar.anchorMin=Vector2.zero;
        bar.anchorMax=new Vector2(0f,1f);
        bar.pivot=new Vector2(0f,.5f);
        bar.offsetMin=Vector2.zero;
        bar.offsetMax=Vector2.zero;
        hud.fill=fg.GetComponent<Image>();
        hud.fill.color=new Color(.22f,.72f,.56f,.9f);
        hud.fill.raycastTarget=false;

        var textObject=new GameObject("Distance Label",typeof(RectTransform),
            typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        var textRect=(RectTransform)textObject.transform;
        textRect.SetParent(root,false);
        textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;
        textRect.offsetMin=Vector2.zero;textRect.offsetMax=Vector2.zero;
        hud.label=textObject.GetComponent<TextMeshProUGUI>();
        hud.label.font=gm.eventLogText.font;
        hud.label.fontSize=12;
        hud.label.alignment=TextAlignmentOptions.Center;
        hud.label.color=Color.white;
        hud.label.raycastTarget=false;
        hud.label.enableWordWrapping=false;
        hud.Render(true);
    }

    private void LateUpdate(){Render(false);}
    private void Render(bool force)
    {
        if(manager==null || fill==null || label==null)return;
        float target=Mathf.Max(1f,manager.CurrentDistancePerWave);
        bool exploring=manager.IsWaitingForExploration;
        float distance=Mathf.Clamp(manager.ExplorationDistance,0f,target);
        if(!force && Mathf.Abs(distance-lastDistance)<.01f &&
            Mathf.Abs(target-lastTarget)<.01f && exploring==lastExploring)return;
        lastDistance=distance;lastTarget=target;lastExploring=exploring;
        var rect=fill.rectTransform;
        rect.anchorMax=new Vector2(Mathf.Clamp01(distance/target),1f);
        // During combat the next-encounter counter is inactive; keep 0/YY
        // visible for testing but never imply progress while fighting.
        label.text=Mathf.FloorToInt(distance)+"/"+Mathf.CeilToInt(target)+
            (exploring?"  Khám phá":"  Chiến đấu / chờ");
    }
}
