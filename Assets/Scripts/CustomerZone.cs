using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn lên vùng Trigger của khách hàng.
/// Khi người chơi vào vùng trigger + đang cầm tài liệu + bấm E → giao thành công.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CustomerZone : MonoBehaviour
{
    [SerializeField, Tooltip("Dữ liệu của khách hàng này")]
    private CustomerData customerData;

    private bool playerInZone = false;

    void Start()
    {
        // Đảm bảo collider là Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // Khi game bắt đầu, nếu khách hàng này là người hiện tại thì set
        // (Có thể gọi SetCurrentCustomer từ hệ thống quest/nhiệm vụ)
    }

    void Update()
    {
        if (!playerInZone) return;
        if (Keyboard.current == null) return;

        // Bấm E khi ở trong vùng trigger
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            bool delivered = false;

            // Kiểm tra giao tài liệu
            DocumentManager dm = DocumentManager.Instance;
            if (dm != null && dm.IsHoldingDocument)
            {
                dm.DeliverDocument();
                delivered = true;
            }

            // Kiểm tra giao xôi
            ToppingManager tm = ToppingManager.Instance;
            if (tm != null && tm.isHoldingFood)
            {
                tm.DeliverFood();
                delivered = true;
            }

            if (delivered)
            {
                Debug.Log($"[Customer] Giao đồ thành công cho {customerData?.customerName ?? "khách"}!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<FirstPersonController>() != null)
        {
            playerInZone = true;

            // Tự động set khách hàng hiện tại khi player vào vùng
            if (customerData != null && DocumentManager.Instance != null)
            {
                DocumentManager.Instance.SetCurrentCustomer(customerData);
            }

            Debug.Log($"[Customer] Player vào vùng của {customerData?.customerName ?? "khách"}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<FirstPersonController>() != null)
        {
            playerInZone = false;
            Debug.Log($"[Customer] Player rời vùng của {customerData?.customerName ?? "khách"}");
        }
    }
}
