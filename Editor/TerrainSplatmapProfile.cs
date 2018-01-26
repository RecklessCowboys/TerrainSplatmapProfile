using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TerrainSplatmapProfile : ScriptableObject
{
    public TerrainData target;
    public List<TextureDefinition> textureDefinitions;

    // Apply the texture definitions to the target. Call CanApply to validate inputs.
    public void Apply()
    {
        ApplyTextureDefinitions(textureDefinitions, target);
    }

    // Returns true if Apply can be called. Returns false if Apply cannot be called, and gives >= 1 reason why not.
    public bool CanApply(List<string> reasons)
    {
        if (target == null)
        {
            reasons.Add("Terrain Data Target not set.");
        }

        if (textureDefinitions.Count == 0)
        {
            reasons.Add("No texture definitions.");
        }
        else
        {
            int i = -1;
            bool haveAllAlphamaps = AllTextureDefinitionsHaveAlphamaps(textureDefinitions, ref i);
            if (!haveAllAlphamaps)
            {
                var msg = string.Format("Texture Defintion at position {0} is missing an Alphamap.", i);
                reasons.Add(msg);
            }

            i = -1;
            bool allReadable = false;
            if (haveAllAlphamaps)
            {
                if (AllTextureDefinitionsHaveReadableAlphamaps(textureDefinitions, ref i))
                {
                    allReadable = true;
                }
                else
                {
                    var msg = string.Format("Texture Defintion at position {0} has unreadable Alphamap.", i);
                    reasons.Add(msg);
                }
            }

            if (target != null && haveAllAlphamaps && allReadable)
            {
                Vector2 firstUncoveredPosition = new Vector2(-1, -1);
                if (!TerrainIsFullyCovered(target.alphamapWidth, target.alphamapHeight, textureDefinitions, ref firstUncoveredPosition))
                {
                    var msg = string.Format(
                        "Every alphamap is fully transparent at position ({0},{1}). Update your alphamaps so that at least one covers this position.",
                        firstUncoveredPosition.x, firstUncoveredPosition.y);
                    reasons.Add(msg);
                }
            }
        }

        return reasons.Count == 0;
    }

    //
    // Apply Helpers
    //

    private static void ApplyTextureDefinitions(List<TextureDefinition> textureDefinitions, TerrainData terrainData)
    {
        SplatPrototype[] newSplatPrototypes = new SplatPrototype[textureDefinitions.Count];

        for (int i = 0; i < textureDefinitions.Count; i++)
        {
            var td = textureDefinitions[i];
            newSplatPrototypes[i] = td.splatPrototypeData.ToSplatPrototype();
        }

        var normalizedAlphamaps = NormalizeAlphamaps(terrainData.alphamapWidth, terrainData.alphamapHeight, textureDefinitions);

        terrainData.splatPrototypes = newSplatPrototypes;
        terrainData.SetAlphamaps(0, 0, normalizedAlphamaps);
    }

    private static float[,,] NormalizeAlphamaps(int width, int height, List<TextureDefinition> textureDefinitions)
    {
        // Compute the sum of the alphamaps at each pixel. This will be used to normalize the maps in the next step.
        float[,] summedAlphamaps = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float sum = 0;
                for (int i = 0; i < textureDefinitions.Count; i++)
                {
                    sum += textureDefinitions[i].alphamap.GetPixel(x, height - z).grayscale;
                }
                summedAlphamaps[x, z] = sum;
            }
        }

        // Normalize the alphamap using summedAlphamaps.
        float[,,] normalizedAlphamaps = new float[height, width, textureDefinitions.Count];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                for (int i = 0; i < textureDefinitions.Count; i++)
                {
                    normalizedAlphamaps[z, x, i] = textureDefinitions[i].alphamap.GetPixel(x, height - z).grayscale / summedAlphamaps[x, z];
                }
            }
        }

        return normalizedAlphamaps;
    }

    //
    // Validation Helpers
    //

    // Returns true if every textureDefinitions has a non-null alphamap. False otherwise.
    private static bool AllTextureDefinitionsHaveAlphamaps(List<TextureDefinition> textureDefinitions, ref int indexOfFirstItemWithoutAlphamap)
    {
        for (int i = 0; i < textureDefinitions.Count; i++)
        {
            if (textureDefinitions[i].alphamap == null)
            {
                indexOfFirstItemWithoutAlphamap = i;
                return false;
            }
        }

        return true;
    }

    private static bool AllTextureDefinitionsHaveReadableAlphamaps(List<TextureDefinition> textureDefinitions, ref int indexOfFirstNonReadableTexture)
    {
        for (int i = 0; i < textureDefinitions.Count; i++)
        {
            var path = AssetDatabase.GetAssetPath(textureDefinitions[i].alphamap);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer && !importer.isReadable)
            {
                indexOfFirstNonReadableTexture = i;
                return false;
            }
        }

        return true;
    }

    // Verify the map has full coverage, that there are no pixels who's corresponding alphamap values sum up to 0.
    // Check that ValidateAllTexturesHaveAlphamaps returns true before calling this.
    private static bool TerrainIsFullyCovered(int width, int height, List<TextureDefinition> textureDefinitions, ref Vector2 firstUncoveredPosition)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float sum = 0;
                for (int i = 0; i < textureDefinitions.Count && sum == 0; i++)
                {
                    sum += textureDefinitions[i].alphamap.GetPixel(x, z).grayscale;
                }
                if (sum == 0)
                {
                    firstUncoveredPosition = new Vector2(x, z);
                    return false;
                }
            }
        }

        return true;
    }
}
