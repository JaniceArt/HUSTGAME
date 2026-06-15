using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CustomerState
{
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

    [Header("=== TÊN ANIMATION ===")]
    [Tooltip("Tên state đi bộ trong Animator Controller")]
    public string walkAnimName = "Walking";
    
    [Tooltip("Tên state đứng im trong Animator Controller")]
    public string idleAnimName = "Idle";

    [Tooltip("Tên state tức giận (nếu chờ quá lâu)")]
    public string angryAnimName = "Angry Point";

    [Tooltip("Thời gian chờ tối đa (giây) trước khi tức giận. Đặt 0 để không tức giận.")]
    public float patienceTime = 10f;

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Điểm khách hàng bắt đầu đi ra (Vd: Ngoài đường)")]
    public Transform startPoint;
    
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
                    PlayAnim(angryAnimName);
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
    }

    void LateUpdate()
    {
        // HÀM NÀY CHẠY SAU KHI ANIMATOR ĐÃ TÍNH TOÁN XONG!
        // Sẽ đè bẹp mọi góc xoay sai trái của cái Animation!
        
        if (currentState == CustomerState.WalkingIn && counterPoint != null)
        {
            Vector3 direction = (counterPoint.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, yRotationOffset, 0);
        }
        else if ((currentState == CustomerState.WaitingToTalk || currentState == CustomerState.WaitingForOrder || currentState == CustomerState.Talking) && counterPoint != null)
        {
            transform.rotation = counterPoint.rotation * Quaternion.Euler(0, yRotationOffset, 0);
        }
        else if (currentState == CustomerState.WalkingOut && startPoint != null)
        {
            Vector3 direction = (startPoint.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, yRotationOffset, 0);
        }
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

        // Ép Animator nhận đúng parameter mặc định
        if (animator != null) animator.SetInteger("AnimState", 0);

        ChangeState(CustomerState.WalkingIn); // Gọi ở đây để tránh lỗi '-1'
        
        if (startPoint != null && counterPoint != null)
        {
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.Warp(startPoint.position);
                agent.SetDestination(counterPoint.position);
                
                // Đợi cho đến khi tìm xong đường và đi đến nơi
                while (agent.pathPending || agent.remainingDistance > 0.2f)
                {
                    yield return null;
                }
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
            }

            transform.position = counterPoint.position;
        }
        else
        {
            Debug.Log($"[DIAGNOSTICS] Đã vào else block (chờ 2s) vì có 1 biến bị null!");
            yield return new WaitForSeconds(2f);
        }
        
        if (animator != null) animator.Play(idleAnimName);
        ChangeState(CustomerState.WaitingToTalk);
    }

    void ChangeState(CustomerState newState)
    {
        currentState = newState;
        switch (currentState)
        {
            case CustomerState.WalkingIn:
                PlayAnim(walkAnimName);
                PlayWalkingSound(true);
                break;
            case CustomerState.WaitingToTalk:
                PlayAnim(idleAnimName);
                PlayWalkingSound(false);
                break;
            case CustomerState.Talking:
                break;
            case CustomerState.WaitingForOrder:
                PlayAnim((isAngry && !string.IsNullOrEmpty(angryAnimName)) ? angryAnimName : idleAnimName);
                break;
            case CustomerState.WalkingOut:
                PlayAnim(walkAnimName);
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

    private bool checkedAnimState = false;
    private bool hasAnimStateParam = false;

    void PlayAnim(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        
        // Kiểm tra xem Animator này có dùng biến AnimState (cách mới) hay không
        if (!checkedAnimState)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "AnimState")
                {
                    hasAnimStateParam = true;
                    break;
                }
            }
            checkedAnimState = true;
        }

        if (hasAnimStateParam)
        {
            // Dùng Integer parameter "AnimState": 0=Idle, 1=Walking, 2=Angry
            if (stateName == walkAnimName)
                animator.SetInteger("AnimState", 1);
            else if (stateName == angryAnimName)
                animator.SetInteger("AnimState", 2);
            else // Idle hoặc bất kỳ state nào khác
                animator.SetInteger("AnimState", 0);
        }
        else
        {
            // Cách cũ cho Hust Boy và Cosplayer
            animator.CrossFade(stateName, 0.05f, 0, 0f);
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

    void OnDialogFinished(int choiceResult)
    {
        Debug.Log($"[DIALOG - {customerData.customerName}]: Đã nói xong! Lựa chọn: {choiceResult}");
        
        // Tương lai: Có thể lưu biến choiceResult vào Customer để xử lý (thêm tiền bo, v.v.)
        
        ChangeState(CustomerState.WaitingForOrder);
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
            if (dm.LastPrintWasColor == customerData.requiresColor && dm.LastPrintQuantity == customerData.requiredCopies)
            {
                dm.DeliverDocument();
                hasReceivedDocument = true;
                Debug.Log($"<color=green>[Thành công]</color> Khách {customerData.customerName} đã nhận đúng tài liệu!");
            }
            else
            {
                hasWrongOrder = true;
                Debug.LogWarning($"<color=red>[Thất bại]</color> TÀI LIỆU SAI! Khách đòi In Màu: {customerData.requiresColor}, Số lượng: {customerData.requiredCopies}. (Bạn in màu: {dm.LastPrintWasColor}, Số lượng: {dm.LastPrintQuantity})");
            }
        }

        // TH 2: Giao Hộp Xôi
        if (tm != null && tm.isHoldingFood && !hasReceivedFood)
        {
            hasHandedSomething = true;
            bool isCorrect = ValidateFood(tm);
            if (isCorrect)
            {
                tm.DeliverFood();
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
                drinkMan.DeliverDrink();
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
            // Bấm E nhưng không cầm gì trên tay (hoặc cầm món khách đã nhận rồi)
            if ((tm == null || !tm.isHoldingFood) && (dm == null || !dm.IsHoldingDocument) && (drinkMan == null || !drinkMan.isHoldingDrink))
            {
                Debug.LogWarning($"<color=orange>[Nhắc nhở]</color> Bạn ĐANG TAY KHÔNG! Hãy cầm hộp xôi, tài liệu hoặc nước lên tay trước khi bấm Giao hàng.");
            }
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
                FinishAndWalkOut();
            }
        }
    }

    void OnPostDialogFinished(int choiceResult)
    {
        // Tương lai: Xử lý tiền bo sau khi khách ăn xong
        FinishAndWalkOut();
    }

    void FinishAndWalkOut()
    {
        // Phát tiếng ting ting (trả tiền)
        GameObject phoneObj = GameObject.Find("phone");
        if (phoneObj != null)
        {
            AudioSource audio = phoneObj.GetComponent<AudioSource>();
            if (audio != null) audio.Play();
        }

        ChangeState(CustomerState.WalkingOut);
        StartCoroutine(WalkOutRoutine());
    }

    private bool hasFinishedWalking = false;
    private bool hasFinishedMonologue = false;

    IEnumerator WalkOutRoutine()
    {
        Debug.Log($"[Customer] {customerData?.customerName} đã mua xong và rời đi!");
        
        if (animator != null) animator.Play(walkAnimName);
        
        hasFinishedWalking = false;
        hasFinishedMonologue = false;

        // Khởi động việc lẩm bẩm song song
        StartCoroutine(MonologueRoutine());

        // Đi ngược lại ra ngoài đường
        if (startPoint != null)
        {
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = walkSpeed;
                agent.SetDestination(startPoint.position);
                
                while (agent.pathPending || agent.remainingDistance > 0.2f)
                {
                    yield return null;
                }
            }
            else
            {
                while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(startPoint.position.x, startPoint.position.z)) > 0.1f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, startPoint.position, walkSpeed * Time.deltaTime);
                    yield return null;
                }
            }
        }
        
        if (DocumentManager.Instance != null)
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
}
