using UnityEngine;
using System.Collections;

public class FragilePlatform : MonoBehaviour
{
    public int platformID; 
    public float timeToBreak = 1.5f;
    private bool isBreaking = false;
    
    private Renderer rend;
    private MaterialPropertyBlock propBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    public void StartBreakingLocally()
    {
        if (isBreaking) return; 
        isBreaking = true;
        StartCoroutine(BreakRoutine());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StartBreakingLocally();
        }
    }

    IEnumerator BreakRoutine()
    {
        ChangeColor(Color.yellow);
        yield return new WaitForSeconds(timeToBreak / 2f);

        ChangeColor(Color.red);
        yield return new WaitForSeconds(timeToBreak / 2f);

        gameObject.SetActive(false);
    }

    private void ChangeColor(Color newColor)
    {
        rend.GetPropertyBlock(propBlock);
        propBlock.SetColor(ColorProperty, newColor);
        rend.SetPropertyBlock(propBlock);
    }
}