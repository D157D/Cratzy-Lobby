using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    public Image[] slots; // 3 slot

    public Sprite shieldIcon;
    public Sprite magnetIcon;
    public Sprite boomIcon;

    public void UpdateUI(ItemType[] items)
    {
        // 👉 chống lỗi null
        if (slots == null || items == null) return;

        int count = Mathf.Min(slots.Length, items.Length);

        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null) continue;

            if (items[i] == ItemType.None)
            {
                slots[i].sprite = null;
                slots[i].enabled = false;
            }
            else
            {
                slots[i].enabled = true;
                slots[i].sprite = GetSprite(items[i]);
            }
        }
    }

    Sprite GetSprite(ItemType item)
    {
        switch (item)
        {
            case ItemType.Shield: return shieldIcon;
            case ItemType.Boom: return boomIcon;
            default: return null;
        }
    }
}