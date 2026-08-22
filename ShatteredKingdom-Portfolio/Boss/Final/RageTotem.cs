using System;
using UnityEngine;

public class RageTotem : MonoBehaviour, IDamageable
{ 
    [Header("딜")]
    [SerializeField] private float tickInterval = 2.0f;
    [SerializeField] private float radius = 6.0f;
    [SerializeField] private int damage = 15;

    [Header("체력")]
    [SerializeField] private float maxHP = 50f;
    private float currentHP;

    [Header("잡몹")]
    [SerializeField] private bool spawnMobs = false;
    [SerializeField] private GameObject[] mobPrefabs;
    [SerializeField] private float mobSpawnInterval = 4.0f;
    [SerializeField] private int mobSpawnCount = 1;
    [SerializeField] private float mobSpawnRadius = 3.0f;

    [Header("히트 연출")]
    [SerializeField] private GameObject totemRenderer;
    [SerializeField] private Color flashColor = Color.yellow;
    [SerializeField] private float flashTime = 0.15f;

    private Color originalColor;

    private Transform player;
    private float tickTimer;
    private float mobTimer;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public float GetHPPercent()
    {
        return currentHP / maxHP;
    }

    public void Bind(Transform playerTr)
    {
        player = playerTr;
    }

    private void Update()
    {
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            DoAoE();
        }

        if (spawnMobs && mobPrefabs != null)
        {
            mobTimer -= Time.deltaTime;
            if (mobTimer <= 0f)
            {
                mobTimer = mobSpawnInterval;
                SpawnMobs();
            }
        }
    }

    private void DoAoE()
    {
        Flash();

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Player")) continue;

            PlayerBase ph = hits[i].GetComponent<PlayerBase>();
            if (ph != null) ph.TakeDamage(damage);
            Debug.Log($"토템공격 히트, 데미지 : {damage}");
        }
    }

    private void SpawnMobs()
    {
        if (mobPrefabs == null || mobPrefabs.Length == 0) return;

        for (int i = 0; i < mobSpawnCount; i++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle * mobSpawnRadius;
            Vector3 pos = transform.position + new Vector3(r.x, 0f, r.y);

            int idx = UnityEngine.Random.Range(0, mobPrefabs.Length);
            GameObject prefab = mobPrefabs[idx];

            if (prefab != null)
                Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    private void Flash()
    {
        if (totemRenderer == null) return;

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        totemRenderer.SetActive(true);
        yield return new WaitForSeconds(flashTime);
        totemRenderer.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        if (currentHP <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
