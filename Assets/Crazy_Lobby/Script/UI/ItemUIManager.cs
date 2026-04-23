using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Crazy_Lobby.UI
{
    public class ItemUIManager : MonoBehaviour
    {
        public static ItemUIManager Instance;

        [Header("Giao diện UI")]
        public GameObject itemNotificationPanel; // Bảng chứa thông báo
        public TextMeshProUGUI itemNotificationText; // Text hiển thị

        // Lưu trữ số lượng từng loại Item người chơi đã nhặt
        private Dictionary<string, int> inventory = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowItemPickup(string itemName, int amount)
        {
            // Bật GameObject và Panel lên để đảm bảo luôn hiển thị
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            itemNotificationPanel.SetActive(true);
            
            // Cộng dồn số lượng item vừa nhặt vào kho (inventory)
            if (inventory.ContainsKey(itemName))
            {
                inventory[itemName] += amount;
            }
            else
            {
                inventory.Add(itemName, amount);
            }
            
            // Cập nhật text hiển thị tổng số lượng hiện có
            itemNotificationText.text = $"{itemName}: {inventory[itemName]}";
        }
    }
}