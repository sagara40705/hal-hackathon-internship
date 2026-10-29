using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Stage Select/StageData")]
public class StageData : ScriptableObject
{
    [SerializeField, Tooltip("ステージ名")]
    private string _stageName;

    [SerializeField, Tooltip("ステージのサムネイル画像")]
    private Sprite _stageThumbnail;

    [SerializeField, Tooltip("遷移先シーン")]
    private SceneAsset _scene;

    public string StageName => _stageName;
    public Sprite StageThumbnail => _stageThumbnail;
    public SceneAsset Scene => _scene;
}

