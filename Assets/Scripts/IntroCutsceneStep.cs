using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cutscene đầu game: Khóa người chơi -> Fade In "Ngày 1" -> Đọc thoại -> Fade Out -> Dịch chuyển -> Bắt đầu bán hàng.
/// </summary>
public class IntroCutsceneStep : SequenceStep
{
    [Header("=== CẤU HÌNH CUTSCENE ĐẦU GAME ===")]
    [Tooltip("Danh sách các câu thoại của nhân vật chính lúc vừa vào game (ấn dấu + để thêm nhiều câu)")]
    [TextArea(2, 4)]
    public List<string> introDialogs = new List<string> { "Hôm nay lại một ngày làm việc mệt mỏi..." };

    [Tooltip("Thời gian màn hình đen hiện chữ Ngày 1 trước khi sáng lên (giây)")]
    public float delayBeforeDialog = 2.0f;

    [Header("=== CHUYỂN CẢNH ===")]
    [Tooltip("Điểm người chơi sẽ bị dịch chuyển tới để bắt đầu bán hàng")]
    public Transform gameplaySpawnPoint;

    [Tooltip("Hiệu ứng mờ đen màn hình (Tạo Canvas -> Panel đen -> Thêm CanvasGroup, và kéo vào đây)")]
    public CanvasGroup fadeCanvas;
    
    [Tooltip("Thời gian mờ dần/sáng dần (giây)")]
    public float fadeDuration = 1.5f;

    [Header("UI Tùy chọn")]
    [Tooltip("Kéo các UI cần GIẤU ĐI lúc Cutscene đang chạy vào đây (VD: HintCanvas)")]
    public GameObject[] objectsToHide;

    [Header("=== ÂM THANH ===")]
    [Tooltip("Âm thanh phát ra lúc mới vào game (VD: Tiếng chim hót, tiếng thở dài...)")]
    public AudioClip introSound;
    
    [Tooltip("Âm thanh lúc màn hình chớp đen để chuyển cảnh (VD: Tiếng whoosh, tiếng chớp mắt...)")]
    public AudioClip transitionSound;

    private AudioSource audioSource;
    private GameObject hiddenHintCanvas;

    private void Awake()
    {
        // Tự động gắn thêm cái loa (AudioSource) để phát nhạc
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Nhạc 2D để nghe rõ ràng không bị nhỏ
    }

    public override void StartStep()
    {
        // Khoá di chuyển và KHÓA CẢ XOAY CAMERA
        FirstPersonController.CanMove = false; 

        // Đảm bảo triệt tiêu lực di chuyển (nếu có)
        FirstPersonController player = FindObjectOfType<FirstPersonController>();
        if (player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            // Ép hướng nhìn về phía của gameplaySpawnPoint để không bị úp mặt vào tường lúc Cutscene
            if (gameplaySpawnPoint != null)
            {
                Vector3 spawnEuler = gameplaySpawnPoint.rotation.eulerAngles;
                player.transform.rotation = Quaternion.Euler(0, spawnEuler.y, 0);
                player.xRotation = 0f; // Đưa góc cúi/ngẩng về thẳng đứng
                
                Camera cam = player.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.transform.localRotation = Quaternion.identity;
                }
            }
        }

        // Bắt đầu chuỗi kịch bản
        StartCoroutine(CutsceneRoutine());
    }

    IEnumerator CutsceneRoutine()
    {
        // Giấu các UI không mong muốn (VD: HintCanvas được kéo vào)
        if (objectsToHide != null)
        {
            foreach (GameObject obj in objectsToHide)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // Tự động tìm và giấu luôn HintCanvas cho chắc cú (khỏi cần kéo thả)
        hiddenHintCanvas = GameObject.Find("HintCanvas");
        if (hiddenHintCanvas != null)
        {
            hiddenHintCanvas.SetActive(false);
        }

        // Đảm bảo màn hình hoàn toàn sáng (không đen) lúc mới vào game
        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
        }

        // Phát âm thanh bắt đầu game (nếu có)
        if (audioSource != null && introSound != null)
        {
            audioSource.PlayOneShot(introSound);
        }

        // Đợi một chút cho scene load xong trước khi hiện thoại
        yield return new WaitForSeconds(0.5f);

        // Hiện thoại lên màn hình
        if (DialogManager.Instance != null && introDialogs != null && introDialogs.Count > 0)
        {
            List<DialogNode> nodes = new List<DialogNode>();
            foreach (string sentence in introDialogs)
            {
                if (!string.IsNullOrEmpty(sentence))
                {
                    nodes.Add(new DialogNode { sentence = sentence, hasChoices = false });
                }
            }

            if (nodes.Count > 0)
            {
                // Bật Dialog
                DialogManager.Instance.StartDialogSequence(nodes, (result) => 
                {
                    // DialogManager tự động mở khoá camera khi xong, nên mình phải ép khoá lại ngay lập tức
                    FirstPersonController.CanMove = false; 
                    
                    // Người chơi bấm Space đọc xong hết thoại -> Gọi hàm dịch chuyển (lúc này mới hiện chữ Ngày 1)
                    StartCoroutine(TransitionToGameplay());
                });
            }
            else
            {
                StartCoroutine(TransitionToGameplay());
            }
        }
        else
        {
            // Nếu không có thoại thì bỏ qua luôn
            StartCoroutine(TransitionToGameplay());
        }
    }

    IEnumerator TransitionToGameplay()
    {
        // Phát âm thanh lúc mờ đen chuyển cảnh (nếu có)
        if (audioSource != null && transitionSound != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }

        // 1. Đọc thoại xong -> Màn hình từ từ mờ đen lại (Hiện chữ Ngày 1)
        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvas.alpha = t / fadeDuration;
                yield return null;
            }
            fadeCanvas.alpha = 1f;
        }

        // 2. Chờ màn hình đen một lúc để người chơi đọc chữ Ngày 1
        yield return new WaitForSeconds(delayBeforeDialog);

        // 3. Dịch chuyển người chơi đến chỗ bán hàng (Trong lúc màn hình đang đen thui)
        if (gameplaySpawnPoint != null)
        {
            FirstPersonController player = FindObjectOfType<FirstPersonController>();
            if (player != null)
            {
                // Tắt Component CharacterController (nếu có) trước khi dịch chuyển
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = gameplaySpawnPoint.position;
                Vector3 euler = gameplaySpawnPoint.rotation.eulerAngles;
                player.transform.rotation = Quaternion.Euler(0, euler.y, 0);

                if (cc != null) cc.enabled = true;
            }
        }

        // Đợi một chút cho cảnh load kịp
        yield return new WaitForSeconds(0.5f);

        // 6. Sáng màn hình trở lại (Fade In) để bắt đầu chơi
        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvas.alpha = 1f - (t / fadeDuration);
                yield return null;
            }
            fadeCanvas.alpha = 0f;
        }

        // Bật lại các UI đã giấu đi
        if (objectsToHide != null)
        {
            foreach (GameObject obj in objectsToHide)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (hiddenHintCanvas != null)
        {
            hiddenHintCanvas.SetActive(true);
        }

        // Mở khoá cho phép người chơi di chuyển và xoay chuột bình thường
        FirstPersonController.CanMove = true; 

        // Tự động kết thúc Cutscene để nhường quyền cho WaitForDoorOpenStep
        CompleteStep();
    }
}
