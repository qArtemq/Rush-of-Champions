using UnityEngine;
using UnityEngine.SceneManagement;
public class RaceManagerButtons : MonoBehaviour
{
    [SerializeField] public GameObject pauseGame;
    [SerializeField] GameObject choiceMenuGame;
    [SerializeField] GameObject choiceRestartGame;
    [SerializeField] GameObject choiceQuitGame;
    [SerializeField] GameObject backGraund;
    [SerializeField] public GameObject settingsGame;
    public bool inSettings = false;
    bool isChoise = false;
    Setting setting;
    void Start()
    {
        setting = FindObjectOfType<Setting>();

        pauseGame.SetActive(false);
        choiceMenuGame.SetActive(false);
        choiceRestartGame.SetActive(false);
        choiceQuitGame.SetActive(false);
        backGraund.SetActive(false);
        settingsGame.SetActive(false);

        // Скрываем и блокируем курсор в начале игры
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !inSettings)
        {
            if (isChoise)
            {
                CloseAllChoiceMenus();
            }
            else
            {
                TogglePause();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            if (choiceMenuGame.activeSelf)
            {
                YesGoToMenu();
            }
            else if (choiceQuitGame.activeSelf)
            {
                YesQuitGame();
            }
            else if (choiceRestartGame.activeSelf)
            {
                YesRestartGame();
            }
        }
    }
    private void OpenChoiceMenu(GameObject choiceMenu)
    {
        CloseAllChoiceMenus();
        pauseGame.SetActive(false);
        choiceMenu.SetActive(true);
        isChoise = true;
    }
    private void CloseAllChoiceMenus()
    {
        choiceMenuGame.SetActive(false);
        choiceRestartGame.SetActive(false);
        choiceQuitGame.SetActive(false);
        pauseGame.SetActive(true);
        isChoise = false;
    }
    private void TogglePause()
    {
        if (Time.timeScale == 1f)
        {
            Time.timeScale = 0f;
            pauseGame.SetActive(true);
            backGraund.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PauseAllSoundsExceptBackground();
        }
        else
        {
            Time.timeScale = 1f;
            pauseGame.SetActive(false);
            backGraund.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ResumeAllPausedSounds();
        }
    }
    public void ContinueGame()
    {
        Time.timeScale = 1f;
        pauseGame.SetActive(false);
        backGraund.SetActive(false);

        // Скрываем курсор при продолжении игры
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ResumeAllPausedSounds(); // Возобновляем все звуки
    }
    private void PauseAllSoundsExceptBackground()
    {
        setting.PauseNonBackgroundSounds(); // Вызов метода из Setting для остановки всех звуков
    }
    private void ResumeAllPausedSounds()
    {
        setting.ResumeNonBackgroundSounds(); // Вызов метода из Setting для возобновления всех звуков
    }
    public void YesRestartGame()
    {
        // Получаем текущую сцену
        Scene currentScene = SceneManager.GetActiveScene();

        // Перезагружаем текущую сцену
        SceneManager.LoadScene(currentScene.name);

        if(Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        // Скрываем курсор после перезапуска
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void NoRestartGame()
    {
        pauseGame.SetActive(true);
        choiceRestartGame.SetActive(false);
        isChoise = false;
    }
    public void ChoiceRestartGame()
    {
        OpenChoiceMenu(choiceRestartGame);
    }
    public void SettingGame()
    {
        pauseGame.SetActive(false);
        inSettings = true;
        settingsGame.SetActive(inSettings);
    }
    public void YesGoToMenu()
    {
        SceneManager.LoadScene("Menu");
        Time.timeScale = 1f;
    }
    public void NoGoToMenu()
    {
        pauseGame.SetActive(true);
        choiceMenuGame.SetActive(false);
        isChoise = false;
    }
    public void ChoiceGoToMenuGame()
    {
        OpenChoiceMenu(choiceMenuGame);
    }
    public void YesQuitGame()
    {
        Application.Quit();
    }
    public void NoQuitGame()
    {
        pauseGame.SetActive(true);
        choiceQuitGame.SetActive(false);
        isChoise = false;
    }
    public void ChoiceQuitGame()
    {
        OpenChoiceMenu(choiceQuitGame);
    }
}
