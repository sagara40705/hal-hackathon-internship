using UnityEngine;

/// <summary>ダメージ床に乗った瞬間の閃光と火花を再生する。</summary>
public sealed class DamageHitEffect : MonoBehaviour
{
    private Material _material;

    private void Awake()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Destroy(gameObject);
            return;
        }

        _material = new Material(shader);
        CreateBurst("Flash", 1, 0.14f, 0f, 0.8f, new Color(1f, 0.92f, 0.82f, 1f));
        CreateBurst("Sparks", 12, 0.4f, 2.5f, 0.13f, new Color(1f, 0.16f, 0.06f, 1f));
        Destroy(gameObject, 0.8f);
    }

    private void CreateBurst(string name, short count, float lifetime, float speed, float size, Color color)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        var particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.05f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.06f;
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
