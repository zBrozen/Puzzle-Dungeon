using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    public enum ItemType { Sword, Key, PuzzlePiece, Beetle, Other }

    [CreateAssetMenu(fileName = "NewItem", menuName = "PuzzleDungeon/Item")]
    public class ItemData : ScriptableObject
    {
        public string ItemName;
        public ItemType Type;
        public GameObject Prefab; // The visual representation of the item to spin above head
        public Sprite Icon;
    }
}
