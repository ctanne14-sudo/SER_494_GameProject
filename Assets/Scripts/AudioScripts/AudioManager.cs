using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip clickClip;
    private AudioClip selectClip;
    private AudioClip[] footstepClips;
    private string currentMusic = "";
    private string lastFloorScene = "";
    private HashSet<Object> hookedMenus = new HashSet<Object>();
    private float scanTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAudioManager()
    {
        if (Instance != null) return;

        GameObject audioObject = new GameObject("Audio Manager");
        audioObject.AddComponent<AudioManager>();
    }

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        clickClip = Resources.Load<AudioClip>("Audio/sfx_click");
        selectClip = Resources.Load<AudioClip>("Audio/sfx_select");
        footstepClips = Resources.LoadAll<AudioClip>("Audio/Footsteps");

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.StartsWith("Floor")) lastFloorScene = scene.name;
        PlayMusic(MusicForScene(scene.name));
        EnsureListener();
        hookedMenus.Clear();
        scanTimer = 0f;
    }

    string MusicForScene(string sceneName)
    {
        if (sceneName == "TitleMenu" || sceneName == "MainMenu" || sceneName.Contains("Intro")) return "music_title";
        if (sceneName.StartsWith("Floor1")) return "music_floor1";
        if (sceneName.StartsWith("Floor2")) return "music_floor2";
        if (sceneName.StartsWith("Floor3")) return "music_boss";
        if (sceneName.Contains("Combat") && lastFloorScene.StartsWith("Floor3")) return "music_boss_battle";
        if (sceneName.Contains("Combat")) return "music_battle";
        return "music_floor1";
    }

    public string CurrentMusic()
    {
        return currentMusic;
    }

    public void PlayMusic(string clipName)
    {
        if (clipName == currentMusic) return;

        currentMusic = clipName;
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
        StopAllCoroutines();
        StartCoroutine(SwitchMusic(clip));
    }

    IEnumerator SwitchMusic(AudioClip clip)
    {
        while (musicSource.isPlaying && musicSource.volume > 0.01f)
        {
            musicSource.volume -= Time.unscaledDeltaTime * 2f;
            yield return null;
        }

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        while (musicSource.volume < GameSettings.MusicVolume)
        {
            musicSource.volume = Mathf.Min(GameSettings.MusicVolume, musicSource.volume + Time.unscaledDeltaTime);
            yield return null;
        }
    }

    public void SetMusicVolume(float volume)
    {
        GameSettings.MusicVolume = volume;
        musicSource.volume = volume;
    }

    public void PlaySfx(AudioClip clip, float pitch = 1f)
    {
        if (clip == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, GameSettings.SfxVolume);
    }

    public void PlayClick()
    {
        PlaySfx(clickClip);
    }

    public void PlaySelect()
    {
        PlaySfx(selectClip);
    }

    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        AudioClip step = footstepClips[Random.Range(0, footstepClips.Length)];
        PlaySfx(step, Random.Range(0.92f, 1.08f));
    }

    void Update()
    {
        scanTimer -= Time.unscaledDeltaTime;
        if (scanTimer > 0f) return;

        scanTimer = 0.5f;
        EnsureListener();
        HookMenus();
        AddFootsteps();
        ApplySensitivity();
    }

    void EnsureListener()
    {
        AudioListener myListener = GetComponent<AudioListener>();
        if (myListener == null) myListener = gameObject.AddComponent<AudioListener>();

        bool sceneHasListener = false;
        foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (listener != myListener && listener.enabled && listener.gameObject.activeInHierarchy) sceneHasListener = true;
        }

        myListener.enabled = !sceneHasListener;
    }

    void HookMenus()
    {
        foreach (UIDocument menu in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            if (hookedMenus.Contains(menu) || menu.rootVisualElement == null) continue;

            hookedMenus.Add(menu);
            menu.rootVisualElement.RegisterCallback<ClickEvent>(OnMenuClick, TrickleDown.TrickleDown);
            SettingsMenu.Setup(menu.rootVisualElement);
        }

        foreach (UnityEngine.UI.Button button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
        {
            if (hookedMenus.Contains(button)) continue;

            hookedMenus.Add(button);
            button.onClick.AddListener(PlayClick);
        }
    }

    void OnMenuClick(ClickEvent evt)
    {
        if (evt.target is Button) PlayClick();
    }

    void AddFootsteps()
    {
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null && player.GetComponent<Footsteps>() == null)
        {
            player.gameObject.AddComponent<Footsteps>();
        }
    }

    void ApplySensitivity()
    {
        if (!GameSettings.HasCustomSensitivity()) return;

        foreach (CameraMovement cameraMovement in FindObjectsByType<CameraMovement>(FindObjectsSortMode.None))
        {
            cameraMovement.sensitivity = GameSettings.MouseSensitivity;
        }
    }
}
