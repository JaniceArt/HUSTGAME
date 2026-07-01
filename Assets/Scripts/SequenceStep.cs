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

    [Header("=== GỌI SỰ KIỆN TIẾP THEO SỚM ===")]
    [Tooltip("Nếu tick, sự kiện tiếp theo sẽ được gọi (chạy song song) sau X giây kể từ lúc sự kiện này BẮT ĐẦU.")]
    public bool autoAdvanceNextStep = false;
    
    [Tooltip("Số giây chờ (từ lúc bắt đầu) để gọi sự kiện tiếp theo.")]
    public float advanceDelay = 0f;

    protected bool isCompleted = false;

    public void ResetCompletion()
    {
        isCompleted = false;
    }

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

    public void CompleteStepEarly()
    {
        if (isCompleted) return;
        isCompleted = true;
        
        // Hoàn thành nhiệm vụ góc trái
        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveTitle))
        {
            ObjectiveManager.Instance.CompleteObjective();
        }

        if (SequenceManager.Instance != null)
        {
            Debug.Log($"[Sequence] Sự kiện {gameObject.name} BÁO HOÀN THÀNH SỚM để nhường bước cho sự kiện tiếp theo!");
            SequenceManager.Instance.NextStep();
        }
    }
}
