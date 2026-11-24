using UnityEngine;

public static class GameSettings
{
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";
    private const string SensitivityKey = "MouseSensitivity";

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat(MusicKey, 0.6f); }
        set { PlayerPrefs.SetFloat(MusicKey, value); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat(SfxKey, 0.8f); }
        set { PlayerPrefs.SetFloat(SfxKey, value); PlayerPrefs.Save(); }
    }

    public static float MouseSensitivity
    {
        get { return PlayerPrefs.GetFloat(SensitivityKey, 5f); }
        set { PlayerPrefs.SetFloat(SensitivityKey, value); PlayerPrefs.Save(); }
    }

    public static bool HasCustomSensitivity()
    {
        return PlayerPrefs.HasKey(SensitivityKey);
    }
}
