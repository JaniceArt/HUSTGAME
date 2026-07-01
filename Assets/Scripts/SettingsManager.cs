using UnityEngine;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("=== GIAO DIỆN CHỮ ===")]
    [Tooltip("Chữ hiển thị số Âm lượng (từ 0 đến 10)")]
    public TextMeshProUGUI volumeText;
    
    [Tooltip("Chữ hiển thị số Độ nhạy chuột (từ 1 đến 10)")]
    public TextMeshProUGUI sensitivityText;

    [Header("=== QUẢN LÝ MENU ===")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    // Các biến lưu giá trị hiện tại
    private int currentVolume = 10; // 0 đến 10
    private int currentSensitivity = 5; // 1 đến 10

    void Start()
    {
        // Ép cứng Fullscreen theo yêu cầu
        Screen.fullScreen = true;

        // 1. Tải cài đặt từ lần chơi trước (nếu có), nếu chưa có thì lấy mặc định
        currentVolume = PlayerPrefs.GetInt("MasterVolume", 10);    // Mặc định 10
        currentSensitivity = PlayerPrefs.GetInt("MouseSensitivity", 5); // Mặc định 5

        // Áp dụng ngay cài đặt khi vừa mở game
        ApplySettings();
        UpdateUI();
    }

    // --- CHUYỂN ĐỔI MENU ---
    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // --- ÂM LƯỢNG (VOLUME) ---
    public void IncreaseVolume()
    {
        if (currentVolume < 10)
        {
            currentVolume++;
            ApplySettings();
            UpdateUI();
            SaveSettings();
        }
    }

    public void DecreaseVolume()
    {
        if (currentVolume > 0)
        {
            currentVolume--;
            ApplySettings();
            UpdateUI();
            SaveSettings();
        }
    }

    // --- ĐỘ NHẠY CHUỘT (SENSITIVITY) ---
    public void IncreaseSensitivity()
    {
        if (currentSensitivity < 10)
        {
            currentSensitivity++;
            ApplySettings();
            UpdateUI();
            SaveSettings();
        }
    }

    public void DecreaseSensitivity()
    {
        if (currentSensitivity > 1)
        {
            currentSensitivity--;
            ApplySettings();
            UpdateUI();
            SaveSettings();
        }
    }

    // --- HÀM HỖ TRỢ LÕI ---
    private void ApplySettings()
    {
        // Áp dụng Âm lượng (Quy đổi 0-10 thành 0.0 - 1.0)
        AudioListener.volume = currentVolume / 10f;

        // Cập nhật độ nhạy chuột cho Player nếu đang ở trong màn chơi
        var player = FindObjectOfType<FirstPersonController>();
        if (player != null)
        {
            player.UpdateSensitivityFromSettings(currentSensitivity);
        }
        else
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerObj.SendMessage("UpdateSensitivityFromSettings", currentSensitivity, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void UpdateUI()
    {
        if (volumeText != null)
            volumeText.text = currentVolume.ToString();

        if (sensitivityText != null)
            sensitivityText.text = currentSensitivity.ToString();
    }

    private void SaveSettings()
    {
        // Lưu vào ổ cứng để lần sau mở game vẫn giữ nguyên
        PlayerPrefs.SetInt("MasterVolume", currentVolume);
        PlayerPrefs.SetInt("MouseSensitivity", currentSensitivity);
        PlayerPrefs.Save();
    }
}
