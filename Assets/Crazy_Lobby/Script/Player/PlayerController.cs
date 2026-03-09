using UnityEngine;
using Fusion;

namespace Cray_Lobby.Player
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 Movement;
        public NetworkButtons Buttons;
    }

    public enum InputButtons
    {
        Jump = 0,
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _jumpForce = 8f;
        [SerializeField] private float _gravity = -20f;

        private CharacterController _cc;

        [Networked] private Vector3 NetworkVelocity { get; set; }
        private Vector3 _localVelocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                Vector3 direction = new Vector3(data.Movement.x, 0, data.Movement.y).normalized;
                Vector3 currentVel = NetworkVelocity;

                if (_cc.isGrounded && currentVel.y < 0)
                {
                    currentVel.y = -2f;
                }

                currentVel.x = direction.x * _moveSpeed;
                currentVel.z = direction.z * _moveSpeed;

                if (data.Buttons.IsSet(InputButtons.Jump) && _cc.isGrounded)
                {
                    currentVel.y = _jumpForce;
                }

                currentVel.y += _gravity * Runner.DeltaTime;

                _cc.Move(currentVel * Runner.DeltaTime);
                NetworkVelocity = currentVel;
            }
        }

        private void Update()
        {
            if (Object != null && Object.IsValid) return;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            bool jump = Input.GetButton("Jump");

            Vector3 direction = new Vector3(h, 0, v).normalized;

            if (_cc.isGrounded && _localVelocity.y < 0)
                _localVelocity.y = -2f;

            _localVelocity.x = direction.x * _moveSpeed;
            _localVelocity.z = direction.z * _moveSpeed;

            if (jump && _cc.isGrounded)
                _localVelocity.y = _jumpForce;

            _localVelocity.y += _gravity * Time.deltaTime;

            _cc.Move(_localVelocity * Time.deltaTime);
        }
    }
}