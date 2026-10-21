using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// サムネイルと配置Prefabの一覧を保存するデータアセット
/// 作成者 : 酒井康佑
/// </summary>
[CreateAssetMenu(
    fileName = "SO_ModelThumbnailTable",
    menuName = "Data/Model Thumbnail Table"
)]
public class ModelThumbnailData : ScriptableObject
{
    [SerializeField]
    private List<ModelThumbnailEntry> entries = new List<ModelThumbnailEntry>();

    public IReadOnlyList<ModelThumbnailEntry> Entries => entries;
    /// <summary>
    /// サムネイル画像と対応するPrefabの一組
    /// </summary>
    [Serializable]
    public class ModelThumbnailEntry
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private GameObject prefab;

        public string Id => id;
        public Sprite Thumbnail => thumbnail;
        public GameObject Prefab => prefab;
    }
}
