using UnityEngine;

public class DrinkManager : MonoBehaviour
{
    public static DrinkManager Instance;

    [Header("=== CÁC CHAI NƯỚC TRÊN TAY ===")]
    public GameObject handCoke;
    public GameObject handPepsi;
    public GameObject handLavie;

    [Header("=== TRẠNG THÁI ===")]
    public bool isHoldingDrink = false;
    public DrinkType currentDrink = DrinkType.None;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        HideAllDrinks();
    }

    public void PickUpDrink(DrinkType type, GameObject fridgeDrinkObj)
    {
        if (isHoldingDrink)
        {
            Debug.LogWarning("[DrinkManager] Đang cầm 1 chai nước rồi, không thể cầm thêm!");
            return;
        }

        // Tạm thời ẩn chai nước trong tủ lạnh đi (nếu muốn nó biến mất)
        if (fridgeDrinkObj != null) fridgeDrinkObj.SetActive(false);

        isHoldingDrink = true;
        currentDrink = type;

        HideAllDrinks();
        switch (type)
        {
            case DrinkType.Coke: if (handCoke != null) handCoke.SetActive(true); break;
            case DrinkType.Pepsi: if (handPepsi != null) handPepsi.SetActive(true); break;
            case DrinkType.Lavie: if (handLavie != null) handLavie.SetActive(true); break;
        }

        Debug.Log($"[DrinkManager] Đã nhặt chai {type}");
    }

    public void DeliverDrink()
    {
        if (!isHoldingDrink) return;

        isHoldingDrink = false;
        currentDrink = DrinkType.None;
        HideAllDrinks();
        Debug.Log("[DrinkManager] Đã giao nước!");
    }

    public void DropDrink()
    {
        if (!isHoldingDrink) return;

        isHoldingDrink = false;
        currentDrink = DrinkType.None;
        HideAllDrinks();
        Debug.Log("[DrinkManager] Đã vứt chai nước!");
    }

    void HideAllDrinks()
    {
        if (handCoke != null) handCoke.SetActive(false);
        if (handPepsi != null) handPepsi.SetActive(false);
        if (handLavie != null) handLavie.SetActive(false);
    }
}
