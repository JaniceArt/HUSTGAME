using System.Collections;
using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class HingeDoor : MonoBehaviour
{
    [Header("=== CÀI ĐẶT CỬA TỦ LẠNH ===")]
    [Tooltip("Góc xoay khi cửa đang đóng (thường là 0)")]
    public float closedAngleY = 0f;
    
    [Tooltip("Góc xoay khi cửa mở (ví dụ: 90 hoặc -90)")]
    public float openAngleY = 90f;
    
    [Tooltip("Tốc độ mở cửa")]
    public float openSpeed = 5f;

    public enum HingeSide { Left, Right, Center }

    [Header("=== AUTO PIVOT (TỰ ĐỘNG TẠO BẢN LỀ) ===")]
    [Tooltip("Bản lề cửa nằm ở mép nào?")]
    public HingeSide hingeSide = HingeSide.Left;

    [Header("=== ÂM THANH (Tuỳ chọn) ===")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 5f)] public float volume = 2f; // Cho phép tăng âm lượng lớn hơn
    [Tooltip("Tốc độ phát âm thanh (Pitch)")]
    [Range(0.1f, 3f)] public float soundSpeed = 1f;

    private bool isOpen = false;
    private bool isAnimating = false;
    
    private Transform pivotTransform; // Tâm xoay thực sự
    private Quaternion initialRotation; // Ghi nhớ góc xoay gốc
    private AudioSource audioSource;

    public bool IsOpen => isOpen;

    void Start()
    {
        InteractableObject io = GetComponent<InteractableObject>();
        if (io != null) io.type = InteractableType.FridgeDoor;

        CreateAutoPivot();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Âm thanh 2D để nghe to rõ hơn
    }

    void CreateAutoPivot()
    {
        // 1. Tính toán vị trí bản lề (Local Space)
        Vector3 localPivotPos = Vector3.zero;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            float offsetX = 0;
            float offsetZ = 0;
            
            // Tự động tìm trục nào dài hơn (chiều rộng của cánh cửa)
            if (box.size.x > box.size.z)
            {
                if (hingeSide == HingeSide.Left) offsetX = -box.size.x / 2f;
                else if (hingeSide == HingeSide.Right) offsetX = box.size.x / 2f;
            }
            else
            {
                if (hingeSide == HingeSide.Left) offsetZ = -box.size.z / 2f;
                else if (hingeSide == HingeSide.Right) offsetZ = box.size.z / 2f;
            }
            
            localPivotPos = box.center + new Vector3(offsetX, 0, offsetZ);
        }
        else
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null)
            {
                float offsetX = 0;
                float offsetZ = 0;
                
                if (mf.mesh.bounds.size.x > mf.mesh.bounds.size.z)
                {
                    if (hingeSide == HingeSide.Left) offsetX = -mf.mesh.bounds.extents.x;
                    else if (hingeSide == HingeSide.Right) offsetX = mf.mesh.bounds.extents.x;
                }
                else
                {
                    if (hingeSide == HingeSide.Left) offsetZ = -mf.mesh.bounds.extents.z;
                    else if (hingeSide == HingeSide.Right) offsetZ = mf.mesh.bounds.extents.z;
                }

                localPivotPos = mf.mesh.bounds.center + new Vector3(offsetX, 0, offsetZ);
            }
        }

        // 2. Chuyển vị trí bản lề ra World Space
        Vector3 worldPivotPos = transform.TransformPoint(localPivotPos);

        // 3. Tạo một GameObject tàng hình làm cái tâm xoay (Pivot)
        GameObject pivotObj = new GameObject("AutoPivot_" + gameObject.name);
        pivotObj.transform.position = worldPivotPos;
        pivotObj.transform.rotation = transform.rotation;
        
        // Đặt Pivot làm anh em với Cánh Cửa (cùng chung 1 cha)
        pivotObj.transform.parent = transform.parent;

        // Ép Cánh Cửa làm con của Pivot
        transform.parent = pivotObj.transform;

        // 4. Bắt đầu dùng cái Pivot này để xoay
        pivotTransform = pivotObj.transform;
        initialRotation = pivotTransform.localRotation;
    }

    public void Interact()
    {
        if (isAnimating || pivotTransform == null) return;
        
        isOpen = !isOpen;
        
        // Phát âm thanh
        if (audioSource != null)
        {
            AudioClip clipToPlay = isOpen ? openSound : closeSound;
            if (clipToPlay != null)
            {
                audioSource.pitch = soundSpeed;
                audioSource.PlayOneShot(clipToPlay, volume);
            }
        }

        StartCoroutine(AnimateDoor(isOpen ? openAngleY : closedAngleY));
    }

    IEnumerator AnimateDoor(float angleOffset)
    {
        isAnimating = true;
        
        Quaternion startRot = pivotTransform.localRotation;
        Quaternion targetRot = initialRotation * Quaternion.Euler(0, angleOffset, 0);

        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * openSpeed;
            pivotTransform.localRotation = Quaternion.Slerp(startRot, targetRot, progress);
            yield return null;
        }

        pivotTransform.localRotation = targetRot;
        isAnimating = false;
    }
}
