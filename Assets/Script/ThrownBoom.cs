using UnityEngine;
using Fusion;

public class ThrownBoom : NetworkBehaviour
{
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 25f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject explosionEffect;

    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float explodeDistance = 1.5f;

    private Transform target;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            target = FindNearestPlayer();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (target == null) return;

        // Bay tới target
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Runner.DeltaTime
        );

        // Quay mặt về target
        transform.LookAt(target);

        // Gần thì nổ
        if (Vector3.Distance(transform.position, target.position) < explodeDistance)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Object || !Object.HasStateAuthority) return;
        Explode();
    }

    void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PlayerMovement>(out var targetPlayer))
            {
                // 🛡️ CHECK SHIELD
                var shield = hit.GetComponent<PlayerShield>();
                if (shield != null && shield.isActive)
                    continue;

                Vector3 dir = (hit.transform.position - transform.position).normalized;
                targetPlayer.ApplyKnockback(dir * explosionForce + Vector3.up * 8f);
            }
        }

        // Hiệu ứng nổ
        if (explosionEffect != null)
            Runner.Spawn(explosionEffect, transform.position, Quaternion.identity);

        Runner.Despawn(Object);
    }

    Transform FindNearestPlayer()
    {
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();

        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (var p in players)
        {
            // Bỏ qua chính mình
            if (p.transform == transform.root) continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                nearest = p.transform;
            }
        }

        return nearest;
    }
}