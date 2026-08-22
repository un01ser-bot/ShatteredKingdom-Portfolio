using UnityEngine;

public class Enemy_Hybrid : EnemyStandard
{
    [SerializeField] private float meleeRange = 3.5f;
    [SerializeField] private float throwRange = 8f;
    [SerializeField] private float attackAngle = 15f;

    [SerializeField] private float turnSpeed = 500f;

    [SerializeField] private float meleeCooldown = 1.2f;
    [SerializeField] private float throwCooldown = 2.0f;

    

    
    [SerializeField] private int bulletType = 1;
    [SerializeField] private Transform throwPoint;

    [SerializeField] private string throwTrigger = "Attack1";
    [SerializeField] private string meleeTrigger = "Attack2";

    private float nextMeleeTime = 0f;
    private float nextThrowTime = 0f;

    protected override void DoAttack()
    {
        if (isDie) return;
        if (isAttacking) return;
        if (target == null || data == null || anim == null) return;

        float dist = Vector3.Distance(transform.position, target.position);

        bool canThrow = dist > meleeRange && dist <= Mathf.Min(throwRange, data.attackRange) && Time.time >= nextThrowTime;
        bool canMelee = dist <= meleeRange && Time.time >= nextMeleeTime;

        if (!canThrow && !canMelee) return;

        //돌아보게하는 ㅂ분
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;
        toTarget.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );

        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > attackAngle) return;

        //끝
        isAttacking = true;

        if (canThrow)
        {
            anim.SetTrigger(throwTrigger);
            nextThrowTime = Time.time + throwCooldown;
        }
        else if(canMelee)
        {
            anim.SetTrigger(meleeTrigger);
            nextMeleeTime = Time.time + meleeCooldown;
        }
    }

   

    public void Anim_Throw()
    {
        if (isDie) return;
        if (bulletPool == null || target == null) return;

        BigToySystem.Instance.audioMgr.PlaySFX("Enemy_Golem_Ranged");

        Transform spawn;
        if (throwPoint != null)
            spawn = throwPoint;
        else
            spawn = this.transform;


        Vector3 dir = target.position - spawn.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        GameObject stone = bulletPool.Get(bulletType,spawn.position,Quaternion.LookRotation(dir.normalized));
        Enemybullet bullet = stone.GetComponent<Enemybullet>();
        if (bullet != null)
        {
            bullet.Init(bulletPool, bulletType, dir);
            bullet.SetDamage(GetAttackDamage());
        }

    }

    public void Anim_MeleeHit()
    {
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > meleeRange) return;

        BigToySystem.Instance.audioMgr.PlaySFX("Enemy_Golem_Melee");

        PlayerBase ph = target.GetComponent<PlayerBase>();
        if (ph != null)
            ph.TakeDamage(GetAttackDamage());
    }

    public void Anim_AttackEnd()
    {
        isAttacking = false;
        anim.SetBool("AttackEnd", true);
    }
}