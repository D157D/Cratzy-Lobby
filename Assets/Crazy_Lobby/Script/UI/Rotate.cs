using System.Collections;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    void Awake()
    {
        StartCoroutine(RotateUI());
    }

    private IEnumerator RotateUI()
    {
        while (true)
        {
            transform.Rotate(0, 0, 1);
            yield return null;
        }
    }
}

