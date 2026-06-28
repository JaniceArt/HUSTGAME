using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FindSpeakerStep : SequenceStep
{
    [Header("=== CÀI ĐẶT LOA ===")]
    [Tooltip("Kéo cục Loa (Speaker) vào đây")]
    public GameObject speakerObject; 
    
    [Tooltip("Nguồn phát âm thanh gắn trên Loa")]
    public AudioSource speakerAudio; 

    [Tooltip("Tầm nhìn để bấm chuột tắt loa (mét)")]
    public float interactDistance = 3f;

    [Header("=== THOẠI BẮT ĐẦU (KHI VỪA NGHE TIẾNG) ===")]
    public List<DialogNode> startDialogs;

    [Header("=== THOẠI KẾT THÚC (SAU KHI TẮT LOA) ===")]
    public List<DialogNode> endDialogs;

    private bool isSpeakerFound = false;
    private bool isStepActive = false;
    private bool wasShowingPrompt = false;

    public override void StartStep()
    {
        isStepActive = true;
        isSpeakerFound = false;

        Debug.Log("[FindSpeakerStep] Bắt đầu sự kiện tìm loa!");

        if (speakerObject != null) speakerObject.SetActive(true);
        if (speakerAudio != null) speakerAudio.Play();

        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveTitle))
        {
            ObjectiveManager.Instance.ShowObjective(objectiveTitle);
        }

        StartCoroutine(StartInitialDialog());
    }

    IEnumerator StartInitialDialog()
    {
        // Chờ 10s cho âm thanh phát ra hù người chơi tí đã rồi mới nói nhảm
        yield return new WaitForSeconds(10f);
        
        if (DialogManager.Instance != null && startDialogs != null && startDialogs.Count > 0)
        {
            FirstPersonController.CanMove = false; // Khóa camera lúc đang thoại tự nhẩm
            DialogManager.Instance.StartDialogSequence(startDialogs, (result) => 
            {
                FirstPersonController.CanMove = true;
            });
        }
    }

    // XÓA HẾT HÀM UPDATE VÌ BÂY GIỜ SẼ DÙNG INTERACTABLE OBJECT ĐỂ TƯƠNG TÁC CHUẨN!

    public void TurnOffSpeaker()
    {
        if (isSpeakerFound || !isStepActive) return; // Tránh bấm nhiều lần
        isSpeakerFound = true;
        Debug.Log("[FindSpeakerStep] Đã tắt loa!");
        
        if (speakerAudio != null) speakerAudio.Stop();
        
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective("Đã tắt loa, mọi thứ lại im ắng...");
        }

        if (DialogManager.Instance != null && endDialogs != null && endDialogs.Count > 0)
        {
            FirstPersonController.CanMove = false;
            DialogManager.Instance.StartDialogSequence(endDialogs, (result) => 
            {
                FirstPersonController.CanMove = true;
                CompleteStep(); // Báo hiệu xong nhiệm vụ, chuyển sang Step tiếp theo
            });
        }
        else
        {
            CompleteStep();
        }
    }
}
