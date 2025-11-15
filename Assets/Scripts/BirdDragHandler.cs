using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
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
    }
    
    private void OnMouseDown()
    {
        isDragging = true;
        offset = transform.position - GetMouseWorldPosition();
        
        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName = "Hovering";
        }
        
        if (animator != null)
        {
            animator.SetBool("Grabbed", true);
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
        
        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName = originalSortingLayer;
        }
        
        if (animator != null)
        {
            animator.SetBool("Grabbed", false);
            animator.SetBool("Hover", false);
        }
    }
    
    private void OnMouseEnter()
    {
        if (animator != null)
        {
            animator.SetBool("Hover", true);
        }
    }
    
    private void OnMouseExit()
    {
        if (animator != null)
        {
            animator.SetBool("Hover", false);
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}
