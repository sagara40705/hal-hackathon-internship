using UnityEngine;

public class DamageTile : MonoBehaviour
{
    [SerializeField] private GameObject _hitEffectPrefab;

    public void OnEnterPlayer(Player player)
    {
        if (_hitEffectPrefab != null)
            Instantiate(_hitEffectPrefab, player.transform.position + Vector3.up, Quaternion.Euler(90f, 0f, 0f));

        player.Damage();
    }
}
