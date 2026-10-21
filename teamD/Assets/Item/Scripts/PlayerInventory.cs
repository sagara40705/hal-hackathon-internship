using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// シーン内のプレイヤーが所持するアイテム
[DisallowMultipleComponent]
public sealed class PlayerInventory : MonoBehaviour
{
    [SerializeField, Tooltip("所持数が変わったときの通知。引数は種類と変更後の個数")]
    private UnityEvent<ItemType, int> _onCountChanged = new();

    private readonly Dictionary<ItemType, int> _counts = new();

    public int KeyCount => GetCount(ItemType.Key);

    // アイテムの個数
    public int GetCount(ItemType type)
    {
        return _counts.TryGetValue(type, out int count) ? count : 0;
    }

    // アイテムを追加できるかどうか
    public bool CanAdd(ItemType type, int amount)
    {
        return System.Enum.IsDefined(typeof(ItemType), type) && amount > 0 && GetCount(type) <= int.MaxValue - amount;
    }

    // アイテムを追加する
    public bool TryAdd(ItemType type, int amount)
    {
        if (!CanAdd(type, amount))
        {
            return false;
        }
           
        int count = GetCount(type) + amount;
        _counts[type] = count;
        _onCountChanged?.Invoke(type, count);
        return true;
    }
}
