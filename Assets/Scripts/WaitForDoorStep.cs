using UnityEngine;

/// <summary>
/// Bước kịch bản: Chờ người chơi mở cửa cuốn rồi mới gọi khách hàng.
/// </summary>
public class WaitForDoorStep : SequenceStep
{
    [Tooltip("Kéo cái cửa cuốn (SlidingDoor) vào đây")]
    public SlidingDoor doorToWait;

    public override void StartStep()
    {
        // Nếu cửa đã mở sẵn rồi thì đi tiếp luôn
        if (doorToWait != null && doorToWait.IsOpen)
        {
            CompleteStep();
        }
    }

    void Update()
    {
        // Liên tục kiểm tra xem cửa đã mở chưa. Nếu mở rồi thì hoàn thành bước này và gọi bước tiếp theo.
        if (!isCompleted && doorToWait != null && doorToWait.IsOpen)
        {
            CompleteStep();
        }
    }
}
