using Fusion;
using UnityEngine;

public class PlayerMinimap : NetworkBehaviour
{
    [Header("Minimap Camera")]
    [SerializeField] private GameObject minimapCameraPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0, 20f, 0);

    private Transform minimapCam;

    public override void Spawned()
    {
        // Only create minimap camera for the local player
        if (!Object.HasInputAuthority) return;

        GameObject cam = Instantiate(minimapCameraPrefab);
        minimapCam = cam.transform;

        // Optional: parent to player
        minimapCam.SetParent(null); // keep independent for smoother control
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || minimapCam == null) return;

        UpdateMinimapCamera();
    }

    private void UpdateMinimapCamera()
    {
        Vector3 targetPos = transform.position + offset;

        minimapCam.position = targetPos;

        // Keep looking straight down
        minimapCam.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}