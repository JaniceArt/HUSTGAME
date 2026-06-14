using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InteractableObject))]
public class PaperAirplaneStep : SequenceStep
{
    public Animator airplaneAnimator;
    
    [Tooltip("Tên animation lúc máy bay bay vào")]
    public string flyInAnimation = "FlyIn";

    [Header("Âm thanh")]
    [Tooltip("Âm thanh ném giấy bay vào")]
    public AudioClip throwSound; 
    [Range(0f, 5f)] public float throwSoundVolume = 2f; // Tăng âm lượng lên

    [Tooltip("Âm thanh lúc xem giấy (jumpscare)")]
    public AudioClip jumpscareSound; 
    [Range(0f, 5f)] public float jumpscareSoundVolume = 1f;

    [Header("UI Xem giấy (Jumpscare)")]
    [Tooltip("Hình ảnh nội dung tờ giấy")]
    public Sprite paperImageSprite; 
    
    private AudioSource audioSource;
    private GameObject paperUI;
    private bool isViewing = false;
    private bool hasViewed = false;

    public bool HasViewed => hasViewed;

    void Start()
    {
        InteractableObject interactObj = GetComponent<InteractableObject>();
        if (interactObj != null)
        {
            interactObj.type = InteractableType.PaperAirplane;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Âm thanh 2D để luôn nghe to rõ bất kể khoảng cách
    }

    void OnEnable()
    {
        // Khi tờ giấy được bật lên (bởi AutoEventStep), tự động phát âm thanh ném
        if (audioSource != null && throwSound != null)
        {
            audioSource.PlayOneShot(throwSound, throwSoundVolume);
        }
    }

    public override void StartStep()
    {
        gameObject.SetActive(true);
        if (airplaneAnimator != null)
        {
            airplaneAnimator.Play(flyInAnimation);
        }
        
        if (throwSound != null && gameObject.activeInHierarchy)
        {
            // Chỉ phát trong StartStep nếu nó không tự phát ở OnEnable (để tránh phát 2 lần)
            // nhưng thực ra AutoEventStep tự bật nên OnEnable sẽ chạy. Ta có thể an toàn bỏ qua hoặc giữ lại
            // Ở đây tôi cứ giữ lại nhưng thêm volume:
            // audioSource.PlayOneShot(throwSound, throwSoundVolume);
        }

        Debug.Log("[Sequence] Máy bay giấy bay vào! Bấm E để vứt rác, V để xem.");
    }

    public void ViewPaper()
    {
        if (isViewing || hasViewed) return;
        isViewing = true;
        hasViewed = true;

        if (jumpscareSound != null)
        {
            audioSource.PlayOneShot(jumpscareSound, jumpscareSoundVolume);
        }

        StartCoroutine(ShowJumpscareUI());
    }

    private IEnumerator ShowJumpscareUI()
    {
        // Tìm Canvas từ InteractionSystem (promptPanel)
        InteractionSystem isys = FindObjectOfType<InteractionSystem>();
        Canvas canvas = isys != null ? isys.promptPanel.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();

        if (canvas != null)
        {
            paperUI = new GameObject("JumpscarePaperUI");
            paperUI.transform.SetParent(canvas.transform, false);
            
            Image img = paperUI.AddComponent<Image>();
            if (paperImageSprite != null)
                img.sprite = paperImageSprite;
            else
                img.color = Color.white; // Placeholder nếu không gán ảnh
            
            RectTransform rt = paperUI.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(800, 800);
            
            // Hiệu ứng Jumpscare (phóng to nhanh)
            float t = 0;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(0.1f, 1.2f, t / 0.15f);
                rt.localScale = new Vector3(scale, scale, 1);
                yield return null;
            }
            
            // Lắc nhẹ (Screen shake)
            Vector3 originalPos = rt.anchoredPosition;
            float shakeTime = 0.35f;
            while (shakeTime > 0)
            {
                shakeTime -= Time.deltaTime;
                rt.anchoredPosition = originalPos + (Vector3)(Random.insideUnitCircle * 25f);
                yield return null;
            }
            rt.anchoredPosition = originalPos;
            rt.localScale = Vector3.one;

            // Đợi 2 giây
            yield return new WaitForSeconds(2.0f);
            
            Destroy(paperUI);
            isViewing = false;

            // Hiện câu thoại của nhân vật chính sau khi xem xong
            if (DialogManager.Instance != null)
            {
                List<DialogNode> nodes = new List<DialogNode>();
                DialogNode node = new DialogNode();
                node.sentence = "Đm đứa nào ném đấy, giật cả mình";
                node.hasChoices = false;
                nodes.Add(node);
                DialogManager.Instance.StartDialogSequence(nodes, (result) => {
                    CompleteStep(); // Chuyển sang sự kiện/khách tiếp theo sau khi lẩm bẩm xong!
                });
            }
            else
            {
                CompleteStep();
            }
        }
        else
        {
            isViewing = false;
            CompleteStep();
        }
    }

    /// <summary>
    /// Gọi khi người chơi bấm E vào máy bay giấy.
    /// </summary>
    public void ThrowAway()
    {
        if (isViewing || !hasViewed) return; // Không cho vứt nếu đang xem hoặc chưa xem
        Debug.Log("[Sequence] Đã vứt máy bay giấy!");
        gameObject.SetActive(false);
        // Không gọi CompleteStep ở đây nữa, vì đã gọi lúc xem xong rồi
    }
}
