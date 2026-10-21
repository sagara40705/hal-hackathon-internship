using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScriptableObjectの一覧から、ランダムにサムネイルを生成する
/// 作成者 : 酒井康佑
/// </summary>
public class ThumbnailSpawner : MonoBehaviour
{
    [Header("生成に使うデータ")]
    [SerializeField, Tooltip("サムネイル画像と配置Prefabの一覧")]
    private ModelThumbnailData thumbnailData;

    [SerializeField, Tooltip("生成するサムネイルUIのPrefab")]
    private GameObject thumbnailTemplate;

    [SerializeField, Tooltip("サムネイルを並べる親オブジェクト")]
    private Transform thumbnailParent;

    [SerializeField, Tooltip("配置処理を行うコントローラー")]
    private ModelPlacementController placementController;

    [Header("生成設定")]
    [SerializeField, Min(1), Tooltip("一度に表示するサムネイル数")]
    private int thumbnailCount = 5;

    [SerializeField, Tooltip("ゲーム開始時にもサムネイルを生成する")]
    private bool spawnOnStart = true;

    // このスクリプトが生成したサムネイルを記録する
    private readonly List<GameObject> spawnedThumbnails = new List<GameObject>();

    private void Start()
    {
        if (spawnOnStart)
            Refill();
    }

    /// <summary>
    /// SOの一覧からランダムに選んで、サムネイルを補充する
    /// </summary>>
    public void Refill()
    {
        if(thumbnailData == null ||
           thumbnailTemplate == null ||
           thumbnailParent == null ||
           placementController == null )
        {
            Debug.LogWarning("ThumbnailSpawnerの参照が設定されていません", this);
            return;
        }

        // 既存のサムネイルを片付ける
        ClearSpawnedThumbnails();

        // 画像と配置Prefabの両方が設定されたエントリーだけを候補にする
        List<ModelThumbnailData.ModelThumbnailEntry> candidates =
            new List<ModelThumbnailData.ModelThumbnailEntry>();

        foreach(ModelThumbnailData.ModelThumbnailEntry entry in thumbnailData.Entries)
        {
            if (entry != null && entry.Thumbnail != null && entry.Prefab != null)
                candidates.Add(entry);
        }

        // 候補がなければ何もしない
        if(candidates.Count == 0)
        {
            Debug.LogWarning("SOに有効なサムネイルとPrefabの組がありません", thumbnailData);
            return;
        }

        // 候補の順番をシャッフルする
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
        }

        // 候補数を超えない範囲で生成する
        int count = Mathf.Min(thumbnailCount, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            ModelThumbnailData.ModelThumbnailEntry entry = candidates[i];

            // UI用Prefabを親の下に生成する
            GameObject thumbnail = Instantiate(
                thumbnailTemplate,
                thumbnailParent,
                false
            );

            thumbnail.name = string.IsNullOrEmpty(entry.Id)
                ? $"Thumbnail_{i + 1}"
                : $"Thumbnail_{entry.Id}";

            Image image = thumbnail.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning(
                    "サムネイルUIのPrefabにImageコンポーネントがありません。",
                    thumbnail
                );
                Destroy(thumbnail);
                continue;
            }

            // SOに登録した画像を表示する
            image.sprite = entry.Thumbnail;
            image.preserveAspect = true;
            image.raycastTarget = true;

            // 生成したサムネイルに、配置Prefabと配置処理を設定する
            ModelThumbnailDrag drag = thumbnail.GetComponent<ModelThumbnailDrag>();
            if (drag == null)
                drag = thumbnail.AddComponent<ModelThumbnailDrag>();

            drag.Initialize(entry.Prefab, placementController);

            spawnedThumbnails.Add(thumbnail);
        }
    }

    /// <summary>
    /// 前回生成したサムネイルを削除する
    /// </summary>
    private void ClearSpawnedThumbnails()
    {
        foreach (GameObject thumbnail in spawnedThumbnails)
        {
            if (thumbnail != null)
                Destroy(thumbnail);
        }

        spawnedThumbnails.Clear();
    }

}
