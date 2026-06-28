using UnityEngine;
using System.Collections.Generic;

public class PuddleDiscoveryTrigger : SequenceStep
{
    [Tooltip("Vũng nước cần kiểm tra (Kéo vũng nước vào đây)")]
    public GameObject puddleObject;

    [Tooltip("Kịch bản thoại của nhân vật chính khi thấy vũng nước")]
    public List<DialogNode> playerMonologue;

    [Tooltip("Dòng chữ nhiệm vụ hiện lên sau khi nói xong (VD: Lấy cây lau nhà)")]
    public string tutorialText = "Lấy chổi lau nhà để lau vũng nước";

    private bool isActiveStep = false;
    private bool hasTriggered = false;

    public override void StartStep()
    {
        isActiveStep = true;
        hasTriggered = false;
    }

    void Update()
    {
        if (!isActiveStep || hasTriggered || puddleObject == null || !puddleObject.activeInHierarchy) return;

        // Phải có Main Camera
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // Bắn một tia Raycast từ giữa màn hình
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        
        // Bắn xa tối đa 10 mét. CHÚ Ý: Cái vũng nước phải có Collider (BoxCollider) thì tia Raycast mới chạm vào được!
        if (Physics.Raycast(ray, out hit, 10f))
        {
            if (hit.collider.gameObject == puddleObject || hit.collider.transform.IsChildOf(puddleObject.transform))
            {
                hasTriggered = true;
                
                // Hiện tự thoại
                if (DialogManager.Instance != null && playerMonologue != null && playerMonologue.Count > 0)
                {
                    DialogManager.Instance.StartDialogSequence(playerMonologue, (result) => 
                    {
                        ShowObjectiveAndNextStep();
                    });
                }
                else
                {
                    ShowObjectiveAndNextStep();
                }
            }
        }
    }

    void ShowObjectiveAndNextStep()
    {
        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(tutorialText))
        {
            ObjectiveManager.Instance.ShowObjective(tutorialText);
        }

        // Báo cho SequenceManager biết đã soi xong vũng nước và thoại xong,
        // để chuyển sang bước tiếp theo (Lau Dọn)
        CompleteStep();
    }
}
