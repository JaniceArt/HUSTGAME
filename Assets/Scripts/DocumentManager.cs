using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton - Gắn lên 1 GameObject bất kỳ (ví dụ Player hoặc GameManager).
/// Quản lý toàn bộ trạng thái tài liệu: đang cầm, xem, giao.
/// </summary>
public class DocumentManager : MonoBehaviour
{
    public static DocumentManager Instance { get; private set; }

    // ===================== TRẠNG THÁI =====================

    [Header("=== TRẠNG THÁI (chỉ đọc) ===")]
    [SerializeField, ReadOnly] private bool isHoldingDocument = false;
    [SerializeField, ReadOnly] private bool isViewingDocument = false;

    /// <summary>Người chơi đang cầm tài liệu?</summary>
    public bool IsHoldingDocument => isHoldingDocument;

    /// <summary>Dữ liệu khách hàng hiện tại (ai đang được phục vụ)</summary>
    public CustomerData CurrentCustomer { get; private set; }

    [Header("=== TÀI LIỆU TRÊN BÀN ===")]
    [SerializeField, Tooltip("Object xấp tài liệu đã đóng gói để sẵn trên bàn (ẩn lúc đầu)")]
    private GameObject packedPaperOnTable;

    public bool IsDocumentPackedOnTable { get; private set; }

    [Header("=== TÀI LIỆU CẦM TAY ===")]
    [SerializeField, Tooltip("Object tài liệu đã xếp khít trên tay (ẩn đi lúc đầu)")]
    private GameObject heldPaperInHand;

    [SerializeField, Tooltip("Prefab dùng để ném xuống đất khi bấm Chuột Trái (nếu cần)")]
    private GameObject droppedPaperPrefab;

    // ===================== UI XEMTÀI LIỆU =====================

    [Header("=== VIEW CANVAS (Bấm V để xem tài liệu) ===")]
    [SerializeField, Tooltip("Canvas hiển thị tài liệu (ẩn lúc đầu)")]
    private GameObject viewCanvas;

    [SerializeField, Tooltip("UI Image hiển thị hình ảnh tài liệu")]
    private UnityEngine.UI.Image documentDisplayImage;

    // ===================== PRINT SETTINGS =====================

    [Header("=== CẤU HÌNH IN ===")]
    [SerializeField, Tooltip("Tốc độ giữ E để đóng gói tài liệu (giây)")]
    private float packageHoldDuration = 5f;

    /// <summary>Thời gian giữ E để đóng gói</summary>
    public float PackageHoldDuration => packageHoldDuration;

    // Dữ liệu từ lần in gần nhất
    private bool lastPrintWasColor = false;
    private int lastPrintQuantity = 1;

    // -------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Đảm bảo đóng gói trên bàn bị ẩn ban đầu
        if (packedPaperOnTable != null)
        {
            packedPaperOnTable.SetActive(false);
        }

        // Ẩn ViewCanvas lúc đầu
        if (viewCanvas != null)
            viewCanvas.SetActive(false);
    }

    void Update()
    {
        HandleViewInput();
    }

    // ===================== VIEW (Bấm V) =====================

    void HandleViewInput()
    {
        if (Keyboard.current == null) return;

        // Bấm V để bật/tắt UI tài liệu (cho phép cả khi trên tay hoặc nằm chờ trên bàn)
        if (Keyboard.current.vKey.wasPressedThisFrame && (isHoldingDocument || IsDocumentPackedOnTable))
        {
            ToggleViewCanvas();
        }

        // Bấm ESC để thoát xem ảnh
        if (Keyboard.current.escapeKey.wasPressedThisFrame && isViewingDocument)
        {
            ToggleViewCanvas();
        }
    }

    void ToggleViewCanvas()
    {
        isViewingDocument = !isViewingDocument;

        if (viewCanvas != null)
            viewCanvas.SetActive(isViewingDocument);

        // Cập nhật hình ảnh tài liệu theo khách hàng hiện tại
        if (isViewingDocument && documentDisplayImage != null && CurrentCustomer != null)
        {
            documentDisplayImage.sprite = CurrentCustomer.documentImage;
        }

        // Hiện/ẩn cursor và đóng băng player khi xem tài liệu
        Cursor.lockState = isViewingDocument ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isViewingDocument;
        FirstPersonController.CanMove = !isViewingDocument;
    }

    // ===================== PUBLIC API =====================

    /// <summary>
    /// Gọi từ PrintCanvasUI khi người chơi bấm PRINT.
    /// Lưu lại thông tin in để dùng sau.
    /// </summary>
    public void SetPrintSettings(bool isColor, int quantity)
    {
        lastPrintWasColor = isColor;
        lastPrintQuantity = quantity;
        Debug.Log($"[Document] In {(isColor ? "MÀU" : "ĐEN TRẮNG")} x{quantity}");
    }

    /// <summary>
    /// Gọi khi set khách hàng hiện tại (từ hệ thống nhiệm vụ/quest).
    /// </summary>
    public void SetCurrentCustomer(CustomerData customer)
    {
        CurrentCustomer = customer;
        Debug.Log($"[Document] Khách hàng hiện tại: {customer?.customerName ?? "Không có"}");
    }

    /// <summary>
    /// Gọi khi người chơi đóng gói xong tờ giấy (giữ E đủ 5s).
    /// </summary>
    public void PackageDocumentDone()
    {
        IsDocumentPackedOnTable = true;
        
        // Hiện cục giấy đã đóng gói nằm sẵn trên bàn
        if (packedPaperOnTable != null)
        {
            packedPaperOnTable.SetActive(true);
        }

        Debug.Log("[Document] Đã đóng gói xong! Bấm E để nhặt hoặc V để soi.");
    }

    /// <summary>
    /// Nhặt tài liệu đã đóng gói từ trên bàn lên tay.
    /// </summary>
    public void PickUpDocument()
    {
        if (!IsDocumentPackedOnTable) return;

        IsDocumentPackedOnTable = false;
        isHoldingDocument = true;

        // Ẩn tài liệu trên bàn
        if (packedPaperOnTable != null)
        {
            packedPaperOnTable.SetActive(false);
        }

        // BẬT TÀI LIỆU TRÊN TAY LÊN
        if (heldPaperInHand != null)
        {
            heldPaperInHand.SetActive(true);
        }

        Debug.Log("[Document] Đã nhặt tài liệu lên tay! Bấm V để xem.");
    }

    /// <summary>
    /// Gọi khi giao tài liệu cho khách thành công.
    /// </summary>
    public void DeliverDocument()
    {
        isHoldingDocument = false;
        isViewingDocument = false;

        if (viewCanvas != null)
            viewCanvas.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ẩn cục tài liệu trên tay
        if (heldPaperInHand != null)
        {
            heldPaperInHand.SetActive(false);
        }

        Debug.Log("[Document] Giao thành công!");
    }

    /// <summary>
    /// Vứt tài liệu đang cầm xuống đất
    /// </summary>
    public void DropDocument()
    {
        if (!isHoldingDocument) return;

        isHoldingDocument = false;
        isViewingDocument = false;

        if (viewCanvas != null)
            viewCanvas.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ẩn tài liệu trên tay
        if (heldPaperInHand != null)
        {
            heldPaperInHand.SetActive(false);
        }

        // Đẻ ra 1 cục rớt lạch cạch xuống đất
        if (droppedPaperPrefab != null && Camera.main != null)
        {
            GameObject drop = Instantiate(droppedPaperPrefab, Camera.main.transform.position + Camera.main.transform.forward * 0.5f, Quaternion.identity);
            Rigidbody rb = drop.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Camera.main.transform.forward * 3f + Vector3.up * 1f, ForceMode.Impulse);
            }
        }

        Debug.Log("[Document] Đã vứt tài liệu!");
    }
}
