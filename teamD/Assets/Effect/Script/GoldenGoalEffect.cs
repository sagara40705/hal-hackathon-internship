using UnityEngine;

namespace TeamD.Effects
{
    /// <summary>One-shot goal celebration. Origin is the centre of the tile's top surface.</summary>
    [DisallowMultipleComponent]
    public sealed class GoldenGoalEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("有効になった時に一度再生する")]
        private bool playOnEnable = true;
        [SerializeField, Tooltip("再生終了後に非アクティブにする")]
        private bool disableWhenFinished;
        private ParticleSystem[] systems;
        private bool playing;

        private void Awake()
        {
#if UNITY_EDITOR
            var preview = transform.Find("EditorOnly_GoalPreview");
            if (preview != null) preview.gameObject.SetActive(false);
#endif
            Cache();
        }

        // パーティカルシステムを取得する
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
                gameObject.SetActive(true);
            }

            // 再生中のパーティカルシステムを停止してから再生する
            foreach (var system in systems)
            {
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(false);
            }
            playing = true;
        }

        // 再生位置を指定して再生する
        public void PlayAt(Vector3 surfaceCenter)
        {
            transform.position = surfaceCenter;
            Play();
        }
        
        // 再生を停止する
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
