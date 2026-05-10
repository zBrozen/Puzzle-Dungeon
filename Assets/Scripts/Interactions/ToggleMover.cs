using UnityEngine;
public class ToggleMover : MonoBehaviour
{
    [Header("Points de déplacement")]
    [Tooltip("Point de départ (état désactivé)")]
    public Transform pointA;
    [Tooltip("Point d'arrivée (état activé)")]
    public Transform pointB;
    [Header("Paramètres")]
    [Tooltip("Vitesse de déplacement de l'objet")]
    public float speed = 5f;
    
    [Tooltip("État actuel. Si coché, l'objet se dirige vers le point B.")]
    public bool isToggled = false;

    [Tooltip("Si coché, la plateforme revient automatiquement au Point A quand le joueur la quitte.")]
    public bool returnToAOnExit = false;

    [Tooltip("Si coché, le retour auto ne se déclenche que si le Point B est plus haut que le Point A (ascenseur).")]
    public bool onlyResetOnAscent = true;

    public enum MovementType { Toggle, PauseResume }
    [Header("Behavior")]
    [SerializeField] private MovementType _movementType = MovementType.Toggle;
    [SerializeField, Tooltip("En mode PauseResume, inverse automatiquement la direction une fois arrivé au bout.")]
    private bool _autoReverseAtEnd = true;

    [Header("Visuel (Optionnel)")]
    [Tooltip("Glisse ici le GameObject (le modèle 3D) que tu veux masquer pendant le déplacement.")]
    public GameObject visualObjectToHide;

    private Vector3 _lastPosition;
    private Vector3 _targetPosA;
    private Vector3 _targetPosB;
    private bool _movingToB = true;
    private System.Collections.Generic.List<CharacterController> _passengers = new System.Collections.Generic.List<CharacterController>();

    private void Start()
    {
        _lastPosition = transform.position;
        
        // On mémorise les positions de départ et d'arrivée au lancement du jeu.
        // Cela évite un bug classique : si tu as mis PointA et PointB en ENFANTS 
        // de la plateforme dans Unity, ils bougeraient avec elle à l'infini !
        if (pointA != null) _targetPosA = pointA.position;
        if (pointB != null) _targetPosB = pointB.position;
    }

    private void LateUpdate()
    {
        // Sécurité au cas où les points ne sont pas assignés dans l'inspecteur
        if (pointA == null || pointB == null) 
        {
            Debug.LogWarning("ToggleMover : Point A ou Point B non assigné sur " + gameObject.name);
            return;
        }

        // On détermine la cible en fonction du mode et de l'état
        Vector3 targetPosition;
        bool isMoving = false;

        if (_movementType == MovementType.PauseResume)
        {
            // En mode PauseResume, isToggled sert de "Play/Pause"
            if (!isToggled)
            {
                targetPosition = transform.position;
                isMoving = false;
            }
            else
            {
                targetPosition = _movingToB ? _targetPosB : _targetPosA;
                isMoving = Vector3.Distance(transform.position, targetPosition) > 0.001f;
            }
        }
        else
        {
            // Mode Toggle classique
            targetPosition = isToggled ? _targetPosB : _targetPosA;
            isMoving = Vector3.Distance(transform.position, targetPosition) > 0.001f;
        }

        // On calcule la nouvelle position
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        Vector3 deltaPosition = newPosition - transform.position;
        
        // Si on vient d'arriver au bout en mode PauseResume, on prépare l'inversion
        if (_movementType == MovementType.PauseResume && isToggled && !isMoving)
        {
            if (_autoReverseAtEnd) _movingToB = !_movingToB;
            isToggled = false; // On s'arrête en attendant le prochain signal
        }

        // Gestion du masquage visuel
        if (visualObjectToHide != null && visualObjectToHide != this.gameObject)
        {
            if (isMoving && visualObjectToHide.activeSelf)
                visualObjectToHide.SetActive(false);
            else if (!isMoving && !visualObjectToHide.activeSelf)
                visualObjectToHide.SetActive(true);
        }

        // --- DÉPLACEMENT DES PASSAGERS ET DE LA PLATEFORME ---
        if (isMoving)
        {
            if (deltaPosition.y > 0)
            {
                // On ajoute un tout petit bonus en Y (0.01f) pour s'assurer que le joueur 
                // ne soit jamais "enfoncé" dans le sol de la plateforme à cause de sa propre gravité.
                MovePassengers(deltaPosition + Vector3.up * 0.01f);
                transform.position = newPosition;
            }
            else
            {
                transform.position = newPosition;
                MovePassengers(deltaPosition);
            }
        }
        else
        {
            transform.position = newPosition;
        }
        
        _lastPosition = transform.position;
    }

    private void MovePassengers(Vector3 delta)
    {
        // On déplace manuellement les CharacterControllers
        foreach(var passenger in _passengers)
        {
            if (passenger != null)
            {
                // On peut parfois ajouter un tout petit offset en Y si la gravité du joueur est trop forte
                // (ex: passenger.Move(delta + Vector3.up * 0.001f);) mais l'ordre d'exécution suffit généralement.
                passenger.Move(delta);
            }
        }
    }

    /// <summary>
    /// Inverse l'état actuel du toggle (A vers B, ou B vers A).
    /// Parfait pour un interrupteur classique à activer/désactiver (ex: coup d'épée, scarabée).
    /// </summary>
    public void Toggle()
    {
        isToggled = !isToggled;
    }

    /// <summary>
    /// Force un état spécifique.
    /// Parfait pour un bouton pression : 
    /// - OnEnter() -> SetToggleState(true)
    /// - OnExit() -> SetToggleState(false)
    /// </summary>
    /// <param name="state">True pour aller vers B, False pour aller vers A</param>
    public void SetToggleState(bool state)
    {
        isToggled = state;
    }

    /// <summary>
    /// Force la plateforme à se diriger vers un point spécifique, peu importe le mode (Toggle ou PauseResume).
    /// </summary>
    /// <param name="toPointB">True pour aller vers B, False pour aller vers A</param>
    public void ForceMoveToPoint(bool toPointB)
    {
        if (_movementType == MovementType.PauseResume)
        {
            _movingToB = toPointB;
            isToggled = true; // On active le mouvement
        }
        else
        {
            isToggled = toPointB;
        }
    }

    // --- GESTION DES COLLISIONS ---
    // Méthode pour les objets avec Rigidbody classique
    private void OnCollisionEnter(Collision collision)
    {
        collision.transform.SetParent(transform, true);
    }

    private void OnCollisionExit(Collision collision)
    {
        collision.transform.SetParent(null, true);
    }

    // Méthode pour le CharacterController (qui ignore les modifications de Transform de son parent)
    private void OnTriggerEnter(Collider other)
    {
        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null)
        {
            if (!_passengers.Contains(cc)) _passengers.Add(cc);
        }
        else
        {
            // Si c'est un autre objet, on le parente
            other.transform.SetParent(transform, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null)
        {
            if (_passengers.Contains(cc)) _passengers.Remove(cc);
            
            // Si l'option est activée et qu'il n'y a plus personne sur la plateforme...
            if (returnToAOnExit && _passengers.Count == 0)
            {
                // On vérifie si c'est un trajet montant (B plus haut que A)
                bool isAscendingPlatform = _targetPosB.y > _targetPosA.y;

                // On ne redescend que si :
                // 1. On n'avait pas encore atteint B
                // 2. Et (on s'en fiche si c'est montant OU c'est bien une plateforme montante)
                float distanceToB = Vector3.Distance(transform.position, _targetPosB);
                if (isToggled && distanceToB > 0.1f)
                {
                    if (!onlyResetOnAscent || isAscendingPlatform)
                    {
                        isToggled = false;
                    }
                }
            }
        }
        else
        {
            other.transform.SetParent(null, true);
        }
    }
    // --- OUTIL DE LEVEL DESIGN ---
    // Cette fonction dessine une ligne cyan dans la scène Unity pour visualiser le trajet
    private void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA.position, pointB.position);
            
            Gizmos.color = Color.green; // Point de départ
            Gizmos.DrawWireSphere(pointA.position, 0.2f);
            
            Gizmos.color = Color.red; // Point d'arrivée
            Gizmos.DrawWireSphere(pointB.position, 0.2f);
        }
    }
}