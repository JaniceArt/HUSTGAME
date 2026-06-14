using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class InvisibleWallWarning : MonoBehaviour
{
    [Header("=== GIAO DIỆN ===")]
    [Tooltip("Kéo dòng chữ UI (TextMeshPro) trên Canvas vào đây")]
    public TextMeshProUGUI warningText;
    
    [Header("=== CÀI ĐẶT ===")]
    [Tooltip("Nội dung lời nhắc nhở")]
    public string message = "Không được bỏ quán đi linh tinh!";
    
    [Tooltip("Chữ sẽ hiện rõ trong bao nhiêu giây trước khi mờ đi?")]
    public float displayTime = 2f;

    private Coroutine fadeCoroutine;

    void SetTextAlpha(float alpha)
    {
        if (warningText != null)
        {
            Color c = warningText.color;
            c.a = alpha;
            warningText.color = c;
        }
    }

    void Start()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        SetTextAlpha(0f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) ShowWarning();
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) ShowWarning();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) ShowWarning();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) ShowWarning();
    }

    void ShowWarning()
    {
        if (warningText == null)
        {
            Debug.LogWarning("BẠN CHƯA KÉO TEXT VÀO SCRIPT: " + gameObject.name);
            return;
        }

        Debug.Log(">> ĐÃ CHẠM VÀO TƯỜNG: " + gameObject.name + " | Text đang bật lên!");

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        warningText.text = message;
        SetTextAlpha(1f); // Hiện rõ 100%
        
        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(displayTime);

        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            SetTextAlpha(newAlpha);
            yield return null;
        }

        SetTextAlpha(0f);
    }
}
