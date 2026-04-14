using Fusion;
using UnityEngine;

public struct MyInputData : INetworkInput
{
    public Vector2 move;
    public NetworkBool jump;

    // 🔥 QUAN TRỌNG: dùng bool (KHÔNG dùng NetworkBool)
    public bool useItemPressed;

    public int selectedSlot;
}