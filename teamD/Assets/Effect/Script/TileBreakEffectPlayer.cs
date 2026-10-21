using UnityEngine;

/// <summary>破壊されたセルの破片を、元のタイルから独立して再生する。</summary>
public class TileBreakEffectPlayer : MonoBehaviour
{
    [SerializeField, Range(1, 32), Tooltip("セルごとの破片の数")]
    private int _fragmentsPerCell = 10;

    [SerializeField, Min(0.05f), Tooltip("破片の寿命")]
    private float _lifetime = 0.55f;

    [SerializeField, Min(0.01f), Tooltip("破片のスケール")]
    private float _fragmentScale = 0.22f;

    [SerializeField, Min(0f), Tooltip("破片の拡散速度")]
    private float _spreadSpeed = 2f;

    [SerializeField, Min(0f), Tooltip("破片の上方向速度")]
    private float _upwardSpeed = 2.5f;

    [SerializeField, Min(0f), Tooltip("重力")]
    private float _gravity = 1.5f;

    // Destroy予約済みのグループからも、その場で表示情報を取得する。
    public void Play(TileGroup group)
    {
        if (group == null || group.Cells == null)
        {
            return;
        }

        // 破壊されたセルの破片を、元のタイルから独立して再生する。
        foreach (TileCell cell in group.Cells)
        {
            if (cell == null)
            {
                continue;
            }

            MeshFilter mesh = cell.GetComponent<MeshFilter>();
            MeshRenderer source = cell.GetComponent<MeshRenderer>();
            if (mesh == null || mesh.sharedMesh == null || source == null || source.sharedMaterial == null)
            {
                continue;
            }

            // 破片のパーティクルを生成する
            GameObject effect = new GameObject("TileBreakFragments");
            effect.transform.SetParent(transform, false);
            effect.transform.position = source.bounds.center;
            effect.transform.rotation = cell.transform.rotation;

            // パーティクルを設定
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.05f;
            main.startLifetime = Mathf.Max(0.05f, _lifetime);
            main.startSpeed = 0f;
            main.maxParticles = Mathf.Clamp(_fragmentsPerCell, 1, 32);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.gravityModifier = _gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startSize3D = true;
            Vector3 scale = cell.transform.lossyScale;
            main.startSizeX = Mathf.Abs(scale.x) * _fragmentScale;
            main.startSizeY = Mathf.Abs(scale.y) * _fragmentScale;
            main.startSizeZ = Mathf.Abs(scale.z) * _fragmentScale;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)main.maxParticles) });
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = Vector3.Scale(mesh.sharedMesh.bounds.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.6f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-_spreadSpeed, _spreadSpeed);
            velocity.y = new ParticleSystem.MinMaxCurve(_upwardSpeed * 0.5f, _upwardSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(-_spreadSpeed, _spreadSpeed);
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh.sharedMesh;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
        }
    }
}
