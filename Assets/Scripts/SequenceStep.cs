using UnityEngine;

/// <summary>
/// Lớp cơ sở cho mọi bước trong SequenceManager.
/// </summary>
public abstract class SequenceStep : MonoBehaviour
{
    [Tooltip("Thời gian chờ (giây) TRƯỚC KHI bước này bắt đầu")]
    public float waitBeforeStart = 0f;

    [Header("=== NHIỆM VỤ (OBJECTIVE) ===")]
    [Tooltip("Tiêu đề nhiệm vụ hiện ở góc màn hình (VD: Phục vụ khách). Bỏ trống nếu không muốn hiện.")]
    public string objectiveTitle;

    protected bool isCompleted = false;

    /// <summary>
    /// Gọi bởi SequenceManager khi bước này được kích hoạt (sau khi đã chờ xong).
    /// </summary>
    public abstract void StartStep();

    /// <summary>
    /// Các lớp con gọi hàm này khi nhiệm vụ hoàn thành để báo cho Manager đi tiếp.
    /// </summary>
    protected void CompleteStep()
    {
        if (isCompleted) return;
        isCompleted = true;
        
        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveTitle))
        {
            ObjectiveManager.Instance.CompleteObjective();
        }

        if (SequenceManager.Instance != null)
        {
            SequenceManager.Instance.NextStep();
        }
    }
}
