using UnityEngine;

public class BirdGameManager : MonoBehaviour
{
    public static BirdGameManager Instance { get; private set; }

    public GameObject birdPrefab;
    
    private float minSpawnX = -7.8f;
    private float maxSpawnX = 7.8f;
    private float minSpawnY = -4.6f;
    private float maxSpawnY = 3f;
    
    public int numPairsToSpawn = 5; //the number of pairs of birds to spawn; each pair consists of two birds that do not share any of the same color in the same body part
    
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
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
        Vector3 spawnPosition = new Vector3(
            Random.Range(minSpawnX, maxSpawnX),
            Random.Range(minSpawnY, maxSpawnY),
            0f
        );

        GameObject bird = Instantiate(birdPrefab, spawnPosition, Quaternion.identity);
        BirdRandomizer birdRandomizer = bird.GetComponent<BirdRandomizer>();
        if (birdRandomizer != null)
        {
            birdRandomizer.SetColors(crestIndex, headIndex, wingIndex, bellyIndex);
        }
    }
}