using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn lên GameObject "PrintCanvas" (UI Canvas).
/// Quản lý toàn bộ logic và giao diện của màn hình in.
/// </summary>
public class PrintCanvasUI : MonoBehaviour
{
    [Header("=== CANVAS ===")]
    [SerializeField, Tooltip("Chính Canvas này (để tự ẩn/hiện)")]
    private GameObject printCanvas;

    [Header("=== CHỌN MÀU (Exclusive Toggle) ===")]
    [SerializeField, Tooltip("Button hoặc Toggle: In Màu")]
    private Button colorButton;

    [SerializeField, Tooltip("Button hoặc Toggle: In Đen Trắng")]
    private Button bwButton;

    [SerializeField, Tooltip("Image nền của nút In Màu (để đổi màu khi chọn)")]
    private Image colorButtonBg;

    [SerializeField, Tooltip("Image nền của nút In Đen Trắng (để đổi màu khi chọn)")]
    private Image bwButtonBg;

    [Header("=== SỐ LƯỢNG ===")]
    [SerializeField, Tooltip("Text hiển thị số lượng bản in")]
    private TextMeshProUGUI quantityText;

    [SerializeField, Tooltip("Button tăng số lượng [+]")]
    private Button plusButton;

    [SerializeField, Tooltip("Button giảm số lượng [-]")]
    private Button minusButton;

    [Header("=== NÚT IN ===")]
    [SerializeField, Tooltip("Button [PRINT] để thực thi in")]
    private Button printButton;

    [Header("=== SPAWN GiẤY 3D ===")]
    [SerializeField, Tooltip("Prefab tờ giấy 3D (sẽ Instantiate khi in)")]
    private GameObject paperPrefab;

    [SerializeField, Tooltip("Vị trí spawn giấy (Transform ở khay máy in)")]
    private Transform spawnPoint;

    [Header("=== THỜI GIAN & ÂM THANH IN ===")]
    [Tooltip("Máy in chạy mất bao lâu trước khi phọt giấy ra (giây)")]
    public float printDuration = 2.0f;
    public AudioClip printSound;
    public AudioClip clickSound; // Âm thanh bấm nút UI
    [Range(0f, 1f)] public float printVolume = 1f;
    [Range(0.1f, 3f)] public float printPitch = 1f;
    private AudioSource printAudioSource;

    // --- State ---
    private bool isColor = true;    // Mặc định chọn In Màu
    private int quantity = 1;       // Mặc định 1 bản
    private const int MIN_QTY = 1;
    private const int MAX_QTY = 99;

    // Ảnh để phân biệt nút đang chọn / không chọn
    public Sprite selectedSprite;
    public Sprite unselectedSprite;

    // -------------------------------------------------------

    void Start()
    {
        // Gán sự kiện cho các nút
        if (colorButton != null) colorButton.onClick.AddListener(SelectColor);
        if (bwButton    != null) bwButton.onClick.AddListener(SelectBW);
        if (plusButton  != null) plusButton.onClick.AddListener(IncreaseQuantity);
        if (minusButton != null) minusButton.onClick.AddListener(DecreaseQuantity);
        if (printButton != null) printButton.onClick.AddListener(OnPrintPressed);

        // Cập nhật UI ban đầu
        UpdateToggleVisuals();
        UpdateQuantityText();

        // Ép tắt Raycast Target của số lượng để tránh bị đè lên nút cộng trừ
        if (quantityText != null)
        {
            quantityText.raycastTarget = false;
        }

        printAudioSource = gameObject.AddComponent<AudioSource>();
        printAudioSource.playOnAwake = false;
        printAudioSource.loop = true;

        // Ẩn canvas lúc đầu
        if (printCanvas != null)
            printCanvas.SetActive(false);
    }

    // ===================== TOGGLE MÀU =====================

    void PlayClickSound()
    {
        if (clickSound != null && printAudioSource != null)
        {
            printAudioSource.PlayOneShot(clickSound, 0.8f);
        }
    }

    void SelectColor()
    {
        PlayClickSound();
        isColor = true;
        UpdateToggleVisuals();
        Debug.Log("[Printer] Chọn: In Màu");
    }

    void SelectBW()
    {
        PlayClickSound();
        isColor = false;
        UpdateToggleVisuals();
        Debug.Log("[Printer] Chọn: In Đen Trắng");
    }

    void UpdateToggleVisuals()
    {
        if (colorButtonBg != null && selectedSprite != null && unselectedSprite != null)
        {
            colorButtonBg.sprite = isColor ? selectedSprite : unselectedSprite;
            colorButtonBg.color = Color.white; // Xóa màu ép cũ nếu có
        }
        
        if (bwButtonBg != null && selectedSprite != null && unselectedSprite != null)
        {
            bwButtonBg.sprite = isColor ? unselectedSprite : selectedSprite;
            bwButtonBg.color = Color.white; // Xóa màu ép cũ nếu có
        }
    }

    // ===================== SỐ LƯỢNG =====================

    void IncreaseQuantity()
    {
        PlayClickSound();
        quantity = Mathf.Min(quantity + 1, MAX_QTY);
        UpdateQuantityText();
    }

    void DecreaseQuantity()
    {
        PlayClickSound();
        quantity = Mathf.Max(quantity - 1, MIN_QTY);
        UpdateQuantityText();
    }

    void UpdateQuantityText()
    {
        if (quantityText != null)
            quantityText.text = quantity.ToString();
    }

    // ===================== PRINT =====================

    void OnPrintPressed()
    {
        PlayClickSound();
        StartCoroutine(PrintRoutine());
    }

    System.Collections.IEnumerator PrintRoutine()
    {
        if (printButton != null) printButton.interactable = false;

        // Bật tiếng máy in
        if (printSound != null)
        {
            printAudioSource.clip = printSound;
            printAudioSource.volume = printVolume;
            printAudioSource.pitch = printPitch;
            printAudioSource.Play();
        }

        // Chờ máy in chạy
        yield return new WaitForSeconds(printDuration);

        // Tắt tiếng
        if (printAudioSource.isPlaying) printAudioSource.Stop();

        // 1. Thông báo DocumentManager
        if (DocumentManager.Instance != null)
            DocumentManager.Instance.SetPrintSettings(isColor, quantity);

        // 2. Spawn giấy 3D tại khay máy in
        if (paperPrefab != null && spawnPoint != null)
        {
            GameObject go = Instantiate(paperPrefab, spawnPoint.position, spawnPoint.rotation);
            PrintedPaper pp = go.GetComponent<PrintedPaper>();
            if (pp != null && DocumentManager.Instance != null)
            {
                pp.storedCustomerData = DocumentManager.Instance.CurrentCustomer;
                pp.isColor = isColor;
                pp.quantity = quantity;
            }
            Debug.Log($"[Printer] Đã in {quantity} bản {(isColor ? "màu" : "đen trắng")} → giấy xuất hiện tại khay!");
        }
        else
        {
            Debug.LogWarning("[Printer] Chưa gán PaperPrefab hoặc SpawnPoint!");
        }

        // 3. Đóng PrintCanvas và mở lại nút
        if (printButton != null) printButton.interactable = true;
        ClosePrintCanvas();
    }

    // ===================== MỞ / ĐÓNG =====================

    /// <summary>Mở PrintCanvas (gọi từ InteractionSystem khi bấm E vào máy in)</summary>
    public void OpenPrintCanvas()
    {
        if (printCanvas != null)
            printCanvas.SetActive(true);

        // Hiện cursor để click UI và đóng băng player
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        FirstPersonController.CanMove = false;

        // Reset số lượng về 1
        quantity = 1;
        UpdateQuantityText();
    }

    /// <summary>Đóng PrintCanvas</summary>
    public void ClosePrintCanvas()
    {
        if (printAudioSource != null && printAudioSource.isPlaying)
        {
            printAudioSource.Stop();
        }
        StopAllCoroutines(); // Dừng tiến trình in nếu người chơi tắt ngang
        if (printButton != null) printButton.interactable = true;

        if (printCanvas != null)
            printCanvas.SetActive(false);

        // Khoá cursor lại cho FPS và cho phép di chuyển
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        FirstPersonController.CanMove = true;
    }

    void Update()
    {
        // Nhấn ESC để thoát nếu đang mở
        if (printCanvas != null && printCanvas.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePrintCanvas();
            }
        }
    }
}
