using UnityEngine;
using UnityEngine.AI;

public class EnemyMelee : EnemyStandard
{ 
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float hitRange = 1.5f;

    [Header("공격판정")]
    [SerializeField] private float hitHeight = 1.0f;
    [SerializeField] private float hitForwardOffset = 0.6f;
    [SerializeField] private float hitRadius = 0.35f;

    [Header("접근거리")]
    [SerializeField] private float approachMargin = 0.15f;

    [SerializeField] private float aimTurnSpeed = 720f;

    [SerializeField] private float attackAngle = 15f;

    private float nextAttackTime;
    

    protected override void Start()
    {
        base.Start();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            float reach = hitForwardOffset + hitRadius + approachMargin;
            agent.stoppingDistance = Mathf.Max(0f, hitRange - reach);
        }
    }

    protected override void DoAttack()
    {
        if (target == null) return;
        if (Time.time < nextAttackTime) return;
        if (isAttacking) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;
        toTarget.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            aimTurnSpeed * Time.deltaTime
        );

        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > attackAngle) return;

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = true;
        anim.SetTrigger("Attack");
    }

    public void AnimMeleeHit()
    {
        if (target == null) return;

        Vector3 hitCenter = transform.position + Vector3.up * hitHeight + transform.forward * hitForwardOffset;
        Collider[] hits = Physics.OverlapSphere(hitCenter, hitRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Player")) continue;

            BigToySystem.Instance.audioMgr.PlaySFX("Enemy_Spider_Attack");

            PlayerBase ph = hits[i].GetComponentInParent<PlayerBase>();
            if (ph != null)
            {
                ph.TakeDamage(GetAttackDamage());
            }
            break;
        }
    }

    public void AnimAttackEnd()
    {
        isAttacking = false;
        anim.SetBool("AttackEnd", true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 hitCenter = transform.position + Vector3.up * hitHeight + transform.forward * hitForwardOffset;
        Gizmos.DrawWireSphere(hitCenter, hitRadius);
    }
#endif
}