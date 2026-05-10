using UnityEngine;

/// <summary>
/// Script à placer sur un Trigger pour "appeler" une plateforme ToggleMover.
/// </summary>
public class ToggleMoverCaller : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("La plateforme à appeler.")]
    public ToggleMover mover;

    [Tooltip("Si coché, appelle la plateforme vers le Point B. Sinon, vers le Point A.")]
    public bool targetState = false;

    [SerializeField, Tooltip("Tag du joueur pour déclencher l'appel.")]
    private string _playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (this == null || mover == null) return;

        if (other.CompareTag(_playerTag))
        {
            // On force la plateforme à se déplacer vers la cible, quel que soit son mode actuel
            mover.ForceMoveToPoint(targetState);
            Debug.Log($"[ToggleMoverCaller] Plateforme {mover.gameObject.name} appelée vers {(targetState ? "Point B" : "Point A")}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // On vérifie si l'objet ou le mover existent encore (important pour éviter les MissingReferenceException dans l'éditeur)
        if (this == null || mover == null) return;

        try {
            // Visualisation dans l'éditeur pour aider le Level Design
            Gizmos.color = targetState ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, mover.transform.position);
        } catch {
            // Sécurité supplémentaire si l'un des objets est en cours de destruction
        }
    }
}
