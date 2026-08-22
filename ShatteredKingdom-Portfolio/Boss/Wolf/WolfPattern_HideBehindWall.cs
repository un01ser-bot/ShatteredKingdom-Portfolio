using System;
using UnityEngine;

public class WolfPattern_HideBehindWall : MonoBehaviour, IBossPattern
{
    [SerializeField] float telegraphTime = 1.2f;
    [SerializeField] Boss_Wolf_Projectile projectilePrefab;
    [SerializeField] Transform shootOrigin;
    [SerializeField] float spawnRadius = 0.6f;
    [SerializeField] LayerMask wallMask;

    [Header("Telegraph Blink")]
    [SerializeField] GameObject telegraphObj;
    [SerializeField] float blinkInterval = 0.2f;

    bool isRunning;
    float t;
    Action finishedCb;
    Transform target;

    float blinkTimer;
    int blinkCount;
    bool blinkOn;

    public bool IsRunning
    {
        get { return isRunning; }
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public bool CanUse()
    {
        if (isRunning) return false;
        if (projectilePrefab == null) return false;
        return true;
    }

    public void StartPattern(Action finished)
    {
        if (!CanUse())
        {
            if (finished != null) finished();
            return;
        }

        isRunning = true;
        t = 0f;
        finishedCb = finished;

        blinkTimer = blinkInterval;
        blinkCount = 0;
        blinkOn = false;

        if (telegraphObj != null)
        {
            telegraphObj.SetActive(false);
            telegraphObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void ForceStop()
    {
        if (!isRunning) return;

        isRunning = false;
        t = 0f;

        if (telegraphObj != null)
            telegraphObj.SetActive(false);

        finishedCb = null;
    }

    public void Tick(float dt)
    {
        if (!isRunning) return;

        t += dt;

        if (telegraphObj != null)
        {

            blinkTimer -= dt;
            if (blinkTimer <= 0f)
            {
                blinkTimer += blinkInterval;

                blinkOn = !blinkOn;
                telegraphObj.SetActive(blinkOn);

                if (blinkOn)
                {
                    blinkCount++;
                    if (blinkCount >= 3)
                    {
                        telegraphObj.SetActive(false);
                        FireRing();
                        Finish();
                        return;
                    }
                }
            }
        }

        if (t >= telegraphTime)
        {
            if (telegraphObj != null)
                telegraphObj.SetActive(false);

            FireRing();
            Finish();
        }
    }

    

    void Finish()
    {
        isRunning = false;
        var cb = finishedCb;
        finishedCb = null;
        if (cb != null) cb();
    }

    void FireRing()
    {
        Transform origin;

        if (shootOrigin != null)
            origin = shootOrigin;
        else
            origin = transform;

        Vector3 center = origin.position;

        int bulletCount = 8;
        float angleStep = 360f / bulletCount;
        float offset = UnityEngine.Random.Range(0f, angleStep);

        for (int i = 0; i < bulletCount; i++)
        {
            float ang = offset + angleStep * i;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;

            Vector3 spawnPos = center + dir * spawnRadius;
            var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
            proj.SetWallMask(wallMask);
            proj.Fire(dir);
        }
    }
}