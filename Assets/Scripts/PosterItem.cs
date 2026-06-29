using UnityEngine;

/// <summary>
/// Gắn script này cùng với InteractableObject (chọn Type = Poster) lên bức tường/bức tranh.
/// </summary>
[RequireComponent(typeof(InteractableObject))]
public class PosterItem : MonoBehaviour
{
    [Tooltip("Bức ảnh 2D sẽ hiện lên toàn màn hình khi người chơi bấm xem")]
    public Sprite posterImage;

    public void Inspect()
    {
        if (PosterViewer.Instance != null && posterImage != null)
        {
            PosterViewer.Instance.ViewPoster(posterImage);
        }
        else
        {
            Debug.LogWarning("[PosterItem] Chưa gán ảnh hoặc chưa có PosterViewer trong Scene!");
        }
    }
}
