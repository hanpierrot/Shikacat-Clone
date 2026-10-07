using System;
using UnityEngine;

[Serializable]
public class LevelCollection
{
    public int version = 1;
    public LevelData[] levels = Array.Empty<LevelData>();
}

public static class LevelLoader
{
    public static LevelCollection Parse(string json)
    {
        if (TryParse(json, out LevelCollection collection, out string error)) return collection;

        Debug.LogError($"LevelLoader: Cannot read JSON: {error}");
        return new LevelCollection();
    }
    
    public static bool TryParse(string json, out LevelCollection collection, out string error)
    {
        try
        {
            collection = JsonUtility.FromJson<LevelCollection>(json);
        }
        catch (ArgumentException e)
        {
            collection = null;
            error = e.Message;
            return false;
        }
        
        if (collection == null)
        {
            error = "Empty json file";
            return false;
        }
        
        if (collection.levels == null) collection.levels = Array.Empty<LevelData>();
        foreach (LevelData level in collection.levels)
            level.Prepare();

        error = null;
        return true;
    }

    public static string ToJson(LevelCollection collection) => JsonUtility.ToJson(collection, true);
    
    public static LevelData[] Load(TextAsset asset)
    {
        if (asset == null)
        {
            Debug.LogError("LevelLoader: missing LevelData.");
            return Array.Empty<LevelData>();
        }
        
        LevelCollection collection = Parse(asset.text);
        for(int i = 0; i < collection.levels.Length; i++)
            if(!collection.levels[i].Validate(out string error))
                Debug.LogError($"LevelLoader: level {i + 1} invalid: {error}");

        return collection.levels;
    }
}
