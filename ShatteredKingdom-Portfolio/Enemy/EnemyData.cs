using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Enemy Data", fileName = "EnemyData_")]
public class EnemyData : ScriptableObject
{
    public string enemyName;

    public int maxHP = 10;

    public float detectDistance = 12f;

    public int questKey;
    
    public float attackRange = 2f;

    //µµ¸Á¹üÀ§
    public float keepDistance = 0f;

    public int attackDamage = 1;
    public int expReward = 5;
    public int goldReward = 2;
}