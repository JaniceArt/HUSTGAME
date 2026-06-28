using UnityEngine;

/// <summary>
/// Điểm giao hàng chung cho TẤT CẢ khách hàng.
/// Gắn script này vào 1 Empty GameObject đặt trên mặt bàn hoặc vị trí giao hàng mong muốn.
/// </summary>
public class DeliveryPoint : MonoBehaviour
{
    public static DeliveryPoint Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
