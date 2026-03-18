using UnityEngine;
using Fusion;

public class MapManager : NetworkBehaviour
{
    public static MapManager Instance;
    
    public FragilePlatform[] allPlatforms; 

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < allPlatforms.Length; i++)
        {
            allPlatforms[i].platformID = i;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_TriggerPlatformBreak(int platformID)
    {
        if (platformID >= 0 && platformID < allPlatforms.Length)
        {
            allPlatforms[platformID].StartBreakingLocally();
        }
    }
}