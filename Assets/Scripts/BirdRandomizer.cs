using UnityEngine;

public class BirdRandomizer : MonoBehaviour
{
    [SerializeField] private Color[] colorPalette = new Color[4];
    
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
        if (colorPalette.Length != 4)
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
            crestRenderer.color = colorPalette[CrestColorIndex];
        
        if (headRenderer != null)
            headRenderer.color = colorPalette[HeadColorIndex];
        
        if (wingRenderer != null)
            wingRenderer.color = colorPalette[WingColorIndex];
        
        if (bellyRenderer != null)
            bellyRenderer.color = colorPalette[BellyColorIndex];
    }
}
