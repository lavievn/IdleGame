using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore; 

public class CombatManager : MonoBehaviour
{
    [SerializeField] private MonsterSpawner monsterSpawner;
    private GameManager gameManager;
    private HeroController heroController;
    
    private EntityDataSO runtimeHeroData;
    private int currentHeroHP, maxHeroHP;
    private bool isBattling = false;
    private Coroutine heroAttackCoroutine;

    public class ActiveMonsterInfo
    {
        public GameObject go;
        public MonsterController controller;
        public EntityDataSO data;
        public int currentHP;
        public int maxHP;
        public Coroutine attackCoroutine;
    }

    private List<ActiveMonsterInfo> activeMonsters = new List<ActiveMonsterInfo>();

    void Start() { gameManager = GetComponent<GameManager>(); heroController = Object.FindFirstObjectByType<HeroController>(); }

    public void SetupHeroInfo(EntityDataSO hData)
    {
        runtimeHeroData = hData;
        maxHeroHP = runtimeHeroData.GetCalculatedHealth();
        currentHeroHP = maxHeroHP;
        if (heroController != null) { heroController.UpdateHealthBar(currentHeroHP, maxHeroHP); heroController.UpdateAtkUI(runtimeHeroData.GetCalculatedDamage()); }
    }

    public void StartBattle(List<GameObject> monsters, EntityDataSO baseMonsterData)
    {
        activeMonsters.Clear();
        foreach (var m in monsters)
        {
            ActiveMonsterInfo info = new ActiveMonsterInfo();
            info.go = m;
            info.controller = m.GetComponent<MonsterController>();
            info.data = Instantiate(baseMonsterData);
            info.data.currentLevel = Mathf.Clamp(runtimeHeroData.currentLevel + Random.Range(-5, 6), 1, 999);
            
            info.maxHP = Mathf.RoundToInt(info.data.GetCalculatedHealth() * (1f + (info.data.currentLevel - 1) * 0.05f));
            info.currentHP = info.maxHP;
            if (info.controller != null) info.controller.UpdateHealthBar(info.currentHP, info.maxHP);
            activeMonsters.Add(info);
        }

        isBattling = true;
        heroAttackCoroutine = StartCoroutine(HeroAttackRoutine());

        foreach (var info in activeMonsters)
        {
            info.attackCoroutine = StartCoroutine(MonsterAttackRoutine(info));
        }
    }

    private ActiveMonsterInfo GetNearestMonster()
    {
        ActiveMonsterInfo nearest = null;
        float minDist = float.MaxValue;
        if (heroController == null) return null;
        float heroX = heroController.heroRect.position.x;

        foreach (var info in activeMonsters)
        {
            if (info.currentHP <= 0 || info.go == null) continue;
            float dist = Mathf.Abs(heroX - info.go.GetComponent<RectTransform>().position.x);
            if (dist < minDist) { minDist = dist; nearest = info; }
        }
        return nearest;
    }

    private IEnumerator HeroAttackRoutine()
    {
        while (isBattling && activeMonsters.Count > 0 && currentHeroHP > 0)
        {
            ActiveMonsterInfo target = GetNearestMonster();
            if (target == null) { yield return null; continue; }

            if (heroController.CurrentState == HeroState.Combat)
            {
                if (heroController.attackMode == AttackMode.RangedMagic)
                {
                    yield return new WaitForSeconds(2.0f);
                    if (!isBattling || activeMonsters.Count == 0) yield break;
                    if (heroController != null) heroController.PlayAttackFeedback();
                    List<ActiveMonsterInfo> targets = new List<ActiveMonsterInfo>(activeMonsters);
                    foreach (var t in targets) { if (t.currentHP > 0) DealDamageToMonster(t, true); }
                }
                else
                {
                    yield return new WaitForSeconds(1.0f / Mathf.Max(0.1f, runtimeHeroData.baseAttackSpeed));
                    if (!isBattling || activeMonsters.Count == 0) yield break;
                    target = GetNearestMonster(); 
                    if (target != null) DealDamageToMonster(target, false);
                }
            }
            else
            {
                yield return null; 
            }
        }
    }

    private void DealDamageToMonster(ActiveMonsterInfo target, bool isAoE)
    {
        float dmg = (runtimeHeroData.GetCalculatedDamage() - 2) * Random.Range(0.75f, 1.0f);
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(dmg));
        target.currentHP -= finalDmg;

        if (!isAoE && heroController != null) heroController.PlayAttackFeedback();
        
        if (target.controller != null) { target.controller.UpdateHealthBar(target.currentHP, target.maxHP); target.controller.ShowDamage(finalDmg); }

        if (target.currentHP <= 0) HandleMonsterDeath(target);
    }

    private void HandleMonsterDeath(ActiveMonsterInfo target)
    {
        activeMonsters.Remove(target);
        if (target.attackCoroutine != null) StopCoroutine(target.attackCoroutine);
        
        int exp = (runtimeHeroData.currentLevel + target.data.currentLevel) * 5; 
        bool isLevelUp = runtimeHeroData.AddExp(exp);

        if (isLevelUp)
        {
            maxHeroHP = runtimeHeroData.GetCalculatedHealth();
            currentHeroHP = maxHeroHP; 
            if (heroController != null) { heroController.UpdateHealthBar(currentHeroHP, maxHeroHP); heroController.UpdateAtkUI(runtimeHeroData.GetCalculatedDamage()); }
        }

        if (gameManager != null) gameManager.UpdateEventLog($"Đánh bại quái. Nhận {exp} EXP!");

        monsterSpawner.DespawnMonster(target.go);
        if (target.data != null) Destroy(target.data);

        if (gameManager != null) gameManager.OnMonsterDied(target.go);
        if (activeMonsters.Count == 0) isBattling = false;
    }

    private IEnumerator MonsterAttackRoutine(ActiveMonsterInfo info)
    {
        while (isBattling && info.currentHP > 0 && currentHeroHP > 0)
        {
            if (info.go == null || info.controller == null) yield break;

            if (info.controller.currentState == MonsterState.Attacking)
            {
                if (info.controller.attackMode == AttackMode.RangedMagic)
                {
                    yield return new WaitForSeconds(2.0f);
                    if (!isBattling || info.currentHP <= 0 || info.go == null) yield break;
                }
                else
                {
                    yield return new WaitForSeconds(1.0f / Mathf.Max(0.1f, info.data.baseAttackSpeed));
                    if (!isBattling || info.currentHP <= 0 || info.go == null) yield break;
                }

                float dmg = (info.data.GetCalculatedDamage() - 5) * Random.Range(0.75f, 1.0f);
                int finalDmg = Mathf.Max(1, Mathf.RoundToInt(dmg));
                currentHeroHP -= finalDmg;

                if (info.controller != null) info.controller.PlayAttackFeedback();
                if (heroController != null) { heroController.UpdateHealthBar(currentHeroHP, maxHeroHP); heroController.ShowDamage(finalDmg); }

                if (currentHeroHP <= 0) { ForceClearAllMonsters(); if (gameManager != null) gameManager.OnHeroDied(); }
            }
            else
            {
                yield return null; 
            }
        }
    }

    public void ForceClearAllMonsters() 
    { 
        isBattling = false; 
        foreach(var info in activeMonsters) 
        {
            if (info.go != null) monsterSpawner.DespawnMonster(info.go);
            if (info.data != null) Destroy(info.data);
        }
        activeMonsters.Clear();
    }
}