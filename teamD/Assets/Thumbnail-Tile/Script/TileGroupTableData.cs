using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// タイルグループPrefabの一覧を保存するデータアセット
/// 作成者 : 酒井康佑
/// </summary>
[CreateAssetMenu(
    fileName = "SO_ModelThumbnailTable",
    menuName = "Data/Model Thumbnail Table"
)]
public class TileGroupTableData : ScriptableObject
{
    [SerializeField]
    private List<TileGroup> _tileGroupTable = new List<TileGroup>();

    public IReadOnlyList<TileGroup> TileGroupTable => _tileGroupTable;
}
