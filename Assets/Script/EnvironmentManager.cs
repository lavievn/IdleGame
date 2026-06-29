using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("MÔI TRƯỜNG")]
    [SerializeField] private RectTransform[] groundDetails; 
    public float scrollSpeed = 150f; 

    private float groundWidth;
    private int battleLockCount = 0; 
    
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
        if (IsScrolling) ScrollEnvironment();
    }

    public void LockScroll() { battleLockCount++; }
    public void UnlockScroll() { battleLockCount = Mathf.Max(0, battleLockCount - 1); }

    public void ScrollEnvironment()
    {
        PanEnvironment(scrollSpeed * Time.deltaTime);
    }

    public void PanEnvironment(float amount)
    {
        foreach (RectTransform detail in groundDetails)
        {
            if (detail == null) continue;
            detail.anchoredPosition += new Vector2(amount, 0);
            
            if (amount > 0 && detail.anchoredPosition.x > groundWidth + 100f) 
                detail.anchoredPosition = new Vector2(-100f, detail.anchoredPosition.y);
            else if (amount < 0 && detail.anchoredPosition.x < -100f)
                detail.anchoredPosition = new Vector2(groundWidth + 100f, detail.anchoredPosition.y);
        }
    }
}