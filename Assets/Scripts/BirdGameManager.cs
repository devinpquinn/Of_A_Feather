using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BirdGameManager : MonoBehaviour
{
    public static BirdGameManager Instance { get; private set; }

    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D grabbedCursor;

    public GameObject birdPrefab;
    public TextMeshProUGUI levelText;
    
    private float minSpawnX = -7.8f;
    private float maxSpawnX = 7.8f;
    private float minSpawnY = -4.6f;
    private float maxSpawnY = 3f;
    
    private float minBirdDistance = 1.5f; // Minimum distance between birds at spawn
    private int maxSpawnAttempts = 50; // Maximum attempts to find a valid spawn position
    
    public int numPairsToSpawn = 5; //the number of pairs of birds to spawn; each pair consists of two birds that do not share any of the same color in the same body part
    
    private int birdCounter = 1;
    private int currentPairCount = 0;
    
    public bool IsRoundComplete { get; private set; } = false;
    public GameObject victoryScreen;
    
    // Track all paired birds for celebration
    private List<System.Tuple<BirdDragHandler, BirdDragHandler>> pairedBirds = new List<System.Tuple<BirdDragHandler, BirdDragHandler>>();
    
    private List<Vector3> spawnedPositions = new List<Vector3>();
    
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        spawnedPositions.Clear();
        SetDefaultCursor();
        UpdateLevelText();
        SpawnBirdPairs(numPairsToSpawn);
    }
    
    private void SpawnBirdPairs(int pairCount)
    {
        for (int i = 0; i < pairCount; i++)
        {
            int crestIndex, headIndex, wingIndex, bellyIndex;
            
            // Randomly select colors for the first bird
            crestIndex = Random.Range(0, 4);
            headIndex = Random.Range(0, 4);
            wingIndex = Random.Range(0, 4);
            bellyIndex = Random.Range(0, 4);
            
            // Spawn the first bird
            SpawnBirdAtRandomPosition(crestIndex, headIndex, wingIndex, bellyIndex);
            
            int crestIndex2, headIndex2, wingIndex2, bellyIndex2;
            
            // Ensure the second bird has different colors for each body part
            do
            {
                crestIndex2 = Random.Range(0, 4);
            } while (crestIndex2 == crestIndex);
            
            do
            {
                headIndex2 = Random.Range(0, 4);
            } while (headIndex2 == headIndex);
            
            do
            {
                wingIndex2 = Random.Range(0, 4);
            } while (wingIndex2 == wingIndex);
            
            do
            {
                bellyIndex2 = Random.Range(0, 4);
            } while (bellyIndex2 == bellyIndex);
            
            // Spawn the second bird
            SpawnBirdAtRandomPosition(crestIndex2, headIndex2, wingIndex2, bellyIndex2);
        }
    }

    private void SpawnBirdAtRandomPosition(int crestIndex, int headIndex, int wingIndex, int bellyIndex)
    {
        Vector3 spawnPosition = FindValidSpawnPosition();

        GameObject bird = Instantiate(birdPrefab, spawnPosition, Quaternion.identity);
        bird.name = $"Bird {birdCounter}";
        birdCounter++;
        
        BirdRandomizer birdRandomizer = bird.GetComponent<BirdRandomizer>();
        if (birdRandomizer != null)
        {
            birdRandomizer.SetColors(crestIndex, headIndex, wingIndex, bellyIndex);
        }
        
        spawnedPositions.Add(spawnPosition);
    }
    
    private Vector3 FindValidSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 candidatePosition = new Vector3(
                Random.Range(minSpawnX, maxSpawnX),
                Random.Range(minSpawnY, maxSpawnY),
                0f
            );
            
            bool isValidPosition = true;
            
            // Check distance from all previously spawned birds
            foreach (Vector3 existingPosition in spawnedPositions)
            {
                float distance = IsometricDistance(candidatePosition, existingPosition);
                if (distance < minBirdDistance)
                {
                    isValidPosition = false;
                    break;
                }
            }
            
            if (isValidPosition)
            {
                return candidatePosition;
            }
        }
        
        // If we couldn't find a valid position after max attempts, return a random position
        // This is a fallback to prevent infinite loops
        Debug.LogWarning("Could not find ideal spawn position, using fallback");
        return new Vector3(
            Random.Range(minSpawnX, maxSpawnX),
            Random.Range(minSpawnY, maxSpawnY),
            0f
        );
    }
    
    public static float IsometricDistance(Vector3 a, Vector3 b)
    {
        //decrease X distance to simulate isometric perspective
        Vector3 delta = a - b;
        return Mathf.Sqrt(((delta.x * 0.75f) * (delta.x * 0.75f)) + (delta.y * delta.y));
    }
    
    public void RegisterPair(BirdDragHandler bird1, BirdDragHandler bird2)
    {
        var pair = new System.Tuple<BirdDragHandler, BirdDragHandler>(bird1, bird2);
        if (!pairedBirds.Contains(pair))
        {
            pairedBirds.Add(pair);
        }
    }
    
    public void UnregisterPair(BirdDragHandler bird1, BirdDragHandler bird2)
    {
        // Remove both possible orderings of the pair
        pairedBirds.RemoveAll(p => 
            (p.Item1 == bird1 && p.Item2 == bird2) || 
            (p.Item1 == bird2 && p.Item2 == bird1));
    }
    
    public void OnPairFormed()
    {
        currentPairCount++;
        //Debug.Log($"Pair formed! Current pairs: {currentPairCount}/{numPairsToSpawn}");
        
        if (currentPairCount >= numPairsToSpawn)
        {
            OnRoundComplete();
        }
    }
    
    public void OnPairBroken()
    {
        currentPairCount--;
        //Debug.Log($"Pair broken. Current pairs: {currentPairCount}/{numPairsToSpawn}");
    }
    
    private void OnRoundComplete()
    {
        IsRoundComplete = true;
        //Debug.Log("Round Complete! All bird pairs have been matched!");
        StartCoroutine(PlayCelebrationAnimation());
    }
    
    private IEnumerator PlayCelebrationAnimation()
    {
        // Show victory ui
        victoryScreen.SetActive(true);
        
        levelText.GetComponent<Animator>().Play("LevelText_Out", 0, 0f);
        
        yield return new WaitForSeconds(0.5f);
        
        // Play nudge animation for each pair
        foreach (var pair in pairedBirds)
        {
            if (pair.Item1 != null && pair.Item2 != null)
            {
                pair.Item1.PlayCelebrationNudge();
                
                yield return new WaitForSeconds(0.05f);

                pair.Item2.PlayCelebrationNudge();
                
                // Wait before next pair
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    // Public methods for victory screen buttons
    public void PlayAgainEasier()
    {
        numPairsToSpawn = Mathf.Max(1, numPairsToSpawn - 1);
        StartCoroutine(ResetLevel());
    }
    
    public void PlayAgainSameDifficulty()
    {
        StartCoroutine(ResetLevel());
    }
    
    public void PlayAgainHarder()
    {
        numPairsToSpawn++;
        StartCoroutine(ResetLevel());
    }
    
    private IEnumerator ResetLevel()
    {
        yield return new WaitForSeconds(0.075f);
    
        // Hide victory screen
        victoryScreen.GetComponent<Animator>().Play("VictoryScreen_Reset", 0, 0f);
        
        yield return new WaitForSeconds(0.7f);
        
        // Destroy all pair lines first
        LineRenderer[] allLines = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
        foreach (LineRenderer line in allLines)
        {
            Destroy(line.gameObject);
        }
        
        // Destroy all existing birds
        BirdDragHandler[] allBirds = FindObjectsByType<BirdDragHandler>(FindObjectsSortMode.None);
        foreach (BirdDragHandler bird in allBirds)
        {
            Destroy(bird.gameObject);
        }
        
        // Reset all game state
        birdCounter = 1;
        currentPairCount = 0;
        IsRoundComplete = false;
        pairedBirds.Clear();
        spawnedPositions.Clear();
        
        // Update level text and spawn new birds
        UpdateLevelText();
        SpawnBirdPairs(numPairsToSpawn);
        yield return new WaitForSeconds(0.5f);
        
        victoryScreen.SetActive(false);
    }
    
    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = "lvl. " + numPairsToSpawn.ToString();
        }
        
        levelText.GetComponent<Animator>().Play("LevelText_In", 0, 0f);
    }
    
    // Cursor management methods
    public void SetDefaultCursor()
    {
        if (defaultCursor != null)
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        }
    }
    
    public void SetHoverCursor()
    {
        if (hoverCursor != null)
        {
            Cursor.SetCursor(hoverCursor, Vector2.zero, CursorMode.Auto);
        }
    }
    
    public void SetGrabbedCursor()
    {
        if (grabbedCursor != null)
        {
            Cursor.SetCursor(grabbedCursor, Vector2.zero, CursorMode.Auto);
        }
    }
}