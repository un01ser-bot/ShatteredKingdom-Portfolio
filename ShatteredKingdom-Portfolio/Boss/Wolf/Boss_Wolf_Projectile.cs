using UnityEngine;

public class Boss_Wolf_Projectile : MonoBehaviour
{
    [SerializeField] float speed = 18f;
    [SerializeField] int damage = 40;
    [SerializeField] float lifeTime = 4f;

    LayerMask wallMask;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetWallMask(LayerMask m)
    {
        wallMask = m;
    }

    public void Fire(Vector3 dir)
    {
        dir.y = 0f;
        dir.Normalize();
        rb.linearVelocity = dir * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & wallMask) != 0)
        {
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerBase>();
            if (player != null)
            {
                var boss = GetComponentInParent<Wolf_Boss_Base>();

                int finalDamage = boss != null
                    ? Mathf.RoundToInt(damage * boss.DamageMul)
                    : damage;

                player.TakeDamage(finalDamage);

                Debug.Log($"투사체 히트, 데미지 : {finalDamage}");
            }

            Destroy(gameObject);
        }
    }
}