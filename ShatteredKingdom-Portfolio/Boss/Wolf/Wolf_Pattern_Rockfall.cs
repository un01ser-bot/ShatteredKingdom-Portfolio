using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Wolf_Pattern_Rockfall : MonoBehaviour, IBossPattern
{
    private Transform target;
    private Action onFinished;
    private Coroutine co;

    [SerializeField] private GameObject telegraphPrefab;
    [SerializeField] private GameObject rockPrefab;

    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int damage = 1;
    [SerializeField] private float impactRadius = 1.8f;

    [SerializeField] private int count = 6;
    [SerializeField] private float spawnRadiusMin = 2f;
    [SerializeField] private float spawnRadiusMax = 8f;

    [SerializeField] private float telegraphTime = 2f;

    [SerializeField] private float rockSpawnHeight = 8f;
    [SerializeField] private float rockFallSpeed = 18f;

    [SerializeField] private float cooldown = 6f;
    private float cooldownTimer;

    private readonly List<GameObject> telegraphs = new();

    public bool IsRunning { get; private set; }

    [Header("Debug Gizmos")]
    [SerializeField] private bool drawDamageGizmos = true;
    [SerializeField] private float gizmoY = 0.05f;
    [SerializeField] private float gizmoKeepSeconds = 3f;
    [SerializeField] private bool logDamageHits = false;

    private struct ImpactDebug
    {
        public Vector3 pos;
        public float radius;
        public float time;
        public int hitcount;
    }

    private readonly List<ImpactDebug> recentImpacts = new();
    private readonly List<Collider> lastHitColliders = new();


    public void SetTarget(Transform t)
    {
        target = t;
    }

    public bool CanUse()
    {
        if (IsRunning) return false;
        if (target == null) return false;

        return true;
    }

    public void StartPattern(Action finished)
    {
        if (!CanUse())
        {
            if (finished != null)
                finished();
            return;
        }

        onFinished = finished;
        IsRunning = true;

        co = StartCoroutine(CoPlay());
    }

    public void ForceStop()
    {
        if (co != null)
            StopCoroutine(co);

        co = null;
        ClearTelegraphs();
        IsRunning = false;

        if (onFinished != null)
        {
            onFinished();
            onFinished = null;
        }
    }

    public void Tick(float dt)
    {
        //비어있는거 맞습니다
    }

    private IEnumerator CoPlay()
    {
        if (target == null)
        {
            Finish();
            yield break;
        }

        List<Vector3> points = new List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            Vector3 p;
            if (TryGetPointNearTarget(out p))
            {
                points.Add(p);
                SpawnTelegraph(p, impactRadius);
            }
        }

        yield return new WaitForSeconds(telegraphTime);

        BigToySystem.Instance.audioMgr.PlaySFX("Boss_Wolf_Rockfall");

        foreach (var p in points)
        {
            DoImpactDamage(p, impactRadius, damage);

            if (rockPrefab != null)
                StartCoroutine(SpawnAndFallRock(p));
        }

        ClearTelegraphs();
        Finish();
    }

    private void Finish()
    {
        IsRunning = false;
        co = null;

        if (onFinished != null)
        {
            onFinished();
            onFinished = null;
        }
    }

    private bool TryGetPointNearTarget(out Vector3 point)
    {
        point = Vector3.zero;

        for (int t = 0; t < 20; t++)
        {
            Vector2 rnd = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(spawnRadiusMin, spawnRadiusMax);
            Vector3 desired = target.position + new Vector3(rnd.x, 0f, rnd.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(desired, out hit, 2.0f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        return false;
    }

    private void SpawnTelegraph(Vector3 pos, float radius)
    {
        if (telegraphPrefab == null) return;

        GameObject tg = Instantiate(telegraphPrefab);
        tg.transform.position = pos + Vector3.up * 0.02f;

        float d = radius * 2f;
        tg.transform.localScale = new Vector3(d, d, 1f);

        telegraphs.Add(tg);
    }

    private void ClearTelegraphs()
    {
        foreach (var tg in telegraphs)
        {
            if (tg != null)
                Destroy(tg);
        }
        telegraphs.Clear();
    }

    private void DoImpactDamage(Vector3 pos, float radius, int dmg)
    {
        //디버그용
        if (drawDamageGizmos)
        {
            float now = Time.time;
            for (int i = recentImpacts.Count - 1; i >= 0; i--)
            {
                if (now - recentImpacts[i].time > gizmoKeepSeconds)
                    recentImpacts.RemoveAt(i);
            }
        }


        Collider[] cols = Physics.OverlapSphere(pos, radius, playerLayer);

        //디버그용
        if (drawDamageGizmos)
        {
            lastHitColliders.Clear();
            for (int i = 0; i < cols.Length; i++)
            {
                lastHitColliders.Add(cols[i]);
            }
            recentImpacts.Add(new ImpactDebug { pos = pos, radius = radius, time = Time.time, hitcount = cols.Length });

        }

        HashSet<PlayerBase> hitPlayers = new HashSet<PlayerBase>();

        for (int i = 0; i < cols.Length; i++)
        {
            

            PlayerBase ph = cols[i].GetComponentInParent<PlayerBase>();
            if (ph != null && hitPlayers.Add(ph))
            {
                var boss = GetComponentInParent<Wolf_Boss_Base>();
                int finalDamage = boss != null ? Mathf.RoundToInt(damage * boss.DamageMul) : damage;
                ph.TakeDamage(finalDamage);
                Debug.Log($"낙석 히트, 데미지 : {finalDamage}");

            }
        }
    }

    private IEnumerator SpawnAndFallRock(Vector3 impactPos)
    {
        Vector3 startPos = impactPos + Vector3.up * rockSpawnHeight;
        GameObject rock = Instantiate(rockPrefab, startPos, Quaternion.identity);

        while (rock != null && Vector3.Distance(rock.transform.position, impactPos) > 0.05f)
        {
            rock.transform.position = Vector3.MoveTowards(rock.transform.position, impactPos, rockFallSpeed * Time.deltaTime);
            yield return null;
        }

        if (rock != null)
            Destroy(rock, 2f);
    }

    private void OnDrawGizmos()
    {
        if (!drawDamageGizmos) return;
        if (recentImpacts == null) return;

        float now = Application.isPlaying ? Time.time : 0f;


        for (int i = 0; i < recentImpacts.Count; i++)
        {
            var imp = recentImpacts[i];


            if (Application.isPlaying && now - imp.time > gizmoKeepSeconds) continue;


            Gizmos.color = (imp.hitcount > 0) ? Color.green : Color.red;

            Vector3 p = imp.pos + Vector3.up * gizmoY;
            Gizmos.DrawWireSphere(p, imp.radius);


            Gizmos.DrawSphere(p, 0.05f);
        }


        if (lastHitColliders != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < lastHitColliders.Count; i++)
            {
                var c = lastHitColliders[i];
                if (c == null) continue;
                Gizmos.DrawSphere(c.bounds.center, 0.08f);
            }
        }
    }

}