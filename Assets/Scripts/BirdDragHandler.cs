using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D grabbedCursor;
    
    private float topBufferDistance = 2.0f;
    private float bottomBufferDistance = 0.33f;
    private float sideBufferDistance = 1.0f;
    
    private static bool isDraggingAny = false;
    
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private SortingGroup sortingGroup;
    private string originalSortingLayer;
    private Animator animator;
    
    private void Start()
    {
        mainCamera = Camera.main;
        sortingGroup = GetComponent<SortingGroup>();
        animator = GetComponent<Animator>();
        
        if (sortingGroup != null)
        {
            originalSortingLayer = sortingGroup.sortingLayerName;
        }
        
        if (defaultCursor != null)
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        }
    }
    
    private void OnMouseDown()
    {
        isDragging = true;
        isDraggingAny = true;
        offset = transform.position - GetMouseWorldPosition();
        
        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName = "Hovering";
        }
        
        if (animator != null)
        {
            animator.SetBool("Grabbed", true);
        }
        
        if (grabbedCursor != null)
        {
            Cursor.SetCursor(grabbedCursor, Vector2.zero, CursorMode.Auto);
        }
    }
    
    private void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 targetPosition = GetMouseWorldPosition() + offset;
            transform.position = ClampToScreenBounds(targetPosition);
        }
    }
    
    private void OnMouseUp()
    {
        isDragging = false;
        isDraggingAny = false;
        
        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName = originalSortingLayer;
        }
        
        if (animator != null)
        {
            animator.SetBool("Grabbed", false);
            animator.SetBool("Hover", false);
        }
        
        if (defaultCursor != null)
        {
            Cursor.SetCursor(hoverCursor, Vector2.zero, CursorMode.Auto);
        }
    }
    
    private void OnMouseEnter()
    {
        if (animator != null && !isDraggingAny)
        {
            animator.SetBool("Hover", true);
        }
        
        if (hoverCursor != null && !isDraggingAny)
        {
            Cursor.SetCursor(hoverCursor, Vector2.zero, CursorMode.Auto);
        }
    }
    
    private void OnMouseExit()
    {
        if (animator != null)
        {
            animator.SetBool("Hover", false);
        }
        
        if (defaultCursor != null && !isDragging)
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
    
    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        // Get the camera's viewport boundaries in world space
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, position.z));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, position.z));
        
        // Clamp the position within the buffer zone with separate distances
        float clampedX = Mathf.Clamp(position.x, bottomLeft.x + sideBufferDistance, topRight.x - sideBufferDistance);
        float clampedY = Mathf.Clamp(position.y, bottomLeft.y + bottomBufferDistance, topRight.y - topBufferDistance);
        
        return new Vector3(clampedX, clampedY, position.z);
    }
}
