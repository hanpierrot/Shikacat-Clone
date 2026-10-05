using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelCollection
{
    public int version;
    public LevelData[] levels = Array.Empty<LevelData>();
}

public class LevelLoader
{
    public static LevelData[] Load(TextAsset asset)
    {
        if (asset == null)
        {
            Debug.LogError("LevelLoader: missing LevelData.");
            return Array.Empty<LevelData>();
        }
        
        var collection = JsonUtility.FromJson<LevelCollection>(asset.text);
        for (int i = 0; i < collection.levels.Length; i++)
        {
            LevelData level = collection.levels[i];
            level.Prepare();
            if(!level.Validate(out string error))
                Debug.LogError($"LevelLoader: level {i + 1} invalid: {error}");
        }
        
        return collection.levels;
    }
}
