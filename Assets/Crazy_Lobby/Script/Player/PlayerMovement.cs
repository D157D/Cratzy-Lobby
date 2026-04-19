using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Player.Components
{
    public class PlayerMovement
    {
        private readonly NetworkCharacterController _ncc;
        private readonly CharacterAnimation _characterAnimation;
        private readonly Transform _transform;
        private readonly NetworkRunner _runner;

        private readonly float _jumpForce;
        private readonly float _rotationSpeed = 10f;

        public PlayerMovement(NetworkCharacterController ncc, CharacterAnimation characterAnimation, Transform transform, NetworkRunner runner, float jumpForce, float maxSpeed, float acceleration, float braking)
        {
            _ncc = ncc;
            _characterAnimation = characterAnimation;
            _transform = transform;
            _runner = runner;
            _jumpForce = jumpForce;

            _ncc.maxSpeed = maxSpeed;
            _ncc.acceleration = acceleration;
            _ncc.braking = braking;
        }

        public void ProcessInput(NetworkInputData data)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, data.CameraYaw, 0);
            Vector3 moveDirection = cameraRotation * new Vector3(data.Movement.x, 0, data.Movement.y).normalized;

            _ncc.Move(moveDirection);

            if (data.Jump && _ncc.Grounded)
            {
                _ncc.Jump(true, _jumpForce);
                _characterAnimation.TriggerJump();
            }

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                _transform.rotation = Quaternion.Slerp(_transform.rotation, Quaternion.LookRotation(moveDirection), _runner.DeltaTime * _rotationSpeed);
            }
        }

        public void UpdateAnimations()
        {
            _characterAnimation.UpdateMoveAnimation(_ncc.Velocity, _ncc.maxSpeed);
            _characterAnimation.UpdateJumpState(_ncc.Grounded, _ncc.Velocity.y, _runner.DeltaTime);
        }

        // Thêm hàm này vào class PlayerMovement để thay đổi tốc độ chạy
        public void SetMaxSpeed(float newSpeed)
        {
            if (_ncc != null)
            {
                _ncc.maxSpeed = newSpeed; 
            }
        }
    }
}