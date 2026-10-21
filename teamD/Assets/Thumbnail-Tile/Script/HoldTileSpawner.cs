using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScriptableObjectの一覧から、ランダムに手持ちタイルを生成する
/// 作成者 : 酒井康佑
/// </summary>
public class HoldTileSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("タイルグリッドコントローラー")]
    private TileGridController _tileGridController;

    [Header("生成に使うデータ")]
    [SerializeField, Tooltip("生成するタイルグループのテーブル")]
    private TileGroupTableData _tileGroupTable;

    [SerializeField, Tooltip("手持ちタイルのUI要素")]
    private List<HoldTileElement> _holdTileElements;

    [Header("生成設定")]
    [SerializeField, Tooltip("ゲーム開始時にも手持ちタイルを生成する")]
    private bool _spawnOnStart = true;

    private void Start()
    {
        if (_spawnOnStart)
            Refill();
    }

    /// <summary>
    /// SOの一覧からランダムに選んで、手持ちタイルを補充する
    /// </summary>>
    public void Refill()
    {
        if(_tileGroupTable == null)
        {
            Debug.LogWarning("生成するタイルの参照が設定されていません", this);
            return;
        }

        // 生成するタイルの候補を取得
        List<TileGroup> candidates = new List<TileGroup>();
        foreach (TileGroup tileGroup in _tileGroupTable.TileGroupTable)
        {
            if (tileGroup != null)
                candidates.Add(tileGroup);
        }

        // 候補がなければ何もしない
        if(candidates.Count == 0)
        {
            Debug.LogWarning("SOに有効なPrefabがありません");
            return;
        }

        // 候補の順番をシャッフルする
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
        }

        // 候補数を超えない範囲で生成する
        int count = Mathf.Min(_holdTileElements.Count, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            TileGroup tileGroup = candidates[i];
            _holdTileElements[i].Initialize(_tileGridController, tileGroup);
        }
    }
}
