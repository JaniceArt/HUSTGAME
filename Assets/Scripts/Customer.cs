using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CustomerState
{
    WaitingForTurn, // Trạng thái mặc định (0) để khách không tự động đi khi chưa đến lượt
    WalkingIn,
    WaitingToTalk,
    Talking,
    WaitingForOrder,
    WalkingOut
}

[RequireComponent(typeof(InteractableObject))]
public class Customer : SequenceStep
{
    [Header("=== KHÁCH HÀNG ===")]
    public CustomerData customerData;

    [Tooltip("Animator của khách hàng")]
    public Animator animator;

    [Tooltip("AudioSource để phát âm thanh (tự tìm nếu để trống)")]
    public AudioSource audioSource;

    [Header("=== VẬT PHẨM ĐẶC BIỆT ===")]
    [Tooltip("Prefab của cái USB (nhớ gắn InteractableObject type = UsbDrive vào prefab này)")]
    public GameObject usbPrefab;
    
    [Tooltip("Điểm xuất phát lúc ném USB (Bắt buộc gán)")]
    public Transform usbSpawnPoint;

    [Tooltip("Điểm USB rớt xuống bàn (Bắt buộc gán)")]
    public Transform usbDropPoint;

    [Tooltip("Điểm xuất phát lúc văng tiền (thường là tay hoặc ngực khách)")]
    public Transform moneySpawnPoint;

    [Tooltip("Điểm tiền rớt xuống bàn (thường là mặt bàn quầy)")]
    public Transform moneyDropPoint;

    [Tooltip("Vũng nước CÓ SẴN trên scene (ẩn lúc đầu). Sẽ bật lên khi khách quay đi (dùng thay cho Prefab)")]
    public GameObject prePlacedPuddle;

    [Header("=== ANIMATION PARAMETERS ===")]
    public string isWalkingParam = "isWalking";
    public string isAngryParam = "isAngry"; // Đổi lại thành IsAngry ở Inspector nếu cần!
    public string isDancingParam = "isDancing";

    [Tooltip("Thời gian chờ tối đa (giây) trước khi tức giận. Đặt 0 để không tức giận.")]
    public float patienceTime = 10f;

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Điểm khách hàng bắt đầu đi ra (Vd: Ngoài đường)")]
    public Transform startPoint;
    
    [Tooltip("Điểm đặc biệt để khách đi về (VD: Đi vào nhà vệ sinh). Bỏ trống nếu muốn khách đi về lại Start Point.")]
    public Transform customExitPoint;
    
    [Tooltip("Điểm khách hàng đứng mua hàng (Vd: Trước quầy)")]
    public Transform counterPoint;

    [Tooltip("Tốc độ đi bộ của khách")]
    public float walkSpeed = 2.5f;

    [Tooltip("Góc bù (Nhập 90, -90, hoặc 180 nếu khách bị quay ngang)")]
    public float yRotationOffset = 0f;

    [Header("=== TRẠNG THÁI (Chỉ đọc) ===")]
    [SerializeField] private CustomerState currentState;
    public CustomerState CurrentState => currentState;

    private bool hasReceivedDocument = false;
    private bool hasReceivedFood = false;
    private bool hasReceivedDrink = false;
    private int foodDeliveredCount = 0; // Số hộp xôi đã giao

    private int currentDialogIndex = 0;
    private float waitTimer = 0f;
    private bool isAngry = false;

    // Biến cờ tĩnh để theo dõi xem đã làm xong Tutorial cho người khách đầu tiên chưa
    private static bool hasDoneTutorial = false;

    void Start()
    {
        InteractableObject interactObj = GetComponent<InteractableObject>();
        if (interactObj != null)
        {
            interactObj.type = InteractableType.Customer;
        }

        // Tự động tìm AudioSource nếu chưa gán
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
                Debug.Log($"[Customer] Tự động tìm được Animator trên: {animator.gameObject.name}");
            else
                Debug.LogError($"[Customer] KHÔNG TÌM THẤY Animator nào trong children!");
        }

        // Tự động đánh dấu hoàn thành nếu khách không có nhu cầu món đó
        if (customerData != null)
        {
            hasReceivedDocument = !customerData.needsDocument;
            hasReceivedFood = !customerData.needsFood;
        }
    }

    void Update()
    {
        // DEBUG: Log state mỗi 2 giây
        if (Mathf.FloorToInt(Time.time) % 2 == 0 && Mathf.FloorToInt(Time.time) != Mathf.FloorToInt(Time.time - Time.deltaTime))
            Debug.Log($"[Customer Update] {customerData?.customerName} | State: {currentState} | waitTimer: {waitTimer:F1} | patience: {patienceTime} | isAngry: {isAngry}");

        // Tính giờ chờ khi khách đang đợi đồ
        if (currentState == CustomerState.WaitingForOrder && patienceTime > 0)
        {
            if (!isAngry)
            {
                waitTimer += Time.deltaTime;
                
                // Debug mỗi giây
                if (Mathf.FloorToInt(waitTimer) != Mathf.FloorToInt(waitTimer - Time.deltaTime))
                    Debug.Log($"[Angry Timer] {customerData?.customerName}: {Mathf.FloorToInt(waitTimer)}s / {patienceTime}s");
                
                if (waitTimer >= patienceTime)
                {
                    isAngry = true;
                    // Phát âm thanh tức giận loop cùng animation
                    PlayAngrySound(true);
                    Debug.Log($"[Customer] Khách {customerData?.customerName} đã chờ quá {patienceTime}s và bắt đầu tức giận!");
                }
            }
        }
        else if (currentState != CustomerState.WaitingForOrder)
        {
            // Debug để biết state hiện tại nếu timer không chạy
            // (chỉ log 1 lần mỗi khi state thay đổi - dùng flag tạm)
        }

        // Đồng bộ Parameter cho Animator một cách an toàn để tránh bị treo/lỗi
        if (animator != null)
        {
            bool walking = (currentState == CustomerState.WalkingIn || currentState == CustomerState.WalkingOut);
            
            // Chỉ cập nhật giá trị nếu có sự thay đổi (để dễ debug)
            bool currentWalking = false;
            if (HasParameter(isWalkingParam)) 
            {
                currentWalking = animator.GetBool(isWalkingParam);
                if (currentWalking != walking)
                {
                    Debug.Log($"[Customer] Đã đổi lệnh isWalking thành: {walking}");
                    animator.SetBool(isWalkingParam, walking);
                }
            }

            if (HasParameter(isAngryParam)) animator.SetBool(isAngryParam, isAngry);
        }
    }

    // Hàm kiểm tra an toàn xem Animator có chứa parameter này không
    private bool HasParameter(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void LateUpdate()
    {
        // HÀM NÀY CHẠY SAU KHI ANIMATOR ĐÃ TÍNH TOÁN XONG!
        // Giúp đè bẹp những góc xoay sai trái của Animation một cách mượt mà.
        
        Quaternion targetRotation = transform.rotation;

        if (currentState == CustomerState.WalkingIn && counterPoint != null)
        {
            Vector3 direction = (counterPoint.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, yRotationOffset, 0);
        }
        else if ((currentState == CustomerState.WaitingToTalk || currentState == CustomerState.WaitingForOrder || currentState == CustomerState.Talking) && counterPoint != null)
        {
            targetRotation = counterPoint.rotation * Quaternion.Euler(0, yRotationOffset, 0);
        }
        else if (currentState == CustomerState.WalkingOut && startPoint != null)
        {
            Vector3 direction = (startPoint.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, yRotationOffset, 0);
        }

        // Xoay mượt mà thay vì giật cục (snap)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
    }

    /// <summary>
    /// Được gọi bởi SequenceManager khi đến lượt khách này.
    /// </summary>
    public override void StartStep()
    {
        gameObject.SetActive(true);
        DocumentManager.Instance.SetCurrentCustomer(customerData);
        
        Debug.Log($"[DEBUG] Object name: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // Báo lỗi chính xác
        if (animator == null)
        {
            Debug.LogError($"<color=red>[LỖI NGHIÊM TRỌNG]</color> Khách {gameObject.name} CHƯA GẮN ANIMATOR vào script! Hãy kéo component Animator vào ô Animator trong script Customer.");
        }
        else
        {
            if (animator.runtimeAnimatorController == null)
                Debug.LogError($"<color=red>[LỖI NGHIÊM TRỌNG]</color> Khách {gameObject.name} CHƯA CÓ ANIMATOR CONTROLLER! Hãy kéo file Cus2 vào ô Controller của component Animator.");
            
            // XÓA ĐOẠN CHECK AVATAR Ở ĐÂY VÌ FILE GLB KHÔNG CẦN AVATAR VẪN CHẠY ĐƯỢC
            
            // Ép tắt Root Motion để tránh đánh nhau với code di chuyển
            animator.applyRootMotion = false;
        }

        if (startPoint == null)
        {
            Debug.LogError("[LOI] StartPoint bị NULL! Thử tìm tự động...");
            GameObject sp = GameObject.Find("StartPoint");
            if (sp != null) startPoint = sp.transform;
            Debug.LogError($"[LOI] Tự động tìm StartPoint thành công: {startPoint != null}");
        }
        
        if (counterPoint == null || counterPoint.gameObject.name != "EndPoint")
        {
            Debug.LogError("[LOI] EndPoint bị NULL hoặc gán nhầm! Ép tìm lại...");
            GameObject ep = GameObject.Find("EndPoint");
            if (ep != null) counterPoint = ep.transform;
        }

        hasReceivedDocument = !customerData.needsDocument;
        hasReceivedFood = !customerData.needsFood;
        hasReceivedDrink = !customerData.needsDrink;
        foodDeliveredCount = 0;
        currentDialogIndex = 0;
        waitTimer = 0f;
        isAngry = false;

        // Bắt đầu đi bộ vào (để Coroutine xử lý chuyển state)
        StartCoroutine(WalkInRoutine());
    }

    IEnumerator WalkInRoutine()
    {
        // Đợi để Animator kịp khởi tạo sau SetActive(true)
        yield return null;
        yield return null;
        yield return null;

        // Ép Animator nhận đúng parameter mặc định một cách an toàn
        if (animator != null) 
        {
            if (HasParameter("AnimState")) animator.SetInteger("AnimState", 0);
            if (HasParameter(isDancingParam)) animator.SetBool(isDancingParam, false);
        }

        ChangeState(CustomerState.WalkingIn); // Gọi ở đây để tránh lỗi '-1'
        
        if (startPoint != null && counterPoint != null)
        {
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.updateRotation = false; // Tắt tự xoay của NavMeshAgent để tự xoay mượt bằng script
                agent.speed = walkSpeed;
                agent.isStopped = false;
                agent.Warp(startPoint.position);
                agent.SetDestination(counterPoint.position);
                
                // Đợi cho đến khi tìm xong đường và đi đến nơi
                while (agent.pathPending || agent.remainingDistance > 0.1f)
                {
                    yield return null;
                }
                
                agent.isStopped = true;
                agent.ResetPath();
            }
            else
            {
                // Fallback nếu người chơi quên gắn NavMeshAgent (trượt cũ)
                transform.position = startPoint.position;
                while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(counterPoint.position.x, counterPoint.position.z)) > 0.1f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, counterPoint.position, walkSpeed * Time.deltaTime);
                    yield return null; 
                }
                transform.position = counterPoint.position;
            }

            // Đã đến nơi (bỏ check khoảng cách quá gắt < 0.2f gây kẹt)
            if (animator != null) animator.SetInteger("AnimState", 0);
        
            // Xoay mặt về hướng quầy
            transform.rotation = counterPoint.rotation;
        
            ChangeState(CustomerState.WaitingToTalk);

            // [TUTORIAL] Khi khách đầu tiên đi vào quầy
            if (!hasDoneTutorial && ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.ShowObjective("Nói chuyện với khách hàng để lấy Order");
            }
        }
        else
        {
            Debug.Log($"[DIAGNOSTICS] Đã vào else block (chờ 2s) vì có 1 biến bị null!");
            yield return new WaitForSeconds(2f);
            ChangeState(CustomerState.WaitingToTalk);
        }
    }

    void ChangeState(CustomerState newState)
    {
        currentState = newState;

        // Phát âm thanh tương ứng với trạng thái
        switch (currentState)
        {
            case CustomerState.WalkingIn:
                PlayWalkingSound(true);
                break;
            case CustomerState.WaitingToTalk:
                PlayWalkingSound(false);
                break;
            case CustomerState.Talking:
                break;
            case CustomerState.WaitingForOrder:
                if (customerData != null && customerData.dancesWhileWaiting && animator != null)
                {
                    if (HasParameter(isDancingParam)) animator.SetBool(isDancingParam, true);
                    PlayDancingSound(true);
                }
                break;
            case CustomerState.WalkingOut:
                if (animator != null)
                {
                    if (HasParameter(isDancingParam)) animator.SetBool(isDancingParam, false);
                    PlayDancingSound(false);
                }
                PlayAngrySound(false); // Dừng angry sound khi đi ra
                PlayWalkingSound(true);
                break;
        }
        Debug.Log($"[Customer {customerData?.customerName}] Chuyển trạng thái: {newState}");
    }

    void PlayWalkingSound(bool play)
    {
        if (audioSource == null || customerData?.walkingSound == null) return;
        if (play)
        {
            audioSource.clip = customerData.walkingSound;
            audioSource.loop = true;
            audioSource.volume = customerData.walkingSoundVolume;
            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
        }
    }

    void PlayAngrySound(bool play)
    {
        if (audioSource == null || customerData?.angrySound == null) return;
        if (play)
        {
            audioSource.clip = customerData.angrySound;
            audioSource.loop = true;
            audioSource.volume = customerData.angrySoundVolume;
            audioSource.Play();
        }
        else
        {
            if (audioSource.clip == customerData.angrySound)
                audioSource.Stop();
        }
    }

    void PlayDancingSound(bool play)
    {
        if (audioSource == null || customerData?.dancingSound == null) return;
        if (play)
        {
            // Nếu nhạc đang chơi rồi thì kệ nó, không Play lại từ đầu nữa
            if (audioSource.clip == customerData.dancingSound && audioSource.isPlaying) return;

            audioSource.spatialBlend = 1f; // 1 = Âm thanh 3D (âm thanh phát ra từ vị trí của ông khách)
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 10f; // Bạn càng đi xa nhạc càng nhỏ
            
            audioSource.clip = customerData.dancingSound;
            audioSource.loop = true;
            audioSource.volume = customerData.dancingSoundVolume; 
            audioSource.Play();
        }
        else
        {
            if (audioSource.clip == customerData.dancingSound)
                audioSource.Stop();
        }
    }



    /// <summary>
    /// Khi người chơi bấm E vào khách hàng.
    /// </summary>
    public void Interact()
    {
        if (currentState == CustomerState.WaitingToTalk)
        {
            // Bấm E lần đầu để bắt đầu nói chuyện
            HandleDialog();
        }
        else if (currentState == CustomerState.WaitingForOrder)
        {
            HandleDelivery();
        }
        // Lưu ý: Nếu currentState == Talking, không làm gì khi bấm E (chỉ nhận Space ở Update)
    }

    void HandleDialog()
    {
        ChangeState(CustomerState.Talking);

        if (customerData.dialogNodes == null || customerData.dialogNodes.Count == 0)
        {
            ChangeState(CustomerState.WaitingForOrder);
            return;
        }

        if (DialogManager.Instance != null)
        {
            // Bàn giao toàn bộ kịch bản cho DialogManager xử lý
            DialogManager.Instance.StartDialogSequence(
                customerData.dialogNodes, 
                OnDialogFinished
            );
        }
        else
        {
            Debug.LogError("[Customer] LỖI: DialogManager chưa có trong Scene!");
            ChangeState(CustomerState.WaitingForOrder);
        }
    }

    private bool hasToldOrder = false;

    void OnDialogFinished(int choiceResult)
    {
        Debug.Log($"[DIALOG - {customerData.customerName}]: Đã nói xong! Lựa chọn: {choiceResult}");
        
        // Spawn USB nếu khách có mang USB (Chỉ vứt lần đầu tiên nói chuyện)
        if (!hasToldOrder && customerData != null && customerData.hasUsb && usbPrefab != null && counterPoint != null)
        {
            StartCoroutine(ThrowUsbRoutine());
        }

        hasToldOrder = true;

        // Nếu khách này được tick 'Leaves Angry', họ sẽ dỗi và bỏ đi LUÔN sau khi nói chuyện
        // Không chờ đồ ăn đồ uống gì nữa!
        if (customerData != null && customerData.leavesAngry)
        {
            Debug.Log($"[Customer] Khách {customerData.customerName} tức giận bỏ về ngay sau hội thoại!");
            FinishAndWalkOut();
            return;
        }

        ChangeState(CustomerState.WaitingForOrder);

        // [TUTORIAL] Sau khi khách nói xong order
        if (!hasDoneTutorial && ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective("Làm đồ ăn, nước uống theo yêu cầu và giao cho khách");
        }
    }

    IEnumerator ThrowUsbRoutine()
    {
        if (usbSpawnPoint == null || usbDropPoint == null)
        {
            Debug.LogError($"[Customer] Khách {customerData?.customerName} CẦN ném USB nhưng chưa gán điểm usbSpawnPoint hoặc usbDropPoint! Hãy gán trong Inspector.");
            yield break;
        }

        Vector3 startPos = usbSpawnPoint.position;
        Vector3 endPos = usbDropPoint.position; 
        
        GameObject spawnedUsb = Instantiate(usbPrefab, startPos, Quaternion.identity);
        
        // Tạm thời tắt collider/rigidbody (nếu có) để diễn hoạt ảnh ném lơ lửng cho chuẩn
        Collider col = spawnedUsb.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Rigidbody rb = spawnedUsb.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        float duration = 0.5f; // Thời gian bay là 0.5 giây
        float time = 0;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            
            // Bay theo đường cong parabol
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.8f; // Tạo độ vòng (đỉnh parabol nảy lên 0.8m)
            
            // Vừa bay vừa lộn nhào cho chân thực
            if (spawnedUsb != null)
            {
                spawnedUsb.transform.position = currentPos;
                spawnedUsb.transform.Rotate(Vector3.right * 720 * Time.deltaTime);
            }

            yield return null;
        }

        if (spawnedUsb != null)
        {
            spawnedUsb.transform.position = endPos;
            spawnedUsb.transform.rotation = usbDropPoint.rotation; // Rơi xuống nằm y hệt như cái góc của Drop Point

            // Bật lại collider để main có thể tương tác bấm E
            if (col != null) col.enabled = true;
            if (rb != null) rb.isKinematic = false;
            
            Debug.Log($"[Customer] USB đã hạ cánh an toàn xuống bàn!");
        }
    }

    private void StartItemFlight(GameObject originalItemInHand)
    {
        if (originalItemInHand == null) return;
        StartCoroutine(FlyOriginalItemRoutine(originalItemInHand));
    }

    private System.Collections.IEnumerator FlyOriginalItemRoutine(GameObject item)
    {
        // 1. Lưu lại toàn bộ vị trí/trạng thái gốc trên tay
        Transform parentBackup = item.transform.parent;
        Vector3 localPosBackup = item.transform.localPosition;
        Quaternion localRotBackup = item.transform.localRotation;
        
        // Ép bật lên
        item.SetActive(true);

        // 2. Tháo khỏi Camera
        item.transform.SetParent(null);
        
        Animator[] anims = item.GetComponentsInChildren<Animator>();
        foreach (var anim in anims) anim.enabled = false;

        float duration = 0.5f; 
        float time = 0;
        
        Vector3 startPos = item.transform.position;
        Vector3 endPos = DeliveryPoint.Instance != null 
            ? DeliveryPoint.Instance.transform.position 
            : (transform.position + Vector3.up * 1.1f); 

        // SỬA LỖI ĐI CHỆCH: Bù trừ độ lệch giữa Transform (điểm gốc) và Mesh (phần hình ảnh)
        Vector3 visualCenter = GetVisualCenter(item);
        Vector3 offset = item.transform.position - visualCenter;
        Vector3 targetPos = endPos + offset;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            
            // Bay vòng cung nhẹ
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.4f; 
            
            item.transform.position = currentPos;
            yield return null;
        }
        
        // 3. Bay tới nơi -> Tắt tàng hình
        item.SetActive(false);
        
        // 4. Trả item về lại tay Camera
        item.transform.SetParent(parentBackup);
        item.transform.localPosition = localPosBackup;
        item.transform.localRotation = localRotBackup;
        
        foreach (var anim in anims) anim.enabled = true;
    }

    private Vector3 GetVisualCenter(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return obj.transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds.center;
    }

    void HandleDelivery()
    {
        DocumentManager dm = DocumentManager.Instance;
        ToppingManager tm = ToppingManager.Instance;

        bool hasHandedSomething = false;
        bool hasWrongOrder = false;

        // TH 1: Giao Tài liệu
        if (dm != null && dm.IsHoldingDocument && !hasReceivedDocument)
        {
            hasHandedSomething = true;
            if (dm.DocumentCustomerData == customerData && dm.DocumentIsColor == customerData.requiresColor && dm.DocumentQuantity == customerData.requiredCopies)
            {
                GameObject itemToFly = dm.GetHeldPaperObject();
                dm.DeliverDocument();
                StartItemFlight(itemToFly);
                hasReceivedDocument = true;
                Debug.Log($"<color=green>[Thành công]</color> Khách {customerData.customerName} đã nhận đúng tài liệu!");
            }
            else
            {
                hasWrongOrder = true;
                Debug.LogWarning($"<color=red>[Thất bại]</color> TÀI LIỆU SAI! Khách đòi In Màu: {customerData.requiresColor}, Số lượng: {customerData.requiredCopies}. (Bạn đưa bản in màu: {dm.DocumentIsColor}, Số lượng: {dm.DocumentQuantity}, của khách: {dm.DocumentCustomerData?.customerName})");
            }
        }

        // TH 2: Giao Hộp Xôi
        if (tm != null && tm.isHoldingFood && !hasReceivedFood)
        {
            hasHandedSomething = true;
            bool isCorrect = ValidateFood(tm);
            if (isCorrect)
            {
                GameObject itemToFly = tm.GetHeldFoodObject();
                tm.DeliverFood();
                StartItemFlight(itemToFly);
                foodDeliveredCount++;
                int required = customerData.requiredFoodQuantity > 0 ? customerData.requiredFoodQuantity : 1;
                Debug.Log($"<color=green>[Tiến độ]</color> Đã giao {foodDeliveredCount}/{required} hộp xôi.");
                if (foodDeliveredCount >= required)
                {
                    hasReceivedFood = true;
                    Debug.Log($"<color=green>[Đủ số lượng]</color> Khách {customerData.customerName} đã nhận đủ {required} hộp xôi!");
                }
            }
            else
            {
                hasWrongOrder = true;
                tm.MarkFoodAsWrong();
                Debug.LogWarning($"<color=red>[Đầu bếp gà]</color> HỘP XÔI SAI TOPPING!");
            }
        }

        // TH 3: Giao Nước
        DrinkManager drinkMan = DrinkManager.Instance;
        if (drinkMan != null && drinkMan.isHoldingDrink && !hasReceivedDrink)
        {
            hasHandedSomething = true;
            if (drinkMan.currentDrink == customerData.requiredDrink)
            {
                GameObject itemToFly = drinkMan.GetActiveDrinkObject();
                drinkMan.DeliverDrink();
                StartItemFlight(itemToFly);
                hasReceivedDrink = true;
                Debug.Log($"<color=green>[Thành công]</color> Khách {customerData.customerName} đã nhận đúng chai {customerData.requiredDrink}!");
            }
            else
            {
                hasWrongOrder = true;
                Debug.LogWarning($"<color=red>[Thất bại]</color> GIAO SAI NƯỚC! Khách đòi {customerData.requiredDrink} nhưng bạn đưa {drinkMan.currentDrink}.");
            }
        }

        if (!hasHandedSomething)
        {
            Debug.Log("[Customer] Người chơi không cầm gì hoặc cầm đồ sai loại -> Xin nhắc lại Order.");
            RepeatOrder();
            return;
        }
        else if (hasWrongOrder)
        {
            ShowWrongOrderDialog();
        }

        // Báo cáo tiến độ cho dễ biết khách đang đợi gì
        if (customerData.needsDocument && !hasReceivedDocument)
            Debug.Log($"<color=cyan>[Tiến độ]</color> Khách {customerData.customerName} đang chờ nhận TÀI LIỆU...");
        if (customerData.needsFood && !hasReceivedFood)
            Debug.Log($"<color=cyan>[Tiến độ]</color> Khách {customerData.customerName} đang chờ nhận HỘP XÔI...");
        if (customerData.needsDrink && !hasReceivedDrink)
            Debug.Log($"<color=cyan>[Tiến độ]</color> Khách {customerData.customerName} đang chờ nhận NƯỚC ({customerData.requiredDrink})...");

        // Kiểm tra xem đã đủ hết chưa
        CheckCompletion();
    }

    void RepeatOrder()
    {
        ChangeState(CustomerState.Talking);
        if (DialogManager.Instance != null && customerData != null && !string.IsNullOrEmpty(customerData.repeatOrderDialog))
        {
            List<DialogNode> nodes = new List<DialogNode>
            {
                new DialogNode { sentence = customerData.repeatOrderDialog, hasChoices = false }
            };
            DialogManager.Instance.StartDialogSequence(nodes, (result) => 
            {
                ChangeState(CustomerState.WaitingForOrder);
            });
        }
        else
        {
            ChangeState(CustomerState.WaitingForOrder);
        }
    }

    void ShowWrongOrderDialog()
    {
        if (DialogManager.Instance != null && customerData != null && !string.IsNullOrEmpty(customerData.wrongOrderDialog))
        {
            List<DialogNode> wrongNodes = new List<DialogNode>
            {
                new DialogNode { sentence = customerData.wrongOrderDialog, hasChoices = false }
            };
            
            ChangeState(CustomerState.Talking);
            
            DialogManager.Instance.StartDialogSequence(wrongNodes, (result) => 
            {
                ChangeState(CustomerState.WaitingForOrder);
            });
        }
    }

    bool ValidateFood(ToppingManager tm)
    {
        if (customerData.requiredToppings == null) return true; // Không yêu cầu gì

        // Xôi mặc định phải có (Giả sử hộp xôi luôn có xôi)
        // Kiểm tra từng món trong Enum
        bool needsPate = customerData.requiredToppings.Contains(ToppingType.Pate);
        bool needsSausage = customerData.requiredToppings.Contains(ToppingType.Sausage);
        bool needsCucumber = customerData.requiredToppings.Contains(ToppingType.Cucumber);
        bool needsKetchup = customerData.requiredToppings.Contains(ToppingType.Ketchup);
        bool needsEgg = customerData.requiredToppings.Contains(ToppingType.Egg);

        if (tm.HasPate != needsPate) return false;
        if (tm.HasSausage != needsSausage) return false;
        if (tm.HasCucumber != needsCucumber) return false;
        if (tm.HasKetchup != needsKetchup) return false;
        if (tm.HasEgg != needsEgg) return false;

        return true;
    }

    void CheckCompletion()
    {
        if (hasReceivedDocument && hasReceivedFood && hasReceivedDrink)
        {
            // Tắt nhảy múa khi đã nhận FULL đồ
            if (customerData.dancesWhileWaiting && animator != null)
            {
                if (HasParameter(isDancingParam)) animator.SetBool(isDancingParam, false);
                PlayDancingSound(false);
            }

            // [TUTORIAL] Hoàn thành khách đầu tiên
            if (!hasDoneTutorial)
            {
                hasDoneTutorial = true; // Đánh dấu đã xong tut
                if (ObjectiveManager.Instance != null)
                {
                    ObjectiveManager.Instance.ShowObjective("Hãy luôn chú ý những yêu cầu đặc biệt của khách hàng");
                    // Tắt tutorial sau 6 giây
                    StartCoroutine(HideTutorialRoutine(6f));
                }
            }

            if (customerData.postDeliveryDialogNodes != null && customerData.postDeliveryDialogNodes.Count > 0)
            {
                ChangeState(CustomerState.Talking);
                if (DialogManager.Instance != null)
                {
                    DialogManager.Instance.StartDialogSequence(
                        customerData.postDeliveryDialogNodes, 
                        OnPostDialogFinished
                    );
                }
                else
                {
                    FinishAndWalkOut();
                }
            }
            else
            {
                // Khách không nói gì -> Đứng ở dáng Idle chờ 1 giây rồi mới đi
                StartCoroutine(WaitBeforeFinishRoutine(1f));
            }
        }
    }

    IEnumerator WaitBeforeFinishRoutine(float waitTime)
    {
        // Chắc chắn đang đứng im (Idle)
        if (animator != null)
        {
            if (HasParameter(isWalkingParam)) animator.SetBool(isWalkingParam, false);
            if (HasParameter(isAngryParam)) animator.SetBool(isAngryParam, false);
            if (HasParameter(isDancingParam)) animator.SetBool(isDancingParam, false);
        }

        yield return new WaitForSeconds(waitTime);
        FinishAndWalkOut();
    }

    void OnPostDialogFinished(int choiceResult)
    {
        // Tương lai: Xử lý tiền bo sau khi khách ăn xong
        FinishAndWalkOut();
    }

    void FinishAndWalkOut()
    {
        StartCoroutine(FinishAndWalkOutRoutine());
    }

    IEnumerator FinishAndWalkOutRoutine()
    {
        if (customerData != null)
        {
            // Phát tiếng ting ting (trả tiền) nếu khách này có trả tiền
            if (customerData.paysMoney)
            {
                GameObject phoneObj = GameObject.Find("phone");
                if (phoneObj != null)
                {
                    AudioSource audio = phoneObj.GetComponent<AudioSource>();
                    if (audio != null) audio.Play();
                }
            }

            // Văng tiền ra khỏi người (nếu có cài Prefab tiền)
            if (customerData.moneyPrefabToThrow != null)
            {
                // Nếu người chơi có gán điểm thả tiền, tiền sẽ bay đẹp mắt vào đó
                if (moneySpawnPoint != null && moneyDropPoint != null)
                {
                    yield return StartCoroutine(ThrowMoneyRoutine(customerData.moneyPrefabToThrow, customerData.throwAmount));
                    
                    // Chờ tiền bay xong rồi nghỉ nửa giây cho tự nhiên mới quay lưng đi
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    // Chế độ ném tiền vật lý ngẫu nhiên nếu không gán điểm cụ thể
                    for (int i = 0; i < customerData.throwAmount; i++)
                    {
                        // Tạo tiền ở vị trí ngực khách hàng (cao lên 1.2m)
                        Vector3 spawnPos = transform.position + Vector3.up * 1.2f + Random.insideUnitSphere * 0.2f;
                        GameObject moneyObj = Instantiate(customerData.moneyPrefabToThrow, spawnPos, Random.rotation);
                        
                        // Thêm Rigidbody để tiền có vật lý bay rớt
                        Rigidbody rb = moneyObj.GetComponent<Rigidbody>();
                        if (rb == null) rb = moneyObj.AddComponent<Rigidbody>();
                        
                        // BoxCollider nếu chưa có
                        if (moneyObj.GetComponent<Collider>() == null)
                        {
                            moneyObj.AddComponent<BoxCollider>();
                        }

                        rb.mass = 0.5f;
                        rb.linearDamping = 0.5f;

                        // Lực bay: văng lên trên và tỏa ra xung quanh ngẫu nhiên
                        Vector3 randomDir = (transform.forward + Random.insideUnitSphere * 1.5f).normalized;
                        randomDir.y = Mathf.Abs(randomDir.y) + 1f; // Ép hướng bay lồng lên trên
                        
                        float throwForce = Random.Range(4f, 7f);
                        rb.AddForce(randomDir * throwForce, ForceMode.Impulse);
                        
                        // Thêm độ xoáy tự do
                        rb.AddTorque(Random.insideUnitSphere * 20f, ForceMode.Impulse);
                    }
                }
            }

            // Hiện vũng nước có sẵn HOẶC Đẻ ra vũng nước mới
            if (prePlacedPuddle != null)
            {
                prePlacedPuddle.SetActive(true);
            }
            else if (customerData.leavePuddlePrefab != null)
            {
                Vector3 puddlePos = transform.position;
                puddlePos.y += 0.02f; // Nổi lên 1 chút để không bị đè vân bề mặt
                Instantiate(customerData.leavePuddlePrefab, puddlePos, Quaternion.identity);
            }
        }

        if (customerData != null && customerData.leavesAngry)
        {
            StartCoroutine(AngryLeaveRoutine());
        }
        else
        {
            // Tắt trạng thái tức giận (nếu có do đợi lâu) để khách đi bộ bình thường ra cửa!
            isAngry = false;
            if (animator != null && HasParameter(isAngryParam)) animator.SetBool(isAngryParam, false);
            PlayAngrySound(false);

            ChangeState(CustomerState.WalkingOut);
            StartCoroutine(WalkOutRoutine());
        }
    }

    IEnumerator ThrowMoneyRoutine(GameObject moneyPrefab, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Spawn tiền
            GameObject moneyObj = Instantiate(moneyPrefab, moneySpawnPoint.position, Random.rotation);
            
            // Xóa Rigidbody nếu có để bay theo đường Parabol thủ công
            Rigidbody rb = moneyObj.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            // Tính toán vị trí rớt ngẫu nhiên quanh điểm DropPoint (để tiền rải rác trên bàn)
            Vector2 randomCircle = Random.insideUnitCircle * 0.3f;
            Vector3 targetPos = moneyDropPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Cho nó bay trong 0.5 giây
            StartCoroutine(AnimateMoneyFly(moneyObj.transform, moneySpawnPoint.position, targetPos, 0.5f));
            
            // Chờ 1 chút xíu rồi mới văng tờ tiếp theo cho nó liên tục thành dòng
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator AnimateMoneyFly(Transform moneyTrans, Vector3 start, Vector3 end, float duration)
    {
        float time = 0;
        
        // Tạo một đường Parabol đẩy nó nẩy lên trên 1 chút rồi rớt xuống bàn
        float heightOffset = 0.5f; 

        while (time < duration)
        {
            if (moneyTrans == null) yield break;
            
            time += Time.deltaTime;
            float t = time / duration;
            
            // Nội suy vị trí tuyến tính
            Vector3 currentPos = Vector3.Lerp(start, end, t);
            
            // Thêm độ cao Parabol (từ 0 lên 1 rồi về 0)
            float parabola = Mathf.Sin(t * Mathf.PI);
            currentPos.y += parabola * heightOffset;
            
            moneyTrans.position = currentPos;
            
            // Xoay tiền lộn xộn
            moneyTrans.Rotate(Vector3.right * 720f * Time.deltaTime);

            yield return null;
        }

        if (moneyTrans != null)
        {
            moneyTrans.position = end;
            // Xoay nằm bẹp xuống bàn ngẫu nhiên
            moneyTrans.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            
            // Phát tiếng rớt tiền
            if (customerData != null && customerData.moneyDropSound != null)
            {
                AudioSource.PlayClipAtPoint(customerData.moneyDropSound, end);
            }
            
            // Thêm BoxCollider và InteractableObject để người chơi có thể nhặt tiền
            if (moneyTrans.GetComponent<Collider>() == null)
            {
                BoxCollider bc = moneyTrans.gameObject.AddComponent<BoxCollider>();
                // Cho BoxCollider to ra một chút để dễ nhặt
                bc.size = new Vector3(0.5f, 0.1f, 0.5f);
            }
            
            InteractableObject io = moneyTrans.gameObject.GetComponent<InteractableObject>();
            if (io == null) io = moneyTrans.gameObject.AddComponent<InteractableObject>();
            io.type = InteractableType.Money;
        }
    }

    IEnumerator AngryLeaveRoutine()
    {
        isAngry = true;
        PlayAngrySound(true); // Bật âm thanh tức giận
        
        // Cập nhật animator ngay lập tức
        if (animator != null && HasParameter(isAngryParam)) animator.SetBool(isAngryParam, true);
        
        // Đợi 2.5 giây cho hoạt ảnh dậm chân cắn rứt chạy xong
        yield return new WaitForSeconds(2.5f);
        
        isAngry = false;
        if (animator != null && HasParameter(isAngryParam)) animator.SetBool(isAngryParam, false);
        PlayAngrySound(false); // Tắt âm thanh tức giận
        
        ChangeState(CustomerState.WalkingOut);
        StartCoroutine(WalkOutRoutine());
    }

    private bool hasFinishedWalking = false;
    private bool hasFinishedMonologue = false;

    IEnumerator WalkOutRoutine()
    {
        Debug.Log($"[Customer] {customerData?.customerName} đã mua xong và rời đi!");
        
        // Chờ 0.8s để đảm bảo món đồ cuối cùng bay đến tận tay khách hàng (Mất 0.5s bay) rồi mới bắt đầu quay đít đi
        yield return new WaitForSeconds(0.8f);

        hasFinishedWalking = false;
        hasFinishedMonologue = false;

        if (animator != null && HasParameter(isWalkingParam)) animator.SetBool(isWalkingParam, true);

        // Khởi động việc lẩm bẩm song song
        StartCoroutine(MonologueRoutine());

        Transform exitTarget = (customExitPoint != null) ? customExitPoint : startPoint;

        // Đi ngược lại ra ngoài
        if (exitTarget != null)
        {
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.updateRotation = false; // Tắt tự xoay
                agent.speed = walkSpeed;
                agent.isStopped = false;
                agent.SetDestination(exitTarget.position);
                
                // Nới lỏng khoảng cách từ 0.1f lên 0.5f để tránh khách bị kẹt vào tường không thể hoàn thành
                while (agent.pathPending || agent.remainingDistance > 0.5f)
                {
                    yield return null;
                }
                
                agent.isStopped = true;
                agent.ResetPath();
            }
            else
            {
                while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(exitTarget.position.x, exitTarget.position.z)) > 0.5f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, exitTarget.position, walkSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = exitTarget.position;
            }
        }
        
        if (DocumentManager.Instance != null && DocumentManager.Instance.CurrentCustomer == customerData)
        {
            DocumentManager.Instance.SetCurrentCustomer(null);
        }

        hasFinishedWalking = true;
        CheckIfFullyDone();
    }

    IEnumerator MonologueRoutine()
    {
        if (customerData != null && !string.IsNullOrEmpty(customerData.playerReactionAfterCustomerLeaves) && DialogManager.Instance != null)
        {
            // Đợi 1 khoảng thời gian sau khi khách quay lưng đi
            yield return new WaitForSeconds(customerData.delayBeforeReaction);

            Debug.Log($"[Customer] Khách {customerData.customerName} đã đi được {customerData.delayBeforeReaction}s. Bắt đầu tự thoại: {customerData.playerReactionAfterCustomerLeaves}");
            List<DialogNode> nodes = new List<DialogNode>
            {
                new DialogNode { sentence = customerData.playerReactionAfterCustomerLeaves, hasChoices = false }
            };
            
            DialogManager.Instance.StartDialogSequence(nodes, (result) => 
            {
                hasFinishedMonologue = true;
                CheckIfFullyDone();
            });
        }
        else
        {
            hasFinishedMonologue = true;
            CheckIfFullyDone();
        }
    }

    void CheckIfFullyDone()
    {
        if (hasFinishedWalking && hasFinishedMonologue)
        {
            CompleteStep(); // Báo cho SequenceManager biết để gọi sự kiện tiếp theo
            gameObject.SetActive(false); // Ẩn khách đi
        }
    }

    IEnumerator HideTutorialRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.HideObjective();
        }
    }
}
