using UnityEngine;

public class FinalOrb : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Heal Effect")]
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private float healEffectDuration = 2f;

    private Transform bossTr;
    private FianlBoss_Health bossHp;
    private float healAmount;

    private bool dead = false;

    public void Init(Transform boss, FianlBoss_Health hp, float heal)
    {
        bossTr = boss;
        bossHp = hp;
        healAmount = heal;
    }

    private void Update()
    {
        if (dead) return;
        if (bossTr == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            bossTr.position,
            moveSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (dead) return;

        if (other.CompareTag("Player"))
        {
            dead = true;
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            if (bossHp != null)
            {
                bossHp.Heal(healAmount);
            }

            SpawnHealEffect();

            dead = true;
            Destroy(gameObject);
        }
    }

    private void SpawnHealEffect()
    {
        if (healEffectPrefab == null || bossTr == null) 
            return;

        GameObject fx = Instantiate(healEffectPrefab, bossTr.position, Quaternion.identity, bossTr);

        Destroy(fx, healEffectDuration);
    }
}