using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // Nếu bạn dùng TextMeshPro

/// <summary>
/// Kéo script này thả vào các nút bấm (VD: Btn_Play, Btn_Quit) để tạo hiệu ứng phóng to và đổi màu khi di chuột vào.
/// </summary>
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("=== CÀI ĐẶT PHÓNG TO ===")]
    [Tooltip("Độ to khi chuột chỉ vào (Ví dụ: 1.1 là to lên 10%)")]
    public float hoverScale = 1.1f;
    [Tooltip("Tốc độ phóng to/thu nhỏ")]
    public float scaleSpeed = 10f;

    [Header("=== CÀI ĐẶT MÀU CHỮ ===")]
    [Tooltip("Màu chữ khi chuột chỉ vào")]
    public Color hoverColor = Color.red;
    [Tooltip("Màu chữ bình thường ban đầu")]
    public Color normalColor = Color.white;

    private Vector3 originalScale;
    private Vector3 targetScale;
    
    private TextMeshProUGUI buttonText;

    void Start()
    {
        // Lưu lại kích thước gốc của nút
        originalScale = transform.localScale;
        targetScale = originalScale;

        // Tự động tìm chữ bên trong nút
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = normalColor;
        }
    }

    void Update()
    {
        // Phóng to thu nhỏ một cách mượt mà theo thời gian
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    // Khi chuột DI VÀO nút
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale; // Đặt mục tiêu là phóng to
        
        if (buttonText != null)
        {
            buttonText.color = hoverColor; // Đổi màu
        }
    }

    // Khi chuột RỜI KHỎI nút
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale; // Đặt mục tiêu là thu về bình thường
        
        if (buttonText != null)
        {
            buttonText.color = normalColor; // Trả lại màu cũ
        }
    }
}
