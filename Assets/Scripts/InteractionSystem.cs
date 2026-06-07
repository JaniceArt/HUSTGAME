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

    [Tooltip("Text hiện nội dung gợi ý")]
    public TextMeshProUGUI promptText;

    [Tooltip("Độ cao UI nổi lên phía trên object (mét)")]
    public float promptHeightOffset = 0f;

    [Header("Hold E - Đóng hộp")]
    [Tooltip("Thời gian giữ E để đóng hộp (giây)")]
    public float holdDuration = 2f;

    [Tooltip("Image vòng tròn progress (Image Type = Filled, Radial 360)")]
    public Image holdProgressRing;

    // RectTransform để di chuyển vị trí
    private RectTransform promptRect;

    // Object đang nhìn vào
    private InteractableObject currentTarget;
    private Collider currentCollider;

    // Hold E timer
    private float holdTimer = 0f;
    private bool isHolding = false;
    private GameObject _ringBackground; // Vòng nền xám mờ

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
        ScanForInteractable();

        // Cập nhật vị trí UI mỗi frame — dùng tâm collider + offset nhỏ
        if (currentTarget != null && currentCollider != null)
            UpdatePromptPosition(currentCollider.bounds.center);

        HandleInput();
        HandleDropInput();
    }

    // ===================== THẢ ĐỒ (CHUỘT TRÁI) =====================
    void HandleDropInput()
    {
        if (Mouse.current == null) return;

        // Bấm chuột trái để vứt đồ đang cầm
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool dropped = false;

            // Thử vứt tài liệu
            if (DocumentManager.Instance != null && DocumentManager.Instance.IsHoldingDocument)
            {
                DocumentManager.Instance.DropDocument();
                dropped = true;
            }

            // Thử vứt xôi (nếu không vứt tài liệu)
            if (!dropped && ToppingManager.Instance != null && ToppingManager.Instance.isHoldingFood)
            {
                ToppingManager.Instance.DropFood();
            }
        }
    }

    // ===================== INPUT =====================

    void HandleInput()
    {
        if (Keyboard.current == null || currentTarget == null) 
        {
            ResetHold();
            return;
        }

        bool ePressed = Keyboard.current.eKey.isPressed;

        // --- Bấm E cho PackedPaperOnTable (nhặt tài liệu đóng gói từ bàn) ---
        if (currentTarget.type == InteractableType.PackedPaperOnTable && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (DocumentManager.Instance != null && DocumentManager.Instance.IsDocumentPackedOnTable)
            {
                DocumentManager.Instance.PickUpDocument();
            }
        }

        // --- Hold/Press E cho CloseBox ---
        if (currentTarget.type == InteractableType.CloseBox)
        {
            ToppingManager tm = ToppingManager.Instance;
            if (tm != null)
            {
                if (!tm.IsBoxClosed)
                {
                    float duration = holdDuration;
                    if (ePressed)
                    {
                        isHolding = true;
                        holdTimer += Time.deltaTime;
                        SetProgressRing(holdTimer / duration);

                        if (holdTimer >= duration)
                        {
                            tm.CloseBox();
                            ResetHold();
                            HidePrompt();
                        }
                    }
                    else
                    {
                        ResetHold();
                    }
                }
                else
                {
                    // Đã đóng, bấm E để nhặt
                    if (Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        tm.PickUpFood();
                        HidePrompt();
                    }
                }
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
                isHolding = true;
                holdTimer += Time.deltaTime;
                SetProgressRing(holdTimer / duration);

                if (holdTimer >= duration)
                {
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

        // --- Bấm E thường cho các action khác ---
        ResetHold(); // Không phải CloseBox thì không giữ
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            HandleInteraction(currentTarget);
        }
    }

    void ResetHold()
    {
        holdTimer = 0f;
        isHolding = false;
        SetProgressRing(0f);
    }

    // ===================== OVERLAP SPHERE =====================

    void ScanForInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);

        InteractableObject bestTarget = null;
        Collider bestCollider = null;
        float bestDot = -1f;

        foreach (Collider col in hits)
        {
            InteractableObject obj = col.GetComponentInParent<InteractableObject>();
            if (obj == null || !CanInteract(obj)) continue;

            // Dùng tâm của Collider thay vì tâm của Transform (vì tâm Transform của cửa bị lệch hẳn sang một bên)
            Vector3 dirToObj = (col.bounds.center - playerCamera.transform.position).normalized;
            float dot = Vector3.Dot(playerCamera.transform.forward, dirToObj);

            if (dot > 0.5f && dot > bestDot)
            {
                bestDot = dot;
                bestTarget = obj;
                bestCollider = col;
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

    bool CanInteract(InteractableObject obj)
    {
        // Cửa kéo không cần ToppingManager — xử lý riêng
        if (obj.type == InteractableType.SlidingDoor)
        {
            SlidingDoor door = obj.GetComponent<SlidingDoor>();
            return door != null && !door.IsMoving;
        }

        // Máy in: luôn tương tác được
        if (obj.type == InteractableType.Printer)
            return true;

        // Tờ giấy đã in: chỉ tương tác khi chưa cầm tài liệu
        if (obj.type == InteractableType.PrintedPaper)
        {
            DocumentManager dm = DocumentManager.Instance;
            return dm != null && !dm.IsHoldingDocument;
        }

        // Khách hàng: chỉ giao khi đang cầm tài liệu và khách đang chờ
        if (obj.type == InteractableType.Customer)
        {
            DocumentManager dm = DocumentManager.Instance;
            Customer customer = obj.GetComponent<Customer>();
            return dm != null && dm.IsHoldingDocument && customer != null && customer.isWaitingForDocument;
        }

        ToppingManager tm = ToppingManager.Instance;
        if (tm == null) return false;

        switch (obj.type)
        {
            case InteractableType.FoamBoxStack:
                return !tm.IsBoxClosed && !tm.HasFoamBox;

            case InteractableType.StickyRicePot:
                return !tm.IsBoxClosed && tm.HasFoamBox && !tm.HasRice;

            case InteractableType.PateBowl:
                return !tm.IsBoxClosed && tm.HasRice && !tm.HasPate;

            case InteractableType.EggBox:
                return !tm.IsBoxClosed && tm.HasPate && !tm.eggOnPan && !tm.HasEgg;

            case InteractableType.Pan:
                return !tm.IsBoxClosed && tm.eggOnPan && tm.EggIsReady;

            case InteractableType.SausageBowl:
                return !tm.IsBoxClosed && tm.HasPate && !tm.HasSausage;

            case InteractableType.CucumberBowl:
                return !tm.IsBoxClosed && tm.HasPate && !tm.HasCucumber;

            case InteractableType.KetchupBox:
                return !tm.IsBoxClosed && tm.HasPate && !tm.HasKetchup;

            // Đóng hộp: cho phép nếu chưa đóng (Hold E) hoặc đã đóng nhưng chưa nhặt (Press E)
            case InteractableType.CloseBox:
                if (!tm.IsBoxClosed) return tm.HasPate && !tm.eggOnPan;
                else return !tm.isHoldingFood;

            // Cửa kéo: luôn có thể tương tác (không phụ thuộc vào cooking flow)
            case InteractableType.SlidingDoor:
                SlidingDoor door = obj.GetComponent<SlidingDoor>();
                return door != null && !door.IsMoving;

            case InteractableType.PackedPaperOnTable:
                return DocumentManager.Instance != null && DocumentManager.Instance.IsDocumentPackedOnTable;

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
                PrintCanvasUI printerUI = obj.GetComponent<PrintCanvasUI>();
                if (printerUI != null) printerUI.OpenPrintCanvas();
                break;

            case InteractableType.Customer:
                Customer customer = obj.GetComponent<Customer>();
                if (customer != null) customer.ReceiveDocument();
                break;

            // PrintedPaper được xử lý bằng Hold E ở HandleInput(), không cần ở đây
        }

        HidePrompt();
    }

    // ===================== TEXT GỢI Ý =====================

    string GetPromptText(InteractableObject obj)
    {
        switch (obj.type)
        {
            case InteractableType.FoamBoxStack:   return "[E]  Lấy hộp xốp";
            case InteractableType.StickyRicePot:  return "[E]  Múc xôi";
            case InteractableType.PateBowl:       return "[E]  Thêm patê";
            case InteractableType.EggBox:         return "[E]  Đập trứng lên chảo";
            case InteractableType.Pan:            return "[E]  Lấy trứng ốp la";
            case InteractableType.SausageBowl:    return "[E]  Thêm xúc xích";
            case InteractableType.CucumberBowl:   return "[E]  Thêm dưa leo";
            case InteractableType.KetchupBox:     return "[E]  Thêm kết chúp";
            case InteractableType.CloseBox:
                ToppingManager tmClose = ToppingManager.Instance;
                return (tmClose != null && tmClose.IsBoxClosed) ? "[E]  Nhặt hộp xôi" : "[Giữ E]  Đóng hộp";
            case InteractableType.SlidingDoor:
                SlidingDoor slideDoor = obj.GetComponent<SlidingDoor>();
                bool open = slideDoor != null && slideDoor.IsOpen;
                return open ? "[E]  Đóng cửa" : "[E]  Mở cửa";
            case InteractableType.Printer:        return "[E]  Sử dụng máy in";
            case InteractableType.PrintedPaper:   return "[Giữ E]  Đóng gói tài liệu";
            case InteractableType.PackedPaperOnTable: return "[E]  Nhặt lên tay  [V] Xem";
            case InteractableType.Customer:       return "[E]  Giao tài liệu";
            default:                              return "[E]  Tương tác";
        }
    }

    // ===================== UI =====================

    void ShowPrompt(string text)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText  != null) promptText.text = text;
    }

    void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
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
