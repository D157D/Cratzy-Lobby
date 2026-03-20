// using UnityEngine;
// using UnityEngine.InputSystem;
// using Fusion;

// public class PlayerMovement : NetworkBehaviour
// {
//     [SerializeField] CharacterController character;
//     [SerializeField] Animator animator;

//     private Vector2 moveInput;

//     private float walkSpeed = 5f;
//     private float runSpeed = 8f;

//     private float jumpForce = 7f;
//     private float gravity = -20f;

//     private float yVelocity;

//     // 🔥 INPUT
//     private bool jumpPressed;

//     // 🔥 DOUBLE JUMP
//     private int jumpCount = 0;
//     private int maxJump = 2;

//     void Awake()
//     {
//         if (character == null)
//             character = GetComponent<CharacterController>();

//         if (animator == null)
//             animator = GetComponentInChildren<Animator>();
//     }

//     void Update()
//     {
//         if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
//         {
//             jumpPressed = true;
//         }
//     }

//     public override void FixedUpdateNetwork()
//     {
//         if (!HasInputAuthority) return;
//         if (character == null) return;

//         Camera camObj = Camera.main;
//         if (camObj == null) return;

//         Transform cam = camObj.transform;

//         // 🎮 MOVE THEO CAMERA
//         Vector3 forward = cam.forward;
//         Vector3 right = cam.right;

//         forward.y = 0;
//         right.y = 0;

//         forward.Normalize();
//         right.Normalize();

//         Vector3 move = forward * moveInput.y + right * moveInput.x;

//         // 🏃 SPEED
//         float currentSpeed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed
//             ? runSpeed
//             : walkSpeed;

//         // 🪂 RESET KHI CHẠM ĐẤT
//         if (character.isGrounded)
//         {
//             jumpCount = 0;

//             if (yVelocity < 0)
//                 yVelocity = -2f;
//         }

//         // 🪂 DOUBLE JUMP
//         if (jumpPressed && jumpCount < maxJump)
//         {
//             float force = (jumpCount == 0) ? jumpForce : jumpForce * 0.8f; // nhảy lần 2 thấp hơn

//             yVelocity = Mathf.Sqrt(force * -2f * gravity);

//             if (animator != null)
//                 animator.SetTrigger("Jump");

//             jumpCount++;
//             jumpPressed = false;
//         }

//         // GRAVITY
//         yVelocity += gravity * Runner.DeltaTime;

//         // MOVE
//         Vector3 velocity = move * currentSpeed;
//         velocity.y = yVelocity;

//         character.Move(velocity * Runner.DeltaTime);

//         // XOAY NHÂN VẬT
//         if (move != Vector3.zero)
//         {
//             transform.forward = move;
//         }

//         // 🎬 ANIMATION
//         if (animator != null)
//         {
//             float speed = move.magnitude * currentSpeed;

//             animator.SetFloat("Speed", speed);
//             animator.SetBool("IsGrounded", character.isGrounded);
//         }
//     }

//     public void OnMove(InputValue value)
//     {
//         moveInput = value.Get<Vector2>();
//     }
// }