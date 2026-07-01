using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalPhoneCallStep : SequenceStep
{
    public static NormalPhoneCallStep Instance { get; private set; }
    [Header("=== CÀI ĐẶT ĐIỆN THOẠI ===")]
    [Tooltip("Kéo GameObject Điện Thoại (có gắn InteractableObject type = Phone) vào đây")]
    public GameObject phoneObject;
    public AudioClip ringSound;
    public AudioClip hangUpSound;

    [Tooltip("Nhiệm vụ sẽ hiện ra màn hình (VD: Nghe điện thoại)")]
    public string objectiveText = "Nghe điện thoại đang reo";

    [Header("=== KỊCH BẢN THOẠI ===")]
    [Tooltip("Thoại lúc vừa nhấc máy (VD: Alo? Ai đấy?)")]
    public List<DialogNode> preCallDialog;

    [Tooltip("Giọng nói từ đầu dây bên kia (phát sau câu Alo)")]
    public List<DialogNode> phoneVoiceDialog;

    [Tooltip("Thoại sau khi cúp máy (VD: Lại gọi nhầm số...)")]
    public List<DialogNode> afterCallDialog;

    private AudioSource phoneAudio;
    private bool isPhoneAnswered = false;
    public bool IsWaitingForAnswer => !isPhoneAnswered;

    void Awake()
    {
        phoneAudio = gameObject.AddComponent<AudioSource>();
    }

    public override void StartStep()
    {
        Instance = this;
        isPhoneAnswered = false;
        FirstPersonController.CanMove = true;
        
        // Bật tương tác cho điện thoại
        if (phoneObject != null)
        {
            InteractableObject io = phoneObject.GetComponent<InteractableObject>();
            if (io != null) io.enabled = true;
        }

        // Không hiện tutorial
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.HideObjective();
        }

        // Bắt đầu reo chuông liên tục
        if (ringSound != null)
        {
            phoneAudio.clip = ringSound;
            phoneAudio.loop = true;
            phoneAudio.Play();
        }
        
        Debug.Log("[NormalPhoneCall] Điện thoại đang reo...");
    }

    // Được gọi từ InteractionSystem khi người chơi click vào Điện Thoại
    public void AnswerPhone()
    {
        if (isPhoneAnswered) return;
        isPhoneAnswered = true;

        // Tắt tương tác điện thoại
        if (phoneObject != null)
        {
            InteractableObject io = phoneObject.GetComponent<InteractableObject>();
            if (io != null) io.enabled = false;
        }

        // Ẩn nhiệm vụ
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.CompleteObjective();
            ObjectiveManager.Instance.HideObjective();
        }

        // Bắt đầu chuỗi kịch bản gọi điện
        StartCoroutine(PhoneCallRoutine());
    }

    IEnumerator PhoneCallRoutine()
    {
        // Khoá người chơi để đứng yên nói chuyện
        FirstPersonController.CanMove = false;
        
        // Cố tình ép camera nhìn vào điện thoại một xíu cho tập trung
        Transform player = FindObjectOfType<FirstPersonController>().transform;
        if (player != null && phoneObject != null)
        {
            Vector3 dir = (phoneObject.transform.position - player.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) player.rotation = Quaternion.LookRotation(dir);
        }

        // Nhấc máy -> Tắt tiếng chuông
        phoneAudio.Stop();
        phoneAudio.loop = false;

        // 1. Cho chạy thoại ngập ngừng (VD: Alo? ...)
        if (DialogManager.Instance != null && preCallDialog != null && preCallDialog.Count > 0)
        {
            bool isPreDone = false;
            DialogManager.Instance.StartDialogSequence(preCallDialog, (result) => { isPreDone = true; });
            while (!isPreDone) yield return null;
        }
        
        // 2. Giọng nói bên kia điện thoại
        if (DialogManager.Instance != null && phoneVoiceDialog != null && phoneVoiceDialog.Count > 0)
        {
            bool isVoiceDone = false;
            // Thoại điện thoại: Bấm chuột đọc bình thường, có thể chọn Choice
            DialogManager.Instance.StartDialogSequence(phoneVoiceDialog, (result) => { isVoiceDone = true; }, new Color(0.6f, 0.85f, 1f)); // Màu xanh da trời nhạt cho người gọi
            while (!isVoiceDone) yield return null;
        }

        // 3. Tắt máy
        if (hangUpSound != null)
        {
            phoneAudio.PlayOneShot(hangUpSound);
        }
        yield return new WaitForSeconds(1.5f); // Dư âm xíu

        // 4. Thoại nội tâm sau khi cúp máy
        if (DialogManager.Instance != null && afterCallDialog != null && afterCallDialog.Count > 0)
        {
            bool isAfterDone = false;
            DialogManager.Instance.StartDialogSequence(afterCallDialog, (result) => { isAfterDone = true; });
            while (!isAfterDone) yield return null;
        }

        // Trả lại điều khiển
        FirstPersonController.CanMove = true;
        
        Debug.Log("[NormalPhoneCall] Cuộc gọi kết thúc bình thường.");
        
        // Chuyển sang sự kiện tiếp theo
        CompleteStep();
        Instance = null;
    }
}
