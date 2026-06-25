using UnityEngine;
using UnityEngine.Pool;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform monsterContainer; 
    [SerializeField] private Vector3 spawnPosition = new Vector3(-100f, 0f, 0f); 
    [SerializeField] private float minY = 10f; 
    [SerializeField] private float maxY = 90f;

    private IObjectPool<GameObject> monsterPool;

    void Awake()
    {
        monsterPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(monsterPrefab, monsterContainer),
            actionOnGet: (m) => {
                float randomY = Random.Range(minY, maxY);
                m.transform.localPosition = new Vector3(spawnPosition.x, randomY, 0f);
                m.GetComponent<MonsterController>()?.InitRandomAttackMode();
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