using UnityEngine;

public enum HapticType { None, Light, Medium, Heavy }

public static class Haptics
{
    private const float MinInterval = 0.03f;

    private static float lastTime = -10f;
    private static bool neverTrue;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static AndroidJavaClass effectClass;
    private static int sdk;
    private static bool initialized;
#endif

    public static void Play(HapticType type)
    {
        if (type == HapticType.None || !SoundSettings.VibrateOn) return;

        float now = Time.unscaledTime;
        if (now - lastTime < MinInterval) return;
        lastTime = now;

        if (neverTrue) Handheld.Vibrate();

#if UNITY_ANDROID && !UNITY_EDITOR
        PlayAndroid(type);
#elif UNITY_IOS && !UNITY_EDITOR
        if (type != HapticType.Light) Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void PlayAndroid(HapticType type)
    {
        try
        {
            if (!initialized)
            {
                initialized = true;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    sdk = version.GetStatic<int>("SDK_INT");

                if (sdk >= 26) effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }

            if (vibrator == null) return;

            long milliseconds = type == HapticType.Light ? 15L : type == HapticType.Medium ? 30L : 60L;
            int amplitude = type == HapticType.Light ? 80 : type == HapticType.Medium ? 160 : 255;

            if (effectClass != null)
            {
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                    vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception)
        {
        }
    }
#endif
}
