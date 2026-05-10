using UnityEngine;

namespace PuzzleDungeon.Systems.Runs
{
    [CreateAssetMenu(fileName = "NewDecorConfig", menuName = "PuzzleDungeon/Decor Configuration")]
    public class DecorConfiguration : BaseConfiguration
    {
        // Les configurations de décor héritent de tout ce qui est dans BaseConfiguration.
        // Elles sont destinées à être des sous-configurations réutilisables.
    }
}
