using UnityEngine;
using System.Collections;

public class FragilePlatform : MonoBehaviour
{
    public int platformID; // Sẽ được MapManager gán tự động
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

    // Hàm này được gọi bởi MapManager thông qua RPC
    public void StartBreakingLocally()
    {
        if (isBreaking) return; // Nếu đang vỡ rồi thì thôi
        isBreaking = true;
        StartCoroutine(BreakRoutine());
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra nếu đối tượng đạp lên là người chơi (cần gán tag "Player" cho nhân vật)
        if (collision.gameObject.CompareTag("Player"))
        {
            // Tùy thuộc vào logic mạng (Multiplayer), bạn có thể gọi trực tiếp hàm này 
            // hoặc gửi tín hiệu cho MapManager để MapManager gọi RPC đồng bộ cho mọi người.
            StartBreakingLocally();
        }
    }

    IEnumerator BreakRoutine()
    {
        // Đổi màu vàng
        ChangeColor(Color.yellow);
        yield return new WaitForSeconds(timeToBreak / 2f);

        // Đổi màu đỏ
        ChangeColor(Color.red);
        yield return new WaitForSeconds(timeToBreak / 2f);

        // Tắt khối sàn (chỉ tắt render và collider)
        gameObject.SetActive(false);
    }

    private void ChangeColor(Color newColor)
    {
        rend.GetPropertyBlock(propBlock);
        propBlock.SetColor(ColorProperty, newColor);
        rend.SetPropertyBlock(propBlock);
    }
}