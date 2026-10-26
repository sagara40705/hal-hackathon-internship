using UnityEngine;

public class StageSlelectObjectLength : MonoBehaviour
{
    [Header("オブジェクトの大きさ")]
    [SerializeField] private float lengthX = 10f;
    [SerializeField] private float lengthY = 1f;
    [SerializeField] private float lengthZ = 1f;

    private void Start()
    {
        SetLength();
    }

    public void SetLength()
    {
        transform.localScale = new Vector3(
            lengthX,
            lengthY,
            lengthZ
        );
    }
}
