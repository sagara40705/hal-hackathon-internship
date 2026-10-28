using System;
using System.Collections.Generic;
using UnityEngine;

namespace NormalStage.EditorTools
{
    // Editor専用の設計図。生成するPrefabにはこのコンポーネントを付けない。
    public sealed class TileGroupLayout : ScriptableObject
    {
        public TileGroupLayoutData data = new TileGroupLayoutData();
    }

    [Serializable]
    public sealed class TileGroupLayoutData
    {
        public string prefabName = "Group_New";
        public string outputFolder = "Assets/NormalStage/Prefab/TileGroup";
        public bool isStatic;
        public float tileSize = 1f;
        public Vector2Int origin;
        public List<TileGroupCellStamp> cells = new List<TileGroupCellStamp>();

        public TileGroupLayoutData Copy()
        {
            var copy = new TileGroupLayoutData
            {
                prefabName = prefabName, outputFolder = outputFolder,
                isStatic = isStatic, tileSize = tileSize, origin = origin
            };
            foreach (var cell in cells)
                copy.cells.Add(new TileGroupCellStamp(cell.position, cell.prefab));
            return copy;
        }
    }

    [Serializable]
    public sealed class TileGroupCellStamp
    {
        public Vector2Int position;
        public GameObject prefab;

        public TileGroupCellStamp(Vector2Int position, GameObject prefab)
        {
            this.position = position;
            this.prefab = prefab;
        }
    }
}
