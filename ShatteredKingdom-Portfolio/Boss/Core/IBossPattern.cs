using System;

public interface IBossPattern
{
    bool IsRunning { get; }

    void SetTarget(UnityEngine.Transform t);
    bool CanUse();
    void StartPattern(Action finished);
    void ForceStop();

    void Tick(float dt);
}