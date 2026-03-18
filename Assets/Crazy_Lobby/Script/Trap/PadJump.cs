using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Trap
{
    public class PadJump : NetworkBehaviour
    {
        [SerializeField] private float knockbackForce = 15f;

        void OnTriggerEnter(Collider other)
        {
            if (Object == null || !Runner.IsServer)
                return;

            if (other.gameObject.TryGetComponent<NetworkCharacterController>(out var ncc))
            {
                Vector3 knockbackDirection = other.transform.position - transform.position;
                knockbackDirection.y = 0;

                if (knockbackDirection.sqrMagnitude < 0.001f)
                {
                    knockbackDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                }

                Vector3 newVelocity = ncc.Velocity + (knockbackDirection.normalized * knockbackForce);
                newVelocity.y = 5f;
                ncc.Velocity = newVelocity;
            }
        }
    }
}