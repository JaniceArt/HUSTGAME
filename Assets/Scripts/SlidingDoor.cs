using System.Collections;
using UnityEngine;

/// <summary>
/// Cửa xếp accordion — scale X về một cạnh cố định.
/// Anchor Side = Right → cạnh local X lớn nhất đứng yên.
/// Anchor Side = Left  → cạnh local X nhỏ nhất đứng yên.
/// </summary>
public class SlidingDoor : MonoBehaviour
{
    [Header("Cài đặt cửa xếp")]
    public DoorAnchor anchorSide   = DoorAnchor.Right;
    public float      openDuration = 0.7f;

    [Range(0f, 0.6f)]
    public float openScaleX = 0.30f;

    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Âm thanh (tuỳ chọn)")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)] public float volume = 0.8f;
    
    [Tooltip("Tốc độ phát âm thanh (Pitch). Tăng để âm thanh nhanh và thanh hơn, giảm để chậm và trầm hơn.")]
    [Range(0.1f, 3f)] public float soundSpeed = 1f;

    public enum DoorAnchor { Left, Right }

    // ── runtime ─────────────────────────────────────
    private Vector3 _closedScale;
    private float   _edgeSigned;   // Tọa độ X local của cạnh được giữ cố định
    private Vector3 _anchorWorld;  // Vị trí world của cạnh đó (CỐ ĐỊNH)

    private bool        _isOpen   = false;
    private bool        _isMoving = false;
    private AudioSource _sfx;

    // ────────────────────────────────────────────────

    void Awake()
    {
        _closedScale = transform.localScale;

        // Tính rightMost và leftMost trong LOCAL space của cửa
        float rightMost = float.MinValue;
        float leftMost  = float.MaxValue;

        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            Bounds wb = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = wb.center + new Vector3(
                    wb.extents.x * ((i & 1) != 0 ? 1f : -1f),
                    wb.extents.y * ((i & 2) != 0 ? 1f : -1f),
                    wb.extents.z * ((i & 4) != 0 ? 1f : -1f));

                // InverseTransformPoint → tọa độ trong door LOCAL space (trước scale)
                float lx = transform.InverseTransformPoint(corner).x;
                if (lx > rightMost) rightMost = lx;
                if (lx < leftMost)  leftMost  = lx;
            }
        }

        if (rends.Length == 0) { rightMost = 0.5f; leftMost = -0.5f; }

        // Đã đảo ngược để 'Right' thực sự giữ cố định mép phải (do trục X của model bị ngược)
        _edgeSigned = anchorSide == DoorAnchor.Right ? leftMost : rightMost;

        // Lưu lại vị trí cố định của cạnh này trong World Space để neo cửa
        _anchorWorld = transform.TransformPoint(new Vector3(_edgeSigned, 0, 0));

        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake  = false;
        _sfx.spatialBlend = 1f;
    }

    // ── Public API ──────────────────────────────────

    public bool IsOpen   => _isOpen;
    public bool IsMoving => _isMoving;

    public void Interact()
    {
        if (_isMoving) return;
        _isOpen = !_isOpen;
        StartCoroutine(_isOpen ? CoOpen() : CoClose());

        if (SequenceManager.Instance != null)
        {
            if (_isOpen && !SequenceManager.Instance.IsFinished)
            {
                // Mở cửa -> Hoàn thành nhiệm vụ "Mở cửa tiệm"
                if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.CompleteObjective();
                
                // Báo cho hệ thống nhảy sang bước Khách Hàng (HustBoy)
                SequenceManager.Instance.NextStep();
            }
            else if (!_isOpen && SequenceManager.Instance.IsFinished)
            {
                // Đóng cửa khi đã hết khách -> Hoàn thành nhiệm vụ "Đóng cửa tiệm" và Load Ngày Mới
                if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.CompleteObjective();
                
                DayTransitionController dtc = Object.FindObjectOfType<DayTransitionController>();
                if (dtc != null)
                {
                    dtc.gameObject.SetActive(true); // Đảm bảo nó được bật nếu bị tắt
                }
            }
        }
    }

    // ── Coroutines ──────────────────────────────────

    IEnumerator CoOpen()
    {
        _isMoving = true;
        PlaySound(openSound);
        yield return Animate(transform.localScale.x, _closedScale.x * openScaleX);
        _isMoving = false;
    }

    IEnumerator CoClose()
    {
        _isMoving = true;
        PlaySound(closeSound);
        yield return Animate(transform.localScale.x, _closedScale.x);
        _isMoving = false;
    }

    IEnumerator Animate(float fromSX, float toSX)
    {
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float curSX = Mathf.Lerp(fromSX, toSX,
                              curve.Evaluate(Mathf.Clamp01(t / openDuration)));

            // 1. Áp dụng scale mới
            transform.localScale = new Vector3(curSX, _closedScale.y, _closedScale.z);

            // 2. Tính xem vị trí cạnh neo đang bị lệch đi đâu
            Vector3 currentEdgeWorld = transform.TransformPoint(new Vector3(_edgeSigned, 0, 0));

            // 3. Dịch chuyển nguyên cái cửa bù lại phần bị lệch để cạnh neo đứng yên 1 chỗ
            transform.position += (_anchorWorld - currentEdgeWorld);

            yield return null;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null) 
        {
            _sfx.pitch = soundSpeed;
            _sfx.PlayOneShot(clip, volume);
        }
    }
}
