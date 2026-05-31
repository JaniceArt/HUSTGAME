using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn lên Player.
/// Mỗi frame dùng OverlapSphere tìm object tương tác gần nhất player đang nhìn vào.
/// Bấm E → tương tác thường | Giữ E 2s → đóng hộp xốp.
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
    public float promptHeightOffset = 1.2f;

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

    // -------------------------------------------------------

    void Awake()
    {
        if (promptPanel != null)
            promptRect = promptPanel.GetComponent<RectTransform>();

        SetProgressRing(0f);
    }

    void Update()
    {
        ScanForInteractable();

        // Cập nhật vị trí UI mỗi frame theo collider hiện tại
        if (currentTarget != null && currentCollider != null)
            UpdatePromptPosition(currentCollider.bounds.center);

        HandleInput();
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

        // --- Hold E cho CloseBox ---
        if (currentTarget.type == InteractableType.CloseBox)
        {
            if (ePressed)
            {
                isHolding = true;
                holdTimer += Time.deltaTime;
                SetProgressRing(holdTimer / holdDuration);

                if (holdTimer >= holdDuration)
                {
                    // Đã giữ đủ 2s → đóng hộp
                    ToppingManager.Instance.CloseBox();
                    ResetHold();
                    HidePrompt();
                }
            }
            else
            {
                // Nhả E → reset vòng tròn
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

            Vector3 dirToObj = (obj.transform.position - playerCamera.transform.position).normalized;
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

        ToppingManager tm = ToppingManager.Instance;
        if (tm == null) return false;

        switch (obj.type)
        {
            case InteractableType.FoamBoxStack:
                return !tm.HasFoamBox;

            case InteractableType.StickyRicePot:
                return tm.HasFoamBox && !tm.HasRice;

            case InteractableType.PateBowl:
                return tm.HasRice && !tm.HasPate;

            case InteractableType.EggBox:
                return tm.HasPate && !tm.eggOnPan && !tm.HasEgg;

            case InteractableType.Pan:
                return tm.eggOnPan && tm.EggIsReady;

            case InteractableType.SausageBowl:
                return tm.HasPate && !tm.HasSausage;

            case InteractableType.CucumberBowl:
                return tm.HasPate && !tm.HasCucumber;

            case InteractableType.KetchupBox:
                return tm.HasPate && !tm.HasKetchup;

            // Đóng hộp: phải có patê, hộp chưa đóng, không đang rán trứng
            case InteractableType.CloseBox:
                return tm.HasPate && !tm.IsBoxClosed && !tm.eggOnPan;

            // Cửa kéo: luôn có thể tương tác (không phụ thuộc vào cooking flow)
            case InteractableType.SlidingDoor:
                SlidingDoor door = obj.GetComponent<SlidingDoor>();
                return door != null && !door.IsMoving;

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
            case InteractableType.CloseBox:       return "[Giữ E]  Đóng hộp";
            case InteractableType.SlidingDoor:
                SlidingDoor door = obj.GetComponent<SlidingDoor>();
                bool open = door != null && door.IsOpen;
                return open ? "[E]  Đóng cửa" : "[E]  Mở cửa";
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
    }

    void UpdatePromptPosition(Vector3 worldPos)
    {
        if (promptRect == null || playerCamera == null) return;

        Vector3 offsetPos = worldPos + Vector3.up * promptHeightOffset;
        Vector3 screenPos = playerCamera.WorldToScreenPoint(offsetPos);

        if (screenPos.z < 0)
        {
            promptPanel.SetActive(false);
            return;
        }

        promptRect.position = new Vector3(screenPos.x, screenPos.y, 0f);
    }
}
