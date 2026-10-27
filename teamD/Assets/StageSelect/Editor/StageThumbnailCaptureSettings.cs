using System;
using UnityEditor;
using UnityEngine;

/// <summary>ステージ写真の一括撮影設定。ビルドには含めないエディタ専用データ。</summary>
[CreateAssetMenu(fileName = "StageThumbnailCaptureSettings", menuName = "Stage Select/Thumbnail Capture Settings")]
public class StageThumbnailCaptureSettings : ScriptableObject
{
    [Header("撮影するシーン")]
    [Tooltip("シーン名の代わりにシーンファイルを登録します。Build Settingsへの登録は不要です。")]
    [SerializeField] private SceneAsset[] scenes = Array.Empty<SceneAsset>();

    [Header("保存先（Assets内のフォルダ）")]
    [SerializeField] private DefaultAsset outputFolder;

    [Header("画像サイズ")]
    [Range(16, 4096)] [SerializeField] private int width = 640;
    [Range(16, 4096)] [SerializeField] private int height = 360;

    [Header("撮影用Cameraのオブジェクト名（空欄ならMainCameraタグ）")]
    [Tooltip("専用Cameraを使う場合は、各シーンに同じ名前のCameraを配置してください。Cameraコンポーネントは無効でも撮影できます。")]
    [SerializeField] private string cameraName = "";

    public SceneAsset[] Scenes => scenes;
    public DefaultAsset OutputFolder => outputFolder;
    public int Width => width;
    public int Height => height;
    public string CameraName => cameraName;
}
