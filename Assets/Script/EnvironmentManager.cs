using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("MÔI TRƯỜNG")]
    [SerializeField] private RectTransform[] groundDetails; 
    public float scrollSpeed = 150f; 

    private float groundWidth;
    private int battleLockCount = 0; 
    
    // Đất trôi tự do khi không ai bị khóa màn hình
    public bool IsScrolling => battleLockCount <= 0 && HeroController.ActiveHeroes.Count > 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (groundDetails.Length > 0 && groundDetails[0].parent != null)
            groundWidth = ((RectTransform)groundDetails[0].parent).rect.width;
        else
            groundWidth = Screen.width;
    }

    void Update()
    {
        if (IsScrolling) 
        {
            ScrollEnvironment(scrollSpeed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        // THUẬT TOÁN CAMERA MỀM (DYNAMIC FRAMING)
        if (battleLockCount > 0)
        {
            HandleDynamicFraming();
        }
    }

    public void LockScroll() { battleLockCount++; }
    public void UnlockScroll() { battleLockCount = Mathf.Max(0, battleLockCount - 1); }

    public void ScrollEnvironment(float amount)
    {
        PanEnvironment(amount);
    }

    private void HandleDynamicFraming()
    {
        if (HeroController.ActiveHeroes.Count == 0) return;
        HeroController hero = HeroController.ActiveHeroes[0];
        if (hero == null || hero.IsDead) return;

        // BẮT BUỘC DÙNG WORLD SPACE (.position.x) ĐỂ BỎ QUA SỰ KHÁC BIỆT CỦA ANCHOR
        float minX = hero.heroRect.position.x;
        float maxX = minX;

        bool hasValidMonsters = false;

        foreach (var mon in MonsterController.ActiveMonsters)
        {
            if (mon.currentState == MonsterState.Dead) continue;
            
            // Chỉ lấy tọa độ các quái đang trực tiếp tham chiến
            if (mon.currentState == MonsterState.Approaching || mon.currentState == MonsterState.Attacking)
            {
                float mx = mon.GetComponent<RectTransform>().position.x;
                if (mx < minX) minX = mx;
                if (mx > maxX) maxX = mx;
                hasValidMonsters = true;
            }
        }

        if (!hasValidMonsters) return;

        // 1. Tính tâm điểm giao tranh thực tế
        float combatCenterX = (minX + maxX) / 2f;
        
        // 2. Xác định tâm của màn hình UI
        float targetCenter = Screen.width / 2f; 
        if (groundDetails.Length > 0 && groundDetails[0].parent != null)
        {
            targetCenter = groundDetails[0].parent.position.x; // Lấy tâm của thẻ Container chứa cỏ
        }

        // 3. Tính độ lệch để Camera trượt theo
        float diff = targetCenter - combatCenterX;

        // 4. Nếu lệch > 2 units thì bắt đầu Pan mượt mà
        if (Mathf.Abs(diff) > 2f)
        {
            // Tốc độ đuổi theo của Camera (Hệ số 2.5f)
            float panStep = diff * Time.deltaTime * 2.5f;

            // Pan Hero (Theo World Space)
            hero.heroRect.position += new Vector3(panStep, 0, 0);

            // Pan Quái (Theo World Space)
            foreach (var mon in MonsterController.ActiveMonsters)
            {
                if (mon.currentState != MonsterState.Dead)
                {
                    mon.GetComponent<RectTransform>().position += new Vector3(panStep, 0, 0);
                }
            }

            // Pan Mặt đất (Đổi về Local Space vì cỏ cuộn bằng AnchoredPosition)
            float scaleFactor = 1f;
            if (groundDetails.Length > 0 && groundDetails[0] != null)
            {
                Canvas canvas = groundDetails[0].GetComponentInParent<Canvas>();
                if (canvas != null) scaleFactor = canvas.scaleFactor;
            }
            
            if (scaleFactor <= 0) scaleFactor = 1f;
            PanEnvironment(panStep / scaleFactor);
        }
    }

    public void PanEnvironment(float amount)
    {
        foreach (RectTransform detail in groundDetails)
        {
            if (detail == null) continue;
            detail.anchoredPosition += new Vector2(amount, 0);
            
            // Cỏ trượt vòng lặp
            if (amount > 0 && detail.anchoredPosition.x > groundWidth + 100f) 
                detail.anchoredPosition = new Vector2(-100f, detail.anchoredPosition.y);
            else if (amount < 0 && detail.anchoredPosition.x < -100f)
                detail.anchoredPosition = new Vector2(groundWidth + 100f, detail.anchoredPosition.y);
        }
    }
}