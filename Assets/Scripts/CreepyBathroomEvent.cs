using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sự kiện rùng rợn: Thuộc SequenceManager (Ví dụ Element 5).
/// Tới lượt bước này, nó mới bật lên. Khi player đến gần thì đi thụt lùi/đi vào trong rồi biến mất, sau đó chuyển sang Element 6.
/// </summary>
public class CreepyBathroomEvent : SequenceStep
{
    [Header("=== NHÂN VẬT MA ===")]
    [Tooltip("Kéo con ma (model 3D) vào đây")]
    public GameObject ghostModel;
    
    [Tooltip("Animator của con ma")]
    public Animator ghostAnimator;

    [Tooltip("Tên animation đi bộ (VD: Walk)")]
    public string walkAnimationName = "Walk";

    [Header("=== DI CHUYỂN ===")]
    [Tooltip("Điểm con ma sẽ đi tới để trốn (Vd: Tạo 1 cục Empty sâu trong NVS kéo vào)")]
    public Transform hidePoint;
    
    [Tooltip("Tốc độ đi bộ lúc trốn")]
    public float walkSpeed = 2f;

    [Header("=== ÂM THANH (Tùy chọn) ===")]
    [Tooltip("Âm thanh dọa ma khi nó bắt đầu bỏ đi (VD: Tiếng sột soạt, tiếng bước chân)")]
    public AudioClip scareSound;

    [Header("=== THOẠI (Tùy chọn) ===")]
    [Tooltip("Main sẽ nói gì sau khi con ma biến mất? (Bấm dấu + để thêm câu, để trống nếu không nói)")]
    [TextArea(2, 3)]
    public List<string> monologueLines;

    private bool hasTriggered = false;

    // SequenceManager sẽ gọi hàm này khi đến lượt (Element 5)
    public override void StartStep()
    {
        // Chắc chắn là con ma đang bật lên để đứng lấp ló
        if (ghostModel != null)
        {
            ghostModel.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem người chạm vào có phải là Player không (chỉ chạy 1 lần)
        if (!hasTriggered && other.CompareTag("Player") && !isCompleted)
        {
            hasTriggered = true;
            StartCoroutine(GhostWalkAway());
        }
    }

    IEnumerator GhostWalkAway()
    {
        // 1. Phát âm thanh (nếu có)
        if (scareSound != null)
        {
            AudioSource.PlayClipAtPoint(scareSound, ghostModel.transform.position, 1f);
        }

        // 2. Chạy animation đi bộ
        if (ghostAnimator != null && !string.IsNullOrEmpty(walkAnimationName))
        {
            bool played = false;
            foreach (AnimatorControllerParameter param in ghostAnimator.parameters)
            {
                if (param.name == "AnimState")
                {
                    ghostAnimator.SetInteger("AnimState", 1); 
                    played = true;
                    break;
                }
            }
            if (!played) ghostAnimator.Play(walkAnimationName);
        }

        // 3. Xoay mặt và di chuyển dần về phía điểm trốn
        if (hidePoint != null && ghostModel != null)
        {
            Vector3 direction = (hidePoint.position - ghostModel.transform.position).normalized;
            direction.y = 0; 

            if (direction != Vector3.zero)
            {
                ghostModel.transform.rotation = Quaternion.LookRotation(direction);
            }

            while (ghostModel != null && Vector3.Distance(new Vector3(ghostModel.transform.position.x, 0, ghostModel.transform.position.z), new Vector3(hidePoint.position.x, 0, hidePoint.position.z)) > 0.1f)
            {
                ghostModel.transform.position = Vector3.MoveTowards(
                    ghostModel.transform.position, 
                    hidePoint.position, 
                    walkSpeed * Time.deltaTime
                );
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 4. Đi đến nơi thì tàng hình (xóa sổ con ma)
        if (ghostModel != null)
        {
            Destroy(ghostModel);
        }
        
        // 5. Main tự thoại (nếu có nhập chữ)
        if (monologueLines != null && monologueLines.Count > 0 && DialogManager.Instance != null)
        {
            List<DialogNode> nodes = new List<DialogNode>();
            foreach (string sentence in monologueLines)
            {
                if (!string.IsNullOrEmpty(sentence))
                {
                    nodes.Add(new DialogNode { sentence = sentence, hasChoices = false });
                }
            }

            if (nodes.Count > 0)
            {
                // Gọi hộp thoại lên, người chơi đọc và bấm Space
                DialogManager.Instance.StartDialogSequence(nodes, (result) => 
                {
                    // Đọc xong hết thoại thì mới nhảy sang Element tiếp theo
                    CompleteStep();
                });
            }
            else
            {
                CompleteStep(); // Nhảy bước luôn nếu danh sách trống
            }
        }
        else
        {
            CompleteStep(); // Nhảy bước luôn nếu không có thoại
        }
    }
}
