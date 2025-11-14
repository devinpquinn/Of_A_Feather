using UnityEngine;

public class BirdDragHandler : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        // Debug checks
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
        }
        else
        {
            // Check for Physics2DRaycaster
            var raycaster = mainCamera.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
            if (raycaster == null)
            {
                Debug.LogWarning("Physics2DRaycaster not found on Main Camera! OnMouse events require Physics2DRaycaster for 2D colliders.");
                Debug.LogWarning("Add a Physics2DRaycaster component to your Main Camera.");
            }
            else
            {
                Debug.Log("Physics2DRaycaster found and active: " + raycaster.enabled);
            }
        }
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("No Collider2D found on " + gameObject.name);
        }
        else
        {
            Debug.Log("Collider2D found: " + collider.GetType().Name + " | Enabled: " + collider.enabled + " | Is Trigger: " + collider.isTrigger);
            Debug.Log("GameObject layer: " + LayerMask.LayerToName(gameObject.layer) + " (" + gameObject.layer + ")");
        }
        
        Debug.Log("BirdDragHandler initialized on " + gameObject.name + " at position " + transform.position);
    }
    
    private void Update()
    {
        // Manual raycast check for debugging
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            
            if (hit.collider != null)
            {
                Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
                if (hit.collider.gameObject == gameObject)
                {
                    Debug.Log("Hit THIS bird!");
                }
            }
            else
            {
                Debug.Log("Raycast hit nothing at world position: " + worldPoint);
            }
        }
    }
    
    private void OnMouseDown()
    {
        isDragging = true;
        Debug.Log("Bird drag started.");
        offset = transform.position - GetMouseWorldPosition();
    }
    
    private void OnMouseDrag()
    {
        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + offset;
        }
    }
    
    private void OnMouseUp()
    {
        isDragging = false;
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}
