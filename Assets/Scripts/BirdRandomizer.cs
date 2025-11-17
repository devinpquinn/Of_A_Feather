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
    [HideInInspector] public int CrestColorIndex { get; private set; }
    [HideInInspector] public int HeadColorIndex { get; private set; }
    [HideInInspector] public int WingColorIndex { get; private set; }
    [HideInInspector] public int BellyColorIndex { get; private set; }
    
    public int[] GetColors()
    {
        return new int[] { CrestColorIndex, HeadColorIndex, WingColorIndex, BellyColorIndex };
    }
    
    public void SetPartColor(int bodyPartIndex, int colorIndex)
    {
        switch (bodyPartIndex)
        {
            case 0:
                CrestColorIndex = colorIndex;
                if (crestRenderer != null)
                    crestRenderer.color = colorPalette[colorIndex];
                break;
            case 1:
                HeadColorIndex = colorIndex;
                if (headRenderer != null)
                    headRenderer.color = colorPalette[colorIndex];
                break;
            case 2:
                WingColorIndex = colorIndex;
                if (wingRenderer != null)
                    wingRenderer.color = colorPalette[colorIndex];
                break;
            case 3:
                BellyColorIndex = colorIndex;
                if (bellyRenderer != null)
                    bellyRenderer.color = colorPalette[colorIndex];
                break;
            default:
                Debug.LogError("Invalid body part index!");
                break;
        }
    }
    
    public void SetColors(int crestIndex, int headIndex, int wingIndex, int bellyIndex)
    {
        SetPartColor(0, crestIndex);
        SetPartColor(1, headIndex);
        SetPartColor(2, wingIndex);
        SetPartColor(3, bellyIndex);
    }
    
    public void RandomizeColors()
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
