using UnityEngine;

/// <summary>
/// Gắn lên Prefab tờ giấy 3D (PaperPrefab) được Instantiate từ máy in.
/// Cho phép người chơi giữ E 5s để đóng gói lấy tài liệu.
/// Script này KHÔNG tự phát hiện player — nó được gọi bởi InteractionSystem.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PrintedPaper : MonoBehaviour
{
    public CustomerData storedCustomerData;
    public bool isColor;
    public int quantity;

    /// <summary>
    /// Gọi khi người chơi giữ E đủ thời gian.
    /// Destroy tờ giấy và thông báo DocumentManager.
    /// </summary>
    public void PackageDocument()
    {
        if (DocumentManager.Instance != null)
        {
            DocumentManager.Instance.PackageDocumentDone(storedCustomerData, isColor, quantity);
        }

        Debug.Log("[Paper] Đã đóng gói tài liệu!");
        Destroy(gameObject);
    }
}
