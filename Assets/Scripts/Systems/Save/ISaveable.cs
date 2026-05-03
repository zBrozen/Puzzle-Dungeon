using UnityEngine;

namespace PuzzleDungeon.Systems.Save
{
    public interface ISaveable
    {
        string UniqueID { get; }
        void PopulateSaveData(GameData data);
        void LoadFromSaveData(GameData data);
    }
}
