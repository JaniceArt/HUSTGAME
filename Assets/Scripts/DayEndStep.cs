using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bước kết thúc ngày:
/// Chờ người chơi đóng cửa -> Màn hình đen -> Hiển thị "DAY X" -> Dịch chuyển người chơi -> Sáng màn hình -> Xong bước.
/// </summary>
public class DayEndStep : SequenceStep
{
    [Header("=== KẾT THÚC NGÀY ===")]
    [Tooltip("Số thứ tự của ngày tiếp theo (VD: 2)")]
    public int nextDayNumber = 2;

    [Tooltip("Chữ hiển thị trên màn hình đen (Text thường)")]
    public Text dayTextUI;

    [Tooltip("Chữ hiển thị trên màn hình đen (TextMeshPro)")]
    public TextMeshProUGUI dayTextTMP;

    [Tooltip("Hiệu ứng mờ đen màn hình (Canvas Group)")]
    public CanvasGroup fadeCanvas;
    
    [Tooltip("Thời gian mờ đen (giây)")]
    public float fadeDuration = 1.5f;

    [Tooltip("Thời gian chờ màn hình đen có chữ DAY X (giây)")]
    public float blackScreenDuration = 2.0f;

    [Tooltip("Điểm người chơi sẽ bị dịch chuyển tới để bắt đầu ngày mới (VD: Điểm StartDay2)")]
    public Transform nextDaySpawnPoint;

    [Header("=== ÂM THANH ===")]
    [Tooltip("Âm thanh phát lúc chuyển ngày (Tùy chọn)")]
    public AudioClip transitionSound;
    private AudioSource audioSource;

    private bool isWaitingForDoor = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public override void StartStep()
    {
        isWaitingForDoor = true;
        
        // Báo nhiệm vụ đóng cửa tiệm
        if (ObjectiveManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(objectiveTitle))
                ObjectiveManager.Instance.ShowObjective(objectiveTitle);
            else
                ObjectiveManager.Instance.ShowObjective("Đóng cửa tiệm và về nhà");
        }

        Debug.Log($"[DayEndStep] Đang chờ người chơi đóng cửa để kết thúc ngày...");
    }

    /// <summary>
    /// Gọi bởi SlidingDoor khi cửa bị đóng và SequenceManager đang ở bước này.
    /// </summary>
    public void OnDoorClosed()
    {
        if (!isWaitingForDoor) return;
        isWaitingForDoor = false;
        
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.CompleteObjective();
            ObjectiveManager.Instance.HideObjective(); // Ẩn luôn cho đỡ chướng mắt lúc chuyển cảnh
        }

        StartCoroutine(TransitionToNextDay());
    }

    IEnumerator TransitionToNextDay()
    {
        // Khóa người chơi
        FirstPersonController.CanMove = false; 

        if (audioSource != null && transitionSound != null)
            audioSource.PlayOneShot(transitionSound);

        // 1. Mờ đen dần
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

        // Hiện chữ Day X
        if (dayTextUI != null)
        {
            dayTextUI.gameObject.SetActive(true);
            dayTextUI.text = "DAY " + nextDayNumber;
        }
        if (dayTextTMP != null)
        {
            dayTextTMP.gameObject.SetActive(true);
            dayTextTMP.text = "DAY " + nextDayNumber;
        }

        // 2. Chờ màn hình đen
        yield return new WaitForSeconds(blackScreenDuration);

        // Tắt chữ
        if (dayTextUI != null) dayTextUI.gameObject.SetActive(false);
        if (dayTextTMP != null) dayTextTMP.gameObject.SetActive(false);

        // 3. Dịch chuyển
        if (nextDaySpawnPoint != null)
        {
            FirstPersonController player = FindObjectOfType<FirstPersonController>();
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = nextDaySpawnPoint.position;
                player.transform.rotation = nextDaySpawnPoint.rotation;

                if (cc != null) cc.enabled = true;
            }
        }

        // 4. Sáng màn hình dần
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

        // Mở khóa người chơi
        FirstPersonController.CanMove = true; 

        // 5. Kết thúc bước, chuyển sang bước tiếp theo trong SequenceManager
        CompleteStep();
    }
}
