using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý chuỗi sự kiện tuyến tính trong game (Ví dụ: Khách A -> Máy bay giấy -> Khách B)
/// Gắn lên 1 GameManager object trong Scene.
/// </summary>
public class SequenceManager : MonoBehaviour
{
    public static SequenceManager Instance { get; private set; }

    [Tooltip("Danh sách các bước trong kịch bản. Chạy từ trên xuống dưới.")]
    public List<SequenceStep> steps = new List<SequenceStep>();

    [Tooltip("Nếu true: Tự động chạy ngay khi game bắt đầu. Nếu false: Chờ hàm StartSequence() được gọi.")]
    public bool startAutomatically = false;

    private int currentStepIndex = 0;
    private bool hasStarted = false;

    public int CurrentStepIndex => currentStepIndex;
    public bool IsFinished => hasStarted && currentStepIndex >= steps.Count;
    public SequenceStep CurrentStep => (currentStepIndex >= 0 && currentStepIndex < steps.Count) ? steps[currentStepIndex] : null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Vô hiệu hóa tất cả các bước (ngoại trừ bước đầu tiên) lúc khởi đầu
        foreach (var step in steps)
        {
            if (step != null && step.gameObject != this.gameObject)
                step.gameObject.SetActive(false);
        }

        // Bắt đầu bước đầu tiên nếu được gán tự động chạy
        if (startAutomatically)
        {
            StartSequence();
        }
    }

    /// <summary>
    /// Gọi hàm này (VD: Từ SlidingDoor) để bắt đầu kịch bản nếu startAutomatically = false.
    /// </summary>
    public void StartSequence()
    {
        if (hasStarted) return; // Không cho phép chạy lại nếu đã chạy rồi
        hasStarted = true;

        if (steps.Count > 0 && steps[0] != null)
        {
            currentStepIndex = 0;
            StartCoroutine(RunStepRoutine(steps[currentStepIndex]));
        }
    }

    /// <summary>
    /// Gọi bởi SequenceStep hiện tại khi nó đã hoàn thành nhiệm vụ.
    /// Kích hoạt bước tiếp theo.
    /// </summary>
    public void NextStep()
    {
        if (currentStepIndex < steps.Count)
        {
            Debug.Log($"[Sequence] Đã hoàn thành bước {currentStepIndex}: {steps[currentStepIndex].gameObject.name}");
            currentStepIndex++;
        }

        if (currentStepIndex < steps.Count && steps[currentStepIndex] != null)
        {
            StartCoroutine(RunStepRoutine(steps[currentStepIndex]));
        }
        else
        {
            Debug.Log("[Sequence] TOÀN BỘ KỊCH BẢN ĐÃ HOÀN THÀNH!");
            // Nếu bạn muốn báo hết game, có thể bật UI Hết Game ở đây!
        }
    }

    private IEnumerator RunStepRoutine(SequenceStep step)
    {
        if (step.waitBeforeStart > 0)
        {
            Debug.Log($"[Sequence] Đang chờ {step.waitBeforeStart} giây trước khi gọi: {step.gameObject.name}...");
            yield return new WaitForSeconds(step.waitBeforeStart);
        }

        Debug.Log($"[Sequence] Bắt đầu bước {currentStepIndex}: {step.gameObject.name}");
        step.gameObject.SetActive(true);
        step.StartStep();

        // Hiển thị nhiệm vụ lên màn hình (nếu có)
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective(step.objectiveTitle);
        }
    }
}
