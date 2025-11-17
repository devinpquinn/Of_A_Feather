using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D grabbedCursor;
    
    [Header("Pairing Settings")]
    [SerializeField] private float pairingDistance = 2.0f;
    
    private float topBufferDistance = 2.0f;
    private float bottomBufferDistance = 0.33f;
    private float sideBufferDistance = 1.0f;
    
    private static bool isDraggingAny = false;
    
    private BirdDragHandler currentPartner = null;
    
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
        
        // Break existing pair
        BreakPair();
        
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
        
        // Check for nearby birds to pair with
        CheckForPairing();
        
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
    
    private void CheckForPairing()
    {
        BirdDragHandler[] allBirds = FindObjectsByType<BirdDragHandler>(FindObjectsSortMode.None);
        BirdDragHandler closestValidBird = null;
        float closestDistance = float.MaxValue;
        
        foreach (BirdDragHandler otherBird in allBirds)
        {
            if (otherBird == this) continue;
            
            float distance = Vector3.Distance(transform.position, otherBird.transform.position);
            
            // Check if within pairing distance
            if (distance <= pairingDistance)
            {
                // If the other bird has a partner, this bird must be closer than the current partner
                if (otherBird.currentPartner != null)
                {
                    float partnerDistance = Vector3.Distance(otherBird.transform.position, otherBird.currentPartner.transform.position);
                    if (distance >= partnerDistance)
                    {
                        continue; // Not close enough to steal the partner
                    }
                }
                
                // Check if colors are mismatched
                if (AreColorsMismatched(otherBird))
                {
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestValidBird = otherBird;
                    }
                }
            }
        }
        
        // If a valid bird was found, establish the pair
        if (closestValidBird != null)
        {
            EstablishPair(closestValidBird);
        }
    }
    
    private bool AreColorsMismatched(BirdDragHandler otherBird)
    {
        BirdRandomizer myRandomizer = GetComponent<BirdRandomizer>();
        BirdRandomizer otherRandomizer = otherBird.GetComponent<BirdRandomizer>();
        
        if (myRandomizer == null || otherRandomizer == null)
        {
            return false;
        }
        
        int[] myColors = myRandomizer.GetColors();
        int[] otherColors = otherRandomizer.GetColors();
        
        return myRandomizer.CheckMismatched(otherColors);
    }
    
    private void EstablishPair(BirdDragHandler otherBird)
    {
        // Break the other bird's existing pair if it has one
        if (otherBird.currentPartner != null)
        {
            otherBird.BreakPair();
        }
        
        // Establish the new pair
        currentPartner = otherBird;
        otherBird.currentPartner = this;
        
        // Set rotations based on which bird is leftmost
        if (transform.position.x < otherBird.transform.position.x)
        {
            // This bird is on the left
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            otherBird.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            // Other bird is on the left
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            otherBird.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        
        Debug.Log($"Paired {gameObject.name} with {otherBird.gameObject.name}");
    }
    
    private void BreakPair()
    {
        if (currentPartner != null)
        {
            Debug.Log($"Breaking pair between {gameObject.name} and {currentPartner.gameObject.name}");
            
            // Break the connection from both sides
            BirdDragHandler formerPartner = currentPartner;
            currentPartner.currentPartner = null;
            currentPartner = null;
        }
    }
}
