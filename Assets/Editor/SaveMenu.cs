using UnityEditor;

public static class SaveMenu
{
    [MenuItem("Shikacat/Clear Save")]
    private static void ClearSave()
    {
        PlayerProgress.Clear();
        CoinWallet.Clear();
        UnityEngine.Debug.Log("Shikacat: đã xoá tiến độ đã lưu.");
    }

    [MenuItem("Shikacat/Add 1000 Coins")]
    private static void AddCoins()
    {
        CoinWallet.Add(1000);
        UnityEngine.Debug.Log($"Shikacat: coin hiện có {CoinWallet.Balance}.");
    }
}
