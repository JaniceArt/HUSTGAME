using UnityEngine;

/// <summary>
/// Gắn lên Prefab tờ giấy 3D (PaperPrefab) được Instantiate từ máy in.
/// Cho phép người chơi giữ E 5s để đóng gói lấy tài liệu.
/// Script này KHÔNG tự phát hiện player — nó được gọi bởi InteractionSystem.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PrintedPaper : MonoBehaviour
{
    // Không cần field nào — logic giữ E được xử lý bởi InteractionSystem
    // Script này chỉ dùng để đánh dấu đây là giấy đã in

    /// <summary>
    /// Gọi khi người chơi giữ E đủ thời gian.
    /// Destroy tờ giấy và thông báo DocumentManager.
    /// </summary>
    public void PackageDocument()
    {
        if (DocumentManager.Instance != null)
        {
            DocumentManager.Instance.PackageDocumentDone();
        }

        Debug.Log("[Paper] Đã đóng gói tài liệu!");
        Destroy(gameObject);
    }
}
