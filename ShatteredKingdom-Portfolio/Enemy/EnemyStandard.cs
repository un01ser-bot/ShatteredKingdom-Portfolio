using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStandard : MonoBehaviour, IDamageable
{
    protected enum State { Idle, Chase, Attack}

    [SerializeField] protected EnemyData data;
    [SerializeField] protected Transform player;
    [SerializeField] protected bool autoFindPlayer = true; //싱글 테스트용입니다

    protected NavMeshAgent agent;
    protected Animator anim;

    protected Transform target;

    protected EnemyBulletPool bulletPool;
    protected LayerMask playerLayer;

    protected int currentHP;
    protected State currentState;

    protected bool isAttacking = false;
    protected bool isDie = false;

    [Header("던전레벨별 스케일링")]
    [SerializeField] private int appliedDungeonLevel = 1;

    private int baseMaxHP;
    private int baseAttackDamage;
    private int baseExp;
    private int baseGold;

    private int scaledMaxHP;
    private int scaledAttackDamage;
    private int scaledExp;
    private int scaledGold;

    public event Action<int, int> OnRewardDropped;

    [SerializeField, Range(0f, 1f)]
    protected float runeDropChance = 0.2f;

    [SerializeField]
    private int runePowderItemId = 41;

    protected bool isAggro = false;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        AutoBind();

        if (data != null)
        {
            baseMaxHP = data.maxHP;
            baseAttackDamage = data.attackDamage;
            baseExp = data.expReward;
            baseGold = data.goldReward;

        }

        ApplyDungeonScaling(1);

        ChangeState(State.Idle);
    }

    protected virtual void Start()
    {
        if (autoFindPlayer && player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
            SetTarget(player);
    }

    protected virtual void AutoBind()
    {
        
        //GameObject player = GameObject.FindGameObjectWithTag("Player");
        //if (player != null)
        //    target = player.transform;

        
        if (bulletPool == null)
            bulletPool = FindFirstObjectByType<EnemyBulletPool>();

        
        playerLayer = LayerMask.GetMask("Player");

        
    }

    protected void TryFindPlayer()
    {
        if (target != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            target = p.transform;
        }
    }

    //던전에서 스폰할때 이 함수 호출하고 매개변수에 던전레벨 넣어주시면 됩니다
    public void ApplyDungeonScaling(int dungeonLevel)
    {
        if (data == null) return;

        if (dungeonLevel < 1) dungeonLevel = 1;
        appliedDungeonLevel = dungeonLevel;

        float plusHP = 1f + (dungeonLevel - 1) * 2.5f;
        float plusAttackDamage = 1f + (dungeonLevel - 1) * 0.4f;
        float plusReward = 1f + (dungeonLevel - 1) * 0.5f;

        scaledMaxHP = Mathf.Max(1, Mathf.RoundToInt(baseMaxHP * plusHP));
        scaledAttackDamage = Mathf.Max(1, Mathf.RoundToInt(baseAttackDamage * plusAttackDamage));
        scaledExp = Mathf.Max(0,Mathf.RoundToInt(baseExp * plusReward));
        scaledGold = Mathf.Max(0, Mathf.RoundToInt(baseGold * plusReward));

        currentHP = scaledMaxHP;
        isDie = false;
        isAttacking = false;


    }

    //----테스트용입니다
    private void OnValidate()
    {
        if(!Application.isPlaying) return;
        ApplyDungeonScaling(appliedDungeonLevel);
    }
    //-------------
    protected virtual void Update()
    {
        

        if (isDie) return;

        TryFindPlayer();

        switch (currentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;   
        }
    }

    protected virtual void UpdateIdle()
    {
        if (target == null || data == null) return;

        if (isAggro)
        {
            ChangeState(State.Chase);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= data.detectDistance)
            ChangeState(State.Chase);
    }

    protected virtual void UpdateChase()
    {
        if (target == null || data == null || isDie)
        {
            ChangeState(State.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (!isAggro && dist > data.detectDistance)
        {
            ChangeState(State.Idle);
            return;
        }

        if (dist <= data.attackRange)
        {
            ChangeState(State.Attack);
            return;
        }

        agent.isStopped = false;
        if (isAttacking) return;
        if (!isAttacking)
        {
            agent.SetDestination(target.position);
            if (anim != null) anim.SetBool("isMove", true);
        }
       
    }

    protected virtual void UpdateAttack()
    {
        if (isDie) return;

        if (target == null || data == null)
        {
            ChangeState(State.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > data.attackRange)
        {
            ChangeState(State.Chase);
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
        if (anim != null) anim.SetBool("isMove", false);

        DoAttack();
    }

    public int GetAttackDamage()
    {
        return scaledAttackDamage;
    }

    protected virtual void DoAttack() { }

    protected virtual void ChangeState(State newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                agent.ResetPath();
                if (anim != null) anim.SetBool("isMove", false);
                break;

            case State.Chase:
                agent.isStopped = false;
                break;

            case State.Attack:
                agent.isStopped = true;
                agent.ResetPath();
                if (anim != null) anim.SetBool("isMove", false);
                break;

            
        }
    }

    public virtual void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public virtual void TakeDamage(int dmg)
    {
        if (isDie || currentHP <= 0) return;

        currentHP -= dmg;

        if (anim != null)
            anim.SetTrigger("isDamage");

        isAggro = true;
        ChangeState(State.Chase);

        if (currentHP <= 0 && !isDie)
        {
            currentHP = 0;
            Die();
        }
    }

    public float GetHPPercent()
    {
        return (float)currentHP / scaledMaxHP;
    }

    public virtual void DamageEnd()
    {
        isAttacking = false;
    }


    protected virtual void Die()
    {
        if (isDie) return;

        GetReward();
        isDie = true;
        isAttacking = false;

        if(agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        
        if (anim != null)
        {
            anim.SetBool("isMove", false);
            anim.SetTrigger("Die");

        }

        if (OnRewardDropped != null)
        {
            OnRewardDropped.Invoke(scaledExp, scaledGold);
        }
        var col = this.GetComponent<CapsuleCollider>();
        if(col != null) col.enabled = false;

        var ui = GetComponentInChildren<Enemy_HPUI>();
        if (ui != null) ui.gameObject.SetActive(false);

        Destroy(gameObject, 3f);
    }

    private void GetReward()
    {
        BigToySystem.Instance.Get<InventoryManager>().AddMoney(scaledGold);
        BigToySystem.Instance.Get<DataManager>().Player.GetEXP(scaledExp);
        BigToySystem.Instance.Get<QuestManager>().AddProgress(data.questKey, 1);

        if (UnityEngine.Random.value < runeDropChance)
        {
            BigToySystem.Instance.Get<InventoryManager>().AddItem(runePowderItemId, 1);
        }
    }
    
    

}