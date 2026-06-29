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
    public float fadeDuration = 1.5f;
    
    [Tooltip("Âm thanh bấm nút")]
    public AudioClip clickSound;

    private AudioSource audioSource;
    private bool isLoading = false;

    private void Awake()
    {
        // Mở lại trỏ chuột khi ở Main Menu (đề phòng từ game thoát ra bị khoá chuột)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        // Mới vào menu thì sáng dần màn hình (nếu có Fade)
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 1f;
            StartCoroutine(FadeInRoutine());
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
        
        if (fadeCanvas != null)
        {
            StartCoroutine(FadeOutAndLoad());
        }
        else
        {
            SceneManager.LoadScene(firstGameSceneName);
        }
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

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = 1f - (t / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }

    private IEnumerator FadeOutAndLoad()
    {
        // Mờ đen dần
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = t / fadeDuration;
            yield return null;
        }
        fadeCanvas.alpha = 1f;

        // Chuyển Scene
        SceneManager.LoadScene(firstGameSceneName);
    }
}
