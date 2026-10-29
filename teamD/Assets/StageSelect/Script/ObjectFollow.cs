using UnityEngine;

public class ObjectFollow : MonoBehaviour
{
    [SerializeField, Tooltip("追従対象のTransform")]
    private Transform _target;

    private Vector3 _offset;

    private void Start()
    {
        if (_target != null)
            _offset = transform.position - _target.position;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        Vector3 targetPosition = _target.position + _offset;
        targetPosition.y = transform.position.y;
        transform.position = targetPosition;
    }
}
