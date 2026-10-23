using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class CheatMenu : MonoBehaviour
{
    private bool open;
    private Vector2 scroll;
    private string levelInput = "1";
    private string coinInput = "200";
    private float timeScale = 1f;
    private bool showFps;
    private float fpsAccum;
    private int fpsFrames;
    private float fps;

    private void Awake() => DontDestroyOnLoad(gameObject);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) SetOpen(!open);

        fpsAccum += Time.unscaledDeltaTime;
        fpsFrames++;
        if (fpsAccum >= 0.5f)
        {
            fps = fpsFrames / fpsAccum;
            fpsAccum = 0f;
            fpsFrames = 0;
        }
    }
    
    private void SetOpen(bool value)
    {
        open = value;
        GridInputHandler.Blocked = value;
    }
    
    private void OnDisable()
    {
        GridInputHandler.Blocked = false;
        Time.timeScale = 1f;
    }
    
    private void OnGUI()
    {
        float scale = Mathf.Clamp(Screen.height / 900f, 1f, 3f);   // Co theo độ phân giải để dễ chạm trên điện thoại
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        if (showFps) GUI.Label(new Rect(w - 140, 4, 136, 24), $"{fps:0} fps");

        if (!open)
        {
            if (GUI.Button(new Rect(4, 4, 90, 34), "CHEAT")) SetOpen(true);
            return;
        }

        GameManager gm = GameManager.Instance;
        GUI.Box(new Rect(8, 8, w - 16, h - 16), "CHEAT MENU");
        if (GUI.Button(new Rect(w - 76, 12, 60, 28), "Close")) SetOpen(false);
        if (gm == null) return;

        GUILayout.BeginArea(new Rect(16, 44, w - 32, h - 60));
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label($"Level {gm.CurrentLevelNumber}/{gm.LevelCount}   Coins {CoinWallet.Balance}   " +
                        $"Time {(gm.HasTimeLimit ? gm.TimeRemaining.ToString("0.0") : "-")}   " +
                        $"Moves {(gm.HasMoveLimit ? gm.MovesRemaining.ToString() : "-")}   " +
                        $"Stars override {(gm.ForcedStars == 0 ? "off" : gm.ForcedStars.ToString())}");

        Header("Coins");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+100")) CoinWallet.Add(100);
        if (GUILayout.Button("+1000")) CoinWallet.Add(1000);
        if (GUILayout.Button("Set 0")) SetCoins(0);
        if (GUILayout.Button("Set 200")) SetCoins(CoinWallet.StartingCoins);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        coinInput = GUILayout.TextField(coinInput, GUILayout.Width(120));
        if (GUILayout.Button("Set coins") && int.TryParse(coinInput, out int coins)) SetCoins(coins);
        GUILayout.EndHorizontal();

        Header("Level");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("WIN now")) { gm.CheatWin(); SetOpen(false); }
        if (GUILayout.Button("LOSE (time out)")) { gm.CheatLose(true); SetOpen(false); }
        if (GUILayout.Button("LOSE (out of moves)")) { gm.CheatLose(false); SetOpen(false); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restart")) { gm.CheatRestart(); SetOpen(false); }
        if (GUILayout.Button("< Prev")) { gm.CheatGoToLevel(gm.CurrentLevelNumber - 1); SetOpen(false); }
        if (GUILayout.Button("Next >")) { gm.CheatGoToLevel(gm.CurrentLevelNumber + 1); SetOpen(false); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        levelInput = GUILayout.TextField(levelInput, GUILayout.Width(120));
        if (GUILayout.Button("Go to level") && int.TryParse(levelInput, out int level))
        {
            gm.CheatGoToLevel(level);
            SetOpen(false);
        }
        if (GUILayout.Button("Save this level")) gm.CheatSaveCurrentLevel();
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Replay tutorial")) { gm.CheatReplayTutorial(); SetOpen(false); }
        if (GUILayout.Button("Reset booster + mechanic tutorials")) { BoosterTutorial.ResetSeen(); MechanicIntro.ResetSeen(); }

        Header("Limits");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+10s")) gm.CheatAddTime(10f);
        if (GUILayout.Button("+30s")) gm.CheatAddTime(30f);
        if (GUILayout.Button("+3 moves")) gm.CheatAddMoves(3);
        if (GUILayout.Button("+10 moves")) gm.CheatAddMoves(10);
        GUILayout.EndHorizontal();
        gm.UnlimitedTime = GUILayout.Toggle(gm.UnlimitedTime, "Unlimited time");
        gm.UnlimitedMoves = GUILayout.Toggle(gm.UnlimitedMoves, "Unlimited moves");

        Header("Boosters");
        BoosterManager bm = BoosterManager.Instance;
        if (bm != null)
        {
            bm.UnlimitedBoosters = GUILayout.Toggle(bm.UnlimitedBoosters, "Unlimited boosters (không trừ kho, không bật popup mua)");

            foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType)))
            {
                GUILayout.Label($"{type}: stock {BoosterInventory.Count(type)}   {(bm.IsUnlocked(type) ? "unlocked" : "LOCKED")}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+1")) BoosterInventory.Add(type, 1);
                if (GUILayout.Button("+5")) BoosterInventory.Add(type, 5);
                if (GUILayout.Button("Stock 0")) SetBoosterStock(type, 0);
                if (GUILayout.Button("Unlock")) bm.CheatUnlock(type);
                if (GUILayout.Button("Use free")) { bm.CheatUse(type); SetOpen(false); }
                if (GUILayout.Button("Buy popup")) { bm.RequestPurchase(type); SetOpen(false); }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock all"))
                foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType))) bm.CheatUnlock(type);
            if (GUILayout.Button("Reset boosters (khóa lại + kho 0)")) BoosterInventory.Clear();
            if (GUILayout.Button("End freeze")) gm.CheatEndFreeze();
            GUILayout.EndHorizontal();
        }
        
        Header("Other");
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Force stars on win: {(gm.ForcedStars == 0 ? "off" : gm.ForcedStars.ToString())}", GUILayout.Width(220));
        if (GUILayout.Button("Cycle (off/1/2/3)")) gm.ForcedStars = (gm.ForcedStars + 1) % 4;
        GUILayout.EndHorizontal();
        GUILayout.Label($"Time scale: {timeScale:0.00}x");
        float newScale = GUILayout.HorizontalSlider(timeScale, 0.1f, 3f);
        if (!Mathf.Approximately(newScale, timeScale))
        {
            timeScale = newScale;
            Time.timeScale = timeScale;
        }
        showFps = GUILayout.Toggle(showFps, "Show FPS");

        Header("Save");
        GUI.color = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("CLEAR SAVE (progress + coins) and go to level 1"))
        {
            gm.CheatClearSave();
            SetOpen(false);
        }
        GUI.color = Color.white;

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static void SetCoins(int target)
    {
        int delta = target - CoinWallet.Balance;
        if (delta > 0) CoinWallet.Add(delta);
        else if (delta < 0) CoinWallet.TrySpend(-delta);
    }
    
    private static void SetBoosterStock(BoosterType type, int target)
    {
        while (BoosterInventory.Count(type) > target && BoosterInventory.TryConsume(type)) { }
        int delta = target - BoosterInventory.Count(type);
        if (delta > 0) BoosterInventory.Add(type, delta);
    }

    private static void Header(string text)
    {
        GUILayout.Space(8);
        GUILayout.Label("— " + text + " —");
    }
}
#else
public class CheatMenu : MonoBehaviour
{
    private void Awake() => Destroy(gameObject);
}
#endif
