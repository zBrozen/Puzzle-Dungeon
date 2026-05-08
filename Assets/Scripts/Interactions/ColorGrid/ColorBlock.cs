using UnityEngine;
using System.Collections;

public class ColorBlock : MonoBehaviour
{
    public int colorID; // 0: Red, 1: Blue, 2: Green, 3: White (Button/Target)
    public MeshRenderer meshRenderer;
    public Material[] colorMaterials;
    
    private bool isFallen = false;
    private bool isCurrent = false;
    private Vector3 originalPosition;
    
    public System.Action<ColorBlock> OnPlayerEnter;

    void Awake()
    {
        originalPosition = transform.position;
    }

    public void SetColor(int id)
    {
        colorID = id;
        if (meshRenderer != null && colorMaterials != null && id < colorMaterials.Length)
        {
            meshRenderer.material = colorMaterials[id];
        }
    }

    public void Fall()
    {
        if (isFallen || isCurrent) return;
        isFallen = true;
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(originalPosition + Vector3.down * 10f));
    }

    public void Rise()
    {
        if (!isFallen) return;
        isFallen = false;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(originalPosition));
    }

    public void SetAsCurrent(bool current)
    {
        isCurrent = current;
        if (isCurrent) Rise();
    }

    private IEnumerator MoveRoutine(Vector3 targetPos)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
        if (targetPos.y < originalPosition.y) gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isFallen)
        {
            Debug.Log($"Block {gameObject.name} (Color {colorID}) triggered by {other.name}!");
            OnPlayerEnter?.Invoke(this);
        }
    }
}
