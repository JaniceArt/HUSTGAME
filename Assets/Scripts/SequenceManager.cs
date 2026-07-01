using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameDay
{
    public string dayName = "Ngày X";

    [Tooltip("Sự kiện bắt đầu ngày (VD: Chờ mở cửa)")]
    public SequenceStep dayStart;

    [Tooltip("Các sự kiện ở giữa (Khách hàng, hù ma, v.v...)")]
    public List<SequenceStep> events = new List<SequenceStep>();

    [Tooltip("Sự kiện kết thúc ngày (VD: Mờ đen màn hình)")]
    public SequenceStep dayEnd;
}

/// <summary>
/// Quản lý chuỗi sự kiện tuyến tính trong game chia theo Ngày.
/// </summary>
public class SequenceManager : MonoBehaviour
{
    public static SequenceManager Instance { get; private set; }

    [Header("=== MỞ ĐẦU ===")]
    [Tooltip("Cutscene giới thiệu ban đầu (chạy trước cả Ngày 1)")]
    public SequenceStep introCutscene;

    [Header("=== CÁC NGÀY TRONG GAME ===")]
    public List<GameDay> days = new List<GameDay>();

    [Header("=== CÀI ĐẶT ===")]
    [Tooltip("Nếu true: Tự động chạy ngay khi game bắt đầu. Nếu false: Chờ hàm StartSequence() được gọi.")]
    public bool startAutomatically = false;

    // --- State Nội Bộ ---
    private List<SequenceStep> flattenedSteps = new List<SequenceStep>();
    private int currentStepIndex = 0;
    private bool hasStarted = false;

    public int CurrentStepIndex => currentStepIndex;
    public bool IsFinished => hasStarted && currentStepIndex >= flattenedSteps.Count;
    public SequenceStep CurrentStep => (currentStepIndex >= 0 && currentStepIndex < flattenedSteps.Count) ? flattenedSteps[currentStepIndex] : null;

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
        // Trải phẳng tất cả các bước thành 1 mảng tuyến tính duy nhất để dễ chạy
        FlattenSteps();

        // Vô hiệu hóa tất cả các bước lúc khởi đầu
        foreach (var step in flattenedSteps)
        {
            if (step != null && step.gameObject != this.gameObject)
                step.gameObject.SetActive(false);
        }

        if (startAutomatically)
        {
            StartSequence();
        }
    }

    void FlattenSteps()
    {
        flattenedSteps.Clear();

        if (introCutscene != null) flattenedSteps.Add(introCutscene);

        foreach (var day in days)
        {
            if (day.dayStart != null) flattenedSteps.Add(day.dayStart);
            foreach (var ev in day.events)
            {
                if (ev != null) flattenedSteps.Add(ev);
            }
            if (day.dayEnd != null) flattenedSteps.Add(day.dayEnd);
        }
    }

    public void StartSequence()
    {
        if (hasStarted) return;
        hasStarted = true;

        if (flattenedSteps.Count > 0 && flattenedSteps[0] != null)
        {
            currentStepIndex = 0;
            StartCoroutine(RunStepRoutine(flattenedSteps[currentStepIndex]));
        }
    }

    public void NextStep()
    {
        if (currentStepIndex < flattenedSteps.Count)
        {
            Debug.Log($"[Sequence] Đã hoàn thành bước {currentStepIndex}: {flattenedSteps[currentStepIndex].gameObject.name}");
            currentStepIndex++;
        }

        if (currentStepIndex < flattenedSteps.Count && flattenedSteps[currentStepIndex] != null)
        {
            StartCoroutine(RunStepRoutine(flattenedSteps[currentStepIndex]));
        }
        else
        {
            Debug.Log("[Sequence] TOÀN BỘ KỊCH BẢN ĐÃ HOÀN THÀNH!");
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
        step.ResetCompletion(); // Reset trạng thái để dùng lại nếu cần
        step.StartStep();

        // Hiển thị nhiệm vụ lên màn hình (nếu có)
        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(step.objectiveTitle))
        {
            ObjectiveManager.Instance.ShowObjective(step.objectiveTitle);
        }

        if (step.autoAdvanceNextStep)
        {
            StartCoroutine(AutoAdvanceRoutine(step));
        }
    }

    private IEnumerator AutoAdvanceRoutine(SequenceStep step)
    {
        if (step.advanceDelay > 0)
        {
            yield return new WaitForSeconds(step.advanceDelay);
        }
        
        // Gọi hàm của step để báo hoàn thành sớm (chỉ báo hiệu để qua bước, step cũ vẫn chạy bth)
        step.CompleteStepEarly();
    }
}
