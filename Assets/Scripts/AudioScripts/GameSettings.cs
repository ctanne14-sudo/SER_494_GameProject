using UnityEngine;

public static class GameSettings
{
    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";
    private const string SensitivityKey = "MouseSensitivity";
    public const float DefaultMasterVolume = 1f;
    public const float DefaultMusicVolume = 0.06f;
    public const float DefaultSfxVolume = 0.08f;

    public static float MasterVolume
    {
        get { return PlayerPrefs.GetFloat(MasterKey, DefaultMasterVolume); }
        set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat(MusicKey, DefaultMusicVolume); }
        set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat(SfxKey, DefaultSfxVolume); }
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float EffectiveMusicVolume
    {
        get { return MasterVolume * MusicVolume; }
    }

    public static float EffectiveSfxVolume
    {
        get { return MasterVolume * SfxVolume; }
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
