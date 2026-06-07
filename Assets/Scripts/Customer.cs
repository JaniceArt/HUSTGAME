using UnityEngine;

/// <summary>
/// Gắn lên NPC / Object đại diện cho khách hàng.
/// Người chơi nhìn vào khách và bấm E để giao tài liệu.
/// </summary>
[RequireComponent(typeof(InteractableObject))]
public class Customer : MonoBehaviour
{
    [Tooltip("Dữ liệu của khách hàng này (tên, hình ảnh tài liệu cần in)")]
    public CustomerData customerData;

    [Tooltip("Khách này có đang chờ lấy tài liệu không?")]
    public bool isWaitingForDocument = true;

    void Start()
    {
        // Đảm bảo type của InteractableObject là Customer
        InteractableObject interactObj = GetComponent<InteractableObject>();
        if (interactObj != null)
        {
            interactObj.type = InteractableType.Customer;
        }

        // Tạm thời tự động set khách hàng này làm khách hiện tại để test
        if (isWaitingForDocument && DocumentManager.Instance != null && customerData != null)
        {
            DocumentManager.Instance.SetCurrentCustomer(customerData);
        }
    }

    /// <summary>
    /// Gọi từ InteractionSystem khi người chơi bấm E vào khách hàng
    /// </summary>
    public void ReceiveDocument()
    {
        if (!isWaitingForDocument) return;

        DocumentManager dm = DocumentManager.Instance;
        if (dm != null && dm.IsHoldingDocument)
        {
            // Xử lý giao hàng
            dm.DeliverDocument();
            isWaitingForDocument = false; // Đã nhận xong, không nhận nữa
            
            Debug.Log($"[Customer] Đã giao tài liệu thành công cho {customerData?.customerName ?? "khách"}! Cảm ơn!");
            
            // Tạm thời ẩn khách hàng đi (Sau này bạn có thể thay bằng animation đi về)
            gameObject.SetActive(false);
        }
    }
}
