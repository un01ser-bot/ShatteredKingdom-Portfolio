using UnityEngine;

public class Boss_Health : MonoBehaviour
{
    [SerializeField] private int MaxHp = 100;
    private int currentHp;

    private Wolf_Boss_Base bossBase;
    private bool isDead = false;

    void Start()
    {
        currentHp = MaxHp;
        bossBase = GetComponent<Wolf_Boss_Base>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;
        if (currentHp <= 0)
        {
            isDead = true;
            bossBase.Die();
        }
    }

    public float GetHPPercent()
    {
        return (float)currentHp / MaxHp;
    }

    public void SetMaxHP(int newMax)
    {
        MaxHp = newMax;
        currentHp = newMax;
        isDead = false;
    }
}