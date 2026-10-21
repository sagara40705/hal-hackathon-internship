using UnityEngine;

public class DamageTile : MonoBehaviour
{
    public void OnEnterPlayer(Player player)
    {
        player.Damage();
    }
}
