using UnityEngine;

public static class LevelRepository
{
    private static TextAsset cachedAsset;
    private static LevelData[] cached;

    public static LevelData[] Get(TextAsset asset)
    {
        if (asset != null && asset == cachedAsset && cached != null) return cached;

        cached = LevelLoader.Load(asset);
        cachedAsset = asset;
        return cached;
    }
}
