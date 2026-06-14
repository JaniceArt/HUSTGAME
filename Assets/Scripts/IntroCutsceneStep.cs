using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cutscene đầu game: Khóa người chơi -> Đọc thoại -> Mở khóa và bắt đầu bán hàng.
/// </summary>
public class IntroCutsceneStep : SequenceStep
{
    [Header("=== CẤU HÌNH CUTSCENE ĐẦU GAME ===")]
    [Tooltip("Danh sách các câu thoại của nhân vật chính lúc vừa vào game (ấn dấu + để thêm nhiều câu)")]
    [TextArea(2, 4)]
    public List<string> introDialogs = new List<string> { "Hôm nay lại một ngày làm việc mệt mỏi..." };

    [Tooltip("Thời gian đứng yên trước khi bắt đầu nói (giây)")]
    public float delayBeforeDialog = 1.5f;

    [Header("=== CHUYỂN CẢNH SAU CUTSCENE ===")]
    [Tooltip("Điểm người chơi sẽ bị dịch chuyển tới để bắt đầu bán hàng (Tạo 1 cục Empty Object sau quầy và kéo vào đây)")]
    public Transform gameplaySpawnPoint;

    [Tooltip("Hiệu ứng mờ đen màn hình (Tạo Canvas -> Panel đen -> Thêm CanvasGroup, và kéo vào đây)")]
    public CanvasGroup fadeCanvas;
    
    [Tooltip("Thời gian mờ dần (giây)")]
    public float fadeDuration = 1f;

    public override void StartStep()
    {
        // Khoá di chuyển và xoay camera
        FirstPersonController.CanMove = false; 

        // Bắt đầu chờ và hiện thoại
        StartCoroutine(CutsceneRoutine());
    }

    IEnumerator CutsceneRoutine()
    {
        // Chờ 1 chút để màn hình ổn định
        yield return new WaitForSeconds(delayBeforeDialog);

        if (DialogManager.Instance != null && introDialogs != null && introDialogs.Count > 0)
        {
            List<DialogNode> nodes = new List<DialogNode>();
            foreach (string sentence in introDialogs)
            {
                if (!string.IsNullOrEmpty(sentence))
                {
                    nodes.Add(new DialogNode { sentence = sentence, hasChoices = false });
                }
            }

            if (nodes.Count > 0)
            {
                // Bật Dialog
                DialogManager.Instance.StartDialogSequence(nodes, (result) => 
                {
                    StartCoroutine(TransitionToGameplay());
                });
            }
            else
            {
                StartCoroutine(TransitionToGameplay());
            }
        }
        else
        {
            // Nếu không có thoại thì bỏ qua luôn
            StartCoroutine(TransitionToGameplay());
        }
    }

    IEnumerator TransitionToGameplay()
    {
        // 1. Fade out màn hình ra màu đen
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

        // 2. Dịch chuyển người chơi đến chỗ bán hàng
        if (gameplaySpawnPoint != null)
        {
            FirstPersonController player = FindObjectOfType<FirstPersonController>();
            if (player != null)
            {
                // Tắt Component CharacterController (nếu có) trước khi dịch chuyển để tránh lỗi vật lý
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = gameplaySpawnPoint.position;
                player.transform.rotation = gameplaySpawnPoint.rotation;

                if (cc != null) cc.enabled = true;
            }
        }

        // 3. Đợi 0.5s ở màn hình đen cho ổn định
        yield return new WaitForSeconds(0.5f);

        // 4. Fade in (Màn hình sáng dần lại)
        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvas.alpha = 1f - (t / fadeDuration);
                yield return null;
            }
            fadeCanvas.alpha = 0f;
        }

        // Mở khoá cho phép người chơi di chuyển và xoay chuột
        FirstPersonController.CanMove = true; 

        // Báo cho SequenceManager gọi bước tiếp theo (khách hàng đầu tiên)
        CompleteStep(); 
    }
}
