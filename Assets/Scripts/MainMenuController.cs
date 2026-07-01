using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gắn script này vào một GameObject trống (VD: MainMenuManager) trong Scene Menu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("=== CÀI ĐẶT CHUYỂN SCENE ===")]
    [Tooltip("Tên Scene bạn muốn nạp khi bấm nút Play (Phải có trong Build Settings)")]
    public string firstGameSceneName = "Day1_Game";

    [Header("=== HIỆU ỨNG (TÙY CHỌN) ===")]
    [Tooltip("Panel đen để làm hiệu ứng fade out (CanvasGroup)")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1f;
    
    [Header("=== ÂM THANH ===")]
    [Tooltip("Nhạc nền (sẽ lặp lại liên tục)")]
    public AudioClip bgmSound;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    [Tooltip("Âm thanh khi di chuột qua nút (Hover)")]
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float hoverVolume = 0.8f;

    [Tooltip("Âm thanh khi bấm nút (Click)")]
    public AudioClip clickSound;
    [Range(0f, 1f)] public float clickVolume = 1f;

    private AudioSource audioSource;
    private bool isLoading = false;

    private void Awake()
    {
        // Mở lại trỏ chuột khi ở Main Menu (đề phòng từ game thoát ra bị khoá chuột)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        // Phát nhạc nền (BGM)
        if (bgmSound != null)
        {
            audioSource.clip = bgmSound;
            audioSource.volume = bgmVolume;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        // Mới vào menu thì sáng dần màn hình (nếu có Fade)
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f; // Bỏ Fade In, chỉ giữ alpha trong suốt từ đầu
        }
    }

    /// <summary>
    /// Kéo hàm này vào sự kiện OnClick() của nút PLAY
    /// </summary>
    public void PlayGame()
    {
        if (isLoading) return;
        PlayClickSound();
        isLoading = true;
        
        StartCoroutine(FadeOutAndLoad());
    }

    /// <summary>
    /// Kéo hàm này vào sự kiện OnClick() của nút QUIT / EXIT
    /// </summary>
    public void QuitGame()
    {
        if (isLoading) return;
        PlayClickSound();
        Debug.Log("[MainMenu] Đã bấm thoát game!");
        
        // Application.Quit() chỉ hoạt động khi Build ra file .exe, trong Editor nó sẽ không tự tắt
        Application.Quit();

#if UNITY_EDITOR
        // Dòng này giúp dừng Play Mode khi đang test trong Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void PlayHoverSound()
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }


    private IEnumerator FadeOutAndLoad()
    {
        // Mờ đen dần
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            
            if (fadeCanvas != null)
                fadeCanvas.alpha = t / fadeDuration;
            else
                autoFadeAlpha = t / fadeDuration; // Dùng fade code nếu lười
                
            yield return null;
        }
        
        if (fadeCanvas != null) fadeCanvas.alpha = 1f;
        else autoFadeAlpha = 1f;

        // Chuyển Scene
        SceneManager.LoadScene(firstGameSceneName);
    }

    // --- CÁCH LƯỜI BIẾNG: TỰ VẼ MÀN HÌNH ĐEN BẰNG CODE ---
    private float autoFadeAlpha = 0f;
    private Texture2D blackTexture;

    private void OnGUI()
    {
        // Nếu người chơi có dùng UI Canvas thì bỏ qua hàm này
        if (fadeCanvas != null) return;

        if (autoFadeAlpha > 0f)
        {
            if (blackTexture == null)
            {
                blackTexture = new Texture2D(1, 1);
                blackTexture.SetPixel(0, 0, Color.black);
                blackTexture.Apply();
            }

            GUI.color = new Color(0, 0, 0, autoFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
        }
    }
}
