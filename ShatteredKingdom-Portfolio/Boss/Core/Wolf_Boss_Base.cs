using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;



public class Wolf_Boss_Base : MonoBehaviour, IDamageable
{
    enum State
    {
        Chase,
        Look,
        CastPattern
    }

    enum CastStep
    {
        None,
        preLeap,
        Pattern
    }

    private State currentState;
    private CastStep currentCastStep = CastStep.None;

    private IBossPattern currentPattern;

    private NavMeshAgent agent;
    public Animator anim { get; private set; }
    private Transform target;

    [SerializeField] private float chaseDistance = 8f;
    [SerializeField] private float lookDistance = 3f;
    [SerializeField] private float basicAttackDistance = 2.2f;
    [SerializeField] private float coneCastMaxDistance = 6f;

    [SerializeField] private float turnSpeed = 5f;

    [SerializeField] Boss_Health health;

    private bool isDead = false;

    [Header("기본공격")]
    [SerializeField] private float basicAttackCooldown = 1.2f;
    [SerializeField] private int basicAttackDamage = 5;
    private float basicAtkTimer = 0f;
    private bool isBasicAttacking = false;

    [Header("스킬전체쿨타임")]
    [SerializeField] private float skillCooldown = 6f;
    private float skillTimer = 0f;

    [SerializeField] private float leapCooldown = 6f;
    private float leapTimer = 0f;

    [SerializeField] private float skillStartMinDistance = 4f;
    [SerializeField] private WolfPattern_Leap leapPattern;
    [SerializeField] private WolfPattern_Cone conePattern;
    [SerializeField] private WolfPattern_Circle circlePattern;
    [SerializeField] private Wolf_Pattern_Rockfall rockfallPattern;

    [SerializeField] private WolfPattern_ShareCircle shareCirclePattern;
    [SerializeField] private WolfPattern_HideBehindWall hideBehindWallPattern;
    [SerializeField] private WolfPattern_AllDamage allDamagePattern;

    private bool isCasting = false;
    bool moveLocked = false;

    [SerializeField] private float openerDelay = 0.6f;
    private bool openerUsed = false;

    private int lastPatternId = -1;

    private bool attackSfxToggle;


    [Header("던전 스케일링")]
    [SerializeField] private int dungeonLevel = 1;
    [SerializeField] private int baseMaxHP = 500;
    [SerializeField] private int baseAttackDamage = 5;
    [SerializeField] private int baseExp = 0;
    [SerializeField] private int baseGold = 0;

    private int scaledMaxHP;
    private int scaledAttackDamage;
    private int scaledExp;
    private int scaledGold;

    public float DamageMul { get; private set; } = 1f;
    public int ScaledAttackDamage => scaledAttackDamage;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            target = p.transform;

        if (leapPattern != null) leapPattern.SetTarget(target);
        if (conePattern != null) conePattern.SetTarget(target);
        if (circlePattern != null) circlePattern.SetTarget(target);
        if (rockfallPattern != null) rockfallPattern.SetTarget(target);

        if (shareCirclePattern != null) shareCirclePattern.SetTarget(target);
        if (hideBehindWallPattern != null) hideBehindWallPattern.SetTarget(target);
        if (allDamagePattern != null) allDamagePattern.SetTarget(target);

        
        ApplyDungeonScaling(dungeonLevel);
        ChangeState(State.Chase);
    }



    void Update()
    {
        if (isDead) return;

        // 타겟 유효성 검사 + 재탐색
        if (target == null || !target.gameObject.activeInHierarchy || !target.CompareTag("Player"))
        {
            FindTarget();
            if (target == null) return;
        }

        if (target == null)
        {
            isCasting = false;
            currentPattern = null;
            currentCastStep = CastStep.None;
            ChangeState(State.Chase);
            return;
        }

        basicAtkTimer -= Time.deltaTime;
        skillTimer -= Time.deltaTime;
        leapTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, target.position);

        switch (currentState)
        {
            case State.Chase:
                UpdateChase(distance);
                break;
            case State.Look:
                UpdateLook(distance);
                break;
            case State.CastPattern:
                UpdateCastPattern();
                break;

        }

    }

    void FindTarget()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            target = p.transform;

            // 패턴들에도 새 타겟 재설정
            if (leapPattern != null) leapPattern.SetTarget(target);
            if (conePattern != null) conePattern.SetTarget(target);
            if (circlePattern != null) circlePattern.SetTarget(target);
            if (rockfallPattern != null) rockfallPattern.SetTarget(target);
            if (shareCirclePattern != null) shareCirclePattern.SetTarget(target);
            if (hideBehindWallPattern != null) hideBehindWallPattern.SetTarget(target);
            if (allDamagePattern != null) allDamagePattern.SetTarget(target);
        }
    }

    public void ApplyDungeonScaling(int dungeonLevel)
    {
        if (dungeonLevel < 1) dungeonLevel = 1;

        float plusHP = 1f + (dungeonLevel - 1) * 0.1f;
        float plusAttackDamage = 1f + (dungeonLevel - 1) * 0.2f;
        float plusReward = 1f + (dungeonLevel - 1) * 0.15f;

        scaledMaxHP = Mathf.Max(1, Mathf.RoundToInt(baseMaxHP * plusHP));
        scaledAttackDamage = Mathf.Max(1, Mathf.RoundToInt(baseAttackDamage * plusAttackDamage));
        scaledExp = Mathf.Max(0, Mathf.RoundToInt(baseExp * plusReward));
        scaledGold = Mathf.Max(0, Mathf.RoundToInt(baseGold * plusReward));

        DamageMul = (float)scaledAttackDamage / Mathf.Max(1, baseAttackDamage);

        basicAttackDamage = scaledAttackDamage;

        if (health != null)
        {
            health.SetMaxHP(scaledMaxHP);
        }
    }

    private void UpdateChase(float distance)
    {

        if (moveLocked)
        {
            agent.isStopped = true;
            agent.ResetPath();
            anim.SetBool("IsMove", false);
            return;
        }

        agent.isStopped = false;
        agent.updateRotation = true;
        agent.speed = 6.5f;

        anim.SetBool("IsMove", true);

        agent.SetDestination(target.position);

        
        if (TryStartSkill(distance))
        {
            return;
        }

        
        if (distance <= lookDistance)
        {
            
            ChangeState(State.Look);
        }

    }

    private void UpdateLook(float distance)
    {
        if (moveLocked)
        {
            agent.isStopped = true;
            agent.ResetPath();
            anim.SetBool("IsMove", false);
            return;
        }


        agent.updateRotation = false;
        agent.speed = 6.5f;


        
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }


        if (distance > chaseDistance)
        {
            ChangeState(State.Chase);
            return;
        }
        
        if (distance <= basicAttackDistance)
        {
            agent.isStopped = true;
            anim.SetBool("IsMove", false);
            
            TryBasicAttack(distance);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(target.position);
        anim.SetBool("IsMove", true);

        
        TryStartSkill(distance);


    }

    

    private void StartCurrentPattern()
    {
        if (currentPattern == null) return;
        

        agent.isStopped = true;
        agent.ResetPath();
        anim.SetBool("IsMove", false);

        if (currentPattern == shareCirclePattern || currentPattern == hideBehindWallPattern || currentPattern == allDamagePattern)
        {
            LockMove();
            isCasting = true;
        }

        if (currentPattern == conePattern || currentPattern == circlePattern || currentPattern == rockfallPattern)
        {
            anim.ResetTrigger("Cast");
            anim.SetTrigger("Cast");
            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Wolf_Pattern");
            return;
        }

        if (currentPattern == shareCirclePattern || currentPattern == hideBehindWallPattern || currentPattern == allDamagePattern)
        {
            anim.ResetTrigger("Gimmik_Cast");
            anim.SetTrigger("Gimmik_Cast");
            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Wolf_Gimmik");
            return;
        }
    }


    private bool TryStartSkill(float distance)
    {
        if (isCasting) return false;
        if (skillTimer > 0f) return false;

        if (leapPattern != null && leapPattern.CanUse() && lastPatternId != 1)
        {
            if (distance >= skillStartMinDistance)
            {
                lastPatternId = 1;
                isCasting = true;

                leapTimer = leapCooldown;

                currentPattern = leapPattern;
                currentCastStep = CastStep.preLeap;
                ChangeState(State.CastPattern);

                leapPattern.StartPattern(null);
                return true;
            }
        }

        

        bool canCone = false;
        if (conePattern != null && conePattern.CanUse())
        {
            if (distance <= coneCastMaxDistance)
                canCone = true;
        }

        bool canCircle = false;
        if (circlePattern != null && circlePattern.CanUse())
        {
            if (distance <= coneCastMaxDistance)
                canCircle = true;
        }

        bool canRockfall = false;
        if (rockfallPattern != null && rockfallPattern.CanUse())
        {
            if (distance <= coneCastMaxDistance)
                canRockfall = true;
        }

        bool canShare = false;
        if (shareCirclePattern != null && shareCirclePattern.CanUse())
        {
            if (distance <= coneCastMaxDistance)
                canShare = true;
        }

        bool canHide = false;
        if (hideBehindWallPattern != null && hideBehindWallPattern.CanUse())
        {
            if (distance <= coneCastMaxDistance)
                canHide = true;
        }

        bool canAllDamage = false;
        if (allDamagePattern != null && allDamagePattern.CanUse())
        {
            if (distance <= coneCastMaxDistance)
                canAllDamage = true;
        }

        if (!canCone && !canCircle && !canRockfall && !canShare && !canHide && !canAllDamage)
            return false;

        if (!openerUsed)
        {
            if (canCone)
            {
                openerUsed = true;
                lastPatternId = 0;

                isCasting = true;
                skillTimer = skillCooldown;

                currentPattern = conePattern;
                currentPattern.StartPattern(null);
                currentCastStep = CastStep.Pattern;
                ChangeState(State.CastPattern);

                StartCurrentPattern();
                return true;
            }

            if (canCircle)
            {
                openerUsed = true;
                lastPatternId = 2;

                isCasting = true;
                skillTimer = skillCooldown;

                currentPattern = circlePattern;
                currentPattern.StartPattern(null);
                currentCastStep = CastStep.Pattern;
                ChangeState(State.CastPattern);

                StartCurrentPattern();
                return true;
            }

            if (canRockfall)
            {
                openerUsed = true;
                lastPatternId = 3;

                isCasting = true;
                skillTimer = skillCooldown;

                currentPattern = rockfallPattern;
                currentPattern.StartPattern(null);
                currentCastStep = CastStep.Pattern;
                ChangeState(State.CastPattern);

                StartCurrentPattern();
                return true;
            }




        }

        if (lastPatternId == 0) canCone = false;
        if (lastPatternId == 2) canCircle = false;
        if (lastPatternId == 3) canRockfall = false;
        if (lastPatternId == 4) canShare = false;
        if (lastPatternId == 5) canHide = false;
        if (lastPatternId == 6) canAllDamage = false;

        List<IBossPattern> usable = new List<IBossPattern>();
        List<int> usableIds = new List<int>();

        if (canCone) { usable.Add(conePattern); usableIds.Add(0); }
        if (canCircle) { usable.Add(circlePattern); usableIds.Add(2); }
        if (canRockfall) { usable.Add(rockfallPattern); usableIds.Add(3); }
        if (canShare) { usable.Add(shareCirclePattern); usableIds.Add(4); }
        if (canHide) { usable.Add(hideBehindWallPattern); usableIds.Add(5); }
        if (canAllDamage) { usable.Add(allDamagePattern); usableIds.Add(6); }

        if (usable.Count == 0)
        {
            canCone = (conePattern != null && conePattern.CanUse() && distance <= coneCastMaxDistance);
            canCircle = (circlePattern != null && circlePattern.CanUse() && distance <= coneCastMaxDistance);
            canRockfall = (rockfallPattern != null && rockfallPattern.CanUse() && distance <= coneCastMaxDistance);
            canShare = (shareCirclePattern != null && shareCirclePattern.CanUse() && distance <= coneCastMaxDistance);
            canHide = (hideBehindWallPattern != null && hideBehindWallPattern.CanUse() && distance <= coneCastMaxDistance);
            canAllDamage = (allDamagePattern != null && allDamagePattern.CanUse() && distance <= coneCastMaxDistance);

            if (canCone) { usable.Add(conePattern); usableIds.Add(0); }
            if (canCircle) { usable.Add(circlePattern); usableIds.Add(2); }
            if (canRockfall) { usable.Add(rockfallPattern); usableIds.Add(3); }
            if (canShare) { usable.Add(shareCirclePattern); usableIds.Add(4); }
            if (canHide) { usable.Add(hideBehindWallPattern); usableIds.Add(5); }
            if (canAllDamage) { usable.Add(allDamagePattern); usableIds.Add(6); }
        }

        if (usable.Count == 0)
            return false;

        int idx = UnityEngine.Random.Range(0, usable.Count);

        

        lastPatternId = usableIds[idx];
        isCasting = true;
        skillTimer = skillCooldown;
        currentPattern = usable[idx];
        currentPattern.StartPattern(null);
        currentCastStep = CastStep.Pattern;
        ChangeState(State.CastPattern);
        StartCurrentPattern();
        return true;


        
    }

    private void UpdateCastPattern()
    {
        
        if (currentCastStep == CastStep.preLeap)
        {
            anim.SetBool("IsMove", true);
        }
        else if (currentCastStep == CastStep.Pattern)
        {
            agent.isStopped = true;
            agent.ResetPath();
            

        }

        switch (currentCastStep)
        {

            case CastStep.preLeap:
                if (leapPattern.IsRunning == false)
                {
                    float dist = Vector3.Distance(transform.position, target.position);

                    if (dist <= coneCastMaxDistance)
                    {

                        currentCastStep = CastStep.Pattern;

                        if (currentPattern != null)
                            currentPattern.StartPattern(null);

                        StartCurrentPattern();

                    }

                    else
                    {
                        currentCastStep = CastStep.None;
                        currentPattern = null;
                        isCasting = false;
                        ChangeState(State.Chase);

                    }

                }
                break;

            case CastStep.Pattern:

                if (currentPattern != null)
                {
                    if (currentPattern.IsRunning)
                    {
                        currentPattern.Tick(Time.deltaTime);
                    }

                    if (currentPattern.IsRunning == false)
                    {
                        currentCastStep = CastStep.None;
                        currentPattern = null;

                        isCasting = false;

                        if (moveLocked) UnlockMove();

                        ChangeState(State.Look);
                    }
                }
                break;

        }

    }

    public void PlayBossBasicAttackSFX()
    {
        string key = attackSfxToggle ? "Boss_Wolf_attack1" : "Boss_Wolf_attack2";
        attackSfxToggle = !attackSfxToggle;

        BigToySystem.Instance.audioMgr.PlaySFX(key);
    }

    private void TryBasicAttack(float distance)
    {
        
        if (isBasicAttacking == true) return;
        if (distance > basicAttackDistance) return;
        if (basicAtkTimer > 0f) return;
        
        isBasicAttacking = true;
        basicAtkTimer = basicAttackCooldown;

        agent.isStopped = true;
        agent.ResetPath();
        anim.SetBool("IsMove", false);
        anim.SetTrigger("BossAttack_1");

        PlayBossBasicAttackSFX();

        PlayerBase ph = target.GetComponent<PlayerBase>();
        if (ph != null)
        {
            ph.TakeDamage(basicAttackDamage);
            
        }

    }

    void LockMove()
    {
        moveLocked = true;

        agent.isStopped = true;
        agent.ResetPath();
        agent.updateRotation = false;

        anim.SetBool("IsMove", false);
    }

    void UnlockMove()
    {
        moveLocked = false;

        agent.isStopped = false;
        agent.updateRotation = true;
    }

    public void AnimAttackEnd()
    {
        isBasicAttacking = false;

        if (currentState == State.CastPattern && currentPattern != null && currentPattern.IsRunning)
            return;

        if (moveLocked) UnlockMove();

        isCasting = false;
        ChangeState(State.Look);
    }

    private void ChangeState(State newstate)
    {
        if (currentState == newstate) return;

        currentState = newstate;

    }

    public void TakeDamage(int dmg)
    {
        health.TakeDamage(dmg);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        isCasting = false;
        isBasicAttacking = false;
        currentPattern = null;
        currentCastStep = CastStep.None;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (anim != null)
        {
            anim.SetBool("IsMove", false);
            anim.ResetTrigger("BossAttack_1");
            anim.ResetTrigger("Cast");
            anim.SetTrigger("Die");
        }

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        Destroy(gameObject, 7f);

    }


}
