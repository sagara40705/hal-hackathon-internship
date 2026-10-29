using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム内で使用する音の種類。
/// 新しい音が必要になったら項目を追加する。
/// 作成者 : 酒井康佑
/// </summary>
public enum SoundId
{
    TitleBGM,
    StageSelectBGM,
    GameBGM,
    ButtonClick,
    TilePlacement,
    TileRefill,
    PlayerAttack,
    PlayerDamage,
    EnemyLockOn,
    EnemyShot,
    EnemyExplosion,
    EnemyDamage,
    GetKey,
    GameClear,
    GameOver,
    Fade,
    Goal,
    TileBreak, // タイル破壊音。既存IDをずらさないよう末尾に追加

}

/// <summary>
/// 音声ファイルと、その音専用の音量をまとめた一覧。
/// </summary>
[CreateAssetMenu(
    fileName = "SO_SoundTable",
    menuName = "Sound/Sound Table")]
public class SoundTableData : ScriptableObject
{
    [Serializable]
    public class SoundEntry
    {
        public SoundId id;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [SerializeField]
    private List<SoundEntry> _entries =
        new List<SoundEntry>();

    /// <summary>
    /// 指定したIDの音を一覧から探す。
    /// </summary>
    public bool TryGetSound(SoundId id, out SoundEntry entry)
    {
        foreach (SoundEntry sound in _entries)
        {
            if (sound != null && sound.id == id)
            {
                entry = sound;
                return true;
            }
        }

        entry = null;
        return false;
    }

    private void OnValidate()
    {
        // 同じIDを登録すると区別できないため、警告を出す
        HashSet<SoundId> registeredIds = new HashSet<SoundId>();

        foreach (SoundEntry sound in _entries)
        {
            if (sound != null && !registeredIds.Add(sound.id))
            {
                Debug.LogWarning(
                    $"サウンドのIDが重複しています：{sound.id}", this);
            }
        }
    }
}
