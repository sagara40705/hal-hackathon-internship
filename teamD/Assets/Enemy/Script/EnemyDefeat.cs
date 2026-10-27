using UnityEngine;
using UnityEngine.Events;

public class EnemyDefeat : MonoBehaviour
{
    [SerializeField, Tooltip("敵の消滅エフェクト")]
    private EnemyBurstEffect _enemyBurstEffect;

    [SerializeField, Tooltip("消滅待ち時間")]
    private float _waitDuration = 1.0f;

    [SerializeField, Tooltip("終了時イベント")]
    private UnityEvent _onEndEvent;

    private bool _isDefeat;

    public void Play()
    {
        if (_isDefeat)
            return;

        _isDefeat = true;
        _enemyBurstEffect.Play();
    }

    private void Update()
    {
        if(!_isDefeat)
            return;

        _waitDuration -= Time.deltaTime;
        if (_waitDuration > 0.0f) 
            return;

        _onEndEvent?.Invoke();
        _isDefeat = true;
    }
}
