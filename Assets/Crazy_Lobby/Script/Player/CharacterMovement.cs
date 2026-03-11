using UnityEngine;

namespace Crazy_Lobby.Player
{
    public class CharacterMovement
    {
        private readonly CharacterController _cc;
        private readonly float _moveSpeed;
        private readonly float _jumpForce;
        private readonly float _gravity;
        private CharacterAnimation _animation;
        private bool _wasGrounded;

        public CharacterMovement(CharacterController cc, CharacterAnimation animation, float moveSpeed, float jumpForce, float gravity)
        {
            _cc = cc;
            _animation = animation;
            _wasGrounded = _cc.isGrounded;
            _moveSpeed = moveSpeed;
            _jumpForce = jumpForce;
            _gravity = gravity;
        }

        public void Move(Vector3 direction, bool isJumping, bool isDoubleJumping, ref Vector3 velocity, float deltaTime)
        {
            if (_cc.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            velocity.x = direction.x * _moveSpeed;
            velocity.z = direction.z * _moveSpeed;

            if (isJumping && _cc.isGrounded)
            {
                velocity.y = _jumpForce;
                _animation.TriggerJump();
            }
            else if (isDoubleJumping && !_cc.isGrounded) 
            {
                // Logic Double Jump: Nhảy lên và lao về phía trước
                velocity.y = _jumpForce;
                
                Vector3 forward = _cc.transform.forward;
                velocity.x = forward.x * _moveSpeed;
                velocity.z = forward.z * _moveSpeed;

                _animation.DoubleJump(direction);
            }

            velocity.y += _gravity * deltaTime;

            _cc.Move(velocity * deltaTime);
            
            // Cập nhật trạng thái nhảy liên tục dựa trên vật lý
            _animation.UpdateJumpState(_cc.isGrounded, velocity.y, deltaTime);

            _wasGrounded = _cc.isGrounded;
        }

        public void Rotate(Transform transform, Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * 15f);
            }
        }
    }
}