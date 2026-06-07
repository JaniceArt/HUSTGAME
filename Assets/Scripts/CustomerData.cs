using UnityEngine;

/// <summary>
/// ScriptableObject chứa dữ liệu riêng của từng khách hàng.
/// Tạo trong Unity: Assets → Create → Game → Customer Data
/// </summary>
[CreateAssetMenu(fileName = "NewCustomer", menuName = "Game/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Tooltip("Tên khách hàng")]
    public string customerName = "Khách hàng";

    [Tooltip("Hình ảnh tài liệu 2D của khách (hiển thị khi bấm V)")]
    public Sprite documentImage;

    [Tooltip("Chọn loại in: true = in màu, false = đen trắng")]
    public bool requiresColor = false;

    [Tooltip("Số lượng bản in yêu cầu")]
    public int requiredCopies = 1;
}
