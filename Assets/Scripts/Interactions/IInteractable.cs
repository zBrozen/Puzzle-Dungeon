using PuzzleDungeon.Player;

namespace PuzzleDungeon.Interactions
{
    public interface IInteractable
    {
        /// <summary>
        /// Called when the player interacts with the object.
        /// </summary>
        void Interact(PlayerController player);

        /// <summary>
        /// Returns the text to display on the UI prompt.
        /// </summary>
        string GetInteractionPrompt(PlayerController player);

        /// <summary>
        /// Checks if the player can interact with this object right now.
        /// </summary>
        bool CanInteract(PlayerController player);
    }
}
