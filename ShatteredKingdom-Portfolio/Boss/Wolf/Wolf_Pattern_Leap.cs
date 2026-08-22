using System;
using UnityEngine;
using UnityEngine.AI;

public class WolfPattern_Leap : MonoBehaviour, IBossPattern
{
    [SerializeField] private float jumpRadius = 2.5f;
    [SerializeField] private float sampleRadius = 2.0f;
    [SerializeField] private float leapDuration = 0.45f;
    [SerializeField] private float leapHeight = 3.5f;

    private NavMeshAgent agent;
    private Transform target;

    private bool isRunning = false;
    private Action onFinished;

    private Vector3 startPos;
    private Vector3 endPos;
    private float elapsed;

    public bool IsRunning
    {
        get { return isRunning; }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
        if (CanUse() == false)
        {
            if (finished != null) finished();
            return;
        }

        if (leapDuration < 0.01f)
            leapDuration = 0.01f;

        Vector2 rand = UnityEngine.Random.insideUnitCircle;
        if (rand == Vector2.zero) rand = Vector2.right;
        rand = rand.normalized * jumpRadius;

        Vector3 desired = target.position + new Vector3(rand.x, 0f, rand.y);

        NavMeshHit hit;
        bool found = NavMesh.SamplePosition(desired, out hit, sampleRadius, NavMesh.AllAreas);

        if (found == false)
        {
            if (finished != null) finished();
            return;
        }

        startPos = transform.position;
        endPos = hit.position;

        isRunning = true;
        onFinished = finished;
        elapsed = 0f;

        agent.enabled = false;
    }

    private void Update()
    {
        if (isRunning == false) return;

        elapsed += Time.deltaTime;
        float t = elapsed / leapDuration;
        t = Mathf.Clamp01(t);

        Vector3 pos = Vector3.Lerp(startPos, endPos, t);
        float height = 4f * leapHeight * t * (1f - t);
        pos.y += height;

        transform.position = pos;

        if (t >= 1f)
        {
            Finish();
        }
    }

    public void Tick(float dt)
    {
        //아무것도안써있는거 맞습니다
    }
    private void Finish()
    {
        isRunning = false;

        agent.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(transform.position);
        }
        else
        {
            agent.Warp(endPos);
        }
        agent.isStopped = false;
        agent.ResetPath();

        if (onFinished != null)
        {
            onFinished();
        }

        onFinished = null;
    }

    public void ForceStop()
    {
        isRunning = false;

        if (!agent.enabled)
            agent.enabled = true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            agent.Warp(endPos);

        agent.isStopped = false;
        agent.ResetPath();

        onFinished = null;
    }
}