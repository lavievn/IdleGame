using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("MÔI TRƯỜNG")]
    [SerializeField] private RectTransform[] groundDetails; 
    [SerializeField] private float scrollSpeed = 150f;

    private float groundWidth;

    void Start()
    {
        if (groundDetails.Length > 0 && groundDetails[0].parent != null)
        {
            groundWidth = ((RectTransform)groundDetails[0].parent).rect.width;
        }
        else
        {
            groundWidth = Screen.width;
        }
    }

    public void ScrollEnvironment()
    {
        // Mặc định cuộn tiến
        PanEnvironment(scrollSpeed * Time.deltaTime);
    }

    public void PanEnvironment(float amount)
    {
        foreach (RectTransform detail in groundDetails)
        {
            if (detail == null) continue;

            detail.anchoredPosition += new Vector2(amount, 0);
            
            // [FIX CHÍ MẠNG]: Hỗ trợ toán học đảo biên hai chiều chống lọt/mất chi tiết nền
            if (amount > 0 && detail.anchoredPosition.x > groundWidth + 100f) 
            {
                detail.anchoredPosition = new Vector2(-100f, detail.anchoredPosition.y);
            }
            else if (amount < 0 && detail.anchoredPosition.x < -100f)
            {
                detail.anchoredPosition = new Vector2(groundWidth + 100f, detail.anchoredPosition.y);
            }
        }
    }
}