using UnityEngine;

namespace PuzzleDungeon.Interactions
{
    /// <summary>
    /// Interface for objects that can be hit by the player's weapon (e.g. sword).
    /// </summary>
    public interface IHittable
    {
        /// <summary>
        /// Called when the object is hit.
        /// </summary>
        void OnHit();
    }
}
