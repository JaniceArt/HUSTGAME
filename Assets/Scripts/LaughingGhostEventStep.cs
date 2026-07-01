using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sự kiện ma cười. Ma đi từ điểm A đến điểm B, cười. 
/// Người chơi nhìn thấy -> Thoại -> Ma để lại giấy và chạy rất nhanh về điểm A -> Đọc giấy -> Thoại.
/// </summary>
public class LaughingGhostEventStep : SequenceStep
{
    [Header("=== NHÂN VẬT (MA) ===")]
    [Tooltip("Object nhân vật ma đang đứng cười")]
    public GameObject ghostNPC;
    
    [Tooltip("Animator của ma")]
    public Animator ghostAnimator;
    
    [Tooltip("Tên tham số bool để ma đi bộ")]
    public string isWalkingParam = "isWalking";

    [Header("=== ĐƯỜNG ĐI ===")]
    [Tooltip("Điểm xuất phát (Điểm A)")]
    public Transform startPoint;

    [Tooltip("Điểm đứng cười (Điểm B)")]
    public Transform endPoint;

    [Tooltip("Tốc độ đi bộ ban đầu")]
    public float normalSpeed = 2f;

    [Tooltip("Tốc độ chạy chối chết khi bị phát hiện")]
    public float escapeSpeed = 10f;

    [Header("=== KHOẢNG CÁCH KÍCH HOẠT ===")]
    [Tooltip("Khoảng cách (mét) người chơi đến gần ma thì ma mới giật mình bỏ chạy")]
    public float triggerDistance = 5f;

    [Header("=== KỊCH BẢN THOẠI ===")]
    [Tooltip("Thoại của người chơi khi nhìn thấy ma")]
    public List<DialogNode> firstMonologue;

    [Header("=== TỜ GIẤY ĐỂ LẠI ===")]
    [Tooltip("Object tờ giấy nằm dưới đất (ẩn lúc đầu)")]
    public GameObject paperObject;
    
    [Tooltip("Hình ảnh nội dung tờ giấy hiện lên màn hình")]
    public Sprite paperImageSprite;

    [Tooltip("Thoại của người chơi sau khi đọc xong tờ giấy")]
    public List<DialogNode> secondMonologue;

    [Header("=== TIỀN ÂM PHỦ ===")]
    [Tooltip("Object tiền âm phủ trên bàn (ẩn lúc đầu)")]
    public GameObject hellMoneyObject;

    [Tooltip("Thoại của người chơi khi quay lại nhìn thấy tiền âm phủ")]
    public List<DialogNode> thirdMonologue;

    [Header("=== ÂM THANH ===")]
    [Tooltip("Âm thanh tiếng cười của ma")]
    public AudioClip laughSound;
    
    [Tooltip("Chỉnh to nhỏ tiếng cười (Kéo lên 2-5 nếu file gốc quá bé)")]
    [Range(0f, 5f)] public float laughVolume = 1f;
    
    [Tooltip("Tiếng hét hoặc tiếng động khi bả bỏ chạy")]
    public AudioClip escapeSound;
    [Range(0f, 5f)] public float escapeVolume = 1f;

    private bool isActiveStep = false;
    private bool isWaitingForPlayer = false;
    private bool hasTriggeredFirstMonologue = false;
    private bool hasTriggeredRunAway = false;
    private bool isViewingPaper = false;
    private bool hasFinishedEvent = false;

    private bool isWaitingForHellMoneyView = false;
    private bool hasSeenHellMoney = false;

    private Coroutine walkCoroutine;

    public override void StartStep()
    {
        isActiveStep = true;
        hasTriggeredFirstMonologue = false;
        hasTriggeredRunAway = false;
        hasFinishedEvent = false;
        isViewingPaper = false;
        isWaitingForPlayer = false;
        isWaitingForHellMoneyView = false;
        hasSeenHellMoney = false;

        if (paperObject != null) paperObject.SetActive(false); // Giấu giấy đi
        if (hellMoneyObject != null) hellMoneyObject.SetActive(false); // Giấu tiền âm phủ

        if (ghostNPC != null)
        {
            ghostNPC.SetActive(true);
            
            // Tự động tìm Animator nếu quên kéo vào Inspector
            if (ghostAnimator == null) ghostAnimator = ghostNPC.GetComponentInChildren<Animator>();

            // Ép tắt Root Motion để tránh animation đánh nhau với script làm lún nhân vật (Giống y hệt script Customer)
            if (ghostAnimator != null) ghostAnimator.applyRootMotion = false;

            if (startPoint != null) 
            {
                UnityEngine.AI.NavMeshAgent agent = ghostNPC.GetComponent<UnityEngine.AI.NavMeshAgent>();
                
                if (agent != null)
                {
                    agent.Warp(startPoint.position);
                }
                else
                {
                    ghostNPC.transform.position = startPoint.position;
                }
            }

            // Bắt đầu đi tới điểm B
            walkCoroutine = StartCoroutine(WalkRoutine(endPoint, normalSpeed, () => 
            {
                // Tới nơi thì tắt dáng đi bộ
                isWaitingForPlayer = true;
                if (ghostAnimator != null && !string.IsNullOrEmpty(isWalkingParam))
                {
                    ghostAnimator.SetBool(isWalkingParam, false);
                }
                
                // Bật tiếng cười!
                if (laughSound != null)
                {
                    AudioSource audio = ghostNPC.GetComponent<AudioSource>();
                    if (audio == null) audio = ghostNPC.AddComponent<AudioSource>();
                    
                    audio.spatialBlend = 1f; // Thuần 3D
                    audio.rolloffMode = AudioRolloffMode.Linear; // Linear giúp âm thanh vang xa hơn
                    audio.minDistance = 2f; // Chỉ khi đứng cách ma 2m thì mới nghe to nhất
                    audio.maxDistance = 60f; // Đi xa 60m mới tắt, đảm bảo 40m vẫn nghe thấy vang vọng
                    // AudioSource.volume trong Unity tối đa chỉ là 1f. Nếu bạn nhập 5, nó cũng chỉ là 1f.
                    // Nếu tiếng vẫn nhỏ thì do bản gốc của file âm thanh bị nhỏ!
                    audio.volume = laughVolume > 1f ? 1f : laughVolume;
                    
                    audio.clip = laughSound;
                    audio.loop = true; // Cứ cười cho đến khi bị bắt
                    audio.Play();
                }
                    
                Debug.Log("[LaughingGhostEvent] Ma đã tới nơi và đang cười...");
            }));
        }

        if (paperObject != null)
        {
            InteractableObject io = paperObject.GetComponent<InteractableObject>();
            if (io != null) io.type = InteractableType.GhostPaper; 
        }
    }

    IEnumerator WalkRoutine(Transform target, float speed, System.Action onReached)
    {
        if (ghostNPC == null || target == null) yield break;

        // BẮT BUỘC: Chờ 1 frame để Animator kịp khởi động sau khi SetActive(true). Nếu không Unity sẽ tàng hình lệnh SetBool!
        yield return null;

        if (ghostAnimator != null) 
        {
            ghostAnimator.enabled = true;
            if (!string.IsNullOrEmpty(isWalkingParam))
            {
                Debug.Log($"[DEBUG ANIMATOR] Đang gọi SetBool('{isWalkingParam}', true) trên {ghostAnimator.gameObject.name}");
                ghostAnimator.SetBool(isWalkingParam, true);
            }
            StartCoroutine(DebugAnimatorRoutine());
        }

        UnityEngine.AI.NavMeshAgent agent = ghostNPC.GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        if (agent != null)
        {
            agent.enabled = true;
            agent.speed = speed;
            agent.acceleration = speed * 10f; // Bắt buộc tăng tốc tức thời
            agent.angularSpeed = 720f;
            agent.updateRotation = true; // Để NavMesh tự xoay người cho tự nhiên
            agent.isStopped = false;
            
            agent.SetDestination(target.position);
            
            // Đợi 1 frame để NavMeshAgent tính toán đường đi, tránh lỗi remainingDistance = 0 ở frame đầu tiên
            yield return null;
            
            // Dùng remainingDistance: Nếu điểm đích nằm ngoài NavMesh, bả sẽ đi đến điểm gần nhất rồi dừng lại
            while (agent.pathPending || agent.remainingDistance > 0.5f)
            {
                yield return null;
            }
            
            agent.isStopped = true;
        }

        onReached?.Invoke();
    }

    IEnumerator DebugAnimatorRoutine()
    {
        while (ghostAnimator != null && ghostNPC.activeInHierarchy)
        {
            AnimatorStateInfo stateInfo = ghostAnimator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(1f);
        }
    }

    void Update()
    {
        if (!isActiveStep || hasFinishedEvent) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        // Bắn 1 tia chung cho các logic dùng Raycast
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, 15f);

        // 1. Quét xem người chơi CÓ NHÌN THẤY MA KHÔNG (Góc nhìn rộng)
        // Bỏ điều kiện isWaitingForPlayer để ma đang đi bộ vẫn có thể nhìn thấy và thoại!
        if (!hasTriggeredFirstMonologue && ghostNPC != null && ghostNPC.activeInHierarchy)
        {
            // Hướng từ camera đến con ma
            Vector3 targetPos = ghostNPC.transform.position + Vector3.up * 1.2f; // Nhắm vào tầm ngực ma
            Vector3 dirToGhost = (targetPos - cam.transform.position).normalized;
            
            // Tính góc giữa hướng nhìn của camera và hướng đến con ma
            float angle = Vector3.Angle(cam.transform.forward, dirToGhost);

            // Nếu con ma nằm trong góc nhìn 60 độ của người chơi (tức là lọt vào màn hình)
            if (angle < 60f)
            {
                // Bắn tia xuyên thấu với khoảng cách 45m
                RaycastHit[] hitsToGhost = Physics.RaycastAll(cam.transform.position, dirToGhost, 45f);
                
                // Sắp xếp các vật cản từ gần đến xa
                System.Array.Sort(hitsToGhost, (a, b) => a.distance.CompareTo(b.distance));

                foreach (RaycastHit h in hitsToGhost)
                {
                    // Chạm trúng con ma trước -> Kích hoạt thoại!
                    if (h.collider.gameObject == ghostNPC || h.collider.transform.IsChildOf(ghostNPC.transform))
                    {
                        OnSeenGhostFromAfar();
                        break;
                    }
                    // Chạm trúng vật cản cứng (Tường, cửa...) trước -> Ma bị che khuất -> Thoát!
                    else if (!h.collider.isTrigger)
                    {
                        break;
                    }
                }
            }
        }

        // 3. Quét xem người chơi có quay lại nhìn thấy tiền âm phủ không (Dùng tia nhìn)
        if (isWaitingForHellMoneyView && !hasSeenHellMoney && hellMoneyObject != null)
        {
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == hellMoneyObject || hit.collider.transform.IsChildOf(hellMoneyObject.transform))
                {
                    OnSeenHellMoney();
                    return;
                }
            }
        }
    }

    private void OnSeenGhostFromAfar()
    {
        hasTriggeredFirstMonologue = true;
        Debug.Log("[LaughingGhostEvent] Người chơi đã nhìn thấy ma từ xa!");

        if (DialogManager.Instance != null && firstMonologue != null && firstMonologue.Count > 0)
        {
            // Chỉ hiện thoại, ma vẫn đứng cười
            DialogManager.Instance.StartDialogSequence(firstMonologue, null);
        }
    }

    // GỌI HÀM NÀY TỪ COLLIDER TRIGGER ĐỂ MA CHẠY TRỐN
    public void TriggerGhostRunAway()
    {
        if (hasTriggeredRunAway) return;
        hasTriggeredRunAway = true;

        // Dừng luôn hành động đi bộ/cười hiện tại lại
        if (walkCoroutine != null) StopCoroutine(walkCoroutine);

        // Tắt tiếng cười
        if (ghostNPC != null)
        {
            AudioSource audio = ghostNPC.GetComponent<AudioSource>();
            if (audio != null) audio.Stop();
        }
        
        // Quét thêm AudioSource trên chính cái Event (phòng trường hợp người dùng gắn nó ở ngoài)
        AudioSource parentAudio = GetComponent<AudioSource>();
        if (parentAudio != null) parentAudio.Stop();

        // Phát âm thanh bỏ chạy (nếu có)
        if (escapeSound != null)
        {
            if (parentAudio == null) parentAudio = gameObject.AddComponent<AudioSource>();
            parentAudio.spatialBlend = 1f;
            parentAudio.rolloffMode = AudioRolloffMode.Linear;
            parentAudio.minDistance = 2f;
            parentAudio.maxDistance = 60f;
            parentAudio.volume = escapeVolume;
            parentAudio.clip = escapeSound;
            parentAudio.loop = false; // Tiếng bỏ chạy chỉ kêu 1 lần
            parentAudio.Play();
        }

        // Rớt tờ giấy (Không dùng Vật lý nữa để tránh văng tung tóe và hất người chơi)
        if (paperObject != null && ghostNPC != null)
        {
            // Để thẳng tờ giấy xuống chân ma
            paperObject.transform.position = ghostNPC.transform.position + Vector3.up * 0.05f; 
            
            // Xoay lung tung một chút cho tự nhiên (chỉ xoay ngang)
            paperObject.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            paperObject.SetActive(true);
        }

        // Ma chạy chối chết về điểm xuất phát
        if (ghostNPC != null && startPoint != null)
        {
            walkCoroutine = StartCoroutine(WalkRoutine(startPoint, escapeSpeed, () => 
            {
                ghostNPC.SetActive(false); // Chạy tới nơi thì bốc hơi
                Debug.Log("[LaughingGhostEvent] Ma đã chạy trốn thành công.");
            }));
        }
        else if (ghostNPC != null)
        {
            ghostNPC.SetActive(false);
        }
    }

    // Hàm này sẽ được gọi từ InteractionSystem khi người chơi bấm vào tờ giấy
    public void ReadPaper()
    {
        if (isViewingPaper || hasFinishedEvent) return;
        isViewingPaper = true;
        
        // Ẩn chữ nhiệm vụ
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.ShowObjective("");

        StartCoroutine(ShowPaperUI());
    }

    private IEnumerator ShowPaperUI()
    {
        InteractionSystem isys = FindObjectOfType<InteractionSystem>();
        Canvas canvas = isys != null ? isys.promptPanel.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();

        GameObject paperUI = null;
        if (canvas != null)
        {
            paperUI = new GameObject("GhostPaperUI");
            paperUI.transform.SetParent(canvas.transform, false);
            
            Image img = paperUI.AddComponent<Image>();
            if (paperImageSprite != null) img.sprite = paperImageSprite;
            else img.color = Color.white; 
            
            RectTransform rt = paperUI.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(800, 800);

            // Đợi người chơi đọc trong 3 giây
            yield return new WaitForSeconds(3.0f);
            
            Destroy(paperUI);
        }

        // Đọc xong thì biến mất tờ giấy dưới đất
        if (paperObject != null) paperObject.SetActive(false);

        // Thoại lần 2
        if (DialogManager.Instance != null && secondMonologue != null && secondMonologue.Count > 0)
        {
            DialogManager.Instance.StartDialogSequence(secondMonologue, (result) => 
            {
                SpawnHellMoney();
            });
        }
        else
        {
            SpawnHellMoney();
        }
    }

    private void SpawnHellMoney()
    {
        if (hellMoneyObject != null)
        {
            hellMoneyObject.SetActive(true);
            isWaitingForHellMoneyView = true;
            
            // Xóa tạm Component Tương tác để chưa cho nhặt vội
            InteractableObject io = hellMoneyObject.GetComponent<InteractableObject>();
            if (io != null) Destroy(io);
                
            Debug.Log("[LaughingGhostEvent] Tiền âm phủ đã xuất hiện trên bàn.");
        }
        else
        {
            FinishEvent();
        }
    }

    private void OnSeenHellMoney()
    {
        hasSeenHellMoney = true;
        isWaitingForHellMoneyView = false;
        
        Debug.Log("[LaughingGhostEvent] Người chơi đã nhìn thấy tiền âm phủ!");

        if (DialogManager.Instance != null && thirdMonologue != null && thirdMonologue.Count > 0)
        {
            DialogManager.Instance.StartDialogSequence(thirdMonologue, (result) => 
            {
                MakeHellMoneyInteractable();
            });
        }
        else
        {
            MakeHellMoneyInteractable();
        }
    }

    private void MakeHellMoneyInteractable()
    {
        if (hellMoneyObject != null)
        {
            InteractableObject io = hellMoneyObject.GetComponent<InteractableObject>();
            if (io == null) io = hellMoneyObject.AddComponent<InteractableObject>();
            io.type = InteractableType.HellMoney;
        }
    }

    public void ThrowHellMoney()
    {
        if (hasFinishedEvent) return;
        Debug.Log("[LaughingGhostEvent] Đã vứt tiền âm phủ!");
        
        if (hellMoneyObject != null) hellMoneyObject.SetActive(false);
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.ShowObjective("");
        
        FinishEvent();
    }

    private void FinishEvent()
    {
        hasFinishedEvent = true;
        isActiveStep = false;
        Debug.Log("[LaughingGhostEvent] Hoàn thành sự kiện ma cười!");
        CompleteStep();
    }
}
