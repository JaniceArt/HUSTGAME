using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bước dọn dẹp: Yêu cầu nhặt chổi và chà sạch các vết bẩn.
/// Gắn script này vào 1 Empty Object (ví dụ: CleaningTask) và thả vào SequenceManager.
/// </summary>
public class CleaningTaskStep : SequenceStep
{
    public static CleaningTaskStep Instance { get; private set; }

    [Header("=== TRẠNG THÁI (chỉ đọc) ===")]
    [SerializeField, ReadOnly] private bool isHoldingBroom = false;
    public bool IsHoldingBroom => isHoldingBroom;

    [Header("=== CÀI ĐẶT DỌN DẸP ===")]
    [Tooltip("Cây chổi nằm dưới đất để nhặt")]
    public GameObject floorBroom;

    [Tooltip("Cây chổi hiển thị trên tay nhân vật (ẩn lúc đầu)")]
    public GameObject heldBroomVisual;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        gameObject.SetActive(true);
        totalStains = stains.Count;
        cleanedStains = 0;

        // Hiện chổi dưới đất
        if (floorBroom != null) floorBroom.SetActive(true);
        
        // Ẩn chổi trên tay
        if (heldBroomVisual != null) heldBroomVisual.SetActive(false);

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
        if (floorBroom != null) floorBroom.SetActive(false);
        if (heldBroomVisual != null) heldBroomVisual.SetActive(true);
        
        Debug.Log("[CleaningTask] Đã cầm chổi!");
    }

    public void CleanStain(GameObject stain)
    {
        if (stains.Contains(stain))
        {
            stains.Remove(stain);
            Destroy(stain); // Xóa vết bẩn khỏi màn hình
            cleanedStains++;
            
            UpdateObjective();

            if (stains.Count == 0)
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
            audioSource.PlayOneShot(scrubSound);
        }
    }

    private void FinishCleaning()
    {
        Debug.Log("[CleaningTask] Đã dọn xong tất cả vết bẩn!");
        
        // Cất chổi
        isHoldingBroom = false;
        if (heldBroomVisual != null) heldBroomVisual.SetActive(false);

        // Chuyển sang bước tiếp theo
        SequenceManager.Instance.NextStep();
    }
}
