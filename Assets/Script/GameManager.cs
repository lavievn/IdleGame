using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TuTienCore; 

public class GameManager : MonoBehaviour
{
    [Header("UI CỐT LÕI")]
    public Image eventLog;
    public TextMeshProUGUI eventLogText; 
    public GameObject preGameUI; 
    public TextMeshProUGUI infoText; 

    [Header("UI GAME OVER")]
    public GameObject gameOverPanel;
    public GameObject confirmationPopup;

    [Header("DỮ LIỆU GỐC")]
    public EntityDataSO heroDataSO;
    public EntityDataSO currentMonsterDataSO;
    private EntityDataSO runtimeHeroData; 

    [Header("SYSTEMS")]
    [SerializeField] private HeroController heroController; 
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private EnvironmentManager environmentManager;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private SaveManager saveManager; 

    public List<GameObject> activeMonsters = new List<GameObject>();
    public float currentAttackRange = 120f; 
    
    private bool hasDeployed = false; 

    void Start()
    {
        if (monsterSpawner == null) monsterSpawner = Object.FindFirstObjectByType<MonsterSpawner>();
        if (environmentManager == null) environmentManager = Object.FindFirstObjectByType<EnvironmentManager>();
        if (heroController == null) heroController = Object.FindFirstObjectByType<HeroController>();
        if (combatManager == null) combatManager = GetComponent<CombatManager>();
        if (saveManager == null) saveManager = GetComponent<SaveManager>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);

        InitHeroData();
        StartCoroutine(AutoSaveRoutine());
    }

    private void InitHeroData()
    {
        runtimeHeroData = Instantiate(heroDataSO);
        bool hasSaveData = saveManager != null && saveManager.LoadGame(runtimeHeroData);

        if (!hasSaveData)
        {
            runtimeHeroData.gender = Random.Range(0, 2) == 0 ? GenderType.Nam : GenderType.Nu;
            runtimeHeroData.entityName = NameDatabase.GetRandomName(runtimeHeroData.gender);
            runtimeHeroData.spiritRoots = SynergyMath.GenerateRandomRoots();
        }

        if (heroController != null) heroController.SetGenderVisual(runtimeHeroData.gender);
        SetupPreGameUI();
    }

    public void UpdateEventLog(string message) { if (eventLogText != null) eventLogText.text = message; }

    private void SetupPreGameUI()
    {
        preGameUI.SetActive(true);
        string rootStr = string.Join(" - ", runtimeHeroData.spiritRoots);
        infoText.text = $"Tên: {runtimeHeroData.entityName}\nLinh căn: {rootStr}\nHP: {runtimeHeroData.GetCalculatedHealth()}";
    }

    public void OnDeployClicked()
    {
        preGameUI.SetActive(false); 
        hasDeployed = true;         
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        heroController.SpawnHero(); 
        CallNextWave();             
    }

    void Update()
    {
        if (heroController == null || !hasDeployed) return;

        bool heroIsMoving = false;
        bool heroIsFighting = false;

        if (activeMonsters.Count == 0)
        {
            if (!heroController.IsAtCenter())
            {
                heroController.MoveToCenter(150f);
                heroIsMoving = true;
                if (environmentManager != null) environmentManager.PanEnvironment(150f * Time.deltaTime);
            }
        }
        else
        {
            float heroActualX = heroController.GetActualXPosition();
            float minDistance = float.MaxValue;
            GameObject nearestMonster = null;

            foreach (var m in activeMonsters)
            {
                if (m == null || !m.activeSelf) continue;
                float dist = heroActualX - m.GetComponent<RectTransform>().anchoredPosition.x;
                if (dist > 0 && dist < minDistance)
                {
                    minDistance = dist;
                    nearestMonster = m;
                }
            }

            // 1. LOGIC DI CHUYỂN CỦA HERO
            if (nearestMonster != null)
            {
                RectTransform targetRect = nearestMonster.GetComponent<RectTransform>();
                float distanceX = heroActualX - targetRect.anchoredPosition.x;
                float distanceY = Mathf.Abs(heroController.GetYPosition() - targetRect.anchoredPosition.y);

                if (distanceX > currentAttackRange || distanceY > 15f) 
                {
                    heroIsMoving = true;
                    heroController.MoveTowardsEnemy(targetRect.anchoredPosition.y, 100f);
                    if (environmentManager != null) environmentManager.PanEnvironment(150f * Time.deltaTime);
                }
                else
                {
                    heroIsFighting = true; 
                }
            }

            // 2. LOGIC DI CHUYỂN ĐỘC LẬP CỦA QUÁI
            foreach (var m in activeMonsters)
            {
                if (m == null || !m.activeSelf) continue;
                RectTransform mRect = m.GetComponent<RectTransform>();
                MonsterController mController = m.GetComponent<MonsterController>();

                float mDistX = heroActualX - mRect.anchoredPosition.x;
                float mDistY = Mathf.Abs(heroController.GetYPosition() - mRect.anchoredPosition.y);

                // Quái trôi theo nền cỏ nếu Hero đang di chuyển
                if (heroIsMoving) mRect.anchoredPosition += new Vector2(150f * Time.deltaTime, 0);

                // Nếu CHƯA VÀO TẦM CỦA NÓ -> Nó tiếp tục lết về phía Hero
                if (mDistX > mController.attackRange || mDistY > 15f)
                {
                    float step = 100f * Time.deltaTime;
                    float newY = Mathf.MoveTowards(mRect.anchoredPosition.y, heroController.GetYPosition(), step);
                    mRect.anchoredPosition = new Vector2(mRect.anchoredPosition.x + step, newY);
                }
            }
        }
        
        heroController.HandleAnimation(heroIsMoving, heroIsFighting);
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (hasDeployed && runtimeHeroData != null && runtimeHeroData.isDirty && saveManager != null) saveManager.SaveGame(runtimeHeroData);
        }
    }

    public void OnHeroDied()
    {
        hasDeployed = false;
        activeMonsters.Clear();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void OnRetryClicked()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        hasDeployed = true;
        if (combatManager != null) combatManager.SetupHeroInfo(runtimeHeroData);
        CallNextWave();
    }

    public void OnResetClicked() { if (confirmationPopup != null) confirmationPopup.SetActive(true); }
    public void OnConfirmResetClicked()
    {
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (saveManager != null) saveManager.DeleteSave();
        if (combatManager != null) combatManager.ForceClearAllMonsters();
        activeMonsters.Clear();
        if (heroController != null) heroController.heroRect.gameObject.SetActive(false);
        InitHeroData();
    }
    public void OnCancelResetClicked() { if (confirmationPopup != null) confirmationPopup.SetActive(false); }

    private void CallNextWave()
    {
        if (monsterSpawner == null) return;
        activeMonsters.Clear();
        
        int spawnCount = Random.Range(1, 4);
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject m = monsterSpawner.SpawnMonster();
            if (m != null)
            {
                RectTransform rect = m.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-100f - (i * 60f), rect.anchoredPosition.y);
                activeMonsters.Add(m);
            }
        }

        // [CẬP NHẬT CHÍ MẠNG]: Kích hoạt trạng thái Combat ngay lập tức khi quái sinh ra
        if (combatManager != null) combatManager.StartBattle(activeMonsters, currentMonsterDataSO);
    }

    public void OnMonsterDied(GameObject deadMonster)
    {
        activeMonsters.Remove(deadMonster);
        if (activeMonsters.Count == 0) StartCoroutine(WaitAndCallNextWave());
    }

    private IEnumerator WaitAndCallNextWave()
    {
        yield return new WaitForSeconds(3f);
        if (hasDeployed) CallNextWave();
    }

    private void OnApplicationQuit() { if (saveManager != null && hasDeployed && runtimeHeroData != null) saveManager.SaveGame(runtimeHeroData); }
}