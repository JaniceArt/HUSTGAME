using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn lên Player.
/// Mỗi frame dùng OverlapSphere tìm object tương tác gần nhất player đang nhìn vào.
/// Bấm E → tương tác thường | Giữ E → đóng hộp xốp / đóng gói tài liệu.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Camera của người chơi")]
    public Camera playerCamera;

    [Tooltip("Khoảng cách tương tác tối đa (mét)")]
    public float interactRange = 3f;

    [Header("UI Prompt")]
    [Tooltip("Panel/GameObject chứa text [E]")]
    public GameObject promptPanel;

    [Tooltip("Text hiện nội dung gợi ý (Ở tâm màn hình)")]
    public TextMeshProUGUI promptText;

    [Tooltip("Text hiển thị hành động (VD: TALK, PICK UP) ở góc màn hình")]
    public TextMeshProUGUI actionHintText;

    [Tooltip("UI Image để hiển thị icon con chuột trái kế bên chữ")]
    public Image actionHintIcon;

    [Tooltip("Text hiển thị hành động phụ (Chuột Phải) ở góc màn hình")]
    public TextMeshProUGUI secondaryActionHintText;

    [Tooltip("UI Image để hiển thị icon con chuột phải kế bên chữ phụ")]
    public Image secondaryActionHintIcon;

    [Tooltip("Ảnh con chuột sáng nút Trái")]
    public Sprite leftClickSprite;

    [Tooltip("Ảnh con chuột sáng nút Phải")]
    public Sprite rightClickSprite;

    [Tooltip("UI hiển thị tâm ngắm (Dấu + hoặc Dấu chấm)")]
    public GameObject crosshairUI;

    [Tooltip("Độ cao UI nổi lên phía trên object (mét)")]
    public float promptHeightOffset = 0f;

    [Header("Hold E - Đóng hộp")]
    [Tooltip("Thời gian giữ E để đóng hộp (giây)")]
    public float holdDuration = 2f;

    [Tooltip("Image vòng tròn progress (Image Type = Filled, Radial 360)")]
    public Image holdProgressRing;

    [Header("Hold E - Âm thanh")]
    public AudioClip packingSound;
    [Range(0f, 1f)] public float packingVolume = 1f;
    [Range(0.1f, 3f)] public float packingPitch = 1f;
    private AudioSource packingAudioSource;

    [Header("Âm thanh khác")]
    [Tooltip("Âm thanh khi vứt đồ vào thùng rác")]
    public AudioClip trashSound;
    [Range(0f, 1f)] public float trashVolume = 0.8f;

    // RectTransform để di chuyển vị trí
    private RectTransform promptRect;

    // Object đang nhìn vào
    private InteractableObject currentTarget;
    private Collider currentCollider;

    // Hold E timer
    private float holdTimer = 0f;
    private bool isHolding = false;
    private GameObject _ringBackground; // Vòng nền xám mờ

    private bool wasDialogActiveLastFrame = false;

    // -------------------------------------------------------

    void Awake()
    {
        if (promptPanel != null)
        {
            promptRect = promptPanel.GetComponent<RectTransform>();
            // Ép tâm và mỏ neo về chính giữa để tránh mọi lỗi lệch UI
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.pivot     = new Vector2(0.5f, 0.5f);
        }

        // Tìm đúng camera ĐANG RENDER game (cùng camera mà FirstPersonController dùng)
        playerCamera = GetComponentInChildren<Camera>();

        // Tự động tạo Progress Ring nếu chưa có
        if (holdProgressRing == null && promptPanel != null)
            CreateProgressRing();

        SetProgressRing(0f);

        packingAudioSource = gameObject.AddComponent<AudioSource>();
        packingAudioSource.playOnAwake = false;
        packingAudioSource.loop = true;
    }

    /// <summary>
    /// Tự tạo vòng tròn tiến trình bằng code, không cần kéo thả trong Inspector.
    /// Tự vẽ hình tròn bằng Texture2D vì sprite Knob không hoạt động trên Unity 6.
    /// </summary>
    void CreateProgressRing()
    {
        // === Tạo texture hình tròn (ring shape) ===
        int size = 128;
        Texture2D circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float outerRadius = center - 1f;
        float innerRadius = outerRadius - 14f; // Độ dày vòng tròn

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerRadius && dist >= innerRadius)
                    circleTex.SetPixel(x, y, Color.white);
                else
                    circleTex.SetPixel(x, y, Color.clear);
            }
        }
        circleTex.Apply();

        Sprite circleSprite = Sprite.Create(circleTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        // === Vòng nền (viền xám mờ) ===
        GameObject bgObj = new GameObject("RingBackground");
        bgObj.transform.SetParent(promptPanel.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = circleSprite;
        bgImage.type = Image.Type.Simple;
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.4f); // Xám mờ

        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot     = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(80, 80);
        bgRt.anchoredPosition = new Vector2(0, -45f);

        // === Vòng tiến trình (xanh lá, fill radial) ===
        GameObject ringObj = new GameObject("HoldProgressRing");
        ringObj.transform.SetParent(promptPanel.transform, false);

        holdProgressRing = ringObj.AddComponent<Image>();
        holdProgressRing.sprite = circleSprite;
        holdProgressRing.type   = Image.Type.Filled;
        holdProgressRing.fillMethod = Image.FillMethod.Radial360;
        holdProgressRing.fillOrigin = (int)Image.Origin360.Top;
        holdProgressRing.fillClockwise = true;
        holdProgressRing.fillAmount = 0f;
        holdProgressRing.color = new Color(0.3f, 1f, 0.3f, 0.95f); // Xanh lá sáng

        RectTransform rt = ringObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(80, 80);
        rt.anchoredPosition = new Vector2(0, -45f);

        // Lưu reference để ẩn/hiện cùng ring
        _ringBackground = bgObj;
        ringObj.SetActive(false);
        bgObj.SetActive(false);
    }

    void Update()
    {
        bool isDialogActive = DialogManager.Instance != null && DialogManager.Instance.dialogPanel != null && DialogManager.Instance.dialogPanel.activeInHierarchy && !DialogManager.Instance.AllowInteraction;
        
        // Ngăn chặn Click "lây lan" từ lúc bấm tắt Hội thoại sang Interaction (Same-frame input bleed)
        if (wasDialogActiveLastFrame && !isDialogActive)
        {
            wasDialogActiveLastFrame = isDialogActive;
            return; // Bỏ qua Update frame này vì hội thoại vừa mới tắt tức thì
        }
        wasDialogActiveLastFrame = isDialogActive;

        // Ẩn Interaction và Prompt nếu người chơi bị khoá di chuyển (Ví dụ: Lúc chuyển cảnh, ngủ)
        if (!FirstPersonController.CanMove)
        {
            if (promptPanel != null && promptPanel.activeSelf) promptPanel.SetActive(false);
            if (actionHintText != null && actionHintText.gameObject.activeSelf) actionHintText.gameObject.SetActive(false);
            if (actionHintIcon != null && actionHintIcon.gameObject.activeSelf) actionHintIcon.gameObject.SetActive(false);
            if (currentTarget != null) currentTarget = null;
            return;
        }

        ScanForInteractable();

        HandleInput();
        HandleVInput();

        UpdateActionHint();
    }

    void HandleVInput()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        // Bấm Chuột Phải để ĂN xôi (bất kể đang nhìn đi đâu)
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (ToppingManager.Instance != null && ToppingManager.Instance.isHoldingFood)
            {
                ToppingManager.Instance.EatFood();
            }
        }
    }

    // ===================== INPUT =====================

    void HandleInput()
    {
        // Chặn tương tác nếu đang trong hội thoại hoặc đang soi giấy
        bool isDialogActive = DialogManager.Instance != null && DialogManager.Instance.dialogPanel != null && DialogManager.Instance.dialogPanel.activeInHierarchy && !DialogManager.Instance.AllowInteraction;
        bool isViewingDoc = DocumentManager.Instance != null && DocumentManager.Instance.IsViewingDocument;
        
        if (isDialogActive || isViewingDoc)
        {
            ResetHold();
            return;
        }

        if (Keyboard.current == null || currentTarget == null) 
        {
            ResetHold();
            return;
        }

        bool ePressed = Mouse.current.leftButton.isPressed;

        // (Đã bỏ phím E nhặt tài liệu trên bàn, người chơi bắt buộc bấm V để xem trước)

        // --- Press E cho CloseBox ---
        if (currentTarget.type == InteractableType.CloseBox)
        {
            ToppingManager tm = ToppingManager.Instance;
            if (tm != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (!tm.IsBoxClosed)
                    {
                        tm.CloseBox();
                    }
                    else
                    {
                        tm.PickUpFood();
                    }
                    HidePrompt();
                }
            }
            return;
        }

        // --- Bấm chuột trái để nhặt tiền ---
        if (currentTarget.type == InteractableType.Money)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Phát tiếng nhặt (có thể dùng chung tiếng thùng rác tạm thời)
                if (trashSound != null) AudioSource.PlayClipAtPoint(trashSound, currentTarget.transform.position, trashVolume);
                
                Destroy(currentTarget.gameObject);
                HidePrompt();
            }
            return;
        }

        // --- Hold E cho PrintedPaper (đóng gói tài liệu) ---
        if (currentTarget.type == InteractableType.PrintedPaper)
        {
            float duration = DocumentManager.Instance != null 
                ? DocumentManager.Instance.PackageHoldDuration 
                : 5f;

            if (ePressed)
            {
                if (!packingAudioSource.isPlaying && packingSound != null)
                {
                    packingAudioSource.clip = packingSound;
                    packingAudioSource.volume = packingVolume;
                    packingAudioSource.pitch = packingPitch;
                    packingAudioSource.Play();
                }

                isHolding = true;
                holdTimer += Time.deltaTime;
                SetProgressRing(holdTimer / duration);

                if (holdTimer >= duration)
                {
                    if (packingAudioSource.isPlaying) packingAudioSource.Stop();
                    PrintedPaper paper = currentTarget.GetComponent<PrintedPaper>();
                    if (paper != null) paper.PackageDocument();
                    ResetHold();
                    HidePrompt();
                }
            }
            else
            {
                ResetHold();
            }
            return;
        }

        // --- Hold E cho Stain (Vết bẩn) ---
        if (currentTarget.type == InteractableType.Stain)
        {
            if ((CleaningTaskStep.Instance != null && CleaningTaskStep.Instance.IsHoldingBroom) ||
                (PuddleCleaningTaskStep.Instance != null && PuddleCleaningTaskStep.Instance.IsHoldingBroom))
            {
                float duration = 2.0f; // Cần 2s để chà xong 1 vết
                if (ePressed)
                {
                    isHolding = true;
                    holdTimer += Time.deltaTime;
                    SetProgressRing(holdTimer / duration);
                    
                    // Phát âm thanh chà chổi (nếu có)
                    if (CleaningTaskStep.Instance != null) CleaningTaskStep.Instance.PlayScrubSound();
                    if (PuddleCleaningTaskStep.Instance != null) PuddleCleaningTaskStep.Instance.PlayScrubSound();

                    if (holdTimer >= duration)
                    {
                        if (CleaningTaskStep.Instance != null) CleaningTaskStep.Instance.CleanStain(currentTarget.gameObject);
                        if (PuddleCleaningTaskStep.Instance != null) PuddleCleaningTaskStep.Instance.CleanStain(currentTarget.gameObject);
                        
                        ResetHold();
                        HidePrompt();
                    }
                }
                else
                {
                    ResetHold();
                }
            }
            return;
        }

        // --- Bấm Chuột trái thường cho các action khác ---
        ResetHold(); // Không phải Hold Action thì không giữ
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleInteraction(currentTarget);
        }
    }

    void ResetHold()
    {
        holdTimer = 0f;
        isHolding = false;
        SetProgressRing(0f);
        if (packingAudioSource != null && packingAudioSource.isPlaying)
        {
            packingAudioSource.Stop();
        }
        if (CleaningTaskStep.Instance != null)
        {
            CleaningTaskStep.Instance.StopScrubSound();
        }
    }

    // ===================== RAYCAST (XUYÊN VẬT THỂ RÁC) =====================

    void ScanForInteractable()
    {
        InteractableObject bestTarget = null;
        Collider bestCollider = null;
        float minDistance = float.MaxValue;

        // Phóng tia ray xa 10 mét để quét mọi thứ, BAO GỒM CẢ TRIGGER
        RaycastHit[] hits = Physics.RaycastAll(playerCamera.transform.position, playerCamera.transform.forward, 10f, Physics.AllLayers, QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            InteractableObject obj = hit.collider.GetComponentInParent<InteractableObject>();

            // Tìm vật thể Tương tác được gần nhất
            if (obj != null && CanInteract(obj))
            {
                // Giới hạn tầm với tùy theo loại vật thể
                float maxAllowedDist = interactRange;
                
                // Đặc quyền tay dài: Vết bẩn, Phi cơ giấy được phép nhặt/quét từ xa (7m)
                if (obj.type == InteractableType.Stain || obj.type == InteractableType.PaperAirplane)
                {
                    maxAllowedDist = 7f;
                }
                else if (obj.type == InteractableType.GhostPaper)
                {
                    maxAllowedDist = 5f; // Giấy tâm linh cho phép quét từ 5 mét
                }
                else if (obj.type == InteractableType.Speaker)
                {
                    maxAllowedDist = 10f; // Loa cho phép bấm từ xa 10 mét
                }
                else if (obj.type == InteractableType.Phone)
                {
                    maxAllowedDist = 2.5f; // Điện thoại chỉ được bắt máy khi đứng thật gần (2.5 mét)
                }

                if (hit.distance <= maxAllowedDist && hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    bestTarget = obj;
                    bestCollider = hit.collider;
                }
            }
        }

        if (bestTarget != null)
        {
            // Nếu target thay đổi thì reset hold
            if (currentTarget != bestTarget) ResetHold();

            currentTarget = bestTarget;
            currentCollider = bestCollider;
            ShowPrompt(GetPromptText(bestTarget));
        }
        else
        {
            currentTarget = null;
            currentCollider = null;
            ResetHold();
            HidePrompt();
        }
    }

    // ===================== ĐIỀU KIỆN =====================

    private bool IsHoldingAnyDeliverable()
    {
        bool holdingDoc = DocumentManager.Instance != null && DocumentManager.Instance.IsHoldingDocument;
        bool holdingFood = ToppingManager.Instance != null && ToppingManager.Instance.isHoldingFood;
        bool holdingDrink = DrinkManager.Instance != null && DrinkManager.Instance.isHoldingDrink;
        return holdingDoc || holdingFood || holdingDrink;
    }

    bool CanInteract(InteractableObject obj)
    {
        // Cửa kéo không cần ToppingManager — xử lý riêng
        if (obj.type == InteractableType.SlidingDoor)
        {
            SlidingDoor door = obj.GetComponent<SlidingDoor>();
            return door != null && !door.IsMoving;
        }

        // Máy in: chỉ cho phép in nếu có khách hàng đang yêu cầu
        if (obj.type == InteractableType.Printer)
        {
            if (DocumentManager.Instance != null)
            {
                CustomerData cust = DocumentManager.Instance.CurrentCustomer;
                if (cust != null && cust.needsDocument)
                {
                    // 1. Phải nghe order rồi mới được in
                    Customer activeCustomer = UnityEngine.Object.FindObjectOfType<Customer>();
                    if (activeCustomer != null && !activeCustomer.HasToldOrder)
                    {
                        return false;
                    }

                    // 2. Nếu khách mang USB, bắt buộc phải nhặt cắm vào rồi mới được in
                    if (cust.hasUsb && !DocumentManager.Instance.HasPluggedInUsb)
                    {
                        return false;
                    }

                    return true; // Thoả mãn mọi điều kiện -> Hiện chữ "Máy in"
                }
            }
            return false;
        }

        // Tờ giấy đã in: chỉ tương tác khi chưa cầm đồ gì khác
        if (obj.type == InteractableType.PrintedPaper)
        {
            DocumentManager dm = DocumentManager.Instance;
            return dm != null && !IsHoldingAnyDeliverable();
        }

        // Khách hàng: chỉ tương tác khi đang đợi nói chuyện hoặc đợi giao hàng (và cầm đúng đồ)
        if (obj.type == InteractableType.Customer)
        {
            Customer customer = obj.GetComponent<Customer>();
            if (customer == null) return false;

            // Đang nói chuyện thì không hiện Prompt gì cả (để UI Hội Thoại tự lo)
            if (customer.CurrentState == CustomerState.WaitingToTalk)
                return true;

            if (customer.CurrentState == CustomerState.WaitingForOrder)
            {
                return true; // Luôn cho phép bấm vào khách hàng để giao hàng HOẶC để họ nhắc lại Order
            }

            return false;
        }

        if (obj.type == InteractableType.Poster)
        {
            return true; // Luôn cho phép bấm vào tranh ảnh để xem
        }

        // Máy bay giấy: luôn cho phép nhặt
        if (obj.type == InteractableType.PaperAirplane)
        {
            return true;
        }

        // Khóa không cho làm đồ ăn nếu không có khách đang đói
        bool canCook = true;
        if (DocumentManager.Instance != null)
        {
            CustomerData cust = DocumentManager.Instance.CurrentCustomer;
            if (cust == null || !cust.needsFood)
            {
                canCook = false;
            }
        }

        switch (obj.type)
        {
            case InteractableType.FoamBoxStack:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && !ToppingManager.Instance.HasFoamBox && canCook;

            case InteractableType.StickyRicePot:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && ToppingManager.Instance.HasFoamBox && !ToppingManager.Instance.HasRice && canCook;

            case InteractableType.PateBowl:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && ToppingManager.Instance.HasRice && !ToppingManager.Instance.HasPate && canCook;

            case InteractableType.EggBox:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && ToppingManager.Instance.HasRice && !ToppingManager.Instance.eggOnPan && !ToppingManager.Instance.HasEgg && canCook;

            case InteractableType.Pan:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && ToppingManager.Instance.eggOnPan && ToppingManager.Instance.EggIsReady && canCook;

            case InteractableType.SausageBowl:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && ToppingManager.Instance.HasRice && !ToppingManager.Instance.HasSausage && canCook;

            case InteractableType.CucumberBowl:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && ToppingManager.Instance.HasRice && !ToppingManager.Instance.HasCucumber && canCook;

            case InteractableType.KetchupBox:
                return ToppingManager.Instance != null && !ToppingManager.Instance.IsBoxClosed && ToppingManager.Instance.HasRice && !ToppingManager.Instance.HasKetchup && canCook;

            // Đóng hộp: cho phép nếu chưa đóng (Hold E) hoặc đã đóng nhưng chưa nhặt (Press E)
            case InteractableType.CloseBox:
                if (!canCook || ToppingManager.Instance == null) return false;
                if (!ToppingManager.Instance.IsBoxClosed) return ToppingManager.Instance.HasRice;
                else return !IsHoldingAnyDeliverable(); // Không cho nhặt nếu đang cầm đồ khác

            // Cửa kéo: luôn có thể tương tác (không phụ thuộc vào cooking flow)
            case InteractableType.SlidingDoor:
                SlidingDoor door = obj.GetComponent<SlidingDoor>();
                return door != null && !door.IsMoving;

            case InteractableType.PackedPaperOnTable:
                return DocumentManager.Instance != null && DocumentManager.Instance.IsDocumentPackedOnTable && !IsHoldingAnyDeliverable();

            case InteractableType.FridgeDoor:
                return true; // Luôn cho phép mở tủ lạnh

            case InteractableType.Drink:
                if (IsHoldingAnyDeliverable()) return false; // Không cầm 2 vật trên tay cùng lúc
                
                // Tủ lạnh đóng thì không cho lấy nước
                HingeDoor fridgeDoor = UnityEngine.Object.FindObjectOfType<HingeDoor>();
                if (fridgeDoor != null && !fridgeDoor.IsOpen) return false; 
                
                return true;

            case InteractableType.TrashCan:
                bool holdingDoc = DocumentManager.Instance != null && DocumentManager.Instance.IsHoldingDocument;
                bool holdingDrink = DrinkManager.Instance != null && DrinkManager.Instance.isHoldingDrink;
                return holdingDoc || holdingDrink;

            case InteractableType.UsbDrive:
                return true; // Luôn cho phép tương tác với USB

            case InteractableType.Broom:
                if (CleaningTaskStep.Instance != null)
                {
                    if (!CleaningTaskStep.Instance.IsHoldingBroom) return !IsHoldingAnyDeliverable();
                    return false; // Đang cầm chổi thì không được bấm vào cái chổi nữa (tránh lỗi che camera)
                }
                if (PuddleCleaningTaskStep.Instance != null)
                {
                    if (!PuddleCleaningTaskStep.Instance.IsHoldingBroom) return !IsHoldingAnyDeliverable();
                    return false; // Đang cầm chổi thì không được bấm vào cái chổi nữa (tránh lỗi che camera)
                }
                if (Day4EndCutsceneStep.Instance != null && Day4EndCutsceneStep.Instance.isWaitingForBroom) return true;
                return false;

            case InteractableType.Stain:
                if (CleaningTaskStep.Instance != null && CleaningTaskStep.Instance.IsHoldingBroom) return true;
                if (PuddleCleaningTaskStep.Instance != null && PuddleCleaningTaskStep.Instance.IsHoldingBroom) return true;
                return false;

            case InteractableType.BroomArea:
                if (CleaningTaskStep.Instance != null && CleaningTaskStep.Instance.IsHoldingBroom && CleaningTaskStep.Instance.AllStainsCleaned) return true;
                if (PuddleCleaningTaskStep.Instance != null && PuddleCleaningTaskStep.Instance.IsHoldingBroom && PuddleCleaningTaskStep.Instance.AllStainsCleaned) return true;
                return false;

            case InteractableType.Speaker:
            case InteractableType.Money:
            case InteractableType.GhostPaper:
            case InteractableType.HellMoney:
                return true;

            case InteractableType.Phone:
                bool isNormalPhoneWaiting = NormalPhoneCallStep.Instance != null && NormalPhoneCallStep.Instance.IsWaitingForAnswer;
                bool isGhostPhoneWaiting = PhoneCallEventStep.Instance != null && PhoneCallEventStep.Instance.IsWaitingForAnswer;
                return isNormalPhoneWaiting || isGhostPhoneWaiting;

            default:
                return false;
        }
    }

    // ===================== XỬ LÝ TƯƠNG TÁC =====================

    void HandleInteraction(InteractableObject obj)
    {
        ToppingManager tm = ToppingManager.Instance;
        if (tm == null) return;

        switch (obj.type)
        {
            case InteractableType.FoamBoxStack:   tm.TakeFoamBox();      break;
            case InteractableType.StickyRicePot:  tm.AddRice();          break;
            case InteractableType.PateBowl:       tm.AddPate();          break;
            case InteractableType.EggBox:         tm.SpawnEggOnPan();    break;
            case InteractableType.Pan:            tm.TakeEggFromPan();   break;
            case InteractableType.SausageBowl:    tm.AddSausage();       break;
            case InteractableType.CucumberBowl:   tm.AddCucumber();      break;
            case InteractableType.KetchupBox:     tm.AddKetchup();       break;

            case InteractableType.SlidingDoor:
                SlidingDoor door = obj.GetComponent<SlidingDoor>();
                if (door != null) door.Interact();
                return; // Không HidePrompt — để player thấy có thể đóng lại

            case InteractableType.Printer:
                if (DocumentManager.Instance != null && DocumentManager.Instance.CurrentCustomer != null)
                {
                    CustomerData cust = DocumentManager.Instance.CurrentCustomer;
                    if (cust.hasUsb && !DocumentManager.Instance.HasPluggedInUsb)
                    {
                        Debug.LogWarning("Phải cắm USB vào máy tính trước!");
                        break;
                    }
                }

                PrintCanvasUI printerUI = obj.GetComponent<PrintCanvasUI>();
                if (printerUI != null) printerUI.OpenPrintCanvas();
                break;

            case InteractableType.Customer:
                Customer customer = obj.GetComponent<Customer>();
                if (customer != null) customer.Interact();
                break;

            case InteractableType.Speaker:
                FindSpeakerStep step = UnityEngine.Object.FindObjectOfType<FindSpeakerStep>();
                if (step != null) step.TurnOffSpeaker();
                break;

            case InteractableType.Phone:
                // Quét TẤT CẢ các event điện thoại đang bật trong scene
                PhoneCallEventStep[] scaryPhoneSteps = UnityEngine.Object.FindObjectsOfType<PhoneCallEventStep>();
                foreach (var s in scaryPhoneSteps) s.AnswerPhone();
                
                NormalPhoneCallStep[] normalPhoneSteps = UnityEngine.Object.FindObjectsOfType<NormalPhoneCallStep>();
                foreach (var s in normalPhoneSteps) s.AnswerPhone();
                break;

            case InteractableType.GhostPaper:
                LaughingGhostEventStep ghostStep = UnityEngine.Object.FindObjectOfType<LaughingGhostEventStep>();
                if (ghostStep != null) ghostStep.ReadPaper();
                break;

            case InteractableType.HellMoney:
                LaughingGhostEventStep hellMoneyStep = UnityEngine.Object.FindObjectOfType<LaughingGhostEventStep>();
                if (hellMoneyStep != null) hellMoneyStep.ThrowHellMoney();
                break;

            case InteractableType.Poster:
                PosterItem poster = obj.GetComponent<PosterItem>();
                if (poster != null) poster.Inspect();
                break;

            case InteractableType.PaperAirplane:
                PaperAirplaneStep plane = obj.GetComponent<PaperAirplaneStep>();
                if (plane != null)
                {
                    if (!plane.HasViewed)
                    {
                        plane.ViewPaper();
                    }
                    else
                    {
                        plane.ThrowAway();
                    }
                }
                break;

            case InteractableType.FridgeDoor:
                HingeDoor fridgeDoor = obj.GetComponent<HingeDoor>();
                if (fridgeDoor != null) fridgeDoor.Interact();
                return; // Không ẩn chữ Gợi ý ngay, để người chơi thấy có thể đóng/mở lại

            case InteractableType.Drink:
                DrinkItem drink = obj.GetComponent<DrinkItem>();
                if (drink != null) drink.Interact();
                break;

            case InteractableType.TrashCan:
                bool threwSomething = false;
                if (DocumentManager.Instance != null && DocumentManager.Instance.IsHoldingDocument)
                {
                    DocumentManager.Instance.TrashDocument();
                    threwSomething = true;
                }
                else if (DrinkManager.Instance != null && DrinkManager.Instance.isHoldingDrink)
                {
                    DrinkManager.Instance.DropDrink(); // DrinkManager.DropDrink chỉ ẩn chai nước đi, dùng luôn được
                    threwSomething = true;
                }

                if (threwSomething && trashSound != null && packingAudioSource != null)
                {
                    packingAudioSource.PlayOneShot(trashSound, trashVolume);
                }
                break;

            case InteractableType.Broom:
                if (CleaningTaskStep.Instance != null)
                {
                    if (!CleaningTaskStep.Instance.IsHoldingBroom) CleaningTaskStep.Instance.PickUpBroom();
                    else if (CleaningTaskStep.Instance.AllStainsCleaned) CleaningTaskStep.Instance.ReturnBroom();
                }
                if (PuddleCleaningTaskStep.Instance != null)
                {
                    if (!PuddleCleaningTaskStep.Instance.IsHoldingBroom) PuddleCleaningTaskStep.Instance.PickUpBroom();
                    else if (PuddleCleaningTaskStep.Instance.AllStainsCleaned) PuddleCleaningTaskStep.Instance.ReturnBroom();
                }
                if (Day4EndCutsceneStep.Instance != null && Day4EndCutsceneStep.Instance.isWaitingForBroom)
                {
                    Day4EndCutsceneStep.Instance.OnBroomPickedUp();
                }
                break;

            case InteractableType.BroomArea:
                if (CleaningTaskStep.Instance != null && CleaningTaskStep.Instance.IsHoldingBroom && CleaningTaskStep.Instance.AllStainsCleaned)
                {
                    CleaningTaskStep.Instance.ReturnBroom();
                }
                if (PuddleCleaningTaskStep.Instance != null && PuddleCleaningTaskStep.Instance.IsHoldingBroom && PuddleCleaningTaskStep.Instance.AllStainsCleaned)
                {
                    PuddleCleaningTaskStep.Instance.ReturnBroom();
                }
                break;

            case InteractableType.UsbDrive:
                // Biến mất USB
                Destroy(obj.gameObject);
                if (DocumentManager.Instance != null)
                {
                    DocumentManager.Instance.HasPluggedInUsb = true;
                }
                Debug.Log("<color=green>[USB]</color> Đã cắm USB vào máy tính!");
                break;

            case InteractableType.PackedPaperOnTable:
                DocumentManager.Instance.ViewPackedPaperDirectly();
                break;
        }

        HidePrompt();
    }

    // ===================== TEXT GỢI Ý =====================

    string GetPromptText(InteractableObject obj)
    {
        switch (obj.type)
        {
            case InteractableType.FoamBoxStack:   return "Hộp xốp";
            case InteractableType.StickyRicePot:  return "Nồi xôi";
            case InteractableType.PateBowl:       return "Patê";
            case InteractableType.EggBox:         return "Khay trứng";
            case InteractableType.Pan:            return "Chảo";
            case InteractableType.SausageBowl:    return "Xúc xích";
            case InteractableType.CucumberBowl:   return "Dưa leo";
            case InteractableType.KetchupBox:     return "Kết chúp";
            case InteractableType.CloseBox:       return "Hộp xôi";
            case InteractableType.SlidingDoor:    return "Cửa ra vào";
            case InteractableType.Printer:        return "Máy in";
            case InteractableType.PrintedPaper:   return "Tài liệu";
            case InteractableType.PackedPaperOnTable: return "Tài liệu đóng gói";
            case InteractableType.Customer:       return "Khách hàng";
            case InteractableType.PaperAirplane:  return "Giấy";
            case InteractableType.FridgeDoor:     return "Tủ lạnh";
            case InteractableType.Drink:          return "Nước uống";
            case InteractableType.TrashCan:       return "Thùng rác";
            case InteractableType.UsbDrive:       return "USB";
            case InteractableType.Broom:          return "Cây chổi";
            case InteractableType.Speaker:        return "Loa";
            case InteractableType.Phone:          return "Điện thoại";
            case InteractableType.BroomArea:      return "Chỗ cất chổi";
            case InteractableType.Poster:         return "Xem ảnh";
            case InteractableType.Stain:
                if (obj.gameObject.name.ToLower().Contains("puddle") || obj.gameObject.name.ToLower().Contains("nước") || obj.gameObject.name.ToLower().Contains("nuoc"))
                    return "Vết nước";
                return "Vết bẩn";
            case InteractableType.Money:          return "Tiền";
            case InteractableType.GhostPaper:     return "Tờ giấy lạ";
            case InteractableType.HellMoney:      return "Tiền âm phủ";
            default:                              return "Vật thể";
        }
    }

    // ===================== UI =====================

    string GetActionHintText(InteractableObject obj)
    {
        switch (obj.type)
        {
            case InteractableType.Customer:
                Customer cust = obj.GetComponent<Customer>();
                if (cust != null && cust.CurrentState == CustomerState.WaitingForOrder)
                {
                    if (IsHoldingAnyDeliverable()) return "Giao hàng";
                    return "Hỏi lại";
                }
                return "Nói chuyện";
                
            case InteractableType.EggBox:
                return "Rán trứng";
                
            case InteractableType.StickyRicePot:
            case InteractableType.PateBowl:
            case InteractableType.SausageBowl:
            case InteractableType.CucumberBowl:
            case InteractableType.KetchupBox:
            case InteractableType.Pan:
                return "Thêm";

            case InteractableType.SlidingDoor:
            case InteractableType.FridgeDoor:
                return "Đóng / Mở";

            case InteractableType.TrashCan:
                return "Vứt";

            case InteractableType.PaperAirplane:
                PaperAirplaneStep planeStep = obj.GetComponent<PaperAirplaneStep>();
                if (planeStep != null && !planeStep.HasViewed) return "Xem";
                return "Vứt rác";

            case InteractableType.GhostPaper:
                return "Xem";

            case InteractableType.HellMoney:
                return "Vứt";

            case InteractableType.Stain:
                return "Dọn (Giữ)";

            case InteractableType.CloseBox:
                ToppingManager tm = ToppingManager.Instance;
                if (tm != null && !tm.IsBoxClosed) return "Đóng gói";
                return "Lấy";

            case InteractableType.PrintedPaper:
                return "Gói (Giữ)";
            case InteractableType.PackedPaperOnTable:
                return "Xem";

            case InteractableType.FoamBoxStack:
            case InteractableType.Drink:
            case InteractableType.UsbDrive:
            case InteractableType.Money:
                return "Nhặt";

            case InteractableType.Speaker:
                return "Tắt loa";

            case InteractableType.Phone:
                bool isNormalPhoneWaiting = NormalPhoneCallStep.Instance != null && NormalPhoneCallStep.Instance.IsWaitingForAnswer;
                bool isGhostPhoneWaiting = PhoneCallEventStep.Instance != null && PhoneCallEventStep.Instance.IsWaitingForAnswer;
                
                if (isNormalPhoneWaiting || isGhostPhoneWaiting)
                {
                    return "Nghe";
                }
                return string.Empty;

            case InteractableType.BroomArea:
                return "Cất chổi";

            case InteractableType.Broom:
                if (CleaningTaskStep.Instance != null && CleaningTaskStep.Instance.IsHoldingBroom) return "Cất chổi";
                if (PuddleCleaningTaskStep.Instance != null && PuddleCleaningTaskStep.Instance.IsHoldingBroom) return "Cất chổi";
                if (Day4EndCutsceneStep.Instance != null && Day4EndCutsceneStep.Instance.isWaitingForBroom) return "Nhặt chổi";
                return "Nhặt chổi";

            case InteractableType.Printer:
                return "Sử dụng";

            default:
                return "Tương tác";
        }
    }

    public void ShowPrompt(string text)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText  != null) promptText.text = text;
        if (crosshairUI != null) crosshairUI.SetActive(false); // Ẩn tâm ngắm
    }

    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
        if (crosshairUI != null) crosshairUI.SetActive(true); // Hiện lại tâm ngắm
    }

    void UpdateActionHint()
    {
        // === HÀNH ĐỘNG CHÍNH (CHUỘT TRÁI) ===
        if (actionHintText != null)
        {
            if (currentTarget != null)
            {
                actionHintText.gameObject.SetActive(true);
                actionHintText.text = GetActionHintText(currentTarget);
                if (actionHintIcon != null)
                {
                    actionHintIcon.gameObject.SetActive(true);
                    actionHintIcon.sprite = leftClickSprite;
                }
            }
            else
            {
                actionHintText.gameObject.SetActive(false);
                if (actionHintIcon != null) actionHintIcon.gameObject.SetActive(false);
            }
        }

        // === HÀNH ĐỘNG PHỤ (CHUỘT PHẢI) ===
        if (secondaryActionHintText != null)
        {
            bool holdingFood = ToppingManager.Instance != null && ToppingManager.Instance.isHoldingFood;
            
            // Nếu đang cầm đồ ăn, hiện Chuột Phải = Ăn Xôi
            if (holdingFood)
            {
                secondaryActionHintText.gameObject.SetActive(true);
                secondaryActionHintText.text = "ĂN XÔI";
                if (secondaryActionHintIcon != null)
                {
                    secondaryActionHintIcon.gameObject.SetActive(true);
                    secondaryActionHintIcon.sprite = rightClickSprite;
                }
            }
            else
            {
                secondaryActionHintText.gameObject.SetActive(false);
                if (secondaryActionHintIcon != null) secondaryActionHintIcon.gameObject.SetActive(false);
            }
        }
    }

    void SetProgressRing(float value)
    {
        if (holdProgressRing != null)
        {
            holdProgressRing.fillAmount = Mathf.Clamp01(value);
            holdProgressRing.gameObject.SetActive(value > 0f);
        }
        if (_ringBackground != null)
        {
            _ringBackground.SetActive(value > 0f);
        }
    }

    void UpdatePromptPosition(Vector3 worldPos)
    {
        if (promptRect == null || playerCamera == null) return;

        // Hiện chữ chính giữa vật thể — không cộng thêm offset nào
        Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);

        // Z < 0 nghĩa là vật thể nằm sau lưng camera
        if (screenPos.z < 0)
        {
            promptPanel.SetActive(false);
            return;
        }

        Canvas canvas = promptPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Cách tính đơn giản nhất: chuẩn hoá toạ độ màn hình (0..1) rồi nhân với kích thước Canvas
        // canvasRect.rect.width/height luôn trả về kích thước đã tính Canvas Scaler
        float canvasW = canvasRect.rect.width;
        float canvasH = canvasRect.rect.height;

        // Anchor đã được ép về (0.5, 0.5) nên (0,0) = giữa Canvas
        Vector2 anchoredPos = new Vector2(
            (screenPos.x / Screen.width  - 0.5f) * canvasW,
            (screenPos.y / Screen.height - 0.5f) * canvasH
        );

        promptRect.anchoredPosition = anchoredPos;
    }
}
