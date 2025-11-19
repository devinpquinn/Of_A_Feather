using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D grabbedCursor;

    private float pairingDistance = 1.25f;

    private float topBufferDistance = 2.0f;
    private float bottomBufferDistance = 0.33f;
    private float sideBufferDistance = 1.0f;

    private static bool isDraggingAny = false;

    private BirdDragHandler currentPartner = null;
    private LineRenderer pairLineRenderer = null;
    private LineRenderer pairLineOutline = null;

    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private SortingGroup sortingGroup;
    private string originalSortingLayer;
    private Animator animator;

    private void Start()
    {
        mainCamera = Camera.main;
        sortingGroup = GetComponentInChildren<SortingGroup>();
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
        // Disable input if round is complete
        if (BirdGameManager.Instance != null && BirdGameManager.Instance.IsRoundComplete)
            return;
            
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
        // Disable hover if round is complete
        if (BirdGameManager.Instance != null && BirdGameManager.Instance.IsRoundComplete)
            return;
            
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

            float distance = BirdGameManager.IsometricDistance(transform.position, otherBird.transform.position);

            // Check if within pairing distance
            if (distance <= pairingDistance)
            {
                // If the other bird has a partner, this bird must be closer than the current partner
                if (otherBird.currentPartner != null)
                {
                    float partnerDistance = BirdGameManager.IsometricDistance(otherBird.transform.position, otherBird.currentPartner.transform.position);
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

        // If a valid bird was found, verify that this bird is closer to it than to any other bird
        if (closestValidBird != null)
        {
            // Check if the closestValidBird is actually the closest bird overall to this bird
            if (IsClosestBird(closestValidBird, allBirds))
            {
                StartCoroutine(EstablishPairDelayed(closestValidBird));
            }
        }
    }

    private bool IsClosestBird(BirdDragHandler targetBird, BirdDragHandler[] allBirds)
    {
        float distanceToTarget = BirdGameManager.IsometricDistance(transform.position, targetBird.transform.position);

        foreach (BirdDragHandler otherBird in allBirds)
        {
            if (otherBird == this || otherBird == targetBird) continue;

            float otherDistance = BirdGameManager.IsometricDistance(transform.position, otherBird.transform.position);

            // If any other bird is closer, the target isn't the closest
            if (otherDistance < distanceToTarget)
            {
                return false;
            }
        }

        return true;
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
    
    public void PlayCelebrationNudge()
    {
        if (animator != null)
        {
            animator.Play("Bird_Celebrate", 0, 0f);
        }
    }

    private IEnumerator EstablishPairDelayed(BirdDragHandler otherBird)
    {
        yield return new WaitForSeconds(0.0833f);
        EstablishPair(otherBird);
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

        // Create line renderer for this bird
        CreatePairLine(otherBird);

        // Play nudge animation on the partner
        if (otherBird.animator != null)
        {
            otherBird.animator.Play("Bird_Nudge", 0, 0f);
        }

        // Register pair with game manager
        if (BirdGameManager.Instance != null)
        {
            BirdGameManager.Instance.RegisterPair(this, otherBird);
        }

        // Notify the game manager
        if (BirdGameManager.Instance != null)
        {
            BirdGameManager.Instance.OnPairFormed();
        }

        Debug.Log($"Paired {gameObject.name} with {otherBird.gameObject.name}");
    }

    private void BreakPair()
    {
        if (currentPartner != null)
        {
            Debug.Log($"Breaking pair between {gameObject.name} and {currentPartner.gameObject.name}");

            // Disable pedestals for both birds
            BirdRandomizer myRandomizer = GetComponent<BirdRandomizer>();
            BirdRandomizer partnerRandomizer = currentPartner.GetComponent<BirdRandomizer>();
            
            if (myRandomizer != null && myRandomizer.pedestal != null)
            {
                myRandomizer.pedestal.GetComponent<Animator>()?.Play("Pedestal_Out", 0, 0f);
            }
            
            if (partnerRandomizer != null && partnerRandomizer.pedestal != null)
            {
                partnerRandomizer.pedestal.GetComponent<Animator>()?.Play("Pedestal_Out", 0, 0f);
            }

            // Destroy line renderer if it exists
            if (pairLineRenderer != null)
            {
                Destroy(pairLineRenderer.gameObject);
                pairLineRenderer = null;
            }
            
            // Destroy outline if it exists
            if (pairLineOutline != null)
            {
                Destroy(pairLineOutline.gameObject);
                pairLineOutline = null;
            }
            
            // Also destroy partner's line renderer if it exists
            if (currentPartner.pairLineRenderer != null)
            {
                Destroy(currentPartner.pairLineRenderer.gameObject);
                currentPartner.pairLineRenderer = null;
            }
            
            // Also destroy partner's outline if it exists
            if (currentPartner.pairLineOutline != null)
            {
                Destroy(currentPartner.pairLineOutline.gameObject);
                currentPartner.pairLineOutline = null;
            }

            // Unregister pair from game manager
            if (BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.UnregisterPair(this, currentPartner);
            }

            // Break the connection from both sides
            BirdDragHandler formerPartner = currentPartner;
            currentPartner.currentPartner = null;
            currentPartner = null;

            // Notify the game manager
            if (BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.OnPairBroken();
            }
        }
    }
    
    private void CreatePairLine(BirdDragHandler otherBird)
    {
        // Get the connection color from this bird's randomizer
        BirdRandomizer myRandomizer = GetComponent<BirdRandomizer>();
        Color connectionColor = myRandomizer != null ? myRandomizer.ConnectionColor : Color.white;
        
        // Create black outline (thicker, lower sorting order)
        GameObject outlineObject = new GameObject($"PairLineOutline_{gameObject.name}_{otherBird.gameObject.name}");
        pairLineOutline = outlineObject.AddComponent<LineRenderer>();
        
        pairLineOutline.positionCount = 2;
        pairLineOutline.startWidth = 0.0833f * 3f;
        pairLineOutline.endWidth = 0.0833f * 3f;
        pairLineOutline.material = new Material(Shader.Find("Sprites/Default"));
        pairLineOutline.startColor = Color.black;
        pairLineOutline.endColor = Color.black;
        pairLineOutline.sortingOrder = -3;
        
        pairLineOutline.SetPosition(0, transform.position);
        pairLineOutline.SetPosition(1, otherBird.transform.position);
        
        // Create colored line on top
        GameObject lineObject = new GameObject($"PairLine_{gameObject.name}_{otherBird.gameObject.name}");
        pairLineRenderer = lineObject.AddComponent<LineRenderer>();
        
        // Configure line renderer
        pairLineRenderer.positionCount = 2;
        pairLineRenderer.startWidth = 0.0833f;
        pairLineRenderer.endWidth = 0.0833f;
        pairLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        pairLineRenderer.startColor = connectionColor;
        pairLineRenderer.endColor = connectionColor;
        pairLineRenderer.sortingOrder = -1;
        
        // Set positions
        pairLineRenderer.SetPosition(0, transform.position);
        pairLineRenderer.SetPosition(1, otherBird.transform.position);
        
        // Enable and color pedestals for both birds
        BirdRandomizer otherRandomizer = otherBird.GetComponent<BirdRandomizer>();
        
        if (myRandomizer != null && myRandomizer.pedestal != null)
        {
            myRandomizer.pedestal.GetComponent<Animator>()?.Play("Pedestal_In", 0, 0f);
            SpriteRenderer pedestalRenderer = myRandomizer.pedestal.GetComponent<SpriteRenderer>();
            if (pedestalRenderer != null)
            {
                pedestalRenderer.color = connectionColor;
            }
        }
        
        if (otherRandomizer != null && otherRandomizer.pedestal != null)
        {
            otherRandomizer.pedestal.GetComponent<Animator>()?.Play("Pedestal_In", 0, 0f);
            SpriteRenderer pedestalRenderer = otherRandomizer.pedestal.GetComponent<SpriteRenderer>();
            if (pedestalRenderer != null)
            {
                pedestalRenderer.color = connectionColor;
            }
        }
    }
}
