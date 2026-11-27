using UnityEngine;

public static class GameSettings
{
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";
    private const string SensitivityKey = "MouseSensitivity";
    public const float DefaultMusicVolume = 0.06f;
    public const float DefaultSfxVolume = 0.08f;

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat(MusicKey, DefaultMusicVolume); }
        set { PlayerPrefs.SetFloat(MusicKey, value); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat(SfxKey, DefaultSfxVolume); }
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
