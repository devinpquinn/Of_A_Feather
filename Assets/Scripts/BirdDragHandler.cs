using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D grabbedCursor;
    
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
            transform.position = GetMouseWorldPosition() + offset;
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
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
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
}
