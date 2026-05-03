using System.Collections.Generic;
using UnityEngine;
using System;
using PuzzleDungeon.Interactions;

namespace PuzzleDungeon.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Starting Items")]
        [SerializeField] private List<ItemData> _startingItems = new List<ItemData>();

        private List<ItemData> _items = new List<ItemData>();

        // Événement pour prévenir l'UI ou d'autres systèmes du changement
        public event Action OnInventoryChanged;

        private void Awake()
        {
            foreach (var item in _startingItems)
            {
                AddItem(item);
            }
        }

        public void AddItem(ItemData item)
        {
            if (item == null) return;
            
            _items.Add(item);
            Debug.Log($"[Inventory] Added: {item.ItemName}");
            OnInventoryChanged?.Invoke();
        }

        public bool HasItem(string itemName)
        {
            return _items.Exists(i => i.ItemName == itemName);
        }

        public bool HasItem(ItemData item)
        {
            return _items.Contains(item);
        }

        public int GetKeyCount()
        {
            return _items.FindAll(i => i.Type == ItemType.Key).Count;
        }

        public bool RemoveItem(string itemName)
        {
            ItemData item = _items.Find(i => i.ItemName == itemName);
            if (item != null)
            {
                _items.Remove(item);
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        public List<ItemData> GetAllItems()
        {
            return new List<ItemData>(_items);
        }
    }
}
