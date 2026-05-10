using UnityEngine;
using System.Collections.Generic;

namespace PuzzleDungeon.Systems.Runs
{
    public abstract class BaseConfiguration : ScriptableObject
    {
        public string ConfigurationID;
        public string Description;
        
        public List<ObjectVariation> Variations = new List<ObjectVariation>();
    }
}
