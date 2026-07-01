using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Sự kiện cuối Ngày 4:
/// 1. Đèn chớp tắt liên tục, có thể bật tắt model bóng ma lúc ẩn lúc hiện.
/// 2. Thoại nội tâm tức giận + Giao nhiệm vụ lấy chổi.
/// 3. Người chơi nhặt chổi -> Đổi nhiệm vụ.
/// 4. Dẫm vào Trigger ngoài cửa -> Tắt Player, Bật Cutscene của bạn làm, chạy đếm ngược chuyển sang ngày mới.
/// </summary>
public class Day4EndCutsceneStep : SequenceStep
{
    [Header("=== GIAI ĐOẠN 1: CHỚP ĐÈN ===")]
    [Tooltip("Danh sách các đèn trong phòng để chớp tắt cùng lúc")]
    public List<Light> roomLights;
    [Tooltip("Âm thanh xẹt điện lúc chớp đèn")]
    public AudioClip flickerSound;
    [Tooltip("Âm lượng xẹt điện (0 đến 1)")]
    [Range(0f, 1f)] public float flickerVolume = 1f;
    [Tooltip("Bóng ma chập chờn (Sẽ bật tắt liên tục cùng với đèn)")]
    public GameObject flickeringGhost;
    [Tooltip("Thời gian đèn chớp tắt (giây)")]
    public float flickerDuration = 6f;
    
    [Header("=== GIAI ĐOẠN ÂM NHẠC ===")]
    [Tooltip("Nhạc nền rùng rợn khi con ma vừa xuất hiện trong bóng tối")]
    public AudioClip ghostAppearMusic;
    [Tooltip("Âm lượng nhạc rùng rợn (0 đến 1)")]
    [Range(0f, 1f)] public float ghostAppearVolume = 1f;

    [Tooltip("Nhạc hành động dồn dập, từ từ lớn lên khi main bắt đầu tức giận")]
    public AudioClip actionChaseMusic;
    [Tooltip("Âm lượng nhạc hành động đuổi bắt (0 đến 1)")]
    [Range(0f, 1f)] public float actionChaseVolume = 1f;

    [Header("=== GIAI ĐOẠN 2: THOẠI & NHIỆM VỤ ===")]
    [Tooltip("Thoại lầm bầm của main (VD: Tức quá, dám trêu ông à)")]
    public List<DialogNode> angryDialog;

    [Header("=== GIAI ĐOẠN 3: LẤY CHỔI VÀ ĐUỔI THEO ===")]
    [Tooltip("Cây chổi dưới đất để người chơi nhặt")]
    public GameObject broomInteractable;
    [Tooltip("Cây chổi dính trên tay (hiển thị trước camera)")]
    public GameObject broomOnHand;
    [Tooltip("Bức tường tàng hình chặn cửa, không cho ra ngoài khi chưa nhặt chổi")]
    public GameObject invisibleWall;
    [Tooltip("Cái Trigger (BoxCollider) ở NGAY CỬA. Người chơi ra khỏi cửa dẫm vào thì ma mới quay đầu chạy.")]
    public Collider doorTrigger;

    [Tooltip("Animator của bóng ma để kích hoạt nó chạy đi khi nhặt chổi")]
    public Animator ghostAnimator;
    [Tooltip("Tên biến Trigger trong Animator để bắt đầu chạy")]
    public string ghostRunTrigger = "Run";
    [Tooltip("Kéo một cái Empty GameObject vào đây làm Điểm Đến để ma chạy tới")]
    public Transform ghostRunDestination;
    [Tooltip("Tốc độ chạy của ma")]
    public float ghostRunSpeed = 6f;
    
    [Header("=== GIAI ĐOẠN 4: CUTSCENE (SLOT CỦA BẠN TÔI) ===")]
    [Tooltip("Cái Trigger (BoxCollider) ngoài cửa. Khi dẫm vào sẽ kích hoạt Cutscene.")]
    public Collider cutsceneTrigger;
    
    [Tooltip("KÉO FILE ANIMATION CỦA BẠN BẠN VÀO ĐÂY (Tắt mặc định nhé)")]
    public GameObject friendCutscenePrefab;
    
    [Tooltip("Thời gian độ dài của đoạn phim (giây). Hết phim sẽ tự qua ngày 5.")]
    public float cutsceneDuration = 5f;

    [Header("=== GIAI ĐOẠN 5: CUTSCENE 2 (TÙY CHỌN) ===")]
    [Tooltip("KÉO FILE CUTSCENE 2 VÀO ĐÂY (Nó sẽ tự động phát ngay sau khi Cutscene 1 chạy xong)")]
    public GameObject friendCutscenePrefab2;
    
    [Tooltip("Thời gian độ dài của Cutscene 2 (giây). Hết phim sẽ tự qua ngày 5.")]
    public float cutscene2Duration = 5f;

    [Header("=== ÂM THANH NÉM CHỔI ===")]
    [Tooltip("Âm thanh lúc ném chổi (sẽ phát trong lúc chiếu Cutscene)")]
    public AudioClip throwMopSound;
    
    [Tooltip("Độ trễ (giây) kể từ lúc phim bắt đầu cho đến khi phát tiếng ném chổi (để căn khớp với hình ảnh)")]
    public float throwMopSoundDelay = 0.5f;

    public static Day4EndCutsceneStep Instance;
    [HideInInspector] public bool isWaitingForBroom = false;

    private bool isActiveStep = false;
    private bool isWaitingForTrap = false;

    private AudioSource audioSource;
    private FirstPersonController playerController;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Tự động giấu cục cutscene đi ngay lúc mới bật Game để không bị lộ
        if (friendCutscenePrefab != null) friendCutscenePrefab.SetActive(false);
        if (friendCutscenePrefab2 != null) friendCutscenePrefab2.SetActive(false);
    }

    private bool isCutsceneStarted = false;
    private bool isWaitingForDoor = false;
    private bool isLightCurrentlyOn = true;
    private AudioSource bgmSource; // Trình phát nhạc nền riêng
    private float warningCooldown = 0f; // Chờ 1 chút mới hiện lại cảnh báo để không bị spam

    public override void StartStep()
    {
        isActiveStep = true;
        isWaitingForBroom = false;
        isWaitingForDoor = false;
        isWaitingForTrap = false;
        isCutsceneStarted = false;

        playerController = FindObjectOfType<FirstPersonController>();

        // Thiết lập bộ phát nhạc nền
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.spatialBlend = 0f; // Nhạc 2D vang khắp đầu
            bgmSource.loop = true;
        }
        bgmSource.Stop();

        // Bật nhạc nền rùng rợn ngay từ lúc bắt đầu chớp đèn
        if (ghostAppearMusic != null)
        {
            bgmSource.clip = ghostAppearMusic;
            bgmSource.volume = ghostAppearVolume;
            bgmSource.Play();
        }

        // Thiết lập ban đầu
        if (flickeringGhost != null) flickeringGhost.SetActive(false); // Ma ẩn lúc đầu
        if (broomOnHand != null) broomOnHand.SetActive(false);
        // Dựng tường tàng hình chặn cửa!
        if (invisibleWall != null) invisibleWall.SetActive(true);

        // Cây chổi dưới đất LUN LUN HIỆN HỮU (không tắt)
        if (broomInteractable != null) broomInteractable.SetActive(true); 
        
        // LUÔN BẬT BẪY CỬA ĐỂ BẮT HÀNH ĐỘNG ĐI RA SỚM
        if (doorTrigger != null) doorTrigger.gameObject.SetActive(true);
        
        if (cutsceneTrigger != null) cutsceneTrigger.gameObject.SetActive(false);
        if (friendCutscenePrefab != null) friendCutscenePrefab.SetActive(false);

        StartCoroutine(ContinuousFlicker());
        StartCoroutine(StorySequence());
    }

    IEnumerator ContinuousFlicker()
    {
        if (flickerSound != null) 
        {
            audioSource.clip = flickerSound;
            audioSource.volume = flickerVolume;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        bool isLightOn = true;
        List<float> originalIntensities = new List<float>();
        
        if (roomLights != null)
        {
            foreach(var light in roomLights)
            {
                if (light != null) originalIntensities.Add(light.intensity);
                else originalIntensities.Add(0f);
            }
        }

        // Chớp liên tục cho đến khi Cutscene bắt đầu
        while (!isCutsceneStarted)
        {
            isLightOn = !isLightOn;
            isLightCurrentlyOn = isLightOn;

            // Bật tắt tất cả các đèn
            if (roomLights != null)
            {
                for (int i = 0; i < roomLights.Count; i++)
                {
                    if (roomLights[i] != null) 
                    {
                        roomLights[i].intensity = isLightOn ? originalIntensities[i] : 0f;
                    }
                }
            }
            
            yield return new WaitForSeconds(Random.Range(0.2f, 0.6f)); // Chớp chậm kinh dị
        }

        // Trả lại đèn bình thường khi xem Cutscene
        if (roomLights != null)
        {
            for (int i = 0; i < roomLights.Count; i++)
            {
                if (roomLights[i] != null) roomLights[i].intensity = originalIntensities[i];
            }
        }
        // Tắt tiếng chớp đèn
        if (flickerSound != null && audioSource.clip == flickerSound)
        {
            audioSource.Stop();
        }
    }

    IEnumerator StorySequence()
    {
        // Vừa nhấp nháy là đợi đèn tắt phát cho ma xuất hiện luôn (bỏ thời gian đợi 6 giây)
        while (isLightCurrentlyOn)
        {
            yield return null;
        }

        // Hiện nguyên hình trong bóng tối!
        if (flickeringGhost != null) flickeringGhost.SetActive(true);

        // Chờ đến khi người chơi NHÌN THẲNG VÀO con ma 5 giây liên tục (Chống nhìn xuyên tường)
        bool isGhostSeen = false;
        Camera cam = Camera.main;
        if (cam == null && playerController != null) cam = playerController.GetComponentInChildren<Camera>();

        float lookAtGhostTimer = 0f;

        while (!isGhostSeen)
        {
            if (cam != null && flickeringGhost != null)
            {
                Vector3 targetPos = flickeringGhost.transform.position + Vector3.up * 1.2f;
                Vector3 dirToGhost = (targetPos - cam.transform.position).normalized;
                float angle = Vector3.Angle(cam.transform.forward, dirToGhost);
                float distToGhost = Vector3.Distance(cam.transform.position, targetPos);

                bool currentlyLooking = false;

                // Góc mở rộng 45 độ, bỏ giới hạn khoảng cách vì sảnh rất dài
                if (angle < 45f)
                {
                    // Raycast chống nhìn xuyên tường (bỏ qua Trigger)
                    RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, dirToGhost, distToGhost, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    bool hitWall = false;
                    foreach(var h in hits)
                    {
                        // Bỏ qua cơ thể của chính người chơi
                        if (h.distance < 1.5f || h.collider.CompareTag("Player") || h.collider.transform.root.CompareTag("Player")) continue;

                        // Bỏ qua bức tường tàng hình chặn cửa!
                        if (invisibleWall != null && h.collider.gameObject == invisibleWall) continue;

                        if (h.collider.gameObject != flickeringGhost && !h.collider.transform.IsChildOf(flickeringGhost.transform))
                        {
                            hitWall = true;
                            break;
                        }
                    }
                    if (!hitWall)
                    {
                        currentlyLooking = true;
                    }
                }

                if (currentlyLooking)
                {
                    lookAtGhostTimer += Time.deltaTime;
                    if (lookAtGhostTimer >= 5f)
                    {
                        isGhostSeen = true;
                    }
                }
                else
                {
                    lookAtGhostTimer = 0f; // Quay đi chỗ khác là đếm lại từ đầu
                }
            }
            yield return null; // Bắt buộc phải chạy mỗi frame để tính Time.deltaTime chính xác
        }

        // Vừa nhìn đủ 5s là thoại luôn, không cần đứng chờ thêm nữa

        // Đổi nhạc Action đuổi bắt
        if (actionChaseMusic != null)
        {
            StartCoroutine(CrossfadeMusic(actionChaseMusic, actionChaseVolume, 3f)); // Fade đổi nhạc trong 3 giây
        }

        // Thoại tức giận
        yield return new WaitForSeconds(0.5f);
        
        if (DialogManager.Instance != null && angryDialog != null && angryDialog.Count > 0)
        {
            bool isDialogDone = false;
            DialogManager.Instance.StartDialogSequence(angryDialog, (result) => { isDialogDone = true; });
            while (!isDialogDone) yield return null;
        }

        // Hiện nhiệm vụ
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.ShowObjective("Lấy cây chổi đuổi theo");
        }
        
        isWaitingForBroom = true; // Bật cho phép click nhặt chổi
    }

    public void OnBroomPickedUp()
    {
        if (!isActiveStep || !isWaitingForBroom) return;

        // Đã nhặt chổi!
        isWaitingForBroom = false;
        if (broomInteractable != null) broomInteractable.SetActive(false);
        if (broomOnHand != null) broomOnHand.SetActive(true); // Gắn lên tay

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.CompleteObjective();
        }

        // Xóa bức tường tàng hình để mở đường máu cho người chơi lao ra ngoài
        if (invisibleWall != null) invisibleWall.SetActive(false);

        // Chuyển sang trạng thái chờ dẫm bẫy cửa để chạy
        isWaitingForDoor = true;
    }

    IEnumerator GhostRunAwayRoutine()
    {
        if (flickeringGhost == null || ghostRunDestination == null) yield break;

        // 1. Kích hoạt Animation chạy
        if (ghostAnimator != null) ghostAnimator.SetTrigger(ghostRunTrigger);

        Vector3 targetPos = ghostRunDestination.position;
        targetPos.y = flickeringGhost.transform.position.y; // Cố định chiều cao, không cho ma bay lên trời hoặc lún xuống đất

        // 2. Quay đầu nhanh về phía mục tiêu
        Quaternion lookRotation = Quaternion.LookRotation(targetPos - flickeringGhost.transform.position);
        float turnTimer = 0f;
        while (turnTimer < 0.2f) // Quay đầu mất khoảng 0.2s
        {
            turnTimer += Time.deltaTime;
            flickeringGhost.transform.rotation = Quaternion.Slerp(flickeringGhost.transform.rotation, lookRotation, 15f * Time.deltaTime);
            yield return null;
        }
        flickeringGhost.transform.rotation = lookRotation; // Cố định góc quay cuối

        // 3. Chạy về phía mục tiêu
        while (Vector3.Distance(flickeringGhost.transform.position, targetPos) > 0.1f)
        {
            flickeringGhost.transform.position = Vector3.MoveTowards(flickeringGhost.transform.position, targetPos, ghostRunSpeed * Time.deltaTime);
            yield return null;
        }

        // Tới nơi thì tắt con ma đi để tránh kẹt
        flickeringGhost.SetActive(false);
    }

    // Hiệu ứng đổi nhạc mượt mà
    IEnumerator CrossfadeMusic(AudioClip newClip, float targetVolume, float duration)
    {
        if (bgmSource == null) yield break;
        
        float startVol = bgmSource.volume;
        float halfDuration = duration / 2f;
        
        // 1. Từ từ nhỏ tiếng nhạc cũ (Fade Out)
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVol * (Time.deltaTime / halfDuration);
            yield return null;
        }
        
        // 2. Đổi bài nhạc
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();
        
        // 3. Từ từ to tiếng nhạc mới (Fade In)
        while (bgmSource.volume < targetVolume)
        {
            bgmSource.volume += targetVolume * (Time.deltaTime / halfDuration);
            yield return null;
        }
        bgmSource.volume = targetVolume;
    }

    // Đặt đoạn code này vào trong một script tên là "TriggerCutscene" gắn trên cái BoxCollider ngoài hành lang
    // Nhưng để tiện quản lý, chúng ta có thể kiểm tra khoảng cách từ người chơi tới trigger ở đây luôn
    private void LateUpdate()
    {
        if (playerController == null) return;
        
        if (warningCooldown > 0) warningCooldown -= Time.deltaTime;

        // Bẫy 0: Cảnh báo chưa lấy chổi (Đã có nhiệm vụ chổi, nhưng chưa nhặt mà cố lao ra)
        if (isWaitingForBroom && !isWaitingForDoor && invisibleWall != null && invisibleWall.activeSelf)
        {
            Collider wallCollider = invisibleWall.GetComponent<Collider>();
            if (wallCollider != null)
            {
                Bounds checkBounds = wallCollider.bounds;
                checkBounds.Expand(1.5f); // Mở rộng vùng cảnh báo ra khoảng 0.75m quanh bức tường

                if (checkBounds.Contains(playerController.transform.position))
                {
                    if (warningCooldown <= 0f)
                    {
                        // Tạo một đoạn thoại ngắn để nhắc nhở
                        if (DialogManager.Instance != null)
                        {
                            List<DialogNode> warnDialog = new List<DialogNode>();
                            DialogNode node = new DialogNode();
                            node.sentence = "Mình cần cầm theo chổi!"; // Cố tình ra ngoài tay không thì main tự nhắc nhở
                            warnDialog.Add(node);
                            DialogManager.Instance.StartDialogSequence(warnDialog, null);
                        }
                        warningCooldown = 4f; // 4 giây sau mới nhắc lại nếu người chơi vẫn cứ ngoan cố đâm đầu vào tường
                    }
                }
            }
        }

        // Bẫy 1: Ra khỏi cửa -> Ma quay đầu chạy
        if (isWaitingForDoor && doorTrigger != null)
        {
            if (doorTrigger.bounds.Contains(playerController.transform.position))
            {
                isWaitingForDoor = false;
                doorTrigger.gameObject.SetActive(false);
                
                StartCoroutine(GhostRunAwayRoutine());
                
                // Kích hoạt bẫy 2: Dẫm vào thì chiếu Cutscene
                if (cutsceneTrigger != null) cutsceneTrigger.gameObject.SetActive(true);
                isWaitingForTrap = true;
            }
        }

        // Bẫy 2: Dẫm trigger cutscene -> Chiếu phim
        if (isWaitingForTrap && cutsceneTrigger != null)
        {
            if (cutsceneTrigger.bounds.Contains(playerController.transform.position))
            {
                isWaitingForTrap = false;
                cutsceneTrigger.gameObject.SetActive(false);
                StartCoroutine(PlayCutsceneRoutine());
            }
        }
    }

    private Camera playerCamera; // Lưu lại để bật lại sau

    IEnumerator PlayCutsceneRoutine()
    {
        isCutsceneStarted = true; // Ngắt vòng lặp chớp đèn!

        // BẮT ĐẦU CHUYỂN GIAO QUYỀN LỰC CHO CUTSCENE
        FirstPersonController.CanMove = false;
        
        // TẮT model người chơi (để không bị chồng hình)
        if (playerController != null)
        {
            foreach (var renderer in playerController.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
        }

        // TẮT HẲN cây chổi trên tay bằng SetActive (để chắc chắn không bị hiện lại)
        if (broomOnHand != null) broomOnHand.SetActive(false);
        
        if (flickeringGhost != null) flickeringGhost.SetActive(false);
        
        // Tắt UI nhiệm vụ
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.HideObjective();

        // 1. CHIẾU CUTSCENE 1 (NÉM CHỔI)
        if (friendCutscenePrefab != null)
        {
            SetupAndPlayCutscene(friendCutscenePrefab);
        }

        // Bắt đầu hẹn giờ phát tiếng ném chổi
        if (throwMopSound != null)
        {
            StartCoroutine(PlayThrowMopSound());
        }

        // NGỒI XEM CUTSCENE 1
        yield return new WaitForSeconds(cutsceneDuration);

        // 2. FADE OUT NHẠC NỀN
        if (bgmSource != null && bgmSource.isPlaying)
        {
            StartCoroutine(CrossfadeMusic(null, 0f, 2f)); // Mượn hàm Crossfade để giảm âm lượng về 0 trong 2 giây
        }

        // 3. TẮT CUTSCENE 1, CHIẾU CUTSCENE 2 (NẾU CÓ)
        if (friendCutscenePrefab != null) friendCutscenePrefab.SetActive(false);

        if (friendCutscenePrefab2 != null)
        {
            SetupAndPlayCutscene(friendCutscenePrefab2);
            yield return new WaitForSeconds(cutscene2Duration);
            friendCutscenePrefab2.SetActive(false);
        }

        // 4. HẾT PHIM -> CHUYỂN NGÀY
        if (Camera.main != null && !Camera.main.enabled)
        {
            Camera.main.enabled = true;
        }
        
        if (playerController != null)
        {
            // Bật lại tất cả Camera của người chơi (vũ khí, UI...)
            Camera[] playerCams = playerController.GetComponentsInChildren<Camera>(true);
            foreach (Camera pc in playerCams)
            {
                pc.enabled = true;
            }

            foreach (var renderer in playerController.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
        }

        FirstPersonController.CanMove = true;
        CompleteStep();
    }

    private void SetupAndPlayCutscene(GameObject cutscenePrefab)
    {
        PlayableDirector director = cutscenePrefab.GetComponentInChildren<PlayableDirector>(true);
        if (director != null)
        {
            director.playOnAwake = false;
            
            TimelineAsset timelineAsset = director.playableAsset as TimelineAsset;
            if (timelineAsset != null)
            {
                Camera mainCam = Camera.main;
                if (mainCam == null && playerController != null)
                {
                    mainCam = playerController.GetComponentInChildren<Camera>();
                }

                Component cineBrain = null;
                if (mainCam != null)
                {
                    cineBrain = mainCam.GetComponent("CinemachineBrain");
                    if (cineBrain == null) cineBrain = mainCam.GetComponent("Cinemachine.CinemachineBrain");
                    
                    if (cineBrain == null)
                    {
                        System.Type brainType = null;
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            brainType = assembly.GetType("Unity.Cinemachine.CinemachineBrain") ?? assembly.GetType("Cinemachine.CinemachineBrain");
                            if (brainType != null) break;
                        }
                        
                        if (brainType != null)
                        {
                            cineBrain = mainCam.gameObject.AddComponent(brainType);
                        }
                    }
                }

                if (cineBrain == null)
                {
                    Transform virtualCam = null;
                    foreach (Transform child in cutscenePrefab.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.Contains("CinemachineCamera"))
                        {
                            virtualCam = child;
                            break;
                        }
                    }

                    if (virtualCam != null)
                    {
                        Camera fallbackCam = virtualCam.GetComponent<Camera>();
                        if (fallbackCam == null) fallbackCam = virtualCam.gameObject.AddComponent<Camera>();
                        fallbackCam.enabled = true;
                    }
                }

                if (playerController != null)
                {
                    Camera[] playerCams = playerController.GetComponentsInChildren<Camera>();
                    foreach (Camera pc in playerCams)
                    {
                        if (pc == mainCam) continue;
                        pc.enabled = false;
                    }
                }

                foreach (var track in timelineAsset.GetOutputTracks())
                {
                    if (cineBrain != null && (track.GetType().Name.Contains("CinemachineTrack") || track.GetType().Name.Contains("Cinemachine")))
                    {
                        director.SetGenericBinding(track, cineBrain);
                    }
                    else if (director.GetGenericBinding(track) == null)
                    {
                        Transform targetChild = null;
                        foreach (Transform child in cutscenePrefab.GetComponentsInChildren<Transform>(true))
                        {
                            if (child.name == track.name)
                            {
                                targetChild = child;
                                break;
                            }
                        }

                        if (targetChild != null)
                        {
                            if (track is AnimationTrack)
                            {
                                Animator anim = targetChild.GetComponent<Animator>();
                                if (anim != null) director.SetGenericBinding(track, anim);
                            }
                            else if (track.GetType().Name.Contains("ActivationTrack"))
                            {
                                director.SetGenericBinding(track, targetChild.gameObject);
                            }
                        }
                    }
                }
            }
            
            cutscenePrefab.SetActive(true);
            director.RebuildGraph();
            director.time = 0;
            director.Play();
        }
    }

    IEnumerator PlayThrowMopSound()
    {
        // Chờ đúng thời gian Delay do bạn căn chỉnh trên Inspector
        yield return new WaitForSeconds(throwMopSoundDelay);
        if (throwMopSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(throwMopSound, 1f);
        }
    }
}
