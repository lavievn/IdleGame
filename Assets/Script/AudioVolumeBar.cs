using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Same geometry and native pointer math in Home and gameplay. The track is
// the only hit target; fill/knob cannot swallow the drag. No uGUI Slider needed.
public sealed class AudioVolumeBar
{
    public readonly RectTransform Track,Fill,Knob;
    private readonly GameAudioChannel channel;
    private readonly TextMeshProUGUI valueLabel;
    public bool IsDragging {get;private set;}
    private float rendered=-1f;

    public AudioVolumeBar(string name,Transform parent,GameAudioChannel channel,TextMeshProUGUI valueLabel)
    {
        this.channel=channel;this.valueLabel=valueLabel;
        Track=Make(name,parent,new Color(.11f,.20f,.25f,.96f));
        var hit=Track.gameObject.AddComponent<CustomInteractable>();hit.onClickEvent=new UnityEvent();
        if(UIManager.Instance!=null)UIManager.Instance.RegisterInteractable(hit);
        Fill=Make(name+" Fill",Track,new Color(.49f,.76f,.80f,.95f));
        Knob=Make(name+" Handle",Track,new Color(.94f,.96f,.99f,1f));
    }
    private static RectTransform Make(string name,Transform parent,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rect=(RectTransform)go.transform;rect.SetParent(parent,false);
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;
        return rect;
    }
    public void Refresh(bool force=false)
    {
        float volume=AudioVolumeSettings.Get(channel);
        if(!force && rendered==volume)return;
        rendered=volume;float width=Track.rect.width;
        Fill.anchorMin=Fill.anchorMax=new Vector2(0,.5f);Fill.pivot=new Vector2(0,.5f);
        Fill.anchoredPosition=Vector2.zero;Fill.sizeDelta=new Vector2(width*volume,12f);
        Knob.anchorMin=Knob.anchorMax=new Vector2(.5f,.5f);Knob.pivot=new Vector2(.5f,.5f);
        Knob.anchoredPosition=new Vector2(width*(volume-.5f),0);Knob.sizeDelta=new Vector2(12f,24f);
        if(valueLabel!=null)valueLabel.text=Mathf.RoundToInt(volume*100f)+"%";
    }
    public bool TryBeginDrag(Vector2 screen)
    {
        if(!Track.gameObject.activeInHierarchy || !RectTransformUtility.RectangleContainsScreenPoint(Track,screen,null))return false;
        IsDragging=true;Drag(screen);return true;
    }
    public void Drag(Vector2 screen)
    {
        if(!IsDragging)return;
        if(!Track.gameObject.activeInHierarchy) {EndDrag();return;}
        Vector2 local;
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(Track,screen,null,out local))return;
        AudioVolumeSettings.Preview(channel,(local.x-Track.rect.xMin)/Mathf.Max(1f,Track.rect.width));Refresh();
    }
    public void EndDrag()
    {
        if(IsDragging)AudioVolumeSettings.Save();IsDragging=false;
    }
}
