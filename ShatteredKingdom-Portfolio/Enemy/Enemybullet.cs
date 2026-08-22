using System.IO;
using UnityEngine;

public class Enemybullet : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 3f;

    private Vector3 dir = Vector3.forward;
    private EnemyBulletPool myPool;
    private int bulletType = 0;

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(bye), lifeTime);

    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    public void Init(EnemyBulletPool pool, int type, Vector3 direction)
    {
        myPool = pool;

        dir = direction.normalized;

        bulletType = type;
    }

    private void Update()
    {
        this.transform.position += dir * speed * Time.deltaTime;
    }

    public void SetDamage(int newDamage)
    {
        damage = Mathf.Max(1, newDamage);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerBase ph = other.GetComponent<PlayerBase>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
            }

            bye();

        }


    }

    private void bye()
    {
        if (myPool != null)
            myPool.Return(bulletType, gameObject);
        else
            gameObject.SetActive(false);
    }

}
