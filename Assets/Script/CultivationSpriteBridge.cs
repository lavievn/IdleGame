using UnityEngine;
using UnityEngine.UI;
using TuTienCore;

// W2 sprite-preview bridge. Presentation only; it never changes movement, AI or combat state.
public sealed class CultivationSpriteBridge : MonoBehaviour
{
    public enum Pose { Normal, Attack, Hit }
    private Image image;
    private bool enemy;
    private string classKey;
    private Pose pose = Pose.Normal;
    private float returnAt;
    private int lastMode = -1;
    private Sprite normal, attack, hit;

    public static void AttachHero(HeroController controller)
    {
        if (controller == null || controller.heroRect == null) return;
        var bridge = controller.heroRect.GetComponent<CultivationSpriteBridge>();
        if (bridge == null) bridge = controller.heroRect.gameObject.AddComponent<CultivationSpriteBridge>();
        bridge.Init(false, ToClass(controller.attackMode));
    }
    public static void AttachMonster(MonsterController controller)
    {
        if (controller == null || controller.Rect == null) return;
        var bridge = controller.Rect.GetComponent<CultivationSpriteBridge>();
        if (bridge == null) bridge = controller.Rect.gameObject.AddComponent<CultivationSpriteBridge>();
        bridge.Init(true, ToClass(controller.attackMode));
    }
    public static void Attack(RectTransform rect)
    {
        if (rect == null) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge != null) bridge.Show(Pose.Attack, .16f);
    }
    public static void Hit(RectTransform rect)
    {
        if (rect == null) return;
        var bridge = rect.GetComponent<CultivationSpriteBridge>();
        if (bridge != null) bridge.Show(Pose.Hit, .2f);
    }
    private static string ToClass(AttackMode mode)
    {
        return mode == AttackMode.RangedPhysical ? "Archer" :
            mode == AttackMode.RangedMagic ? "Mage" : "Swordsman";
    }
    private void Init(bool isEnemy, string type)
    {
        enemy = isEnemy;
        image = GetComponent<Image>();
        if (image == null) return;
        if (classKey != type || normal == null)
        {
            classKey = type;
            string prefix = enemy ? "Enemy_" : "";
            normal = Resources.Load<Sprite>("CultivationSprites/" + prefix + type + "_Normal");
            attack = Resources.Load<Sprite>("CultivationSprites/" + prefix + type + "_Attack");
            hit = Resources.Load<Sprite>("CultivationSprites/" + prefix + type + "_Hit");
        }
        pose = Pose.Normal;
        returnAt = 0;
        Apply();
    }
    private void Show(Pose requested, float duration)
    {
        if (image == null) return;
        // Hit always overrides Attack; repeated hits refresh a fixed duration.
        if (pose == Pose.Hit && requested == Pose.Attack && Time.time < returnAt) return;
        pose = requested;
        returnAt = Time.time + duration;
        Apply();
    }
    private void Update()
    {
        if (image == null) return;
        if (pose != Pose.Normal && Time.time >= returnAt)
        {
            pose = Pose.Normal;
            Apply();
        }
    }
    private void Apply()
    {
        if (image == null || normal == null) return;
        image.sprite = pose == Pose.Hit && hit != null ? hit :
                       pose == Pose.Attack && attack != null ? attack : normal;
        image.color = Color.white; // Enemy tint is already baked into PNG.
        image.preserveAspect = true;
    }
}
