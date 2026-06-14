using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class DrinkItem : MonoBehaviour
{
    [Header("=== LOẠI NƯỚC BÁN ===")]
    public DrinkType drinkType = DrinkType.Coke;

    void Start()
    {
        InteractableObject io = GetComponent<InteractableObject>();
        if (io != null) io.type = InteractableType.Drink;
    }

    public void Interact()
    {
        if (DrinkManager.Instance != null)
        {
            DrinkManager.Instance.PickUpDrink(drinkType, this.gameObject);
        }
    }
}
