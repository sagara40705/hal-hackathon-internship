using UnityEngine;
using System.Collections;

public class JumpTile : MonoBehaviour
{
    [SerializeField, Min(0.01f), Tooltip("ジャンプにかかる秒数")]
    private float _duration = 0.45f;

    [SerializeField, Min(0f), Tooltip("ジャンプの高さ")]
    private float _height = 1f;

    [SerializeField] private Transform _jumpBoard;
    [SerializeField] private Transform _boardSupport;

    [SerializeField, Min(0f), Tooltip("板を押し上げる高さ")]
    private float _pushHeight = 0.25f;

    private Vector3 _boardPosition;
    private Vector3 _supportPosition;
    private Vector3 _supportScale;
    private bool _isAnimating;

    public void OnEnterPlayer(Player player)
    {
        if (player == null || !player.isActiveAndEnabled)
        {
            return;
        }
          
        player.StartCoroutine(player.JumpForward(_duration, _height));
        if (!_isAnimating && _jumpBoard != null && _boardSupport != null)
        {
            StartCoroutine(PushBoard());
        }

    }

    // 板を押し上げるアニメーションのコルーチン
    private IEnumerator PushBoard()
    {
        _isAnimating = true;
        _boardPosition = _jumpBoard.localPosition;
        _supportPosition = _boardSupport.localPosition;
        _supportScale = _boardSupport.localScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 最初の20%で素早く押し上げ、その後ゆっくり戻す。
            float amount = t < 0.2f
                ? Mathf.SmoothStep(0f, 1f, t / 0.2f)
                : Mathf.SmoothStep(1f, 0f, (t - 0.2f) / 0.8f);
            float lift = _pushHeight * amount;

            _jumpBoard.localPosition = _boardPosition + Vector3.up * lift;
            _boardSupport.localScale = _supportScale + Vector3.up * lift;
            // 支えの底面を固定し、伸びた上端が板を押すようにする。
            _boardSupport.localPosition = _supportPosition + Vector3.up * (lift * 0.5f);
            yield return null;
        }

        RestoreBoard();
    }

    // オブジェクトが無効化されたときにアニメーションを停止し、板と支えを元の位置に戻す。
    private void OnDisable()
    {
        if (!_isAnimating)
            return;

        StopAllCoroutines();
        RestoreBoard();
    }

    // アニメーションを終了し、板と支えを元の位置に戻す。
    private void RestoreBoard()
    {
        if (_jumpBoard != null)
        {
            _jumpBoard.localPosition = _boardPosition;
        }
        if (_boardSupport != null)
        {
            _boardSupport.localPosition = _supportPosition;
            _boardSupport.localScale = _supportScale;
        }
        _isAnimating = false;
    }
}
