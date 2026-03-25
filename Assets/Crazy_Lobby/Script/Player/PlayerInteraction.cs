using Fusion;
using UnityEngine;

namespace Crazy_Lobby.Player.Components
{
    public class PlayerInteraction
    {
        private readonly NetworkObject _networkObject;
        private readonly Transform _transform;
        private readonly LayerMask _platformLayer;
        private FragilePlatform _currentPlatform;

        public PlayerInteraction(NetworkObject networkObject, Transform transform, LayerMask platformLayer)
        {
            _networkObject = networkObject;
            _transform = transform;
            _platformLayer = platformLayer;
        }

        public void CheckPlatformBeneath()
        {
            if (!_networkObject.HasStateAuthority) return;

            Ray ray = new Ray(_transform.position + Vector3.up * 0.1f, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, _platformLayer))
            {
                if (hit.collider.TryGetComponent(out FragilePlatform platform) && platform != _currentPlatform)
                {
                    _currentPlatform = platform;
                    MapManager.Instance?.RPC_TriggerPlatformBreak(platform.platformID);
                }
            }
            else
            {
                _currentPlatform = null; // In the air
            }
        }
    }
}