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
    private string suspendedExplorationMusic = "";
    private float suspendedExplorationTime;
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
        bool isCombatScene = scene.name.Contains("Combat");
        if (isCombatScene && IsExplorationMusic(currentMusic) && musicSource.clip != null)
        {
            suspendedExplorationMusic = currentMusic;
            suspendedExplorationTime = musicSource.time;
        }

        if (scene.name.StartsWith("Floor")) lastFloorScene = scene.name;
        string nextMusic = MusicForScene(scene.name);
        bool shouldResumeExploration = !isCombatScene
            && nextMusic == suspendedExplorationMusic
            && IsBattleMusic(currentMusic);
        PlayMusic(nextMusic, shouldResumeExploration ? suspendedExplorationTime : 0f);
        if (!isCombatScene)
        {
            suspendedExplorationMusic = "";
            suspendedExplorationTime = 0f;
        }

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
        PlayMusic(clipName, 0f);
    }

    private void PlayMusic(string clipName, float startTime)
    {
        if (clipName == currentMusic) return;

        AudioClip clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip == null)
        {
            Debug.LogWarning($"Audio clip 'Audio/{clipName}' could not be loaded.", this);
            return;
        }

        currentMusic = clipName;
        StopAllCoroutines();
        StartCoroutine(SwitchMusic(clip, startTime));
    }

    IEnumerator SwitchMusic(AudioClip clip, float startTime)
    {
        while (musicSource.isPlaying && musicSource.volume > 0.01f)
        {
            musicSource.volume -= Time.unscaledDeltaTime * 2f;
            yield return null;
        }

        musicSource.clip = clip;
        musicSource.volume = 0f;
        if (startTime > 0f && clip.length > 0f)
        {
            musicSource.time = Mathf.Min(startTime, Mathf.Max(0f, clip.length - 0.01f));
        }
        musicSource.Play();

        while (musicSource.volume < GameSettings.EffectiveMusicVolume)
        {
            musicSource.volume = Mathf.Min(GameSettings.EffectiveMusicVolume, musicSource.volume + Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private static bool IsExplorationMusic(string clipName)
    {
        return clipName == "music_floor1"
            || clipName == "music_floor2"
            || clipName == "music_boss";
    }

    private static bool IsBattleMusic(string clipName)
    {
        return clipName == "music_battle" || clipName == "music_boss_battle";
    }

    public void SetMasterVolume(float volume)
    {
        GameSettings.MasterVolume = volume;
        musicSource.volume = GameSettings.EffectiveMusicVolume;
    }

    public void SetMusicVolume(float volume)
    {
        GameSettings.MusicVolume = volume;
        musicSource.volume = GameSettings.EffectiveMusicVolume;
    }

    public void PlaySfx(AudioClip clip, float pitch = 1f)
    {
        if (clip == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, GameSettings.EffectiveSfxVolume);
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
