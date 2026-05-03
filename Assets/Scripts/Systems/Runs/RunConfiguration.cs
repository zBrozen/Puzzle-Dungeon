using UnityEngine;
using System.Collections.Generic;

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

    [CreateAssetMenu(fileName = "NewRunConfig", menuName = "PuzzleDungeon/Run Configuration")]
    public class RunConfiguration : ScriptableObject
    {
        public string ConfigurationID;
        public string Description;
        
        public List<ObjectVariation> Variations = new List<ObjectVariation>();
    }
}
