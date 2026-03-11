using UnityEngine;
using Fusion;

namespace Crazy_Lobby.Player
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 Movement;
        public Vector3 LookDirection;
        public NetworkButtons Buttons;
    }

    public enum InputButtons
    {
        Jump = 0,
        DoubleJump = 1,
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _jumpForce = 8f;
        [SerializeField] private float _gravity = -20f;
        private Animator _animator;
        private CharacterController _cc;

        [Networked] private Vector3 NetworkVelocity { get; set; }
        private Vector3 _localVelocity;
        private CharacterMovement _movement;
        private CharacterAnimation _animation;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            _animation = new CharacterAnimation(_animator);
            _movement = new CharacterMovement(_cc, _animation, _moveSpeed, _jumpForce, _gravity);
        }
        

        [System.Obsolete]
        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                FindObjectOfType<Camera>()?.SetLocalPlayer(transform);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                Vector3 direction = new Vector3(data.Movement.x, 0, data.Movement.y).normalized;
                Vector3 currentVel = NetworkVelocity;

                // Use reusable movement logic
                _movement.Move(direction, data.Buttons.IsSet(InputButtons.Jump), data.Buttons.IsSet(InputButtons.DoubleJump), ref currentVel, Runner.DeltaTime);
                
                NetworkVelocity = currentVel;
            }
        }

        private void Update()
        {
            if (Object != null && Object.IsValid) return;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            bool jump = Input.GetButton("Jump");
            bool doublejump = Input.GetButton("DoubleJump");

            Vector3 direction;
            // Tính hướng di chuyển dựa theo Camera
            if (UnityEngine.Camera.main != null)
            {
                Transform camTransform = UnityEngine.Camera.main.transform;
                Vector3 forward = Vector3.Scale(camTransform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 right = Vector3.Scale(camTransform.right, new Vector3(1, 0, 1)).normalized;
                direction = (forward * v + right * h).normalized;
            }
            else
            {
                direction = new Vector3(h, 0, v).normalized;
            }

            // Use reusable movement logic
            _movement.Move(direction, jump, doublejump ,ref _localVelocity, Time.deltaTime);
            
            _movement.Rotate(transform, direction, Time.deltaTime);
            _animation.UpdateMoveAnimation(direction);
        }
    }
}