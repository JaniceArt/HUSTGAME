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
    SlidingDoor    // cửa kéo - bấm E để mở/đóng
}

[RequireComponent(typeof(Collider))]
public class InteractableObject : MonoBehaviour
{
    public InteractableType type;
}
