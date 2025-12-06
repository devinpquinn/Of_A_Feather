using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BirdDragHandler : MonoBehaviour
{
    public Transform rotationParent;

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float maxRotationAngle = 15f;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float velocitySmoothing = 0.1f;
    [SerializeField] private float rotationReleaseDelay = 0.1f;

    private float pairingDistance = 1.25f;

    private float topBufferDistance = 2.0f;
    private float bottomBufferDistance = 0.33f;
    private float sideBufferDistance = 1.0f;

    private static bool isDraggingAny = false;

    private BirdDragHandler currentPartner = null;
    private LineRenderer pairLineRenderer = null;
    private LineRenderer pairLineOutline = null;
    
    private static BirdDragHandler currentlyHighlightedBird = null;

    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 offset;
    private SortingGroup sortingGroup;
    private string originalSortingLayer;
    private Animator animator;
    public Animator pedestalAnimator;
    private Animator cameraAnimator;
    private bool animateCamera = true;

    // Velocity tracking for rotation
    private Vector3 previousMousePosition;
    private Vector3 mouseVelocity;
    private float targetRotation;
    private float timeSinceRelease;
    
    // Dynamic pivot tracking
    private Transform rotationChild;
    private Transform originalParentOfRotationParent;
    private Vector3 originalRotationParentLocalPosition;
    private Quaternion originalRotationParentLocalRotation;

    private void Start()
    {
        mainCamera = Camera.main;
        cameraAnimator = mainCamera.GetComponent<Animator>();
        
        sortingGroup = GetComponentInChildren<SortingGroup>();
        animator = GetComponent<Animator>();
        
        // Cache the child of rotationParent for re-parenting
        if (rotationParent != null && rotationParent.childCount > 0)
        {
            rotationChild = rotationParent.GetChild(0);
            originalParentOfRotationParent = rotationParent.parent;
            originalRotationParentLocalPosition = rotationParent.localPosition;
            originalRotationParentLocalRotation = rotationParent.localRotation;
        }

        if (sortingGroup != null)
        {
            originalSortingLayer = sortingGroup.sortingLayerName;
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
        previousMousePosition = GetMouseWorldPosition();
        mouseVelocity = Vector3.zero;

        // Reposition rotation pivot to cursor location
        if (rotationParent != null && rotationChild != null)
        {
            // Store child's world position and rotation
            Vector3 childWorldPosition = rotationChild.position;
            Quaternion childWorldRotation = rotationChild.rotation;
            
            // Unparent the child temporarily
            rotationChild.SetParent(null);
            
            // Move rotationParent to the cursor's world position (with offset)
            rotationParent.position = GetMouseWorldPosition() + offset;
            
            // Re-parent the child back to rotationParent
            rotationChild.SetParent(rotationParent);
            
            // Restore the child's world position and rotation
            rotationChild.position = childWorldPosition;
            rotationChild.rotation = childWorldRotation;
        }

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

        if (BirdGameManager.Instance != null)
        {
            BirdGameManager.Instance.SetGrabbedCursor();
        }
    }

    private void Update()
    {
        if (isDragging)
        {
            UpdatePairingPreview();
            if (enableRotation)
            {
                UpdateDragRotation();
            }
            timeSinceRelease = 0f;
        }
        else
        {
            // Wait for delay before snapping to zero
            timeSinceRelease += Time.deltaTime;
            if (timeSinceRelease >= rotationReleaseDelay && enableRotation)
            {
                targetRotation = 0f;
            }
        }

        // Apply smooth rotation interpolation
        if (rotationParent != null && enableRotation)
        {
            float currentZRotation = rotationParent.localEulerAngles.z;
            if (currentZRotation > 180f) currentZRotation -= 360f;
            
            float newRotation = Mathf.Lerp(currentZRotation, targetRotation, Time.deltaTime * rotationSpeed);
            rotationParent.localEulerAngles = new Vector3(
                rotationParent.localEulerAngles.x,
                rotationParent.localEulerAngles.y,
                newRotation
            );
        }
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 currentMousePosition = GetMouseWorldPosition();
            
            // Calculate velocity with smoothing
            Vector3 rawVelocity = (currentMousePosition - previousMousePosition) / Time.deltaTime;
            mouseVelocity = Vector3.Lerp(mouseVelocity, rawVelocity, velocitySmoothing);
            
            previousMousePosition = currentMousePosition;
            
            Vector3 targetPosition = currentMousePosition + offset;
            transform.position = ClampToScreenBounds(targetPosition);
        }
    }

    private void UpdateDragRotation()
    {
        // Calculate rotation based on horizontal velocity
        float horizontalVelocity = mouseVelocity.x;
        
        // Map velocity to rotation angle (clamp to max angle, negated for opposite direction)
        float velocityFactor = Mathf.Clamp(horizontalVelocity * -0.5f, -maxRotationAngle, maxRotationAngle);
        targetRotation = velocityFactor;
    }

    private void OnMouseUp()
    {
        // Disable mouse up if round is complete
        if (BirdGameManager.Instance != null && BirdGameManager.Instance.IsRoundComplete)
            return;
    
        isDragging = false;
        isDraggingAny = false;
        
        // Restore original rotation parent hierarchy
        if (rotationParent != null && rotationChild != null)
        {
            // Store child's world position and rotation
            Vector3 childWorldPosition = rotationChild.position;
            Quaternion childWorldRotation = rotationChild.rotation;
            
            // Unparent the child temporarily
            rotationChild.SetParent(null);
            
            // Restore rotationParent to its original parent and local transform
            rotationParent.SetParent(originalParentOfRotationParent);
            rotationParent.localPosition = originalRotationParentLocalPosition;
            rotationParent.localRotation = originalRotationParentLocalRotation;
            
            // Re-parent the child back to rotationParent
            rotationChild.SetParent(rotationParent);
            
            // Restore the child's world position and rotation
            rotationChild.position = childWorldPosition;
            rotationChild.rotation = childWorldRotation;
        }
        
        // Clear any highlighted bird
        ClearHighlightedBird();

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

        if (BirdGameManager.Instance != null)
        {
            BirdGameManager.Instance.SetHoverCursor();
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

        if (!isDraggingAny && BirdGameManager.Instance != null)
        {
            BirdGameManager.Instance.SetHoverCursor();
        }
    }

    private void OnMouseExit()
    {
        if (animator != null)
        {
            animator.SetBool("Hover", false);
        }

        if (!isDraggingAny && BirdGameManager.Instance != null)
        {
            BirdGameManager.Instance.SetDefaultCursor();
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
    
    private void UpdatePairingPreview()
    {
        BirdDragHandler targetBird = FindPotentialPairingTarget();
        
        if (targetBird != currentlyHighlightedBird)
        {
            // Clear previous highlight
            ClearHighlightedBird();
            
            // Set new highlight
            if (targetBird != null)
            {
                HighlightBird(targetBird);
            }
        }
    }
    
    private BirdDragHandler FindPotentialPairingTarget()
    {
        BirdDragHandler[] allBirds = FindObjectsByType<BirdDragHandler>(FindObjectsSortMode.None);
        BirdDragHandler closestValidBird = null;
        BirdDragHandler closestAnyBird = null;
        float closestValidDistance = float.MaxValue;
        float closestAnyDistance = float.MaxValue;

        foreach (BirdDragHandler otherBird in allBirds)
        {
            if (otherBird == this) continue;

            float distance = BirdGameManager.IsometricDistance(transform.position, otherBird.transform.position);

            // Check if within pairing distance
            if (distance <= pairingDistance)
            {
                // Track the closest bird regardless of validity
                if (distance < closestAnyDistance)
                {
                    closestAnyDistance = distance;
                    closestAnyBird = otherBird;
                }
                
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
                    if (distance < closestValidDistance)
                    {
                        closestValidDistance = distance;
                        closestValidBird = otherBird;
                    }
                }
            }
        }
        
        // Prefer valid bird if one exists and is the closest overall
        if (closestValidBird != null && IsClosestBird(closestValidBird, allBirds))
        {
            return closestValidBird;
        }
        
        // Otherwise, return the closest bird within range (even if invalid)
        if (closestAnyBird != null && IsClosestBird(closestAnyBird, allBirds))
        {
            return closestAnyBird;
        }
        
        return null;
    }
    
    private void HighlightBird(BirdDragHandler bird)
    {
        bird.animator?.SetTrigger("Wiggle");
        bird.pedestalAnimator?.SetBool("Highlighted", true);
        currentlyHighlightedBird = bird;
    }
    
    private void ClearHighlightedBird()
    {
        if (currentlyHighlightedBird != null)
        {
            currentlyHighlightedBird.pedestalAnimator?.SetBool("Highlighted", false);
            currentlyHighlightedBird = null;
        }
    }

    private void CheckForPairing()
    {
        BirdDragHandler[] allBirds = FindObjectsByType<BirdDragHandler>(FindObjectsSortMode.None);
        BirdDragHandler closestValidBird = null;
        float closestDistance = float.MaxValue;
        BirdDragHandler closestBirdWithinRange = null;
        float closestBirdDistance = float.MaxValue;

        foreach (BirdDragHandler otherBird in allBirds)
        {
            if (otherBird == this) continue;

            float distance = BirdGameManager.IsometricDistance(transform.position, otherBird.transform.position);

            // Track the closest bird within pairing distance (regardless of color match)
            if (distance <= pairingDistance && distance < closestBirdDistance)
            {
                closestBirdWithinRange = otherBird;
                closestBirdDistance = distance;
            }

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
                EstablishPair(closestValidBird);
            }
        }
        // If no valid bird was found but there's a bird within range, flash matching parts
        else if (closestBirdWithinRange != null)
        {
            FlashMatchingPartsWithBird(closestBirdWithinRange);
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
    
    private void FlashMatchingPartsWithBird(BirdDragHandler otherBird)
    {
        BirdRandomizer myRandomizer = GetComponent<BirdRandomizer>();
        BirdRandomizer otherRandomizer = otherBird.GetComponent<BirdRandomizer>();

        if (myRandomizer == null || otherRandomizer == null)
        {
            return;
        }

        int[] otherColors = otherRandomizer.GetColors();
        
        // Flash matching parts on both birds
        myRandomizer.FlashMatchingParts(otherColors);
        otherRandomizer.FlashMatchingParts(myRandomizer.GetColors());
    }
    
    public void PlayCelebrationNudge()
    {
        if (animator != null)
        {
            animator.Play("Bird_Celebrate", 0, 0f);
        }
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

        otherBird.pedestalAnimator?.SetBool("Paired", true);
        
        // Play camera bump animation
        if(animateCamera && cameraAnimator != null)
        {
            cameraAnimator.SetTrigger("BumpDown");
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

        //Debug.Log($"Paired {gameObject.name} with {otherBird.gameObject.name}");
    }

    private void BreakPair()
    {
        if (currentPartner != null)
        {
            //Debug.Log($"Breaking pair between {gameObject.name} and {currentPartner.gameObject.name}");

            // Disable pedestals for both birds
            pedestalAnimator?.SetBool("Paired", false);
            currentPartner.pedestalAnimator?.SetBool("Paired", false);

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
        pairLineOutline.sortingOrder = -4;
        
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
        pairLineRenderer.sortingOrder = -2;
        
        // Set positions
        pairLineRenderer.SetPosition(0, transform.position);
        pairLineRenderer.SetPosition(1, otherBird.transform.position);
        
        // Enable pedestals for both birds    
        pedestalAnimator?.SetBool("Paired", true);
        otherBird.pedestalAnimator?.SetBool("Paired", true);
    }
}
