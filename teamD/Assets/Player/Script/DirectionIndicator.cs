
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class DirectionIndicator : MonoBehaviour
{
    [Header("上下に浮く設定")]
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatSpeed = 2.0f;

    [Header("消える設定")]
    [SerializeField] private float waitTime = 3.0f;
    [SerializeField] private float fadeTime = 1.0f;

    private Vector3 startPosition;
    private Renderer objectRenderer;
    private Material objectMaterial;
    private Color startColor;

    void Start()
    {
        // 初期位置を保存
        startPosition = transform.position;

        // オブジェクトのRendererを取得
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            // マテリアルを個別に取得
            objectMaterial = objectRenderer.material;
            startColor = objectMaterial.color;
        }

        // 消える処理を開始
        StartCoroutine(FadeOut());
    }

    void Update()
    {
        // 上下にふわふわ動かす
        float y = Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.position = new Vector3(
            startPosition.x,
            startPosition.y + y,
            startPosition.z
        );
    }

    IEnumerator FadeOut()
    {
        // 指定した秒数だけ待つ
        yield return new WaitForSeconds(waitTime);

        // 徐々に透明にする
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(
                1f,
                0f,
                timer / fadeTime
            );

            if (objectMaterial != null)
            {
                Color color = startColor;
                color.a = alpha;
                objectMaterial.color = color;
            }

            yield return null;
        }

        // 完全に透明になったら削除
        Destroy(gameObject);
    }
}
