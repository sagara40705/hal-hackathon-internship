using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>ステージ開始時のポップアップIDと、表示する1枚の画像を管理する。</summary>
[CreateAssetMenu(fileName = "PopupCatalog", menuName = "Game/Popup Catalog")]
public sealed class PopupCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField, Tooltip("シーンから指定する一意のID")]
        private string _id;
        [SerializeField, Tooltip("表示する画像（Sprite / Single）")]
        private Sprite _image;

        public string Id => _id;
        public Sprite Image => _image;
    }

    [SerializeField] private List<Entry> _entries = new();
    public IReadOnlyList<Entry> Entries => _entries;

    public bool TryGetImage(string id, out Sprite image)
    {
        image = null;
        if (string.IsNullOrWhiteSpace(id)) return false;

        bool found = false;
        foreach (Entry entry in _entries)
        {
            if (entry == null || !string.Equals(entry.Id, id, StringComparison.Ordinal)) continue;
            // 重複IDは誤った画像を出さず、設定エラーとして扱う。
            if (found) { image = null; return false; }
            found = true;
            image = entry.Image;
        }
        return image != null;
    }

    private void OnValidate()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Entry entry in _entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                Debug.LogWarning("PopupCatalog: 空のIDがあります。", this);
            else if (!ids.Add(entry.Id))
                Debug.LogWarning($"PopupCatalog: ID '{entry.Id}' が重複しています。", this);
            else if (entry.Image == null)
                Debug.LogWarning($"PopupCatalog: ID '{entry.Id}' の画像が未設定です。", this);
        }
    }
}
