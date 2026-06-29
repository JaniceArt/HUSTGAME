using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn script này vào Main Camera.
/// Bấm phím Z để bắt đầu hiệu ứng Zoom In chầm chậm mượt mà.
/// </summary>
public class CameraZoomEffect : MonoBehaviour
{
    [Header("=== CÀI ĐẶT ZOOM ===")]
    [Tooltip("FOV (Góc nhìn) lúc zoom sát vào (Càng nhỏ càng zoom to. Mặc định thường là 60, zoom sát để 20-30)")]
    public float targetFOV = 20f;
    
    [Tooltip("Tốc độ zoom (thời gian tính bằng giây)")]
    public float zoomDuration = 3f;

    private Camera cam;
    private float initialFOV;
    private bool isZooming = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            initialFOV = cam.fieldOfView;
        }
    }

    void Update()
    {
        // Kiểm tra phím Z bằng hệ thống Input System mới của Unity
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            if (!isZooming && cam != null)
            {
                StartCoroutine(ZoomInRoutine());
            }
        }
    }

    IEnumerator ZoomInRoutine()
    {
        isZooming = true;
        float elapsedTime = 0f;

        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.deltaTime;
            // Thu nhỏ góc nhìn FOV mượt mà
            cam.fieldOfView = Mathf.Lerp(initialFOV, targetFOV, elapsedTime / zoomDuration);
            yield return null;
        }

        cam.fieldOfView = targetFOV; // Đảm bảo chốt ở góc cuối cùng
        
        // (Tuỳ chọn) Đợi 2 giây rồi nhả zoom về bình thường
        // yield return new WaitForSeconds(2f);
        // StartCoroutine(ZoomOutRoutine());
    }

    IEnumerator ZoomOutRoutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(targetFOV, initialFOV, elapsedTime / zoomDuration);
            yield return null;
        }
        cam.fieldOfView = initialFOV;
        isZooming = false;
    }
}
