using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Dùng hệ thống Input mới

public class PauseMenuManager : MonoBehaviour
{
    [Header("=== GIAO DIỆN CHÍNH ===")]
    [Tooltip("Panel chứa Menu Tạm Dừng")]
    public GameObject pauseMenuPanel;
    
    [Header("=== CÀI ĐẶT CHUYỂN SCENE ===")]
    [Tooltip("Tên Scene của Menu Chính (VD: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("=== ÂM THANH ===")]
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float hoverVolume = 0.8f;
    public AudioClip clickSound;
    [Range(0f, 1f)] public float clickVolume = 1f;

    private AudioSource audioSource;

    // Trạng thái game có đang bị tạm dừng hay không
    public static bool isPaused = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Khi mới vào game, chắc chắn menu tắt và thời gian trôi bình thường
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        bool isEscPressed = false;
        
        // Dùng Input System mới
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            isEscPressed = true;
        }
#endif

        // Dùng Input System cũ (để phòng hờ)
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isEscPressed = true;
        }
#endif

        // Tự động Bật/Tắt (Toggle) khi bấm ESC
        if (isEscPressed)
        {
            Debug.Log("[PauseMenu] Đã bấm phím ESC!");
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        // Bật panel
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        
        // Đóng băng thời gian
        Time.timeScale = 0f;
        isPaused = true;

        // Mở khóa và hiện con trỏ chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Báo cho nhân vật chính ngừng di chuyển/xoay camera
        FirstPersonController.CanMove = false;
    }

    public void ResumeGame()
    {
        // Tắt panel
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        
        // Trả lại thời gian
        Time.timeScale = 1f;
        isPaused = false;

        // Khóa lại con trỏ chuột vào giữa màn hình
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Cho phép nhân vật di chuyển lại
        FirstPersonController.CanMove = true;
    }

    public void ReturnToMainMenu()
    {
        PlayClickSound();
        // TRẢ LẠI THỜI GIAN TRƯỚC KHI CHUYỂN SCENE
        // Cực kỳ quan trọng: Nếu giữ nguyên timescale = 0 thì sang menu chính sẽ bị kẹt!
        Time.timeScale = 1f;
        isPaused = false;
        
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        PlayClickSound();
        Debug.Log("[PauseMenu] Đã bấm thoát game!");
        Application.Quit();
        
#if UNITY_EDITOR
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
}
