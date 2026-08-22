using UnityEngine;
using UnityEngine.AI;

public class EnemyRanged : EnemyStandard
{
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private float fleeStep = 4.0f;
    [SerializeField] private float fleeSampleRadius = 2.0f;

    
    [SerializeField] private int bulletType = 0;
    [SerializeField] private Transform firePoint;

    [SerializeField] private float lookPlayerSpeed = 360f;//아직 테스트중
    [SerializeField] private float canShootAngle = 10f;


    private float nextAttackTime;

    protected override void DoAttack()
    {
        if (target == null || data == null || isAttacking) return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (data.keepDistance > 0f && dist <= data.keepDistance)
        {
            Flee();
            return;
        }

        if (Time.time < nextAttackTime) return;

        //--테스트중--
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f) return;//그냥 겹침 방지용입니다
        toTarget.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(toTarget);
        transform.rotation = 
            Quaternion.RotateTowards(transform.rotation, targetRot, lookPlayerSpeed * Time.deltaTime);
        //


        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > canShootAngle) return;

        nextAttackTime = Time.time + attackCooldown;

        isAttacking = true;
        anim.SetTrigger("Attack");

    }

    public void AnimShoot()
    {
        
        if (bulletPool == null || firePoint == null || target == null) return;

        Vector3 dir = (target.position - firePoint.position).normalized;

        GameObject obj = bulletPool.Get(bulletType, firePoint.position, Quaternion.identity);

        BigToySystem.Instance.audioMgr.PlaySFX("Enemy_SkelNec_Attack");

        Enemybullet eb = obj.GetComponent<Enemybullet>();
        eb.Init(bulletPool,bulletType, dir);
        eb.SetDamage(GetAttackDamage());
    }


    public void AnimAttackEnd()
    {
        isAttacking = false;

    }


    private void Flee()
    {
        Vector3 dir = (transform.position - target.position).normalized;
        Vector3 desired = transform.position + dir * fleeStep;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, fleeSampleRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            anim.SetBool("isMove", true);
            agent.SetDestination(hit.position);
        }
    }


    //private void Shoot()
    //{
    //    if (enemyBulletPool != null && firePoint != null && target != null)
    //    {
    //        Vector3 dir = (target.position - firePoint.position).normalized;

    //        GameObject obj = enemyBulletPool.Get(firePoint.position,Quaternion.identity);
    //        Enemybullet eb = obj.GetComponent<Enemybullet>();
    //        eb.Init(enemyBulletPool, dir);
    //        anim.SetTrigger("Attack");
    //    }
    //}

}