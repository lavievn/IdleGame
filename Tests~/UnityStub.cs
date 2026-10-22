// Minimal test double for headless logic tests. NOT a Unity implementation.
// Unity rendering, serialization, native callbacks and coroutines still need Play Mode.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    public class Object
    {
        public static List<Object> objects = new List<Object>();
        public Object() { objects.Add(this); }
        public static T FindFirstObjectByType<T>() where T : Object { return objects.Find(x => x is T) as T; }
        public static T Instantiate<T>(T source) where T : Object { return (T)source.MemberwiseClone(); }
        public static GameObject Instantiate(GameObject source, Transform parent) {
            var g = new GameObject(source.transform is RectTransform);g.name=source.name;g.transform.parent=parent;
            foreach(var c in source.components) { if(c is Transform)continue; var copy=(Component)Activator.CreateInstance(c.GetType());
                foreach(var f in c.GetType().GetFields()) f.SetValue(copy,f.GetValue(c));copy.gameObject=g;g.components.Add(copy); }
            foreach(Transform child in source.transform)Instantiate(child.gameObject,g.transform);return g;
        }
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sort) where T:Object { var list=new List<T>();foreach(var o in objects)if(o is T)list.Add((T)o);return list.ToArray(); }
        public static T FindObjectOfType<T>() where T : Object { return FindFirstObjectByType<T>(); }
        public static void DontDestroyOnLoad(Object o) { }
        public static void Destroy(Object value) { objects.Remove(value); }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : Component { return gameObject.GetComponent<T>(); }
        public T GetComponentInChildren<T>(bool inactive=false) where T:Component { return gameObject.GetComponentInChildren<T>(inactive); }
        public T[] GetComponentsInChildren<T>(bool inactive=false) where T:Component { return gameObject.GetComponentsInChildren<T>(inactive); }
    }
    public class MonoBehaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled { get { return enabled && gameObject.activeInHierarchy; } }
        public Coroutine StartCoroutine(IEnumerator routine) { var c = new Coroutine { routine = routine }; routine.MoveNext(); return c; }
        public void StopAllCoroutines() { }
        public void StopCoroutine(Coroutine coroutine) { coroutine.stopped = true; }
    }
    public class Camera : Component { }
    public class Coroutine { public IEnumerator routine; public bool stopped; }
    public class WaitForSeconds { public float seconds; public WaitForSeconds(float seconds) { this.seconds = seconds; } }
    public class WaitForSecondsRealtime { public float seconds; public WaitForSecondsRealtime(float seconds) { this.seconds = seconds; } }
    public class ScriptableObject : Object { }
    public class GameObject : Object
    {
        public int layer;
        public string name = "New Game Object";
        public bool activeSelf = true;
        public bool activeInHierarchy { get { return activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy); } }
        public Transform transform;
        public List<Component> components = new List<Component>();
        public GameObject(bool rect = false) { transform = rect ? (Transform)new RectTransform() : new Transform(); transform.gameObject = this; components.Add(transform); }
        public GameObject(string name, params Type[] types) : this(true) { this.name=name;foreach(var type in types)if(type!=typeof(RectTransform)) { var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components.Add(c); } }
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; components.Add(c); return c; }
        public T GetComponent<T>() where T : Component { return components.Find(x => x is T) as T; }
        public T GetComponentInChildren<T>(bool inactive=false) where T:Component { var all=GetComponentsInChildren<T>(inactive);return all.Length>0?all[0]:null; }
        public T[] GetComponentsInChildren<T>(bool inactive=false) where T:Component {
            var list=new List<T>();if(inactive||activeInHierarchy)foreach(var c in components)if(c is T)list.Add((T)c);
            foreach(Transform child in transform)list.AddRange(child.gameObject.GetComponentsInChildren<T>(inactive));return list.ToArray(); }
        public void SetActive(bool value)
        {
            if (value == activeSelf) return;
            activeSelf = value;
            foreach (var c in components)
                c.GetType().GetMethod(value ? "OnEnable" : "OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(c, null);
        }
    }
    public class Transform : Component, IEnumerable
    {
        public Transform parent;
        public bool IsChildOf(Transform other) { for(var t=this;t!=null;t=t.parent)if(t==other)return true;return false; }
        public IEnumerator GetEnumerator() { var list=new List<Transform>();foreach(var o in Object.objects) { var t=o as Transform;if(t!=null&&t.parent==this)list.Add(t); }return list.GetEnumerator(); }
        public void SetParent(Transform value, bool worldPositionStays) { parent = value; }
        public void SetAsLastSibling() { }
        public void SetAsFirstSibling() { }
        public void SetSiblingIndex(int index) { }
        private Vector3 local;
        public virtual Vector3 localPosition { get { return local; } set { local = value; } }
        public Vector3 localScale = Vector3.one;
        public Vector3 position { get { return parent == null ? localPosition : parent.TransformPoint(localPosition); } set { localPosition = parent == null ? value : parent.InverseTransformPoint(value); } }
        public Vector3 TransformPoint(Vector3 p) { p = localPosition + Vector3.Scale(p, localScale); return parent == null ? p : parent.TransformPoint(p); }
        public Vector3 InverseTransformPoint(Vector3 p) { if (parent != null) p = parent.InverseTransformPoint(p); p -= localPosition; return new Vector3(p.x/localScale.x,p.y/localScale.y,p.z/localScale.z); }
        public Vector3 TransformVector(Vector3 v) { v = Vector3.Scale(v, localScale); return parent == null ? v : parent.TransformVector(v); }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot = new Vector2(0.5f, 0f), sizeDelta = new Vector2(50f, 100f), anchoredPosition;
        public Vector2 offsetMin, offsetMax;
        public Rect rect { get { var p=parent as RectTransform; float w=sizeDelta.x+(p==null?0:p.rect.width*(anchorMax.x-anchorMin.x)); float h=sizeDelta.y+(p==null?0:p.rect.height*(anchorMax.y-anchorMin.y)); return new Rect(-pivot.x*w,-pivot.y*h,w,h); } }
        private Vector3 anchor { get { var p = parent as RectTransform; return p == null ? Vector3.zero : new Vector3(p.rect.xMin+p.rect.width*(anchorMin.x+anchorMax.x)/2f,p.rect.yMin+p.rect.height*(anchorMin.y+anchorMax.y)/2f,0f); } }
        public override Vector3 localPosition { get { return anchor+(Vector3)anchoredPosition; } set { anchoredPosition = value-anchor; } }
    }
    public struct Rect
    {
        public float xMin,yMin,width,height;
        public Rect(float x,float y,float w,float h) { xMin=x;yMin=y;width=w;height=h; }
        public Vector2 size { get { return new Vector2(width,height); } }
        public float xMax { get { return xMin+width; } }
        public Vector2 center { get { return new Vector2(xMin+width/2,yMin+height/2); } }
    }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x;this.y=y; }
        public static Vector2 one { get { return new Vector2(1,1); } }
        public static Vector2 zero { get { return new Vector2(); } }
        public static Vector2 operator +(Vector2 a,Vector2 b) { return new Vector2(a.x+b.x,a.y+b.y); }
        public static Vector2 operator -(Vector2 a,Vector2 b) { return new Vector2(a.x-b.x,a.y-b.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x,v.y,0); }
        public static Vector2 operator *(Vector2 a,float b) { return new Vector2(a.x*b,a.y*b); }
        public float sqrMagnitude { get { return x*x+y*y; } }
        public float magnitude { get { return (float)System.Math.Sqrt(sqrMagnitude); } }
        public Vector2 normalized { get { var len=magnitude;return len>0f?new Vector2(x/len,y/len):zero; } }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 one { get { return new Vector3(1,1,1); } }
        public static Vector3 zero { get { return new Vector3(); } }
        public static Vector3 Scale(Vector3 a,Vector3 b) { return new Vector3(a.x*b.x,a.y*b.y,a.z*b.z); }
        public static Vector3 operator +(Vector3 a,Vector3 b) { return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
        public static Vector3 operator -(Vector3 a,Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
        public static Vector3 operator *(Vector3 a,float f) { return new Vector3(a.x*f,a.y*f,a.z*f); }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x,v.y); }
    }
    public static class Mathf
    {
        public static float Abs(float a) { return Math.Abs(a); }
        public static float Sin(float a) { return (float)Math.Sin(a); }
        public static float Max(float a,float b) { return Math.Max(a,b); }
        public static float Min(float a,float b) { return Math.Min(a,b); }
        public static int Min(int a,int b) { return Math.Min(a,b); }
        public static float Sqrt(float x) { return (float)Math.Sqrt(x); }
        public static float Clamp01(float a) { return Math.Max(0f,Math.Min(1f,a)); }
        public static int Max(int a,int b) { return Math.Max(a,b); }
        public static float Clamp(float value,float min,float max) { return Math.Max(min,Math.Min(max,value)); }
        public static int Clamp(int a,int b,int c) { return Math.Max(b,Math.Min(a,c)); }
        public static float MoveTowards(float a,float b,float d) { return a+Math.Sign(b-a)*Math.Min(Math.Abs(b-a),d); }
        public static float Lerp(float a,float b,float t) { return a+(b-a)*Clamp01(t); }
        public static int RoundToInt(float a) { return (int)Math.Round(a); }
    }
    public static class Random
    {
        private static System.Random random = new System.Random(1234);
        public static void InitState(int seed) { random = new System.Random(seed); }
        public static int Range(int a,int b) { return random.Next(a,b); }
        public static float Range(float a,float b) { return a+(b-a)*(float)random.NextDouble(); }
    }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1f) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public static Color white { get { return new Color(1f,1f,1f,1f); } }
        public static Color black { get { return new Color(0f,0f,0f,1f); } }
        public static Color gray { get { return new Color(.5f,.5f,.5f,1f); } }
    }
    public class CanvasRenderer : Component { }
    public class CanvasGroup : MonoBehaviour { public float alpha=1f; public bool interactable=true,blocksRaycasts=true; }
    public class Sprite : Object
    {
        public Texture2D texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
        public Rect textureRect = new Rect(0,0,2,2);
        public Rect rect = new Rect(0,0,2,2);
        public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float pixelsPerUnit,
            uint extrude=0,SpriteMeshType meshType=SpriteMeshType.Tight)
        {
            return new Sprite { texture=texture,rect=rect,textureRect=rect };
        }
    }
    public enum SpriteMeshType { FullRect, Tight }
    public class TextAsset : Object
    {
        public string text;
        public TextAsset(string text) { this.text = text; }
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public class Texture : Object { public int width,height; }
    public enum TextureFormat { RGBA32 }
    public class Texture2D : Texture
    {
        public Texture2D(int w,int h,TextureFormat format,bool mipmap){width=w;height=h;}
        public Color32[] pixels;
        public void SetPixels32(Color32[] colors) { pixels=colors; }
        public void Apply(bool updateMipmaps=true) {}
    }
    public static class ImageConversion
    {
        public static bool LoadImage(Texture2D image,byte[] data,bool markNonReadable=false)
        {
            if(image==null||data==null||data.Length==0)return false;
            image.width=600;image.height=900;return true;
        }
    }
    public class Shader : Object { }
    public class Material : Object
    {
        public string name;
        public Material(Shader shader){}
        public void SetFloat(string key,float value){}
        public void SetVector(string key,Vector4 vector){}
    }
    public struct Vector4
    {
        public float x,y,z,w;
        public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
    }
    public enum RenderTextureFormat { ARGB32 }
    public class RenderTexture : Texture
    {
        public int depth;public RenderTextureFormat format;
        public RenderTexture(int w,int h,int d,RenderTextureFormat f){width=w;height=h;depth=d;format=f;}
        public void Create(){} public void Release(){}
    }
    public enum RenderMode { ScreenSpaceOverlay }
    public class Canvas : Component { public RenderMode renderMode; public int sortingOrder,sortingLayerID,targetDisplay,additionalShaderChannels; public float scaleFactor; public static void ForceUpdateCanvases() { } }
    public static class Time { public static float deltaTime=1f/60f,unscaledDeltaTime=1f/60f,time,timeScale=1f; public static int frameCount; }
    public class AudioClip : Object { public float length = 180f; }
    public class AudioSource : MonoBehaviour
    {
        public bool playOnAwake,loop,isPlaying;
        public float spatialBlend,volume,time;
        public AudioClip clip;
        public void Play() { isPlaying = clip != null; time = 0f; }
        public void Stop() { isPlaying = false; }
    }
    public static class Resources
    {
        private static readonly Dictionary<string,Object> entries=new Dictionary<string,Object>();
        public static T Load<T>(string path) where T : Object
        {
            Object value; return entries.TryGetValue(path,out value)?value as T:null;
        }
        public static void Register(string path,Object value) {entries[path]=value;}
        public static void Unregister(string path){entries.Remove(path);}
    }
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string,float> floats=new Dictionary<string,float>();
        public static float GetFloat(string key,float fallback=0f) { float v;return floats.TryGetValue(key,out v)?v:fallback; }
        public static void SetFloat(string key,float value) { floats[key]=value; }
        public static void Save() { }
    }
    public class SerializeField : Attribute { }
    public class HideInInspector : Attribute { }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public class ContextMenu : Attribute { public ContextMenu(string s) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a,float b) { } }
    public class MinAttribute : Attribute { public MinAttribute(float a) { } }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int v) { } }
    public class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public class CreateAssetMenuAttribute : Attribute { public string fileName,menuName; }
}
namespace UnityEngine.UI
{
    public class CanvasScaler : UnityEngine.MonoBehaviour {
      public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
      public ScaleMode uiScaleMode; public UnityEngine.Vector2 referenceResolution;
    }
    public class Graphic : UnityEngine.MonoBehaviour {
      public bool raycastTarget;public UnityEngine.Color color=UnityEngine.Color.white;
      public UnityEngine.RectTransform rectTransform { get { return (UnityEngine.RectTransform)transform; } }
    }
    public class RawImage : MaskableGraphic { public UnityEngine.Texture texture; }
    public class Image : MaskableGraphic {
      public enum Type { Simple, Sliced }
      public float fillAmount;public UnityEngine.Sprite sprite;public Type type;public bool preserveAspect;
      public UnityEngine.Material material;
    }
    public class GraphicRaycaster : UnityEngine.Component { }
    public class RectMask2D : UnityEngine.Component { }
    public class Button : UnityEngine.MonoBehaviour { public bool interactable=true; public class ButtonClickedEvent:UnityEngine.Events.UnityEvent {} public ButtonClickedEvent onClick=new ButtonClickedEvent(); }
    public class MaskableGraphic : Graphic
    {
        protected static readonly UnityEngine.Texture s_WhiteTexture = new UnityEngine.Texture();
        public virtual UnityEngine.Texture mainTexture { get { return s_WhiteTexture; } }
        public void SetVerticesDirty() { }
        public void SetAllDirty() { }
        protected virtual void OnPopulateMesh(VertexHelper vh) { }
    }
    public struct UIVertex
    {
        public UnityEngine.Vector3 position;
        public UnityEngine.Color color;
        public UnityEngine.Vector2 uv0;
        public static UIVertex simpleVert { get { return new UIVertex(); } }
    }
    public class VertexHelper
    {
        public readonly List<UnityEngine.Vector3> vertices = new List<UnityEngine.Vector3>();
        public readonly List<int> triangles = new List<int>();
        public int currentVertCount { get { return vertices.Count; } }
        public void Clear() { vertices.Clear(); triangles.Clear(); }
        public void AddVert(UnityEngine.Vector3 p,UnityEngine.Color c,UnityEngine.Vector2 uv) { vertices.Add(p); }
        public void AddVert(UIVertex vertex) { vertices.Add(vertex.position); }
        public void AddTriangle(int a,int b,int c) { triangles.Add(a);triangles.Add(b);triangles.Add(c); }
    }
}
namespace TMPro {
 public enum FontStyles { Normal, Bold }
 public enum FontWeight { Regular }
 public enum TextOverflowModes { Ellipsis, Overflow }
 public class TMP_FontAsset { public object material; }
 public class TMP_InputField : UnityEngine.MonoBehaviour {
  public enum ContentType { Standard, DecimalNumber } public enum LineType { SingleLine }
  public string text; public bool isFocused; public ContentType contentType;public LineType lineType;
  public int characterLimit;public float pointSize;public UnityEngine.UI.Image targetGraphic;
  public UnityEngine.RectTransform textViewport;public TextMeshProUGUI textComponent;
  public void ActivateInputField(){isFocused=true;}
 }
 public enum TextAlignmentOptions { Center, TopLeft, Left, BottomRight, TopRight, MidlineLeft }
 public class TextMeshProUGUI : UnityEngine.UI.MaskableGraphic { public TMP_FontAsset font=new TMP_FontAsset(); public object fontSharedMaterial; public FontStyles fontStyle; public FontWeight fontWeight;public TextOverflowModes overflowMode;public bool enableWordWrapping; public string text; public float alpha,fontSize,fontSizeMin,fontSizeMax; public bool enableAutoSizing; public TextAlignmentOptions alignment; }
}
namespace UnityEngine.Pool
{
    public interface IObjectPool<T> { T Get(); void Release(T value); }
    public class ObjectPool<T> : IObjectPool<T>
    {
        private Func<T> create; private Action<T> get, release;
        public ObjectPool(Func<T> createFunc,Action<T> actionOnGet,Action<T> actionOnRelease,Action<T> actionOnDestroy,int defaultCapacity,int maxSize) { create=createFunc;get=actionOnGet;release=actionOnRelease; }
        public T Get() { T value=create();get(value);return value; }
        public void Release(T value) { release(value); }
    }
}

namespace UnityEngine.Events {
 public delegate void UnityAction();
 public class UnityEvent { private UnityAction callback;public void AddListener(UnityAction a){callback+=a;} public void Invoke(){if(callback!=null)callback();} }
}
namespace UnityEngine {
 public static class Application { public static string persistentDataPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"idle-tests-"+Guid.NewGuid());public static string streamingAssetsPath=System.IO.Path.GetTempPath();public static void Quit(){} }
 public static class Debug { public static void Log(string s){} public static void LogWarning(string s){} public static void LogError(string s){System.Console.Error.WriteLine(s); } }
 public static class ColorUtility { public static bool TryParseHtmlString(string s,out Color c){c=new Color();return true;} }
 public static class RectTransformUtility {
  public static bool RectangleContainsScreenPoint(RectTransform r,Vector2 p,object camera=null){ if(r==null)return false;var v=r.InverseTransformPoint(p);return v.x>=r.rect.xMin&&v.x<=r.rect.xMax&&v.y>=r.rect.yMin&&v.y<=r.rect.yMin+r.rect.height; }
  public static bool ScreenPointToLocalPointInRectangle(RectTransform r,Vector2 screen,object camera,out Vector2 local){local=r==null?Vector2.zero:(Vector2)r.InverseTransformPoint(screen);return r!=null;}
 }
 // Test JSON double for primitive fields and identity lists; not Unity serialization.
 public static class JsonUtility {
   public static T FromJson<T>(string json) where T : new() { return new T(); }
  public static string ToJson(object value,bool pretty){var fields=new List<string>();foreach(var f in value.GetType().GetFields()) {
   if(f.FieldType==typeof(int)||f.FieldType==typeof(float)||f.FieldType.IsEnum)fields.Add("\""+f.Name+"\":"+Convert.ToString(f.FieldType.IsEnum?(object)Convert.ToInt32(f.GetValue(value)):f.GetValue(value),System.Globalization.CultureInfo.InvariantCulture));
   else if(f.FieldType==typeof(List<int>) || f.FieldType==typeof(List<float>)) {
    var nums=new List<string>();var items=(System.Collections.IEnumerable)f.GetValue(value);
    if(items!=null)foreach(var item in items)nums.Add(Convert.ToString(item,System.Globalization.CultureInfo.InvariantCulture));
    fields.Add("\""+f.Name+"\":["+string.Join(",",nums.ToArray())+"]");
   }
   else if(f.FieldType==typeof(List<string>)) {
    var strings=(List<string>)f.GetValue(value);var arr=new List<string>();
    if(strings!=null)foreach(string item in strings)arr.Add("\""+item.Replace("\\","\\\\").Replace("\"","\\\"")+"\"");
    fields.Add("\""+f.Name+"\":["+string.Join(",",arr.ToArray())+"]");
   }
   else if(f.FieldType==typeof(string)) fields.Add("\""+f.Name+"\":\""+f.GetValue(value)+"\"");
   else if(f.FieldType==typeof(bool))
    fields.Add("\""+f.Name+"\":"+((bool)f.GetValue(value)?"true":"false"));
   else if(NestedEquipmentSaveStub.Supports(f.FieldType))
    fields.Add("\""+f.Name+"\":"+NestedEquipmentSaveStub.Encode(f.GetValue(value)));
   else if(f.FieldType==typeof(List<InventoryStack>)) {
    var items=(List<InventoryStack>)f.GetValue(value);var rows=new List<string>();
    if(items!=null)foreach(var item in items)if(item!=null)
     rows.Add("{\"itemId\":\""+item.itemId+"\",\"quantity\":"+item.quantity+"}");
    fields.Add("\""+f.Name+"\":["+string.Join(",",rows.ToArray())+"]");
   }
   else if(f.FieldType==typeof(List<TuTienCore.ElementType>)) {var roots=(List<TuTienCore.ElementType>)f.GetValue(value);var nums=new List<string>();if(roots!=null)foreach(var root in roots)nums.Add(((int)root).ToString());fields.Add("\""+f.Name+"\":["+string.Join(",",nums.ToArray())+"]");}
  }return "{"+string.Join(",",fields)+"}"; }
  public static void FromJsonOverwrite(string json,object value) { foreach(var f in value.GetType().GetFields()) {
   if(NestedEquipmentSaveStub.Supports(f.FieldType)) {
    NestedEquipmentSaveStub.ReadInto(json,value,f);continue;
   }
   if(f.FieldType==typeof(List<int>) || f.FieldType==typeof(List<float>)) {
    var arr=System.Text.RegularExpressions.Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*\\[([^]]*)\\]");
    if(arr.Success){var list=(System.Collections.IList)Activator.CreateInstance(f.FieldType);foreach(var item in arr.Groups[1].Value.Split(','))
     if(item.Trim().Length>0)list.Add(Convert.ChangeType(item.Trim(),f.FieldType.GetGenericArguments()[0],System.Globalization.CultureInfo.InvariantCulture));f.SetValue(value,list);}continue;
   }
   if(f.FieldType==typeof(List<string>)) {
    var match=System.Text.RegularExpressions.Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*\\[([^]]*)\\]");
    if(match.Success) {
     var values=new List<string>();
     foreach(System.Text.RegularExpressions.Match item in
       System.Text.RegularExpressions.Regex.Matches(match.Groups[1].Value,"\"([^\"]*)\""))
       values.Add(item.Groups[1].Value);
     f.SetValue(value,values);
    }
    continue;
   }
   if(f.FieldType==typeof(string)) {var str=System.Text.RegularExpressions.Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*\"([^\"]*)\"");if(str.Success)f.SetValue(value,str.Groups[1].Value);continue;}
   if(f.FieldType==typeof(bool)) {
    var flag=System.Text.RegularExpressions.Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*(true|false)");
    if(flag.Success)f.SetValue(value,flag.Groups[1].Value=="true");continue;
   }
   if(f.FieldType==typeof(List<InventoryStack>)) {
    var arr=System.Text.RegularExpressions.Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*\\[([^]]*)\\]");
    if(arr.Success) {
     var items=new List<InventoryStack>();
     foreach(System.Text.RegularExpressions.Match entry in
       System.Text.RegularExpressions.Regex.Matches(arr.Groups[1].Value,"\\{[^{}]*\\}")) {
      var id=System.Text.RegularExpressions.Regex.Match(entry.Value,"\"itemId\"\\s*:\\s*\"([^\"]*)\"");
      var qty=System.Text.RegularExpressions.Regex.Match(entry.Value,"\"quantity\"\\s*:\\s*([0-9]+)");
      if(id.Success&&qty.Success)items.Add(new InventoryStack {
       itemId=id.Groups[1].Value,quantity=int.Parse(qty.Groups[1].Value)
      });
     }
     f.SetValue(value,items);
    }
    continue;
   }
   if(f.FieldType==typeof(List<TuTienCore.ElementType>)) {var arr=System.Text.RegularExpressions.Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*\\[([^]]*)\\]");if(arr.Success){var roots=new List<TuTienCore.ElementType>();foreach(var item in arr.Groups[1].Value.Split(','))if(item.Trim().Length>0)roots.Add((TuTienCore.ElementType)int.Parse(item.Trim()));f.SetValue(value,roots);}continue;}
   var m=System.Text.RegularExpressions.Regex.Match(json,"\""+f.Name+"\"\\s*:\\s*(-?[0-9.]+)");if(!m.Success)continue;
   if(f.FieldType==typeof(int))f.SetValue(value,int.Parse(m.Groups[1].Value));
   else if(f.FieldType==typeof(float))f.SetValue(value,float.Parse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture));
   else if(f.FieldType.IsEnum)f.SetValue(value,Enum.ToObject(f.FieldType,int.Parse(m.Groups[1].Value))); } }
 }
}
namespace UnityEngine.InputSystem { public class Mouse { public static Mouse current;public Control leftButton=new Control(),position=new Control(); public class Control {public bool wasPressedThisFrame,wasReleasedThisFrame,isPressed;public UnityEngine.Vector2 value;public UnityEngine.Vector2 ReadValue(){return value;}} } }

namespace UnityEngine {
 public enum FindObjectsInactive { Include }
 public enum FindObjectsSortMode { None }
 public enum FullScreenMode { Windowed }
 public static class Screen { public static int width=1000,height=563;public static void SetResolution(int w,int h,FullScreenMode mode){width=w;height=h;} }
}

namespace UnityEngine.Video
{
    public enum VideoSource { VideoClip, Url }
    public enum VideoRenderMode { RenderTexture }
    public enum VideoAudioOutputMode { None }
    public class VideoClip : UnityEngine.Object { public double length=11.5; }
    public class VideoPlayer : UnityEngine.MonoBehaviour
    {
        public bool playOnAwake,isLooping,skipOnDrop,isPlaying;
        public VideoSource source;
        public VideoRenderMode renderMode;
        public VideoAudioOutputMode audioOutputMode;
        public VideoClip clip;
        public string url;
        public uint width=640,height=960;
        public UnityEngine.RenderTexture targetTexture;
        public event System.Action<VideoPlayer> prepareCompleted;
        public event System.Action<VideoPlayer,string> errorReceived;
        public void Prepare(){ if(prepareCompleted!=null)prepareCompleted(this); }
        public void Play(){isPlaying=true;}
        public void Stop(){isPlaying=false;}
        public void Pause(){isPlaying=false;}
    }
}
