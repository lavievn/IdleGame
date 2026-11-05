using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

// W2 sprite-preview bridge. Presentation only; it never changes movement, AI or combat state.
public sealed class CultivationSpriteBridge : MonoBehaviour
{
    public enum Pose { Normal, Attack, Hit }
    // Presentation windows only. CombatManager owns damage, projectiles and
    // attack cadence; these durations never grant damage immunity or delay a hit.
    private const float AttackDisplayDuration = .16f;
    private const float HitDisplayDuration = .2f;
    [Header("Presentation only (does not resize the combat RectTransform)")]
    [Min(.01f)] public float heroScale = 2f;
    [Min(.01f)] public float monsterScale = 2f;
    public bool heroFlipHorizontal = true;
    public bool monsterFlipHorizontal = false;

    // All preview PNGs share a 900x600 canvas and feet near (300,10), measured
    // from bottom-left. 480 source pixels of body height become 80 Ground units.
    // Keep this scale across poses: wide weapons/effects must not shrink the body.
    private const float SourcePixelsPerUnit = 6f;
    private static readonly Vector2 SourceFeet = new Vector2(300f, 10f);
    // GameManager clones the active Hero visual for B2 companions. Serialize
    // these references so Unity remaps them to the clone's existing body/UI,
    // rather than creating a second body or retaining the original actor's UI.
    [SerializeField, HideInInspector] private Image sourceImage;
    [SerializeField, HideInInspector] private bool sourceWasEnabled;
    [SerializeField, HideInInspector] private Image image;
    [SerializeField, HideInInspector] private Image healthFill;
    private bool enemy;
    private string classKey;
    private string fallbackClass;
    private bool normalTint,attackTint,hitTint;
    private Vector2 normalFoot=SourceFeet,attackFoot=SourceFeet,hitFoot=SourceFeet;
    private Vector2 FrameFoot => pose==Pose.Hit && hit!=null?hitFoot:
        pose==Pose.Attack && attack!=null?attackFoot:normalFoot;
    private Pose pose = Pose.Normal;
    private float returnAt;
    private Sprite normal, attack, hit;
    private Sprite[] normalFrames, attackFrames, hitFrames;
    private float poseStartedAt;
    private float bodyOpacity = 1f;
    public Image BodyGraphic => image != null && normal != null ? image : GetComponent<Image>();
    // Read-only diagnostic reference, not a measurement of opaque shoe pixels.
    // This is the same source registration used by ApplyLayout for every pose.
    public Vector2 BodyReferenceFootUV => image != null && normal != null && image.sprite != null
        ? new Vector2(FrameFoot.x/image.sprite.rect.width,FrameFoot.y/image.sprite.rect.height)
        : new Vector2(.5f,0f);
    public Pose CurrentPose => pose;
    public Vector2 VisibleBodySize => image != null && normal != null
        ? new Vector2(430f,480f) * (Mathf.Max(.01f,enemy?monsterScale:heroScale)/SourcePixelsPerUnit)
        : ((RectTransform)transform).rect.size;
    public static void SetBodyOpacity(RectTransform rect,float opacity)
    {
        if (rect == null) return;
        var bridge=rect.GetComponent<CultivationSpriteBridge>();
        var graphic=bridge!=null && bridge.image!=null && bridge.normal!=null
            ? bridge.image : rect.GetComponent<Image>();
        if(bridge!=null)bridge.bodyOpacity=Mathf.Clamp01(opacity);
        if(graphic!=null) { var c=graphic.color;c.a=Mathf.Clamp01(opacity);graphic.color=c; }
    }

    public static void AttachHero(HeroController controller)
    {
        if (controller == null || controller.heroRect == null) return;
        var bridge = controller.heroRect.GetComponent<CultivationSpriteBridge>();
        if (bridge == null) bridge = controller.heroRect.gameObject.AddComponent<CultivationSpriteBridge>();
        bridge.healthFill = controller.hpFillImage;
        bridge.Init(false, ToClass(controller.attackMode));
    }
    public static void AttachMonster(MonsterController controller)
    {
        if (controller == null || controller.Rect == null) return;
        controller.RefreshSpriteIdentity();
        var bridge = controller.Rect.GetComponent<CultivationSpriteBridge>();
        if (bridge == null) bridge = controller.Rect.gameObject.AddComponent<CultivationSpriteBridge>();
        bridge.healthFill = controller.hpFillImage;
        bridge.Init(true,controller.SpriteIdentityKey??ToClass(controller.attackMode),ToClass(controller.attackMode));
        SetMonsterFacing(controller.Rect, controller.FacingRight);
    }
    public static void SetMonsterFacing(RectTransform rect, bool right)
    {
        if (rect == null) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge == null) return;
        bridge.monsterFlipHorizontal = !right;
        bridge.ApplyLayout(); // Mirror only the body child, never HP/text/root.
    }
    public static void Attack(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge != null) bridge.Show(Pose.Attack, AttackDisplayDuration);
    }
    public static void Hit(RectTransform rect)
    {
        if (rect == null || !rect.gameObject.activeInHierarchy) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge != null) bridge.Show(Pose.Hit, HitDisplayDuration);
    }
    public static void ResetPose(RectTransform rect)
    {
        if (rect == null) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge != null) bridge.ResetPose();
    }
    private static string ToClass(AttackMode mode)
    {
        return mode == AttackMode.RangedPhysical ? "Archer" :
            mode == AttackMode.RangedMagic ? "Mage" : "Swordsman";
    }
    private void Init(bool isEnemy, string type,string fallback=null)
    {
        fallback=fallback??type;
        if (sourceImage == null)
        {
            sourceImage = GetComponent<Image>();
            if (sourceImage == null) return;
            sourceWasEnabled = sourceImage.enabled;
        }
        if (enemy != isEnemy || classKey != type || fallbackClass!=fallback || normal == null)
        {
            ReleaseSprites();
            enemy = isEnemy;
            classKey = type;
            fallbackClass=fallback;
            // Enemy PNGs are the same class/pose artwork with a baked 30% dark
            // red overlay. Keep white Image tint so that it is not applied twice.
            string prefix = enemy ? "Enemy_" : "";
            string path = "CultivationSprites/" + prefix + type;
            // Three-frame Archer animations. Other roles keep the existing
            // single-frame presentation without any behavior change.
            if(!enemy && type=="Archer")
            {
                normalFrames=LoadSequence(path+"_Normal");
                attackFrames=LoadSequence(path+"_Attack");
                hitFrames=LoadSequence(path+"_Hit");
            }
            // Load each fallback only if its complete sequence is unavailable.
            // Loading it first leaked a created Sprite when frame[0] replaced it.
            normalFoot=attackFoot=hitFoot=SourceFeet;
            normalTint=attackTint=hitTint=false;
            if(enemy)
            {
                normal=LoadEnemyFrame(type,fallback,"Normal",out normalTint,out normalFoot);
                attack=LoadEnemyFrame(type,fallback,"Attack",out attackTint,out attackFoot);
                hit=LoadEnemyFrame(type,fallback,"Hit",out hitTint,out hitFoot);
            }
            else
            {
                normal=normalFrames!=null?normalFrames[0]:LoadFrame(path+"_Normal");
                attack=attackFrames!=null?attackFrames[0]:LoadFrame(path+"_Attack");
                hit=hitFrames!=null?hitFrames[0]:LoadFrame(path+"_Hit");
            }
        }
        if (normal == null)
        {
            // Missing preview assets must not hide the original actor image.
            sourceImage.enabled = sourceWasEnabled;
            if (image != null) image.enabled = false;
            return;
        }
        if (image == null)
        {
            var body = new GameObject("Cultivation Sprite Body", typeof(RectTransform), typeof(Image));
            body.layer = gameObject.layer;
            body.transform.SetParent(transform, false);
            body.transform.SetAsFirstSibling();
            image = body.GetComponent<Image>();
            image.raycastTarget = false;
        }
        // Only replace the body graphic. Existing bars/text stay on the logical
        // actor root and are never mirrored or resized by the preview bridge.
        sourceImage.enabled = false;
        image.enabled = true;
        ResetPose();
    }
    private static Sprite LoadFrame(string path)
    {
        // Uploaded PNGs use Multiple/automatic slices, including tiny detached
        // effects. Load the texture, not an unspecified first Sprite subasset.
        var texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
    }
    private static Sprite LoadEnemyFrame(string identity,string role,string poseName,out bool tint,out Vector2 foot)
    {
        tint=false;foot=SourceFeet;
        var frame=LoadFrame("CultivationSprites/Enemy_"+identity+"_"+poseName);
        if(frame!=null)
        {
            // Existing Wolf PNG soles are at source row 565/566 of 600,
            // unlike the Hero registration. Only the body child pivot changes.
            if(identity=="Wolf")foot=new Vector2(300f,34f);
            return frame;
        }
        if(identity!=role)frame=LoadFrame("CultivationSprites/Enemy_"+role+"_"+poseName);
        if(frame!=null)return frame; // Authored/baked enemy color: never tint twice.
        if(identity!=role)
        {
            frame=LoadFrame("CultivationSprites/"+identity+"_"+poseName);
            if(frame!=null)
            {
                tint=true;if(identity=="Wolf")foot=new Vector2(300f,34f);
                return frame;
            }
        }
        frame=LoadFrame("CultivationSprites/"+role+"_"+poseName);
        tint=frame!=null;
        return frame;
    }
    private static Sprite[] LoadSequence(string path)
    {
        var frames=new Sprite[3];
        for(int i=0;i<3;i++)
        {
            frames[i]=LoadFrame(path+"_"+(i+1));
            if(frames[i]==null)
            {
                for(int j=0;j<=i;j++)if(frames[j]!=null)Destroy(frames[j]);
                return null;
            }
        }
        return frames;
    }
    private void Show(Pose requested, float duration)
    {
        if (image == null || !isActiveAndEnabled) return;
        // Ignore (do not queue) ordinary hits throughout an Attack display.
        // Only a NEW attack callback can start a new Attack display window.
        if (pose == Pose.Attack && requested == Pose.Hit && Time.time < returnAt) return;
        // Very fast, valid attacks still deal damage/fire normally, but do not
        // keep rewinding the Archer to frame 1 or extend this visual window.
        if(attackFrames!=null && requested==Pose.Attack && pose==Pose.Attack && Time.time<returnAt)return;
        pose = requested;
        poseStartedAt = Time.time;
        returnAt = Time.time + duration;
        Apply();
    }
    private void ResetPose()
    {
        pose = Pose.Normal;
        poseStartedAt = Time.time;
        returnAt = 0f;
        Apply();
    }
    private void OnDisable()
    {
        ResetPose();
    }
    private void Update()
    {
        if (image == null) return;
        if (pose != Pose.Normal && Time.time >= returnAt)
        {
            pose = Pose.Normal;
            poseStartedAt = returnAt;
            Apply();
        }
        if(normalFrames!=null)Apply();
        else ApplyLayout();
    }
    private void Apply()
    {
        if (image == null || normal == null) return;
        var frames=pose==Pose.Hit?hitFrames:pose==Pose.Attack?attackFrames:normalFrames;
        if(frames!=null)
        {
            float duration=pose==Pose.Hit?HitDisplayDuration:
                pose==Pose.Attack?AttackDisplayDuration:.42f;
            int index=pose==Pose.Normal
                ? Mathf.FloorToInt(Mathf.Max(0f,Time.time-poseStartedAt)/(.42f/3f))%3
                : Mathf.Clamp(Mathf.FloorToInt((Time.time-poseStartedAt)/
                    (duration/3f)),0,2);
            image.sprite=frames[index];
        }
        else image.sprite = pose == Pose.Hit && hit != null ? hit :
                       pose == Pose.Attack && attack != null ? attack : normal;
        bool fallbackTint=pose==Pose.Hit && hit!=null?hitTint:pose==Pose.Attack && attack!=null?attackTint:normalTint;
        image.color=fallbackTint?new Color(.8f,.7f,.7f,1f):Color.white;
        var tint=image.color;tint.a=bodyOpacity;image.color=tint;
        image.preserveAspect = false; // rect uses the full frame's exact aspect ratio.
        ApplyLayout();
    }
    private void ApplyLayout()
    {
        if (image == null || image.sprite == null) return;
        Rect frame = image.sprite.rect;
        var body = image.rectTransform;
        body.anchorMin = body.anchorMax = new Vector2(.5f, 0f);
        body.pivot = new Vector2(FrameFoot.x / frame.width, FrameFoot.y / frame.height);
        body.anchoredPosition = Vector2.zero;
        body.sizeDelta = new Vector2(frame.width / SourcePixelsPerUnit, frame.height / SourcePixelsPerUnit);
        float scale = Mathf.Max(.01f, enemy ? monsterScale : heroScale);
        bool flip = enemy ? monsterFlipHorizontal : heroFlipHorizontal;
        body.localScale = new Vector3(flip ? -scale : scale, scale, 1f);
        // Keep the existing health bar above the enlarged silhouette without
        // scaling its width/fill/text or changing the actor's logical root.
        if (healthFill != null)
        {
            var bar = healthFill.transform.parent as RectTransform;
            if (bar == transform) bar = healthFill.rectTransform;
            bool owned = false;
            for (Transform p = bar; p != null; p = p.parent)
                if (p == transform) { owned = true; break; }
            if (owned && bar != null && bar != transform)
            {
                Vector3 local = transform.InverseTransformPoint(bar.position);
                local.y = VisibleBodySize.y + 6f;
                bar.position = transform.TransformPoint(local);
            }
        }
    }
    private void ReleaseSprites()
    {
        if(normalFrames!=null) { foreach(var frame in normalFrames) if(frame!=null)Destroy(frame); }
        else if(normal!=null)Destroy(normal);
        if(attackFrames!=null) { foreach(var frame in attackFrames) if(frame!=null)Destroy(frame); }
        else if(attack!=null)Destroy(attack);
        if(hitFrames!=null) { foreach(var frame in hitFrames) if(frame!=null)Destroy(frame); }
        else if(hit!=null)Destroy(hit);
        normalFrames=attackFrames=hitFrames=null;
        normal=attack=hit=null;
    }
    private void OnDestroy()
    {
        ReleaseSprites();
        if (image != null) Destroy(image.gameObject);
        if (sourceImage != null) sourceImage.enabled = sourceWasEnabled;
    }
}
