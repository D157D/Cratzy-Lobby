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
        public void UpdateMoveAnimation(Vector3 moveDirection)
        {
            if (_animator == null) return;

            float runningValue = moveDirection.sqrMagnitude > 0.001f ? 1f : 0f;
            _animator.SetFloat("running", runningValue);
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