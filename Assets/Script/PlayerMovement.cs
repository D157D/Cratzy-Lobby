using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private CharacterController character;
    [SerializeField] private Animator animator;

    private Vector2 moveInput;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -20f;

    private float yVelocity;
    private bool jumpPressed;
    private int jumpCount = 0;
    private int maxJump = 2;

    // --- BIẾN NETWORKED CHO ITEM ---
    [Networked] public TickTimer shieldTimer { get; set; }
    [Networked] public TickTimer slowTimer { get; set; } // Timer làm chậm cho Magnet
    
    // Biến phụ trợ để xử lý lực đẩy tức thời (Knockback)
    private Vector3 impactVelocity = Vector3.zero;

    public bool IsShielded => !shieldTimer.ExpiredOrNotRunning(Runner);
    public bool IsSlowed => !slowTimer.ExpiredOrNotRunning(Runner);

    void Awake()
    {
        if (character == null) character = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (character == null) return;

        // 1. TÍNH TOÁN TỐC ĐỘ (Xử lý làm chậm từ Magnet)
        float currentSpeed = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ? runSpeed : walkSpeed;
        
        if (IsSlowed)
        {
            currentSpeed *= 0.4f; // Giảm 60% tốc độ khi bị dính Magnet
        }

        // 2. DI CHUYỂN
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        // 3. XỬ LÝ TRẠNG THÁI CHẠM ĐẤT
        if (character.isGrounded)
        {
            jumpCount = 0;
            if (yVelocity < 0) yVelocity = -2f;
        }

        // 4. NHẢY & DOUBLE JUMP
        if (jumpPressed && jumpCount < maxJump)
        {
            float force = (jumpCount == 0) ? jumpForce : jumpForce * 0.8f;
            yVelocity = Mathf.Sqrt(force * -2f * gravity);

            if (animator != null) animator.SetTrigger("Jump");

            jumpCount++;
            jumpPressed = false;
        }

        // 5. TRỌNG LỰC
        yVelocity += gravity * Runner.DeltaTime;

        // 6. XỬ LÝ LỰC TÁC ĐỘNG (KNOCKBACK)
        // Làm mượt lực đẩy theo thời gian
        impactVelocity = Vector3.Lerp(impactVelocity, Vector3.zero, Runner.DeltaTime * 5f);

        // 7. TỔNG HỢP VẬN TỐC
        Vector3 finalVelocity = (move * currentSpeed) + impactVelocity;
        finalVelocity.y = yVelocity;

        character.Move(finalVelocity * Runner.DeltaTime);

        // 8. XOAY NHÂN VẬT
        if (move != Vector3.zero)
        {
            transform.forward = move;
        }

        // 9. ANIMATION
        if (animator != null)
        {
            float animSpeed = move.magnitude * (IsSlowed ? 0.5f : 1f); // Giảm tốc độ anim nếu bị làm chậm
            animator.SetFloat("Speed", move.magnitude * currentSpeed);
            animator.SetBool("IsGrounded", character.isGrounded);
        }
    }

    // --- HÀM XỬ LÝ TÁC ĐỘNG TỪ ITEM ---
    public void ApplyKnockback(Vector3 force)
    {
        // Nếu đang bật Shield thì miễn nhiễm hoàn toàn
        if (IsShielded)
        {
            Debug.Log("<color=blue>[Shield]</color> Đã chặn được tác động!");
            return;
        }

        // Gán lực đẩy tức thời
        impactVelocity = force;
        
        // Nếu có lực hướng lên thì gán cho yVelocity để bay lên
        if (force.y > 0)
        {
            yVelocity = force.y;
        }
    }

    // Input System Callback
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}