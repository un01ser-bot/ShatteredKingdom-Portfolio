using System;
using System.Collections.Generic;
using UnityEngine;

public class FinalPattern_OrbCollect : MonoBehaviour, IBossPattern
{
    [Header("Orb Prefab")]
    [SerializeField] private GameObject orbPrefab;

    [Header("Spawn")]
    [SerializeField] private float spawnRadius = 6f;
    [SerializeField] private float orbLifetime = 6f;

    [Header("Heal")]
    [SerializeField] private float healPercentPerOrb = 0.06f;
    [SerializeField] private float hpThreshold = 0.5f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 12f;

    public bool IsRunning { get; private set; }

    private Transform target;

    private Transform bossTr;
    private FianlBoss_Health bossHp;

    private Action finishedCb;
    private float nextReadyTime = 0f;

    private readonly List<GameObject> spawnedOrbs = new List<GameObject>();
    private float runningTimer = 0f;

    public void BindBoss(Transform boss, FianlBoss_Health hp)
    {
        bossTr = boss;
        bossHp = hp;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public bool CanUse()
    {
        if (IsRunning) return false;
        if (Time.time < nextReadyTime) return false;
        if (target == null) return false;
        if (bossTr == null || bossHp == null) return false;

        return bossHp.GetHPPercent() < hpThreshold;
    }

    public void StartPattern(Action finished)
    {
        if (!CanUse())
        {
            finished?.Invoke();
            return;
        }

        IsRunning = true;
        finishedCb = finished;
        runningTimer = 0f;

        Spawn4Orbs();
    }

    public void Tick(float dt)
    {
        if (!IsRunning) return;

        runningTimer += dt;

        CleanupNullOrbs();

        if (runningTimer >= orbLifetime)
        {
            ForceStop();
            return;
        }

        if (spawnedOrbs.Count == 0)
        {
            End();
        }
    }

    public void ForceStop()
    {
        if (!IsRunning) return;

        for (int i = 0; i < spawnedOrbs.Count; i++)
        {
            if (spawnedOrbs[i] != null)
                Destroy(spawnedOrbs[i]);
        }
        spawnedOrbs.Clear();

        End();
    }

    private void Spawn4Orbs()
    {
        if (orbPrefab == null) return;

        float maxHP = bossHp.GetMaxHP();
        float healAmount = maxHP * healPercentPerOrb;

        Vector3 center = bossTr.position;

        Vector3[] dirs =
        {
            bossTr.forward,
            -bossTr.forward,
            bossTr.right,
            -bossTr.right
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = center + dirs[i].normalized * spawnRadius;

            GameObject orbObj = Instantiate(orbPrefab, pos, Quaternion.identity);
            spawnedOrbs.Add(orbObj);

            FinalOrb orb = orbObj.GetComponent<FinalOrb>();
            if (orb != null)
                orb.Init(bossTr, bossHp, healAmount);
        }
    }

    private void CleanupNullOrbs()
    {
        for (int i = spawnedOrbs.Count - 1; i >= 0; i--)
        {
            if (spawnedOrbs[i] == null)
                spawnedOrbs.RemoveAt(i);
        }
    }

    private void End()
    {
        IsRunning = false;
        nextReadyTime = Time.time + cooldown;

        var cb = finishedCb;
        finishedCb = null;
        cb?.Invoke();
    }
}