using UnityEngine;

/// <summary>Small, reusable feedback burst. Contains no collision or gameplay state.</summary>
[DisallowMultipleComponent]
public sealed class GimmickPulse : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _systems;

    public void Play(Color color)
    {
        if (!isActiveAndEnabled) return;
        foreach (var system in _systems)
        {
            if (system == null) continue;
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.startColor = color;
            system.Play(false);
        }
    }
}
