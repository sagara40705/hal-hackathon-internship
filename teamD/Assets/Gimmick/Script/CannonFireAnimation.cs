using UnityEngine;

/// <summary>大砲の発射反動と、砲口の発射位置を管理する。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class CannonFireAnimation : MonoBehaviour
{
    private static readonly int FireState = Animator.StringToHash("Base Layer.Fire");

    [SerializeField, Tooltip("砲口の発射位置")]
    private Transform muzzle;

    private Animator animator;

    public Transform Muzzle => muzzle != null ? muzzle : transform;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayFire()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.Play(FireState, 0, 0f);
    }
}
