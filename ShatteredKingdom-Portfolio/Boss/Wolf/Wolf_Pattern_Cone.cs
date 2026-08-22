using System;
using UnityEngine;

public class WolfPattern_Cone : MonoBehaviour, IBossPattern
{
    [SerializeField] private float range = 5f; 
    [SerializeField] private float angleDeg = 120f; 
    [SerializeField] private float telegraphTime = 1.2f;
    [SerializeField] private int damage = 10;

    [Header("Telegraph")]
    [SerializeField] private GameObject telegraphObj;
    [SerializeField] private Renderer telegraphRenderer;
    [SerializeField] private string fillProperty = "_Fill";
    
    [SerializeField] private bool swapXY = true;
    [SerializeField] private float telegraphScaleFix = 1f;
    [SerializeField] private float telegraphForwardOffset = 0.5f;

    [Header("Effect")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private float effectY = 0.05f;
    [SerializeField] private float lifeTime = 1.0f;
    [SerializeField] private float effectScale = 1.5f;


    private Transform target;
    private bool isRunning = false;
    private Action onFinished;

    private float timer;

    private float totalTime;


    [SerializeField] private bool drawDamageGizmo = true;
    [SerializeField] private float gizmoY = 0.05f; 
    [SerializeField] private int gizmoSegments = 24;


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
        if (target == null) return false;
        if (isRunning == true) return false;
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
        onFinished = finished;

        totalTime = telegraphTime;
        if (totalTime < 0.01f)
            totalTime = 0.01f;

        timer = totalTime;

        if (telegraphObj != null)
        {
            telegraphObj.SetActive(true);
        }
        SyncTelegraphSize();
        SetFill(0f);

    }

    
    public void Tick(float dt)
    {
        if (isRunning == false) return;

        timer = timer - dt;

        float progress = 1f - (timer / totalTime);
        if (progress < 0f) progress = 0f;
        if (progress > 1f) progress = 1f;

        
        SetFill(progress);



        if (timer > 0f) return;

        SpawnEffect();


        if (IsInCone_LocalZ(target, range, angleDeg))
        {
            var ph = target.GetComponent<PlayerBase>();
            if (ph != null)
            {
                var boss = GetComponentInParent<Wolf_Boss_Base>();
                int finalDamage = boss != null ? Mathf.RoundToInt(damage * boss.DamageMul) : damage;
                ph.TakeDamage(finalDamage);
                Debug.Log($"부채꼴공격 히트, 데미지 : {finalDamage}");
            }
            
        }


        Finish();
    }

    private bool IsInCone_LocalZ(Transform t, float range, float angleDeg)
    {
        if (t == null) return false;

        
        Transform pivot = transform;

        Vector3 local = pivot.InverseTransformPoint(t.position);
        local.y = 0f; 

        float forward = local.z; 
        if (forward < 0f || forward > range) return false;

        float halfRad = (angleDeg * 0.5f) * Mathf.Deg2Rad;
        float halfWidthAtZ = forward * Mathf.Tan(halfRad);

        return Mathf.Abs(local.x) <= halfWidthAtZ;
    }

    private void SyncTelegraphSize()
    {
        if (telegraphObj == null) return;

        Transform tr = telegraphObj.transform;

        float halfRad = (angleDeg * 0.5f) * Mathf.Deg2Rad;
        float length = range * telegraphScaleFix;
        float width = (1.2f * range * Mathf.Tan(halfRad)) * telegraphScaleFix;

        Vector3 s = tr.localScale;

        if (!swapXY)
        {
            s.x = width;
            s.y = length;
        }
        else
        {
            s.x = length;
            s.y = width;
        }

        tr.localScale = s;

        Vector3 p = tr.localPosition;
        p.x = 0f;
        p.z = (range * telegraphForwardOffset);
        tr.localPosition = p;
    }


    public void ForceStop()
    {
        if (!isRunning) return;

        isRunning = false;
        
        timer = 0f;

        SetFill(0f);

        if(telegraphObj != null)
            telegraphObj.SetActive(false);

        onFinished = null;
    }

    private void Finish()
    {
        isRunning = false;

        SetFill(0f);

        if (telegraphObj != null)
            telegraphObj.SetActive(false);

        if (onFinished != null)
        {
            onFinished();
        }

        onFinished = null;
    }

    private void SetFill(float v)
    {
        if (telegraphRenderer == null) return;

        Material mat = telegraphRenderer.material;
        if(mat == null) return;

        if(mat.HasProperty(fillProperty))
            mat.SetFloat(fillProperty, v);

    }

    private void OnDrawGizmos()
    {
        if (!drawDamageGizmo) return;

        Transform pivot = transform;

        
        Vector3 origin = pivot.position;
        origin.y += gizmoY;

        float halfRad = (angleDeg * 0.5f) * Mathf.Deg2Rad;

        
        Vector3 leftDir = Quaternion.AngleAxis(-angleDeg * 0.5f, Vector3.up) * pivot.forward;
        Vector3 rightDir = Quaternion.AngleAxis(angleDeg * 0.5f, Vector3.up) * pivot.forward;

        leftDir.y = 0f; leftDir.Normalize();
        rightDir.y = 0f; rightDir.Normalize();

        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + leftDir * range);
        Gizmos.DrawLine(origin, origin + rightDir * range);

        
        Vector3 prev = origin + leftDir * range;
        for (int i = 1; i <= gizmoSegments; i++)
        {
            float t = i / (float)gizmoSegments;
            float ang = Mathf.Lerp(-angleDeg * 0.5f, angleDeg * 0.5f, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * pivot.forward;
            dir.y = 0f;
            dir.Normalize();

            Vector3 pt = origin + dir * range;
            Gizmos.DrawLine(prev, pt);
            prev = pt;
        }

        
        Gizmos.DrawSphere(origin, 0.08f);
    }

    private void SpawnEffect()
    {
        Debug.Log("SpawnEffect() 호출됨");

        if (effectPrefab == null)
        {
            Debug.LogWarning("effectPrefab이 null임");
            return;
        }

        Vector3 pos = transform.position;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
            pos = hit.point;

        pos.y += effectY;

        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);

        GameObject fx = Instantiate(effectPrefab, pos, rot);

        fx.transform.localScale = Vector3.one * effectScale;

        foreach (Transform t in fx.GetComponentInChildren<Transform>())
            t.localScale = Vector3.one;

        var particles = fx.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Clear();
            ps.Play();
        }

        if (lifeTime > 0f)
            Destroy(fx, lifeTime);
    }

}