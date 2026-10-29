using UnityEngine;

public class ClearUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // クリア画面の表示開始時に再生する。
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySEById(SoundId.GameClear);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
