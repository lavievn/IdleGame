using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

// W2 sprite-preview bridge. Presentation only; it never changes movement, AI or combat state.
public sealed class CultivationSpriteBridge : MonoBehaviour
{
    public const string DefaultHeroVisualKey="Swordsman";
    public const string DefaultHeroTexturePath="CultivationSprites/Swordsman_Normal";
    public enum Pose { Normal, Attack, Hit }
    public enum Motion { Idle, Walk, Run }
    // One path-based profile for every weapon. No serialized Sprite GUIDs are
    // needed: Unity imports the supplied full canvases as textures.
    [System.Serializable]
    private sealed class HeroAnimationProfile
    {
        public string idlePrefix="", walkPrefix="", runPrefix="", hitPrefix="";
        public int idleCount=0, walkCount=0, runCount=0, hitCount=0;
        public float idleFps=24f, walkFps=24f, runFps=24f, hitFps=30f;
        public float feetX=220f, feetY=17f, pixelsPerUnit=6f;
    }
    private HeroAnimationProfile heroProfile;
    private HeroController heroController;
    private Sprite[] walkFrames, runFrames;
    private Motion motion;
    private float motionStartedAt;
    private bool usingHeroFrame;
    public Motion CurrentMotion => motion;
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
    private Vector2 FrameFoot => usingHeroFrame
        ? new Vector2(heroProfile.feetX,heroProfile.feetY) : pose==Pose.Hit && hit!=null?hitFoot:
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
        bridge.heroController = controller;
        bridge.Init(false, DefaultHeroVisualKey); // Character identity is independent of weapon/combat mode.
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
                normal=LoadFrame(DefaultHeroTexturePath);
                attack=hit=null; // Preserve the current single Normal frame for unsupported actions.
                var profile=Resources.Load<TextAsset>("HeroAnimation/SharedHero");
                if(profile!=null)
                {
                    heroProfile=new HeroAnimationProfile();
                    JsonUtility.FromJsonOverwrite(profile.text,heroProfile);
                    normalFrames=LoadSequence(heroProfile.idlePrefix,heroProfile.idleCount);
                    walkFrames=LoadSequence(heroProfile.walkPrefix,heroProfile.walkCount);
                    runFrames=LoadSequence(heroProfile.runPrefix,heroProfile.runCount);
                    hitFrames=LoadSequence(heroProfile.hitPrefix,heroProfile.hitCount);
                }
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
    private static Sprite[] LoadSequence(string prefix,int count)
    {
        if(string.IsNullOrEmpty(prefix) || count<=0)return null;
        var frames=new Sprite[count];
        // Export numbering is zero-based, with a trailing five-digit index.
        // Direct paths make ordering deterministic regardless of import order.
        for(int i=0;i<count;i++)
        {
            frames[i]=LoadFrame(prefix+i.ToString("D5"));
            if(frames[i]==null)
            {
                foreach(var frame in frames)if(frame!=null)Destroy(frame);
                return null; // Never play a partial sequence.
            }
        }
        return frames;
    }
    private void UpdateMotion()
    {
        if(enemy || heroController==null || Time.timeScale<=0f)return;
        Motion next=Motion.Idle;
        if(heroController.IsDeployed && heroController.MovementFrame==Time.frameCount &&
            heroController.MovementDistanceThisFrame>.001f)
            next=heroController.IsExplorationMovement?Motion.Walk:Motion.Run;
        if(next==motion)return;
        motion=next;motionStartedAt=Time.time;
    }
    private Sprite[] MotionFrames => motion==Motion.Walk?walkFrames:
        motion==Motion.Run?runFrames:normalFrames;
    private float MotionFps => motion==Motion.Walk?heroProfile.walkFps:
        motion==Motion.Run?heroProfile.runFps:heroProfile.idleFps;
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
        // Hit is visual-only, plays once at the profile FPS, and can still be
        // interrupted by Attack. The Attack window remains exactly .16 seconds.
        if(requested==Pose.Hit && hitFrames!=null)
            duration=hitFrames.Length/Mathf.Max(.01f,heroProfile.hitFps);
        returnAt = Time.time + duration;
        Apply();
    }
    private void ResetPose()
    {
        pose = Pose.Normal;
        motion=Motion.Idle;
        motionStartedAt=Time.time;
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
        UpdateMotion();
        if (pose != Pose.Normal && Time.time >= returnAt)
        {
            pose = Pose.Normal;
            poseStartedAt = returnAt;
            Apply();
        }
        if(heroProfile!=null)Apply();
        else ApplyLayout();
    }
    private void Apply()
    {
        if (image == null || normal == null) return;
        var frames=pose==Pose.Hit?hitFrames:pose==Pose.Attack?attackFrames:MotionFrames;
        usingHeroFrame=!enemy && frames!=null;
        if(usingHeroFrame)
        {
            bool loop=pose==Pose.Normal; // Idle/Walk/Run loop; Hit is one-shot.
            float fps=loop?MotionFps:heroProfile.hitFps;
            float elapsed=Mathf.Max(0f,Time.time-(loop?motionStartedAt:poseStartedAt));
            int index=Mathf.FloorToInt(elapsed*Mathf.Max(.01f,fps));
            index=loop?index%frames.Length:Mathf.Min(index,frames.Length-1);
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
        float ppu=usingHeroFrame?Mathf.Max(.01f,heroProfile.pixelsPerUnit):SourcePixelsPerUnit;
        body.sizeDelta = new Vector2(frame.width / ppu, frame.height / ppu);
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
        foreach(var frames in new[]{normalFrames,walkFrames,runFrames,attackFrames,hitFrames})
            if(frames!=null)foreach(var frame in frames)if(frame!=null)Destroy(frame);
        if(normal!=null)Destroy(normal);
        if(attack!=null)Destroy(attack);
        if(hit!=null)Destroy(hit);
        normalFrames=walkFrames=runFrames=attackFrames=hitFrames=null;
        normal=attack=hit=null;
        heroProfile=null;usingHeroFrame=false;
    }
    private void OnDestroy()
    {
        ReleaseSprites();
        if (image != null) Destroy(image.gameObject);
        if (sourceImage != null) sourceImage.enabled = sourceWasEnabled;
    }
}
