using UnityEngine;
using System.Collections.Generic;

public class CleaningTaskStep : SequenceStep
{
    public static CleaningTaskStep Instance { get; private set; }

    [Header("=== VẬT DỤNG DỌN DẸP ===")]
    [Tooltip("Cây chổi nằm dưới đất (Để nhặt)")]
    public GameObject floorBroom;
    
    [Tooltip("Cây chổi hiển thị trên tay Main (Sẽ bật khi nhặt)")]
    public GameObject heldBroomVisual;

    [Header("=== VẾT BẨN ===")]
    [Tooltip("Danh sách các vết bẩn cần dọn")]
    public List<GameObject> stains;

    public bool isHoldingBroom { get; private set; }
    private int totalStains;
    private int cleanedStains;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void StartStep()
    {
        gameObject.SetActive(true);
        totalStains = stains.Count;
        cleanedStains = 0;
        isHoldingBroom = false;

        if (floorBroom != null) floorBroom.SetActive(true);
        if (heldBroomVisual != null) heldBroomVisual.SetActive(false);

        foreach (var stain in stains)
        {
            if (stain != null) stain.SetActive(true);
        }

        UpdateObjective();
    }

    public void PickUpBroom()
    {
        isHoldingBroom = true;
        if (floorBroom != null) floorBroom.SetActive(false);
        if (heldBroomVisual != null) heldBroomVisual.SetActive(true);
        
        // Cập nhật lại UI cho chắc
        UpdateObjective();
    }

    public void CleanStain(GameObject stainObj)
    {
        if (stains.Contains(stainObj))
        {
            stains.Remove(stainObj);
            cleanedStains++;
            stainObj.SetActive(false); // Ẩn đi thay vì xóa để an toàn
            
            UpdateObjective();

            if (stains.Count == 0)
            {
                // Dọn xong! Bỏ chổi xuống (ẩn) và qua vòng
                if (heldBroomVisual != null) heldBroomVisual.SetActive(false);
                isHoldingBroom = false;
                
                SequenceManager.Instance.NextStep();
            }
        }
    }

    void UpdateObjective()
    {
        if (ObjectiveManager.Instance != null)
        {
            if (!isHoldingBroom)
            {
                ObjectiveManager.Instance.ShowObjective("Lấy chổi để dọn dẹp");
            }
            else
            {
                ObjectiveManager.Instance.ShowObjective($"Chà vết bẩn ({cleanedStains}/{totalStains})");
            }
        }
    }
}
