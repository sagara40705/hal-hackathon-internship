using UnityEngine;

public class ThumbnailRestoreController : MonoBehaviour
{
    [SerializeField, Tooltip("補充させるサムネイル一覧")]
    private GameObject[] thumbnails;

    // ボタンを押したとき、サムネイルをすべて再表示する
    public void RestoreThumbnails()
    {
        if (thumbnails == null)
            return;

        foreach(GameObject thumbnails in thumbnails)
        {
            if(thumbnails != null)
            thumbnails.SetActive(true);
        }
    }

}
