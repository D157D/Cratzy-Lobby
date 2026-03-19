using UnityEngine;

namespace Crazy_Lobby.Player
{
    public class CharacterAnimation
    {
        private readonly Animator _animator;
        public CharacterAnimation(Animator animator)
        {
            _animator = animator;
        }
        public void UpdateMoveAnimation(Vector3 velocity, float maxSpeed = 1f)
        {
            if (_animator == null) return;

            // Lấy giá trị tốc độ thực tế trên mặt phẳng ngang
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            
            float safeMaxSpeed = maxSpeed > 0.1f ? maxSpeed : 1f;
            float speedRatio = horizontalVelocity.magnitude / safeMaxSpeed;

            _animator.SetFloat("running", Mathf.Clamp01(speedRatio));
        }

        public void UpdateJumpState(bool isGrounded, float verticalVelocity, float deltaTime)
        {
            if (_animator == null) return;

            float targetValue = 0f; 
            if (!isGrounded)
            {
                targetValue = verticalVelocity > 0 ? 1f : 0.5f;
            }

            _animator.SetFloat("Jump_BlendTree", targetValue, 0.15f, deltaTime);
        }

        public void TriggerJump()
        {
            if (_animator == null) return;
            _animator.SetTrigger("Jump");
        }
        
        public void DoubleJump(Vector3 moveDirection)
        {
            if(_animator == null) return;
            _animator.SetTrigger("doubleJump");
        }
    }
}