using UnityEngine;
using TMPro;
using System.Collections;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("=== UI GIAO DIỆN ===")]
    public GameObject objectivePanel; // Chứa nguyên cái cụm UI này
    public TextMeshProUGUI objectiveText; // Text duy nhất hiển thị cả Tiêu đề và Mô tả

    [Header("=== CÀI ĐẶT ===")]
    public string inProgressIcon = "O";
    public string completedIcon = "X";
    public Color completedColor = Color.gray;
    
    [Header("=== NHIỆM VỤ ĐẦU TIÊN (Lúc mới vào game) ===")]
    public string defaultObjectiveTitle = "Mở cửa tiệm";

    private Color originalTextColor;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        if (objectiveText != null) originalTextColor = objectiveText.color;
        
        // Hiện nhiệm vụ mặc định lúc đầu game
        ShowObjective(defaultObjectiveTitle);
    }

    /// <summary>
    /// Hiển thị nhiệm vụ mới. Nếu bỏ trống, sẽ ẩn UI.
    /// </summary>
    public void ShowObjective(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            HideObjective();
            return;
        }

        if (objectivePanel != null) objectivePanel.SetActive(true);

        if (objectiveText != null)
        {
            objectiveText.color = originalTextColor;
            objectiveText.text = $"{inProgressIcon} {title}";
        }
    }

    /// <summary>
    /// Đánh dấu hoàn thành nhiệm vụ (làm biến mất luôn theo yêu cầu)
    /// </summary>
    public void CompleteObjective()
    {
        HideObjective();
    }

    public void HideObjective()
    {
        if (objectivePanel != null) objectivePanel.SetActive(false);
    }
}
