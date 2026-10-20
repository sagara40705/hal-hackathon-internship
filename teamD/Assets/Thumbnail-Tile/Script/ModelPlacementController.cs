using UnityEngine;

/// <summary>
/// 配置位置の判定、配置プレビューの表示、Prefabの生成を管理するクラス。
/// </summary>
public class ModelPlacementController : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField, Tooltip("配置位置を計算するときに使うカメラ")]
    private Camera placementCamera;

    [SerializeField, Tooltip("配置先として判定する床のLayer")]
    private LayerMask placementSurfaceLayers;

    [SerializeField, Min(0.1f), Tooltip("カメラからレイを飛ばす最大距離")]
    private float maxDistance = 1000f;

    [Header("Preview")]
    [SerializeField, Tooltip("プレビュー専用マテリアル")]
    private Material previewMaterial;

    [SerializeField, Tooltip("生成したタイルを登録するグリッド")]
    private TileGrid tileGrid;

    // ドラッグ中に表示している、Prefabの仮のコピー。
    private GameObject previewInstance;

    /// <summary>
    /// ドラッグ開始時に、Prefabのプレビューを作成する。
    /// </summary>
    /// <param name="prefab">配置予定のPrefab</param>
    public void BeginPreview(GameObject prefab)
    {
        // 前回のプレビューが残っていた場合は先に削除する。
        ClearPreview();

        if (prefab == null)
        {
            Debug.LogWarning("プレビュー対象のPrefabが設定されていません。");
            return;
        }

        // 透明マテリアルがないと半透明表示にできないため、作成を中止する。
        if (previewMaterial == null)
        {
            Debug.LogWarning("プレビュー用マテリアルが設定されていません。");
            return;
        }

        // Prefabを仮生成し、プレビュー用の見た目と動作に切り替える。
        previewInstance = Instantiate(prefab);
        previewInstance.name = prefab.name + "_Preview";
        SetPreviewAppearance(previewInstance);
        previewInstance.SetActive(false);
    }

    /// <summary>
    /// ドラッグ中の画面位置を床に投影し、プレビューを移動させる。
    /// </summary>
    /// <param name="screenPosition">マウスまたはタッチの画面座標</param>
    public void UpdatePreview(Vector2 screenPosition)
    {
        if (previewInstance == null)
            return;

        // 配置可能な床に当たった場合、その位置にプレビューを表示する。
        if (TryGetPlacementPoint(screenPosition, out Vector3 point))
        {
            previewInstance.transform.position = point;
            previewInstance.SetActive(true);
        }
        else
        {
            // 床に当たらない位置ではプレビューを隠す。
            previewInstance.SetActive(false);
        }
    }

    /// <summary>
    /// ドラッグ終了位置を判定し、床に当たっていれば本物のPrefabを配置する。
    /// </summary>
    /// <param name="prefab">配置するPrefab</param>
    /// <param name="screenPosition">ドラッグ終了時の画面座標</param>
    /// <returns>配置できた場合はtrue</returns>
    public bool EndPreviewAndPlace(GameObject prefab, Vector2 screenPosition)
    {
        bool placed = false;

        // TileGridが設定されていない場合は配置できないので警告を出す。
        if (tileGrid == null)
        {
            Debug.LogWarning("TileGridが設定されていません。");
        }
        // Prefabが設定されていない場合も配置できないので警告を出す。
        else if (prefab != null && TryGetPlacementPoint(screenPosition, out Vector3 point))
        {
            placed = tileGrid.TryCreateAndPlaceTile(prefab, point);
        }

        // 配置の成否にかかわらず、仮のプレビューを削除する。
        ClearPreview();
        return placed;
    }

    /// <summary>
    /// 画面座標からカメラのレイを飛ばし、配置可能な床との交点を取得する。
    /// </summary>
    /// <param name="screenPosition">調べる画面座標</param>
    /// <param name="point">床に当たったワールド座標</param>
    /// <returns>指定LayerのColliderに当たった場合はtrue</returns>
    private bool TryGetPlacementPoint(Vector2 screenPosition, out Vector3 point)
    {
        point = default;

        if (placementCamera == null || tileGrid == null)
        {
            Debug.LogWarning("配置用カメラまたはTileGridが設定されていません。");
            return false;
        }

        Ray ray = placementCamera.ScreenPointToRay(screenPosition);

        // TileGridと同じ平面を、カーソル位置のRayが通るか調べる
        Plane gridPlane = new Plane(
            tileGrid.transform.up,
            tileGrid.transform.position
        );

        // Rayとグリッド平面の交点を計算する。交点がない場合や、最大距離を超える場合は配置できない。
        if (!gridPlane.Raycast(ray, out float distance) ||
            distance < 0f ||
            distance > maxDistance)
        {
            return false;
        }

        // Rayとグリッド平面の交点を、グリッド座標に変換する
        Vector3 worldPosition = ray.GetPoint(distance);
        Vector2Int gridPosition = tileGrid.WorldToGrid(worldPosition);

        // グリッドの範囲外なら配置できない
        if (!tileGrid.IsInGrid(gridPosition))
            return false;

        // グリッド座標をマスの中心のワールド座標へ戻す
        point = tileGrid.GridToWorld(gridPosition);
        return true;
    }

    /// <summary>
    /// 仮のPrefabを半透明にし、物理判定やゲーム動作を止める。
    /// </summary>
    /// <param name="preview">プレビュー用に生成したGameObject</param>
    private void SetPreviewAppearance(GameObject preview)
    {
        // プレビューが床や他のオブジェクトと衝突しないようColliderを無効にする。
        Collider[] colliders = preview.GetComponentsInChildren<Collider>(true);
        foreach (Collider item in colliders)
            item.enabled = false;

        // 2D Colliderを持つPrefabにも対応して無効にする。
        Collider2D[] colliders2D = preview.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D item in colliders2D)
            item.enabled = false;

        // Animatorやスクリプトなど、Prefabに含まれる動作コンポーネントを止める。
        Behaviour[] behaviours = preview.GetComponentsInChildren<Behaviour>(true);
        foreach (Behaviour item in behaviours)
            item.enabled = false;

        // Rigidbodyの落下や物理シミュレーションを止める。
        Rigidbody[] rigidbodies = preview.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody item in rigidbodies)
        {
            item.isKinematic = true;
            item.useGravity = false;
        }

        // Rigidbody2Dの物理シミュレーションも止める。
        Rigidbody2D[] rigidbodies2D = preview.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D item in rigidbodies2D)
        {
            item.bodyType = RigidbodyType2D.Kinematic;
            item.simulated = false;
        }

        // モデルの各Rendererにプレビュー用マテリアルを設定する。
        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer item in renderers)
        {
            Material[] materials = new Material[item.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = previewMaterial;

            item.sharedMaterials = materials;
        }
    }

    /// <summary>
    /// 作成済みのプレビューを削除する。
    /// </summary>
    private void ClearPreview()
    {
        if (previewInstance == null)
            return;

        Destroy(previewInstance);
        previewInstance = null;
    }

    /// <summary>
    /// 管理オブジェクトが無効化されたとき、残っているプレビューを片付ける。
    /// </summary>
    private void OnDisable()
    {
        ClearPreview();
    }
}
