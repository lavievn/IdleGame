using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Opt-in observation only. Never forces a Canvas rebuild or writes a Transform.
// LateUpdate follows GroundPresentation (200); the render callback and coroutine
// also sample changes made by UI rebuilding or later scripts in native Unity.
[DefaultExecutionOrder(32000)]
public sealed class HeroRenderProbe : MonoBehaviour
{
    private HeroController owner;
    private int untilFrame=-1;
    private int canvasFrame=-1,canvasPass;
    private bool subscribed;
    private Coroutine endOfFrame;
    public bool IsTracing => isActiveAndEnabled && owner!=null && owner.traceVisualPositionChanges &&
        owner.isActiveAndEnabled && owner.heroRect!=null &&
        owner.heroRect.gameObject.activeInHierarchy && Time.frameCount<=untilFrame;

    public static void Record(HeroController hero,string stage,int minimumFrames=0)
    {
        if(hero==null || !hero.traceVisualPositionChanges || !hero.isActiveAndEnabled ||
            hero.heroRect==null || !hero.heroRect.gameObject.activeInHierarchy)return;
        var probe=hero.GetComponent<HeroRenderProbe>();
        if(probe==null)probe=hero.gameObject.AddComponent<HeroRenderProbe>();
        if(!probe.isActiveAndEnabled)return;
        probe.owner=hero;
        int frames=Mathf.Clamp(Mathf.Max(minimumFrames,hero.renderTraceFrameCount),10,120);
        probe.untilFrame=Time.frameCount+frames-1;
        probe.Subscribe();
        probe.Sample(stage);
    }
    private void Subscribe()
    {
        if(!subscribed) { Canvas.willRenderCanvases+=OnWillRender;subscribed=true; }
        if(endOfFrame==null)endOfFrame=StartCoroutine(SampleEndOfFrame());
    }
    private void StopTracing()
    {
        if(subscribed) { Canvas.willRenderCanvases-=OnWillRender;subscribed=false; }
        if(endOfFrame!=null) { StopCoroutine(endOfFrame);endOfFrame=null; }
        untilFrame=-1;
    }
    private void LateUpdate()
    {
        if(!IsTracing) { StopTracing();return; }
        Sample("LateUpdate after Ground");
    }
    private void OnWillRender()
    {
        if(!IsTracing) { StopTracing();return; }
        if(canvasFrame!=Time.frameCount) { canvasFrame=Time.frameCount;canvasPass=0; }
        canvasPass++;
        Sample("Canvas.willRenderCanvases pass="+canvasPass);
    }
    private IEnumerator SampleEndOfFrame()
    {
        while(IsTracing)
        {
            yield return new WaitForEndOfFrame();
            if(IsTracing)Sample("EndOfFrame after rendering");
        }
        // The coroutine completes itself; do not StopCoroutine on its own stack.
        endOfFrame=null;
        if(subscribed) { Canvas.willRenderCanvases-=OnWillRender;subscribed=false; }
    }
    private void OnDisable() { StopTracing(); }
    private void OnDestroy() { StopTracing(); }
    private void Sample(string stage)
    {
        // Stack traces on every render-stage log add Console work/allocations.
        // Retain the stage/frame data without repeatedly formatting a call stack.
        Debug.LogFormat(LogType.Log,LogOption.NoStacktrace,null,"{0}",Describe(owner,stage));
    }

    // A reference point in an Image's full rect, NOT its alpha silhouette.
    // Pixel-adjusted rect observes native Canvas pixel rounding without rebuilding.
    public static Vector3 ImageReferenceWorld(Image image,Vector2 uv,bool pixelAdjusted)
    {
        Rect rect=pixelAdjusted?image.GetPixelAdjustedRect():image.rectTransform.rect;
        return image.rectTransform.TransformPoint(new Vector3(rect.xMin+rect.width*uv.x,
            rect.yMin+rect.height*uv.y,0f));
    }
    public static Vector3 LogicFootWorld(RectTransform rect)
    {
        return rect.TransformPoint(new Vector3(rect.rect.center.x,rect.rect.yMin,0f));
    }
    public static string Describe(HeroController hero,string stage)
    {
        if(hero==null || hero.heroRect==null)return "[HeroRender] "+stage+" references missing";
        var root=hero.heroRect;
        var space=EnvironmentManager.Instance;
        var area=space!=null?space.BattleArea:null;
        var canvas=FindCanvas(root);
        var camera=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
        var bridge=root.GetComponent<CultivationSpriteBridge>();
        var body=bridge!=null?bridge.BodyGraphic:root.GetComponent<Image>();
        var fx=root.GetComponent<HeroSpawnVfx>();
        var foot=LogicFootWorld(root);
        var text=new StringBuilder("[HeroRender] ").Append(stage)
            .Append(" actor=").Append(hero.gameObject.name).Append(" life=").Append(hero.SpawnVersion)
            .Append(" frame=").Append(Time.frameCount).Append(" time=").Append(N(Time.time))
            .Append(" dt=").Append(N(Time.deltaTime)).Append(" unscaledDt=").Append(N(Time.unscaledDeltaTime))
            .Append(" timeScale=").Append(N(Time.timeScale)).Append(" state=").Append(hero.CurrentState)
            .Append(" moved=").Append(N(hero.MovementDistanceThisFrame))
            .Append(" movementFrame=").Append(hero.MovementFrame)
            .Append(" movementInputDt=").Append(N(hero.MovementInputDeltaThisFrame))
            .Append(" movementDt=").Append(N(hero.MovementDeltaThisFrame))
            .Append(" discardedMovementDt=").Append(N(hero.MovementInputDeltaThisFrame-hero.MovementDeltaThisFrame))
            .Append(hero.DescribeLaneTrace())
            .Append(" mode=").Append(hero.attackMode).Append(" pose=").Append(bridge!=null?bridge.CurrentPose.ToString():"fallback")
            .Append(" window=").Append(Screen.width).Append('x').Append(Screen.height)
            .Append(" canvasPath=").Append(canvas!=null?Path(canvas.transform):"none")
            .Append(" canvasScale=").Append(canvas!=null?N(canvas.scaleFactor):"none")
            .Append(" renderMode=").Append(canvas!=null?canvas.renderMode.ToString():"none")
            .Append(" projectionCamera=").Append(camera!=null?Path(camera.transform):"overlay-or-no-camera");
        if(camera!=null)text.Append(" cameraWorld=").Append(V(camera.transform.position));
        Point(text,"logicPivot",root.position,area,camera);
        Point(text,"logicFoot",foot,area,camera);
        RectInfo(text,"hero",root,camera);
        ImageInfo(text,"body",body,bridge!=null?bridge.BodyReferenceFootUV:new Vector2(.5f,0f),foot,area,camera);
        ImageInfo(text,"vfx",fx!=null?fx.EffectGraphic:null,new Vector2(.5f,.09f),foot,area,camera);
        ImageInfo(text,"glow",fx!=null?fx.GlowGraphic:null,new Vector2(.5f,.5f),foot,area,camera);
        if(fx!=null)text.Append(" vfxPlaying=").Append(fx.IsPlaying).Append(" vfxElapsed=").Append(N(fx.Elapsed))
            .Append(" displayedVfxFrame=").Append(fx.DisplayedFrame);
        text.Append(" ancestry=[");
        for(Transform parent=root.parent;parent!=null;parent=parent.parent)
        {
            text.Append(" {");TransformInfo(text,parent);
            var rect=parent as RectTransform;
            if(rect!=null)RectLayout(text,rect);
            text.Append('}');
        }
        text.Append(']');
        if(area!=null)
        {
            RectInfo(text,"ground",area,camera);
            var terrain=area.GetComponent<GroundPresentation>();
            if(terrain!=null)text.Append(" terrainPhase=").Append(N(terrain.TilePhase))
                .Append(" farPhase=").Append(N(terrain.FarPhase)).Append(" distantPhase=").Append(N(terrain.DistantPhase))
                .Append(terrain.DescribeLayoutTrace());
        }
        if(space!=null)text.Append(" pan=").Append(N(space.LastAppliedCameraPan))
            .Append(" cameraSpeed=").Append(N(space.CurrentCameraSpeed))
            .Append(" backgroundSpeed=").Append(N(space.CurrentBackgroundSpeed)).Append(space.DescribeCameraTrace());
        return text.ToString();
    }
    private static void ImageInfo(StringBuilder text,string label,Image image,Vector2 uv,Vector3 foot,
        RectTransform area,Camera camera)
    {
        if(image==null) { text.Append(' ').Append(label).Append("=none");return; }
        var canvas=FindCanvas(image.rectTransform);
        var imageCamera=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
        text.Append(' ').Append(label).Append("Graphic={enabled=").Append(image.enabled)
            .Append(" active=").Append(image.gameObject.activeInHierarchy).Append(" alpha=").Append(N(image.color.a))
            .Append(" canvasPath=").Append(canvas!=null?Path(canvas.transform):"none")
            .Append(" canvasScale=").Append(canvas!=null?N(canvas.scaleFactor):"none")
            .Append(" renderMode=").Append(canvas!=null?canvas.renderMode.ToString():"none")
            .Append(" projectionCamera=").Append(imageCamera!=null?Path(imageCamera.transform):"overlay-or-no-camera")
            .Append(" preserveAspect=").Append(image.preserveAspect).Append(" referenceUV=").Append(V(uv));
        if(image.sprite!=null)text.Append(" spriteRect=").Append(V(image.sprite.rect.size))
            .Append(" textureSize=").Append(image.sprite.texture.width).Append('x').Append(image.sprite.texture.height);
        var reference=ImageReferenceWorld(image,uv,false);
        var drawnReference=ImageReferenceWorld(image,uv,true);
        // Both projections produce screen pixels. If the body was unexpectedly
        // reparented to another Canvas, do not silently use the Hero's camera.
        Point(text,"reference",reference,area,imageCamera);
        Point(text,"pixelReference",drawnReference,area,imageCamera);
        var drawn=RectTransformUtility.WorldToScreenPoint(imageCamera,drawnReference);
        text.Append(" footDeltaScreen=").Append(V(drawn-RectTransformUtility.WorldToScreenPoint(camera,foot)));
        text.Append('}');
        RectInfo(text,label,image.rectTransform,imageCamera);
    }
    private static void Point(StringBuilder text,string label,Vector3 world,RectTransform area,Camera camera)
    {
        text.Append(' ').Append(label).Append("World=").Append(V(world))
            .Append(' ').Append(label).Append("Screen=").Append(V(RectTransformUtility.WorldToScreenPoint(camera,world)));
        if(area!=null)text.Append(' ').Append(label).Append("Battle=").Append(V(area.InverseTransformPoint(world)));
    }
    private static void RectInfo(StringBuilder text,string label,RectTransform rect,Camera camera)
    {
        text.Append(' ').Append(label).Append("Rect={");TransformInfo(text,rect);RectLayout(text,rect);
        text.Append(" cornersScreen=");
        var bounds=rect.rect;
        foreach(var corner in new[]{new Vector3(bounds.xMin,bounds.yMin,0),new Vector3(bounds.xMin,bounds.yMax,0),
            new Vector3(bounds.xMax,bounds.yMax,0),new Vector3(bounds.xMax,bounds.yMin,0)})
            text.Append(V(RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(corner))));
        text.Append('}');
    }
    private static void RectLayout(StringBuilder text,RectTransform rect)
    {
        text.Append(" anchored=").Append(V(rect.anchoredPosition)).Append(" anchors=").Append(V(rect.anchorMin)).Append(V(rect.anchorMax))
            .Append(" pivot=").Append(V(rect.pivot)).Append(" sizeDelta=").Append(V(rect.sizeDelta))
            .Append(" rectSize=").Append(V(rect.rect.size));
    }
    private static void TransformInfo(StringBuilder text,Transform transform)
    {
        text.Append(" path=").Append(Path(transform)).Append(" world=").Append(V(transform.position))
            .Append(" local=").Append(V(transform.localPosition)).Append(" scale=").Append(V(transform.localScale))
            .Append(" worldBasisX=").Append(V(transform.TransformVector(new Vector3(1,0,0))))
            .Append(" worldBasisY=").Append(V(transform.TransformVector(new Vector3(0,1,0))));
        var group=transform.GetComponent<CanvasGroup>();
        if(group!=null)text.Append(" groupAlpha=").Append(N(group.alpha));
    }
    private static Canvas FindCanvas(Transform transform)
    {
        // Use rootCanvas for the actual rendering camera when nested canvases exist.
        Canvas canvas=null;
        for(var parent=transform;parent!=null;parent=parent.parent)
        {
            var found=parent.GetComponent<Canvas>();if(found!=null)canvas=found;
        }
        return canvas;
    }
    private static string Path(Transform transform)
    {
        string path="";
        for(var parent=transform;parent!=null;parent=parent.parent)
            path=parent.gameObject.name+(path.Length==0?"":"/"+path);
        return path;
    }
    private static string N(float value) { return value.ToString("R",CultureInfo.InvariantCulture); }
    private static string V(Vector2 v) { return "("+N(v.x)+","+N(v.y)+")"; }
    private static string V(Vector3 v) { return "("+N(v.x)+","+N(v.y)+","+N(v.z)+")"; }
}
