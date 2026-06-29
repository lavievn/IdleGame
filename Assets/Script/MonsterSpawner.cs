using UnityEngine;
using UnityEngine.Pool;
using TuTienCore;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform monsterContainer; 
    [SerializeField] private Vector2 spawnPosition = new Vector2(-100f, 0f); 
    [SerializeField] private float minY = 10f; 
    [SerializeField] private float maxY = 90f;

    private IObjectPool<GameObject> monsterPool;

    void Awake()
    {
        monsterPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(monsterPrefab, monsterContainer),
            actionOnGet: (m) => {
                // Ép chuẩn hệ tọa độ RectTransform và Anchor Bottom-Left
                RectTransform rect = m.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0f); 
                    
                    float randomY = Random.Range(minY, maxY);
                    rect.anchoredPosition = new Vector2(spawnPosition.x, randomY);
                }
                
                // Khởi tạo logic chiến đấu ngẫu nhiên
                var controller = m.GetComponent<MonsterController>();
                if (controller != null)
                {
                    controller.InitRandomAttackMode();
                }

                m.SetActive(true);
                m.transform.localScale = Vector3.one;
            },
            actionOnRelease: (m) => m.SetActive(false),
            actionOnDestroy: (m) => Destroy(m),
            defaultCapacity: 5, maxSize: 20
        );
    }

    public GameObject SpawnMonster() => monsterPool.Get();
    public void DespawnMonster(GameObject monster) => monsterPool.Release(monster);
}