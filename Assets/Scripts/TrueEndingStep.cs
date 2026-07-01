using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrueEndingStep : SequenceStep
{
    [Header("=== GIAI ĐOẠN 1: BƯỚC VÀO QUÁN ===")]
    [Tooltip("Kéo Empty GameObject chứa VỊ TRÍ và HƯỚNG NHÌN bạn muốn người chơi dịch chuyển tới ngay lúc bắt đầu")]
    public Transform playerStartPoint;
    
    [Tooltip("Thoại lầm bầm khi vừa quay về (VD: Phù, cuối cùng cũng cắt đuôi được nó...)")]
    public List<DialogNode> monologue1;

    [Tooltip("Bức tường tàng hình (Collider) chắn cửa để nhốt người chơi trong phòng")]
    public GameObject doorBlocker;

    [Header("=== GIAI ĐOẠN 2: CHỜ NGƯỜI CHƠI TỰ NHÌN VÀO CHỮ MÁU ===")]
    [Tooltip("Kéo vật thể chứa dòng chữ máu trên tường vào đây. Người chơi phải TỰ Xoay mặt nhìn vào đây thì mới hiện thoại.")]
    public Transform bloodyTextTarget;
    [Tooltip("Kéo riêng hình ảnh/chữ máu thực tế vào đây để ẩn/hiện (Nếu để trống sẽ tự động dùng Target ở trên)")]
    public GameObject bloodyTextGraphic;
    [Tooltip("Âm thanh kinh dị giật mình khi VỪA NHÌN THẤY chữ máu")]
    public AudioClip horrorStingSound;
    [Tooltip("Thoại khi nhìn thấy chữ máu (VD: Cái gì thế này... Mình nhớ nãy đâu có...)")]
    public List<DialogNode> monologue2;

    [Header("=== GIAI ĐOẠN 3: ÉP BẺ CỔ NHÌN RA CỬA ===")]
    [Tooltip("Tạo 1 Empty GameObject ngoài cửa, kéo vào đây làm mục tiêu để bẻ cổ nhân vật nhìn ra")]
    public Transform doorTarget;
    [Tooltip("Thời gian bẻ cổ (giây). Càng chậm càng sợ.")]
    public float forceRotateDuration = 1.5f;
    [Tooltip("Âm thanh sập nguồn tắt đèn ngay khi vừa quay lưng lại")]
    public AudioClip powerOffSound;

    [Header("=== GIAI ĐOẠN 4: MA NỮ ÁP SÁT ===")]
    [Tooltip("Nhóm ma nữ đứng ngoài cửa (Tạo 1 cục chứa nhiều con ma kéo vào đây)")]
    public GameObject femaleGhostsGroup;
    [Tooltip("Danh sách đèn trong phòng để làm mờ đi")]
    public List<Light> roomLights;
    [Tooltip("Âm thanh dọa ma cuối cùng khi nguyên dàn ma hiện ra")]
    public AudioClip finalJumpscareSound;

    [Header("=== KẾT THÚC ===")]
    [Tooltip("Thời gian đứng hình sợ hãi trước khi kết thúc game (chuyển cảnh/màn hình đen)")]
    public float blackScreenDelay = 4f;
    [Tooltip("Font chữ rùng rợn cho màn hình True Ending (Để trống sẽ lấy theo font phụ đề)")]
    public TMPro.TMP_FontAsset endTextFont;

    private FirstPersonController playerController;
    private AudioSource audioSource;
    private bool isStepActive = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public override void StartStep()
    {
        isStepActive = true;
        // Bật lại Player (vì Cutscene trước đó có thể đã tắt Player đi để chiếu phim)
        playerController = FindObjectOfType<FirstPersonController>(true);
        if (playerController != null && !playerController.gameObject.activeSelf)
        {
            playerController.gameObject.SetActive(true);
        }
        
        // Giấu dàn ma nữ đi trước
        if (femaleGhostsGroup != null) femaleGhostsGroup.SetActive(false);
        
        // Giấu chữ máu đi trước (để lúc đầu quay mặt vào không thấy)
        if (bloodyTextGraphic != null) bloodyTextGraphic.SetActive(false);
        else if (bloodyTextTarget != null) bloodyTextTarget.gameObject.SetActive(false);

        // ĐÓNG SẬP CỬA LẠI (Hoặc bật tường tàng hình lên để nhốt người chơi)
        if (doorBlocker != null) doorBlocker.SetActive(true);

        StartCoroutine(EndingSequence());
    }

    IEnumerator EndingSequence()
    {
        // ================= GIAI ĐOẠN 1: DỊCH CHUYỂN, KHÓA GÓC NHÌN & ĐỌC THOẠI =================
        FirstPersonController.CanMove = false;
        Camera cam = Camera.main;
        if (cam == null && playerController != null) cam = playerController.GetComponentInChildren<Camera>();

        // 1. Dịch chuyển và khóa góc nhìn theo vị trí cho sẵn
        if (playerStartPoint != null && playerController != null && cam != null)
        {
            // Dịch chuyển cơ thể
            playerController.transform.position = playerStartPoint.position;
            
            // Ép cả góc xoay Y của cơ thể Player để đồng bộ
            Vector3 euler = playerStartPoint.rotation.eulerAngles;
            playerController.transform.rotation = Quaternion.Euler(0, euler.y, 0);

            // QUAN TRỌNG: Đồng bộ lại biến xRotation của chuột để khi mở khóa không bị giật về góc cũ!
            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;
            playerController.xRotation = pitch;
            
            // Ép Camera local y hệt như những gì FirstPersonController sẽ làm khi mở khóa
            cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // CHO HIỆN CHỮ MÁU LÊN TƯỜNG SAU LƯNG NGAY LẬP TỨC!
        // Để lỡ người chơi có xoay chuột nhanh quá thì chữ đã ở đó sẵn rồi.
        if (bloodyTextGraphic != null) bloodyTextGraphic.SetActive(true);
        if (bloodyTextTarget != null) bloodyTextTarget.gameObject.SetActive(true);

        // 2. Đọc thoại 1 (Khóa di chuyển, bắt người chơi phải tự bấm phím để qua)
        bool isDialogDone = false;
        if (DialogManager.Instance != null && monologue1 != null && monologue1.Count > 0)
        {
            DialogManager.Instance.StartDialogSequence(monologue1, (result) => { isDialogDone = true; });
            while (!isDialogDone) yield return null;
        }
        else if (bloodyTextTarget != null) bloodyTextTarget.gameObject.SetActive(true);

        // ================= GIAI ĐOẠN 2: THẢ TỰ DO, ĐỢI NHÌN CHỮ MÁU =================
        // 3. Mở khóa cho người chơi di chuyển
        FirstPersonController.CanMove = true;

        // 4. Chờ người chơi TỰ quay mặt vào tường chữ
        bool isLookingAtText = false;
        while (!isLookingAtText)
        {
            if (cam != null && bloodyTextTarget != null)
            {
                Vector3 dirToText = (bloodyTextTarget.position - cam.transform.position).normalized;
                float angle = Vector3.Angle(cam.transform.forward, dirToText);
                
                // Nếu góc nhìn hướng về phía chữ máu (dưới 40 độ) -> Đã nhìn thấy
                if (angle < 40f)
                {
                    isLookingAtText = true;
                }
            }
            yield return null;
        }

        // KHÓA DI CHUYỂN ĐỂ ÉP NHÌN VÀO CHỮ MÁU
        FirstPersonController.CanMove = false;

        // ÉP CAMERA NHÌN THẲNG VÀO CHỮ MÁU
        if (cam != null && bloodyTextTarget != null)
        {
            Quaternion startRot = cam.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(bloodyTextTarget.position - cam.transform.position);
            
            float elapsed = 0f;
            while (elapsed < 0.5f) // Xoay mượt trong 0.5 giây
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.5f;
                cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
            cam.transform.rotation = targetRot;

            // Đồng bộ lại xRotation cho controller (dù không mở khóa nhưng cho chắc cú)
            Vector3 finalEuler = cam.transform.rotation.eulerAngles;
            float pitch = finalEuler.x;
            if (pitch > 180f) pitch -= 360f;
            playerController.xRotation = pitch;
        }

        // Vừa nhìn thấy là phát tiếng giật mình
        if (horrorStingSound != null) audioSource.PlayOneShot(horrorStingSound);

        // CHO MA NỮ XUẤT HIỆN SẴN Ở CỬA NGAY TỪ BÂY GIỜ (Dù người chơi đang nhìn tường chữ)
        if (femaleGhostsGroup != null) femaleGhostsGroup.SetActive(true);

        // TẮT CHỨC NĂNG TỰ ĐỘNG MỞ KHÓA CỦA DIALOG MANAGER VÌ MÌNH SẼ KHÓA TỚI CUỐI GAME
        if (DialogManager.Instance != null) DialogManager.Instance.UnlockPlayerOnComplete = false;

        isDialogDone = false;
        if (DialogManager.Instance != null && monologue2 != null && monologue2.Count > 0)
        {
            DialogManager.Instance.StartAutoDialogSequence(monologue2, (result) => { isDialogDone = true; }, 1f, 3f);
            while (!isDialogDone) yield return null;
        }

        // ================= KỊCH TÍNH CUỐI CÙNG (ÉP BẺ CỔ RA CỬA) =================
        // KHÔNG THẢ TỰ DO NỮA, ĐỌC THOẠI XONG LÀ SẬP NGUỒN VÀ BẺ CỔ LUÔN!

        // VỪA QUAY LƯNG LÀ TẮT ĐÈN (CHỈ TẮT NHỮNG ĐÈN TRONG DANH SÁCH BẠN KÉO VÀO)
        if (powerOffSound != null) audioSource.PlayOneShot(powerOffSound);
        
        if (roomLights != null)
        {
            foreach (var l in roomLights) 
            {
                if (l != null) l.intensity = 0f;
            }
        }

        if (cam != null && doorTarget != null)
        {
            Quaternion startRot = cam.transform.rotation;
            // Tính toán góc nhìn hướng thẳng ra ngoài cửa
            Quaternion targetRot = Quaternion.LookRotation(doorTarget.position - cam.transform.position);
            
            float elapsed = 0f;
            
            // Từ từ xoay camera trong bóng tối
            while (elapsed < forceRotateDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / forceRotateDuration;
                
                // Càng về sau xoay càng nhanh (hàm SmoothStep)
                float curve = t * t * (3f - 2f * t);

                // Ép xoay camera
                cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, curve);
                yield return null;
            }
            // Đảm bảo xoay đúng đích
            cam.transform.rotation = targetRot;
        }

        // 5. Quay ra cửa thì thót tim (Bầy ma đã đứng chờ sẵn vì bật từ bước trước rồi)
        if (finalJumpscareSound != null) audioSource.PlayOneShot(finalJumpscareSound);

        // Bật lại chức năng mở khóa của DialogManager cho các màn sau (nếu có)
        if (DialogManager.Instance != null) DialogManager.Instance.UnlockPlayerOnComplete = true;

        // 6. Đứng hình đối mặt với dàn ma nữ X giây (chạy nhạc)
        yield return new WaitForSeconds(blackScreenDelay);

        // 7. FADE ĐEN MÀN HÌNH VÀ HIỆN CHỮ END
        GameObject canvasObj = new GameObject("EndGameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject bgObj = new GameObject("BlackBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0); // Bắt đầu trong suốt
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject textObj = new GameObject("TheEndText");
        textObj.transform.SetParent(canvasObj.transform, false);
        TMPro.TextMeshProUGUI endText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        
        // Cài đặt Font chữ
        if (endTextFont != null) endText.font = endTextFont;
        else if (DialogManager.Instance != null && DialogManager.Instance.dialogText != null) endText.font = DialogManager.Instance.dialogText.font;

        endText.text = "BAD ENDING";
        endText.color = new Color(0.8f, 0, 0, 0); // Đỏ thẫm, bắt đầu trong suốt
        endText.fontSize = 100;
        endText.fontStyle = TMPro.FontStyles.Bold;
        endText.alignment = TMPro.TextAlignmentOptions.Center;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // Tối dần màn hình trong 2.5 giây
        float fadeElapsed = 0f;
        while (fadeElapsed < 2.5f)
        {
            fadeElapsed += Time.deltaTime;
            bgImage.color = new Color(0, 0, 0, Mathf.Clamp01(fadeElapsed / 2.5f));
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        // Hiện dần chữ TRUE ENDING trong 3 giây
        fadeElapsed = 0f;
        while (fadeElapsed < 3f)
        {
            fadeElapsed += Time.deltaTime;
            endText.color = new Color(0.8f, 0, 0, Mathf.Clamp01(fadeElapsed / 3f));
            yield return null;
        }

        yield return new WaitForSeconds(4f); // Đứng nhìn chữ End 4 giây

        // Kết thúc
        Debug.Log("GAME OVER - BAD ENDING");
        CompleteStep();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
