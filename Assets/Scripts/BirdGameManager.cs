using UnityEngine;

public class BirdGameManager : MonoBehaviour
{
    public static BirdGameManager Instance { get; private set; }

    public GameObject birdPrefab;
    
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
    
    private System.Collections.Generic.List<Vector3> spawnedPositions = new System.Collections.Generic.List<Vector3>();
    
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        spawnedPositions.Clear();
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
    
    public void OnPairFormed()
    {
        currentPairCount++;
        Debug.Log($"Pair formed! Current pairs: {currentPairCount}/{numPairsToSpawn}");
        
        if (currentPairCount >= numPairsToSpawn)
        {
            OnRoundComplete();
        }
    }
    
    public void OnPairBroken()
    {
        currentPairCount--;
        Debug.Log($"Pair broken. Current pairs: {currentPairCount}/{numPairsToSpawn}");
    }
    
    private void OnRoundComplete()
    {
        IsRoundComplete = true;
        Debug.Log("🎉 Round Complete! All bird pairs have been matched!");
    }
}