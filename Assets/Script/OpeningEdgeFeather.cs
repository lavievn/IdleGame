using UnityEngine;
using UnityEngine.UI;

// Applies a real alpha-gradient to cloud/mountain Image edges.
// Effect is measured inward in UI-rect units, independent of source
// alpha and without modifying any of the imported artwork.
[RequireComponent(typeof(Image))]
public sealed class OpeningEdgeFeather : MonoBehaviour
{
    private Material instance;
    private RectTransform rect;
    private float pixels;
    private static bool warned;

    public void Configure(Image image,float insetPixels)
    {
        if(image==null)return;
        rect=image.rectTransform;
        pixels=Mathf.Clamp(insetPixels,0f,128f);
        var shader=Resources.Load<Shader>("OpeningSoftEdge");
        if(shader==null)
        {
            if(!warned)
            {
                warned=true;
                Debug.LogWarning("[Opening] Missing Resources/OpeningSoftEdge shader; using normal UI sprites.");
            }
            return;
        }
        instance=new Material(shader);
        instance.name="Opening Feather (runtime)";
        image.material=instance;
        Refresh();
    }

    public void Refresh()
    {
        if(rect==null||instance==null)return;
        instance.SetFloat("_FeatherPixels",pixels);
        instance.SetVector("_RectSize",
            new Vector4(Mathf.Max(1f,rect.rect.width),
                Mathf.Max(1f,rect.rect.height),0f,0f));
    }

    private void OnDestroy()
    {
        var image=GetComponent<Image>();
        if(image!=null)image.material=null;
        if(instance!=null)Destroy(instance);
    }
}
