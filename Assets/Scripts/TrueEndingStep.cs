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

    [Header("=== GIAI ĐOẠN 2: CHỜ NGƯỜI CHƠI TỰ NHÌN VÀO CHỮ MÁU ===")]
    [Tooltip("Kéo vật thể chứa dòng chữ máu trên tường vào đây. Người chơi phải TỰ Xoay mặt nhìn vào đây thì mới hiện thoại.")]
    public Transform bloodyTextTarget;
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
        if (bloodyTextTarget != null) bloodyTextTarget.gameObject.SetActive(false);

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
            
            // Ép góc nhìn Camera (Rot X và Y)
            cam.transform.rotation = playerStartPoint.rotation;
            
            // Ép cả góc xoay Y của cơ thể Player để đồng bộ
            Vector3 euler = playerStartPoint.rotation.eulerAngles;
            playerController.transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }

        // 2. Đọc thoại 1 (Khóa di chuyển)
        bool isDialogDone = false;
        if (DialogManager.Instance != null && monologue1 != null && monologue1.Count > 0)
        {
            DialogManager.Instance.StartDialogSequence(monologue1, (result) => { isDialogDone = true; });
            while (!isDialogDone) yield return null;
        }

        // Vừa đọc thoại xong là CHO HIỆN CHỮ MÁU LÊN TƯỜNG SAU LƯNG!
        if (bloodyTextTarget != null) bloodyTextTarget.gameObject.SetActive(true);

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

        // Vừa nhìn thấy là phát tiếng giật mình
        if (horrorStingSound != null) audioSource.PlayOneShot(horrorStingSound);

        // Khóa lại 1 chút để đọc thoại
        FirstPersonController.CanMove = false;
        isDialogDone = false;
        if (DialogManager.Instance != null && monologue2 != null && monologue2.Count > 0)
        {
            DialogManager.Instance.StartDialogSequence(monologue2, (result) => { isDialogDone = true; });
            while (!isDialogDone) yield return null;
        }


        // ================= GIAI ĐOẠN 3: ĐỢI QUAY NGƯỢC LẠI =================
        // 5. Thả tự do lần nữa
        FirstPersonController.CanMove = true;

        // 6. Chờ người chơi QUAY LƯNG LẠI với bức tường chữ (Quay ngược lại)
        bool hasTurnedBack = false;
        while (!hasTurnedBack)
        {
            if (cam != null && bloodyTextTarget != null)
            {
                Vector3 dirToText = (bloodyTextTarget.position - cam.transform.position).normalized;
                float angle = Vector3.Angle(cam.transform.forward, dirToText);
                
                // Nếu góc nhìn lệch khỏi chữ máu hơn 100 độ (nghĩa là đã quay lưng / quay đi chỗ khác)
                if (angle > 100f)
                {
                    hasTurnedBack = true;
                }
            }
            yield return null;
        }

        // ================= KỊCH TÍNH CUỐI CÙNG (ÉP BẺ CỔ RA CỬA) =================
        // KHÓA DI CHUYỂN NGAY LẬP TỨC ĐỂ DỌA
        FirstPersonController.CanMove = false;

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

        // 5. Quay ra cửa thì thót tim: DÀN MA NỮ XUẤT HIỆN Ở CỬA
        if (femaleGhostsGroup != null) femaleGhostsGroup.SetActive(true);
        if (finalJumpscareSound != null) audioSource.PlayOneShot(finalJumpscareSound);

        // 6. Đứng hình đối mặt với dàn ma nữ 4 giây trước khi hết game
        yield return new WaitForSeconds(blackScreenDelay);

        // TODO: Chuyển cảnh sang Credit hoặc Đen màn hình (Game Over)
        Debug.Log("GAME OVER - TRUE ENDING");
        CompleteStep();
    }
}
