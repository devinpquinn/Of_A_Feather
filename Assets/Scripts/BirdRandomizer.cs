using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BirdRandomizer : MonoBehaviour
{
    [SerializeField] private Color[] colorPalette = new Color[4];
    
    [Header("Connection Settings")]
    [SerializeField] private Color connectionColor = Color.white;
    
    public Color ConnectionColor => connectionColor;
    
    [Header("Body Part References")]
    [SerializeField] private SpriteRenderer crestRenderer;
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private SpriteRenderer wingRenderer;
    [SerializeField] private SpriteRenderer bellyRenderer;
    
    public GameObject pedestal;
    
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
    
    public bool CheckMismatched(int[] partnerColors)
    {
        return CrestColorIndex != partnerColors[0] &&
               HeadColorIndex != partnerColors[1] &&
               WingColorIndex != partnerColors[2] &&
               BellyColorIndex != partnerColors[3];
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
    
    public void FlashMatchingParts(int[] partnerColors)
    {
        List<int> matchingParts = new List<int>();
        
        if (CrestColorIndex == partnerColors[0])
            matchingParts.Add(0);
        if (HeadColorIndex == partnerColors[1])
            matchingParts.Add(1);
        if (WingColorIndex == partnerColors[2])
            matchingParts.Add(2);
        if (BellyColorIndex == partnerColors[3])
            matchingParts.Add(3);
        
        if (matchingParts.Count > 0)
        {
            StartCoroutine(FlashPartsCoroutine(matchingParts));
        }
    }
    
    private IEnumerator FlashPartsCoroutine(List<int> partIndices)
    {
        // Store original colors
        Dictionary<int, Color> originalColors = new Dictionary<int, Color>();
        
        foreach (int partIndex in partIndices)
        {
            SpriteRenderer renderer = GetRendererForPart(partIndex);
            if (renderer != null)
            {
                originalColors[partIndex] = renderer.color;
                renderer.color = Color.white;
            }
        }
        
        // Lerp back to original colors over 0.5 seconds
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            foreach (int partIndex in partIndices)
            {
                SpriteRenderer renderer = GetRendererForPart(partIndex);
                if (renderer != null && originalColors.ContainsKey(partIndex))
                {
                    renderer.color = Color.Lerp(Color.white, originalColors[partIndex], t);
                }
            }
            
            yield return null;
        }
        
        // Ensure final colors are set correctly
        foreach (int partIndex in partIndices)
        {
            SpriteRenderer renderer = GetRendererForPart(partIndex);
            if (renderer != null && originalColors.ContainsKey(partIndex))
            {
                renderer.color = originalColors[partIndex];
            }
        }
    }
    
    private SpriteRenderer GetRendererForPart(int partIndex)
    {
        switch (partIndex)
        {
            case 0: return crestRenderer;
            case 1: return headRenderer;
            case 2: return wingRenderer;
            case 3: return bellyRenderer;
            default: return null;
        }
    }
}
