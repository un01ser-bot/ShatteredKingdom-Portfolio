using System;
using UnityEngine;

public class WolfPattern_Circle : MonoBehaviour, IBossPattern
{
    [SerializeField] private float radius = 4f;
    [SerializeField] private float telegraphTime = 1.2f;
    [SerializeField] private int damage = 10;

    [Header("Telegraph")]
    [SerializeField] private GameObject telegraphObj;
    [SerializeField] private Renderer telegraphRenderer;
    [SerializeField] private string fillProperty = "_Fill";

    [SerializeField, Range(0.1f, 1f)]
    private float circleFillRatio = 0.8f;

    [Header("Effect")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private float effectY = 0.05f;
    [SerializeField] private float effectLifeTime = 1f;

    private Transform target;
    private bool isRunning = false;
    private Action onFinished;



    private float timer;
    private float totalTime;

    public bool IsRunning => isRunning;

    [SerializeField] private float insideEpsilon = 0.05f;

    [SerializeField] private bool drawDamageGizmo = true;
    [SerializeField] private float gizmoY = 0.05f;


    public void SetTarget(Transform t)
    {
        target = t;
    }

    public bool CanUse()
    {
        if (target == null) return false;
        if (isRunning) return false;
        return true;
    }

    public void StartPattern(Action finished)
    {
        if (!CanUse())
        {
            finished?.Invoke();
            return;
        }

        isRunning = true;
        onFinished = finished;

        totalTime = telegraphTime;
        if (totalTime < 0.01f) totalTime = 0.01f;

        timer = totalTime;

        if (telegraphObj != null)
            telegraphObj.SetActive(true);

        //SyncTelegraphSize();
        SetFill(0f);
    }

    private float GetTelegraphWorldRadius()
    {
        if (telegraphRenderer == null) return radius;

        Bounds b = telegraphRenderer.bounds;

        float rx = b.extents.x;
        float rz = b.extents.z;

        return Mathf.Max(rx, rz) * circleFillRatio;
    }

    public void Tick(float dt)
    {
        if (!isRunning) return;

        timer -= dt;

        float t = 1f - (timer / totalTime);
        t = Mathf.Clamp01(t);

        float progress = Mathf.Lerp(0.7f, 0.8f, t);
        

        SetFill(progress);

        if (timer > 0f) return;

        float r = GetTelegraphWorldRadius() + insideEpsilon;

        SpawnEffect();

        if (IsInCircleWorld(target, r))
        {
            PlayerBase ph = target.GetComponent<PlayerBase>();
            if (ph != null)
            {
                var boss = GetComponentInParent<Wolf_Boss_Base>();

                int finalDamage = boss != null
                    ? Mathf.RoundToInt(damage * boss.DamageMul)
                    : damage;

                ph.TakeDamage(finalDamage);

                Debug.Log($"원형공격 히트, 데미지 : {finalDamage} (base:{damage})");
            }
        }

        Finish();
    }

    private bool IsInCircleWorld(Transform t, float rWorld)
    {
        if (t == null) return false;

        Vector3 center = transform.position;
        Vector3 tp = t.position;

        center.y = 0f;
        tp.y = 0f;

        return Vector3.Distance(center, tp) <= rWorld;
    }

    

    private void SyncTelegraphSize()
    {
        if (telegraphObj == null) return;

        Transform tr = telegraphObj.transform;

        float diameter = radius * 2f;

        Vector3 scale = tr.localScale;
        scale.x = diameter;
        scale.y = diameter;
        tr.localScale = scale;

        Vector3 pos = tr.localPosition;
        pos.x = 0f;
        pos.z = 0f;
        tr.localPosition = pos;
    }

    public void ForceStop()
    {
        if (!isRunning) return;

        isRunning = false;
        timer = 0f;

        SetFill(0f);

        if (telegraphObj != null)
            telegraphObj.SetActive(false);

        onFinished = null;
    }

    private void Finish()
    {
        isRunning = false;

        SetFill(0f);

        if (telegraphObj != null)
            telegraphObj.SetActive(false);

        onFinished?.Invoke();
        onFinished = null;
    }

    private void SetFill(float v)
    {
        if (telegraphRenderer == null) return;

        Material mat = telegraphRenderer.material;
        if (mat == null) return;

        if (mat.HasProperty(fillProperty))
            mat.SetFloat(fillProperty, v);
    }

    private void OnDrawGizmos()
    {
        if (!drawDamageGizmo) return;

        Transform baseTf = (telegraphObj != null) ? telegraphObj.transform : transform;

        Vector3 c = baseTf.position;
        c.y = gizmoY;

        float r = Application.isPlaying ? (GetTelegraphWorldRadius() + insideEpsilon) : radius;

        Gizmos.DrawWireSphere(c, r);
        Gizmos.DrawSphere(c, 0.08f);
    }

    private void SpawnEffect()
    {
        if (effectPrefab == null)
            return;

        Vector3 pos = transform.position;
        pos.y += effectY;

        GameObject fx = Instantiate(effectPrefab,pos,Quaternion.identity);
        Destroy(fx,effectLifeTime);
    }

}