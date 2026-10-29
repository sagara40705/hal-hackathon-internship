
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
/// <summary>
/// 設定した位置にあるオブジェクトの場所でUIのボタンを押すとそのステージにいくスクリプト
/// 作成者：牛田大翔
/// </summary>
public class StageSelectManager : MonoBehaviour
{
    [Serializable]
    public class StageInfo
    {
        [Header("ステージ情報")]
        public string stageName;

        [Header("ステージの位置")]
        public Transform stagePosition;

        [Header("移動先のシーン名")]
        public string sceneName;
    }

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Canvasの開始ボタン")]
    [SerializeField] private Button startButton;

    [Header("ステージ設定")]
    [SerializeField] private StageInfo[] stages;

    [Header("選択可能な距離")]
    [SerializeField] private float selectDistance = 1.5f;

    private StageInfo currentStage;

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartSelectedStage);
        }

        UpdateStageSelection();
    }

    private void Update()
    {
        UpdateStageSelection();
    }

    // Playerの位置から選択中のステージを判定
    private void UpdateStageSelection()
    {
        currentStage = null;

        if (player == null)
        {
            SetButtonEnabled(false);
            return;
        }

        float nearestDistance = float.MaxValue;

        foreach (StageInfo stage in stages)
        {
            if (stage == null || stage.stagePosition == null)
                continue;

            // 水平方向の距離を計算（Y座標は無視）
            Vector2 playerPos = new Vector2(
                player.position.x,
                player.position.z
            );

            Vector2 stagePos = new Vector2(
                stage.stagePosition.position.x,
                stage.stagePosition.position.z
            );

            float distance = Vector2.Distance(
                playerPos,
                stagePos
            );

            if (distance <= selectDistance &&
                distance < nearestDistance)
            {
                nearestDistance = distance;
                currentStage = stage;
            }
        }

        // ステージの近くにいるときだけ押せる
        SetButtonEnabled(currentStage != null);
    }

    private void SetButtonEnabled(bool enabled)
    {
        if (startButton != null)
        {
            startButton.interactable = enabled;
        }
    }

    // Canvasのボタンを押したときに実行
    public void StartSelectedStage()
    {
        if (currentStage == null)
            return;

        if (string.IsNullOrEmpty(currentStage.sceneName))
        {
            Debug.LogWarning("移動先のシーン名が設定されていません");
            return;
        }

        Debug.Log("ステージ開始：" + currentStage.stageName);

        SceneManager.LoadScene(currentStage.sceneName);
    }
}
