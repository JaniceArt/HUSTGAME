using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhoneCallEventStep : SequenceStep
{
    public static PhoneCallEventStep Instance { get; private set; }
    [Header("=== CÀI ĐẶT ĐIỆN THOẠI ===")]
    [Tooltip("Kéo GameObject Điện Thoại (có gắn InteractableObject type = Phone) vào đây")]
    public GameObject phoneObject;
    public AudioClip ringSound;
    public AudioClip weirdSound;
    public AudioClip hangUpSound;

    [Tooltip("Thoại lẩm bẩm lúc vừa nhấc máy (VD: Alo? ... ) - Để màn hình không bị đứng yên")]
    public List<DialogNode> preCallDialog;

    [Tooltip("Giọng nói từ đầu dây bên kia (phát sau tiếng kì lạ)")]
    public List<DialogNode> phoneVoiceDialog;

    [Header("=== THỰC THỂ BÊN NGOÀI ===")]
    [Tooltip("Kéo prefab/GameObject con ma ở ngoài cửa vào đây")]
    public GameObject scaryEntity;
    [Tooltip("Vị trí ma đứng nhìn chằm chằm (ngoài đường)")]
    public Transform outsideSpawnPoint;
    [Tooltip("Âm thanh lúc ma lảng vảng ngoài cửa sau khi đóng (tiếng cào cửa, thở...)")]
    public AudioClip ghostWaitSound;
    [Tooltip("Thoại lúc ma lảng vảng ngoài cửa (VD: Mở cửa ra...)")]
    public List<DialogNode> ghostWaitDialog;
    [Tooltip("Thoại nội tâm của Main sau khi ma đã rời đi (VD: Thoát rồi, sợ quá...)")]
    public List<DialogNode> afterGhostDialog;
    [Tooltip("Thời gian (giây) người chơi có để chạy ra đóng cửa")]
    public float timeToCloseDoor = 7f; 
    
    [Header("=== GAME OVER ===")]
    [Tooltip("UI Game Over mờ đen hoặc dọa (kéo Canvas Group vào đây)")]
    public CanvasGroup gameOverCanvas;
    [Tooltip("Âm thanh Jumpscare khi Game Over")]
    public AudioClip jumpscareSound;

    private AudioSource phoneAudio;
    private AudioSource jumpscareAudio;
    private SlidingDoor mainDoor;
    private bool isPhoneAnswered = false;
    public bool IsWaitingForAnswer => !isPhoneAnswered;
    private bool isWaitingForDoorClose = false;
    private float doorTimer = 0f;

    void Awake()
    {
        phoneAudio = gameObject.AddComponent<AudioSource>();
        jumpscareAudio = gameObject.AddComponent<AudioSource>();
        if (scaryEntity != null) scaryEntity.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.alpha = 0f;
    }

    public override void StartStep()
    {
        Instance = this;
        // Reset trạng thái (dùng cho cả lúc mới bắt đầu hoặc lúc Game Over chơi lại)
        isPhoneAnswered = false;
        isWaitingForDoorClose = false;
        if (scaryEntity != null) scaryEntity.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.alpha = 0f;
        FirstPersonController.CanMove = true;
        
        mainDoor = FindObjectOfType<SlidingDoor>();

        // Bật tương tác cho điện thoại
        if (phoneObject != null)
        {
            InteractableObject io = phoneObject.GetComponent<InteractableObject>();
            if (io != null) io.enabled = true;
        }

        // Không hiện tutorial nữa theo yêu cầu
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
        
        Debug.Log("[PhoneEvent] Điện thoại đang reo...");
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

        // Ẩn nhiệm vụ nghe điện thoại
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.CompleteObjective();
            ObjectiveManager.Instance.HideObjective();
        }

        // Tạm thời vô hiệu hóa việc tự động mở khóa camera của DialogManager để giữ kịch bản
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.UnlockPlayerOnComplete = false;
        }

        // Bắt đầu chuỗi kịch bản gọi điện
        StartCoroutine(PhoneCallRoutine());
    }

    IEnumerator PhoneCallRoutine()
    {
        // 1. Khoá người chơi
        FirstPersonController.CanMove = false;
        
        // Cố tình ép camera nhìn vào điện thoại một xíu cho tập trung
        Transform player = FindObjectOfType<FirstPersonController>().transform;
        if (player != null && phoneObject != null)
        {
            Vector3 dir = (phoneObject.transform.position - player.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero) player.rotation = Quaternion.LookRotation(dir);
        }

        // 2. Phát âm thanh kì lạ ngầm trong background
        phoneAudio.loop = false;
        if (weirdSound != null)
        {
            phoneAudio.clip = weirdSound;
            phoneAudio.Play();
        }

        // 3. Cho chạy thoại ngập ngừng (VD: Alo? ...) để tránh đứng hình màn hình
        if (DialogManager.Instance != null && preCallDialog != null && preCallDialog.Count > 0)
        {
            bool isPreDone = false;
            // Thoại của nhân vật chính -> Cần bấm chuột để đọc xong
            DialogManager.Instance.StartDialogSequence(preCallDialog, (result) => { isPreDone = true; });
            while (!isPreDone) yield return null;
        }
        else
        {
            // Nếu không gán thoại thì ép đợi 3 giây
            yield return new WaitForSeconds(3f);
        }
        
        // 4. Giọng nói bên kia điện thoại
        if (DialogManager.Instance != null && phoneVoiceDialog != null && phoneVoiceDialog.Count > 0)
        {
            bool isVoiceDone = false;
            // Thoại điện thoại: Tự chạy, gõ chữ siêu siêu chậm (6x), tự tắt sau 4s
            DialogManager.Instance.StartAutoDialogSequence(phoneVoiceDialog, (result) => { isVoiceDone = true; }, 6f, 4f);
            while (!isVoiceDone) yield return null;
        }

        // Chờ tiếng cười/âm thanh kì lạ kết thúc hẳn rồi mới được cúp máy
        while (phoneAudio.isPlaying && phoneAudio.clip == weirdSound)
        {
            yield return null;
        }

        // 5. Tắt máy
        if (hangUpSound != null)
        {
            phoneAudio.PlayOneShot(hangUpSound);
        }
        yield return new WaitForSeconds(1f); // Dư âm xíu

        // 6. Cho phép di chuyển và xuất hiện con ma ngoài cửa
        FirstPersonController.CanMove = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Trả lại cài đặt mặc định cho DialogManager
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.UnlockPlayerOnComplete = true;
        }

        // Cảnh báo nhiệm vụ khẩn cấp!
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective("ĐÓNG CỬA NGAY LẬP TỨC!!!");
        }
        
        if (scaryEntity != null && outsideSpawnPoint != null)
        {
            scaryEntity.SetActive(true);

            // Dừng ngay mọi âm thanh mặc định (Play On Awake) của ma nữ để chờ đến đúng kịch bản
            AudioSource[] allAudio = scaryEntity.GetComponentsInChildren<AudioSource>();
            foreach (var a in allAudio) a.Stop();

            // An toàn: Tắt NavMeshAgent và Customer script (nếu có) để không bị kẹt khi teleport
            UnityEngine.AI.NavMeshAgent agent = scaryEntity.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            Customer cusScript = scaryEntity.GetComponent<Customer>();
            if (cusScript != null) cusScript.enabled = false;

            scaryEntity.transform.position = outsideSpawnPoint.position;
            
            // Ma nhìn chằm chằm vào cửa/người chơi
            if (player != null)
            {
                Vector3 lookDir = (player.position - scaryEntity.transform.position).normalized;
                lookDir.y = 0;
                scaryEntity.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        // Bắt đầu đếm ngược ngầm (Không hiện nhiệm vụ để người chơi tự hoảng loạn)
        doorTimer = timeToCloseDoor;
        isWaitingForDoorClose = true;
    }

    void Update()
    {
        if (isWaitingForDoorClose)
        {
            doorTimer -= Time.deltaTime;

            // Kiểm tra xem cửa đã đóng chưa
            if (mainDoor != null && !mainDoor.IsOpen)
            {
                // Người chơi ĐÃ ĐÓNG CỬA kịp thời! -> THẮNG
                isWaitingForDoorClose = false;
                StartCoroutine(SurviveRoutine());
            }
            else if (doorTimer <= 0f)
            {
                // HẾT GIỜ MÀ CỬA VẪN MỞ! -> GAME OVER
                isWaitingForDoorClose = false;
                StartCoroutine(GameOverRoutine());
            }
        }
    }

    IEnumerator SurviveRoutine()
    {
        Debug.Log("[PhoneEvent] Đã đóng cửa kịp thời! Thở phào nhẹ nhõm.");
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.CompleteObjective();
            ObjectiveManager.Instance.HideObjective();
        }

        // --- 1. Đợi 3 giây im lặng hãi hùng sau khi đóng cửa ---
        float silenceTimer = 3f;
        while (silenceTimer > 0)
        {
            silenceTimer -= Time.deltaTime;
            if (mainDoor != null && mainDoor.IsOpen)
            {
                StartCoroutine(GameOverRoutine());
                yield break;
            }
            yield return null;
        }

        // --- 2. Hết 3 giây, bắt đầu phá phách và gào rú ---
        // Phát âm thanh ma ở ngoài cửa (Full 3D)
        if (ghostWaitSound != null && scaryEntity != null)
        {
            AudioSource ghostAudio = scaryEntity.GetComponent<AudioSource>();
            if (ghostAudio == null) ghostAudio = scaryEntity.AddComponent<AudioSource>();
            
            ghostAudio.spatialBlend = 1f; // Full 3D
            ghostAudio.maxDistance = 25f;
            ghostAudio.rolloffMode = AudioRolloffMode.Linear;
            ghostAudio.clip = ghostWaitSound;
            ghostAudio.loop = false;
            ghostAudio.Play();
        }

        // Hiện thoại của ma ngoài cửa
        if (DialogManager.Instance != null && ghostWaitDialog != null && ghostWaitDialog.Count > 0)
        {
            // Cho phép người chơi tương tác (mở cửa) trong khi ma đang nói
            DialogManager.Instance.AllowInteraction = true;
            
            // Thoại ma: Tự chạy, gõ chữ siêu siêu chậm (8x), lề mề
            DialogManager.Instance.StartAutoDialogSequence(ghostWaitDialog, null, 8f, 3f);
        }

        // --- 3. Ma đứng ngoài cửa quậy đúng 20 giây ---
        float ghostTimer = 20f;
        while (ghostTimer > 0)
        {
            ghostTimer -= Time.deltaTime;
            
            // Nếu tò mò kéo cửa lên lại khi ma chưa đi -> CHẾT!
            if (mainDoor != null && mainDoor.IsOpen)
            {
                Debug.Log("[PhoneEvent] Mở cửa ra xem và bị vồ!");
                
                // Dừng tiếng ma lảng vảng để đổi sang tiếng hù
                if (scaryEntity != null)
                {
                    AudioSource ghostAudio = scaryEntity.GetComponent<AudioSource>();
                    if (ghostAudio != null) ghostAudio.Stop();
                }

                StartCoroutine(GameOverRoutine());
                yield break; // Kết thúc SurviveRoutine ngay lập tức
            }

            yield return null;
        }

        // Con ma bỏ đi (biến mất)
        if (scaryEntity != null) scaryEntity.SetActive(false);

        // Dừng tiếng ma (nếu còn)
        if (scaryEntity != null)
        {
            AudioSource ghostAudio = scaryEntity.GetComponent<AudioSource>();
            if (ghostAudio != null) ghostAudio.Stop();
        }

        // --- Đợi 5 giây cho hoàn toàn yên ắng ---
        yield return new WaitForSeconds(5f);

        // --- 4. Thoại nội tâm sau khi thoát nạn ---
        if (DialogManager.Instance != null && afterGhostDialog != null && afterGhostDialog.Count > 0)
        {
            bool isAfterDone = false;
            // Thoại của nhân vật chính -> Phải bấm chuột để đọc (tốc độ bình thường mặc định)
            DialogManager.Instance.StartDialogSequence(afterGhostDialog, (result) => { isAfterDone = true; });
            while (!isAfterDone) yield return null;
        }

        // Chuyển sang sự kiện tiếp theo
        CompleteStep();
        Instance = null;
    }

    IEnumerator GameOverRoutine()
    {
        // Ép buộc dừng bất kỳ hội thoại nào đang chạy (Ví dụ: ma đang chửi mà bị Game Over)
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive)
        {
            DialogManager.Instance.ForceStopDialog();
        }

        // Tắt ngay lập tức mọi tiếng khóc/tiếng ồn của ma nữ
        if (scaryEntity != null)
        {
            AudioSource[] allAudio = scaryEntity.GetComponentsInChildren<AudioSource>();
            foreach (var a in allAudio) a.Stop();
        }

        // Ẩn nhiệm vụ đi nếu game over
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.HideObjective();
        }

        Debug.Log("[PhoneEvent] GAME OVER! Đóng cửa quá chậm!");
        FirstPersonController.CanMove = false;

        Transform player = FindObjectOfType<FirstPersonController>().transform;

        // Jumpscare: Ma bay thẳng vào mặt người chơi
        if (scaryEntity != null && player != null)
        {
            Vector3 startPos = scaryEntity.transform.position;
            Vector3 endPos = player.position + player.forward * 1f; // Điểm ngay trước mặt
            Camera cam = player.GetComponentInChildren<Camera>();

            // Phát âm thanh ré lên ngay lúc bắt đầu lao
            if (jumpscareSound != null) jumpscareAudio.PlayOneShot(jumpscareSound);

            float dashDuration = 0.4f; // Mất 0.4 giây để bay tới
            float time = 0;
            
            while (time < dashDuration)
            {
                time += Time.deltaTime;
                float t = time / dashDuration;

                // Di chuyển ma
                scaryEntity.transform.position = Vector3.Lerp(startPos, endPos, t);
                scaryEntity.transform.LookAt(player);

                // Ép camera người chơi luôn ngửa lên trên 45 độ và hướng về phía con ma
                if (cam != null)
                {
                    Vector3 dirToGhost = (scaryEntity.transform.position - player.position).normalized;
                    dirToGhost.y = 0; // Giữ hướng ngang
                    
                    if (dirToGhost != Vector3.zero)
                    {
                        // Xoay về hướng ma, sau đó ngửa lên 35 độ (trục X âm)
                        cam.transform.rotation = Quaternion.LookRotation(dirToGhost) * Quaternion.Euler(-35f, 0f, 0f);
                    }
                }

                yield return null;
            }
            
            scaryEntity.transform.position = endPos;
        }
        else
        {
            if (jumpscareSound != null) jumpscareAudio.PlayOneShot(jumpscareSound);
        }

        // Mờ đen màn hình Game Over
        if (gameOverCanvas != null)
        {
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                gameOverCanvas.alpha = t / 0.5f;
                yield return null;
            }
            gameOverCanvas.alpha = 1f;
        }

        // Cho người chơi xem màn hình Game Over ít nhất 1.5 giây (tránh việc họ đang bấm hoảng loạn bị tua qua lố)
        yield return new WaitForSeconds(1.5f);

        // Chờ người chơi bấm SPACE để chơi lại
        bool clicked = false;
        while (!clicked)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame) clicked = true;
            yield return null;
        }

        // RESET ĐỂ CHƠI LẠI SỰ KIỆN NÀY (Checkpoint)
        Debug.Log("[PhoneEvent] Restarting Checkpoint...");
        
        // Trả lại camera về thẳng đứng
        if (player != null)
        {
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null) cam.transform.localRotation = Quaternion.identity;
        }

        // Tắt con ma đi NGAY LẬP TỨC TRƯỚC KHI MỞ MẮT
        if (scaryEntity != null) scaryEntity.SetActive(false);

        // Chạy lại bước này từ đầu (chuông lại reo)
        // Việc này sẽ gọi StartCoroutine(PhoneCallRoutine())
        StartStep();

        // Bây giờ mới Mờ sáng lại (Mở mắt ra thấy cảnh vật bình thường, ma đã biến mất)
        if (gameOverCanvas != null)
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime;
                gameOverCanvas.alpha = 1f - (t / 1f);
                yield return null;
            }
            gameOverCanvas.alpha = 0f;
        }
    }
}
