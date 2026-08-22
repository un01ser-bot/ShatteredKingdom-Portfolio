using System;
using System.Collections.Generic;
using UnityEngine;

public class FinalPattern_RageTotem : MonoBehaviour, IBossPattern
{
    public bool IsRunning { get; private set; }

    [SerializeField] private GameObject totemPrefab;
    [SerializeField] private int totemCount = 1;
    [SerializeField] private float spawnRadius = 5f;

    [SerializeField] private float timeLimit = 25f;

    [SerializeField] private GameObject invinciblePrefab;
    [SerializeField] private GameObject healPrefab;

    private Transform target;
    private Final_Boss_Base boss;
    private FianlBoss_Health bossHp;

    private Action finishedCb;
    private float nextReadyTime = 0f;

    private readonly List<GameObject> totems = new List<GameObject>();
    private float timeLeft;
    private bool spawned = false;

    private GameObject invincibleInstance;

    public void BindBoss(Final_Boss_Base b, FianlBoss_Health hp)
    {
        boss = b;
        bossHp = hp;
    }

    public void SetTarget(Transform t) => target = t;

    public bool CanUse()
    {
        if (IsRunning) return false;
        if (Time.time < nextReadyTime) return false;
        if (boss == null || bossHp == null) return false;

        return bossHp.GetHPPercent() <= 0.10f;
    }

    public void StartPattern(Action finished)
    {
        if (IsRunning) { finished?.Invoke(); return; }

        IsRunning = true;
        finishedCb = finished;

        timeLeft = timeLimit;
        spawned = false;
        totems.Clear();

        if (boss != null)
        {
            invincibleInstance = SpawnPrefab(invinciblePrefab, boss.transform.position, boss.transform);
        }
    }

    public void SpawnTotemsFromAnim()
    {
        if (!IsRunning) return;
        if (spawned) return;

        spawned = true;

        if (totemPrefab == null || boss == null) return;

        Vector3 center = boss.transform.position;

        for (int i = 0; i < totemCount; i++)
        {
            Vector2 r = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 pos = center + new Vector3(r.x, 0f, r.y);

            GameObject obj = Instantiate(totemPrefab, pos, Quaternion.identity);
            totems.Add(obj);
        }
    }

    public void Tick(float dt)
    {
        if (!IsRunning) return;

        timeLeft -= dt;

        if (timeLeft <= 0f)
        {
            IsRunning = false;
            nextReadyTime = Time.time + 999999f;

            if (boss != null) boss.ResetFightToFull();

            if (invincibleInstance != null)
                Destroy(invincibleInstance);

            if (boss != null)
                SpawnPrefab(healPrefab, boss.transform.position, boss.transform, 1f);
       
            var cb0 = finishedCb;
            finishedCb = null;
            cb0?.Invoke();
            return;
        }

        for (int i = totems.Count - 1; i >= 0; i--)
        {
            if (totems[i] == null) totems.RemoveAt(i);
        }

        if (spawned && totems.Count == 0)
        {
            IsRunning = false;
            nextReadyTime = Time.time + 999999f;

            if (boss != null) boss.EndRagePhase();
            if (boss != null && boss.anim != null) boss.anim.SetTrigger("Rage_End");

            if (invincibleInstance != null)
                Destroy(invincibleInstance);

            var cb1 = finishedCb;
            finishedCb = null;
            cb1?.Invoke();
        }
    }

    public void ForceStop()
    {
        if (!IsRunning) return;

        for (int i = 0; i < totems.Count; i++)
        {
            if (totems[i] != null) Destroy(totems[i]);
        }
        totems.Clear();

        if (invincibleInstance != null)
            Destroy(invincibleInstance);

        IsRunning = false;

        var cb = finishedCb;
        finishedCb = null;
        cb?.Invoke();
    }

    private GameObject SpawnPrefab(GameObject prefab, Vector3 position, Transform parent = null, float lifeTime = 0f)
    {
        if (prefab == null) 
            return null;

        GameObject obj = Instantiate(prefab, position, Quaternion.identity);

        if (parent != null)
            obj.transform.SetParent(parent, true);

        if (lifeTime > 0f)
            Destroy(obj,lifeTime);

        return obj;
    }
}