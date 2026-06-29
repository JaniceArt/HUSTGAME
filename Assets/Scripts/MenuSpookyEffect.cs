using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn script này vào một GameObject trống trong Scene Main Menu.
/// Kéo đèn, âm thanh và con ma vào để tạo hiệu ứng chớp tắt hù dọa.
/// </summary>
public class MenuSpookyEffect : MonoBehaviour
{
    [Header("=== CÀI ĐẶT HÙ DỌA ===")]
    [Tooltip("Ánh sáng chính của quán (Directional Light)")]
    public Light mainLight;
    
    [Tooltip("Nhân vật ma (Kéo con ma đang đứng đằng sau anh bán xôi vào đây)")]
    public GameObject ghostObject;

    [Tooltip("Âm thanh lúc chớp đèn (VD: Tiếng sấm, nhiễu tivi, bùm...)")]
    public AudioClip glitchSound;

    [Header("=== THỜI GIAN ===")]
    [Tooltip("Thời gian ngẫu nhiên THẤP NHẤT để xảy ra chớp đèn (giây)")]
    public float minWaitTime = 5f;
    
    [Tooltip("Thời gian ngẫu nhiên CAO NHẤT để xảy ra chớp đèn (giây)")]
    public float maxWaitTime = 15f;

    private AudioSource audioSource;
    private float originalLightIntensity;

    void Start()
    {
        // Khởi tạo âm thanh
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Giấu con ma đi lúc đầu
        if (ghostObject != null)
        {
            ghostObject.SetActive(false);
        }

        // Lưu lại độ sáng gốc của đèn
        if (mainLight != null)
        {
            originalLightIntensity = mainLight.intensity;
        }

        // Bắt đầu vòng lặp hù dọa
        StartCoroutine(SpookyRoutine());
    }

    IEnumerator SpookyRoutine()
    {
        while (true)
        {
            // 1. Chờ một khoảng thời gian ngẫu nhiên
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // 2. BẮT ĐẦU CHỚP TẮT (GLITCH)
            
            // Phát âm thanh giật mình
            if (audioSource != null && glitchSound != null)
            {
                audioSource.PlayOneShot(glitchSound);
            }

            // Đèn chớp tắt liên tục vài nhịp
            for (int i = 0; i < 3; i++)
            {
                if (mainLight != null) mainLight.intensity = 0f; // Tắt đèn
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                
                if (mainLight != null) mainLight.intensity = originalLightIntensity * 0.5f; // Sáng mờ mờ
                yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
            }

            // 3. ĐÈN TẮT HẲN VÀ MA XUẤT HIỆN
            if (mainLight != null) mainLight.intensity = 0f; // Tối thui
            if (ghostObject != null) ghostObject.SetActive(true); // Ma hiện ra sau lưng!

            // 4. Giữ cảnh ma xuất hiện trong một tích tắc (0.2s - 0.4s)
            yield return new WaitForSeconds(Random.Range(0.2f, 0.4f));

            // 5. MỌI THỨ TRỞ LẠI BÌNH THƯỜNG
            if (ghostObject != null) ghostObject.SetActive(false); // Giấu ma đi
            if (mainLight != null) mainLight.intensity = originalLightIntensity; // Bật lại đèn bình thường
            
            // Lặp lại vòng tuần hoàn chờ đợi...
        }
    }
}
