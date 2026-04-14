using UnityEngine;
using Fusion;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerItem : NetworkBehaviour
{
    [Networked, Capacity(3), OnChangedRender(nameof(OnItemsChanged))]
    public NetworkArray<ItemType> Items => default;

    [Header("UI")]
    [SerializeField] private Image[] slots;
    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Sprite boomIcon;

    [Header("Boom")]
    [SerializeField] private NetworkPrefabRef boomPrefab;
    [SerializeField] private Transform throwPoint;

    private int currentIndex = 0;

    // ================= INIT =================
    public override void Spawned()
    {
        if (HasInputAuthority)
            UpdateUI();
    }

    // ================= INPUT NETWORK =================
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out MyInputData input)) return;

        if (input.useItemPressed)
        {
            if (Object.HasStateAuthority)
                ExecuteItem(input.selectedSlot);
            else
                RPC_UseItem(input.selectedSlot);
        }
    }

    // ================= RPC =================
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_UseItem(int index)
    {
        ExecuteItem(index);
    }

    // ================= SERVER LOGIC =================
    void ExecuteItem(int index)
    {
        if (!Object.HasStateAuthority) return;
        if (index < 0 || index >= 3) return;

        ItemType item = Items[index];
        if (item == ItemType.None) return;

        Debug.Log($"[Server] Use {item}");

        switch (item)
        {
            case ItemType.Shield:
                UseShield();
                break;

            case ItemType.Boom:
                ThrowBoom();
                break;
        }

        // Xóa item sau khi dùng
        Items.Set(index, ItemType.None);
    }

    // ================= SHIELD =================
    void UseShield()
    {
        var shield = GetComponent<PlayerShield>();
        if (shield != null)
        {
            shield.hasShield = true;
            shield.ActivateShield();
        }
    }

    // ================= BOOM =================
    void ThrowBoom()
    {
        Vector3 pos = throwPoint != null
            ? throwPoint.position
            : transform.position + transform.forward + Vector3.up;

        Runner.Spawn(boomPrefab, pos, Quaternion.identity, Object.InputAuthority);
    }

    // ================= INPUT LOCAL =================
    void Update()
    {
        if (!HasInputAuthority) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) currentIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) currentIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) currentIndex = 2;

        UpdateUI();
    }

    // ================= INVENTORY =================
    public void AddItem(ItemType item)
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < 3; i++)
        {
            if (Items[i] == ItemType.None)
            {
                Items.Set(i, item);
                return;
            }
        }
    }

    public bool IsInventoryFull()
    {
        for (int i = 0; i < 3; i++)
        {
            if (Items[i] == ItemType.None)
                return false;
        }
        return true;
    }

    // ================= UI =================
    void OnItemsChanged()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (!HasInputAuthority || slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            ItemType item = Items[i];

            slots[i].enabled = item != ItemType.None;
            slots[i].sprite = GetSprite(item);
            slots[i].color = (i == currentIndex) ? Color.yellow : Color.white;
        }
    }

    Sprite GetSprite(ItemType item)
    {
        switch (item)
        {
            case ItemType.Shield: return shieldIcon;
            case ItemType.Boom: return boomIcon;
        }
        return null;
    }

    // ================= GET SLOT =================
    public int GetLocalSelectedSlot()
    {
        return currentIndex;
    }
}