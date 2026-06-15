using UnityEngine;
using System.Collections;

public class DayNightCycle : MonoBehaviour
{
    [Header("=== MẶT TRỜI (DIRECTIONAL LIGHT) ===")]
    [Tooltip("Kéo Đèn Mặt Trời (Directional Light) vào đây")]
    public Light sunLight;
    
    [Tooltip("Góc xoay của mặt trời ban ngày (thường là 50 độ X)")]
    public Vector3 dayRotation = new Vector3(50f, -30f, 0f);
    
    [Tooltip("Góc xoay của mặt trời ban đêm (Mặt trời lặn, thường là -10 độ X)")]
    public Vector3 nightRotation = new Vector3(-10f, -30f, 0f);

    [Tooltip("Màu của mặt trời vào ban ngày (Thường là màu Trắng hoặc Vàng nhạt)")]
    public Color dayColor = Color.white;
    
    [Tooltip("Màu của mặt trời vào ban đêm (Tùy chọn)")]
    public Color nightColor = new Color(0.1f, 0.1f, 0.2f);

    [Header("=== KỊCH BẢN CHUYỂN ĐÊM ===")]
    [Tooltip("Sẽ bắt đầu tối đi SAU KHI bước thứ mấy trong Sequence hoàn thành? (VD: 3 là sau khi hoàn thành 3 bước)")]
    public int startNightAfterStep = 3;
    
    [Tooltip("Thời gian chuyển từ ngày sang đêm (tính bằng giây)")]
    public float transitionDuration = 15f;

    [Header("=== SKYBOX (BẦU TRỜI) ===")]
    [Tooltip("Tùy chọn: Kéo Material Skybox ban ngày vào đây")]
    public Material daySkybox;
    [Tooltip("Tùy chọn: Kéo Material Skybox ban đêm vào đây")]
    public Material nightSkybox;

    [Header("=== AMBIENT LIGHT (ĐỘ SÁNG TRONG NHÀ/BÓNG RÂM) ===")]
    [Tooltip("Độ sáng môi trường ban ngày (Trắng hoặc Xám nhạt)")]
    public Color dayAmbientColor = new Color(0.6f, 0.6f, 0.6f);
    [Tooltip("Độ sáng môi trường ban đêm (Xanh đen cực tối)")]
    public Color nightAmbientColor = new Color(0.1f, 0.1f, 0.15f);

    private bool isTransitioning = false;
    private bool isNight = false;

    void Start()
    {
        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(dayRotation);
            sunLight.color = dayColor;
        }

        // Nếu có gán Day Skybox thì set bầu trời ban ngày luôn lúc mới vào game
        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
        }

        // Đặt luôn ánh sáng môi trường ban ngày
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = dayAmbientColor;
    }

    void Update()
    {
        if (isTransitioning || isNight) return;

        // Chờ đến khi hệ thống Sequence có mặt và đạt đủ số bước yêu cầu
        if (SequenceManager.Instance != null)
        {
            if (SequenceManager.Instance.CurrentStepIndex >= startNightAfterStep)
            {
                StartCoroutine(TransitionToNight());
            }
        }
    }

    IEnumerator TransitionToNight()
    {
        isTransitioning = true;
        float elapsedTime = 0f;

        Quaternion startRot = Quaternion.Euler(dayRotation);
        Quaternion endRot = Quaternion.Euler(nightRotation);

        Debug.Log("[DayNightCycle] Trời bắt đầu tối...");

        // Chuẩn bị một Material trung gian để blend mượt mà giữa 2 Skybox
        Material blendedSkybox = null;
        if (daySkybox != null && nightSkybox != null)
        {
            blendedSkybox = new Material(daySkybox);
            RenderSettings.skybox = blendedSkybox;
        }

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            
            if (sunLight != null)
            {
                // Xoay mặt trời từ từ xuống dưới đường chân trời
                sunLight.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                // Chuyển màu mặt trời dần sang màu tối
                sunLight.color = Color.Lerp(dayColor, nightColor, t);
            }

            // Chuyển dần mượt mà giữa 2 Skybox
            if (blendedSkybox != null)
            {
                blendedSkybox.Lerp(daySkybox, nightSkybox, t);
            }

            // Chuyển dần độ sáng môi trường (từ sáng sang tối)
            RenderSettings.ambientLight = Color.Lerp(dayAmbientColor, nightAmbientColor, t);
            
            yield return null;
        }

        if (sunLight != null)
        {
            sunLight.transform.rotation = endRot;
            sunLight.color = nightColor;
        }
        
        RenderSettings.ambientLight = nightAmbientColor;

        // Set cứng Skybox ban đêm khi hoàn tất
        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
        }

        Debug.Log("[DayNightCycle] Đã hoàn tất chuyển sang đêm!");

        isTransitioning = false;
        isNight = true;
    }

    // ==========================================
    // CÁC HÀM HỖ TRỢ TEST NHANH TRONG EDITOR
    // ==========================================

    [ContextMenu("👀 Xem Trước: BAN ĐÊM")]
    public void PreviewNightMode()
    {
        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(nightRotation);
            sunLight.color = nightColor;
        }

        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = nightAmbientColor;
        
        Debug.Log("🌙 Đã ép cảnh sang BAN ĐÊM để xem trước! (Bấm Play game sẽ tự reset lại bình thường)");
    }

    [ContextMenu("☀️ Xem Trước: BAN NGÀY")]
    public void PreviewDayMode()
    {
        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(dayRotation);
            sunLight.color = dayColor;
        }

        if (daySkybox != null)
        {
            RenderSettings.skybox = daySkybox;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = dayAmbientColor;
        
        Debug.Log("☀️ Đã khôi phục cảnh về BAN NGÀY!");
    }
}
