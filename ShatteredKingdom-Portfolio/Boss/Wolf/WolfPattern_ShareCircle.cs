using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WolfPattern_ShareCircle : MonoBehaviour, IBossPattern
{
    [SerializeField] private GameObject circlePrefab;
    [SerializeField] private float followTime = 0.3f;
    [SerializeField] private float growTime = 1.2f;
    [SerializeField] private float maxRadius = 5f;
    [SerializeField] private int totalDamage = 80;
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private bool punishOutside = false;
    [SerializeField] private int outsideDamage = 80;
    [SerializeField] private float patternCooldown = 0f;
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private float effectLifeTime = 1.5f;

    public bool IsRunning { get; private set; }

    private Transform target;
    private NavMeshAgent agent;
    private GameObject circleObj;
    private ShareZone shareZone;
    private float t;
    private bool locked;
    private float nextReadyTime = 0f;
    private Action finishedCb;


    private float finalRadius;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoY = 0.05f;

    private Vector3 gizmoCenter;
    private float gizmoRadius;


    private void Awake()
    {
        agent = GetComponentInParent<NavMeshAgent>();
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public bool CanUse()
    {
        return Time.time >= nextReadyTime;
    }

    public void StartPattern(Action finished)
    {
        if (IsRunning) return;
        if (circlePrefab == null || target == null)
        {
            if (finished != null) finished.Invoke();
            return;
        }

        nextReadyTime = Time.time + patternCooldown;
        finishedCb = finished;

        circleObj = Instantiate(circlePrefab);
        circleObj.SetActive(true);

        var mr = circleObj.GetComponentInChildren<MeshRenderer>(true);
        if (mr != null) mr.enabled = true;
        if (mr != null) mr.material.color = Color.red;


        shareZone = circleObj.GetComponent<ShareZone>();
        if (shareZone == null)
            shareZone = circleObj.AddComponent<ShareZone>();

        t = 0f;
        locked = false;
        IsRunning = true;

        Vector3 pos = GetGroundPos(target.position);
        SetCircleScaleByRadius(0.2f);
        circleObj.transform.position = new Vector3(pos.x, 0f, pos.z);

        gizmoCenter = circleObj.transform.position;
        gizmoRadius = 0.2f;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void Tick(float dt)
    {
        if (!IsRunning) return;

        if (circleObj == null || target == null)
        {
            End(false);
            return;
        }

        t += dt;

        if (!locked)
        {
            Vector3 pos = GetGroundPos(target.position);
            circleObj.transform.position = new Vector3(pos.x, 0f, pos.z);

            if (t >= followTime)
                locked = true;
        }

        float growT = (t - followTime) / growTime;
        if (growT < 0f) growT = 0f;
        if (growT > 1f) growT = 1f;

        float radiusNow = 0.2f + (maxRadius - 0.2f) * growT;

        finalRadius = radiusNow;


        SetCircleScaleByRadius(radiusNow);

        gizmoRadius = radiusNow;
        gizmoCenter = circleObj.transform.position;

        if (growT >= 1f)
        {
            ResolveDamage();
            End(true);
        }
    }

    public void ForceStop()
    {
        End(false);
    }

    private void ResolveDamage()
    {
        if (circleObj == null) return;

        SpawnEffect();

        var boss = GetComponentInParent<Wolf_Boss_Base>();
        float mul = boss != null ? boss.DamageMul : 1f;

        int scaledTotal = Mathf.RoundToInt(totalDamage * mul);
        int scaledOutside = Mathf.RoundToInt(outsideDamage * mul);

        var rawList = shareZone != null ? shareZone.GetInsideList() : null;

        Vector3 center = circleObj.transform.position;
        center.y = 0f;

        List<PlayerBase> insideList = new List<PlayerBase>();
        if (rawList != null)
        {
            foreach (var p in rawList)
            {
                if (p == null) continue;

                Vector3 pos = p.transform.position;
                pos.y = 0f;

                if (Vector3.Distance(center, pos) <= finalRadius)
                    insideList.Add(p);
            }
        }

        int n = insideList.Count;

        var allPlayers = GameObject.FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);

        if (n < minPlayers)
        {
            foreach (var p in insideList)
                if (p != null)
                {
                    p.TakeDamage(scaledTotal);
                    Debug.Log($"모여맞기 실패, 데미지 : {scaledTotal}");
                }

            if (punishOutside)
            {
                foreach (var p in allPlayers)
                    if (p != null && !insideList.Contains(p))
                        p.TakeDamage(scaledOutside);

                Debug.Log($"모여맞기 실패(밖), 데미지 : {scaledOutside}");
            }
            return;
        }

        int each = Mathf.CeilToInt((float)scaledTotal / n);
        foreach (var p in insideList)
            if (p != null)
            {
                p.TakeDamage(each);
                Debug.Log($"모여맞기 성공, 데미지 : 각각 {each}");
            }
    }

    private Vector3 GetGroundPos(Vector3 worldPos)
    {
        Ray ray = new Ray(worldPos + Vector3.up, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10f))
            return hit.point;
        return worldPos;
    }

    private void SetCircleScaleByRadius(float radius)
    {
        if (circleObj == null) return;
        float d = radius * 2f;
        circleObj.transform.localScale = new Vector3(d, 0.05f, d);


    }

    private void End(bool invokeFinished)
    {
        IsRunning = false;

        if (circleObj != null)
            Destroy(circleObj);

        circleObj = null;
        shareZone = null;

        if (agent != null)
            agent.isStopped = false;

        var cb = finishedCb;
        finishedCb = null;

        if (invokeFinished && cb != null)
            cb.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;


        Vector3 c = gizmoCenter;
        c.y = gizmoY;


        if (gizmoRadius <= 0.001f) return;

        Gizmos.DrawWireSphere(c, gizmoRadius);
        Gizmos.DrawSphere(c, 0.08f);
    }

    private void SpawnEffect()
    {
        if (effectPrefab != null)
        {
            Vector3 spawnPos = circleObj.transform.position;
            spawnPos.y += 0.05f;

            var fx = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            Destroy(fx, effectLifeTime);
        }
    }

}