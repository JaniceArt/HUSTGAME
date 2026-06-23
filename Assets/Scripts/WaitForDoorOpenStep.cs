using System.Collections;
using UnityEngine;

/// <summary>
/// Bước chờ người chơi mở cửa:
/// Hiện nhiệm vụ "Mở cửa tiệm". Khi cửa kéo được mở, bước này hoàn thành và chạy sự kiện tiếp theo.
/// </summary>
public class WaitForDoorOpenStep : SequenceStep
{
    private bool isWaitingForDoor = false;

    public override void StartStep()
    {
        isWaitingForDoor = true;
        
        if (ObjectiveManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(objectiveTitle))
                ObjectiveManager.Instance.ShowObjective(objectiveTitle);
            else
                ObjectiveManager.Instance.ShowObjective("Đến kéo cửa cuốn để mở tiệm");
        }

        Debug.Log($"[WaitForDoorOpenStep] Đang chờ người chơi mở cửa để bắt đầu ngày mới...");
    }

    public void OnDoorOpened()
    {
        if (!isWaitingForDoor) return;
        isWaitingForDoor = false;
        
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.CompleteObjective();
        }

        CompleteStep(); // Chuyển sang sự kiện khách hàng đầu tiên
    }
}
