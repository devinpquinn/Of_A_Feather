using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private SortingGroup sortingGroup;
    private string originalSortingLayer;
    
    private void Start()
    {
        mainCamera = Camera.main;
        sortingGroup = GetComponent<SortingGroup>();
        
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
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }
}
