using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Final_Boss_Base : MonoBehaviour, IDamageable
{
    enum State { Chase, Look, CastPattern }
    enum CastStep { None, preLeap, Pattern }

    private State currentState;
    private CastStep currentCastStep = CastStep.None;

    private IBossPattern currentPattern;

    private NavMeshAgent agent;
    public Animator anim { get; private set; }
    private Transform target;

    [Header("거리")]
    [SerializeField] private float chaseDistance = 8f;
    [SerializeField] private float lookDistance = 3f;
    [SerializeField] private float basicAttackDistance = 2.2f;
    [SerializeField] private float castMaxDistance = 6f;

    [SerializeField] private float turnSpeed = 5f;

    [SerializeField] private FianlBoss_Health health;

    private bool isDead = false;

    [Header("기본공격")]
    [SerializeField] private float basicAttackCooldown = 1.2f;
    [SerializeField] private int basicAttackDamage = 5;
    private float basicAtkTimer = 0f;
    private bool isBasicAttacking = false;

    [Header("스킬전체쿨타임")]
    [SerializeField] private float skillCooldown = 6f;
    private float skillTimer = 0f;

    [Header("도약(PreLeap)")]
    [SerializeField] private float leapCooldown = 6f;
    private float leapTimer = 0f;
    [SerializeField] private float skillStartMinDistance = 4f;

    [Header("패턴 참조 (Final)")]
    [SerializeField] private FinalPattern_Leap leapPattern;
    [SerializeField] private FinalPattern_Cone conePattern;
    [SerializeField] private FinalPattern_Circle circlePattern;
    [SerializeField] private FinalPattern_Rockfall rockfallPattern;
    [SerializeField] private FinalPattern_ShareCircle shareCirclePattern;
    [SerializeField] private FinalPattern_HideBehindWall hideBehindWallPattern;
    [SerializeField] private FinalPattern_AllDamage allDamagePattern;

    [SerializeField] private FinalPattern_OrbCollect orbCollectPattern;

    [SerializeField] private float rageHpPercent = 0.10f;
    private bool rageTriggered = false;
    private bool isInvincible = false;

    [SerializeField] private FinalPattern_RageTotem rageTotemPattern;
    [SerializeField] private bool enableRageTotem = true;

    [Header("스킬 ON/OFF")]
    [SerializeField] private bool enableLeap = true;
    [SerializeField] private bool enableCone = true;
    [SerializeField] private bool enableCircle = true;
    [SerializeField] private bool enableRockfall = true;
    [SerializeField] private bool enableShareCircle = true;
    [SerializeField] private bool enableHideBehindWall = true;
    [SerializeField] private bool enableAllDamage = true;

    [SerializeField] private bool enableOrbCollect = true;

    private bool isCasting = false;
    private bool moveLocked = false;

    [SerializeField] private bool useOpener = true;
    private bool openerUsed = false;

    private int lastPatternId = -1;

    private bool attackSfxToggle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.transform;

        BindTargets();
        ChangeState(State.Chase);
    }

    public void BindTargets()
    {
        if (leapPattern != null) leapPattern.SetTarget(target);
        if (conePattern != null) conePattern.SetTarget(target);
        if (circlePattern != null) circlePattern.SetTarget(target);
        if (rockfallPattern != null) rockfallPattern.SetTarget(target);

        if (shareCirclePattern != null) shareCirclePattern.SetTarget(target);
        if (hideBehindWallPattern != null) hideBehindWallPattern.SetTarget(target);
        if (allDamagePattern != null) allDamagePattern.SetTarget(target);

        if (orbCollectPattern != null)
        {
            orbCollectPattern.SetTarget(target);
            orbCollectPattern.BindBoss(transform, health);
        }
        if (rageTotemPattern != null)
        {
            rageTotemPattern.SetTarget(target);
            rageTotemPattern.BindBoss(this, health);
        }
    }

    private void Update()
    {
        if (isDead) return;
        if (target == null) return;

        basicAtkTimer -= Time.deltaTime;
        skillTimer -= Time.deltaTime;
        leapTimer -= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, target.position);

        switch (currentState)
        {
            case State.Chase: UpdateChase(distance); break;
            case State.Look: UpdateLook(distance); break;
            case State.CastPattern: UpdateCastPattern(); break;
        }
    }

    private void UpdateChase(float distance)
    {
        if (moveLocked)
        {
            StopMove();
            return;
        }

        ResumeMove();
        if (agent != null) agent.speed = 6.5f;
        if (anim != null) anim.SetBool("IsMove", true);

        if (agent != null) agent.SetDestination(target.position);

        if (TryStartSkill(distance)) return;

        if (distance <= lookDistance)
            ChangeState(State.Look);
    }

    private void UpdateLook(float distance)
    {
        if (moveLocked)
        {
            StopMove();
            return;
        }

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = 6.5f;
        }

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
            StopMove();
            TryBasicAttack(distance);
            return;
        }

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
        if (anim != null) anim.SetBool("IsMove", true);

        TryStartSkill(distance);
    }

    private void StartCurrentPattern()
    {
        if (currentPattern == null) return;

        StopMove();

        if ((object)currentPattern == (object)shareCirclePattern || (object)currentPattern == (object)hideBehindWallPattern ||
            (object)currentPattern == (object)allDamagePattern || (object)currentPattern == (object)orbCollectPattern || (object)currentPattern == (object)rageTotemPattern)
        {
            LockMove();
            isCasting = true;
        }

        if ((object)currentPattern == (object)conePattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Cast");
                anim.SetTrigger("Cast");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Pattern");

            conePattern.StartPattern(null);
            return;
        }

        if ((object)currentPattern == (object)circlePattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Cast");
                anim.SetTrigger("Cast");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Pattern");

            circlePattern.StartPattern(null);
            return;
        }

        if ((object)currentPattern == (object)rockfallPattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Cast");
                anim.SetTrigger("Cast");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Pattern");

            rockfallPattern.StartPattern(null);
            return;
        }

        if ((object)currentPattern == (object)shareCirclePattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Gimmik_Cast");
                anim.SetTrigger("Gimmik_Cast");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Gimmik");

            shareCirclePattern.StartPattern(null);
            return;
        }

        if ((object)currentPattern == (object)hideBehindWallPattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Gimmik_Cast");
                anim.SetTrigger("Gimmik_Cast");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Gimmik");

            hideBehindWallPattern.StartPattern(null);
            return;
        }

        if ((object)currentPattern == (object)allDamagePattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Gimmik_Cast");
                anim.SetTrigger("Gimmik_Cast");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Gimmik");

            allDamagePattern.StartPattern(null);
            return;
        }

        if ((object)currentPattern == (object)orbCollectPattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Gimmik_Cast");
                anim.SetTrigger("Gimmik_Cast");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Gimmik");

            orbCollectPattern.StartPattern(null);
            return;
        }

        if ((object)currentPattern == (object)rageTotemPattern)
        {
            if (anim != null)
            {
                anim.ResetTrigger("Rage");
                anim.SetTrigger("Rage");
            }

            BigToySystem.Instance.audioMgr.PlaySFX("Boss_Demon_Guard");

            rageTotemPattern.StartPattern(null);
            return;
        }
    }

    private bool TryStartSkill(float distance)
    {
        if (isCasting) return false;
        if (skillTimer > 0f) return false;

        if (enableLeap && leapPattern != null && leapPattern.CanUse() && leapTimer <= 0f && lastPatternId != 1)
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

        bool canCone = enableCone && conePattern != null && conePattern.CanUse() && distance <= castMaxDistance;
        bool canCircle = enableCircle && circlePattern != null && circlePattern.CanUse() && distance <= castMaxDistance;
        bool canRockfall = enableRockfall && rockfallPattern != null && rockfallPattern.CanUse() && distance <= castMaxDistance;

        bool canShare = enableShareCircle && shareCirclePattern != null && shareCirclePattern.CanUse() && distance <= castMaxDistance;
        bool canHide = enableHideBehindWall && hideBehindWallPattern != null && hideBehindWallPattern.CanUse() && distance <= castMaxDistance;
        bool canAllDamage = enableAllDamage && allDamagePattern != null && allDamagePattern.CanUse() && distance <= castMaxDistance;

        bool canOrb = enableOrbCollect && orbCollectPattern != null && orbCollectPattern.CanUse() && distance <= castMaxDistance;
        bool canRage = enableRageTotem && rageTotemPattern != null && rageTotemPattern.CanUse();

        if (!canCone && !canCircle && !canRockfall && !canShare && !canHide && !canAllDamage && !canOrb && !canRage)
            return false;

        if (useOpener && !openerUsed)
        {
            openerUsed = true;

            if (canCone) return StartSkill(conePattern, 0);
            if (canCircle) return StartSkill(circlePattern, 2);
            if (canRockfall) return StartSkill(rockfallPattern, 3);
            if (canShare) return StartSkill(shareCirclePattern, 4);
            if (canHide) return StartSkill(hideBehindWallPattern, 5);
            if (canAllDamage) return StartSkill(allDamagePattern, 6);
            if (canOrb) return StartSkill(orbCollectPattern, 7);
            if (canRage) return StartSkill(rageTotemPattern, 8);
        }

        if (lastPatternId == 0) canCone = false;
        if (lastPatternId == 2) canCircle = false;
        if (lastPatternId == 3) canRockfall = false;
        if (lastPatternId == 4) canShare = false;
        if (lastPatternId == 5) canHide = false;
        if (lastPatternId == 6) canAllDamage = false;
        if (lastPatternId == 7) canOrb = false;
        if (lastPatternId == 8) canRage = false;

        var usable = new List<IBossPattern>();
        var usableIds = new List<int>();

        if (canCone) { usable.Add(conePattern); usableIds.Add(0); }
        if (canCircle) { usable.Add(circlePattern); usableIds.Add(2); }
        if (canRockfall) { usable.Add(rockfallPattern); usableIds.Add(3); }
        if (canShare) { usable.Add(shareCirclePattern); usableIds.Add(4); }
        if (canHide) { usable.Add(hideBehindWallPattern); usableIds.Add(5); }
        if (canAllDamage) { usable.Add(allDamagePattern); usableIds.Add(6); }
        if (canOrb) { usable.Add(orbCollectPattern); usableIds.Add(7); }
        if (canRage) { usable.Add(rageTotemPattern); usableIds.Add(8); }

        if (usable.Count == 0)
        {
            if (enableCone && conePattern != null && conePattern.CanUse() && distance <= castMaxDistance) { usable.Add(conePattern); usableIds.Add(0); }
            if (enableCircle && circlePattern != null && circlePattern.CanUse() && distance <= castMaxDistance) { usable.Add(circlePattern); usableIds.Add(2); }
            if (enableRockfall && rockfallPattern != null && rockfallPattern.CanUse() && distance <= castMaxDistance) { usable.Add(rockfallPattern); usableIds.Add(3); }
            if (enableShareCircle && shareCirclePattern != null && shareCirclePattern.CanUse() && distance <= castMaxDistance) { usable.Add(shareCirclePattern); usableIds.Add(4); }
            if (enableHideBehindWall && hideBehindWallPattern != null && hideBehindWallPattern.CanUse() && distance <= castMaxDistance) { usable.Add(hideBehindWallPattern); usableIds.Add(5); }
            if (enableAllDamage && allDamagePattern != null && allDamagePattern.CanUse() && distance <= castMaxDistance) { usable.Add(allDamagePattern); usableIds.Add(6); }
            if (enableOrbCollect && orbCollectPattern != null && orbCollectPattern.CanUse() && distance <= castMaxDistance) { usable.Add(orbCollectPattern); usableIds.Add(7); }
            if (enableRageTotem && rageTotemPattern != null && rageTotemPattern.CanUse()) { usable.Add(rageTotemPattern); usableIds.Add(8); }
        }

        if (usable.Count == 0) return false;

        int idx = Random.Range(0, usable.Count);
        currentPattern = usable[idx];
        lastPatternId = usableIds[idx];

        isCasting = true;
        skillTimer = skillCooldown;

        currentCastStep = CastStep.Pattern;
        ChangeState(State.CastPattern);
        StartCurrentPattern();
        return true;
    }

    private bool StartSkill(IBossPattern pattern, int patternId)
    {
        if (pattern == null) return false;

        lastPatternId = patternId;
        isCasting = true;
        skillTimer = skillCooldown;

        currentPattern = pattern;
        currentCastStep = CastStep.Pattern;
        ChangeState(State.CastPattern);

        StartCurrentPattern();
        return true;
    }

    private void UpdateCastPattern()
    {
        if (currentCastStep == CastStep.preLeap)
        {
            if (anim != null) anim.SetBool("IsMove", true);
        }
        else if (currentCastStep == CastStep.Pattern)
        {
            StopMove();
        }

        switch (currentCastStep)
        {
            case CastStep.preLeap:
                if (leapPattern != null && leapPattern.IsRunning == false)
                {
                    float dist = Vector3.Distance(transform.position, target.position);

                    if (dist <= castMaxDistance)
                    {
                        currentCastStep = CastStep.Pattern;
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
                        currentPattern.Tick(Time.deltaTime);

                    if (currentPattern.IsRunning == false)
                    {
                        currentCastStep = CastStep.None;
                        currentPattern = null;

                        if (moveLocked) UnlockMove();

                        isCasting = false;
                        ChangeState(State.Look);
                    }
                }
                break;
        }
    }

    public void PlayBossBasicAttackSFX()
    {
        string key = attackSfxToggle ? "Boss_Demon_attack1" : "Boss_Demon_attack2";
        attackSfxToggle = !attackSfxToggle;

        BigToySystem.Instance.audioMgr.PlaySFX(key);
    }

    private void TryBasicAttack(float distance)
    {
        if (isBasicAttacking) return;
        if (distance > basicAttackDistance) return;
        if (basicAtkTimer > 0f) return;

        isBasicAttacking = true;
        basicAtkTimer = basicAttackCooldown;

        StopMove();
        if (anim != null) anim.SetTrigger("BossAttack_1");

        PlayBossBasicAttackSFX();

        PlayerBase ph = target.GetComponent<PlayerBase>();
        if (ph != null) ph.TakeDamage(basicAttackDamage);
    }

    private void StopMove()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        if (anim != null) anim.SetBool("IsMove", false);
    }

    private void ResumeMove()
    {
        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
    }

    private void LockMove()
    {
        moveLocked = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.updateRotation = false;
        }

        if (anim != null) anim.SetBool("IsMove", false);
    }

    private void UnlockMove()
    {
        moveLocked = false;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
    }

    public void AnimRageSummon()
    {
        if (currentPattern == null) return;

        if ((object)currentPattern == (object)rageTotemPattern && rageTotemPattern != null)
            rageTotemPattern.SpawnTotemsFromAnim();
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

    public void EndRagePhase()
    {
        isInvincible = false;
    }

    private void ChangeState(State newstate)
    {
        if (currentState == newstate) return;
        currentState = newstate;
    }

    public void TakeDamage(int dmg)
    {
        if (health == null) return;

        if (isInvincible) return;

        float beforePct = health.GetHPPercent();
        float afterHp = health.currentHp - dmg;
        float rageHp = health.maxHp * rageHpPercent;

        if (!rageTriggered && health.currentHp > rageHp && afterHp <= rageHp)
        {
            health.SetCurrentHP(rageHp);
            rageTriggered = true;
            isInvincible = true;

            if (enableRageTotem && rageTotemPattern != null)
            {
                isCasting = true;
                skillTimer = skillCooldown;
                currentPattern = rageTotemPattern;
                currentCastStep = CastStep.Pattern;
                ChangeState(State.CastPattern);
                StartCurrentPattern();
            }
            return;
        }

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
        PlayCutScene();
    }

    public void PlayCutScene()
    {
        BigToySystem.Instance.Get<EndingsController>().PlayTrueEnding();
    }

    public void ResetFightToFull()
    {
        if (health != null)
        {
            health.SetCurrentHP(health.GetMaxHP());
        }

        isInvincible = false;
        rageTriggered = true;

        isCasting = false;
        isBasicAttacking = false;
        currentPattern = null;
        currentCastStep = CastStep.None;

        if (moveLocked) UnlockMove();

        if (leapPattern != null) leapPattern.ForceStop();
        if (conePattern != null) conePattern.ForceStop();
        if (circlePattern != null) circlePattern.ForceStop();
        if (rockfallPattern != null) rockfallPattern.ForceStop();
        if (shareCirclePattern != null) shareCirclePattern.ForceStop();
        if (hideBehindWallPattern != null) hideBehindWallPattern.ForceStop();
        if (allDamagePattern != null) allDamagePattern.ForceStop();
        if (orbCollectPattern != null) orbCollectPattern.ForceStop();
        if (rageTotemPattern != null) rageTotemPattern.ForceStop();

        lastPatternId = -1;
        openerUsed = false;

        skillTimer = 0f;
        leapTimer = 0f;
        basicAtkTimer = 0f;

        ChangeState(State.Chase);
    }
    public void SetTarget(Transform t)
    {
        target = t;
        BindTargets();
        ChangeState(State.Chase);
    }

}