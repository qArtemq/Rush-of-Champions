using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    [Header("Video Setting")]
    [SerializeField] GameObject controlsGame;
    [SerializeField] GameObject audioGame;
    [SerializeField] GameObject videoGame;
    [SerializeField] Toggle ToggleFull;
    [SerializeField] Dropdown resolutionDropdown;
    [SerializeField] Dropdown qualityDropdown;

    [Header("Audio Setting")]
    [SerializeField] Toggle allSoundsToggle;
    [SerializeField] Slider generalVolumeSlider;
    [SerializeField] Slider backgroundMusicSlider;
    [SerializeField] AudioMixer audioMixerGeneral;
    [SerializeField] AudioMixer audioMixerBack;
    [SerializeField] AudioMixerGroup audioMixerGroupGeneral;
    [SerializeField] AudioMixerGroup audioMixerGroupBack;

    [Header("Other Setting")]
    [SerializeField] Toggle[] otherSettings;
    [SerializeField] Toggle firstSettings;

    private Resolution[] resolutions;
    private Menu menu;
    private RaceManagerButtons RaceManagerButtons;

    public bool isInGameSettings = false;

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            isInGameSettings = false;
        }
        else
        {
            isInGameSettings = true;
        }
        if (isInGameSettings)
        {
            RaceManagerButtons = FindObjectOfType<RaceManagerButtons>();
        }
        else
        {
            menu = FindObjectOfType<Menu>();
        }
        FirstPage();

        AssignAudioMixerToAllSources();

        InitializeSettings();
        ToggleFull.onValueChanged.AddListener(delegate { SetFullScreen(ToggleFull.isOn); });
        allSoundsToggle.onValueChanged.AddListener(delegate { ToggleAllSounds(allSoundsToggle.isOn); });
        generalVolumeSlider.onValueChanged.AddListener(delegate { SetGeneralVolume(generalVolumeSlider.value); });
        backgroundMusicSlider.onValueChanged.AddListener(delegate { SetBackgroundMusicVolume(backgroundMusicSlider.value); });
    }

    private void FirstPage()
    {
        controlsGame.SetActive(true);
        audioGame.SetActive(false);
        videoGame.SetActive(false);
        foreach (Toggle button in otherSettings)
        {
            button.isOn = false;
        }
        firstSettings.isOn = true;
    }

    void Update()
    {
        if (isInGameSettings)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && RaceManagerButtons.inSettings)
            {
                RaceManagerButtons.settingsGame.SetActive(false);
                RaceManagerButtons.pauseGame.SetActive(true);
                RaceManagerButtons.inSettings = false;
                FirstPage();

                SaveSettings();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                menu.CloseSettings();
                FirstPage();
                SaveSettings();
            }
        }
    }
    // Метод для возобновления всех звуков
    public void ResumeNonBackgroundSounds()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            // Проверяем, что это не фоновая музыка
            if (audioSource.gameObject.name != "Background Music")
            {
                audioSource.UnPause();
            }
        }
    }

    public void PauseNonBackgroundSounds()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            // Проверяем, что это не фоновая музыка
            if (audioSource.gameObject.name != "Background Music")
            {
                audioSource.Pause();
            }
        }
    }
    public void QuitSetting()
    {
        if (isInGameSettings)
        {
            RaceManagerButtons.settingsGame.SetActive(false);
            RaceManagerButtons.pauseGame.SetActive(true);
            RaceManagerButtons.inSettings = false;
            FirstPage();

            SaveSettings();
        }
        else
        {
            menu.CloseSettings();
            FirstPage();
            SaveSettings();
        }
    }
    public void ControlSetting()
    {
        controlsGame.SetActive(true);
        audioGame.SetActive(false);
        videoGame.SetActive(false);
    }
    public void AudioSetting()
    {
        controlsGame.SetActive(false);
        audioGame.SetActive(true);
        videoGame.SetActive(false);
    }
    public void VideoSetting()
    {
        controlsGame.SetActive(false);
        audioGame.SetActive(false);
        videoGame.SetActive(true);

        if (ToggleFull.isOn != Screen.fullScreen)
        {
            ToggleFull.isOn = Screen.fullScreen;
        }
    }
    void InitializeSettings()
    {
        // Получаем доступные разрешения и заполняем dropdown
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> resolutionOptions = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            resolutionOptions.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(resolutionOptions);
        resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();

        // Устанавливаем начальное значение качества
        qualityDropdown.value = PlayerPrefs.GetInt("Quality", 2);
        qualityDropdown.RefreshShownValue();

        // Настройка начального значения для полноэкранного режима
        bool isFullScreen = PlayerPrefs.GetInt("FullScreen", 1) == 1;
        Screen.fullScreen = isFullScreen;
        ToggleFull.isOn = isFullScreen;

        // Инициализация настроек звука
        allSoundsToggle.isOn = PlayerPrefs.GetInt("AllSounds", 1) == 1;
        generalVolumeSlider.value = PlayerPrefs.GetFloat("GeneralVolume", 1.0f);
        backgroundMusicSlider.value = PlayerPrefs.GetFloat("BackgroundMusic", 1.0f);

        // Устанавливаем параметры экрана и качества
        ApplyResolution();
        ApplyQuality();
        ApplyAudioSettings();
    }
    // Метод для назначения AudioMixer ко всем AudioSource, кроме фоновой музыки
    private void AssignAudioMixerToAllSources()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            // Пропускаем фоновую музыку, если у нее определенное имя
            if (audioSource.gameObject.name == "Background Music")
            {
                audioSource.outputAudioMixerGroup = audioMixerGroupBack;
            }
            else
            {
                audioSource.outputAudioMixerGroup = audioMixerGroupGeneral;
            }
        }
    }
    public void ApplyAudioSettings()
    {
        ToggleAllSounds(allSoundsToggle.isOn);
        SetGeneralVolume(generalVolumeSlider.value);
        SetBackgroundMusicVolume(backgroundMusicSlider.value);
    }
    // Метод для установки уровня громкости фоновой музыки
    public void SetBackgroundMusicVolume(float volume)
    {
        // Предполагается, что у вас есть источник звука для фоновой музыки
        AudioSource backgroundMusic = GameObject.Find("Background Music").GetComponent<AudioSource>();
        if (backgroundMusic != null)
        {
            audioMixerBack.SetFloat("BackGraundVolume", volume);
        }
        PlayerPrefs.SetFloat("BackgroundMusic", volume);
        PlayerPrefs.Save();
    }
    // Метод для установки общего уровня громкости
    public void SetGeneralVolume(float volume)
    {
        audioMixerGeneral.SetFloat("GeneralVolume", volume);
        PlayerPrefs.SetFloat("GeneralVolume", volume);
        PlayerPrefs.Save();
    }
    // Метод для включения/выключения всех звуков
    public void ToggleAllSounds(bool isEnabled)
    {
        AudioListener.pause = !isEnabled;
        PlayerPrefs.SetInt("AllSounds", isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;

        // Сохранение состояния экрана
        PlayerPrefs.SetInt("FullScreen", isFullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void ApplyResolution()
    {
        SetResolution(resolutionDropdown.value);
    }

    public void ApplyQuality()
    {
        SetQuality(qualityDropdown.value);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("Resolution", resolutionDropdown.value);
        PlayerPrefs.SetInt("Quality", qualityDropdown.value);
        PlayerPrefs.SetInt("FullScreen", ToggleFull.isOn ? 1 : 0);
        PlayerPrefs.SetInt("AllSounds", allSoundsToggle.isOn ? 1 : 0);
        PlayerPrefs.SetFloat("GeneralVolume", generalVolumeSlider.value);
        PlayerPrefs.SetFloat("BackgroundMusic", backgroundMusicSlider.value);
        PlayerPrefs.Save();
    }

    public void ResetToDefault()
    {
        // Устанавливаем настройки по умолчанию
        resolutionDropdown.value = resolutions.Length - 1; // Последнее разрешение
        qualityDropdown.value = 2; // Среднее качество
        ToggleFull.isOn = true; // Полный экран
        allSoundsToggle.isOn = true;
        generalVolumeSlider.value = 1.0f;
        backgroundMusicSlider.value = 1.0f;

        ApplyAudioSettings();
        ApplyResolution();
        ApplyQuality();
        Screen.fullScreen = true;


        SaveSettings();
    }
}