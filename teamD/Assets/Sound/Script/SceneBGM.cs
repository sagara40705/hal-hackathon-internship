using UnityEngine;

/// <summary>
/// シーン開始時に、指定したBGMを再生する。
/// </summary>
public class SceneBGM : MonoBehaviour
{
    [SerializeField] private SoundId _bgmId = SoundId.GameBGM;

    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGMById(_bgmId);
        }
    }
}
