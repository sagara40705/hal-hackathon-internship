using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
/// <summary>
/// UIの矢印でプレイヤーが動くようになるスクリプト+マウスホイール移動
/// 作成者：牛田大翔
/// </summary>
public class StageSelectPlayerUIMove : MonoBehaviour
{
    [Header("ステージの移動先")]
    public Transform[] stagePositions;

    [Header("移動設定")]
    public float moveSpeed = 3f;

    [Header("マウスホイール設定")]
    public float wheelThreshold = 0.1f;

    [Header("矢印UI")]
    public GameObject leftArrowUI;
    public GameObject rightArrowUI;

    [Header("ステージ画像")]
    public Image stageImage;
    public Sprite[] stageSprites;

    private int currentStageIndex = 0;
    private Coroutine moveCoroutine;
    private bool isMoving = false;

    private void Start()
    {
        if (stagePositions == null || stagePositions.Length == 0)
        {
            Debug.LogError("ステージ位置が設定されていません。");
            return;
        }

        if (stagePositions[0] == null)
        {
            Debug.LogError("最初のステージ位置が設定されていません。");
            return;
        }

        transform.position = stagePositions[currentStageIndex].position;

        UpdateArrowUI();
        UpdateStageImage();
    }

    private void Update()
    {
        if (isMoving || Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > wheelThreshold)
        {
            MoveRight();
        }
        else if (scroll < -wheelThreshold)
        {
            MoveLeft();
        }
    }

    public void MoveLeft()
    {
        if (isMoving || currentStageIndex <= 0)
            return;

        currentStageIndex--;
        StartMove();
    }

    public void MoveRight()
    {
        if (isMoving || currentStageIndex >= stagePositions.Length - 1)
            return;

        currentStageIndex++;
        StartMove();
    }

    private void StartMove()
    {
        if (stagePositions[currentStageIndex] == null)
        {
            Debug.LogError("移動先のステージ位置が設定されていません。");
            return;
        }

        Vector3 targetPosition =
            stagePositions[currentStageIndex].position;

        isMoving = true;

        UpdateArrowUI();
        UpdateStageImage();

        moveCoroutine = StartCoroutine(MovePlayer(targetPosition));
    }

    private IEnumerator MovePlayer(Vector3 targetPosition)
    {
        while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;

        isMoving = false;
        moveCoroutine = null;

        UpdateArrowUI();
        UpdateStageImage();
    }

    private void UpdateArrowUI()
    {
        if (leftArrowUI != null)
            leftArrowUI.SetActive(currentStageIndex > 0);

        if (rightArrowUI != null)
        {
            rightArrowUI.SetActive(
                stagePositions != null &&
                currentStageIndex < stagePositions.Length - 1
            );
        }
    }

    private void UpdateStageImage()
    {
        if (stageImage == null)
            return;

        if (stageSprites == null ||
            currentStageIndex >= stageSprites.Length)
        {
            Debug.LogWarning("ステージ画像の配列が不足しています。");
            return;
        }

        stageImage.sprite = stageSprites[currentStageIndex];
    }
}
