using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D grabbedCursor;
    
    [Header("Momentum Settings")]
    [SerializeField] private float momentumMultiplier = 1.5f;
    [SerializeField] private float maxFlingSpeed = 20f;
    [SerializeField] private int velocitySamples = 3;
    [SerializeField] private float flingDuration = 0.1f;
    
    private static bool isDraggingAny = false;
    
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private SortingGroup sortingGroup;
    private string originalSortingLayer;
    private Animator animator;
    private Rigidbody2D rb;
    
    // Momentum tracking
    private Vector3 lastMousePosition;
    private Vector3[] recentVelocities;
    private int velocityIndex = 0;
    private Coroutine stopCoroutine;
    
    private void Start()
    {
        mainCamera = Camera.main;
        sortingGroup = GetComponent<SortingGroup>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        // Initialize velocity tracking array
        recentVelocities = new Vector3[velocitySamples];
        
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
        
        // Reset velocity tracking
        lastMousePosition = GetMouseWorldPosition();
        for (int i = 0; i < recentVelocities.Length; i++)
        {
            recentVelocities[i] = Vector3.zero;
        }
        velocityIndex = 0;
        
        // Stop any existing velocity and coroutines
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
            stopCoroutine = null;
        }
        
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
            Vector3 currentMousePosition = GetMouseWorldPosition();
            transform.position = currentMousePosition + offset;
            
            // Track velocity for momentum calculation
            Vector3 velocity = (currentMousePosition - lastMousePosition) / Time.deltaTime;
            recentVelocities[velocityIndex] = velocity;
            velocityIndex = (velocityIndex + 1) % recentVelocities.Length;
            lastMousePosition = currentMousePosition;
        }
    }
    
    private void OnMouseUp()
    {
        isDragging = false;
        isDraggingAny = false;
        
        // Calculate average velocity from recent samples
        Vector3 averageVelocity = Vector3.zero;
        for (int i = 0; i < recentVelocities.Length; i++)
        {
            averageVelocity += recentVelocities[i];
        }
        averageVelocity /= recentVelocities.Length;
        
        // Apply momentum to the bird
        if (rb != null)
        {
            Vector2 flingVelocity = averageVelocity * momentumMultiplier;
            
            // Clamp to maximum fling speed
            if (flingVelocity.magnitude > maxFlingSpeed)
            {
                flingVelocity = flingVelocity.normalized * maxFlingSpeed;
            }
            
            rb.linearVelocity = flingVelocity;
            
            // Start coroutine to stop the bird after the fling duration
            if (stopCoroutine != null)
            {
                StopCoroutine(stopCoroutine);
            }
            stopCoroutine = StartCoroutine(StopBirdAfterDuration());
        }
        
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
    
    private System.Collections.IEnumerator StopBirdAfterDuration()
    {
        yield return new WaitForSeconds(flingDuration);
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        stopCoroutine = null;
    }
}
