using UnityEngine;

public class FianlBoss_Health : MonoBehaviour
{
    public int maxHp = 100;
    public float currentHp;

    private Final_Boss_Base bossBase;
    private bool isDead = false;

    private void Awake()
    {
        currentHp = maxHp;
        bossBase = GetComponent<Final_Boss_Base>();

        if (bossBase == null)
        {
            Debug.LogError("Final_Bose_Base ¾øÀ½");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (damage <= 0) return;

        currentHp -= damage;

        if (currentHp <= 0)
        {
            currentHp = 0;
            isDead = true;

            if (bossBase != null)
                bossBase.Die();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0) return;
        currentHp = Mathf.Min(currentHp + amount, maxHp);
    }

    public float GetHPPercent()
    {
        if (maxHp <= 0) return 0f;
        return (float)currentHp / maxHp;
    }

    public float GetMaxHP()
    {
        return maxHp;
    }

    public void SetCurrentHP(float value)
    {
        currentHp = Mathf.Clamp(value, 0f, maxHp);
    }
}