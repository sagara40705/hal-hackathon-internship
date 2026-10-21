using System;
using System.Collections.Generic;
using UnityEngine;

// シーンに1つだけ
[DefaultExecutionOrder(100)]
public class PeriodicBlockContacts : MonoBehaviour
{
    private class Motion
    {
        public Player Player;
        public BoxCollider Collider;
        public Vector3 PreviousCenter;
    }

    private static PeriodicBlockContacts _instance;
    private static readonly HashSet<Collider> Blocks = new HashSet<Collider>();
    private readonly List<Motion> _players = new List<Motion>();
    private RaycastHit[] _hits = new RaycastHit[16];
    private Collider[] _overlaps = new Collider[16];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBlocks() => Blocks.Clear();

    public static void Register(Collider collider) => Blocks.Add(collider);
    public static void Unregister(Collider collider) => Blocks.Remove(collider);

    public static void EnsureExists()
    {
        if (_instance == null)
            _instance = new GameObject("PeriodicBlockContacts").AddComponent<PeriodicBlockContacts>();
    }

    private void Awake()
    {
        foreach (Player player in FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            BoxCollider collider = player.GetComponentInChildren<BoxCollider>();
            if (collider != null)
                _players.Add(new Motion
                {
                    Player = player,
                    Collider = collider,
                    PreviousCenter = collider.transform.TransformPoint(collider.center)
                });
        }
    }

    private void LateUpdate()
    {
        bool checkContacts = Blocks.Count > 0;
        foreach (Motion motion in _players)
        {
            if (!IsAlive(motion))
                continue;

            Vector3 center = motion.Collider.transform.TransformPoint(motion.Collider.center);
            Vector3 offset = center - motion.PreviousCenter;
            if (checkContacts && offset.sqrMagnitude > 0.00000001f)
            {
                float distance = offset.magnitude;
                Vector3 direction = offset / distance;
                Vector3 halfExtents = HalfExtents(motion);
                Quaternion rotation = motion.Collider.transform.rotation;
                int count;

                // 固体のみ取得し、同じブロックのTriggerとの重複を避ける。
                // 飽和したときだけ拡張して再取得する。通常フレームでは配列を作らない。
                while ((count = Physics.BoxCastNonAlloc(motion.PreviousCenter, halfExtents,
                    direction, _hits, rotation, distance,
                    Physics.AllLayers, QueryTriggerInteraction.Ignore)) == _hits.Length)
                    Array.Resize(ref _hits, _hits.Length * 2);

                float contactDistance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    if (_hits[i].distance < contactDistance && Blocks.Contains(_hits[i].collider))
                    {
                        contactDistance = _hits[i].distance;
                    }
                        
                }

                if (!float.IsPositiveInfinity(contactDistance))
                {
                    // 高速移動後でもブロックの手前で停止する。
                    Vector3 correction = direction * (distance - Mathf.Max(0f, contactDistance));
                    motion.Player.transform.position -= correction;
                    center -= correction;
                    motion.Player.Damage();
                }
            }
            motion.PreviousCenter = center;
        }
    }

    // ワープは経路を通らない。到着地点だけを確認して次の経路判定を開始する。
    public static void OnTeleported(Player player)
    {
        if (_instance == null)
        {
            return;
        }
            

        foreach (Motion motion in _instance._players)
        {
            if (motion.Player != player || !IsAlive(motion)) continue;


            motion.PreviousCenter = motion.Collider.transform.TransformPoint(motion.Collider.center);
            if (Blocks.Count == 0)
            {
                break;
            }
                

            Vector3 halfExtents = HalfExtents(motion);
            Quaternion rotation = motion.Collider.transform.rotation;
            int count;
            while ((count = Physics.OverlapBoxNonAlloc(motion.PreviousCenter, halfExtents,
                _instance._overlaps, rotation, Physics.AllLayers,
                QueryTriggerInteraction.Ignore)) == _instance._overlaps.Length)
                Array.Resize(ref _instance._overlaps, _instance._overlaps.Length * 2);

            for (int i = 0; i < count; i++)
            {
                if (Blocks.Contains(_instance._overlaps[i]))
                {
                    player.Damage();
                    break;
                }
            }

            break;
        }
    }

    private static bool IsAlive(Motion motion) => motion.Player != null && motion.Collider != null
        && motion.Player.gameObject.activeInHierarchy && motion.Collider.enabled
        && motion.Player.IsTileReserved(motion.Player.CurrnetPosition);

    private static Vector3 HalfExtents(Motion motion)
    {
        Vector3 size = Vector3.Scale(motion.Collider.size, motion.Collider.transform.lossyScale) * 0.5f;
        return new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }
}
