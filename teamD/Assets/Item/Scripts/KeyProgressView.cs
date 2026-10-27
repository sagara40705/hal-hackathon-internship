using UnityEngine;
using UnityEngine.UI;

/// <summary>鍵の所持数通知から、画面上の鍵アイコンと進捗数字だけを更新する。</summary>
[DisallowMultipleComponent]
public sealed class KeyProgressView : MonoBehaviour
{
    [SerializeField] private PlayerInventory _inventory;
    [SerializeField] private GoalCell _goal;
    [SerializeField] private Image _keyIcon;
    [SerializeField] private Image _current;
    [SerializeField] private Image _separator;
    [SerializeField] private Image _required;
    [SerializeField] private Sprite[] _incompleteGlyphs;
    [SerializeField] private Sprite[] _completeGlyphs;

    private void OnEnable()
    {
        if (_inventory != null && _goal != null)
            Refresh(_inventory.KeyCount);
    }

    // PlayerInventory.OnCountChanged の動的引数に接続する。
    public void OnCountChanged(ItemType type, int count)
    {
        if (type == ItemType.Key && isActiveAndEnabled && _goal != null)
            Refresh(count);
    }

    private void Refresh(int count)
    {
        int required = _goal.RequiredKeyCount;
        bool complete = _goal.HasRequiredKeyCount(count);
        Sprite[] glyphs = complete ? _completeGlyphs : _incompleteGlyphs;
        // このHUDの生成素材は0〜3。実際のゴール判定には影響させない。
        if (required > 3 || glyphs == null || glyphs.Length != 5 ||
            _keyIcon == null || _current == null || _separator == null || _required == null)
        {
            Debug.LogError("KeyProgressView: UI画像と数字素材0〜3・/を設定し、必要数を1〜3にしてください。", this);
            return;
        }
        _current.sprite = glyphs[Mathf.Clamp(count, 0, required)];
        _required.sprite = glyphs[required];
        _separator.sprite = glyphs[4];
        _keyIcon.color = complete ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.8f);

    }
}
