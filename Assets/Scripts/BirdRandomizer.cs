using UnityEngine;

public class BirdRandomizer : MonoBehaviour
{
    [Header("Color Palettes")]
    [SerializeField] private Color[] crestPalette = new Color[4];
    [SerializeField] private Color[] bodyPalette = new Color[4];
    
    [Header("Body Part References")]
    [SerializeField] private SpriteRenderer crestRenderer;
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private SpriteRenderer wingRenderer;
    [SerializeField] private SpriteRenderer bellyRenderer;
    
    // Store assigned color indices
    public int CrestColorIndex { get; private set; }
    public int HeadColorIndex { get; private set; }
    public int WingColorIndex { get; private set; }
    public int BellyColorIndex { get; private set; }
    
    private void Start()
    {
        RandomizeColors();
    }
    
    private void RandomizeColors()
    {
        if (crestPalette.Length != 4 || bodyPalette.Length != 4)
        {
            Debug.LogError("All color palettes must contain exactly 4 colors!");
            return;
        }
        
        // Randomly assign colors to each body part from their respective palettes
        CrestColorIndex = Random.Range(0, 4);
        HeadColorIndex = Random.Range(0, 4);
        WingColorIndex = Random.Range(0, 4);
        BellyColorIndex = Random.Range(0, 4);
        
        // Apply colors to sprite renderers
        if (crestRenderer != null)
            crestRenderer.color = crestPalette[CrestColorIndex];
        
        if (headRenderer != null)
            headRenderer.color = bodyPalette[HeadColorIndex];
        
        if (wingRenderer != null)
            wingRenderer.color = bodyPalette[WingColorIndex];
        
        if (bellyRenderer != null)
            bellyRenderer.color = bodyPalette[BellyColorIndex];
    }
}
