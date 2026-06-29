using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn script này vào một UI Canvas (Tạo một Canvas mới tên là "PosterViewerCanvas").
/// Bên trong Canvas, tạo một nút UI Image (tên là PosterImage) và kéo vào đây.
/// </summary>
public class PosterViewer : MonoBehaviour
{
    public static PosterViewer Instance { get; private set; }

    [Tooltip("UI Image dùng để hiển thị bức ảnh")]
    public Image displayImage;

    [Tooltip("Panel hoặc Canvas nền (để làm mờ nền phía sau ảnh nếu muốn)")]
    public GameObject viewerPanel;

    private bool isViewing = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (viewerPanel != null)
            viewerPanel.SetActive(false);
    }

    public void ViewPoster(Sprite posterSprite)
    {
        if (displayImage != null && posterSprite != null)
        {
            displayImage.sprite = posterSprite;
            displayImage.preserveAspect = true; // Giữ nguyên tỷ lệ ảnh để không bị méo
            
            isViewing = true;
            if (viewerPanel != null) viewerPanel.SetActive(true);

            // Tắt chuột và đóng băng người chơi
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            FirstPersonController.CanMove = false;
        }
    }

    void Update()
    {
        if (isViewing)
        {
            // Bấm ESC hoặc Chuột trái/Chuột phải để đóng ảnh
            if (Keyboard.current.escapeKey.wasPressedThisFrame || 
                Mouse.current.leftButton.wasPressedThisFrame || 
                Mouse.current.rightButton.wasPressedThisFrame)
            {
                ClosePoster();
            }
        }
    }

    public void ClosePoster()
    {
        isViewing = false;
        if (viewerPanel != null) viewerPanel.SetActive(false);

        // Trả lại trạng thái cho người chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        FirstPersonController.CanMove = true;
    }
}
