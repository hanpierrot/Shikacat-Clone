using UnityEditor;

public static class SaveMenu
{
    [MenuItem("Shikacat/Clear Save")]
    private static void ClearSave()
    {
        PlayerProgress.Clear();
        BoosterInventory.Clear();
        CoinWallet.Clear();
        UnityEngine.Debug.Log("Shikacat: đã xoá tiến độ đã lưu.");
    }
}
