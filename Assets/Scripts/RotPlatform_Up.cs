using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotPlatform_Up : MonoBehaviour
{
    public float rotatingSpeed = 130f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotatingSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            collision.transform.SetParent(transform);
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            collision.transform.SetParent(null);
        }
    }
}