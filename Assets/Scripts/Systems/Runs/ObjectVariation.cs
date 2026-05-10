using UnityEngine;

namespace PuzzleDungeon.Systems.Runs
{
    [System.Serializable]
    public struct ObjectVariation
    {
        public string ObjectID;
        
        [Header("Toggles")]
        public bool ChangePosition;
        public bool ChangeRotation;
        public bool ChangeActiveState;

        [Header("Values")]
        public Vector3 Position;
        public Vector3 Rotation;
        public bool IsActive;
    }
}
