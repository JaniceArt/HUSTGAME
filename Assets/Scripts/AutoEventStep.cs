using System.Collections;
using UnityEngine;

/// <summary>
/// Bước tự động trong cốt truyện: Chờ X giây -> Chạy Animation -> Gọi bước tiếp theo
/// </summary>
public class AutoEventStep : SequenceStep
{
    [Header("2. HIỆU ỨNG ANIMATION")]
    [Tooltip("Vật thể sẽ xuất hiện và chạy animation (VD: Tờ giấy bay)")]
    public GameObject targetObject;
    
    [Tooltip("Component Animator nằm trên vật thể đó")]
    public Animator targetAnimator;
    
    [Tooltip("Tên state animation cần chạy (VD: PaperFlyIn)")]
    public string animationName = "PaperFlyIn";

    [Header("3. KẾT THÚC")]
    [Tooltip("Chờ thêm mấy giây sau khi tờ giấy rơi xong để gọi Khách 2 vào?")]
    public float waitBeforeNextStep = 2f;

    void Awake()
    {
        // Tự động giấu tờ giấy đi lúc mới vào game (phòng trường hợp bạn quên tắt tick trong Inspector)
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }

    public override void StartStep()
    {
        StartCoroutine(EventRoutine());
    }

    IEnumerator EventRoutine()
    {
        // Thời gian chờ ban đầu (waitBeforeStart) đã được SequenceManager tự động đếm trước khi gọi StartStep() rồi!


        // 2. Bật tờ giấy lên và bắt đầu bay
        if (targetObject != null)
        {
            targetObject.SetActive(true);
            
            if (targetAnimator != null && !string.IsNullOrEmpty(animationName))
            {
                Debug.Log($"[AutoEvent] Tờ giấy xuất hiện! Chạy animation: {animationName}");
                targetAnimator.Play(animationName);
            }
        }

        // 3. Chờ animation bay xong
        if (waitBeforeNextStep > 0)
        {
            yield return new WaitForSeconds(waitBeforeNextStep);
        }

        // 4. Kiểm tra xem vật thể có phải là PaperAirplane không
        if (targetObject != null)
        {
            PaperAirplaneStep plane = targetObject.GetComponent<PaperAirplaneStep>();
            if (plane != null)
            {
                Debug.Log("[AutoEvent] Target là Paper Airplane! Nhường quyền sang màn cho Paper Airplane (đợi người chơi bấm V).");
                // KHÔNG gọi CompleteStep ở đây! PaperAirplaneStep sẽ tự gọi sau khi người chơi xem.
                yield break; 
            }
        }

        // Báo cáo hoàn thành nếu là một Event bình thường
        Debug.Log("[AutoEvent] Đã bay xong! Gọi bước tiếp theo.");
        CompleteStep();
    }
}
