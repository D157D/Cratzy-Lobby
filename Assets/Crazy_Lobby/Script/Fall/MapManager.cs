using UnityEngine;
using Fusion;

public class MapManager : NetworkBehaviour
{
    public static MapManager Instance;
    
    // Kéo thả toàn bộ các khối sàn vào đây trên Inspector
    public FragilePlatform[] allPlatforms; 

    void Awake()
    {
        Instance = this;
        // Đánh ID cho từng sàn để dễ gọi tên
        for (int i = 0; i < allPlatforms.Length; i++)
        {
            allPlatforms[i].platformID = i;
        }
    }

    // Gửi lệnh qua mạng: Yêu cầu tất cả các máy (All) làm vỡ sàn có ID này
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_TriggerPlatformBreak(int platformID)
    {
        // Khi nhận được lệnh, tìm đúng cái sàn đó và bắt đầu đếm ngược ở MÁY LOCAL
        if (platformID >= 0 && platformID < allPlatforms.Length)
        {
            allPlatforms[platformID].StartBreakingLocally();
        }
    }
}