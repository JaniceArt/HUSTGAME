using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bước dọn vũng nước: Yêu cầu nhìn thấy vũng nước trước, tự thoại, rồi mới nhặt chổi lau.
/// Gắn script này vào 1 Empty Object (ví dụ: PuddleCleaningTask) và thả vào SequenceManager.
/// </summary>
public class PuddleCleaningTaskStep : SequenceStep
{
    // Cập nhật tĩnh liên tục tới bước dọn vũng nước ĐANG CHẠY
    public static PuddleCleaningTaskStep Instance { get; private set; }

    [Header("=== TRẠNG THÁI (chỉ đọc) ===")]
    [SerializeField, ReadOnly] private bool isHoldingBroom = false;
    public bool IsHoldingBroom => isHoldingBroom;

    [Header("=== PHÁT HIỆN VŨNG NƯỚC ===")]
    [Tooltip("Kịch bản thoại của nhân vật chính khi thấy vũng nước")]
    public List<DialogNode> discoveryMonologue;
    private bool hasDiscovered = false;

    [Header("=== CÀI ĐẶT DỌN DẸP ===")]
    [Tooltip("Cây chổi nằm dưới đất để nhặt")]
    public GameObject floorBroom;

    [Tooltip("Cây chổi hiển thị trên tay nhân vật (ẩn lúc đầu)")]
    public GameObject heldBroomVisual;

    [Tooltip("Hộp vô hình bự để trả chổi cho dễ ngắm (Tùy chọn)")]
    public GameObject broomReturnArea;

    [Tooltip("Danh sách các vũng nước cần dọn (phải gắn script InteractableObject type = Stain)")]
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

        // Ẩn vũng nước lúc đầu (Sẽ được bật lên bằng script Customer khi ông áo mưa đi về)
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
        
        hasDiscovered = false;

        // GIẤU cây chổi đi cho đến khi người chơi nhìn thấy vũng nước
        if (floorBroom != null) floorBroom.SetActive(false);
        
        // Ẩn chổi trên tay và khu vực trả chổi
        if (heldBroomVisual != null) heldBroomVisual.SetActive(false);
        if (broomReturnArea != null) broomReturnArea.SetActive(false);

        // Xóa chữ nhiệm vụ cũ nếu có
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.ShowObjective("");
        
        Debug.Log("[PuddleCleaningTask] Đang đợi người chơi phát hiện vũng nước...");
    }

    void Update()
    {
        if (!hasDiscovered)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>(); // Đề phòng Camera không có tag MainCamera
            if (cam == null) return;
            
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            
            // Dùng RaycastAll để tia sáng đi xuyên qua được mặt sàn (phòng trường hợp vũng nước nằm sát/bên dưới sàn)
            RaycastHit[] hits = Physics.RaycastAll(ray, 10f);
            foreach (RaycastHit hit in hits)
            {
                foreach (var stain in stains)
                {
                    // Vũng nước phải đang hiện hữu trên Scene (bật bởi Customer)
                    if (stain != null && stain.activeInHierarchy)
                    {
                        if (hit.collider.gameObject == stain || hit.collider.transform.IsChildOf(stain.transform))
                        {
                            TriggerDiscovery();
                            return;
                        }
                    }
                }
            }
        }
    }

    void TriggerDiscovery()
    {
        hasDiscovered = true;
        
        if (DialogManager.Instance != null && discoveryMonologue != null && discoveryMonologue.Count > 0)
        {
            DialogManager.Instance.StartDialogSequence(discoveryMonologue, (result) => 
            {
                StartCleaningPhase();
            });
        }
        else
        {
            StartCleaningPhase();
        }
    }

    void StartCleaningPhase()
    {
        // Hiện chổi dưới đất
        if (floorBroom != null)
        {
            floorBroom.SetActive(true);
            InteractableObject io = floorBroom.GetComponent<InteractableObject>();
            if (io != null) io.enabled = true; // Bật lại để nhặt được
        }
        
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective("Lấy cây chổi");
        }
        Debug.Log("[PuddleCleaningTask] Bắt đầu nhiệm vụ dọn dẹp!");
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
        
        UpdateObjective(); // Đổi chữ thành Lau sạch vũng nước
        Debug.Log("[PuddleCleaningTask] Đã cầm chổi!");
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
            ObjectiveManager.Instance.ShowObjective($"Lau sạch vũng nước ({cleanedStains}/{totalStains})");
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
        Debug.Log("[PuddleCleaningTask] Đã lau xong tất cả vũng nước! Yêu cầu mang cất chổi.");
        
        // Bật cái hộp trả chổi bự lên cho người chơi dễ ngắm
        if (broomReturnArea != null) broomReturnArea.SetActive(true);

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective("Mang cây chổi cất đi");
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

        Debug.Log("[PuddleCleaning] Đã cất chổi/cây lau nhà xong! Chuyển bước tiếp theo.");
        Instance = null; // Dọn dẹp con trỏ
        CompleteStep();
    }
}
