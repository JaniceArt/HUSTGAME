using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gắn vào scene DayTransition.
/// Hiện text "Day 2", fade in/out rồi tự chuyển sang gameplay tiếp theo.
/// </summary>
public class DayTransitionController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI dayText;         // Text "Day 2"
    public CanvasGroup canvasGroup;         // CanvasGroup chứa toàn bộ UI

    [Header("Cài đặt")]
    public string nextSceneName = "Day2_Game";
    public string dayLabel      = "Day 2";

    [Tooltip("Thời gian hiện text (giây)")]
    public float displayDuration = 2f;

    [Tooltip("Thời gian fade in / fade out (giây)")]
    public float fadeDuration = 0.8f;

    // -------------------------------------------------------

    void Start()
    {
        if (dayText != null)
            dayText.text = dayLabel;

        StartCoroutine(PlayTransition());
    }

    IEnumerator PlayTransition()
    {
        // 1. Fade IN (đen → hiện text)
        yield return Fade(0f, 1f);

        // 2. Giữ text
        yield return new WaitForSeconds(displayDuration);

        // 3. Fade OUT (text → đen)
        yield return Fade(1f, 0f);

        // 4. Load scene tiếp theo
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
