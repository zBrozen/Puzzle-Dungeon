using UnityEngine;
using System.Collections.Generic;

namespace PuzzleDungeon.Systems.Runs
{
    [CreateAssetMenu(fileName = "NewRunConfig", menuName = "PuzzleDungeon/Run Configuration")]
    public class RunConfiguration : BaseConfiguration
    {
        [Header("Modular Decor")]
        public List<DecorConfiguration> DecorConfigs = new List<DecorConfiguration>();
    }
}
