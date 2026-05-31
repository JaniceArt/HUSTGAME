using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn vào scene Cutscene.
/// - Tự động chuyển sang gameplay sau khi cutscene kết thúc.
/// - Có thể gọi EndCutscene() từ Timeline Signal hoặc tự chờ duration.
/// </summary>
public class CutsceneController : MonoBehaviour
{
    [Tooltip("Tên scene gameplay (phải trùng với tên trong Build Settings)")]
    public string gameplaySceneName = "Day1_Game";

    [Tooltip("Tự động skip sau bao nhiêu giây (0 = không tự skip)")]
    public float autoDuration = 0f;

    [Header("Fade")]
    public CanvasGroup fadeCanvas;       // UI CanvasGroup màu đen để fade
    public float fadeDuration = 1f;

    void Start()
    {
        // Fade in (từ đen ra cảnh)
        if (fadeCanvas != null)
            StartCoroutine(FadeIn());

        // Tự động skip nếu cài duration
        if (autoDuration > 0f)
            StartCoroutine(AutoEnd());

        // Cho phép bấm phím bất kỳ để skip
        // (xoá nếu không muốn skip)
    }

    void Update()
    {
        if (Input.anyKeyDown)
            EndCutscene();
    }

    IEnumerator AutoEnd()
    {
        yield return new WaitForSeconds(autoDuration);
        EndCutscene();
    }

    /// <summary>
    /// Gọi hàm này từ cuối Timeline (Signal) để chuyển scene
    /// </summary>
    public void EndCutscene()
    {
        StartCoroutine(FadeOutAndLoad(gameplaySceneName));
    }

    IEnumerator FadeIn()
    {
        if (fadeCanvas == null) yield break;
        fadeCanvas.alpha = 1f;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = 1f - t / fadeDuration;
            yield return null;
        }
        fadeCanvas.alpha = 0f;
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        // Fade ra đen
        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvas.alpha = t / fadeDuration;
                yield return null;
            }
            fadeCanvas.alpha = 1f;
        }

        SceneManager.LoadScene(sceneName);
    }
}
