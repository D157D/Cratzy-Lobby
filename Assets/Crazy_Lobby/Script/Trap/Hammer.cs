using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Trap
{
    public class Hammmer : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _speed = 2f;
        [SerializeField] private float _minAngle = -90f;
        [SerializeField] private float _maxAngle = 90f;
        [SerializeField] private Vector3 _rotationAxis = Vector3.right;

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
            float t = (Mathf.Sin((float)Runner.SimulationTime * _speed) + 1f) * 0.5f;
            float angle = Mathf.Lerp(_minAngle, _maxAngle, t);
            transform.localRotation = Quaternion.AngleAxis(angle, _rotationAxis.normalized);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Runner.IsServer)
                return;

            if (other.TryGetComponent<NetworkCharacterController>(out var ncc))
            {
                Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;

                knockbackDirection.y = 0;

                ncc.Velocity += knockbackDirection * 15f;
            }
        }       
    }
}