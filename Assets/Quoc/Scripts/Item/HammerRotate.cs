using UnityEngine;
using Fusion;

public class HammerRotate : NetworkBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float minAngle = -90f;
    [SerializeField] private float maxAngle = 90f;

    [SerializeField] private Vector3 axis = Vector3.forward;

    private float t = 0f;
    private Quaternion startRotation;

    public override void Spawned()
    {
        startRotation = transform.localRotation;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        t += Runner.DeltaTime * speed;
        float pingPong = Mathf.PingPong(t, 1f);

        float angle = Mathf.Lerp(minAngle, maxAngle, pingPong);

        transform.localRotation = startRotation * Quaternion.AngleAxis(angle, axis);
    }
}