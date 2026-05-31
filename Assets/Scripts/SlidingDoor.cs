using System.Collections;
using UnityEngine;

/// <summary>
/// Hiệu ứng cửa xếp accordion (cửa kéo thu gọn lại một bên).
/// Script co lại theo trục X trong khi giữ nguyên một cạnh cố định —
/// giống đúng như cửa xếp inox ngoài đời thực.
/// </summary>
public class SlidingDoor : MonoBehaviour
{
    [Header("Cài đặt cửa xếp")]

    [Tooltip("Cạnh nào đứng yên khi cửa thu lại?\n" +
             "Right = cạnh phải cố định (cửa thu về phải)\n" +
             "Left  = cạnh trái cố định (cửa thu về trái)")]
    public DoorAnchor anchorSide = DoorAnchor.Right;

    [Tooltip("Thời gian mở / đóng (giây)")]
    public float openDuration = 0.7f;

    [Tooltip("Tỉ lệ scale X khi cửa mở hoàn toàn (0.30 = dải dày ~30% chiều rộng cửa)")]
    [Range(0f, 0.6f)]
    public float openScaleX = 0.30f;

    [Tooltip("Curve chuyển động — EaseInOut cho cảm giác mượt")]
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Âm thanh (tuỳ chọn)")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    // -------------------------------------------------------

    public enum DoorAnchor { Left, Right }

    private Vector3 _closedLocalPos;
    private Vector3 _closedLocalScale;
    private float   _halfWidthLocal;   // nửa chiều rộng gốc (local units)

    private bool _isOpen   = false;
    private bool _isMoving = false;

    private AudioSource _audio;

    // -------------------------------------------------------

    void Awake()
    {
        _closedLocalPos   = transform.localPosition;
        _closedLocalScale = transform.localScale;

        // Đọc kích thước mesh thực tế:
        // - Unity Plane: mesh rộng 10 units → extents.x = 5
        // - Unity Quad : mesh rộng  1 unit  → extents.x = 0.5
        // - Custom mesh: tuỳ mesh
        MeshFilter mf = GetComponent<MeshFilter>();
        float meshExtentX = (mf != null && mf.sharedMesh != null)
            ? mf.sharedMesh.bounds.extents.x
            : 0.5f; // fallback cho Quad

        // Nhân với localScale để ra đơn vị parent-space
        _halfWidthLocal = meshExtentX * _closedLocalScale.x;

        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake  = false;
        _audio.spatialBlend = 1f; // 3-D sound
    }

    // ===================== PUBLIC API =====================

    /// <summary>Gọi từ InteractionSystem khi bấm E</summary>
    public void Interact()
    {
        if (_isMoving) return;
        _isOpen = !_isOpen;
        StartCoroutine(_isOpen ? CoOpen() : CoClose());
    }

    public bool IsOpen   => _isOpen;
    public bool IsMoving => _isMoving;

    // ===================== COROUTINES =====================

    IEnumerator CoOpen()
    {
        _isMoving = true;
        if (openSound != null) _audio.PlayOneShot(openSound, volume);

        float startScaleX = transform.localScale.x;
        float endScaleX   = _closedLocalScale.x * openScaleX;

        // Cạnh cố định (world local X) khi đóng
        float anchorLocalX = anchorSide == DoorAnchor.Right
            ? _closedLocalPos.x + _halfWidthLocal   // cạnh phải
            : _closedLocalPos.x - _halfWidthLocal;  // cạnh trái

        yield return Animate(startScaleX, endScaleX, anchorLocalX);
        _isMoving = false;
    }

    IEnumerator CoClose()
    {
        _isMoving = true;
        if (closeSound != null) _audio.PlayOneShot(closeSound, volume);

        float startScaleX = transform.localScale.x;
        float endScaleX   = _closedLocalScale.x;   // trở về scale gốc

        float anchorLocalX = anchorSide == DoorAnchor.Right
            ? _closedLocalPos.x + _halfWidthLocal
            : _closedLocalPos.x - _halfWidthLocal;

        yield return Animate(startScaleX, endScaleX, anchorLocalX);

        // Snap chính xác về vị trí gốc
        transform.localPosition = _closedLocalPos;
        transform.localScale    = _closedLocalScale;
        _isMoving = false;
    }

    // Coroutine dùng chung: animate scaleX + giữ cạnh anchor cố định
    IEnumerator Animate(float fromScaleX, float toScaleX, float anchorLocalX)
    {
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t       = Mathf.Clamp01(elapsed / openDuration);
            float easedT  = curve.Evaluate(t);
            float curScaleX = Mathf.Lerp(fromScaleX, toScaleX, easedT);

            // Điều chỉnh position để cạnh anchor không dịch chuyển
            float halfCur = curScaleX * 0.5f;
            float newPosX = anchorSide == DoorAnchor.Right
                ? anchorLocalX - halfCur   // anchor phải → center đi về phải
                : anchorLocalX + halfCur;  // anchor trái  → center đi về trái

            transform.localScale    = new Vector3(curScaleX,
                                                  _closedLocalScale.y,
                                                  _closedLocalScale.z);
            transform.localPosition = new Vector3(newPosX,
                                                  _closedLocalPos.y,
                                                  _closedLocalPos.z);
            yield return null;
        }
    }

    // ===================== GIZMOS =====================

    void OnDrawGizmosSelected()
    {
        // Vẽ outline vị trí đóng (trắng) và mở (xanh lá)
        Matrix4x4 parentMat = transform.parent != null
            ? transform.parent.localToWorldMatrix
            : Matrix4x4.identity;

        Vector3 closedPos = transform.localPosition;
        Vector3 scale     = transform.localScale;

        // Đọc mesh extent giống hệt trong Awake
        MeshFilter mf = GetComponent<MeshFilter>();
        float meshExtentX = (mf != null && mf.sharedMesh != null)
            ? mf.sharedMesh.bounds.extents.x
            : 0.5f;
        float halfW = meshExtentX * scale.x;
        float anchorX = anchorSide == DoorAnchor.Right
            ? closedPos.x + halfW
            : closedPos.x - halfW;

        float openSX    = scale.x * openScaleX;
        float halfOpen  = openSX * 0.5f;
        float openPosX  = anchorSide == DoorAnchor.Right
            ? anchorX - halfOpen
            : anchorX + halfOpen;

        Gizmos.matrix = parentMat;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(closedPos, scale);

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(
            new Vector3(openPosX, closedPos.y, closedPos.z),
            new Vector3(openSX,   scale.y,      scale.z));

        // Mũi tên chỉ hướng thu
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            parentMat.MultiplyPoint3x4(closedPos),
            parentMat.MultiplyPoint3x4(new Vector3(openPosX, closedPos.y, closedPos.z)));
    }
}
