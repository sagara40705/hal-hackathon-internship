using UnityEngine;

namespace TeamD.Effects
{
    /// <summary>One-shot goal celebration. Origin is the centre of the tile's top surface.</summary>
    [DisallowMultipleComponent]
    public sealed class GoldenGoalEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("有効になった時に一度再生する。イベントから呼ぶ場合はOFF。")]
        private bool playOnEnable = true;
        [SerializeField, Tooltip("再生終了後に非アクティブにする（再利用可能）。")]
        private bool disableWhenFinished;
        private ParticleSystem[] systems;
        private bool playing;

        private void Awake()
        {
#if UNITY_EDITOR
            // Static geometry is for Unity's native prefab preview, never gameplay.
            // EditorOnly objects are automatically stripped from player builds.
            var preview = transform.Find("EditorOnly_GoalPreview");
            if (preview != null) preview.gameObject.SetActive(false);
#endif
            Cache();
        }

        private void Cache()
        {
            if (systems == null) systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        [ContextMenu("Play Goal Effect")]
        public void Play()
        {
            Cache();
            if (!gameObject.activeSelf)
            {
                // OnEnable may also call Play; restarting below is deliberate and idempotent.
                gameObject.SetActive(true);
            }
            foreach (var system in systems)
            {
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(false);
            }
            playing = true;
        }

        /// <summary>Move to a tile's surface centre, then replay.</summary>
        public void PlayAt(Vector3 surfaceCenter)
        {
            transform.position = surfaceCenter;
            Play();
        }

        public void Stop()
        {
            Cache();
            foreach (var system in systems)
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            playing = false;
        }

        private void OnDisable() => Stop();

        private void Update()
        {
            if (!playing) return;
            foreach (var system in systems)
                if (system.IsAlive(false)) return;
            playing = false;
            if (disableWhenFinished) gameObject.SetActive(false);
        }
    }
}
