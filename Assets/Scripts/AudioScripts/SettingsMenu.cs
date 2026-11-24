using UnityEngine;
using UnityEngine.UIElements;

public static class SettingsMenu
{
    public static void Setup(VisualElement root)
    {
        Slider music = root.Q<Slider>("musicVolumeSlider");
        Slider sfx = root.Q<Slider>("sfxVolumeSlider");
        Slider sensitivity = root.Q<Slider>("sensitivitySlider");

        if (music != null)
        {
            music.SetValueWithoutNotify(GameSettings.MusicVolume);
            ShowPercent(music, "Music Volume");
            music.RegisterValueChangedCallback(evt =>
            {
                AudioManager.Instance.SetMusicVolume(evt.newValue);
                ShowPercent(music, "Music Volume");
            });
        }

        if (sfx != null)
        {
            sfx.SetValueWithoutNotify(GameSettings.SfxVolume);
            ShowPercent(sfx, "Sound Effects");
            sfx.RegisterValueChangedCallback(evt =>
            {
                GameSettings.SfxVolume = evt.newValue;
                ShowPercent(sfx, "Sound Effects");
            });
            sfx.RegisterCallback<PointerUpEvent>(evt => AudioManager.Instance.PlaySelect(), TrickleDown.TrickleDown);
        }

        if (sensitivity != null)
        {
            sensitivity.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            sensitivity.label = "Mouse Sensitivity  " + sensitivity.value.ToString("0.0");
            sensitivity.RegisterValueChangedCallback(evt =>
            {
                GameSettings.MouseSensitivity = evt.newValue;
                sensitivity.label = "Mouse Sensitivity  " + evt.newValue.ToString("0.0");
            });
        }
    }

    static void ShowPercent(Slider slider, string name)
    {
        slider.label = name + "  " + Mathf.RoundToInt(slider.value * 100) + "%";
    }
}
