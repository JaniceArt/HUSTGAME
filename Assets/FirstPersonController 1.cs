using UnityEngine;
using UnityEngine.InputSystem; // new input system

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    [Header("Mouse Settings")]
    public float lookSensitivity = 2f;

    private Rigidbody rb;
    private Camera playerCamera;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private float xRotation = 0f;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = GetComponentInChildren<Camera>();

        // Input Actions setup
        inputActions = new PlayerInputActions();

        // Subscribe input
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // Khoá cursor vào giữa màn hình — bắt buộc cho FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
            inputActions.Player.Disable();
    }

    public static bool CanMove = true;

    void Update()
    {
        if (!CanMove) return;
        HandleMouseLook();
    }

    void FixedUpdate()
    {
        if (!CanMove)
        {
            // Dừng nhân vật lại khi đang mở UI
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }
        HandleMovement();
    }

    void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 velocity = move * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        // Đọc delta trực tiếp — tránh bug callback của Input System
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX =  mouseDelta.x * lookSensitivity * 0.1f;
        float mouseY =  mouseDelta.y * lookSensitivity * 0.1f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Camera (child) xoay lên/xuống theo local X
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Toàn bộ body player xoay trái/phải theo world Y
        transform.Rotate(Vector3.up * mouseX, Space.World);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
            isGrounded = true;
    }
}
