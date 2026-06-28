using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bước dọn dẹp: Yêu cầu nhặt chổi và chà sạch các vết bẩn.
/// Gắn script này vào 1 Empty Object (ví dụ: CleaningTask) và thả vào SequenceManager.
/// </summary>
public class CleaningTaskStep : SequenceStep
{
    // Cập nhật tĩnh liên tục tới bước dọn dẹp ĐANG CHẠY
    public static CleaningTaskStep Instance { get; private set; }

    [Header("=== TRẠNG THÁI (chỉ đọc) ===")]
    [SerializeField, ReadOnly] private bool isHoldingBroom = false;
    public bool IsHoldingBroom => isHoldingBroom;

    [Header("=== CÀI ĐẶT DỌN DẸP ===")]
    [Tooltip("Cây chổi nằm dưới đất để nhặt")]
    public GameObject floorBroom;

    [Tooltip("Cây chổi hiển thị trên tay nhân vật (ẩn lúc đầu)")]
    public GameObject heldBroomVisual;

    [Tooltip("Hộp vô hình bự để trả chổi cho dễ ngắm (Tùy chọn)")]
    public GameObject broomReturnArea;

    [Tooltip("Danh sách các vết bẩn cần dọn (phải gắn script InteractableObject type = Stain)")]
    public List<GameObject> stains = new List<GameObject>();

    [Header("=== ÂM THANH ===")]
    [Tooltip("Âm thanh chà xát")]
    public AudioClip scrubSound;
    private AudioSource audioSource;

    private int totalStains;
    private int cleanedStains = 0;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // TỰ ĐỘNG TẮT HẾT VẾT BẨN LÚC MỚI VÀO GAME (Cây chổi thì vẫn để yên đó)
        foreach (var stain in stains)
        {
            if (stain != null) stain.SetActive(false);
        }
    }

    public override void StartStep()
    {
        Instance = this; // Gán Instance là bước ĐANG CHẠY hiện tại
        gameObject.SetActive(true);
        totalStains = stains.Count;
        cleanedStains = 0;

        // Hiện chổi dưới đất
        if (floorBroom != null)
        {
            floorBroom.SetActive(true);
            InteractableObject io = floorBroom.GetComponent<InteractableObject>();
            if (io != null) io.enabled = true; // Bật lại để nhặt được
        }
        
        // Ẩn chổi trên tay và khu vực trả chổi
        if (heldBroomVisual != null) heldBroomVisual.SetActive(false);
        if (broomReturnArea != null) broomReturnArea.SetActive(false);

        // Hiện tất cả vết bẩn
        foreach (var stain in stains)
        {
            if (stain != null) stain.SetActive(true);
        }

        UpdateObjective();
        Debug.Log("[CleaningTask] Bắt đầu nhiệm vụ dọn dẹp!");
    }

    public void PickUpBroom()
    {
        isHoldingBroom = true;
        if (floorBroom != null)
        {
            MeshRenderer[] renderers = floorBroom.GetComponentsInChildren<MeshRenderer>();
            foreach(var r in renderers) r.enabled = false;
        }
        if (heldBroomVisual != null) heldBroomVisual.SetActive(true);
        
        Debug.Log("[CleaningTask] Đã cầm chổi!");
    }

    public void CleanStain(GameObject stain)
    {
        if (stains.Contains(stain))
        {
            // Chỉ ẨN đi thay vì Destroy để hôm sau có thể dùng lại
            stain.SetActive(false); 
            cleanedStains++;
            
            UpdateObjective();

            if (cleanedStains >= totalStains)
            {
                FinishCleaning();
            }
        }
    }

    private void UpdateObjective()
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective($"Dọn dẹp vết bẩn ({cleanedStains}/{totalStains})");
        }
    }

    public void PlayScrubSound()
    {
        if (audioSource != null && scrubSound != null && !audioSource.isPlaying)
        {
            audioSource.clip = scrubSound;
            audioSource.Play();
        }
    }

    public void StopScrubSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public bool AllStainsCleaned => totalStains > 0 && cleanedStains >= totalStains;

    private void FinishCleaning()
    {
        Debug.Log("[CleaningTask] Đã dọn xong tất cả vết bẩn! Yêu cầu mang cất chổi.");
        
        // Bật cái hộp trả chổi bự lên cho người chơi dễ ngắm
        if (broomReturnArea != null) broomReturnArea.SetActive(true);

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective("Mang chổi về cất");
        }
    }

    public void ReturnBroom()
    {
        isHoldingBroom = false;
        if (heldBroomVisual != null) heldBroomVisual.SetActive(false);
        if (broomReturnArea != null) broomReturnArea.SetActive(false); // Ẩn cái hộp bự đi

        if (floorBroom != null)
        {
            MeshRenderer[] renderers = floorBroom.GetComponentsInChildren<MeshRenderer>();
            foreach(var r in renderers) r.enabled = true;
            
            // Tắt tạm thời thay vì XÓA VĨNH VIỄN để hôm sau còn xài lại
            InteractableObject io = floorBroom.GetComponent<InteractableObject>();
            if (io != null) io.enabled = false;
        }

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.HideObjective();
        }

        Debug.Log("[CleaningTask] Đã cất chổi xong! Chuyển bước tiếp theo.");
        Instance = null; // Dọn dẹp con trỏ
        CompleteStep();
    }
}
