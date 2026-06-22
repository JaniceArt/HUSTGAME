using System.Collections.Generic;
using UnityEngine;

public enum ToppingType
{
    Pate,
    Sausage, // Xúc xích
    Cucumber, // Dưa leo
    Ketchup, // Tương ớt/kết chúp
    Egg // Trứng
}

[System.Serializable]
public class DialogChoice
{
    [Tooltip("Dòng chữ hiển thị để người chơi chọn")]
    public string choiceText;

    [Tooltip("Câu khách trả lời lại SAU KHI bạn chọn đáp án này (Bỏ trống nếu không cần)")]
    [TextArea(2, 4)]
    public string replyText;
}

[System.Serializable]
public class DialogNode
{
    [Tooltip("Câu thoại của khách")]
    [TextArea(2, 4)]
    public string sentence;

    [Tooltip("Câu này có bắt người chơi chọn đáp án không?")]
    public bool hasChoices = false;

    [Tooltip("Các đáp án để người chơi chọn")]
    public List<DialogChoice> choices;
}

/// <summary>
/// ScriptableObject chứa dữ liệu riêng của từng khách hàng.
/// Tạo trong Unity: Assets → Create → Game → Customer Data
/// </summary>
[CreateAssetMenu(fileName = "NewCustomer", menuName = "Game/Customer Data")]
public class CustomerData : ScriptableObject
{
    [Header("=== THÔNG TIN CƠ BẢN ===")]
    [Tooltip("Tên khách hàng")]
    public string customerName = "Khách hàng";

    [Header("=== THANH TOÁN & THÁI ĐỘ ===")]
    [Tooltip("Khách này lúc đi về có phát tiếng Ting Ting (chuyển tiền) không? Bỏ tick nếu là khách miễn phí.")]
    public bool paysMoney = true;

    [Tooltip("Khách này có tức giận dậm chân chửi bới 1 lúc rồi mới bỏ đi không?")]
    public bool leavesAngry = false;

    [Header("=== KỊCH BẢN HỘI THOẠI LÚC GỌI MÓN ===")]
    [Tooltip("Danh sách các câu thoại lúc mới vào. Bạn có thể chèn Lựa Chọn vào bất kỳ câu nào!")]
    public List<DialogNode> dialogNodes;

    [Header("=== TÍNH NĂNG ĐẶC BIỆT ===")]
    [Tooltip("Khách này có ném USB ra bàn để mình cắm vào máy tính không?")]
    public bool hasUsb = false;

    [Header("=== KỊCH BẢN HỘI THOẠI LÚC NHẬN ĐỒ ===")]
    [Tooltip("Các câu thoại khách nói SAU KHI nhận xong tất cả đồ ăn/nước uống/giấy tờ")]
    public List<DialogNode> postDeliveryDialogNodes;

    [Header("=== KỊCH BẢN KHI GIAO SAI ĐỒ ===")]
    [Tooltip("Câu khách mắng khi bạn giao sai tài liệu, xôi, hoặc nước")]
    [TextArea(2, 4)]
    public string wrongOrderDialog = "Bạn làm sai món tôi gọi rồi, xem lại đi!";

    [Header("=== THOẠI CỦA PLAYER LÚC ĐỌC TÀI LIỆU ===")]
    [Tooltip("Câu thoại của Player tự lẩm bẩm sau khi đóng màn hình soi tài liệu. Bỏ trống nếu không cần.")]
    [TextArea(2, 4)]
    public string playerReactionAfterReading = "";

    [Header("=== THOẠI CỦA PLAYER SAU KHI KHÁCH ĐI ===")]
    [Tooltip("Câu thoại của Player tự lẩm bẩm sau khi khách mua xong và ra khỏi cửa. Bỏ trống nếu không cần.")]
    [TextArea(2, 4)]
    public string playerReactionAfterCustomerLeaves = "";

    [Tooltip("Đợi mấy giây sau khi khách QUAY MẶT BƯỚC ĐI thì main mới bắt đầu lẩm bẩm? (VD: 3)")]
    public float delayBeforeReaction = 3f;

    [Header("=== YÊU CẦU IN ẤN ===")]
    [Tooltip("Khách có cần in tài liệu không?")]
    public bool needsDocument = true;

    [Tooltip("Hình ảnh tài liệu 2D của khách (hiển thị khi bấm V)")]
    public Sprite documentImage;

    [Tooltip("Chọn loại in: true = in màu, false = đen trắng")]
    public bool requiresColor = false;

    [Tooltip("Số lượng bản in yêu cầu")]
    public int requiredCopies = 1;

    [Header("=== YÊU CẦU ĐỒ ĂN ===")]
    [Tooltip("Khách có mua xôi không?")]
    public bool needsFood = false;

    [Tooltip("Số hộp xôi khách muốn mua")]
    public int requiredFoodQuantity = 1;

    [Tooltip("Danh sách các topping khách muốn thêm vào hộp xôi")]
    public List<ToppingType> requiredToppings;

    [Header("=== ÂM THANH ===")]
    [Tooltip("Tiếng bước chân khi di chuyển")]
    public AudioClip walkingSound;
    [Range(0f, 1f)]
    public float walkingSoundVolume = 1f;

    [Tooltip("Âm thanh khi khách tức giận")]
    public AudioClip angrySound;
    [Range(0f, 1f)]
    public float angrySoundVolume = 1f;

    [Header("=== YÊU CẦU ĐỒ UỐNG ===")]
    [Tooltip("Khách có mua nước không?")]
    public bool needsDrink = false;

    [Tooltip("Khách muốn uống loại nước gì?")]
    public DrinkType requiredDrink = DrinkType.None;
}

public enum DrinkType
{
    None,
    Coke,
    Pepsi,
    Lavie
}
