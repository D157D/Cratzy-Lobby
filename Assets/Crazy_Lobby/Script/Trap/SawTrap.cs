using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Trap
{
    public class SawTrap : NetworkBehaviour
    {
        [SerializeField] private float _rotationSpeed = 500f;
        [SerializeField] private Vector3 _rotationAxis = Vector3.forward;
        [SerializeField] private float _knockbackForce = 10f;

        public override void FixedUpdateNetwork()
        {
            UpdateRotation();
        }
        public override void Render()
        {
            UpdateRotation();
        }
        private void UpdateRotation()
        {
            if (_rotationAxis.sqrMagnitude > 0)
            {
                transform.Rotate(_rotationAxis.normalized, _rotationSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Runner.IsServer)
                return;

            if (other.TryGetComponent<NetworkCharacterController>(out var ncc))
            {
                Vector3 knockbackDirection = other.transform.position - transform.position;
                knockbackDirection.y = 0;

                if (knockbackDirection.sqrMagnitude < 0.001f)
                {
                    knockbackDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                }

                Vector3 newVelocity = ncc.Velocity + (knockbackDirection.normalized * _knockbackForce);
                newVelocity.y = 5f;
                ncc.Velocity = newVelocity;
            }
        }
    }
}