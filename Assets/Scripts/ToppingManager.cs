using UnityEngine;

/// <summary>
/// Gắn lên object "foamboxtopping" trong scene.
/// Quản lý trạng thái hộp xốp và bật/tắt từng topping child.
/// </summary>
public class ToppingManager : MonoBehaviour
{
    public static ToppingManager Instance;

    [Header("=== TRẠNG THÁI (chỉ đọc) ===")]
    [SerializeField, ReadOnly] private bool hasFoamBox = false;
    [SerializeField, ReadOnly] private bool hasRice = false;
    [SerializeField, ReadOnly] private bool hasPate = false;
    [SerializeField, ReadOnly] private bool hasSausage = false;
    [SerializeField, ReadOnly] private bool hasCucumber = false;
    [SerializeField, ReadOnly] private bool hasKetchup = false;
    [SerializeField, ReadOnly] private bool hasEgg = false;
    [SerializeField, ReadOnly] public  bool eggOnPan = false;
    [SerializeField, ReadOnly] private bool eggIsReady = false;
    [SerializeField, ReadOnly] private float eggCookTimer = 0f;
    [SerializeField, ReadOnly] private bool isBoxClosed = false;

    public float eggCookTime = 5f;
    public bool EggIsReady   => eggIsReady;
    public bool IsBoxClosed  => isBoxClosed;
    public bool isHoldingFood { get; private set; } // Nhân vật đang cầm đồ ăn

    // Public getters để InteractionSystem kiểm tra
    public bool HasFoamBox  => hasFoamBox;
    public bool HasRice     => hasRice;
    public bool HasPate     => hasPate;
    public bool HasSausage  => hasSausage;
    public bool HasCucumber => hasCucumber;
    public bool HasKetchup  => hasKetchup;
    public bool HasEgg      => hasEgg;

    [Header("=== CHILD OBJECTS CỦA foamboxtopping ===")]
    [Tooltip("Kéo vào đây tất cả parts của hộp xốp MỞ (hop, nap...)")]
    public GameObject[] foamBoxObjects;

    [Tooltip("Object đại diện cho hộp xốp ĐÃ ĐÓNG NẮP (ẩn lúc đầu) TRÊN BÀN")]
    public GameObject closedBoxObject;

    [Header("=== CẦM TRÊN TAY ===")]
    [SerializeField, Tooltip("Object hộp xôi đã xếp khít trên tay (ẩn đi lúc đầu)")]
    private GameObject heldFoodInHand;

    [SerializeField, Tooltip("Prefab dùng để ném xuống đất khi bấm Chuột Trái (nếu cần)")]
    private GameObject droppedFoodPrefab;

    [Header("=== CÁC MÓN TOPPING === ")]
    [Tooltip("Child object đại diện xôi trên hộp")]
    public GameObject riceOnBox;

    [Tooltip("Child object đại diện patê trên hộp")]
    public GameObject pateOnBox;

    [Tooltip("Child object đại diện xúc xích trên hộp")]
    public GameObject sausageOnBox;

    [Tooltip("Kéo vào đây tất cả pieces của dưa leo (cuc_2_cut_ready x nhiều cái + Cucumbers)")]
    public GameObject[] cucumberOnBox;

    [Tooltip("Child object đại diện trứng ốp la trên hộp")]
    public GameObject eggOnBox;

    [Tooltip("Child object đại diện kết chúp trên hộp")]
    public GameObject ketchupOnBox;

    [Header("=== TRỨNG TRONG EGG BOX ===")]
    [Tooltip("Kéo vào đây từng quả trứng (child) trong EggBoxes theo thứ tự")]
    public GameObject[] eggsInBox;
    private int eggsRemaining;

    [Header("=== TRỨNG TRÊN CHẢO ===")]
    [Tooltip("Object trứng ốp la trên chảo - ẩn lúc đầu")]
    public GameObject eggOnPanObject;

    // -------------------------------------------------------

    void Awake()
    {
        Instance = this;

        // Ẩn tất cả lúc khởi đầu
        SetActiveArray(foamBoxObjects, false);
        SetActive(closedBoxObject,     false);
        SetActive(riceOnBox,           false);
        SetActive(pateOnBox,           false);
        SetActive(sausageOnBox,        false);
        SetActiveArray(cucumberOnBox,  false);
        SetActive(ketchupOnBox,        false);
        SetActive(eggOnBox,            false);
        SetActive(eggOnPanObject,      false);

        // Đếm số trứng ban đầu trong hộp
        eggsRemaining = eggsInBox != null ? eggsInBox.Length : 0;
    }

    void Update()
    {
        // Đếm ngược thời gian nấu trứng
        if (eggOnPan && !eggIsReady)
        {
            eggCookTimer += Time.deltaTime;
            if (eggCookTimer >= eggCookTime)
            {
                eggIsReady = true;
                Debug.Log("[Topping] Trứng đã chín! Bấm E để lấy.");
            }
        }
    }

    // ===================== CÁC HÀM TƯƠNG TÁC =====================

    /// <summary>Bước 1: Lấy hộp xốp rỗng</summary>
    public void TakeFoamBox()
    {
        hasFoamBox = true;
        SetActiveArray(foamBoxObjects, true);
        Debug.Log("[Topping] Đã lấy hộp xốp!");
    }

    /// <summary>Bước 2: Múc xôi vào hộp</summary>
    public void AddRice()
    {
        hasRice = true;
        SetActive(riceOnBox, true);
        Debug.Log("[Topping] Đã thêm xôi!");
    }

    /// <summary>Bước 3: Thêm patê vào hộp</summary>
    public void AddPate()
    {
        hasPate = true;
        SetActive(pateOnBox, true);
        Debug.Log("[Topping] Đã thêm patê!");
    }

    /// <summary>Bước 4a: Đập trứng lên chảo - ẩn 1 quả trong egg box</summary>
    public void SpawnEggOnPan()
    {
        // Ẩn quả trứng cuối cùng còn trong hộp
        if (eggsInBox != null && eggsRemaining > 0)
        {
            eggsRemaining--;
            SetActive(eggsInBox[eggsRemaining], false);
            Debug.Log($"[Topping] Còn {eggsRemaining} trứng trong hộp.");
        }

        eggOnPan = true;
        SetActive(eggOnPanObject, true);
        Debug.Log("[Topping] Trứng đang rán trên chảo...");
    }

    /// <summary>Bước 4a (phần 2): Lấy trứng từ chảo vào hộp</summary>
    public void TakeEggFromPan()
    {
        eggOnPan    = false;
        eggIsReady  = false;
        eggCookTimer = 0f;
        hasEgg      = true;
        SetActive(eggOnPanObject, false);
        SetActive(eggOnBox,       true);
        Debug.Log("[Topping] Đã thêm trứng ốp la!");
    }

    /// <summary>Bước 4b: Thêm xúc xích vào hộp</summary>
    public void AddSausage()
    {
        hasSausage = true;
        SetActive(sausageOnBox, true);
        Debug.Log("[Topping] Đã thêm xúc xích!");
    }

    /// <summary>Bước 4c: Thêm dưa leo vào hộp</summary>
    public void AddCucumber()
    {
        hasCucumber = true;
        SetActiveArray(cucumberOnBox, true);
        Debug.Log("[Topping] Đã thêm dưa leo!");
    }

    /// <summary>Bước 4d: Thêm kết chúp vào hộp</summary>
    public void AddKetchup()
    {
        hasKetchup = true;
        SetActive(ketchupOnBox, true);
        Debug.Log("[Topping] Đã thêm kết chúp!");
    }

    /// <summary>Đóng hộp xốp lại (sau khi giữ E 2s)</summary>
    public void CloseBox()
    {
        isBoxClosed = true;
        
        // Ẩn hộp mở và toàn bộ topping bên trong trên bàn
        SetActiveArray(foamBoxObjects, false);
        SetActive(riceOnBox,           false);
        SetActive(pateOnBox,           false);
        SetActive(sausageOnBox,        false);
        SetActiveArray(cucumberOnBox,  false);
        SetActive(ketchupOnBox,        false);
        SetActive(eggOnBox,            false);

        // HIỆN HỘP ĐÓNG TRÊN BÀN (Chưa nhặt lên)
        SetActive(closedBoxObject, true);

        Debug.Log("[Topping] Đã đóng hộp xốp để trên bàn! Bấm E để nhặt.");
    }

    /// <summary>Bấm E để nhặt hộp xôi đã đóng lên tay</summary>
    public void PickUpFood()
    {
        if (!isBoxClosed || isHoldingFood) return;

        isHoldingFood = true;

        // Ẩn hộp trên bàn
        SetActive(closedBoxObject, false);

        // BẬT HỘP XÔI TRÊN TAY LÊN
        if (heldFoodInHand != null)
        {
            heldFoodInHand.SetActive(true);
        }

        Debug.Log("[Topping] Đã nhặt hộp xôi lên tay!");
    }

    /// <summary>Giao hộp xôi cho khách</summary>
    public void DeliverFood()
    {
        isHoldingFood = false;
        
        if (heldFoodInHand != null)
        {
            heldFoodInHand.SetActive(false);
        }
        
        // Reset lại trạng thái bếp để làm hộp mới
        hasFoamBox = false;
        hasRice = false;
        hasPate = false;
        hasSausage = false;
        hasCucumber = false;
        hasKetchup = false;
        hasEgg = false;
        eggOnPan = false;
        eggIsReady = false;
        isBoxClosed = false;
        eggCookTimer = 0f;

        Debug.Log("[Topping] Giao xôi thành công! Bếp đã được reset.");
    }

    /// <summary>Vứt hộp xôi đang cầm xuống đất</summary>
    public void DropFood()
    {
        if (!isHoldingFood) return;

        isHoldingFood = false;

        // Ẩn cục xôi trên tay
        if (heldFoodInHand != null)
        {
            heldFoodInHand.SetActive(false);
        }

        // Đẻ ra một cục xôi rơi lạch cạch xuống đất
        if (droppedFoodPrefab != null && Camera.main != null)
        {
            GameObject drop = Instantiate(droppedFoodPrefab, Camera.main.transform.position + Camera.main.transform.forward * 0.5f, Quaternion.identity);
            Rigidbody rb = drop.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Camera.main.transform.forward * 3f + Vector3.up * 1f, ForceMode.Impulse);
            }
        }

        // Reset lại trạng thái bếp để làm hộp mới
        hasFoamBox = false;
        hasRice = false;
        hasPate = false;
        hasSausage = false;
        hasCucumber = false;
        hasKetchup = false;
        hasEgg = false;
        eggOnPan = false;
        eggIsReady = false;
        isBoxClosed = false;
        eggCookTimer = 0f;

        Debug.Log("[Topping] Đã vứt hộp xôi!");
    }

    // ---------------------- Helpers ----------------------
    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private void SetActiveArray(GameObject[] arr, bool active)
    {
        if (arr == null) return;
        foreach (var go in arr)
            if (go != null) go.SetActive(active);
    }
}

// Attribute để hiển thị read-only trong Inspector (không cần package thêm)
public class ReadOnlyAttribute : PropertyAttribute { }
