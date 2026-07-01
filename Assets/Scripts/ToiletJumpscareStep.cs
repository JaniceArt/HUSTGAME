using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToiletJumpscareStep : SequenceStep
{
    [Header("=== CÀI ĐẶT JUMPSCARE ===")]
    [Tooltip("Nhân vật Béo (kéo prefab hoặc object Béo vào đây)")]
    public GameObject fatGuy;
    [Tooltip("Vị trí Béo bắt đầu chạy ra (Bên trong nhà vệ sinh, khuất tầm nhìn)")]
    public Transform jumpscareStartPoint;

    [Tooltip("Vị trí Béo dừng lại sau khi lao ra ngoài (Trước cửa nhà vệ sinh)")]
    public Transform jumpscareStopPoint;

    [Tooltip("Âm thanh Jumpscare (Giật mình)")]
    public AudioClip jumpscareSound;

    [Tooltip("Danh sách câu thoại của Béo sau khi jumpscare")]
    public List<DialogNode> fatGuyDialogs;

    [Tooltip("Điểm Béo sẽ đi bộ ra để đi về (Kéo StartPoint ngoài đường vào đây)")]
    public Transform exitPoint;

    [Header("=== VÙNG KÍCH HOẠT (TRIGGER) ===")]
    [Tooltip("Vùng mà người chơi bước vào sẽ kích hoạt Jumpscare. Bỏ trống nếu muốn tự động kích hoạt sau vài giây.")]
    public Collider triggerZone;

    private bool isPlayerInZone = false;

    public override void StartStep()
    {
        Debug.Log("[JUMPSCARE] Đã bắt đầu ToiletJumpscareStep!");

        if (fatGuy != null) fatGuy.SetActive(false); // Giấu Béo đi trước

        // Báo nhiệm vụ mới
        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveTitle))
        {
            ObjectiveManager.Instance.ShowObjective(objectiveTitle);
        }

        if (triggerZone != null)
        {
            Debug.Log("[JUMPSCARE] Đang chờ người chơi bước vào vùng TriggerZone: " + triggerZone.gameObject.name);
            // Cần có 1 script phụ để báo Trigger, hoặc dùng Coroutine để check khoảng cách
            StartCoroutine(CheckPlayerDistanceRoutine());
        }
        else
        {
            Debug.Log("[JUMPSCARE] Không có TriggerZone, kích hoạt nhảy bổ ngay lập tức!");
            // Kích hoạt ngay lập tức
            TriggerJumpscare();
        }
    }

    IEnumerator CheckPlayerDistanceRoutine()
    {
        FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
        Transform player = (playerController != null) ? playerController.transform : null;

        // Fallback nếu không tìm thấy FirstPersonController
        if (player == null && Camera.main != null) 
        {
            player = Camera.main.transform.parent;
            if (player == null) player = Camera.main.transform;
        }

        if (player == null)
        {
            Debug.LogError("[JUMPSCARE] LỖI CỰC KỲ NGHIÊM TRỌNG: Không tìm thấy nhân vật Player!");
            yield break;
        }

        Debug.Log("[JUMPSCARE] Đã tìm thấy Player: " + player.gameObject.name);

        while (!isPlayerInZone && player != null)
        {
            // Kiểm tra bounds chuẩn 3D
            bool inBounds = triggerZone.bounds.Contains(player.position);
            
            // Nếu 3D hụt (do đặt Cube tuốt trên trần nhà Y=6.8), ta kiểm tra xem Player có đang đứng NGAY DƯỚI/TRÊN Box không (kiểm tra 2D)
            Bounds b = triggerZone.bounds;
            bool inBounds2D = (player.position.x >= b.min.x && player.position.x <= b.max.x) &&
                              (player.position.z >= b.min.z && player.position.z <= b.max.z);

            if (inBounds || inBounds2D)
            {
                Debug.Log("[JUMPSCARE] Người chơi đã bước chính xác vào TriggerZone!");
                isPlayerInZone = true;
                TriggerJumpscare();
                break;
            }
            yield return null;
        }
    }

    void TriggerJumpscare()
    {
        // 1. Khoá chuột người chơi
        FirstPersonController.CanMove = false;

        if (fatGuy != null)
        {
            // Ép buộc TẮT script Customer nếu bạn lỡ quên chưa xóa, để tránh việc nó tự đếm ngược và chuyển sang "giận dữ"
            Customer cusScript = fatGuy.GetComponent<Customer>();
            if (cusScript != null) cusScript.enabled = false;

            // Tạm tắt NavMeshAgent để dịch chuyển không bị lỗi
            UnityEngine.AI.NavMeshAgent agent = fatGuy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            // Bắt đầu chuỗi hù doạ quay lưng
            StartCoroutine(TurnAroundJumpscareRoutine());
        }
        else
        {
            // Fallback nếu thiếu setup
            StartCoroutine(StartDialogRoutine());
        }
    }

    IEnumerator TurnAroundJumpscareRoutine()
    {
        Transform player = null;
        FirstPersonController fpc = FindObjectOfType<FirstPersonController>();
        if (fpc != null) player = fpc.transform;
        else if (Camera.main != null) player = Camera.main.transform;

        if (player != null)
        {
            // 2. Tính toán vị trí đằng sau người chơi (cách khoảng 1.5 mét)
            Vector3 spawnPos = player.position - player.forward * 1.5f;
            spawnPos.y = player.position.y; // Cân bằng độ cao

            // An toàn: Đảm bảo Béo không bị kẹt vào tường bằng cách bám vào NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            // 3. Dịch chuyển Béo ra sau lưng
            fatGuy.SetActive(true);
            fatGuy.transform.position = spawnPos;
            
            // Ép Béo nhìn về phía người chơi
            Vector3 dirToPlayer = (player.position - fatGuy.transform.position).normalized;
            dirToPlayer.y = 0;
            if (dirToPlayer != Vector3.zero)
            {
                fatGuy.transform.rotation = Quaternion.LookRotation(dirToPlayer);
            }

            // Chỉnh dáng đứng im, không đi, không giận
            Animator anim = fatGuy.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.speed = 1f;
                anim.SetBool("isWalking", false);
                anim.SetBool("isDancing", false);
                anim.SetBool("isAngry", false);
            }

            // 4. Phát âm thanh hù doạ
            if (jumpscareSound != null)
            {
                AudioSource.PlayClipAtPoint(jumpscareSound, player.position);
            }

            // 5. CƯỠNG CHẾ QUAY CAMERA 180 ĐỘ VỀ PHÍA BÉO (Xoay nhanh trong 0.25 giây)
            Quaternion startRot = player.rotation;
            Vector3 dirToFatGuy = (fatGuy.transform.position - player.position).normalized;
            dirToFatGuy.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(dirToFatGuy);

            float t = 0;
            float turnDuration = 0.25f; // Thời gian xoay cái rụp

            while (t < turnDuration)
            {
                t += Time.deltaTime;
                player.rotation = Quaternion.Slerp(startRot, targetRot, t / turnDuration);
                yield return null;
            }
            player.rotation = targetRot;

            // Chỉnh camera nhìn thẳng vào mặt Béo
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                Vector3 fatGuyFace = fatGuy.transform.position + Vector3.up * 1.6f;
                if (anim != null && anim.isHuman)
                {
                    try 
                    {
                        Transform headBone = anim.GetBoneTransform(HumanBodyBones.Head);
                        if (headBone != null) fatGuyFace = headBone.position;
                    } 
                    catch { }
                }
                else
                {
                    Collider col = fatGuy.GetComponentInChildren<Collider>();
                    if (col != null) fatGuyFace = new Vector3(fatGuy.transform.position.x, col.bounds.max.y - 0.2f, fatGuy.transform.position.z);
                }

                cam.transform.LookAt(fatGuyFace);

                // Đồng bộ góc X vào script FirstPersonController
                if (fpc != null)
                {
                    float newX = cam.transform.localEulerAngles.x;
                    if (newX > 180) newX -= 360;
                    fpc.xRotation = newX;
                }
            }
        }

        // Bắt đầu nói chuyện
        StartCoroutine(StartDialogRoutine());
    }

    IEnumerator StartDialogRoutine()
    {
        // Khựng lại 0.5s cho ngầu (đã đứng im lườm người chơi)
        yield return new WaitForSeconds(0.5f);

        // Đảm bảo không cho phép người chơi xoay chuột trong lúc nói chuyện
        FirstPersonController.CanMove = false;

        if (DialogManager.Instance != null && fatGuyDialogs != null && fatGuyDialogs.Count > 0)
        {
            DialogManager.Instance.StartDialogSequence(fatGuyDialogs, OnDialogFinished, new Color(1f, 0.75f, 0.8f)); // Màu baby pink cho gã béo
        }
        else
        {
            Debug.LogWarning("[JUMPSCARE] Lỗi: BẠN CHƯA THÊM CÂU THOẠI NÀO CHO BÉO! (Béo sẽ bỏ đi luôn)");
            OnDialogFinished(0);
        }
    }

    void OnDialogFinished(int choice)
    {
        FirstPersonController.CanMove = true; // Mở khoá chuột

        // Béo bỏ đi
        if (fatGuy != null)
        {
            StartCoroutine(FatGuyWalkAway());
        }
        else
        {
            CompleteStep();
        }
    }

    IEnumerator FatGuyWalkAway()
    {
        Animator anim = fatGuy.GetComponentInChildren<Animator>();
        if (anim != null) 
        {
            anim.SetBool("isWalking", true);
            anim.SetBool("isAngry", false);
        }

        UnityEngine.AI.NavMeshAgent agent = fatGuy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        if (agent != null && exitPoint != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.updateRotation = false; // Tắt tự động xoay để ép xoay bằng code cho chuẩn
            agent.speed = 2.5f; // Tốc độ đi bộ bình thường
            agent.SetDestination(exitPoint.position);

            // Đợi cho đến khi đi đến nơi
            while (agent.pathPending || agent.remainingDistance > 0.5f)
            {
                // Ép xoay mặt về hướng đang di chuyển
                if (agent.velocity.sqrMagnitude > 0.01f)
                {
                    Vector3 dir = agent.velocity.normalized;
                    dir.y = 0;
                    fatGuy.transform.rotation = Quaternion.Slerp(fatGuy.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                }
                yield return null;
            }
        }
        else
        {
            // Fallback nếu không có NavMeshAgent
            float walkTime = 3f;
            while (walkTime > 0)
            {
                if (exitPoint != null)
                {
                    Vector3 dir = (exitPoint.position - fatGuy.transform.position).normalized;
                    dir.y = 0;
                    if (dir != Vector3.zero)
                    {
                        fatGuy.transform.rotation = Quaternion.Slerp(fatGuy.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
                    }
                }
                
                fatGuy.transform.Translate(Vector3.forward * 2.5f * Time.deltaTime);
                walkTime -= Time.deltaTime;
                yield return null;
            }
        }

        fatGuy.SetActive(false);
        CompleteStep();
    }
}
