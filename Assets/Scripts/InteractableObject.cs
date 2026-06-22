using UnityEngine;

public enum InteractableType
{
    FoamBoxStack,   // foamBoxes - bước 1
    StickyRicePot,  // StickyRicePot - bước 2
    PateBowl,       // PateBowl - bước 3
    EggBox,         // EggBoxes - bước 4a
    Pan,            // Pan - nhặt trứng từ chảo
    SausageBowl,    // SausageBowl - bước 4b
    CucumberBowl,   // CucumberBowl - bước 4c
    KetchupBox,     // KetchupBox - bước 4d
    CloseBox,       // foamboxtopping - giữ E 2s để đóng hộp
    SlidingDoor,    // cửa kéo - bấm E để mở/đóng
    Printer,        // máy in - bấm E mở PrintCanvas
    PrintedPaper,   // tờ giấy 3D - giữ E 5s để đóng gói
    PackedPaperOnTable, // giấy đã đóng gói nằm trên bàn
    Customer,       // khách hàng - bấm E để giao đồ
    PaperAirplane,  // Máy bay giấy - nhặt lên để vứt
    FridgeDoor,     // Cửa tủ lạnh - bấm E để xoay mở/đóng
    Drink,          // Nước uống (Coke, Pepsi, Lavie)
    TrashCan,       // Thùng rác
    UsbDrive,       // USB của khách đưa để in
    Broom,          // Cây chổi để quét dọn
    Stain           // Vết bẩn cần lau dọn
}

[RequireComponent(typeof(Collider))]
public class InteractableObject : MonoBehaviour
{
    public InteractableType type;
}
